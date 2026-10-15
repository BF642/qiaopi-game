using System;using System.IO;using UnityEngine;using UnityEditor;
namespace Qiaopi.Editor { public static class WorkwearReferenceExport {
 public static void Export() {
 var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Models/WifeMeshyMotion/WifeMeshyMotion.fbx");var skin=p.GetComponentInChildren<SkinnedMeshRenderer>();
 var m=skin.sharedMesh;var v=m.vertices;var n=m.normals;
 string path=Path.GetFullPath("../../work/replacement-v289/qa/unity-low-reference.bin");
 using(var w=new BinaryWriter(File.Create(path))){w.Write(v.Length);for(int i=0;i<v.Length;i++){w.Write(v[i].x);w.Write(v[i].y);w.Write(v[i].z);w.Write(n[i].x);w.Write(n[i].y);w.Write(n[i].z);}w.Write(skin.bones.Length);foreach(var b in skin.bones)w.Write(b.name);}
 Debug.Log("WORKWEAR_UNITY_REFERENCE_EXPORTED vertices="+v.Length+" bounds="+m.bounds+" transform="+skin.transform.localToWorldMatrix);
 }
}}
