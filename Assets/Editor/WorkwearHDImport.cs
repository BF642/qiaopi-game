using System;using System.IO;using UnityEngine;using UnityEditor;using Unity.Collections;
namespace Qiaopi.Editor { public static class WorkwearHDImport {
 public static void Import() {
 const string folder="Assets/Resources/Models/WifeMeshyMotion/";
 var skin=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"WifeMeshyMotion.fbx").GetComponentInChildren<SkinnedMeshRenderer>();var source=skin.sharedMesh;
 if(Array.IndexOf(Environment.GetCommandLineArgs(),"-workwear-game-mesh")>=0)throw new InvalidOperationException("Desktop original-geometry policy prohibits reduced workwear meshes.");
 bool game=false;
 string meshName=game?"WifeFacePreservedReviewed":"WifeOriginalHDReviewed";
 string data=Path.GetFullPath("../../work/replacement-v289/"+(game?"wife-game-skin.bin":"wife-hd-skin.bin"));
 using(var reader=new BinaryReader(File.OpenRead(data))){
 if(new string(reader.ReadChars(4))!="QPH1")throw new Exception("Invalid HD transfer data");
 int count=reader.ReadInt32(),indices=reader.ReadInt32();var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];var counts=new NativeArray<byte>(count,Allocator.Temp);var weights=new System.Collections.Generic.List<BoneWeight1>();
 for(int i=0;i<count;i++){
 vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());normals[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());counts[i]=reader.ReadByte();float total=0;
 for(int j=0;j<counts[i];j++){int bone=reader.ReadInt32();float weight=reader.ReadSingle();if(bone<0||bone>=skin.bones.Length||weight<0)throw new Exception("Invalid HD weight");weights.Add(new BoneWeight1 {boneIndex=bone,weight=weight});total+=weight;}if(Mathf.Abs(total-1)>.0001f)throw new Exception("HD weights not normalized");
 }
 var triangles=new int[indices];for(int i=0;i<indices;i++){triangles[i]=reader.ReadInt32();if(triangles[i]<0||triangles[i]>=count)throw new Exception("Invalid HD triangle");}
 var mesh=new Mesh{name=meshName,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=triangles;mesh.bindposes=source.bindposes;
 using(var all=new NativeArray<BoneWeight1>(weights.ToArray(),Allocator.Temp))mesh.SetBoneWeights(counts,all);counts.Dispose();mesh.RecalculateBounds();mesh.RecalculateTangents();
 string path=folder+meshName+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
 var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/MeshyCharacter.shader");var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"ReviewedMaterial.mat");if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,folder+"ReviewedMaterial.mat");}material.shader=shader;
 foreach(string suffix in new[]{"basecolor","normal","metalrough"})AssetDatabase.ImportAsset(folder+"WifeHD_"+suffix+".jpg",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
 material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"WifeHD_basecolor.jpg");material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"WifeHD_normal.jpg"));material.SetTexture("_MetalRough",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"WifeHD_metalrough.jpg"));material.color=Color.white;material.SetFloat("_BumpScale",.45f);material.SetFloat("_RoughnessFloor",.62f);material.SetFloat("_DielectricSpecular",.018f);material.SetFloat("_CullMode",2);EditorUtility.SetDirty(material);
 var set=AssetDatabase.LoadAssetAtPath<NativeMotionSet>(folder+"NativeMotionSet.asset");set.reviewedSkin=mesh;set.reviewedSkinSourceName=source.name;EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
 Debug.Log("WORKWEAR_HD_IMPORTED vertices="+count+" triangles="+indices/3+" bones="+skin.bones.Length+" originalBindposesPreserved=true");
 }
 NativeMotionSourceChecks.QualifyRole();
 }
}}
