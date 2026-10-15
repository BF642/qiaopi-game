using System;using System.Collections.Generic;using UnityEditor;using UnityEngine;
namespace Qiaopi.Editor {
public static class RemainingCastImport {
 public static void InstallAndBuild(){try{
  AssetDatabase.Refresh();
  var stock=AssetDatabase.LoadAssetAtPath<NativeMotionSet>("Assets/Resources/Models/WorkerMeshyMotion/NativeMotionSet.asset");
  if(!stock||!stock.idle||!stock.walking||!stock.seated)throw new Exception("Existing stock Meshy clips unavailable");
  foreach(var role in new[]{"Xusheng","Uncle","Shopkeeper","Foreman"}){
   string folder="Assets/Resources/Models/"+role+"MeshyMotion/",path=folder+role+"MeshyMotion.fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   importer.meshCompression=ModelImporterMeshCompression.Off;importer.optimizeMeshPolygons=false;importer.optimizeMeshVertices=false;
   importer.animationType=ModelImporterAnimationType.Generic;importer.optimizeGameObjects=false;importer.optimizeBones=false;importer.SaveAndReimport();
   var set=AssetDatabase.LoadAssetAtPath<NativeMotionSet>(folder+"NativeMotionSet.asset");
   if(!set){set=ScriptableObject.CreateInstance<NativeMotionSet>();AssetDatabase.CreateAsset(set,folder+"NativeMotionSet.asset");}
   set.role=role;set.model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   set.idle=stock.idle;set.walking=stock.walking;set.seated=stock.seated;
   // Everyday NPCs walk, including their faster locomotion channel; no athletic sprint.
   set.running=stock.walking;
   set.reviewedSkin=null;set.reviewedSkinSourceName="";set.directImport=true;set.qualified=false;set.qualificationVersion=NativeMotionSet.CurrentVersion;
   set.fingerAnimationWeight=1;set.fingerAdaptationReview="Direct original model with existing stock Meshy clips. No per-character visual qualification or borrowed finger/contact correction.";
   set.reviewedContactAndWristAdaptation=false;set.reviewedSeatAndGroundAdaptation=false;
   // The skeleton and clip were transferred unchanged at Worker size. Runtime model
   // scaling uses each role's authored height, so scale the stock clip's unit speed.
   float ratio=ModelLibrary.CharacterHeight(role)/ModelLibrary.CharacterHeight("Worker");
   set.walkingReferenceMetresPerSecond=stock.walkingReferenceMetresPerSecond*ratio;set.runningReferenceMetresPerSecond=set.walkingReferenceMetresPerSecond;
   var paths=new List<string>{path};foreach(var c in set.Clips())if(c){var p=AssetDatabase.GetAssetPath(c);if(!paths.Contains(p))paths.Add(p);}
   set.sourceAssets=paths.ToArray();set.sourceDependencyHashes=new string[paths.Count];for(int i=0;i<paths.Count;i++)set.sourceDependencyHashes[i]=AssetDatabase.GetAssetDependencyHash(paths[i]).ToString();
   EditorUtility.SetDirty(set);Debug.Log("ORIGINAL_CAST_DIRECT_IMPORTED="+role+"; existing stock Meshy clips; original geometry retained; no visual qualification");
  }
  AssetDatabase.SaveAssets();OriginalGeometryRelease.Build();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
