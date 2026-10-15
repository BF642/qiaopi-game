using System;using UnityEditor;using UnityEngine;
namespace Qiaopi.Editor {
public static class SemarangWarehouseImport {
 const string Folder="Assets/Resources/EnvironmentArt/SemarangWarehouse/";
 public static void InstallAndBuild(){try{
  AssetDatabase.Refresh();
  var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"Original.fbx");
  importer.importAnimation=false;importer.meshCompression=ModelImporterMeshCompression.Off;
  importer.isReadable=false;importer.optimizeMeshPolygons=false;importer.optimizeMeshVertices=false;
  importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
  var colour=Texture("BaseColor.jpg",true,false);var normal=Texture("Normal.jpg",false,true);var metal=Texture("MetalSmooth.png",false,false);
  var material=new Material(Shader.Find("Standard")){name="Semarang sugar warehouse · native textures"};
  material.mainTexture=colour;material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");
  material.SetTexture("_MetallicGlossMap",metal);material.EnableKeyword("_METALLICGLOSSMAP");material.SetFloat("_GlossMapScale",1);
  var previous=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Building.mat");
  if(previous){EditorUtility.CopySerialized(material,previous);UnityEngine.Object.DestroyImmediate(material);material=previous;}else AssetDatabase.CreateAsset(material,Folder+"Building.mat");
  var root=new GameObject("Semarang sugar warehouse");
  try{
   var identity=root.AddComponent<RegionalBuilding>();identity.destination="semarang";identity.function="warehouse";identity.assetId="SemarangWarehouse";
   var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Original.fbx"),root.transform,false);
   visual.transform.localRotation=Quaternion.Euler(0,-90,0);
   var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("FBX has no renderers");
   var bounds=renderers[0].bounds;foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);var mats=new Material[renderer.sharedMaterials.Length];for(int i=0;i<mats.Length;i++)mats[i]=material;renderer.sharedMaterials=mats;}
   // Unit conversion only: preserve the original proportions, vertices and UVs.
   float scale=12f/bounds.size.x;visual.transform.localScale*=scale;
   visual.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.min.z*scale);
   float height=bounds.size.y*scale, depth=bounds.size.z*scale;
   // Separate side and rear colliders leave the ground-floor front entrance open.
   AddWall(root,new Vector3(-5.88f,height*.5f,depth*.5f),new Vector3(.18f,height,depth));
   AddWall(root,new Vector3(5.88f,height*.5f,depth*.5f),new Vector3(.18f,height,depth));
   AddWall(root,new Vector3(0,height*.5f,depth-.12f),new Vector3(12f,height,.18f));
   PrefabUtility.SaveAsPrefabAsset(root,Folder+"Model.prefab");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  AssetDatabase.SaveAssets();Debug.Log("REGIONAL_BUILDING_DIRECT_IMPORTED=semarang/warehouse; original source; no art inspection");SemarangJourneyChecks.Run();OriginalGeometryRelease.Build();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void AddWall(GameObject root,Vector3 centre,Vector3 size){var wall=root.AddComponent<BoxCollider>();wall.center=centre;wall.size=size;}
 static Texture2D Texture(string name,bool srgb,bool normal){var imp=(TextureImporter)AssetImporter.GetAtPath(Folder+name);imp.maxTextureSize=8192;imp.sRGBTexture=srgb;imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=true;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+name);}
}}
