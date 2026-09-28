using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class BuildiOS
    {
        public const string BundleId="studio.quanzhou.qiaopi.ios";
        [MenuItem("侨批/导出 iPhone 测试版")]
        public static void Build()
        {
#if UNITY_IOS
            try {
                if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS))throw new Exception("Unity iOS Build Support is required.");
                BuildGame.Configure();
                PlayerSettings.bundleVersion="2.0.4";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,BundleId);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS,ManagedStrippingLevel.Low);
                PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;
                PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneOnly;
                PlayerSettings.iOS.targetOSVersionString="16.0";
                PlayerSettings.iOS.buildNumber="20005";
                PlayerSettings.iOS.appleEnableAutomaticSigning=true;
                string team=Environment.GetEnvironmentVariable("QIAOPI_TEAM_ID");
                if(!string.IsNullOrEmpty(team))PlayerSettings.iOS.appleDeveloperTeamID=team;
                PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
                PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
                PlayerSettings.iOS.requiresFullScreen=true;
                PlayerSettings.iOS.hideHomeButton=true;
                PlayerSettings.iOS.allowHTTPDownload=false;
                PlayerSettings.runInBackground=false;
                QualitySettings.antiAliasing=2;QualitySettings.shadowDistance=42;QualitySettings.shadowCascades=1;
                var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/AppIcon-1.7.png");
                foreach(var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS)){
                    var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS,kind);
                    foreach(var item in icons)item.SetTexture(icon);
                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS,kind,icons);
                }
                BuildGame.CreateScene();
                StoryChecks.Run();InspectionChecks.Run();SaveChecks.Run();DialogueChecks.Run();GalleryChecks.Run();PersonalLetterChecks.Run();LifeIntegrationChecks.Run();
                AssetDatabase.SaveAssets();
                string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/iOS"));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Qiaopi.unity"},locationPathName=output,target=BuildTarget.iOS,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("iOS export failed: "+report.summary.result+" errors="+report.summary.totalErrors);
                var plist=new PlistDocument();string plistPath=Path.Combine(output,"Info.plist");plist.ReadFromFile(plistPath);
                plist.root.SetString("CFBundleDisplayName","侨批·纸短情长");
                plist.root.SetBoolean("UIFileSharingEnabled",true);
                plist.root.SetBoolean("LSSupportsOpeningDocumentsInPlace",true);
                plist.WriteToFile(plistPath);
                Debug.Log("QIAOPI_IOS_EXPORT_SUCCESS="+output+" bytes="+report.summary.totalSize);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            } catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
#else
            Debug.LogError("请安装 iOS Build Support，并先在 Unity 中切换到 iOS 构建目标。");
            if(Application.isBatchMode)EditorApplication.Exit(1);
#endif
        }
    }
}
