using System;using System.IO;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
namespace Qiaopi.Editor { public static class OriginalGeometryRelease {
public static void Build() {try {BuildGame.Configure();BuildGame.CreateScene();AssetDatabase.SaveAssets();
string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../侨批·纸短情长.app"));
var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/Qiaopi.unity"},locationPathName=output,target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Desktop build failed: "+report.summary.result);
Debug.Log("DESKTOP_DIRECT_ASSET_BUILD_SUCCESS="+output+" bytes="+report.summary.totalSize);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
