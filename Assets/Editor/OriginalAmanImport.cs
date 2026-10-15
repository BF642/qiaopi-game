using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Qiaopi.Editor {
public static class OriginalAmanImport {
 public static void InstallAndBuild() {
  try {
   const string folder="Assets/Resources/Models/AmanMeshyMotion/";
   AssetDatabase.Refresh();
   var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"AmanMeshyMotion.fbx");
   importer.meshCompression=ModelImporterMeshCompression.Off;
   importer.optimizeMeshPolygons=false; importer.optimizeMeshVertices=false;
   importer.SaveAndReimport();
   var set=AssetDatabase.LoadAssetAtPath<NativeMotionSet>(folder+"NativeMotionSet.asset");
   if(!set)throw new Exception("Existing Aman motion set missing");
   set.model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"AmanMeshyMotion.fbx");
   // The old corrected skin has reduced topology. Never replace the dense source with it.
   set.reviewedSkin=null;set.reviewedSkinSourceName="";
   set.directImport=true;set.qualified=false;set.qualificationVersion=NativeMotionSet.CurrentVersion;
   var paths=new List<string>{folder+"AmanMeshyMotion.fbx"};
   foreach(var clip in set.Clips())if(clip)paths.Add(AssetDatabase.GetAssetPath(clip));
   set.sourceAssets=paths.ToArray();set.sourceDependencyHashes=new string[paths.Count];
   for(int i=0;i<paths.Count;i++)set.sourceDependencyHashes[i]=AssetDatabase.GetAssetDependencyHash(paths[i]).ToString();
   EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
   Debug.Log("AMAN_ORIGINAL_DIRECT_IMPORTED: original geometry; existing Aman motions retained; no art or motion inspection");
   OriginalGeometryRelease.Build();
  } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}}
