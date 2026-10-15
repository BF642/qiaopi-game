using System;using System.IO;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
namespace Qiaopi.Editor {public static class DesktopHandoffBuild {
[MenuItem("侨批/导出当前电脑版（保留原始资源）")]
public static void Build(){try{BuildGame.CreateScene();AssetDatabase.SaveAssets();
string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds"));Directory.CreateDirectory(folder);
var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Qiaopi.unity"},locationPathName=Path.Combine(folder,"侨批·纸短情长.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Desktop build failed: "+report.summary.result);
Debug.Log("DESKTOP_HANDOFF_BUILD_SUCCESS");if(Application.isBatchMode)EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}}
public static void VerifyCompile(){Debug.Log("HANDOFF_COMPILE_SUCCESS");}
}}
