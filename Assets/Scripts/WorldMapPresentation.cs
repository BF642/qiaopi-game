using System.Collections.Generic;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        readonly List<Bounds> mapRoads=new List<Bounds>();
        readonly List<Bounds> mapRoofs=new List<Bounds>();
        readonly List<Bounds> mapFields=new List<Bounds>();

        void CacheMapSurfaces()
        {
            mapRoads.Clear();mapRoofs.Clear();mapFields.Clear();
            foreach(var renderer in world.GetComponentsInChildren<MeshRenderer>()){
                string name=renderer.name;
                // The original renderers remain after scenery batching. Their bounds
                // retain the authored footprint, including rotated harbor buildings.
                if(name.Contains("recessed joint bed"))mapRoads.Add(renderer.bounds);
                else if(name=="Sloping tiled roof")mapRoofs.Add(renderer.bounds);
                else if(name=="Village planted plot")mapFields.Add(renderer.bounds);
            }
            mapWalls.Sort((a,b)=>b.center.z.CompareTo(a.center.z));
        }
        bool MapFootprint(Bounds b,Rect plan,out Rect footprint)
        {
            float left=Mathf.Max(b.min.x,region.bounds.xMin),right=Mathf.Min(b.max.x,region.bounds.xMax);
            float bottom=Mathf.Max(b.min.z,region.bounds.yMin),top=Mathf.Min(b.max.z,region.bounds.yMax);
            if(right<=left||top<=bottom){footprint=default;return false;}
            footprint=PlanRect(Rect.MinMaxRect(left,bottom,right,top),plan);return true;
        }
        void MapWater(Rect r)
        {
            Box(r,C("AEC1B6"));
            Color wave=new Color(.27f,.45f,.43f,.23f);
            for(float y=r.y+15;y<r.yMax-7;y+=21)
                for(float x=r.x+8+((Mathf.FloorToInt((y-r.y)/21)%2)*14);x<r.xMax-12;x+=54){
                    float length=Mathf.Min(27,r.xMax-x-3);
                    InkLine(new Vector2(x,y),new Vector2(x+length*.45f,y-2),wave,1.2f);
                    InkLine(new Vector2(x+length*.45f,y-2),new Vector2(x+length,y),wave,1.2f);
                }
        }
        Rect FitRegionMap(Rect area)
        {
            // A north-up plan uses a single scale on both axes, including the ship.
            float units=Mathf.Min((area.width-20)/region.bounds.width,(area.height-20)/region.bounds.height);
            float width=region.bounds.width*units+20,height=region.bounds.height*units+20;
            return new Rect(area.center.x-width*.5f,area.center.y-height*.5f,width,height);
        }
        void DrawMapRoof(Rect roof,bool large)
        {
            if(roof.width<5||roof.height<5)return;
            Box(new Rect(roof.x+3,roof.y+5,roof.width,roof.height),new Color(.18f,.24f,.19f,.22f));
            Box(roof,C("777B69"));
            Box(new Rect(roof.x+1,roof.y+1,roof.width-2,roof.height*.49f),C("969884"));
            Box(new Rect(roof.x+1,roof.center.y,roof.width-2,roof.height*.49f),C("686F5D"));
            float spacing=large?7:5;
            for(float x=roof.x+spacing;x<roof.xMax-2;x+=spacing)
                Box(new Rect(x,roof.y+2,1,roof.height-4),new Color(.94f,.91f,.76f,.15f));
            Box(new Rect(roof.x-1,roof.center.y-1,roof.width+2,2),C("D0C4A4"));
            Stroke(roof,C("5B6252"));
        }
        void DrawDetailedRegionPlan(Rect panel,bool large)
        {
            PaperPanel(panel);Rect inside=new Rect(panel.x+14,panel.y+14,panel.width-28,panel.height-28);
            if(loadedWorld=="ship")MapWater(inside);
            Rect r=FitRegionMap(inside);
            Box(r,loadedWorld=="ship"?C("C2A97D"):C("DCCDAA"));
            foreach(var field in mapFields)if(MapFootprint(field,r,out Rect f)){
                Box(f,C("ABB087"));for(float y=f.y+5;y<f.yMax;y+=7)Box(new Rect(f.x+2,y,Mathf.Max(1,f.width-4),1),C("8E966F"));
            }
            if(loadedWorld=="harbor"){
                MapWater(PlanRect(new Rect(-35,21,70,16),r));
                Rect pier=PlanRect(new Rect(-6,21,12,16),r);Box(pier,C("B19B73"));
                for(float y=pier.y+5;y<pier.yMax;y+=7)Box(new Rect(pier.x+1,y,pier.width-2,1),C("8F7A56"));
                Stroke(pier,C("8F7A56"));
            }
            if(loadedWorld=="ship"){
                for(float y=r.y+7;y<r.yMax;y+=8)Box(new Rect(r.x+2,y,r.width-4,1),new Color(.39f,.30f,.18f,.20f));
                Box(new Rect(r.x+4,r.y,2,r.height),C("897655"));Box(new Rect(r.xMax-6,r.y,2,r.height),C("897655"));
            }
            foreach(var road in mapRoads)if(MapFootprint(road,r,out Rect p)){
                Box(p,C("E8DDC0"));Stroke(p,new Color(.66f,.59f,.44f,.23f));
            }
            foreach(var b in mapWalls)if(MapFootprint(b,r,out Rect wall)){
                float lift=Mathf.Clamp(b.size.y*(large?1.3f:.7f),1,7);
                Box(new Rect(wall.x+lift*.5f,wall.y+lift,wall.width,wall.height),new Color(.20f,.22f,.16f,.19f));
                Box(wall,b.size.y>2?C("9C8F72"):C("B6A484"));
                if(wall.width>4&&wall.height>4)Box(new Rect(wall.x+1,wall.y+1,wall.width-2,Mathf.Min(2,wall.height-2)),C("D5C5A3"));
            }
            foreach(var roof in mapRoofs)if(MapFootprint(roof,r,out Rect p))DrawMapRoof(p,large);
            if(large)foreach(var zone in region.zones){
                Rect z=PlanRect(zone.bounds,r);float labelHeight=MobileControls?32:26;
                float labelWidth=Mathf.Min(z.width,zone.name.Length*(MobileControls?22:18)+14);
                Rect tag=new Rect(z.center.x-labelWidth*.5f,z.y+3,labelWidth,labelHeight);
                Box(tag,new Color(.96f,.92f,.80f,.88f));
                Label(tag,zone.name,MobileControls?22:18,ink,true,TextAnchor.MiddleCenter);
            }
            for(int i=routeIndex;i<walkRoute.Count;i++){
                Vector2 p=MapPoint(walkRoute[i],r);Box(new Rect(p.x-2,p.y-2,4,4),C("3D6856"));
            }
            foreach(var note in region.notes){
                Vector2 p=MapPoint(note.position,r);Color color=state.flags.Contains("note_"+note.id)?C("76795F"):red;
                Box(new Rect(p.x-4,p.y-4,8,8),paper);Box(new Rect(p.x-3,p.y-3,6,6),color);
            }
            if(mission.activity=="inspect")for(int i=0;i<inspectionPositions.Length;i++)if(!inspected.Contains(i)){
                Vector2 p=MapPoint(inspectionPositions[i],r);Stroke(new Rect(p.x-5,p.y-5,10,10),C("A17C31"));
            }
            Vector2 target=MapPoint(Target(),r);
            Box(new Rect(target.x-8,target.y-8,16,16),C("B99141"));Stroke(new Rect(target.x-6,target.y-6,12,12),paper);
            Vector2 position=MapPoint(player.transform.position,r);
            Box(new Rect(position.x-7,position.y-7,14,14),paper);Box(new Rect(position.x-5,position.y-5,10,10),C("294D42"));
            Vector3 forward=cam.transform.forward;Vector2 direction=new Vector2(forward.x,-forward.z).normalized;
            Vector2 tip=position+direction*17,side=new Vector2(-direction.y,direction.x);
            InkLine(position,tip,C("294D42"),2.5f);InkLine(tip,tip-direction*6+side*4,C("294D42"),2.5f);InkLine(tip,tip-direction*6-side*4,C("294D42"),2.5f);
            Stroke(r,new Color(.39f,.40f,.30f,.45f));
            Label(new Rect(panel.x+20,panel.y+15,85,32),"北 ↑",MobileControls?25:19,ink,true);
            // The scale bar is computed from the same projection as all markers.
            float length=(r.width-20)*10/region.bounds.width;
            float sx=panel.xMax-length-26,sy=panel.yMax-24;
            Box(new Rect(sx,sy,length,2),ink);Box(new Rect(sx,sy-4,2,8),ink);Box(new Rect(sx+length-2,sy-4,2,8),ink);
            Label(new Rect(sx-12,sy-31,length+24,27),"10 米",MobileControls?22:16,ink,false,TextAnchor.MiddleCenter);
        }
    }
}
