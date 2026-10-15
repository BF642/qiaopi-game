using System;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool firstDayPanel;
        Vector2 firstDayScroll;
        bool FirstDayQA=>Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-firstday-test")>=0;
        bool InFirstDay=>FirstDayChapter.IsActive(state);
        string DayStage=>InFirstDay?state.firstDay.stage:"";
        string DayNpcId=>DayStage=="reply"?"clerk":"xusheng";
        bool DayAtWork=>DayStage=="work"||DayStage=="encounter";

        void TickFirstDay()
        {
            if(!InFirstDay)return;
            if(!puzzle&&!carrying&&!sideCarrying&&FirstDayChapter.TriggerEncounter(state,progress)) {
                walkRoute.Clear();Toast("许生在附近招呼你：接下来的活，得先商量一下。");Save();
            }
        }

        Vector3 FirstDayTarget()
        {
            switch(DayStage) {
                case "encounter":return FirstDayContact;
                case "letter":return writingPlace==null?mission.npcPosition:writingPlace.approach;
                case "bed":return FirstDayBedPoint;
                default:return mission.npcPosition;
            }
        }
        bool FirstDayCanReach(Vector3 point,float range=2.5f)
        {
            return Dist(point)<range&&!Physics.Linecast(player.transform.position+Vector3.up*.9f,
                point+Vector3.up*.9f,~((1<<2)|(1<<29)|(1<<30)),QueryTriggerInteraction.Ignore);
        }
        string FirstDayPrompt()
        {
            if(!InFirstDay)return null;
            if(DayStage=="encounter")return FirstDayCanReach(FirstDayContact)?"E  听许生说这桩事":"";
            if(DayStage=="work")return Complete&&!string.IsNullOrEmpty(state.firstDay.choice)&&FirstDayCanReach(mission.npcPosition)?"E  结清今日工钱":null;
            if(DayStage=="letter"&&NearWritingDesk)return "E  坐下写今天的侨批";
            if(DayStage=="bed")return FirstDayCanReach(FirstDayBedPoint,1.8f)?"E  躺下歇息，过到明早":"";
            if(FirstDayCanReach(mission.npcPosition))return DayStage=="reply"?"E  接过泉州回批":DayStage=="morning"?"E  问问今日的门路":"E  和许生聊聊";
            return DayStage=="letter"?null:"";
        }
        bool HandleFirstDayInteract()
        {
            if(!InFirstDay)return false;
            if(DayStage=="work") {
                if(!Complete||string.IsNullOrEmpty(state.firstDay.choice)||carrying||sideCarrying)return false;
                if(!FirstDayCanReach(mission.npcPosition))return false;
                if(FirstDayChapter.SettleWork(state)) {
                    firstDayPanel=dialogue=lifePanel=false;EnterNode();Save();
                    Toast("工钱已结清。傍晚回到住处，许生等你一起吃饭。");
                }
                return true;
            }
            if(DayStage=="encounter") {
                if(FirstDayCanReach(FirstDayContact))OpenFirstDayPanel();
                else Toast("先到许生身边，商量接下来这份差事。");
                return true;
            }
            if(DayStage=="bed") {
                if(FirstDayCanReach(FirstDayBedPoint,1.8f)&&FirstDayChapter.Sleep(state)) {
                    firstDayPanel=false;EnterNode();fade=.9f;Save();Toast("翌晨 · 天亮了，去听听许生带来的消息。");
                }
                return true;
            }
            if(DayStage=="reply") {
                if(FirstDayCanReach(mission.npcPosition)&&FirstDayChapter.CollectReply(state)) {
                    firstDayPanel=false;EnterNode();lifePanel=false;journal=true;journalScroll=Vector2.zero;Save();
                    Toast("数周后，泉州回批到了。读完可在「安排生活」选择下一份活。");
                }
                return true;
            }
            if(FirstDayCanReach(mission.npcPosition)){OpenFirstDayPanel();return true;}
            return DayStage!="letter";
        }
        void OpenFirstDayPanel()
        {
            if(!InFirstDay)return;
            firstDayPanel=true;dialogue=lifePanel=journal=map=help=gallery=false;
            firstDayScroll=Vector2.zero;walkRoute.Clear();voiceSource.Stop();ShowPortrait(DayNpcId);
            var person=DayStage=="encounter"?firstDayCompanion:npc;
            if(person){
                Vector3 direction=player.transform.position-person.transform.position;direction.y=0;
                if(direction.sqrMagnitude>.01f)person.transform.rotation=Quaternion.LookRotation(direction);
                float height=Mathf.Max(1.5f,ModelLibrary.CharacterHeight(CharacterRoster.Get(DayNpcId).model));
                Vector3 gaze=person.transform.position+Vector3.up*(height*.90f)-CameraEyePoint();
                if(gaze.sqrMagnitude>.01f){cameraYaw=Mathf.Atan2(gaze.x,gaze.z)*Mathf.Rad2Deg;cameraPitch=-Mathf.Atan2(gaze.y,new Vector2(gaze.x,gaze.z).magnitude)*Mathf.Rad2Deg;UpdateCamera(true);}
            }
            CancelCameraPointer();ResetMobileControls();
        }
        void SelectFirstDayWork(string id)
        {
            if(!FirstDayChapter.ChooseWork(state,id))return;
            firstDayPanel=false;EnterNode();Save();
            Toast("这份差事记下了。亲手办完，再去结工钱。");
        }
        void ChooseFirstDayEvening(string id)
        {
            if(!FirstDayChapter.TryEvening(state,id,out string notice)){Toast(notice);return;}
            firstDayPanel=false;EnterNode();Save();Toast(notice);
        }
        void AfterFirstDayLetterSent()
        {
            if(!InFirstDay||DayStage!="bed")return;
            if(letterEditor)ClosePersonalLetter();firstDayPanel=journal=lifePanel=false;
            EnterNode();Save();Toast("批已托寄，回音还需时日。到通铺院床边睡一觉。");
        }
        void DeferFirstDayLetter()
        {
            if(!FirstDayChapter.DeferLetter(state))return;
            firstDayPanel=false;EnterNode();Save();Toast("草稿留着，今晚先歇。到通铺院床边睡下。");
        }
        void FinishFirstDayMorning()
        {
            string result=FirstDayChapter.AdvanceMorning(state);if(string.IsNullOrEmpty(result))return;
            firstDayPanel=false;EnterNode();Save();
            if(DayStage=="reply"){fade=.8f;Toast("数周后 · 日子照常往前走，今天送回批的伙计来了。");}
            else Toast("这段初来乍到的日子记下了。接什么活、何时写批，由你安排。");
        }
        string FirstDayObjective()
        {
            switch(DayStage) {
                case "encounter":return "先和许生商量眼前的事";
                case "evening":return "回住处，与许生吃顿晚饭";
                case "letter":return "坐到桌前，写一封自己的侨批";
                case "bed":return "回通铺院，到床边睡下";
                case "morning":return "问问许生，昨日的事有了什么回音";
                case "reply":return "到后院门口，接泉州回批";
                case "work":
                    if(string.IsNullOrEmpty(state.firstDay.choice))return state.firstDay.route=="shop"?IndoorInspectionObjective():"先做完手头这一趟";
                    if(Complete)return "找"+mission.npcName+"结清今日工钱";
                    if(mission.activity=="inspect")return IndoorInspectionObjective();
                    return (carrying?"送交":"领取")+mission.itemName;
                default:return "";
            }
        }
        void DrawFirstDayEntry()
        {
            if(!InFirstDay)return;
            if(DayStage=="letter") {
                if(HudAction(LifeEntryRect(),"今晚 · 写批与歇息",true,MobileControls?27:21))OpenFirstDayPanel();
            } else if(DayStage=="bed"||DayStage=="morning"||DayStage=="evening"||DayStage=="reply"||DayStage=="encounter") {
                string label=DayStage=="bed"?"认路 · 通铺院床边":DayStage=="reply"?"认路 · 取回批":"认路 · 找许生";
                if(HudAction(LifeEntryRect(),label,false,MobileControls?27:21)) {
                    walkRoute=WalkPath.Find(player.transform.position,FirstDayTarget(),loadedWorld);routeIndex=0;
                    if(walkRoute.Count==0)Toast("先回到附近的石板路，再试试认路。");
                }
            }
        }
        string FirstDaySituation()
        {
            if(DayStage=="encounter") {
                if(state.firstDay.route=="dock")return "许生压低声音：老搬工伤了脚，还有一包货没交上。工头又催一趟加急货，另有两张货单对不上。你刚搬完第一趟，接下来想挣现钱、分担一趟，还是去核账？说定了，还得亲手把活做完。";
                if(state.firstDay.route=="shop")return "许生把一张提货单递过来：差的一袋米得去后院查。掌柜愿教你理账，也可以让你守晚班多领现钱。门外那位同乡还等着人念家信。第一张单你已经核过，余下这半日，想怎样过？";
                return "许生接过你刚送来的批包：何师傅发现另一封批的门牌没有写全。你可以留下复核，也可以先送地址齐全的件，或跟师傅练抄款额、记经手日期。每封批后头都有人等，得先说清哪件事由你办。";
            }
            if(DayStage=="evening")return "许生把凳子往旁边挪了挪。\n"+state.firstDay.outcome+"\n忙了一天，今晚先怎样照顾自己？饭后，后院写批间的灯还亮着。";
            if(DayStage=="letter")return state.personalLettersSent>=PersonalLetters.MaxLetters?"这一程三封自写批已寄完。原来的信与回批都在匣里，你仍可坐下来写草稿，或先到床边歇息。":"许生说：把今天写给家里吧。写给母亲、妻子还是阿满，附不附银，都由你。难处也能写；若暂时不想寄，就把草稿留到明日。";
            if(DayStage=="morning")return FirstDayChapter.MorningPreview(state);
            return "";
        }
        void DrawFirstDayPanel()
        {
            if(!InFirstDay){firstDayPanel=false;return;}
            bool mobile=MobileControls;Rect safe=mobile?MobileUiBounds():new Rect(0,0,W,H);
            Rect r=new Rect(safe.xMin+32,342,safe.width-64,624);PaperPanel(r,true,.94f);
            Box(new Rect(r.x,r.y,r.width,3),letterSeal);
            Label(new Rect(r.x+26,r.y+19,r.width-210,35),FirstDayTimeLabel+" · "+LifeJourney.DestinationName(state),mobile?24:21,sub);
            if(LetterButton(new Rect(r.xMax-147,r.y+17,120,45),"收起",false,22)){firstDayPanel=false;return;}
            var portrait=new Rect(r.x+25,r.y+78,205,280);Box(portrait,C("D4CBB6"));
            if(portraitTexture)GUI.DrawTexture(portrait,portraitTexture,ScaleMode.ScaleToFit,false);
            Label(new Rect(portrait.x,portrait.yMax+18,portrait.width,46),"许生",30,ink,true,TextAnchor.MiddleCenter);
            Label(new Rect(portrait.x,portrait.yMax+69,portrait.width,90),"同乡 · 在外讨生活",mobile?23:20,sub,false,TextAnchor.MiddleCenter);
            float x=r.x+263,w=r.width-291;int fs=mobile?29:26;
            Rect prose=new Rect(x,r.y+80,w,183);string body=FirstDaySituation();float h=Height(body,fs,w-25,true);
            GalleryTouchScroll(prose,ref firstDayScroll,h);
            firstDayScroll=GUI.BeginScrollView(prose,firstDayScroll,new Rect(0,0,w-25,Mathf.Max(183,h+6)),false,false);
            Label(new Rect(0,0,w-25,h+6),body,fs,ink,true);GUI.EndScrollView();
            float y=r.y+287;
            if(DayStage=="encounter") {
                foreach(var c in FirstDayChapter.WorkChoices(state)) {
                    if(FirstDayChoice(new Rect(x,y,w,94),c.title,c.enabled?c.hint:c.unavailableReason,c.enabled)){SelectFirstDayWork(c.id);return;}y+=102;
                }
            } else if(DayStage=="evening") {
                if(FirstDayChoice(new Rect(x,y,w,94),"吃一顿热饭","盘缠 −2 · 身体 +8",state.money>=2)){ChooseFirstDayEvening("meal");return;}
                if(FirstDayChoice(new Rect(x,y+102,w,94),"和许生围坐吃饭","盘缠 −3 · 身体 +6 · 信用 +1",state.money>=3)){ChooseFirstDayEvening("together");return;}
                if(FirstDayChoice(new Rect(x,y+204,w,94),"留着钱，先喝水歇肩","不花盘缠 · 身体 +4",true)){ChooseFirstDayEvening("sleep");return;}
            } else if(DayStage=="letter") {
                if(FirstDayChoice(new Rect(x,y,w,104),"去写批间坐下","亲手写信、选择心意与附银。回批需要等数周。",true)){firstDayPanel=false;OpenPersonalLetter();return;}
                if(FirstDayChoice(new Rect(x,y+120,w,104),"今晚先留着草稿","不扣盘缠，草稿保留。先到床边睡一觉。",true)){DeferFirstDayLetter();return;}
            } else if(DayStage=="morning") {
                if(FirstDayChoice(new Rect(x,y,w,110),"记下这份照应，继续过日子",state.firstDay.letterSent?"时日推移 · 去看数周后到来的泉州回批":"开启自主谋生；草稿随时可以接着写。",true)){FinishFirstDayMorning();return;}
            }
        }
        bool FirstDayChoice(Rect r,string title,string hint,bool enabled)
        {
            bool old=GUI.enabled;GUI.enabled=old&&enabled;
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            Box(r,new Color(.82f,.75f,.62f,hover?.32f:.15f));Box(new Rect(r.x,r.y,3,r.height),enabled?letterSeal:sub);
            Label(new Rect(r.x+18,r.y+10,r.width-36,41),title,MobileControls?29:26,enabled?ink:sub,true);
            Label(new Rect(r.x+18,r.y+54,r.width-36,r.height-54),hint,MobileControls?24:21,sub);
            bool clicked=GUI.Button(r,GUIContent.none,blank);GUI.enabled=old;return clicked;
        }
        InspectionCase InspectionEntry()
        {
            if(InNanyang&&mission.activity=="inspect")return NanyangTasks.Inspection(state,puzzleIndex);
            return InFirstDay&&!string.IsNullOrEmpty(state.firstDay.choice)?FirstDayTasks.Inspection(state,puzzleIndex):InspectionCases.Get(TaskNode,puzzleIndex);
        }
    }
}
