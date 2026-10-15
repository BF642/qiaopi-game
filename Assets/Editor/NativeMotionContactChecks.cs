using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static partial class NativeMotionContactChecks
    {
        [Serializable] public sealed class Row {
            public string pose; public float surfaceHeight, time, leftSole, rightSole, seatBottom;
            public Vector3 hips, leftThigh, rightThigh, leftKnee, rightKnee, leftFoot, rightFoot;
            public Vector3 leftPalm, rightPalm, seatPoint;
        }
        [Serializable] public sealed class Report { public List<Row> rows = new List<Row>(); }
        public static void RunWorker()
        {
            GameObject actor = null;
            try {
                actor = ModelLibrary.Spawn("Worker", null, Vector3.zero);
                var performance = CharacterPerformance.For(actor); performance.Automatic = false;
                var rig = actor.GetComponent<NativeCharacterRig>();
                var report = new Report();
                var skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var labels = new List<int[]>();
                var hip = rig.Joint("BodyRoot").position;
                foreach (var skin in skins) {
                    var vertices = WorldVertices(skin);
                    var label = new int[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++) {
                        Vector3 p = vertices[i];
                        if (p.y < .13f) label[i] = p.x < 0 ? 1 : 2;
                        if (p.y > hip.y - .22f && p.y < hip.y + .02f && p.z < hip.z -.025f && Mathf.Abs(p.x) < .16f) label[i] = 3;
                    }
                    labels.Add(label);
                }
                foreach (string pose in new[] {"Idle", "Walking", "Seated", "SeatedLow", "SeatedHigh"}) {
                    performance.ResetPose(); performance.SetLocomotion(pose == "Walking" ? 1 : 0);
                    float height=pose=="SeatedLow" ? .38f : pose=="SeatedHigh" ? .48f : .43f;
                    performance.SetSeated(pose.StartsWith("Seated"), height);
                    for (int i = 0; i < 450; i++) {
                        performance.Step(1f/30);
                        if (i < 29 || (!pose.StartsWith("Seated") && i % 30 !=29)) continue;
                        var row = new Row { pose=pose, surfaceHeight=height, time=(i+1)/30f, leftSole=float.PositiveInfinity,
                            rightSole=float.PositiveInfinity, seatBottom=float.PositiveInfinity };
                        for (int s = 0; s < skins.Length; s++) {
                            var vertices=WorldVertices(skins[s]);
                            for (int v = 0; v < vertices.Length; v++) {
                                float y=vertices[v].y;
                                if (labels[s][v] == 1) row.leftSole=Mathf.Min(row.leftSole,y);
                                if (labels[s][v] == 2) row.rightSole=Mathf.Min(row.rightSole,y);
                                if (labels[s][v] == 3 && Mathf.Abs(vertices[v].x)<.22f && vertices[v].z < .14f && y<row.seatBottom) { row.seatBottom=y; row.seatPoint=vertices[v]; }
                            }

                        }
                        row.hips=rig.Joint("BodyRoot").position;
                        row.leftThigh=rig.Joint("LeftLeg").position; row.rightThigh=rig.Joint("RightLeg").position;
                        row.leftKnee=rig.Joint("LeftKnee").position; row.rightKnee=rig.Joint("RightKnee").position;
                        row.leftFoot=rig.Joint("LeftFoot").position; row.rightFoot=rig.Joint("RightFoot").position;
                        row.leftPalm=Palm(rig,"Left"); row.rightPalm=Palm(rig,"Right"); report.rows.Add(row);
                        if(pose.StartsWith("Seated")) {
                            Require(row.leftSole>=-.002f && row.rightSole>=-.002f,"SHOE_UNDERGROUND "+pose+" "+row.time);
                            Require(row.leftSole<.02f && row.rightSole<.02f,"SHOE_FLOATING "+pose+" "+row.time);
                            Require(Mathf.Abs(row.seatBottom-height)<.012f,"SEAT_CONTACT "+pose+" "+row.time+" "+row.seatBottom);
                        }
                    }
                }
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/characters-v282/native-pipeline/Worker"));
                File.WriteAllText(Path.Combine(folder,"unity-contact-evidence.json"), JsonUtility.ToJson(report,true));
                Debug.Log("NATIVE_CONTACT_RECORDED rows="+report.rows.Count);
                NativeMotionChecks.Check(actor);
                EditorApplication.Exit(0);
            } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
            finally { if(actor) UnityEngine.Object.DestroyImmediate(actor); }
        }
        static void Require(bool value,string error) {if(!value) throw new InvalidOperationException("NATIVE_CONTACT "+error);}
        static Vector3[] WorldVertices(SkinnedMeshRenderer skin) {
            var points=skin.sharedMesh.vertices; var bind=skin.sharedMesh.bindposes; var bones=skin.bones;
            var matrix=new Matrix4x4[bones.Length];
            for(int i=0;i<bones.Length;i++) matrix[i]=bones[i].localToWorldMatrix*bind[i];
            using(var counts=skin.sharedMesh.GetBonesPerVertex()) using(var weights=skin.sharedMesh.GetAllBoneWeights()) {
                int cursor=0;
                for(int v=0;v<points.Length;v++) {
                    Vector3 p=points[v], q=Vector3.zero;
                    for(int w=0;w<counts[v];w++) {var b=weights[cursor++];q+=matrix[b.boneIndex].MultiplyPoint3x4(p)*b.weight;}
                    points[v]=q;
                }
            }
            return points;
        }
        static Vector3 Palm(NativeCharacterRig rig,string side) {
            var hand=rig.Joint(side+"Hand"); Transform index=null,pinky=null;
            foreach(var t in hand.GetComponentsInChildren<Transform>()) {
                if(t.name.EndsWith("HandIndex1",StringComparison.Ordinal)) index=t;
                if(t.name.EndsWith("HandPinky1",StringComparison.Ordinal)) pinky=t;
            }
            if(!index || !pinky) return Vector3.zero;
            return Vector3.Cross(index.position-hand.position,pinky.position-hand.position).normalized * (side=="Left" ? 1 : -1);
        }
    }
}
