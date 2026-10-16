using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Qiaopi.Editor
{
    public static class BuildCharacterStudy
    {
        const string StudyScene = "Assets/Scenes/CharacterStudy.unity";
        const string StudyModel = "Assets/Resources/Models/WenshengStudy.fbx";

        [MenuItem("侨批/人物样稿/导出独立 Mac 样稿")]
        public static void Build()
        {
            var standalone = NamedBuildTarget.Standalone;
            string oldProduct = PlayerSettings.productName;
            string oldIdentifier = PlayerSettings.GetApplicationIdentifier(standalone);
            int oldWidth = PlayerSettings.defaultScreenWidth, oldHeight = PlayerSettings.defaultScreenHeight;
            FullScreenMode oldFullscreen = PlayerSettings.fullScreenMode;
            bool oldResizable = PlayerSettings.resizableWindow, oldBackground = PlayerSettings.runInBackground;
            var oldBackend = PlayerSettings.GetScriptingBackend(standalone);
#if UNITY_EDITOR_OSX
            var oldArchitecture = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
#endif
            Scene previousScene = SceneManager.GetActiveScene();
            Scene createdScene = default(Scene);
            Exception failure = null;
            try
            {
                // No call to BuildGame.Configure/CreateScene, and no story checks:
                // the normal game scene, build-scene list and version stay intact.
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(StudyModel);
                if (!model) throw new FileNotFoundException("先导入人物样稿 FBX，再构建独立预览：" + StudyModel);
                if (model.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("人物样稿 FBX 不能附带游戏脚本。");
                string work = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/characters-v205"));
                Directory.CreateDirectory(work);
                PlayerSettings.productName = "侨批人物样稿";
                PlayerSettings.SetApplicationIdentifier(standalone, "studio.quanzhou.qiaopi.characterstudy");
                PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 1000;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
                PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);
#if UNITY_EDITOR_OSX
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
#endif
                createdScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
                SceneManager.SetActiveScene(createdScene);
                var viewer = new GameObject("Standalone character study").AddComponent<CharacterStudyViewer>();
                viewer.captureDirectory = Path.Combine(work, "unity-study-captures");
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
                if (!EditorSceneManager.SaveScene(createdScene, StudyScene))
                    throw new IOException("无法保存独立的人物样稿场景。");
                string output = Path.Combine(work, "侨批人物样稿.app");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { StudyScene },
                    locationPathName = output,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("人物样稿构建失败：" + report.summary.result + ", errors=" + report.summary.totalErrors);
                Debug.Log("QIAOPI_CHARACTER_STUDY_BUILD_SUCCESS=" + output + " bytes=" + report.summary.totalSize);
            }
            catch (Exception exception) { failure = exception; Debug.LogException(exception); }
            finally
            {
                PlayerSettings.productName = oldProduct;
                PlayerSettings.SetApplicationIdentifier(standalone, oldIdentifier);
                PlayerSettings.defaultScreenWidth = oldWidth; PlayerSettings.defaultScreenHeight = oldHeight;
                PlayerSettings.fullScreenMode = oldFullscreen;
                PlayerSettings.resizableWindow = oldResizable; PlayerSettings.runInBackground = oldBackground;
                PlayerSettings.SetScriptingBackend(standalone, oldBackend);
#if UNITY_EDITOR_OSX
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = oldArchitecture;
#endif
                if (createdScene.IsValid() && createdScene.isLoaded) EditorSceneManager.CloseScene(createdScene, true);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            }
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else if (failure != null) throw new InvalidOperationException("独立人物样稿尚未构建成功，详见 Console。", failure);
        }
    }
}
