using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Qiaopi.Editor
{
    public static class EnvironmentArtChecks
    {
        static int assertions;
        static readonly string[] ids = { EnvironmentModelLibrary.WritingDesk, EnvironmentModelLibrary.HomeEntry, EnvironmentModelLibrary.OfficeEntry };
        public static void Run()
        {
            assertions=0;EnvironmentModelLibrary.ClearReviewCache();
            ValidateReviewGate();ValidateFallbackRoom();
            foreach(string id in ids) {
                var model=Resources.Load<GameObject>(EnvironmentModelLibrary.ResourcePath(id));
                if(!model) { Require(!EnvironmentModelLibrary.IsReady(id),"Missing model must retain fallback: "+id);continue; }
                if(!EnvironmentModelLibrary.IsReady(id)) Debug.Log("ENVIRONMENT_MODEL_PENDING "+id+" "+EnvironmentModelLibrary.UnavailableReason(id));
                else Debug.Log("ENVIRONMENT_MODEL_APPROVED "+id);
            }
            Debug.Log("ENVIRONMENT_ART_CHECKS_PASSED assertions="+assertions+"; synthetic geometry + fallback verified; actual model art review remains separate");
        }

        static void ValidateReviewGate()
        {
            foreach(string id in ids) {
                var root=Fixture(id);
                try {
                    var review=Review(id,root);
                    Require(EnvironmentModelLibrary.ValidateCandidate(id,root,review,out string reason),id+" valid fixture: "+reason);
                    review.approved=false;Require(!EnvironmentModelLibrary.ValidateCandidate(id,root,review,out _),"Unreviewed fixture rejected");review.approved=true;
                    review.geometrySha256="stale";Require(!EnvironmentModelLibrary.ValidateCandidate(id,root,review,out _),"Changed geometry rejected");review.geometrySha256=EnvironmentModelLibrary.GeometrySignature(root);
                    var collider=root.AddComponent<BoxCollider>();Require(!EnvironmentModelLibrary.ValidateCandidate(id,root,review,out _),"Imported collider rejected");Object.DestroyImmediate(collider);
                    if(id!=EnvironmentModelLibrary.WritingDesk) {
                        review.roofJoinReviewed=false;Require(!EnvironmentModelLibrary.ValidateCandidate(id,root,review,out _),"Unreviewed roof join rejected");review.roofJoinReviewed=true;
                        var obstruction=Part(root,"Opaque fake doorway",new Vector3(0,1.1f,0),new Vector3(.10f,2.2f,.03f));
                        review.geometrySha256=EnvironmentModelLibrary.GeometrySignature(root);Require(!EnvironmentModelLibrary.ValidateCandidate(id,root,review,out reason)&&reason.Contains("doorway"),"Visible obstruction rejected even without collider");Object.DestroyImmediate(obstruction);
                        var plinth=Part(root,"Raised threshold",new Vector3(0,.10f,0),new Vector3(2.2f,.20f,.6f));
                        review.geometrySha256=EnvironmentModelLibrary.GeometrySignature(root);Require(!EnvironmentModelLibrary.ValidateCandidate(id,root,review,out reason)&&reason.Contains("doorway"),"Raised base rejected");Object.DestroyImmediate(plinth);
                    } else {
                        root.transform.GetChild(0).localPosition+=Vector3.up*.08f;review.geometrySha256=EnvironmentModelLibrary.GeometrySignature(root);
                        Require(!EnvironmentModelLibrary.ValidateCandidate(id,root,review,out _),"Desk moving paper height rejected");
                    }
                } finally { DestroyFixture(root); }
            }
        }
        static void ValidateFallbackRoom()
        {
            if(EnvironmentModelLibrary.IsReady(EnvironmentModelLibrary.WritingDesk)||EnvironmentModelLibrary.IsReady(EnvironmentModelLibrary.HomeEntry)||EnvironmentModelLibrary.IsReady(EnvironmentModelLibrary.OfficeEntry)) {
                Debug.Log("ENVIRONMENT_FALLBACK_FIXTURE_SKIPPED approved real assets present; run gameplay WritingDeskQA for actual assets");return;
            }
            var scenery=typeof(WorldGame).Assembly.GetType("Qiaopi.WritingPlaceScenery");var build=scenery.GetMethod("Build",BindingFlags.Public|BindingFlags.Static);
            foreach(string place in new[]{"quanzhou","harbor","port","market","quarters","postoffice"}) {
                var root=new GameObject("Environment fallback check "+place);
                try {
                    bool existingDesk=place=="postoffice",existingRoof=place=="harbor"||existingDesk;
                    build.Invoke(null,new object[]{root.transform,"检查用写批处",place,existingRoof,existingDesk});
                    Require(root.transform.Find("Door jamb collider"),"Fallback jamb present "+place);
                    int native=0;foreach(Transform t in root.transform)if(t.name.StartsWith("Reviewed environment"))native++;
                    Require(native==0,"Missing assets cannot replace fallback "+place);
                    if(!existingDesk) {
                        var table=root.transform.Find("Desk solid top collider");Require(table,"Original desk collider retained "+place);
                        var box=table.GetComponent<BoxCollider>();Require(box.size==new Vector3(1.70f,.067f,.80f)&&table.localPosition==new Vector3(0,.747f,.35f),"Desk physical anchor unchanged "+place);
                    }
                    foreach(var box in root.GetComponentsInChildren<BoxCollider>()) {
                        if(box.name=="Door jamb collider")Require(Mathf.Abs(Mathf.Abs(box.transform.localPosition.x)-1.14f)<1e-6f&&box.size.x==.11f,"Doorway physical width unchanged "+place);
                    }
                } finally { Object.DestroyImmediate(root); }
            }
        }
        static EnvironmentModelReview Review(string id,GameObject model)=>new EnvironmentModelReview{assetId=id,approved=true,materialsReviewed=true,groundLevelReviewed=true,roofJoinReviewed=true,geometrySha256=EnvironmentModelLibrary.GeometrySignature(model),reviewNote="Memory-only test fixture; never written into Resources"};
        static GameObject Fixture(string id)
        {
            var root=new GameObject(id+" validation fixture");Vector3 size=EnvironmentModelLibrary.ExpectedSize(id);
            if(id==EnvironmentModelLibrary.WritingDesk)Part(root,"Desk dimensions fixture",new Vector3(0,size.y*.5f,0),size);
            else {
                float side=(size.x-2.22f)*.5f;
                foreach(float sign in new[]{-1f,1f})Part(root,"Jamb",new Vector3(sign*(1.11f+side*.5f),size.y*.5f,0),new Vector3(side,size.y,size.z));
                Part(root,"Lintel",new Vector3(0,(size.y+2.35f)*.5f,0),new Vector3(2.22f,size.y-2.35f,size.z));
                Part(root,"Flush sill",new Vector3(0,.01f,0),new Vector3(size.x,.02f,size.z));
            }
            return root;
        }
        static GameObject Part(GameObject root,string name,Vector3 position,Vector3 size)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(root.transform,false);part.transform.localPosition=position;part.transform.localScale=size;Object.DestroyImmediate(part.GetComponent<Collider>());return part;
        }
        static void DestroyFixture(GameObject root) { Object.DestroyImmediate(root); }
        static void Require(bool value,string message) { assertions++;if(!value)throw new InvalidOperationException("ENVIRONMENT_ART_CHECK "+message); }

        // Creates review requests outside Resources. It never approves a candidate.
        public static void PrepareReviewRequests()
        {
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/environments-v283/qa"));Directory.CreateDirectory(directory);
            foreach(string id in ids) {
                var model=Resources.Load<GameObject>(EnvironmentModelLibrary.ResourcePath(id));if(!model)continue;
                var review=new EnvironmentModelReview{assetId=id,geometrySha256=EnvironmentModelLibrary.GeometrySignature(model),reviewNote="Pending rendered front/back/seat/doorway inspection, flush ground and roof join review"};
                File.WriteAllText(Path.Combine(directory,id+"-review-request.json"),JsonUtility.ToJson(review,true)+"\n");
            }
        }
    }

    sealed class EnvironmentArtImport : AssetPostprocessor
    {
        bool Applies => assetPath.StartsWith("Assets/Resources/EnvironmentArt/",StringComparison.Ordinal);
        void OnPreprocessModel()
        {
            if(!Applies)return;var importer=(ModelImporter)assetImporter;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.addCollider=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
        }
        void OnPreprocessTexture()
        {
            if(!Applies)return;var importer=(TextureImporter)assetImporter;importer.maxTextureSize=8192;importer.mipmapEnabled=true;importer.anisoLevel=8;
            bool normal=System.IO.Path.GetFileNameWithoutExtension(assetPath).EndsWith("_normal",StringComparison.OrdinalIgnoreCase);
            bool data=System.IO.Path.GetFileNameWithoutExtension(assetPath).EndsWith("_metalrough",StringComparison.OrdinalIgnoreCase);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal&&!data;importer.alphaIsTransparency=false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {name="Standalone",overridden=true,maxTextureSize=8192,format=normal?TextureImporterFormat.BC5:TextureImporterFormat.BC7,compressionQuality=100});
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            foreach(string platform in new[]{"iPhone","Android"}) {
                var settings=importer.GetPlatformTextureSettings(platform);settings.name=platform;settings.overridden=true;settings.maxTextureSize=8192;settings.format=TextureImporterFormat.ASTC_4x4;settings.compressionQuality=100;importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
