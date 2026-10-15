using System;using UnityEditor;using UnityEngine;
namespace Qiaopi.Editor { public static class RegionalBuildRetry {
 public static void Build(){try{
 AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
 AssetDatabase.ImportAsset("Assets/Resources/Models/WifeMeshyMotion/Wife_Chair_Sit_Idle_M.anim",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
 AssetDatabase.ImportAsset("Assets/Scenes/Qiaopi.unity",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
 AssetDatabase.SaveAssets();OriginalGeometryRelease.Build();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
