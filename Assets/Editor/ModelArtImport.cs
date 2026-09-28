using UnityEngine;
using UnityEditor;
namespace Qiaopi.Editor
{
    public class ModelArtImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/Resources/Models/"))return;
            var imp=(ModelImporter)assetImporter;
            imp.importCameras=false;imp.importLights=false;imp.importAnimation=false;
            imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            imp.meshCompression=ModelImporterMeshCompression.Off;
            imp.isReadable=true;imp.addCollider=false;
        }
        void OnPostprocessMaterial(Material material)
        {
            if(!assetPath.StartsWith("Assets/Resources/Models/"))return;
            Color c=material.HasProperty("_Color")?material.color:Color.white;
            material.shader=Shader.Find("Standard");material.color=c;
            material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.10f);
        }
    }
}
