using System;
using UnityEngine;

namespace Qiaopi
{
    public static class TradeMissions
    {
        public static void Apply(GameState s, MissionInfo m)
        {
            if(!TradeWork.IsActive(s))return;
            string route=s.tradeShift.route;m.activity="trade";m.required=1;
            m.itemName=route=="dock"?"分货入栈":route=="shop"?"称米与交账":"送批与收回执";
            m.objective=route=="dock"?"看货签，分送到对应货栈，最后找记工人结账。":route=="shop"?"在陈记柜台称足米、找清钱，再向掌柜交账。":"读批封、问门牌、核本人，交批取回执后回信局交差。";
            m.itemPosition=route=="dock"?TradePlaces.DockPickup(0):route=="shop"?TradePlaces.ShopCounter:TradePlaces.CourierRead;
            m.destination=route=="dock"?TradePlaces.DockDrop(0):route=="shop"?TradePlaces.ShopCounter:TradePlaces.CourierRecipient(TradeWork.CourierRecipient(s));
        }
    }

    public partial class WorldGame
    {
        bool tradePanel;
        int tradePerson=-1;
        string tradePanelStage="",tradeNotice="";
        readonly GameObject[] tradePeople=new GameObject[3];
        bool TradeQA=>Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-trade-test")>=0;
        bool InTrade=>TradeWork.IsActive(state);
        bool TradeHeldBox=>InTrade&&state.tradeShift.route=="dock"&&state.tradeShift.held>=0;
        bool TradeHeldLetter=>InTrade&&state.tradeShift.route=="courier"&&(state.tradeShift.stage=="ask"||state.tradeShift.stage=="verify"||state.tradeShift.stage=="deliver"||state.tradeShift.stage=="return");

        void SetupTradePeople()
        {
            for(int i=0;i<tradePeople.Length;i++)tradePeople[i]=null;
            if(!InTrade||state.tradeShift.route!="courier"||!actors)return;
            for(int i=0;i<3;i++){
                Vector3 p=i==0?TradePlaces.CourierNeighbour:TradePlaces.CourierRecipient(i-1);
                string identity=i==0?"tea_neighbour":CharacterAppearanceProfiles.CourierRecipientIdentity(WorkCourierRelocated,i-1);
                var person=CharacterAppearanceProfiles.Spawn(identity,actors.transform,WorldFactory.CharacterGroundPosition(p));
                tradePeople[i]=person;if(!person)continue;
                person.name=i==0?"茶棚街坊":"陈阿福 · "+TradePlaces.CourierAddress(i-1);
                person.AddComponent<CharacterIdle>();person.transform.rotation=Quaternion.Euler(0,i==2?180:0,0);
            }
        }
        Vector3 TradeTarget()
        {
            if(!InTrade||TradeWork.IsComplete(state))return mission.npcPosition;
            var t=state.tradeShift;
            if(t.route=="dock"){
                if(t.held>=0)return TradePlaces.DockDrop(TradeWork.DockDestination(state,t.held));
                int index=TradeWork.DockPriority(state);if(index<0){index=0;while((t.deliveredMask&(1<<index))!=0)index++;}
                return TradePlaces.DockPickup(index);
            }
            if(t.route=="shop")return TradePlaces.ShopCounter;
            return t.stage=="return"?mission.npcPosition:t.stage=="read"?TradePlaces.CourierRead:t.stage=="ask"?TradePlaces.CourierNeighbour:TradePlaces.CourierRecipient(TradeWork.CourierRecipient(state));
        }
        int NearestTradeStation(bool drop)
        {
            int best=-1;float distance=2.2f;
            for(int i=0;i<(drop?3:TradeWork.DockCount(state));i++){
                if(!drop&&(state.tradeShift.deliveredMask&(1<<i))!=0)continue;
                Vector3 p=drop?TradePlaces.DockDrop(i):TradePlaces.DockPickup(i);
                float d=Dist(p);if(d<distance&&FirstDayCanReach(p,2.2f)){distance=d;best=i;}
            }
            return best;
        }
        int NearTradePerson()
        {
            for(int i=0;i<2;i++)if(FirstDayCanReach(TradePlaces.CourierRecipient(i),2.2f))return i;
            return -1;
        }
        string TradePrompt()
        {
            if(!InTrade)return "";
            if(writingRequested)return NearWritingDesk?"E  落座写批":"";
            if(TradeWork.IsComplete(state))return FirstDayCanReach(mission.npcPosition)?"E  交工 · 结工钱":"";
            var t=state.tradeShift;
            if(WorkEvents.Pending(state)&&FirstDayCanReach(WorkEventPoint(),2.2f))return "E  商量 · "+WorkEvents.Title(state);
            if(t.stage!="return"&&FirstDayCanReach(mission.npcPosition,2.2f))return "E  与"+mission.npcName+"聊两句";
            if(WorkEvents.Pending(state))return "";
            if(t.stage=="return")return FirstDayCanReach(mission.npcPosition,2.2f)?"E  登记原封退批":"";
            if(t.route=="dock"){
                int i=NearestTradeStation(t.held>=0);return i<0?"":t.held>=0?"E  放到"+TradePlaces.DockZone(state,i):"E  扛起"+TradeWork.DockLabel(state,i);
            }
            if(t.route=="shop")return FirstDayCanReach(TradePlaces.ShopCounter,2.2f)?"E  "+(t.stage=="weigh"?"到柜台称米":t.stage=="change"?"核清找零":"记入赊账簿"):"";
            if(t.stage=="read")return FirstDayCanReach(TradePlaces.CourierRead,2.2f)?"E  读批封 · 取批包":"";
            if(t.stage=="ask")return FirstDayCanReach(TradePlaces.CourierNeighbour,2.2f)?"E  向街坊问路":"";
            return NearTradePerson()<0?"":"E  "+(t.stage=="verify"?"核认姓名与门牌":t.stage=="deliver"?"交付侨批":"取回收讫回执");
        }
        bool HandleTradeInteract()
        {
            if(!InTrade)return false;
            if(writingRequested&&NearWritingDesk)return false;
            if(TradeWork.IsComplete(state)){
                if(FirstDayCanReach(mission.npcPosition))HandleLifeInteract();
                return true;
            }
            var t=state.tradeShift;
            if(t.stage!="return"&&FirstDayCanReach(mission.npcPosition,2.2f)){OpenWorkTalk();return true;}
            if(WorkEvents.Pending(state)){
                if(FirstDayCanReach(WorkEventPoint(),2.2f))OpenTradePanel(-1,WorkEventFace());
                return true;
            }
            if(t.stage=="return"){if(FirstDayCanReach(mission.npcPosition,2.2f))OpenTradePanel(-1,mission.npcPosition+Vector3.up*1.5f);return true;}
            if(t.route=="dock"){
                int i=NearestTradeStation(t.held>=0);if(i<0)return true;
                bool dropping=t.held>=0;int item=dropping?t.held:i;
                string notice;bool changed=t.held>=0?TradeWork.DockDrop(state,i,out notice):TradeWork.DockPick(state,i,out notice);
                if(changed){CancelWritingWalk();UpdateTradeArt();Save();BeginWorkPerformance(dropping?"dock-drop":"dock-pick",dropping?TradePlaces.DockDropFace(i):TradePlaces.DockPickupFace(i),0,item);}Toast(notice);return true;
            }
            if(t.route=="shop"){
                if(FirstDayCanReach(TradePlaces.ShopCounter,2.2f))OpenTradePanel(-1,TradeShopFace());
                return true;
            }
            if(t.stage=="read"&&FirstDayCanReach(TradePlaces.CourierRead,2.2f))OpenTradePanel(-1,TradePlaces.CourierReadFace);
            else if(t.stage=="ask"&&FirstDayCanReach(TradePlaces.CourierNeighbour,2.2f))OpenTradePanel(-1,TradePlaces.CourierNeighbour+Vector3.up*1.5f);
            else if(t.stage=="verify"||t.stage=="deliver"||t.stage=="receipt"){
                int i=NearTradePerson();if(i>=0)OpenTradePanel(i,TradePlaces.CourierFace(i));
            }
            return true;
        }
        Vector3 TradeShopFace(string stage = null)
        {
            if(stage==null)stage=InTrade?state.tradeShift.stage:"weigh";
            return stage=="change"?new Vector3(-15.56f,.99f,13.52f):stage=="credit"?new Vector3(-13.4f,1.02f,13.52f):new Vector3(-14.79f,1.30f,13.5f);
        }
        void AimTradeCamera(Vector3 face)
        {
            Vector3 gaze=face-CameraEyePoint();cameraYaw=Mathf.Atan2(gaze.x,gaze.z)*Mathf.Rad2Deg;
            cameraPitch=-Mathf.Atan2(gaze.y,new Vector2(gaze.x,gaze.z).magnitude)*Mathf.Rad2Deg;UpdateCamera(true);
        }
        void OpenTradePanel(int person,Vector3 face)
        {
            workTalkPanel=false;workEventScroll=Vector2.zero;tradePanel=true;tradePerson=person;tradePanelStage=state.tradeShift.stage;tradeNotice="";
            firstDayPanel=nanyangPanel=dialogue=lifePanel=journal=map=help=gallery=false;
            CancelWritingWalk();voiceSource.Stop();
            int actor=person>=0?person+1:state.tradeShift.stage=="ask"?0:-1;
            if(state.tradeShift.route=="courier"&&actor>=0&&tradePeople[actor]){
                CharacterPerformance.For(tradePeople[actor]).LookAt(CameraEyePoint());
            }
            if(state.tradeShift.route=="dock"&&workEventPartner)CharacterPerformance.For(workEventPartner).LookAt(CameraEyePoint());
            AimTradeCamera(face);CancelCameraPointer();ResetMobileControls();
        }
        bool TradeAction(string action,int amount=0)
        {
            if(WorkPerformanceBusy||!InTrade||!tradePanel||workTalkPanel||tradePanelStage!=state.tradeShift.stage)return false;
            var t=state.tradeShift;
            Vector3 point=t.stage=="return"?mission.npcPosition:t.route=="shop"?TradePlaces.ShopCounter:t.stage=="read"?TradePlaces.CourierRead:t.stage=="ask"?TradePlaces.CourierNeighbour:TradePlaces.CourierRecipient(tradePerson);
            if(!FirstDayCanReach(point,2.2f))return false;
            if((action=="check"||action=="deliver"||action=="receipt")&&(tradePerson<0||amount!=tradePerson))return false;
            int oldValue=action=="add-weight"?t.weightUnits:action=="add-change"?t.change:t.debt;string oldStage=t.stage;
            bool changed=false;string notice="";
            switch(action){
                case "add-weight":changed=TradeWork.AdjustWeight(state,amount);break;
                case "confirm-weight":changed=TradeWork.ConfirmWeight(state,out notice);break;
                case "add-change":changed=TradeWork.AdjustChange(state,amount);break;
                case "confirm-change":changed=TradeWork.ConfirmChange(state,out notice);break;
                case "add-debt":changed=TradeWork.AdjustDebt(state,amount);break;
                case "confirm-debt":changed=TradeWork.ConfirmDebt(state,out notice);break;
                case "read":changed=TradeWork.CourierRead(state,out notice);break;
                case "ask":changed=TradeWork.CourierAsk(state,out notice);break;
                case "check":changed=TradeWork.CourierCheck(state,amount,out notice);break;
                case "deliver":changed=TradeWork.CourierDeliver(state,amount,out notice);break;
                case "receipt":changed=TradeWork.CourierReceipt(state,amount,out notice);break;
                case "register-return":changed=TradeWork.CourierReturn(state,out notice);break;
            }
            tradeNotice=notice;
            if(changed){
                UpdateTradeArt();Save();
                if((QA&&!PerformanceQA)&&t.route=="shop"&&tradePanelStage!=t.stage&&t.stage!="done")AimTradeCamera(TradeShopFace());
                tradePanelStage=t.stage;
                if(t.route=="courier"||t.stage=="done"){tradePanel=false;if(!string.IsNullOrEmpty(notice))Toast(notice);}
                PresentTradeAction(action,amount,oldValue,oldStage);
            }
            return changed;
        }
        void AbandonTradeJob()
        {
            if(!lifePanel||!LifeJourney.IsActive(state)||string.IsNullOrEmpty(state.journey.pendingJob))return;
            TradeWork.Clear(state);state.journey.pendingJob="";tradePanel=false;EnterNode();lifePanel=true;Save();
            lifeNotice="已放下这份工作，没有结算工钱；可以重新接活。";
        }
        string TradeObjective()
        {
            if(TradeWork.IsComplete(state))return "工作已办妥，找"+mission.npcName+"结工钱";
            var t=state.tradeShift;
            if(WorkEvents.Pending(state))return "到现场商量："+WorkEvents.Title(state);
            if(t.stage=="return")return "把未交付的侨批原封带回柜台登记";
            if(t.route=="dock")return TradeHeldBox?"按货签送到"+TradePlaces.DockZone(state,TradeWork.DockDestination(state,t.held)):TradeWork.DockPriority(state)>=0?(LifeJourney.DestinationId(state)=="semarang"?"急雨将至，先送糖货避雨":"天色转潮，先送纸货入棚"):"看货签，逐件分送入栈";
            if(t.route=="shop")return t.stage=="weigh"?"到陈记柜台，称足客人要的米":t.stage=="change"?"数清货钱，把找零放上钱盘":"现钱与赊欠分开记清";
            return t.stage=="read"?"读批封，取待送的批包":t.stage=="ask"?"向茶棚街坊问清门牌":t.stage=="verify"?"找到"+TradeWork.CourierAddress(state)+"，核认陈阿福":t.stage=="deliver"?"把侨批交到本人手里":"等本人签收，取回回执";
        }
        string TradeProgress()
        {
            var t=state.tradeShift;if(t.stage=="done")return "待结账";
            if(WorkEvents.Pending(state))return "本轮有一桩事要商量";
            if(t.stage=="return")return "未交付 · 待退局登记";
            if(t.route=="dock"){int n=0;for(int i=0;i<TradeWork.DockCount(state);i++)if((t.deliveredMask&(1<<i))!=0)n++;return "入栈 "+n+" / "+TradeWork.DockCount(state);}
            if(t.route=="shop")return t.stage=="weigh"?"称米":t.stage=="change"?"找零":"赊账";
            return t.stage=="read"?"批封":t.stage=="ask"?"问路":t.stage=="verify"?"核人":t.stage=="deliver"?"交批":"收执";
        }
        void TradeWorldLabels()
        {
            if(!InTrade||Blocked)return;
            var t=state.tradeShift;
            if(t.route=="dock"){
                for(int i=0;i<TradeWork.DockCount(state);i++)if((t.deliveredMask&(1<<i))==0&&t.held!=i&&Dist(TradePlaces.DockPickup(i))<12)Tag(TradePlaces.DockPickupFace(i)+Vector3.up*.75f,TradeWork.DockLabel(state,i),false);
                for(int i=0;i<3;i++)if(Dist(TradePlaces.DockDrop(i))<12)Tag(TradePlaces.DockDropFace(i)+Vector3.up*.85f,TradePlaces.DockZone(state,i),false);
            }else if(t.route=="shop"&&Dist(TradePlaces.ShopCounter)<12)Tag(TradePlaces.ShopFace+Vector3.up*.45f,"陈记 · 称米与交账",false);
            else if(t.route=="courier")for(int i=0;i<3;i++)if(tradePeople[i]&&Dist(tradePeople[i].transform.position)<12)Tag(tradePeople[i].transform.position+Vector3.up*2.05f,tradePeople[i].name,false);
        }
        string TradeWeight(int units){return (units*.5f).ToString("0.#")+" 斤";}
        void DrawTradePanel()
        {
            if(!InTrade){tradePanel=false;return;}
            if(workTalkPanel){DrawWorkTalk();return;}
            if(WorkEvents.Pending(state)){DrawWorkEventPanel();return;}
            var t=state.tradeShift;Rect safe=MobileControls?MobileUiBounds():new Rect(0,0,W,H);
            float width=Mathf.Min(625,safe.width-64);Rect r=new Rect(safe.xMax-width-32,230,width,720);
            PaperPanel(r,true,.96f);Box(new Rect(r.x,r.y,3,r.height),letterSeal);
            string title=t.route=="shop"?t.stage=="weigh"?"陈记 · 称米":t.stage=="change"?"陈记 · 找零":"陈记 · 赊账簿":t.stage=="read"?"一封待送的侨批":t.stage=="ask"?"茶棚街坊":t.stage=="return"?"原封退批登记":t.stage=="receipt"?"收讫回执":"当面核批";
            Label(new Rect(r.x+27,r.y+24,r.width-170,46),title,31,ink,true);
            if(LetterButton(new Rect(r.xMax-120,r.y+20,95,50),"收起",false,23)){tradePanel=false;return;}
            float x=r.x+28,w=r.width-56,y=r.y+93;
            if(t.route=="shop"){
                Label(new Rect(x,y,w,75),"客人要米 "+TradeWeight(TradeWork.ShopWeightTarget(state))+"\n货钱 "+TradeWork.ShopPrice(state)+" 份 · 交来 "+TradeWork.ShopTender(state)+" 份",27,ink,true);
                y+=102;Box(new Rect(x,y,w,1),line);y+=26;
                if(t.stage=="weigh"){
                    Label(new Rect(x,y,w,66),"秤上  "+TradeWeight(t.weightUnits),42,ink,true);
                    Label(new Rect(x,y+73,w,44),"半斤一勺，添减后看秤。",25,sub);
                    string[] titles={"取回半斤","添半斤","添一斤"};int[] values={-1,1,2};
                    for(int i=0;i<3;i++)if(LetterButton(new Rect(x+i*(w+9)/3,y+135,(w-18)/3,63),titles[i],false,24))TradeAction("add-weight",values[i]);
                    if(LetterButton(new Rect(x,y+229,w,65),"称足了，包好米",true,27))TradeAction("confirm-weight");
                }else if(t.stage=="change"){
                    Label(new Rect(x,y,w,64),"找零盘  "+t.change+" 份",40,ink,true);
                    Label(new Rect(x,y+69,w,46),"点钱入盘，放多了可以取回。",25,sub);
                    int[] coins={1,2,5};
                    for(int i=0;i<3;i++){
                        float cx=x+i*(w+9)/3;Rect c=new Rect(cx,y+123,(w-18)/3,65);
                        TouchCircle(c.center,30,new Color(.69f,.48f,.21f,.3f));
                        if(LetterButton(c,"＋"+coins[i],false,28))TradeAction("add-change",coins[i]);
                        if(LetterButton(new Rect(cx,y+199,c.width,51),"取回 "+coins[i],false,22))TradeAction("add-change",-coins[i]);
                    }
                    if(LetterButton(new Rect(x,y+282,w,65),"核清钱，交给客人",true,27))TradeAction("confirm-change");
                }else{
                    Label(new Rect(x,y,w,70),"已收现钱 "+TradeWork.ShopTender(state)+" 份\n另记赊欠，留待日后收取",27,sub);
                    Label(new Rect(x,y+99,w,62),"赊欠栏  "+t.debt+" 份",39,ink,true);
                    if(LetterButton(new Rect(x,y+186,(w-14)/2,61),"减一份",false,26))TradeAction("add-debt",-1);
                    if(LetterButton(new Rect(x+(w+14)/2,y+186,(w-14)/2,61),"记一份",false,26))TradeAction("add-debt",1);
                    if(LetterButton(new Rect(x,y+282,w,65),"把这笔账记妥",true,27))TradeAction("confirm-debt");
                }
            }else{
                string address=TradeWork.CourierAddress(state);string body,button,action;
                if(t.stage=="return"){
                    body="收件人   陈阿福\n\n此批尚未交付，没有收件人回执。\n原封交回柜台，登记"+(t.level==2?"迁居后未能亲交":"未能取得本人收讫")+"。\n\n这次只按退局登记结账。";button="原封交回，登记未送达";action="register-return";
                }else if(t.stage=="read"){
                    body="收件人   陈阿福\n收件处   "+address+"\n\n银信同封，务交本人。\n核清住处，收回签认的回执。";button="读清批封，装好批包";action="read";
                }else if(t.stage=="ask"){
                    body=WorkCourierMoved?"“批封还是旧址北巷七号。他本人迁到了南街三号，记下这条线索，到了仍要当面核清，不能只凭名字交批。”": "“这条街有两位陈阿福，单问名字会弄错。"+(TradeWork.CourierRecipient(state)==0?"南街三号在批局南面，门口挂着门牌。":"北巷七号在北面的后院，他本人会在门口收批。")+"”\n\n“见到人，再核一遍批封上的门牌。”";button="记住门牌，去找本人";action="ask";
                }else if(t.stage=="verify"){
                    body=(WorkCourierMoved?"批封原址   北巷七号\n街坊指向新址   南街三号":"批封写着\n陈阿福 · "+address)+"\n\n面前的人说\n“我是陈阿福，这里是"+TradePlaces.CourierAddress(tradePerson)+"。”";button="核认姓名与住处";action="check";
                }else if(t.stage=="deliver"){
                    body=(WorkCourierMoved?"已核清姓名、批封旧址与迁居后的新住处。":"批封与本人确认的住处相符。")+"\n\n把封好的侨批亲手交给他。\n他核收后，还要留下收讫回执。";button="将侨批交到本人手中";action="deliver";
                }else{
                    body="兹收到此批，银信俱全。\n\n签认   陈阿福\n门牌   "+address+"\n\n带回信局，交给何师傅留底。";button="收起回执，回局交差";action="receipt";
                }
                bool wrongPerson=(t.stage=="deliver"||t.stage=="receipt")&&tradePerson!=t.recipient;
                if(wrongPerson)body="这不是刚才核认的收件人。\n\n请回到"+address+"，找刚才那位陈阿福"+(t.stage=="deliver"?"当面交批。":"领取回执。");
                Label(new Rect(x,y+5,w,320),body,29,ink,true);
                bool previous=GUI.enabled;GUI.enabled=previous&&!wrongPerson;
                if(LetterButton(new Rect(x,y+361,w,69),button,true,27))TradeAction(action,tradePerson<0?0:tradePerson);
                GUI.enabled=previous;
            }
            if(!string.IsNullOrEmpty(tradeNotice))Label(new Rect(x,r.yMax-107,w,87),tradeNotice,23,red);
        }
    }
}
