using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class ExistingCastImport
    {
        public static void Install(string role)
        {
            if (role != "Guide" && role != "MasterHe" && role != "Clerk")
                throw new ArgumentException("Unsupported continuation role: " + role);
            string folder = "Assets/Resources/Models/" + role + "MeshyMotion/";
            string modelPath = folder + role + "MeshyMotion.fbx";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Original conversion missing: " + role);
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshPolygons = importer.optimizeMeshVertices = false;
            importer.optimizeBones = importer.optimizeGameObjects = false;
            importer.importCameras = importer.importLights = false;
            importer.isReadable = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = 32;
            importer.minBoneWeight = 0;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "NativeMaterial.mat");
            if (!material)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, folder + "NativeMaterial.mat");
            }
            material.mainTexture = Texture(folder + "BaseColor.jpg", true, false);
            material.SetTexture("_BumpMap", Texture(folder + "Normal.jpg", false, true));
            material.SetTexture("_MetallicGlossMap", Texture(folder + "MetalSmooth.png", false, false));
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_GlossMapScale", 1);
            EditorUtility.SetDirty(material);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
                if (asset is Material imported)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(imported), material);
            importer.SaveAndReimport();
            var stock = AssetDatabase.LoadAssetAtPath<NativeMotionSet>(
                "Assets/Resources/Models/WorkerMeshyMotion/NativeMotionSet.asset");
            if (!stock || !stock.idle || !stock.walking || !stock.seated)
                throw new InvalidOperationException("Existing stock motions missing");
            var set = AssetDatabase.LoadAssetAtPath<NativeMotionSet>(folder + "NativeMotionSet.asset");
            if (!set)
            {
                set = ScriptableObject.CreateInstance<NativeMotionSet>();
                AssetDatabase.CreateAsset(set, folder + "NativeMotionSet.asset");
            }
            set.role = role;
            set.model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            set.idle = stock.idle; set.walking = stock.walking;
            set.running = stock.walking; set.seated = stock.seated;
            set.walkingReferenceMetresPerSecond = stock.walkingReferenceMetresPerSecond;
            set.runningReferenceMetresPerSecond = stock.walkingReferenceMetresPerSecond;
            set.directImport = true; set.qualified = false;
            set.qualificationVersion = NativeMotionSet.CurrentVersion;
            set.fingerAnimationWeight = 1;
            set.fingerAdaptationReview = "Original Meshy skin; existing stock skeleton and idle/walk/seated clips. Local weight transfer, no independent cloud rigging or art acceptance gate.";
            set.reviewedSkin = null;
            var paths = new List<string> { modelPath };
            foreach (var clip in set.Clips())
            {
                string path = AssetDatabase.GetAssetPath(clip);
                if (!paths.Contains(path)) paths.Add(path);
            }
            set.sourceAssets = paths.ToArray();
            set.sourceDependencyHashes = paths.ConvertAll(p => AssetDatabase.GetAssetDependencyHash(p).ToString()).ToArray();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log("EXISTING_CAST_IMPORTED " + role);
        }

        static Texture2D Texture(string path, bool srgb, bool normal)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Native texture missing: " + path);
            importer.maxTextureSize = 8192;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = srgb;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
