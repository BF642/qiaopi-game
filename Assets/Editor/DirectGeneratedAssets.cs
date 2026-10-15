using System;using System.IO;using System.Collections.Generic;using UnityEditor;using UnityEngine;
namespace Qiaopi.Editor { public static class DirectGeneratedAssets {
 public static void InstallAndBuild(){try {
  const string folder="Assets/Resources/Models/MotherMeshyMotion/";
  AssetDatabase.Refresh();
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"MotherMeshyMotion.fbx");
  if(!model)throw new Exception("Mother FBX did not import");
  var clips=new Dictionary<string,AnimationClip>();
  string staging="Assets/Editor/MeshyMotionSources/MotherDirect";
  foreach(string path in Directory.GetFiles(staging,"*.fbx")) {
   var imp=AssetImporter.GetAtPath(path) as ModelImporter;
   imp.animationType=ModelImporterAnimationType.Generic;imp.importAnimation=true;imp.optimizeGameObjects=false;imp.optimizeBones=false;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.SaveAndReimport();
   foreach(var item in AssetDatabase.LoadAllAssetsAtPath(path))if(item is AnimationClip clip&&!clip.name.StartsWith("__preview__")){
    string take=clip.name.Substring(clip.name.LastIndexOf('|')+1);
    if(take!="Idle_4"&&take!="Casual_Walk"&&take!="Running"&&take!="Chair_Sit_Idle_F")continue;
    var copy=UnityEngine.Object.Instantiate(clip);var settings=AnimationUtility.GetAnimationClipSettings(copy);settings.loopTime=true;settings.loopBlend=false;AnimationUtility.SetAnimationClipSettings(copy,settings);
    string dest=folder+"Mother_"+take+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);
    if(existing){EditorUtility.CopySerialized(copy,existing);UnityEngine.Object.DestroyImmediate(copy);copy=existing;}else AssetDatabase.CreateAsset(copy,dest);clips[take]=copy;
   }
  }
  var set=AssetDatabase.LoadAssetAtPath<NativeMotionSet>(folder+"NativeMotionSet.asset");if(!set){set=ScriptableObject.CreateInstance<NativeMotionSet>();AssetDatabase.CreateAsset(set,folder+"NativeMotionSet.asset");}
  set.role="Mother";set.model=model;set.directImport=true;set.qualified=false;set.qualificationVersion=NativeMotionSet.CurrentVersion;
  set.idle=clips["Idle_4"];set.walking=clips["Casual_Walk"];set.running=clips["Running"];set.seated=clips["Chair_Sit_Idle_F"];
  set.fingerAnimationWeight=1;set.fingerAdaptationReview="Direct import at user request; no visual or motion QA performed.";
  set.walkingReferenceMetresPerSecond=.6291538f;set.runningReferenceMetresPerSecond=4.807385f;
  var paths=new List<string>{folder+"MotherMeshyMotion.fbx"};foreach(var clip in set.Clips())paths.Add(AssetDatabase.GetAssetPath(clip));
  set.sourceAssets=paths.ToArray();set.sourceDependencyHashes=new string[paths.Count];for(int i=0;i<paths.Count;i++)set.sourceDependencyHashes[i]=AssetDatabase.GetAssetDependencyHash(paths[i]).ToString();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
  const string houseFolder="Assets/Resources/EnvironmentArt/QuanzhouCourtyardHome/";
  var house=PrefabUtility.LoadPrefabContents(houseFolder+"Model.prefab");
  try {
   var visual=house.transform.Find("Complete house visual");
   while(visual.childCount>0)UnityEngine.Object.DestroyImmediate(visual.GetChild(0).gameObject);
   UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(houseFolder+"QuanzhouCourtyardHome.fbx"),visual,false);
   var material=AssetDatabase.LoadAssetAtPath<Material>(houseFolder+"House.mat");
   foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {var materials=new Material[renderer.sharedMaterials.Length];for(int i=0;i<materials.Length;i++)materials[i]=material;renderer.sharedMaterials=materials;}
   PrefabUtility.SaveAsPrefabAsset(house,houseFolder+"Model.prefab");
  }finally{PrefabUtility.UnloadPrefabContents(house);}
  Debug.Log("GENERATED_ASSETS_DIRECT_IMPORTED Mother original geometry + Idle_4/Casual_Walk/Running/Chair_Sit_Idle_F; courtyard original detail; quality inspection omitted by user request");
  OriginalGeometryRelease.Build();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
