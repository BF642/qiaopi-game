using System;using UnityEditor;using UnityEngine;
namespace Qiaopi.Editor {
public static class RegionalBuildingImport {
 const string Folder="Assets/Resources/EnvironmentArt/SingaporeGeneralStore/";
 public static void InstallAndBuild(){try{
  AssetDatabase.Refresh();
  var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"Original.fbx");
  importer.importAnimation=false;importer.meshCompression=ModelImporterMeshCompression.Off;
  importer.isReadable=false;importer.optimizeMeshPolygons=false;importer.optimizeMeshVertices=false;
  importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
  var colour=Texture("BaseColor.png",true,false);var normal=Texture("Normal.png",false,true);var metal=Texture("MetalSmooth.png",false,false);
  var material=new Material(Shader.Find("Standard")){name="Singapore general store · native textures"};
  material.mainTexture=colour;material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");
  material.SetTexture("_MetallicGlossMap",metal);material.EnableKeyword("_METALLICGLOSSMAP");material.SetFloat("_GlossMapScale",1);
  var previous=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Building.mat");
  if(previous){EditorUtility.CopySerialized(material,previous);UnityEngine.Object.DestroyImmediate(material);material=previous;}else AssetDatabase.CreateAsset(material,Folder+"Building.mat");
  var root=new GameObject("Singapore general store");
  try{
   var identity=root.AddComponent<RegionalBuilding>();identity.destination="singapore";identity.function="general_store";identity.assetId="SingaporeGeneralStore";
   var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Original.fbx"),root.transform,false);
   visual.transform.localRotation=Quaternion.Euler(0,-90,0);
   var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("FBX has no renderers");
   var bounds=renderers[0].bounds;foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);var mats=new Material[renderer.sharedMaterials.Length];for(int i=0;i<mats.Length;i++)mats[i]=material;renderer.sharedMaterials=mats;}
   // Unit conversion only: preserve the original proportions, vertices and UVs.
   float scale=7.2f/bounds.size.x;visual.transform.localScale*=scale;
   visual.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.min.z*scale);
   var collision=root.AddComponent<BoxCollider>();collision.center=new Vector3(0,bounds.size.y*scale*.5f,bounds.size.z*scale*.5f+1.1f);
   collision.size=new Vector3(7.2f,bounds.size.y*scale,Mathf.Max(.2f,bounds.size.z*scale-2.2f));
   PrefabUtility.SaveAsPrefabAsset(root,Folder+"Model.prefab");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  AssetDatabase.SaveAssets();Debug.Log("REGIONAL_BUILDING_DIRECT_IMPORTED=singapore/general_store; original source; no art inspection");OriginalGeometryRelease.Build();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static Texture2D Texture(string name,bool srgb,bool normal){var imp=(TextureImporter)AssetImporter.GetAtPath(Folder+name);imp.maxTextureSize=8192;imp.sRGBTexture=srgb;imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=true;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+name);}
}}
