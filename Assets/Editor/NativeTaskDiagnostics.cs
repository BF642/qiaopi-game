using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class NativeTaskDiagnostics
    {
        [Serializable] sealed class VertexRow {public int vertex;public float error,belowExpected;public Vector3 actual,expected;public string weights;}
        [Serializable] sealed class PoseRow {public string name; public float highestHand;public Vector3 left,right;public List<VertexRow> hand=new List<VertexRow>();}
        [Serializable] sealed class Report {public List<PoseRow> poses=new List<PoseRow>();}
        public static void RunWorker()
        {
            GameObject actor=null;
            try {
                actor=ModelLibrary.Spawn("Worker",null,Vector3.zero);var rig=actor.GetComponent<NativeCharacterRig>();
                var p=CharacterPerformance.For(actor);p.Automatic=false;
                var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
                Transform hand=rig.Joint("RightHand");var rest=WorldVertices(skin);var local=new Vector3[rest.Length];
                for(int i=0;i<rest.Length;i++) local[i]=hand.InverseTransformPoint(rest[i]);
                var labels=new bool[rest.Length];var weightsText=new string[rest.Length];
                using(var counts=mesh.GetBonesPerVertex()) using(var weights=mesh.GetAllBoneWeights()) {
                    int cursor=0;
                    for(int v=0;v<rest.Length;v++) {float total=0;string text="";
                        for(int w=0;w<counts[v];w++){var b=weights[cursor++];var bone=skin.bones[b.boneIndex];
                            if(bone==hand || bone.IsChildOf(hand))total+=b.weight;text+=bone.name+":"+b.weight.ToString("F3")+";";}
                        labels[v]=total>.45f;weightsText[v]=text;
                    }
                }
                foreach(string aux in new[]{"Bone_049","Bone_050","Bone_023","Bone_033","Bone_034","Bone_018"}) {
                    int count=0;Bounds range=new Bounds();
                    for(int v=0;v<rest.Length;v++) if(weightsText[v].Contains(aux+":")) {if(count++==0)range=new Bounds(rest[v],Vector3.zero);else range.Encapsulate(rest[v]);}
                    Debug.Log("NATIVE_AUX_SKIN "+aux+" count="+count+" min="+range.min.ToString("F4")+" max="+range.max.ToString("F4"));
                }
                var segments=new[]{rig.Joint("LeftArm"),rig.Joint("LeftForearm"),rig.Joint("LeftHand"),rig.Joint("RightArm"),rig.Joint("RightForearm"),rig.Joint("RightHand")};
                var lengths=new[]{Vector3.Distance(segments[0].position,segments[1].position),Vector3.Distance(segments[1].position,segments[2].position),Vector3.Distance(segments[3].position,segments[4].position),Vector3.Distance(segments[4].position,segments[5].position)};
                bool assertTasks=Array.IndexOf(Environment.GetCommandLineArgs(),"-native-assert-task-bounds")>=0;
                var report=new Report();
                foreach(string name in new[]{"Offer","OfferFingersRest","OfferWristRest","Sign","Lift","SetDown","Point","Scoop","Weigh","Carrying"}) {
                    p.ResetPose();float weight=name=="OfferFingersRest" ? 0:.15f;
                    typeof(NativeMotion).GetField("fingerWeight",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig.Motion,weight);
                    if(name=="Carrying") p.SetCarrying(true);else p.Play(name.StartsWith("Offer") ? CharacterGesture.Offer:(CharacterGesture)Enum.Parse(typeof(CharacterGesture),name),2);
                    var row=new PoseRow{name=name};
                    for(int f=0;f<60;f++){
                        p.Step(1f/60);row.highestHand=Mathf.Max(row.highestHand,p.LeftHandPoint.y,p.RightHandPoint.y);
                        if(assertTasks) {
                            if(row.highestHand>1.35f)throw new InvalidOperationException("TASK_HAND_TOO_HIGH "+name);
                            for(int segment=0;segment<4;segment++) {
                                int first=segment<2 ? segment:segment+1;
                                if(Mathf.Abs(Vector3.Distance(segments[first].position,segments[first+1].position)-lengths[segment])>.002f)
                                    throw new InvalidOperationException("TASK_BONE_LENGTH_CHANGED "+name);
                            }
                        }
                    }
                    if(name=="OfferWristRest") {
                        var rests=(Quaternion[])typeof(CharacterPerformance).GetField("nativeHandRest",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p);hand.localRotation=rests[1];
                    }
                    row.left=p.LeftHandPoint;row.right=p.RightHandPoint;
                    var world=WorldVertices(skin);
                    for(int v=0;v<world.Length;v++) if(labels[v]) {
                        Vector3 expected=hand.TransformPoint(local[v]);
                        row.hand.Add(new VertexRow{vertex=v,error=Vector3.Distance(world[v],expected),belowExpected=expected.y-world[v].y,actual=world[v],expected=expected,weights=weightsText[v]});
                    }
                    row.hand.Sort((a,b)=>b.error.CompareTo(a.error));if(row.hand.Count>12)row.hand.RemoveRange(12,row.hand.Count-12);
                    report.poses.Add(row);Debug.Log("NATIVE_TASK_DIAGNOSTIC "+name+" highest="+row.highestHand+" handError="+row.hand[0].error+" weights="+row.hand[0].weights);
                }
                File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/characters-v282/native-pipeline/Worker/task-source-diagnostics.json")),JsonUtility.ToJson(report,true));
                Debug.Log("NATIVE_ALL_WORK_TASK_BOUNDS_PASSED asserted="+assertTasks);
                EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{if(actor)UnityEngine.Object.DestroyImmediate(actor);}
        }
        static Vector3[] WorldVertices(SkinnedMeshRenderer skin) {
            var points=skin.sharedMesh.vertices;var bind=skin.sharedMesh.bindposes;var bones=skin.bones;var matrix=new Matrix4x4[bones.Length];
            for(int i=0;i<bones.Length;i++)matrix[i]=bones[i].localToWorldMatrix*bind[i];
            using(var counts=skin.sharedMesh.GetBonesPerVertex())using(var weights=skin.sharedMesh.GetAllBoneWeights()){
                int cursor=0;for(int v=0;v<points.Length;v++){Vector3 p=points[v],q=Vector3.zero;
                    for(int w=0;w<counts[v];w++){var b=weights[cursor++];q+=matrix[b.boneIndex].MultiplyPoint3x4(p)*b.weight;}points[v]=q;
                }
            }return points;
        }
    }
}
