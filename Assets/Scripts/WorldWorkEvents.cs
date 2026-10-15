using System;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool WorkEventQA=>Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-work-event-test")>=0;
        bool workTalkPanel;
        Vector2 workEventScroll;
        GameObject workEventPartner;
        bool WorkCourierRelocated=>WorkEvents.HasIncident(state)&&state.tradeShift.route=="courier"&&state.tradeShift.level==2;
        bool WorkCourierMoved=>WorkCourierRelocated&&WorkEvents.ChoiceId(state)=="trace";

        Vector3 WorkEventPoint()
        {
            if(!InTrade)return mission.npcPosition;
            if(state.tradeShift.route=="shop")return TradePlaces.ShopCounter;
            if(state.tradeShift.route=="courier")return TradePlaces.CourierNeighbour;
            return TradePlaces.DockPickup(Mathf.Max(0,TradeWork.DockPriority(state)));
        }
        Vector3 WorkEventFace()
        {
            if(state.tradeShift.route=="shop")return TradeShopFace();
            if(state.tradeShift.route=="courier")return TradePlaces.CourierNeighbour+Vector3.up*1.5f;
            return workEventPartner?workEventPartner.transform.position+Vector3.up*1.52f:WorkEventPoint()+Vector3.up;
        }
        void SetupWorkEventPeople()
        {
            workEventPartner=null;
            if(!WorkEvents.HasIncident(state)||state.tradeShift.route!="dock"||!actors)return;
            Vector3 p=WorldFactory.CharacterGroundPosition(new Vector3(-15.4f,0,12));
            workEventPartner=CharacterAppearanceProfiles.Spawn("dock_ashun",actors.transform,p);
            if(workEventPartner){workEventPartner.name="工友阿顺";workEventPartner.AddComponent<CharacterIdle>();workEventPartner.transform.rotation=Quaternion.Euler(0,-90,0);}
        }
        bool ChooseWorkEvent(string id)
        {
            if(WorkPerformanceBusy||!tradePanel||workTalkPanel||!WorkEvents.Pending(state)||tradePanelStage!=state.tradeShift.stage||!FirstDayCanReach(WorkEventPoint(),2.2f))return false;
            if(!WorkEvents.Choose(state,id,out string notice))return false;
            tradePanel=false;workTalkPanel=false;workEventScroll=Vector2.zero;
            // Choices can change the number of parcels or the confirmed address.
            // Rebuild owned props while retaining the same player's standing place.
            EnterNode();lifePanel=false;Save();Toast(notice);return true;
        }
        void DrawWorkEventPanel()
        {
            Rect safe=MobileControls?MobileUiBounds():new Rect(0,0,W,H);
            float width=Mathf.Min(690,safe.width-64);Rect r=new Rect(safe.xMax-width-32,187,width,768);
            PaperPanel(r,true,.96f);Box(new Rect(r.x,r.y,3,r.height),letterSeal);
            Label(new Rect(r.x+28,r.y+22,r.width-167,44),WorkEvents.Title(state),31,ink,true);
            if(LetterButton(new Rect(r.xMax-115,r.y+18,90,50),"收起",false,23)){tradePanel=false;return;}
            float x=r.x+29,w=r.width-58;string route=state.tradeShift.route;
            string speaker=route=="dock"?"工友阿顺":route=="shop"?"陈记柜台":"茶棚街坊";
            Label(new Rect(x,r.y+78,w,34),speaker+" · 这一轮的商量",23,sub);
            string body=WorkEvents.Body(state),greeting=WorkRelationshipText.Greeting(state,route);
            if(WorkEvents.LastRecord(state,route)!=null&&!string.IsNullOrEmpty(greeting))body=greeting+"\n"+body;
            Rect prose=new Rect(x,r.y+123,w,205);float h=Height(body,26,w-26,true);
            GalleryTouchScroll(prose,ref workEventScroll,h);workEventScroll=GUI.BeginScrollView(prose,workEventScroll,new Rect(0,0,w-26,Mathf.Max(205,h+8)),false,false);
            Label(new Rect(0,0,w-26,h+8),body,26,ink,true);GUI.EndScrollView();
            float y=r.y+352;
            foreach(var choice in WorkEvents.Choices(state)){
                Rect c=new Rect(x,y,w,165);bool hover=c.Contains(Event.current.mousePosition);
                Box(c,new Color(.92f,.85f,.71f,hover?.30f:.12f));Box(new Rect(c.x,c.y,3,c.height),letterSeal);
                Label(new Rect(c.x+16,c.y+12,c.width-32,39),choice.title,27,ink,true);
                Label(new Rect(c.x+16,c.y+57,c.width-32,65),choice.description,24,sub);
                string effect="工钱 "+SignedWorkDelta(choice.money)+" · 身体 "+SignedWorkDelta(choice.health)+" · 信用 "+SignedWorkDelta(choice.trust);
                if(choice.relation>0)effect+=" · 留下交情";
                Label(new Rect(c.x+16,c.yMax-32,c.width-32,27),effect,20,letterSeal);
                if(GUI.Button(c,GUIContent.none,blank)){ChooseWorkEvent(choice.id);return;}y+=178;
            }
            Label(new Rect(x,r.yMax-41,w,31),"数值为这次商量带来的额外变化，交工时结算。",19,sub);
        }
        string SignedWorkDelta(int n){return n>0?"+"+n:n==0?"不变":n.ToString();}
        void DrawWorkRelationshipLog(Rect r,bool mobile)
        {
            string summary=WorkRelationshipText.Summary(state);
            if(string.IsNullOrEmpty(summary)){DrawLifeLog(r,mobile);return;}
            Label(new Rect(r.x+10,r.y,r.width-20,34),"一起办过事的人",mobile?27:25,ink,true);
            Label(new Rect(r.x+10,r.y+43,r.width-20,103),summary,mobile?24:22,sub);
            DrawLifeLog(new Rect(r.x,r.y+160,r.width,r.height-160),mobile);
        }
        void OpenWorkTalk()
        {
            OpenTradePanel(-1,mission.npcPosition+Vector3.up*1.5f);workTalkPanel=true;
            if(npc)CharacterPerformance.For(npc).LookAt(CameraEyePoint());
        }
        void DrawWorkTalk()
        {
            Rect safe=MobileControls?MobileUiBounds():new Rect(0,0,W,H);
            float width=Mathf.Min(650,safe.width-64);Rect r=new Rect(safe.xMax-width-32,300,width,555);
            PaperPanel(r,true,.96f);Box(new Rect(r.x,r.y,3,r.height),letterSeal);
            Label(new Rect(r.x+28,r.y+27,r.width-170,47),mission.npcName+" · 近况",31,ink,true);
            if(LetterButton(new Rect(r.xMax-115,r.y+22,88,49),"收起",false,23)){tradePanel=false;workTalkPanel=false;return;}
            string greeting=WorkRelationshipText.Greeting(state,state.tradeShift.route);
            if(string.IsNullOrEmpty(greeting))greeting=state.tradeShift.route=="dock"?"先认货签，再找对应的货栈。雨前纸货得先入棚，办妥了回来一起点清。":state.tradeShift.route=="shop"?"客人要多少米、交来多少现钱，都先讲清楚。称量、找零和赊欠一项项办妥，再来交账。":"银信都托在你手里。同名的人要核清住处，亲交本人，收讫回执也别落下。";
            Label(new Rect(r.x+29,r.y+110,r.width-58,180),greeting,29,ink,true);
            Label(new Rect(r.x+29,r.y+317,r.width-58,92),"眼下先"+TradeObjective()+"。",25,sub);
            if(LetterButton(new Rect(r.x+29,r.yMax-105,r.width-58,65),"记下这些话，接着办活",true,27)){tradePanel=false;workTalkPanel=false;}
        }
    }
}
