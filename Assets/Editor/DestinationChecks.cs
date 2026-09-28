using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    /// <summary>Small decorator fixtures; does not rebuild twelve full towns or save editor scenes.</summary>
    public static class DestinationChecks
    {
        static readonly string[] Destinations = { "singapore", "penang", "rangoon" };
        static readonly string[] Regions = { "port", "market", "quarters", "postoffice" };

        [MenuItem("侨批/检查海外目的地")]
        public static void Run()
        {
            CheckDescriptions();
            var portShapes = new HashSet<string>();
            int cases = 0, triangles = 0;
            foreach (string destination in Destinations)
                foreach (string region in Regions)
                {
                    string shape;
                    triangles += CheckWorld(destination,region,out shape);
                    if(region=="port")Require(portShapes.Add(shape),"port geometry must differ, not only its label/color: "+destination);
                    cases++;
                }
            int materialCount = DestinationMaterialCount();
            // Rebuilding places must reuse the bounded destination palette, while all
            // per-place geometry is released when its parent world is destroyed.
            foreach (string destination in Destinations)
                foreach (string region in Regions) { string unused; CheckWorld(destination,region,out unused); }
            Require(DestinationMaterialCount()==materialCount,"repeat travel allocated additional destination materials");
            CheckHomeUnchanged();
            Debug.Log("DESTINATION_QA_ALL_PASSED: "+cases+" destination/region combinations; distinct port geometry; navigation colliders unchanged; deep-copy descriptions; finite outward meshes; idempotent dressing; mesh cleanup and cached materials. First-pass triangles="+triangles);
        }

        static void CheckDescriptions()
        {
            foreach(string destination in Destinations)foreach(string id in WorldRegions.All)
            {
                var original=WorldRegions.Get(id);string title=original.title,description=original.description;
                string note=original.notes[0].body,zone=original.zones[0].name;
                var described=DestinationWorld.Describe(original,destination);
                bool overseas=Array.IndexOf(Regions,id)>=0;
                Require(described!=original&&described.notes!=original.notes&&described.zones!=original.zones,"description should not alias source");
                Require(described.bounds==original.bounds&&described.spawn==original.spawn&&described.rest==original.rest&&described.sidePickup==original.sidePickup&&described.sideDrop==original.sideDrop,"description moved navigation anchors: "+id);
                for(int i=0;i<original.notes.Length;i++)Require(described.notes[i].id==original.notes[i].id&&described.notes[i].position==original.notes[i].position,"description changed stable note id or position");
                if(overseas)Require(described.title.StartsWith(DestinationWorld.Name(destination)+" · "),"wrong destination title: "+described.title);
                else Require(described.title==title&&described.description==description,"home/sea title modified");
                Require(original.title==title&&original.description==description&&original.notes[0].body==note&&original.zones[0].name==zone,"description mutated source");
                described.notes[0].body="changed copy";described.zones[0].name="changed copy";
                Require(original.notes[0].body==note&&original.zones[0].name==zone,"nested label aliases source");
                Require(WorldRegions.Get(id).title==title,"WorldRegions polluted by destination");
            }
            Require(DestinationWorld.Localize("从新加坡寄回泉州", "penang")=="从槟榔屿寄回泉州","Penang localization");
            Require(DestinationWorld.Localize("从新加坡寄回泉州", "rangoon")=="从仰光寄回泉州","Rangoon localization");
            Require(DestinationWorld.Localize(null,"penang")==null&&DestinationWorld.Localize("", "rangoon")=="","empty localization");
            Require(DestinationWorld.Name(null)=="新加坡"&&DestinationWorld.Name("unknown")=="新加坡","legacy destination fallback");
            Require(DestinationWorld.Describe(null,"penang")==null,"null region guard");
        }

        static int CheckWorld(string destination,string region,out string shape)
        {
            GameObject root=null;Material original=null;var generated=new List<Mesh>();shape="";
            int triangleCount=0;
            try
            {
                root=new GameObject("Destination QA fixture "+destination+"/"+region){hideFlags=HideFlags.HideAndDontSave};
                // One existing ground collider proves the decorator preserves physics.
                var floor=root.AddComponent<BoxCollider>();floor.center=new Vector3(0,-.15f,3);floor.size=new Vector3(72,.3f,70);
                var renderer=root.AddComponent<MeshRenderer>();
                var template=Resources.Load<Material>("WorldMaterial");
                Require(template!=null,"missing world material template");
                original=new Material(template){name="test old limewash",color=new Color(.62f,.58f,.47f),hideFlags=HideFlags.HideAndDontSave};
                renderer.sharedMaterial=original;Color color=original.color;
                Vector3 floorCenter=floor.center,floorSize=floor.size;
                DestinationWorld.Dress(root,region,destination);
                var decoration=root.transform.Find("Destination scenery");Require(decoration!=null,"missing scenery: "+destination+"/"+region);
                var colliders=root.GetComponentsInChildren<Collider>(true);
                Require(colliders.Length==1&&colliders[0]==floor&&floor.enabled&&floor.center==floorCenter&&floor.size==floorSize,"destination changed physics: "+destination+"/"+region);
                Require(original.color==color,"recolor changed a shared base material");
                if(destination!="singapore")Require(renderer.sharedMaterial!=original,"destination tint did not use private palette");
                var filters=decoration.GetComponentsInChildren<MeshFilter>();
                Require(filters.Length>=3&&filters.Length<=12,"unexpected destination draw batches: "+filters.Length);
                foreach(var filter in filters)
                {
                    Mesh mesh=filter.sharedMesh;Require(mesh!=null&&!EditorUtility.IsPersistent(mesh),"missing/transient mesh");generated.Add(mesh);
                    Require(filter.transform.localPosition==Vector3.zero&&filter.transform.localScale==Vector3.one,"batched mesh double-transformed");
                    Require(filter.gameObject.layer==0,"destination unexpectedly outside default camera layer");
                    var mr=filter.GetComponent<MeshRenderer>();Require(mr&&mr.enabled&&mr.sharedMaterial&&mr.sharedMaterial.shader,"missing destination renderer/material");
                    Require(mesh.vertexCount>0&&mesh.bounds.size.sqrMagnitude>0,"empty geometry");
                    var vertices=mesh.vertices;var indices=mesh.triangles;var normals=mesh.normals;
                    Require(normals.Length==vertices.Length,"missing recalculated normals");
                    for(int i=0;i<vertices.Length;i++)
                    {
                        Require(Finite(vertices[i])&&Finite(normals[i]),"non-finite vertex/normal");
                        Require(vertices[i].y>=-3&&vertices[i].y<=40&&Mathf.Abs(vertices[i].x)<200&&Mathf.Abs(vertices[i].z)<200,"decorator coordinate escaped authored scale");
                        Require(normals[i].sqrMagnitude>.9f&&normals[i].sqrMagnitude<1.1f,"unusable mesh normal");
                    }
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        Require(indices[i]>=0&&indices[i]<vertices.Length&&indices[i+1]>=0&&indices[i+1]<vertices.Length&&indices[i+2]>=0&&indices[i+2]<vertices.Length,"invalid triangle index");
                        var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                        Require(Vector3.Cross(b-a,c-a).sqrMagnitude>1e-12f,"degenerate destination face");
                    }
                    triangleCount+=indices.Length/3;
                }
                Require(triangleCount<15000,"destination geometry exceeded mobile budget");
                CheckFeatures(decoration,destination,region);
                int childCount=decoration.childCount;
                DestinationWorld.Dress(root,region,destination);
                Require(root.transform.childCount==1&&decoration.childCount==childCount,"repeat Dress duplicated scenery");
                shape=filters.Length+"/"+triangleCount;
                Debug.Log("DESTINATION_QA="+destination+"/"+region+" meshes="+filters.Length+" triangles="+triangleCount);
            }
            finally
            {
                if(root)UnityEngine.Object.DestroyImmediate(root);
                if(original)UnityEngine.Object.DestroyImmediate(original);
                bool leaked=false;
                foreach(var mesh in generated)if(mesh){leaked=true;UnityEngine.Object.DestroyImmediate(mesh);}
                Require(!leaked,"destination meshes survived destruction of their world");
            }
            return triangleCount;
        }

        static void CheckFeatures(Transform decoration,string destination,string region)
        {
            if(region=="port")
            {
                var labels=decoration.GetComponentsInChildren<TextMesh>();
                Require(labels.Length==1,"expected one destination port sign: "+destination+", actual count="+labels.Length);
                Require(labels[0].text.Contains(DestinationWorld.Name(destination)),"port sign must name its destination: "+destination+", actual text="+labels[0].text);
            }
            if(destination=="rangoon")
            {
                var gold=decoration.Find("Destination detail · gold");Require(gold!=null,"Rangoon skyline missing");
                Bounds b=gold.GetComponent<MeshFilter>().sharedMesh.bounds;
                Require(b.min.z>100&&b.max.y>30&&b.min.y>2,"Rangoon stupa must be distant, elevated skyline");
            }
            if(destination=="penang"&&region=="port")
            {
                var roof=decoration.Find("Destination detail · roof");Require(roof!=null,"Penang stilt-house roofs missing");
                Bounds b=roof.GetComponent<MeshFilter>().sharedMesh.bounds;
                Require(b.min.x>36&&b.max.x>65&&b.min.z<0&&b.max.z>25,"Penang housing should extend along two offshore jetties");
            }
            if(destination=="singapore"&&region=="port")Require(decoration.Find("Destination detail · iron")!=null,"Singapore mooring posts missing");
        }

        static void CheckHomeUnchanged()
        {
            foreach(string id in new[]{"quanzhou","harbor","ship"})
            {
                var root=new GameObject("Destination QA home guard"){hideFlags=HideFlags.HideAndDontSave};
                try { foreach(string d in Destinations)DestinationWorld.Dress(root,id,d);Require(root.transform.childCount==0,"destination decorated home/sea: "+id); }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            DestinationWorld.Dress(null,"port","penang");
        }

        static int DestinationMaterialCount()
        {
            int count=0;
            foreach(var m in Resources.FindObjectsOfTypeAll<Material>())
                if(m.name.StartsWith("destination ")||m.name.Contains("/retint/test old limewash"))count++;
            return count;
        }
        static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z);
        static void Require(bool condition,string message){if(!condition)throw new Exception("DESTINATION_QA_FAILED: "+message);}
    }
}
