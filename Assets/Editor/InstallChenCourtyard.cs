using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Qiaopi.Editor {
public static class InstallChenCourtyard {
    const string Folder="Assets/Resources/EnvironmentArt/QuanzhouCourtyardHome/";
    public static void CheckRoutes() {
        var scene=WorldFactory.Build("quanzhou");
        var meal=new GameObject("Meal table collision used by household");meal.transform.SetParent(scene.transform,false);meal.transform.position=new Vector3(-23,.725f,-.3f);meal.AddComponent<BoxCollider>().size=new Vector3(1.65f,.07f,1.02f);
        var study=new GameObject("Study table collision used by household");study.transform.SetParent(scene.transform,false);study.transform.position=new Vector3(-26.6f,.505f,-.9f);study.AddComponent<BoxCollider>().size=new Vector3(1.08f,.07f,.65f);
        Physics.SyncTransforms();WalkPath.Invalidate();
        try {
            var route=WalkPath.Find(new Vector3(-26.04f,.08f,-9.99f),new Vector3(-26.6f,0,-2.1f),"quanzhou");
            for(int z=-5;z<=0;z++)for(int x=-27;x<=-22;x++) {
                Physics.Raycast(new Vector3(x,4,z),Vector3.down,out var hit,8,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
                var overlaps=Physics.OverlapCapsule(new Vector3(x,.55f,z),new Vector3(x,1.65f,z),.35f,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
                Debug.Log("HOUSE_NAV_CELL "+x+","+z+" ground="+hit.point.y+" groundObject="+(hit.collider?hit.collider.name:"none")+" overlaps="+overlaps.Length);
            }
            var ray=new Ray(new Vector3(-17.88f,1.2f,-8),Vector3.right);
            foreach(var mf in scene.GetComponentsInChildren<MeshFilter>()) {
                var renderer=mf.GetComponent<Renderer>();if(!renderer||!renderer.enabled||!renderer.bounds.IntersectRay(ray,out float near))continue;
                if(!mf.name.Contains("mesh") && !mf.transform.IsChildOf(scene.transform.Find("陈家整屋 · 红砖厝")))continue;
                var inv=mf.transform.worldToLocalMatrix;var origin=inv.MultiplyPoint3x4(ray.origin);var direction=inv.MultiplyVector(ray.direction);
                var vs=mf.sharedMesh.vertices;var ts=mf.sharedMesh.triangles;float closest=999,orientation=0;
                for(int i=0;i<ts.Length;i+=3){var a=vs[ts[i]];var e1=vs[ts[i+1]]-a;var e2=vs[ts[i+2]]-a;var p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-12f)continue;var t=origin-a;float u=Vector3.Dot(t,p)/det;if(u<0||u>1)continue;var q=Vector3.Cross(t,e1);float v=Vector3.Dot(direction,q)/det;if(v<0||u+v>1)continue;float distance=Vector3.Dot(e2,q)/det;if(distance>.03f&&distance<closest){closest=distance;orientation=det;}}
                Debug.Log("HOUSE_REAR_RENDER_PROBE name="+mf.name+" near="+near+" closest="+closest+" det="+orientation+" scale="+mf.transform.lossyScale);
            }
            Vector3[] from={new Vector3(-28.7f,0,-8.4f),new Vector3(-21.8f,0,-7.2f),new Vector3(-26.2f,0,.1f)};
            Vector3[] to={new Vector3(-24.35f,0,-.25f),new Vector3(-21.75f,0,-.25f),new Vector3(-23,0,.8f)};
            for(int i=0;i<from.Length;i++){var family=WalkPath.Find(from[i],to[i],"quanzhou");if(family.Count==0||Vector3.Distance(family[family.Count-1],to[i])>.2f)throw new Exception("Family seat path is not reachable: "+i);Debug.Log("HOUSE_FAMILY_ROUTE "+i+" points="+family.Count);}
            Debug.Log("HOUSE_NAV_ROUTE_POINTS "+route.Count);
            if(route.Count==0)throw new Exception("Courtyard lesson route unavailable");
        } finally {Object.DestroyImmediate(scene);WalkPath.Invalidate();}
    }
    public static void Build() { Run(); BuildGame.Build(); }
    public static void Run() {
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"House.mat");
        if(!material){material=new Material(Shader.Find("Qiaopi/EnvironmentPBR"));AssetDatabase.CreateAsset(material,Folder+"House.mat");}
        string[] names={"basecolor","normal","metalrough"}, slots={"_MainTex","_BumpMap","_MetalRough"};
        for(int i=0;i<3;i++) {
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"QuanzhouCourtyardHome_"+names[i]+".jpg");
            if(!tex||tex.width!=(i==0?8192:4096))throw new Exception("House native map missing: "+names[i]);
            material.SetTexture(slots[i],tex);
        }
        material.SetFloat("_BumpScale",1);material.SetFloat("_RoughnessFloor",.25f);material.SetFloat("_DielectricSpecular",.04f);EditorUtility.SetDirty(material);
        var root=new GameObject("Chen complete courtyard");
        try {
            var visual=new GameObject("Complete house visual");visual.transform.SetParent(root.transform,false);
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"QuanzhouCourtyardHome.fbx"),visual.transform,false);
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
                var mats=new Material[renderer.sharedMaterials.Length];for(int i=0;i<mats.Length;i++)mats[i]=material;renderer.sharedMaterials=mats;
            }
            // Choose the orientation using actual mesh rays, never a guessed FBX convention.
            var filters=visual.GetComponentsInChildren<MeshFilter>();
            foreach(var mf in filters)mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
            bool matched=false;
            foreach(float yaw in new[]{0f,180f})foreach(float mirror in new[]{1f,-1f}) {
                if(matched)continue;
                visual.transform.localRotation=Quaternion.Euler(0,yaw,0);visual.transform.localScale=new Vector3(mirror,1,1);Physics.SyncTransforms();
                foreach(var r in visual.GetComponentsInChildren<Renderer>()) Debug.Log("HOUSE_BOUNDS "+yaw+" "+mirror+" "+r.bounds);
                bool front=Physics.Raycast(new Vector3(0,1.6f,-12),Vector3.forward,out var hit,25)&&hit.point.z>8;
                bool eastOpen=!Physics.Raycast(new Vector3(4,1.4f,-1.55f),Vector3.right,2.3f);
                bool westOpen=!Physics.Raycast(new Vector3(-4,1.4f,-4),Vector3.left,2.3f);
                Debug.Log("HOUSE_PROBE yaw="+yaw+" mirror="+mirror+" front="+front+" hit="+hit.point+" east="+eastOpen+" west="+westOpen);
                if(front&&eastOpen&&westOpen){matched=true;Debug.Log("HOUSE_ORIENTATION yaw="+yaw+" mirror="+mirror);break;}
            }
            if(!matched)throw new Exception("House orientation or side doors failed mesh ray probes");
            foreach(var col in visual.GetComponentsInChildren<MeshCollider>())Object.DestroyImmediate(col);
            // Collision uses the actual wall triangles, excluding floors and roof
            // slopes so the navigation ground ray cannot mistake a roof for ground.
            var vertices=new List<Vector3>();var triangles=new List<int>();
            foreach(var mf in filters) {
                int offset=vertices.Count;var mesh=mf.sharedMesh;
                foreach(var v in mesh.vertices)vertices.Add(root.transform.InverseTransformPoint(mf.transform.TransformPoint(v)));
                var sourceTriangles=mesh.triangles;
                for(int i=0;i<sourceTriangles.Length;i+=3) {
                    int a=offset+sourceTriangles[i],b=offset+sourceTriangles[i+1],c=offset+sourceTriangles[i+2];
                    var normal=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized;
                    if(Mathf.Abs(normal.y)>.65f || Mathf.Min(vertices[a].y,vertices[b].y,vertices[c].y)>2.35f || Mathf.Max(vertices[a].y,vertices[b].y,vertices[c].y)<.2f)continue;
                    triangles.Add(a);triangles.Add(b);triangles.Add(c);
                }
            }
            var collision=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"Walls.asset");
            if(!collision){collision=new Mesh();AssetDatabase.CreateAsset(collision,Folder+"Walls.asset");}else collision.Clear();
            collision.name="Actual courtyard wall triangles";collision.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            collision.SetVertices(vertices);collision.SetTriangles(triangles,0);collision.RecalculateBounds();EditorUtility.SetDirty(collision);
            root.AddComponent<MeshCollider>().sharedMesh=collision;
            Debug.Log("HOUSE_ACTUAL_WALL_COLLISION triangles="+triangles.Count/3);
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"Model.prefab");
        } finally {Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();Debug.Log("CHEN_COURTYARD_INSTALLED_PENDING_RUNTIME_REVIEW");
    }
}
}
