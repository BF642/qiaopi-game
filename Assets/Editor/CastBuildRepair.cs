using System;using System.IO;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
namespace Qiaopi.Editor { public static class CastBuildRepair {
public static void Build(){try{
 AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
 // Rebuild serialized motion-set records after cloud files were hydrated.
 foreach(var guid in AssetDatabase.FindAssets("t:NativeMotionSet",new[]{"Assets/Resources/Models"})) {
  var path=AssetDatabase.GUIDToAssetPath(guid);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
 }
 foreach(var file in new[]{"Worker_Idle_4.anim","Worker_Walking.anim","Worker_Chair_Sit_Idle_M.anim"})AssetDatabase.ImportAsset("Assets/Resources/Models/WorkerMeshyMotion/"+file,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
 BuildGame.Configure();BuildGame.CreateScene();AssetDatabase.SaveAssets();
 string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../侨批·纸短情长.app"));
 var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Qiaopi.unity"},locationPathName=output,target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
 if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Desktop serialization repair failed: "+report.summary.result);
 Debug.Log("DESKTOP_SERIALIZATION_REPAIR_SUCCESS="+output+" bytes="+report.summary.totalSize);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
