using System;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool nanyangPanel;
        string nanyangPanelMode="";
        int nanyangPanelStep=-1;
        Vector2 nanyangScroll;
        GameObject nanyangNeighbour;
        bool NanyangQA=>Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-nanyang-test")>=0;
        bool InNanyang=>NanyangTasks.Applies(state);
        bool NanyangIncident=>InNanyang&&NanyangLife.IsBlocking(state);
        bool NanyangRest=>InNanyang&&state.journey.pendingJob=="rest";
        string NanyangStage=>NanyangIncident?state.nanyang.stage:"";
        string NanyangTime=>NanyangStage=="reply"?"1906年冬 · 数周后":NanyangStage=="letter"||NanyangStage=="waiting"?"1906年冬 · 灯下":"1906年冬 · 谋生日常";

        void SetupNanyangPeople()
        {
            nanyangNeighbour=null;
            if(!InNanyang||NanyangIncident||!actors)return;
            if(NanyangTasks.NpcId(state)=="xusheng"||CharacterRoster.ForNode(TaskNode)=="xusheng"&&loadedWorld=="quarters")return;
            if(!NanyangPlaces.TryGet(loadedWorld,"colleague",out var spot))return;
            nanyangNeighbour=ModelLibrary.Spawn("Xusheng",actors.transform,WorldFactory.CharacterGroundPosition(spot.position));
            if(nanyangNeighbour){nanyangNeighbour.name="许生 · 熟悉的同乡";nanyangNeighbour.AddComponent<CharacterIdle>();nanyangNeighbour.transform.rotation=Quaternion.Euler(0,180,0);}
        }
        Vector3 NanyangTarget()
        {
            if(NanyangRest)return NanyangPlaces.Get("quarters","rest").position;
            if(NanyangStage=="letter"&&writingPlace!=null)return writingPlace.approach;
            if(NanyangStage=="tasks"&&mission.activity=="inspect")return inspectionPositions[0];
            return mission.npcPosition;
        }
        string NanyangPrompt()
        {
            if(!InNanyang)return null;
            if(NanyangRest)return FirstDayCanReach(NanyangTarget(),1.85f)?"E  到床边歇一日":"";
            if(NanyangIncident) {
                if(NanyangStage=="letter"&&NearWritingDesk)return "E  坐下写这段经历";
                if(NanyangStage=="tasks"&&mission.activity=="inspect")return null;
                if(FirstDayCanReach(mission.npcPosition))return NanyangStage=="reply"?"E  接过泉州家人的回批":NanyangStage=="waiting"?"E  说说接下来几周的日子":"E  与"+mission.npcName+"商量";
                return NanyangStage=="letter"?null:"";
            }
            if(carrying||sideCarrying)return null;
            if(string.IsNullOrEmpty(state.journey.pendingJob)&&loadedWorld=="quarters"&&NanyangPlaces.TryGet(loadedWorld,"meal",out var meal)&&FirstDayCanReach(meal.position,1.8f))return "E  饭摊 · 吃碗热饭";
            if(loadedWorld=="quarters"&&NanyangPlaces.TryGet(loadedWorld,"homeletter",out var clip)&&FirstDayCanReach(clip.position,1.5f))return "E  翻看床头的家信";
            if(string.IsNullOrEmpty(state.journey.pendingJob)&&loadedWorld=="quarters"&&FirstDayCanReach(mission.npcPosition))return "E  与许生聊聊近况";
            if(string.IsNullOrEmpty(state.journey.pendingJob)&&nanyangNeighbour&&FirstDayCanReach(nanyangNeighbour.transform.position))return "E  与许生聊聊近况";
            return null;
        }
        bool HandleNanyangInteract()
        {
            if(!InNanyang)return false;
            if(NanyangRest) {
                if(FirstDayCanReach(NanyangTarget(),1.85f)&&LifeJourney.CommitAction(state,"rest",out string rested)){
                    EnterNode();lifePanel=false;fade=.85f;Save();Toast(rested);
                }
                return true;
            }
            if(NanyangIncident) {
                if(NanyangStage=="tasks"&&mission.activity=="inspect")return false;
                if(NanyangStage=="reply") {
                    if(FirstDayCanReach(mission.npcPosition)&&NanyangLife.CollectReply(state)){
                        EnterNode();lifePanel=false;journal=true;journalScroll=Vector2.zero;Save();Toast("泉州回批收入匣中。这段经历会留在家书和往后的谋生里。");
                    }
                    return true;
                }
                if(FirstDayCanReach(mission.npcPosition)){OpenNanyangPanel(NanyangStage);return true;}
                return NanyangStage!="letter";
            }
            if(carrying||sideCarrying)return false;
            if(string.IsNullOrEmpty(state.journey.pendingJob)&&loadedWorld=="quarters"&&NanyangPlaces.TryGet(loadedWorld,"meal",out var meal)&&FirstDayCanReach(meal.position,1.8f)){
                OpenNanyangPanel("meal");return true;
            }
            if(loadedWorld=="quarters"&&NanyangPlaces.TryGet(loadedWorld,"homeletter",out var clip)&&FirstDayCanReach(clip.position,1.5f)){
                if(state.letters.Count>0){journal=true;journalScroll=Vector2.zero;walkRoute.Clear();}else Toast("家信夹还空着。后院有写批桌，可以先写几句给家里。");return true;
            }
            if(string.IsNullOrEmpty(state.journey.pendingJob)&&loadedWorld=="quarters"&&FirstDayCanReach(mission.npcPosition)){OpenNanyangPanel("memory");return true;}
            if(string.IsNullOrEmpty(state.journey.pendingJob)&&nanyangNeighbour&&FirstDayCanReach(nanyangNeighbour.transform.position)){OpenNanyangPanel("memory");return true;}
            return false;
        }
        void OpenNanyangPanel(string mode)
        {
            if(!InNanyang)return;
            nanyangPanel=true;nanyangPanelMode=mode;nanyangPanelStep=state.nanyang.taskProgress;nanyangScroll=Vector2.zero;
            firstDayPanel=dialogue=lifePanel=journal=map=help=gallery=false;walkRoute.Clear();voiceSource.Stop();
            string id=mode=="memory"||mode=="meal"?"xusheng":NanyangTasks.NpcId(state)??"xusheng";
            ShowPortrait(id);
            var person=mode=="memory"&&nanyangNeighbour?nanyangNeighbour:npc;
            if(mode!="meal"&&person){
                Vector3 direction=player.transform.position-person.transform.position;direction.y=0;
                if(direction.sqrMagnitude>.01f)person.transform.rotation=Quaternion.LookRotation(direction);
                float height=Mathf.Max(1.5f,ModelLibrary.CharacterHeight(CharacterRoster.Get(id).model));
                Vector3 gaze=person.transform.position+Vector3.up*(height*.9f)-CameraEyePoint();
                cameraYaw=Mathf.Atan2(gaze.x,gaze.z)*Mathf.Rad2Deg;cameraPitch=-Mathf.Atan2(gaze.y,new Vector2(gaze.x,gaze.z).magnitude)*Mathf.Rad2Deg;UpdateCamera(true);
            }
            CancelCameraPointer();ResetMobileControls();
        }
        void ChooseNanyangResponse(string id)
        {
            if(!nanyangPanel||nanyangPanelMode!="choice"||!FirstDayCanReach(mission.npcPosition))return;
            if(!NanyangLife.ChooseResponse(state,id))return;
            nanyangPanel=false;EnterNode();lifePanel=false;Save();Toast("先"+NanyangTasks.Step(state).title+"。这些事办完以后，才会结算结果。");
        }
        void FinishNanyangStep(bool inspectedEvidence=false)
        {
            if(!NanyangIncident||NanyangStage!="tasks")return;
            var step=NanyangTasks.Step(state);
            if(step.inspect){if(!inspectedEvidence||!puzzle||puzzleIndex!=0||!FirstDayCanReach(step.point,2.2f))return;}
            else if(!nanyangPanel||nanyangPanelMode!="tasks"||nanyangPanelStep!=state.nanyang.taskProgress||!FirstDayCanReach(step.person))return;
            int index=state.nanyang.taskProgress;
            if(!NanyangLife.CompleteStep(state,index,out string notice))return;
            nanyangPanel=puzzle=false;EnterNode();lifePanel=false;Save();Toast(notice);
        }
        void OpenNanyangLetter()
        {
            if(NanyangStage!="letter")return;
            var draft=PersonalLetters.EnsureDraft(state);
            // Preserve an existing player's draft and their chosen recipient.
            if(string.IsNullOrWhiteSpace(draft.body))draft.recipient="wife";
            nanyangPanel=false;OpenPersonalLetter();
        }
        void AfterNanyangLetterSent()
        {
            if(!NanyangIncident||NanyangStage!="waiting")return;
            if(letterEditor)ClosePersonalLetter();nanyangPanel=journal=lifePanel=false;
            EnterNode();lifePanel=false;Save();Toast("信已托寄。跨海还需时日，到许生身边说说之后的打算。");
        }
        void DeferNanyangLetter()
        {
            if(!NanyangLife.DeferLetter(state))return;
            nanyangPanel=false;EnterNode();Save();Toast("草稿留下，继续安排生活。往后的回批仍会记得这段经历。");
        }
        void AdvanceNanyangWeeks()
        {
            if(!NanyangLife.AdvanceWeeks(state))return;
            nanyangPanel=false;EnterNode();lifePanel=false;fade=.9f;Save();Toast("数周后 · 送到泉州的信有了回音。去批局柜台接回批。");
        }
        void VisitNanyangPlace(string worldId)
        {
            if(!NanyangLife.Visit(state,worldId))return;
            nanyangPanel=false;EnterNode();lifePanel=false;Save();Toast("到了"+region.title+"。可以走走、找熟人，或安排下一份工作。");
        }
        string NanyangObjective()
        {
            if(NanyangRest)return "回通铺床边，留一日养身";
            switch(NanyangStage){
                case "choice":return "四份工钱被扣，先问清这笔账";
                case "tasks":return NanyangTasks.Step(state).title;
                case "letter":return "把这一段日子写给泉州的家人";
                case "waiting":return "找许生说说打算，等候跨海回音";
                case "reply":return "去信局柜台，接过泉州回批";
                default:return "";
            }
        }
        void DrawNanyangEntry()
        {
            if(NanyangStage=="letter"){
                if(HudAction(LifeEntryRect(),"今夜 · 写批或存稿",true,MobileControls?27:21))OpenNanyangPanel("letter");return;
            }
            if(HudAction(LifeEntryRect(),NanyangRest?"认路 · 回通铺床边":"认路 · "+(NanyangStage=="tasks"&&mission.activity=="inspect"?"找凭据":mission.npcName),false,MobileControls?27:21)){
                walkRoute=WalkPath.Find(player.transform.position,NanyangTarget(),loadedWorld);routeIndex=0;
                if(walkRoute.Count==0)Toast("先回到附近的石板路，再试着认路。");
            }
        }
        string NanyangMemory()
        {
            string text="许生认得你，也记得你刚来时问过的路。";
            if(state.firstDayVersion==1&&state.firstDay!=null&&!string.IsNullOrEmpty(state.firstDay.morningFeedback))text=state.firstDay.morningFeedback;
            if(state.nanyang.stage=="complete")text+="\n"+WorkRelationshipText.WageMemory(state);
            int count=0;foreach(string route in new[]{"dock","shop","courier"})if(NanyangLife.WorkCount(state,route)>0){count++;text+="\n你在"+NanyangLife.CareerName(state,route)+"这条路上已经有了"+NanyangLife.WorkCount(state,route)+"轮经历。"+(NanyangLife.WorkCount(state,route)<3?"下一趟能承担新的事情。":"这一行的入门与进阶都已经走过，可以试试别的安排。");}
            if(count==0)text+="\n先试一份工作也好，工钱和结账的日子要问在前头。饭摊就在灶间边，晚些也可以坐下来写信。";
            string relationships=WorkRelationshipText.Summary(state);
            if(!string.IsNullOrEmpty(relationships))text+="\n"+relationships;
            return text;
        }
        string NanyangPanelBody()
        {
            var n=state.nanyang;
            switch(nanyangPanelMode){
                case "choice":return (LifeJourney.DestinationId(state)=="semarang"?"三宝垄的结账人借口货物受潮，把损耗摊到新来的雇工身上。许生记得卸货时的情形，你可以找他作证。\n":"")+"活已经做完，结账处却把四份划作没有事先约定的押钱。\n议定工钱 "+n.promisedWage+" 份 · 实领 "+n.paidWage+" 份 · 尚欠 "+n.owed+" 份。熟人介绍的额外报酬另算。\n先忍下、留证追问、找同乡或换工，都要亲自办妥。没有追回的工钱会一直留在记录里。";
                case "tasks":return NanyangTasks.Step(state).body;
                case "letter":return n.outcome+"\n回到住处，灯下有一页空纸。写给谁、附多少银、要不要说出难处，都由你。选如实相告，家人才会回应这段经历；只报平安，就先把难处留在自己心里。";
                case "waiting":return "许生说：先把住处和饭钱安排好，接下来的日子慢慢过。去往泉州的信要经过海路，家人读到后再回批，也需要时日。\n继续后，故事会来到数周后的批局。这里没有需要盯着等的倒计时。";
                case "meal":return state.nanyang.lastMealTurn==state.journey.turns?"这一轮已经吃过热饭。锅里还温着汤，歇一会儿，再想下一份安排。":"灶边端来一碗热饭和汤。两份盘缠，补八点体力，不占谋生轮次。每一轮生活安排可以吃一次，先给自己留够饭钱。";
                default:return NanyangMemory();
            }
        }
        void DrawNanyangPanel()
        {
            if(!InNanyang){nanyangPanel=false;return;}
            bool mobile=MobileControls;Rect safe=mobile?MobileUiBounds():new Rect(0,0,W,H);
            Rect r=new Rect(safe.xMin+32,275,safe.width-64,690);PaperPanel(r,true,.96f);Box(new Rect(r.x,r.y,r.width,3),letterSeal);
            string title=nanyangPanelMode=="choice"?"欠下的四份工钱":nanyangPanelMode=="tasks"?NanyangTasks.Step(state).title:nanyangPanelMode=="meal"?"灶边一碗热饭":nanyangPanelMode=="memory"?"街巷里的熟人":"灯下与家书";
            Label(new Rect(r.x+28,r.y+21,r.width-215,44),title,mobile?31:30,ink,true);
            if(LetterButton(new Rect(r.xMax-145,r.y+18,116,49),"收起",false,23)){nanyangPanel=false;return;}
            Rect portrait=new Rect(r.x+28,r.y+92,200,272);Box(portrait,C("D4CBB6"));
            if(nanyangPanelMode=="meal"){Label(new Rect(portrait.x+18,portrait.y+45,portrait.width-36,155),"热饭\n温汤",42,ink,true,TextAnchor.MiddleCenter);Box(new Rect(portrait.x+55,portrait.y+224,portrait.width-110,2),letterSeal);}
            else if(portraitTexture)GUI.DrawTexture(portrait,portraitTexture,ScaleMode.ScaleToFit,false);
            string speaker=nanyangPanelMode=="meal"?"住处饭摊":nanyangPanelMode=="memory"?"许生":mission.npcName;
            Label(new Rect(portrait.x,portrait.yMax+15,portrait.width,58),speaker,29,ink,true,TextAnchor.MiddleCenter);
            Label(new Rect(portrait.x,portrait.yMax+87,portrait.width,105),LifeJourney.DestinationName(state)+"\n"+NanyangTime.Replace(" · ","\n"),mobile?22:20,sub,false,TextAnchor.MiddleCenter);
            float x=r.x+262,w=r.width-290;Rect prose=new Rect(x,r.y+91,w,222);string body=NanyangPanelBody();int fs=mobile?29:26;float h=Height(body,fs,w-26,true);
            GalleryTouchScroll(prose,ref nanyangScroll,h);nanyangScroll=GUI.BeginScrollView(prose,nanyangScroll,new Rect(0,0,w-26,Mathf.Max(222,h+6)),false,false);
            Label(new Rect(0,0,w-26,h+6),body,fs,ink,true);GUI.EndScrollView();float y=r.y+330;
            if(nanyangPanelMode=="choice"){
                var choices=NanyangLife.Choices(state);float cw=(w-16)/2;
                for(int i=0;i<choices.Count;i++){var c=choices[i];if(FirstDayChoice(new Rect(x+(i%2)*(cw+16),y+(i/2)*164,cw,152),c.title,c.description,c.enabled)){ChooseNanyangResponse(c.id);return;}}
            }else if(nanyangPanelMode=="tasks"){
                if(FirstDayChoice(new Rect(x,y,w,108),NanyangTasks.Step(state).confirm,"办完当前这一步，再前往下一处。",true))FinishNanyangStep();
            }else if(nanyangPanelMode=="letter"){
                if(FirstDayChoice(new Rect(x,y,w,108),"去桌前写给家里","原文自己写；回批会依据经历、心意与附银回应。",true)){OpenNanyangLetter();return;}
                if(FirstDayChoice(new Rect(x,y+125,w,108),"这一页先留作草稿","不扣款，原稿保留，继续安排生活。",true))DeferNanyangLetter();
            }else if(nanyangPanelMode=="waiting"){
                if(FirstDayChoice(new Rect(x,y,w,108),"日子向前，去看数周后的回音","明确跳过海路往返的等待时间。",true))AdvanceNanyangWeeks();
            }else if(nanyangPanelMode=="meal"){
                bool can=state.money>=2&&state.nanyang.lastMealTurn!=state.journey.turns;
                if(FirstDayChoice(new Rect(x,y,w,108),"吃一碗热饭",can?"盘缠 −2 · 身体 +8":state.money<2?"盘缠不足两份，可以安排一次免费养身。":"这一轮已经吃过了。",can)&&NanyangLife.TryMeal(state,out string notice)){nanyangPanel=false;Save();Toast(notice);}
            }else if(FirstDayChoice(new Rect(x,y,w,108),"记下这些话，接着走走","熟人的话会随首日选择、欠薪应对和工作经历变化。",true))nanyangPanel=false;
        }
        void NanyangWorldLabels()
        {
            if(!InNanyang)return;
            if(nanyangNeighbour)Tag(nanyangNeighbour.transform.position+Vector3.up*1.95f,"许生 · 可聊近况",false);
            if(loadedWorld=="quarters"){
                if(NanyangPlaces.TryGet(loadedWorld,"meal",out var meal)&&Dist(meal.position)<9)Tag(meal.position+Vector3.up*1.5f,"饭摊 · 热饭与汤",false);
                if(NanyangPlaces.TryGet(loadedWorld,"homeletter",out var clip)&&Dist(clip.position)<5)Tag(clip.facePoint+Vector3.up*.3f,"床头 · 家信夹",false);
                if(NanyangRest)Tag(NanyangTarget()+Vector3.up*1.3f,"通铺 · 留一日养身",true);
            }
        }
    }
}
