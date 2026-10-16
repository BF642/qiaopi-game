using System;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class RangoonLodgingImport
    {
        const string Folder = "Assets/Resources/EnvironmentArt/RangoonWorkerLodging/";

        [MenuItem("侨批/接入仰光客工院（原始资源）")]
        public static void Install()
        {
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(Folder + "Original.fbx") as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing Rangoon original FBX.");
            importer.importAnimation = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshPolygons = false;
            importer.optimizeMeshVertices = false;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();

            var shader = Shader.Find("Standard");
            if (!shader) throw new InvalidOperationException("Standard shader unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Building.mat");
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, Folder + "Building.mat");
            }
            material.name = "Rangoon worker lodging · native textures";
            material.mainTexture = Texture("BaseColor.jpg", true, false);
            material.SetTexture("_BumpMap", Texture("Normal.jpg", false, true));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", Texture("MetalSmooth.png", false, false));
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_GlossMapScale", 1f);

            var root = new GameObject("Rangoon worker lodging");
            try
            {
                var identity = root.AddComponent<RegionalBuilding>();
                identity.destination = "rangoon";
                identity.function = "worker_lodging";
                identity.assetId = "RangoonWorkerLodging";
                var visual = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Original.fbx"), root.transform, false);
                // Preserve the FBX importer's Blender-to-Unity up-axis conversion.
                visual.transform.localRotation = Quaternion.Euler(0, 180, 0) * visual.transform.localRotation;
                var renderers = visual.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new InvalidOperationException("Original FBX has no renderers.");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    var slots = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < slots.Length; i++) slots[i] = material;
                    renderer.sharedMaterials = slots;
                }
                // Uniform unit conversion only; original geometry and UVs remain intact.
                float scale = 9f / bounds.size.x;
                visual.transform.localScale *= scale;
                visual.transform.localPosition = new Vector3(-bounds.center.x * scale,
                    -bounds.min.y * scale, -bounds.min.z * scale);
                float height = bounds.size.y * scale, depth = bounds.size.z * scale;
                // Independent gameplay collision leaves the verandah and central entrance open.
                Wall(root, new Vector3(-4.42f, height * .5f, depth * .5f), new Vector3(.16f, height, depth));
                Wall(root, new Vector3(4.42f, height * .5f, depth * .5f), new Vector3(.16f, height, depth));
                Wall(root, new Vector3(0, height * .5f, depth - .12f), new Vector3(9, height, .18f));
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "Model.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("RANGOON_WORKER_LODGING_IMPORTED: original geometry and native textures; no art inspection");
        }

        static void Wall(GameObject root, Vector3 center, Vector3 size)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        static Texture2D Texture(string name, bool srgb, bool normal)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + name);
            importer.maxTextureSize = 8192;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = srgb;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + name);
        }
    }
}
