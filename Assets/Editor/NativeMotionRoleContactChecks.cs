using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static partial class NativeMotionContactChecks
    {
        [Serializable] public sealed class RoleContactReport {
            public string role; public float height, seatHeight;
            public int shoeVerticesLeft, shoeVerticesRight, seatVertices;
            public bool contactsPass;
            public List<string> failures = new List<string>();
            public List<Row> rows = new List<Row>();
        }
        // Records source/adapted skin contact for the requested actor. The report
        // is written even when contact fails, so the next repair uses real data.
        public static void RecordRole()
        {
            GameObject actor=null;
            try {
                var args=Environment.GetCommandLineArgs();
                int roleIndex=Array.IndexOf(args,"-native-role");
                string role=roleIndex>=0 && roleIndex+1<args.Length ? args[roleIndex+1] : "Aman";
                float height=ModelLibrary.CharacterHeight(role), seatHeight=role=="Aman" ? .36f : .43f;
                if(height<=0) throw new ArgumentException("Unknown role "+role);
                string expected="Models/"+role+"MeshyMotion/"+role+"MeshyMotion";
                if(ModelLibrary.MeshyResource(role)!=expected)
                    throw new InvalidOperationException("CONTACT_REVIEW_REQUIRES_QUALIFIED_NATIVE_RESOURCE "+role+"; requalify changed source dependencies before testing contacts");
                float proportion=height/ModelLibrary.CharacterHeight("Worker");
                actor=ModelLibrary.Spawn(role,null,Vector3.zero);
                var rig=actor.GetComponent<NativeCharacterRig>();
                if(!rig || !rig.HasImportedMotion)
                    throw new InvalidOperationException("CONTACT_REVIEW_IMPORTED_MOTION_MISSING "+role);
                var performance=CharacterPerformance.For(actor);performance.Automatic=false;
                var report=new RoleContactReport {role=role,height=height,seatHeight=seatHeight};
                var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var labels=new List<int[]>();Vector3 hip=rig.Joint("BodyRoot").position;
                foreach(var skin in skins) {
                    var points=WorldVertices(skin);var label=new int[points.Length];
                    for(int v=0;v<points.Length;v++) {
                        var p=points[v];
                        if(p.y<.13f*proportion) {
                            label[v]=p.x<0?1:2;
                            if(label[v]==1)report.shoeVerticesLeft++;else report.shoeVerticesRight++;
                        }
                        if(p.y>hip.y-.22f*proportion && p.y<hip.y+.02f*proportion &&
                            p.z<hip.z-.025f*proportion && Mathf.Abs(p.x)<.16f*proportion) {
                            label[v]=3;report.seatVertices++;
                        }
                    }
                    labels.Add(label);
                }
                foreach(string pose in new[]{"Idle","Walking","Seated"}) {
                    performance.ResetPose();performance.SetLocomotion(pose=="Walking"?1:0);
                    performance.SetSeated(pose=="Seated",seatHeight);
                    for(int frame=0;frame<450;frame++) {
                        performance.Step(1f/30);if(frame<29 || frame%10!=9)continue;
                        var row=new Row {pose=pose,surfaceHeight=seatHeight,time=(frame+1)/30f,
                            leftSole=float.PositiveInfinity,rightSole=float.PositiveInfinity,seatBottom=float.PositiveInfinity};
                        for(int s=0;s<skins.Length;s++) {
                            var points=WorldVertices(skins[s]);
                            for(int v=0;v<points.Length;v++) {
                                var p=points[v];int label=labels[s][v];
                                if(label==1)row.leftSole=Mathf.Min(row.leftSole,p.y);
                                if(label==2)row.rightSole=Mathf.Min(row.rightSole,p.y);
                                if(label==3 && Mathf.Abs(p.x)<.20f && p.z<.14f && p.y<row.seatBottom) {row.seatBottom=p.y;row.seatPoint=p;}
                            }
                        }
                        row.hips=rig.Joint("BodyRoot").position;row.leftThigh=rig.Joint("LeftLeg").position;row.rightThigh=rig.Joint("RightLeg").position;
                        row.leftKnee=rig.Joint("LeftKnee").position;row.rightKnee=rig.Joint("RightKnee").position;
                        row.leftFoot=rig.Joint("LeftFoot").position;row.rightFoot=rig.Joint("RightFoot").position;
                        row.leftPalm=Palm(rig,"Left");row.rightPalm=Palm(rig,"Right");report.rows.Add(row);
                        if(!float.IsFinite(row.leftSole)||!float.IsFinite(row.rightSole))report.failures.Add(pose+" no shoe probes");
                        else if(pose!="Walking" && (row.leftSole<-.002f||row.rightSole<-.002f||row.leftSole>.02f||row.rightSole>.02f))
                            report.failures.Add(pose+" sole at "+row.time+": "+row.leftSole+", "+row.rightSole);
                        if(pose=="Seated" && (!float.IsFinite(row.seatBottom)||Mathf.Abs(row.seatBottom-seatHeight)>.012f))
                            report.failures.Add("Seat at "+row.time+": "+row.seatBottom);
                    }
                }
                report.contactsPass=report.failures.Count==0;
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/characters-v282/native-pipeline",role));
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder,"unity-role-contact-evidence.json"),JsonUtility.ToJson(report,true));
                Debug.Log("NATIVE_ROLE_CONTACT_RECORDED role="+role+" rows="+report.rows.Count+" pass="+report.contactsPass+" failures="+report.failures.Count);
                EditorApplication.Exit(report.contactsPass?0:2);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
            finally{if(actor)UnityEngine.Object.DestroyImmediate(actor);}
        }
    }
}
