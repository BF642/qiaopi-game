using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Qiaopi.Editor
{
    public static class BuildMeshyCharacterStudy
    {
        const string StudyScene = "Assets/Scenes/MeshyCharacterStudy.unity";

        [MenuItem("侨批/人物样稿/导出人物 3D 动作预览")]
        public static void Build()
        {
            var target = NamedBuildTarget.Standalone;
            string oldProduct = PlayerSettings.productName;
            string oldIdentifier = PlayerSettings.GetApplicationIdentifier(target);
            int oldWidth = PlayerSettings.defaultScreenWidth, oldHeight = PlayerSettings.defaultScreenHeight;
            var oldFullscreen = PlayerSettings.fullScreenMode;
            bool oldResizable = PlayerSettings.resizableWindow, oldBackground = PlayerSettings.runInBackground;
            var oldBackend = PlayerSettings.GetScriptingBackend(target);
#if UNITY_EDITOR_OSX
            var oldArchitecture = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
#endif
            Scene previousScene = SceneManager.GetActiveScene(), study = default(Scene);
            Exception failure = null;
            try
            {
                bool tasksOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-native-task-only")>=0;
                if(tasksOnly) AssetDatabase.Refresh(); else ImportAvailableModels();
                CharacterMaterialChecks.Run();
                if(tasksOnly) NativeMotionChecks.CheckWorkerTasks();
                else CheckAvailableModels();
                string work = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/characters-v282/preview"));
                Directory.CreateDirectory(work);
                PlayerSettings.productName = "侨批人物预览";
                PlayerSettings.SetApplicationIdentifier(target, "studio.quanzhou.qiaopi.meshystudy");
                PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 1000;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
#if UNITY_EDITOR_OSX
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
#endif
                // A fresh batch editor starts with an untitled scene; Unity cannot
                // add another scene to it. Interactive editors retain their open scene.
                // BuildPlayer's explicit array leaves the normal build list intact.
                study = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
                SceneManager.SetActiveScene(study);
                var viewer = new GameObject("Independent native character inspection").AddComponent<MeshyCharacterViewer>();
                viewer.captureDirectory = Path.Combine(work, "unity-native-captures");
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
                if (!EditorSceneManager.SaveScene(study, StudyScene)) throw new IOException("无法保存独立人物预览场景。");
                string output = Path.Combine(work, "侨批人物预览.app");
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { StudyScene }, locationPathName = output,
                    target = BuildTarget.StandaloneOSX, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("动作预览构建失败：" + report.summary.result + ", errors=" + report.summary.totalErrors);
                Debug.Log("QIAOPI_MESHY_STUDY_BUILD_SUCCESS=" + output + " bytes=" + report.summary.totalSize);
            }
            catch (Exception exception) { failure = exception; Debug.LogException(exception); }
            finally
            {
                PlayerSettings.productName = oldProduct;
                PlayerSettings.SetApplicationIdentifier(target, oldIdentifier);
                PlayerSettings.defaultScreenWidth = oldWidth; PlayerSettings.defaultScreenHeight = oldHeight;
                PlayerSettings.fullScreenMode = oldFullscreen;
                PlayerSettings.resizableWindow = oldResizable; PlayerSettings.runInBackground = oldBackground;
                PlayerSettings.SetScriptingBackend(target, oldBackend);
#if UNITY_EDITOR_OSX
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = oldArchitecture;
#endif
                if (study.IsValid() && study.isLoaded) EditorSceneManager.CloseScene(study, true);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            }
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else if (failure != null) throw new InvalidOperationException("独立动作预览未构建成功，详见 Console。", failure);
        }

        static void ImportAvailableModels()
        {
            foreach (string character in new[] { "Wensheng", "Wife", "Mother", "Aman", "Xusheng", "MasterHe", "Clerk", "Uncle", "Guide", "Foreman", "Shopkeeper", "Worker" }) {
                string resource = ModelLibrary.MeshyResource(character);
                if (string.IsNullOrEmpty(resource)) continue;
                string modelAsset = "Assets/Resources/" + resource + ".fbx";
                string source = Path.Combine(Application.dataPath, "Resources/" + resource + ".fbx");
                if (!File.Exists(source)) {
                    if (character == "Wensheng") throw new FileNotFoundException("请先导入默认文生原生骨架模型。", source);
                    Debug.Log("QIAOPI_MESHY_OPTIONAL_MODEL_ABSENT=" + character);
                    continue;
                }
                // Import the three source maps first, so a newly copied FBX can
                // bind real textures on its first material postprocess pass.
                string art = Path.GetDirectoryName(resource).Replace('\\', '/') + "/" + character;
                foreach (string suffix in new[] { "_basecolor.png", "_metalrough.png", "_normal.png" }) {
                    string textureAsset = "Assets/Resources/" + art + suffix;
                    if (!File.Exists(Path.Combine(Application.dataPath, "Resources/" + art + suffix)))
                        throw new FileNotFoundException(character + " 新模型缺少原始材质贴图：" + textureAsset);
                    AssetDatabase.ImportAsset(textureAsset, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
                AssetDatabase.ImportAsset(modelAsset, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
        }

        static void CheckAvailableModels()
        {
            foreach (string character in new[] { "Wensheng", "Wife", "Mother", "Aman", "Xusheng", "MasterHe", "Clerk", "Uncle", "Guide", "Foreman", "Shopkeeper", "Worker" }) {
                string resource = ModelLibrary.MeshyResource(character);
                if (string.IsNullOrEmpty(resource)) continue;
                var prefab = Resources.Load<GameObject>(resource);
                if (!prefab) {
                    if (character == "Wensheng") throw new FileNotFoundException("请先导入文生原生骨架模型：" + resource);
                    continue;
                }
                if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException(character + " 原生人物资产不能附带游戏脚本。");
                if (prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
                    throw new InvalidOperationException(character + " 原生人物资产还没有蒙皮骨架。");
                var actor = ModelLibrary.Spawn(character, null, Vector3.zero);
                if (!actor) throw new InvalidOperationException("无法加载新人物：" + character);
                try {
                    var nativeRig = actor.GetComponent<NativeCharacterRig>();
                    if (nativeRig && nativeRig.HasImportedMotion) NativeMotionChecks.Check(actor);
                    else NativeCharacterChecks.Check(actor);
                }
                finally { UnityEngine.Object.DestroyImmediate(actor); }
                Debug.Log("QIAOPI_MESHY_STUDY_MODEL_CHECKED=" + character);
            }
        }
    }
}
