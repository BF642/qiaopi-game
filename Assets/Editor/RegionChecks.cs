using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Qiaopi.Editor
{
    public static class RegionChecks
    {
        static readonly string[] Nodes="home funding farewell passage shore dock shop courier first_pay first_letter reply records missing trace_desk trace_harbor trace_family storms horizon return_choice stay_choice end_home_lamp end_home_scar end_new_harbor end_school_window end_shop_bridge end_trusted_route end_unsent_letter end_silver_thread".Split(' ');
        public static void Run()
        {
            int total=0;
            foreach(string id in WorldRegions.All){
                GameObject root=null;
                try{
                    root=WorldFactory.Build(id);Physics.SyncTransforms();WalkPath.Invalidate();var r=WorldRegions.Get(id);
                    var targets=new HashSet<Vector3>{r.spawn,r.rest,r.sidePickup,r.sideDrop};
                    foreach(var n in r.notes)targets.Add(n.position);
                    foreach(string node in Nodes){var m=WorldMissions.Get(node);if(m.world!=id)continue;targets.Add(m.npcPosition);targets.Add(m.itemPosition);targets.Add(m.destination);}
                    if(id=="ship")targets.Add(new Vector3(0,0,18));
                    if(id=="market"||id=="postoffice")foreach(var p in new[]{new Vector3(-22,0,-4),new Vector3(21,0,7),new Vector3(0,0,28)})targets.Add(p);
                    foreach(var target in targets){
                        if(!r.IsGround(target))throw new Exception("REGION_QA_OUTSIDE "+id+" "+target);
                        if(Physics.CheckCapsule(target+Vector3.up*.55f,target+Vector3.up*1.65f,.35f,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("REGION_QA_POINT_BLOCKED "+id+" "+target);
                        if(WalkPath.Find(r.spawn,target,id).Count==0)throw new Exception("REGION_QA_PATH_MISSING "+id+" "+target);
                        total++;
                    }
                    var renderers=root.GetComponentsInChildren<Renderer>();
                    Debug.Log("REGION_QA_EDITOR="+id+" bounds="+r.bounds+" reachablePoints="+targets.Count+" renderers="+renderers.Length);
                }finally{
                    if(root){var meshes=new HashSet<Mesh>();foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&!EditorUtility.IsPersistent(f.sharedMesh))meshes.Add(f.sharedMesh);UnityEngine.Object.DestroyImmediate(root);foreach(var m in meshes)if(m)UnityEngine.Object.DestroyImmediate(m);}
                    Physics.SyncTransforms();WalkPath.Invalidate();
                }
            }
            var state=StoryEngine.NewGame();state.nodeId="passage";Require(WorldMissions.Get(state).world=="harbor","departure");state.flags.Add("aboard_passage");Require(WorldMissions.Get(state).world=="ship","boarding");
            var coast=WorldRegions.Get("harbor");var stopped=coast.Clamp(new Vector3(20,0,22));Require(coast.IsGround(stopped)&&Mathf.Abs(stopped.x-20)<.01f&&stopped.z<=21.3f,"coast stays on bank without sideways teleport");
            var save=new WorldSave{story=state,missionNode="passage",layoutVersion=2,px=0,pz=25};Require(WorldSaveStore.IsValid(save),"ship extended save");save.px=25;Require(!WorldSaveStore.IsValid(save),"ship off-deck save rejected");
            state.nodeId="shop";save.missionNode="shop";save.px=30;save.pz=30;Require(WorldSaveStore.IsValid(save),"large-map save");save.layoutVersion=0;save.px=0;save.pz=-10;Require(WorldSaveStore.IsValid(save),"legacy save retained");
            Debug.Log("REGION_QA_ALL_PASSED: 7 distinct maps, "+total+" real-physics reachable mission/exploration points, boarding transition, extended and legacy saves.");
        }
        static void Require(bool result,string label){if(!result)throw new Exception("REGION_QA_"+label);}
    }
}
