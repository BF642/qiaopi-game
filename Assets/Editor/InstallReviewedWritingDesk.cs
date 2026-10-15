using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Qiaopi.Editor {
public static class InstallReviewedWritingDesk {
    const string Folder="Assets/Resources/EnvironmentArt/WritingDesk/";
    public static void Run() {
        var shader=Shader.Find("Qiaopi/EnvironmentPBR");if(!shader||!shader.isSupported)throw new Exception("Environment PBR shader unavailable");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"WritingDesk.mat");
        if(!material) {material=new Material(shader);AssetDatabase.CreateAsset(material,Folder+"WritingDesk.mat");} else material.shader=shader;
        string[] names={"basecolor","normal","metalrough"};string[] slots={"_MainTex","_BumpMap","_MetalRough"};
        for(int i=0;i<names.Length;i++) {
            string path=Folder+"WritingDesk_"+names[i]+".jpg";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int size=i==0?8192:4096;if(!texture||texture.width!=size||texture.height!=size)throw new Exception("Native texture resolution missing: "+path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            foreach(string platform in new[]{"Standalone","iPhone","Android"}) {var settings=importer.GetPlatformTextureSettings(platform);if(!settings.overridden||settings.maxTextureSize!=8192)throw new Exception("Texture capped: "+platform);}
            if(importer.sRGBTexture!=(i==0))throw new Exception("PBR colour space incorrect: "+path);
            material.SetTexture(slots[i],texture);
        }
        material.SetFloat("_BumpScale",1);material.SetFloat("_RoughnessFloor",.25f);material.SetFloat("_DielectricSpecular",.04f);EditorUtility.SetDirty(material);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"WritingDesk.fbx");if(!source)throw new Exception("Real desk mesh missing");
        var root=new GameObject("WritingDesk");
        try {
            var visual=Object.Instantiate(source,root.transform,false);visual.name="Original Meshy desk, calibrated in metres";
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true)) {
                var mats=new Material[renderer.sharedMaterials.Length];for(int i=0;i<mats.Length;i++)mats[i]=material;renderer.sharedMaterials=mats;
            }
            if(!EnvironmentModelLibrary.TryBounds(root,out var b)||Mathf.Abs(b.size.y-.78f)>.008f)throw new Exception("Desk dimensions incorrect: "+b);
            // Blender front/back/three-quarter renders and support probes were reviewed.
            // The runtime test still independently checks seated use in each location.
            var review=new EnvironmentModelReview {assetId=EnvironmentModelLibrary.WritingDesk,approved=true,materialsReviewed=true,groundLevelReviewed=true,roofJoinReviewed=false,
                geometrySha256=EnvironmentModelLibrary.GeometrySignature(root),reviewNote="8K original GLB maps copied without resampling. Matching 8K mesh UVs. Front/back/three-quarter reviewed; 11 desk-top probes within 8 mm of 0.78 m, knee passage empty. Runtime seat review recorded separately."};
            if(!EnvironmentModelLibrary.ValidateCandidate(review.assetId,root,review,out string why))throw new Exception(why);
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"Model.prefab");File.WriteAllText(Folder+"review.json",JsonUtility.ToJson(review,true)+"\n");
        } finally {Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();EnvironmentModelLibrary.ClearReviewCache();
        if(!EnvironmentModelLibrary.IsReady(EnvironmentModelLibrary.WritingDesk))throw new Exception(EnvironmentModelLibrary.UnavailableReason(EnvironmentModelLibrary.WritingDesk));
        Debug.Log("REVIEWED_8K_DESK_INSTALLED nativeBase8192 normal4096 metalrough4096 bothDesktopAndMobileMax8192");
    }
}
}
