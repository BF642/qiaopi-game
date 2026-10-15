using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Qiaopi.Editor
{
    public static class NativeMotionExtraction
    {
        [Serializable] sealed class Extraction
        {
            public string role, model;
            public List<ClipRecord> clips = new List<ClipRecord>();
        }
        [Serializable] sealed class ClipRecord
        {
            public string source, sourceSha256, take, output;
            public int skinBonesCovered, curveCount;
            public float maximumBindMatrixDifference;
            public List<string> ignoredLeafPaths = new List<string>();
        }

        // Copies only Unity's imported animation curves. Source geometry,
        // weights and files remain intact; helper spheres are not instantiated.
        public static void ExtractSeparate()
        {
            GameObject main = null;
            string staging = null;
            try {
                string role = Argument("-native-role"), sourceDirectory = Argument("-native-source-directory");
                if (ModelLibrary.CharacterHeight(role) <= 0 || !Directory.Exists(sourceDirectory))
                    throw new ArgumentException("REAL_ROLE_AND_SOURCE_DIRECTORY_REQUIRED");
                string folder = "Assets/Resources/Models/" + role + "MeshyMotion";
                string model = folder + "/" + role + "MeshyMotion.fbx";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model);
                if (!prefab) throw new InvalidOperationException("PRESERVED_MODEL_MISSING " + model);
                main = Object.Instantiate(prefab);
                var mainAnimator = main.GetComponentInChildren<Animator>(true); mainAnimator.enabled = false;
                var mainBind = RestMatrices(main, mainAnimator.transform);
                staging = "Assets/Editor/MeshyMotionSources/" + role;
                if (Directory.Exists(staging)) throw new InvalidOperationException("STAGING_ALREADY_EXISTS " + staging);
                Directory.CreateDirectory(staging); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                string evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/characters-v282/native-pipeline", role, "unity-separate-extraction.json"));
                var report = File.Exists(evidence) ? JsonUtility.FromJson<Extraction>(File.ReadAllText(evidence)) : new Extraction { role = role, model = model };
                var inputs = Directory.GetFiles(sourceDirectory, "*_Animation_*_withSkin.fbx");
                Array.Sort(inputs, StringComparer.Ordinal);
                if (inputs.Length == 0) throw new InvalidOperationException("SEPARATE_ANIMATION_FILES_MISSING");
                for (int index = 0; index < inputs.Length; index++) {
                    string input = inputs[index], temporary = staging + "/Source_" + index + ".fbx";
                    File.Copy(input, temporary);
                    AssetDatabase.ImportAsset(temporary, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (ModelImporter)AssetImporter.GetAtPath(temporary);
                    importer.importAnimation = true; importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.animationCompression = ModelImporterAnimationCompression.Off;
                    importer.optimizeGameObjects = false; importer.optimizeBones = false;
                    importer.skinWeights = ModelImporterSkinWeights.Custom; importer.maxBonesPerVertex = 32; importer.minBoneWeight = 0;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.importLights = importer.importCameras = false;
                    importer.clipAnimations = importer.defaultClipAnimations;
                    importer.SaveAndReimport();
                    var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(temporary);
                    var source = Object.Instantiate(sourcePrefab);
                    try {
                        var sourceAnimator = source.GetComponentInChildren<Animator>(true); sourceAnimator.enabled = false;
                        var sourceBind = RestMatrices(source, sourceAnimator.transform);
                        float maximumDifference = 0;
                        foreach (var pair in mainBind) {
                            Matrix4x4 matrix;
                            if (!sourceBind.TryGetValue(pair.Key, out matrix))
                                throw new InvalidOperationException("SOURCE_SKIN_BONE_MISSING " + pair.Key);
                            for (int i = 0; i < 16; i++) maximumDifference = Mathf.Max(maximumDifference, Mathf.Abs(pair.Value[i] - matrix[i]));
                        }
                        if (maximumDifference > .001f) throw new InvalidOperationException("SOURCE_BIND_POSE_MISMATCH " + maximumDifference);
                        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(temporary)) {
                            var clip = asset as AnimationClip;
                            if (!clip || clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                            var record = new ClipRecord { source = Path.GetFullPath(input), sourceSha256 = Hash(input), take = clip.name,
                                maximumBindMatrixDifference = maximumDifference, skinBonesCovered = mainBind.Count };
                            var bindings = AnimationUtility.GetCurveBindings(clip); record.curveCount = bindings.Length;
                            var affected = new HashSet<string>(); var ignored = new HashSet<string>();
                            foreach (var binding in bindings) {
                                affected.Add(binding.path);
                                if (binding.path.Length > 0 && !mainAnimator.transform.Find(binding.path)) {
                                    if (!binding.path.EndsWith("_end", StringComparison.Ordinal))
                                        throw new InvalidOperationException("UNMATCHED_ANIMATION_NODE " + binding.path);
                                    ignored.Add(binding.path);
                                }
                            }
                            foreach (var pair in mainBind)
                                if (!affected.Contains(pair.Key)) throw new InvalidOperationException("SKIN_BONE_HAS_NO_SOURCE_CHANNELS " + pair.Key);
                            record.ignoredLeafPaths.AddRange(ignored); record.ignoredLeafPaths.Sort(StringComparer.Ordinal);
                            string take = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                            foreach (char invalid in Path.GetInvalidFileNameChars()) take = take.Replace(invalid, '_');
                            record.output = folder + "/" + role + "_" + take + ".anim";
                            var target = AssetDatabase.LoadAssetAtPath<AnimationClip>(record.output);
                            if (!target) { target = Object.Instantiate(clip); AssetDatabase.CreateAsset(target, record.output); }
                            else EditorUtility.CopySerialized(clip, target);
                            target.name = clip.name;
                            var settings = AnimationUtility.GetAnimationClipSettings(target); settings.loopTime = true; settings.loopBlend = false;
                            AnimationUtility.SetAnimationClipSettings(target, settings);
                            EditorUtility.SetDirty(target);
                            var copied = AnimationUtility.GetCurveBindings(target);
                            if (copied.Length != bindings.Length) throw new InvalidOperationException("EXTRACTED_CURVE_COUNT_CHANGED");
                            foreach (var binding in bindings) {
                                var originalKeys = AnimationUtility.GetEditorCurve(clip, binding).keys;
                                var copiedKeys = AnimationUtility.GetEditorCurve(target, binding).keys;
                                if (originalKeys.Length != copiedKeys.Length) throw new InvalidOperationException("EXTRACTED_KEYS_CHANGED");
                                for (int k = 0; k < originalKeys.Length; k++)
                                    if (!originalKeys[k].Equals(copiedKeys[k])) throw new InvalidOperationException("EXTRACTED_KEY_VALUE_CHANGED");
                            }
                            report.clips.RemoveAll(previous => previous.output == record.output); report.clips.Add(record);
                            Debug.Log("NATIVE_SEPARATE_EXTRACTED take=" + clip.name + " skinBones=" + mainBind.Count +
                                " ignoredLeaves=" + ignored.Count + " bindDifference=" + maximumDifference + " curves=" + bindings.Length);
                        }
                    } finally { Object.DestroyImmediate(source); }
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(evidence, JsonUtility.ToJson(report, true));
                Object.DestroyImmediate(main); main = null;
                AssetDatabase.DeleteAsset(staging); staging = null;
                NativeMotionSourceChecks.QualifyRole();
            } catch (Exception e) {
                if (main) Object.DestroyImmediate(main);
                // Keep failed staging data for inspection; never remove another
                // role or any original file when an assertion did not pass.
                Debug.LogException(e); EditorApplication.Exit(1);
            }
        }

        static Dictionary<string, Matrix4x4> RestMatrices(GameObject model, Transform animationRoot)
        {
            var result = new Dictionary<string, Matrix4x4>();
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                var binds = skin.sharedMesh.bindposes;
                for (int i = 0; i < skin.bones.Length; i++) {
                    string path = AnimationUtility.CalculateTransformPath(skin.bones[i], animationRoot);
                    result[path] = animationRoot.worldToLocalMatrix * skin.transform.localToWorldMatrix * binds[i].inverse;
                }
            }
            return result;
        }
        static string Hash(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            throw new ArgumentException("ARGUMENT_REQUIRED " + name);
        }
    }
}
