using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    /// <summary>Read-only geometry checks on freshly built temporary regional roots.</summary>
    public static class DistrictChecks
    {
        const int Mask=~((1<<2)|(1<<29)|(1<<30));
        static readonly Rect Legacy=new Rect(-35,-31,70,68);
        public static void Run()
        {
            int points=0;
            foreach(string id in new[]{"quanzhou","postoffice","quarters","port"}) {
                GameObject root=null;
                try {
                    root=WorldFactory.Build(id);Physics.SyncTransforms();WalkPath.Invalidate();
                    RegionInfo region=WorldRegions.Get(id);
                    foreach(string route in InquiryPlaces.Routes) {
                        if(InquiryPlaces.World(route)!=id)continue;
                        Vector3 previous=region.spawn;
                        for(int i=0;i<3;i++) {
                            Vector3 point=InquiryPlaces.Evidence(route,i);
                            Require(Open(point,region),id+" evidence stand "+i);
                            var path=WalkPath.Find(previous,point,id);
                            Require(path.Count>0,id+" evidence route "+i);
                            Require((path[path.Count-1]-point).sqrMagnitude<.01f,id+" precise desk arrival "+i);
                            RaycastHit table;
                            Vector3 paper=InquiryPlaces.DocumentPoint(route,i);
                            Require(Physics.Raycast(paper+Vector3.up*.4f,Vector3.down,out table,1.0f,Mask,QueryTriggerInteraction.Ignore),id+" real desk "+i);
                            Require(Mathf.Abs(table.point.y-.78f)<.09f,id+" desk height "+i);
                            Vector3 doorstep=point+Vector3.back*2.5f,approach=point+Vector3.back*5.1f;
                            Require(Open(doorstep,region)&&Open(approach,region),id+" door and porch "+i);
                            Require(Clear(approach,point),id+" uninterrupted doorway approach "+i);
                            Require(!Physics.Linecast(point+Vector3.up*1.62f,paper+Vector3.up*.05f,Mask,QueryTriggerInteraction.Ignore),id+" readable desk sightline "+i);
                            RequireRoof(root,route,i);
                            if(i==0) {
                                Vector3 helper=point+new Vector3(-1.45f,0,.15f);
                                Require(Open(helper,region),id+" helper standing clearance");
                                Require(Clear(point,helper),id+" helper conversation approach");
                            }
                            previous=point;points++;
                        }
                        Require(WalkPath.Find(previous,InquiryPlaces.Return(route),id).Count>0,id+" return to original clerk");
                    }
                    for(int route=0;route<2;route++) {
                        var stops=InquiryPlaces.PedestrianRoute(id,route);
                        for(int i=0;i<stops.Length;i++) {
                            Require(Open(stops[i],region),id+" daily stop "+route+"/"+i);
                            Require(WalkPath.Find(stops[i],stops[(i+1)%stops.Length],id).Count>0,id+" daily path "+route+"/"+i);points++;
                        }
                    }
                    if(id=="postoffice")Require(Clear(new Vector3(34.5f,0,25),new Vector3(38,0,25)),"old post compound joins east cross lane");
                    if(id=="port") {
                        foreach(Vector3 sea in new[]{new Vector3(-47,0,26),new Vector3(-60,0,30),new Vector3(42,0,10)}) {
                            Require(!region.IsGround(sea),"port sea excluded from navigation");
                            RaycastHit water;
                            Require(!Physics.Raycast(sea+Vector3.up*.4f,Vector3.down,out water,1,Mask,QueryTriggerInteraction.Ignore),"port sea is not invisible walkable land");
                            Require(region.IsGround(region.Clamp(sea)),"port water clamp ends on existing bank");
                        }
                    }
                    Measure(region);
                    Transform extension=root.transform.Find("Connected working district · "+id);
                    Require(extension,id+" actual district root");
                    // Every old layout-v2 coordinate stays within the new region.
                    // New blocking props must also stay outside that saved area.
                    var oldStandingSpace=new Bounds(new Vector3(0,1.10f,3),new Vector3(70,1.56f,68));
                    foreach(var collider in extension.GetComponentsInChildren<Collider>())
                        Require(!collider.enabled||!collider.bounds.Intersects(oldStandingSpace),id+" new collider occupies legacy save space: "+collider.name);
                    var meshes=new HashSet<Mesh>();int vertices=0,triangles=0;
                    foreach(var mf in extension.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh&&meshes.Add(mf.sharedMesh)) {vertices+=mf.sharedMesh.vertexCount;triangles+=mf.sharedMesh.triangles.Length/3;}
                    Debug.Log("DISTRICT_GEOMETRY "+id+" vertices="+vertices+" triangles="+triangles+" colliders="+extension.GetComponentsInChildren<Collider>().Length);
                }
                finally {
                    if(root) {var meshes=new HashSet<Mesh>();foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&!EditorUtility.IsPersistent(f.sharedMesh))meshes.Add(f.sharedMesh);UnityEngine.Object.DestroyImmediate(root);foreach(var m in meshes)if(m)UnityEngine.Object.DestroyImmediate(m);}
                    Physics.SyncTransforms();WalkPath.Invalidate();
                }
            }
            Debug.Log("DISTRICT_CHECKS_ALL_PASSED: 4 connected expanded maps, 9 indoor evidence desks with clear doors/readable sightlines/solid overhead roofs, 3 helper positions, protected port water, "+points+" clear and connected evidence/daily-work stops.");
        }
        static bool Clear(Vector3 from,Vector3 to)
        {
            Vector3 move=to-from;move.y=0;
            return move.sqrMagnitude<.0001f||!Physics.CapsuleCast(from+Vector3.up*.55f,from+Vector3.up*1.65f,.35f,move.normalized,move.magnitude,Mask,QueryTriggerInteraction.Ignore);
        }
        static void RequireRoof(GameObject root,string route,int index)
        {
            // Check the rendered roof surface from the visitor's eye, rather than
            // accepting a named roof object whose triangles face the wrong way.
            Transform room=root.transform.Find("Connected working district · "+InquiryPlaces.World(route)+"/"+InquiryPlaces.Title(route,index)+" · enterable workroom/Tiled room roof and rafters");
            Require(room,"physical room roof "+route+"/"+index);
            var probes=new List<MeshCollider>();
            try {
                foreach(var mesh in room.GetComponentsInChildren<MeshFilter>()) {
                    if(!mesh.sharedMesh||!mesh.name.StartsWith("Solid roof underside"))continue;
                    var probe=mesh.gameObject.AddComponent<MeshCollider>();probe.sharedMesh=mesh.sharedMesh;probes.Add(probe);
                }
                Physics.SyncTransforms();RaycastHit ceiling;
                Require(probes.Count>0&&Physics.Raycast(InquiryPlaces.Evidence(route,index)+Vector3.up*1.62f,Vector3.up,out ceiling,4,Mask,QueryTriggerInteraction.Ignore)&&probes.Contains(ceiling.collider as MeshCollider),"visible sheltered ceiling "+route+"/"+index);
            }
            finally {foreach(var probe in probes)if(probe)UnityEngine.Object.DestroyImmediate(probe);Physics.SyncTransforms();}
        }
        static bool Open(Vector3 p,RegionInfo region)
        {
            if(!region.IsGround(p))return false;
            RaycastHit floor;
            if(!Physics.Raycast(p+Vector3.up*4,Vector3.down,out floor,8,Mask,QueryTriggerInteraction.Ignore)||floor.point.y>.5f||floor.normal.y<.65f)return false;
            return !Physics.CheckCapsule(p+Vector3.up*.55f,p+Vector3.up*1.65f,.35f,Mask,QueryTriggerInteraction.Ignore);
        }
        static void Measure(RegionInfo r)
        {
            int minX=Mathf.CeilToInt(r.bounds.xMin),minZ=Mathf.CeilToInt(r.bounds.yMin),width=Mathf.FloorToInt(r.bounds.width),depth=Mathf.FloorToInt(r.bounds.height);
            var open=new bool[width*depth];var seen=new bool[width*depth];
            for(int z=0;z<depth;z++)for(int x=0;x<width;x++)open[z*width+x]=Open(new Vector3(minX+x,0,minZ+z),r);
            int start=(Mathf.RoundToInt(r.spawn.z)-minZ)*width+Mathf.RoundToInt(r.spawn.x)-minX;
            Require(start>=0&&start<open.Length&&open[start],r.id+" sample origin");
            var queue=new Queue<int>();queue.Enqueue(start);seen[start]=true;
            int total=0,legacy=0;int[] dx={-1,1,0,0},dz={0,0,-1,1};
            while(queue.Count>0) {
                int n=queue.Dequeue(),x=n%width,z=n/width;total++;
                Vector3 from=new Vector3(minX+x,0,minZ+z);
                if(Legacy.Contains(new Vector2(from.x,from.z)))legacy++;
                for(int k=0;k<4;k++) {
                    int nx=x+dx[k],nz=z+dz[k];if(nx<0||nz<0||nx>=width||nz>=depth)continue;
                    int next=nz*width+nx;if(!open[next]||seen[next])continue;
                    Vector3 direction=new Vector3(dx[k],0,dz[k]);
                    if(Physics.CapsuleCast(from+Vector3.up*.55f,from+Vector3.up*1.65f,.35f,direction,1,Mask,QueryTriggerInteraction.Ignore))continue;
                    seen[next]=true;queue.Enqueue(next);
                }
            }
            Require(total>legacy+900,r.id+" substantial connected ground increase");
            Debug.Log("DISTRICT_WALKABLE_SAMPLE "+r.id+" grid=1m connected="+total+" legacyRectangle="+legacy+" added="+(total-legacy)+" ratio="+((float)total/legacy).ToString("F3")+" boundingArea="+(r.bounds.width*r.bounds.height).ToString("F0")+" legacyBoundingArea=4760");
        }
        static void Require(bool value,string label){if(!value)throw new Exception("DISTRICT_CHECK_FAILED "+label);}
    }
}
