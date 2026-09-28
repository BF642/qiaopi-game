using System.Collections.Generic;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        readonly List<Bounds> mapWalls=new List<Bounds>();
        int mapTab;
        RegionNote openedNote;
        void AddExplorationNotes()
        {
            Physics.SyncTransforms();mapWalls.Clear();foreach(var c in world.GetComponentsInChildren<Collider>())if(c.enabled&&c.bounds.size.y>.5f&&c.bounds.max.y>.6f)mapWalls.Add(c.bounds);
            CacheMapSurfaces();
            foreach(var n in region.notes){
                Cube("见闻木牌立柱",actors.transform,n.position+Vector3.up*.56f,new Vector3(.09f,1.12f,.09f),wood);
                Cube(n.title,actors.transform,n.position+Vector3.up*1.18f,new Vector3(.82f,.58f,.10f),cream);
                Cube("木牌印记",actors.transform,n.position+new Vector3(-.24f,1.18f,-.065f),new Vector3(.12f,.30f,.018f),Mat("963F2B"));
            }
        }
        RegionNote NearestNote(){foreach(var n in region.notes)if(Dist(n.position)<2.4f)return n;return null;}
        void ReadNote(RegionNote note)
        {
            openedNote=note;fieldNote=true;walkRoute.Clear();journalScroll=Vector2.zero;
            string key="note_"+note.id;if(!state.flags.Contains(key)){state.flags.Add(key);Save();}Sound();
        }
        string NavigationHint()
        {
            Vector3 d=Target()-player.transform.position;
            string[] directions={"北","东北","东","东南","南","西南","西","西北"};
            int i=Mathf.RoundToInt(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg/45f);i=(i+8)%8;
            return "目标在"+directions[i]+"面 · 约 "+Mathf.CeilToInt(Dist(Target()))+" 米";
        }
        void FieldNote()
        {
            if(openedNote==null){fieldNote=false;return;}
            if(MobileControls){MobileFieldNote();return;}
            Overlay("旅途见闻 · "+openedNote.title);
            Label(new Rect(230,253,1090,36),region.title+"  /  已收进地图中的见闻册",18,red);
            Label(new Rect(263,352,1050,240),openedNote.body,29,ink,true);
            Label(new Rect(263,659,1050,61),"除了金色任务标记，沿路的木牌也记着离乡者的生活。",21,sub,true);
            if(Button(new Rect(262,772,440,66),"收好见闻，继续走",true))fieldNote=false;
        }
        void MobileFieldNote()
        {
            Rect r=MapPanelBounds();Box(new Rect(MobileUiBounds().xMin-100,-100,MobileUiBounds().width+200,1200),new Color(.08f,.15f,.12f,.65f));PaperPanel(r);
            Stamp(new Rect(r.x+45,r.y+35,64,64),"见闻",29);
            Label(new Rect(r.x+132,r.y+41,r.width-200,60),openedNote.title,43,ink,true);
            Label(new Rect(r.x+48,r.y+132,r.width-96,58),region.title+"  ·  已收入地图的「见闻册」",30,red);
            float h=Height(MapTouchText(openedNote.body),38,r.width-128,true);
            journalScroll=GUI.BeginScrollView(new Rect(r.x+48,r.y+227,r.width-96,327),journalScroll,new Rect(0,0,r.width-126,Mathf.Max(h+20,322)),false,false);
            Label(new Rect(0,0,r.width-128,h+10),MapTouchText(openedNote.body),38,ink,true);GUI.EndScrollView();
            Label(new Rect(r.x+48,r.yMax-217,r.width-96,52),"沿路的木牌，记着离乡者的生活。",29,sub);
            if(LetterButton(new Rect(r.x+48,r.yMax-145,r.width-96,106),"收好见闻，继续走",true,34))fieldNote=false;
        }
        Rect PlanRect(Rect bounds,Rect panel)
        {
            var a=MapPoint(new Vector3(bounds.xMin,0,bounds.yMax),panel);var b=MapPoint(new Vector3(bounds.xMax,0,bounds.yMin),panel);
            return new Rect(a.x,a.y,Mathf.Max(1,b.x-a.x),Mathf.Max(1,b.y-a.y));
        }
        void DrawRegionPlan(Rect r,bool large){DrawDetailedRegionPlan(r,large);}
        Rect MapPanelBounds()
        {
            if(!MobileControls)return new Rect(165,136,1270,753);
            Rect safe=MobileUiBounds();return new Rect(safe.xMin+60,158,safe.width-120,794);
        }
        void MapTabs()
        {
            Rect r=MapPanelBounds();float gap=14,width=(r.width-96)/3;
            float top=r.y+(MobileControls?99:86),height=MobileControls?90:56;
            string[] labels={"本区行路图","行路记","见闻册"};
            for(int i=0;i<3;i++){
                Rect tab=new Rect(r.x+34+i*(width+gap),top,width,height);
                if(NavTab(tab,labels[i],i==0?"map":i==1?"letter":"album",mapTab==i,MobileControls?32:23)){
                    if(mapTab!=i)journalScroll=Vector2.zero;mapTab=i;
                }
            }
        }
        void GuideToMission()
        {
            walkRoute=WalkPath.Find(player.transform.position,Target(),loadedWorld);routeIndex=0;map=false;dialogue=false;
            if(walkRoute.Count==0)Toast("先走到附近的道路上，再试一次。");
        }
        void RegionMap()
        {
            Rect r=MapPanelBounds();bool mobile=MobileControls;int body=mobile?30:21;
            Box(new Rect(MobileUiBounds().xMin-120,-100,MobileUiBounds().width+240,1200),new Color(.08f,.15f,.12f,.65f));
            PaperPanel(r);Stamp(new Rect(r.x+34,r.y+23,mobile?56:46,mobile?56:46),"行路",mobile?25:21);
            Label(new Rect(r.x+(mobile?109:96),r.y+23,r.width-330,56),"一程山海 · "+region.title,mobile?37:30,ink,true);
            if(LetterButton(new Rect(r.xMax-(mobile?190:148),r.y+17,mobile?154:112,mobile?77:57),"收起",false,mobile?30:22)){map=false;return;}
            MapTabs();
            float top=r.y+(mobile?208:163),bottom=r.yMax-38;
            Rect content=new Rect(r.x+36,top,r.width-72,bottom-top);
            if(mapTab==1){MapJourney(content,mobile);return;}
            if(mapTab==2){MapNotebook(content,mobile);return;}
            float footer=mobile?60:53,mapHeight=content.height-footer;
            float planWidth=Mathf.Min(content.width*.57f,mapHeight+40);
            Rect plan=new Rect(content.x,content.y,planWidth,mapHeight);
            DrawRegionPlan(plan,true);
            float x=plan.xMax+32,width=content.xMax-x;
            Label(new Rect(x,top,width,mobile?41:30),"此刻要办的事",mobile?28:19,red,true);
            Label(new Rect(x,top+(mobile?48:38),width,mobile?96:78),mission.objective,mobile?34:25,ink,true);
            Label(new Rect(x,top+(mobile?151:125),width,48),NavigationHint(),body,ink,true);
            float legendY=top+(mobile?213:190),lineHeight=mobile?49:41;
            MapLegend(new Rect(x,legendY,width,lineHeight),C("294D42"),"你的位置 · 箭头是朝向",body);
            MapLegend(new Rect(x,legendY+lineHeight,width,lineHeight),C("B99141"),"当前任务",body);
            MapLegend(new Rect(x,legendY+lineHeight*2,width,lineHeight),red,"沿路见闻 · 木牌前互动",body);
            if(LetterButton(new Rect(x,plan.yMax-(mobile?102:74),width,mobile?102:74),"为当前任务引路",true,mobile?32:25))GuideToMission();
            Label(new Rect(content.x,plan.yMax+15,content.width,footer-8),mobile?"旧纸记山海，脚下认归途。地图与真实街巷对应；摇杆可随时接手行走。":"旧纸记山海，脚下认归途。地图与真实街巷对应；WASD 可随时接手行走。",mobile?26:18,sub);
        }
        void MapLegend(Rect r,Color color,string text,int size)
        {
            Box(new Rect(r.x,r.y+10,16,16),color);Label(new Rect(r.x+29,r.y,r.width-29,r.height),text,size,sub);
        }
        void MapJourney(Rect content,bool mobile)
        {
            var entries=FullJourneyHistory();
            int body=mobile?31:22,small=mobile?26:18,title=mobile?34:27;
            Label(new Rect(content.x,content.y,content.width,48),"从泉州出发，选择与回响都留在这一程。",small,sub);
            if(entries.Count==0){Label(new Rect(content.x,content.y+150,content.width,120),"走出老厝，故事才刚开始。",title,sub,true,TextAnchor.MiddleCenter);return;}
            float width=content.width-32,total=0;
            foreach(var entry in entries)total+=Height(entry.outcome,body,width-44,true)+Height("你选择："+entry.choice,body,width-44,true)+(mobile?172:139);
            journalScroll=GUI.BeginScrollView(new Rect(content.x,content.y+60,content.width,content.height-60),journalScroll,new Rect(0,0,width,total),false,false);
            float y=0;
            for(int i=entries.Count-1;i>=0;i--){
                var entry=entries[i];float choiceHeight=Height("你选择："+entry.choice,body,width-44,true),outcomeHeight=Height(entry.outcome,body,width-44,true);
                float heading=mobile?105:85;
                Label(new Rect(20,y,width-40,40),entry.year+" · "+entry.location,small,sub);
                Label(new Rect(20,y+(mobile?47:36),width-40,54),entry.title,title,ink,true);
                Label(new Rect(20,y+heading,width-44,choiceHeight+4),"你选择："+entry.choice,body,red,true);
                Label(new Rect(20,y+heading+choiceHeight+19,width-44,outcomeHeight+5),entry.outcome,body,ink,true);
                y+=choiceHeight+outcomeHeight+(mobile?172:139);Box(new Rect(20,y-18,width-40,1),line);
            }
            GUI.EndScrollView();
        }
        string MapTouchText(string text){return MobileControls?text.Replace("按 E 登船","点击「互动」登船").Replace("按 E 阅读","点击「互动」阅读"):text;}
        void MapNotebook(Rect content,bool mobile)
        {
            int count=0;foreach(string id in WorldRegions.All)foreach(var note in WorldRegions.Get(id).notes)if(state.flags.Contains("note_"+note.id))count++;
            Label(new Rect(content.x,content.y,content.width,53),"已收集 "+count+" / 21 处见闻  ·  "+(mobile?"走近场景中的木牌，点击「互动」阅读。":"走近场景中的木牌，按 E 阅读。"),mobile?28:20,sub);
            if(count==0){
                Stamp(new Rect(content.center.x-39,content.y+146,78,78),"见闻",31);
                Label(new Rect(content.x,content.y+259,content.width,135),"旅途中的字迹，还等着你亲自发现。\n泉州老厝外的木牌，记着离乡时的行囊。",mobile?33:27,sub,true,TextAnchor.MiddleCenter);return;
            }
            int body=mobile?31:22,title=mobile?33:26;float width=content.width-32,total=0;
            foreach(string id in WorldRegions.All)foreach(var note in WorldRegions.Get(id).notes)if(state.flags.Contains("note_"+note.id))
                total+=Height(MapTouchText(note.body),body,width-46,true)+(mobile?147:113);
            journalScroll=GUI.BeginScrollView(new Rect(content.x,content.y+65,content.width,content.height-65),journalScroll,new Rect(0,0,width,total),false,false);
            float y=0;
            foreach(string id in WorldRegions.All)foreach(var note in WorldRegions.Get(id).notes)if(state.flags.Contains("note_"+note.id)){
                float h=Height(MapTouchText(note.body),body,width-46,true),offset=mobile?96:73;
                Label(new Rect(20,y,width-40,48),note.title,title,red,true);
                Label(new Rect(20,y+(mobile?50:39),width-40,41),WorldRegions.Get(id).title,mobile?26:18,sub);
                Label(new Rect(20,y+offset,width-46,h+5),MapTouchText(note.body),body,ink,true);
                y+=h+(mobile?147:113);Box(new Rect(20,y-18,width-40,1),line);
            }
            GUI.EndScrollView();
        }
    }
}
