using System;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool inquiryPanel;
        string inquiryPanelMode = "", inquiryNotice = "";
        int inquiryPage;
        GameObject inquiryHelper;
        bool InquiryQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-inquiry-test") >= 0;
        bool InInquiryScene => Investigation.IsEnrolled(state) && Investigation.IsTraceNode(state.nodeId) && state.inquiry.route == state.nodeId;
        int InquiryReadCount {
            get { int n=0;if(InInquiryScene)for(int i=0;i<3;i++)if((state.inquiry.evidenceMask&(1<<i))!=0)n++;return n; }
        }
        Vector3 InquiryTarget()
        {
            for(int i=0;i<3;i++)if((state.inquiry.evidenceMask&(1<<i))==0)return InquiryPlaces.Evidence(state.nodeId,i);
            return mission.npcPosition;
        }
        string InquiryObjective()
        {
            if(Investigation.IsComplete(state))return "与"+mission.npcName+"商量如何处理";
            for(int i=0;i<3;i++)if((state.inquiry.evidenceMask&(1<<i))==0)return "到"+InquiryPlaces.Title(state.nodeId,i)+"查问";
            return "回到核认处，对上款额、日期和地址";
        }
        string InquiryPrompt()
        {
            if(!InInquiryScene)return null;
            if(Investigation.IsComplete(state)&&FirstDayCanReach(mission.npcPosition,2.4f))return null;
            if(InquiryHelperNearest())return "E  向旧识问问这笔批";
            for(int i=0;i<3;i++)if(FirstDayCanReach(InquiryPlaces.Evidence(state.nodeId,i),2.1f))return "E  查看"+Investigation.Evidence(state,i).title;
            if(inquiryHelper&&FirstDayCanReach(inquiryHelper.transform.position,2.3f))return "E  向旧识问问这笔批";
            if(FirstDayCanReach(mission.npcPosition,2.4f))return "E  摊开查批簿，逐项核认";
            return null;
        }
        bool HandleInquiryInteract()
        {
            if(!InInquiryScene)return false;
            if(inquiryPanel)return true;
            if(Investigation.IsComplete(state)&&FirstDayCanReach(mission.npcPosition,2.4f))return false;
            if(InquiryHelperNearest()){OpenInquiryPanel("help");AimInquiryPerson(inquiryHelper);return true;}
            for(int i=0;i<3;i++)if(FirstDayCanReach(InquiryPlaces.Evidence(state.nodeId,i),2.1f)){
                OpenInquiryEvidence(i,true);return true;
            }
            if(inquiryHelper&&FirstDayCanReach(inquiryHelper.transform.position,2.3f)){
                OpenInquiryPanel("help");AimInquiryPerson(inquiryHelper);return true;
            }
            if(FirstDayCanReach(mission.npcPosition,2.4f)){
                OpenInquiryPanel("board");AimInquiryPerson(npc);return true;
            }
            // While investigating, unrelated tea/odd-job points may still be
            // used. The original trace conversation remains gated by Complete.
            return false;
        }
        bool InquiryHelperNearest()
        {
            if(!inquiryHelper||!FirstDayCanReach(inquiryHelper.transform.position,2.3f))return false;
            float nearest=Dist(inquiryHelper.transform.position);
            for(int i=0;i<3;i++)if(FirstDayCanReach(InquiryPlaces.Evidence(state.nodeId,i),2.1f)&&Dist(InquiryPlaces.Evidence(state.nodeId,i))<nearest)return false;
            return true;
        }
        void OpenInquiryPanel(string mode)
        {
            inquiryPanel=true;inquiryPanelMode=mode;inquiryNotice="";
            dialogue=journal=map=help=gallery=lifePanel=false;
            walkRoute.Clear();routeIndex=0;CancelCameraPointer();ResetMobileControls();
            if(voiceSource)voiceSource.Stop();
        }
        void OpenInquiryEvidence(int index,bool atSource)
        {
            if(!InInquiryScene||index<0||index>2)return;
            if(atSource) {
                if(!FirstDayCanReach(InquiryPlaces.Evidence(state.nodeId,index),2.1f))return;
                if(!Investigation.ReadEvidence(state,index,out string notice))return;
                progress=InquiryReadCount;Save();Sound();
            } else if((state.inquiry.evidenceMask&(1<<index))==0)return;
            inquiryPage=index;OpenInquiryPanel("evidence");
            if(atSource)AimInquiryAt(InquiryPlaces.DocumentPoint(state.nodeId,index));
            if(targetMarker)targetMarker.transform.position=InquiryTarget();
        }
        void ChooseInquiryValue(string field,int index)
        {
            if(!inquiryPanel||inquiryPanelMode!="board")return;
            if(Investigation.Select(state,field,index,out string notice)){inquiryNotice="";Save();}
            else inquiryNotice=notice;
        }
        void SubmitInquiry()
        {
            if(!InInquiryScene||!inquiryPanel||inquiryPanelMode!="board")return;
            if(!FirstDayCanReach(mission.npcPosition,2.4f)){inquiryNotice="把查批簿带回核认处，再同经手人逐项核实。";return;}
            if(!Investigation.Submit(state,out string notice)){inquiryNotice=notice;return;}
            progress=3;inquiryPanel=false;Save();Sound();ResetConversation();dialogue=true;
            Toast("三处记录已经对上。这笔款怎样处理，还要由你决定。");
        }
        void RequestInquiryHelp()
        {
            if(!inquiryPanel||inquiryPanelMode!="help"||!inquiryHelper||!FirstDayCanReach(inquiryHelper.transform.position,2.3f))return;
            if(Investigation.UseHelp(state,out string notice)) {
                Save();CharacterPerformance.For(inquiryHelper).Play(CharacterGesture.Point,1.1f);
                inquiryNotice=Investigation.HelpText(state);
            }else inquiryNotice=notice;
        }
        void AimInquiryPerson(GameObject person)
        {
            if(!person)return;
            Vector3 direction=player.transform.position-person.transform.position;direction.y=0;
            if(direction.sqrMagnitude>.1f)person.transform.rotation=Quaternion.LookRotation(direction);
            CharacterPerformance.For(person).LookAt(CameraEyePoint());
            AimInquiryAt(person.transform.position+Vector3.up*1.45f);
        }
        void AimInquiryAt(Vector3 target)
        {
            Vector3 direction=target-CameraEyePoint();
            cameraYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            cameraPitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            UpdateCamera(true);
        }
        void SetupInquiryArt()
        {
            inquiryHelper=null;if(!InInquiryScene)return;
            for(int i=0;i<3;i++) {
                Vector3 paperAt=InquiryPlaces.DocumentPoint(state.nodeId,i);
                var sheet=Cube("查批原件 · "+Investigation.Evidence(state,i).title,actors.transform,paperAt,new Vector3(.43f,.008f,.29f),cream);
                for(int lineIndex=0;lineIndex<6;lineIndex++)
                    Cube("原件墨迹",actors.transform,paperAt+new Vector3(-.04f+(lineIndex%2)*.018f,.006f,-.108f+lineIndex*.037f),new Vector3(lineIndex==5?.16f:.33f,.002f,.005f),teal);
                Cube("原件朱印",actors.transform,paperAt+new Vector3(.135f,.009f,-.08f),new Vector3(.065f,.003f,.055f),Mat("A54C39"));
            }
            if(Investigation.CanUseHelp(state)||state.inquiry.helpUsed) {
                string helperModel=state.inquiry.helpRoute=="dock"?"Clerk":state.inquiry.helpRoute=="shop"?"Shopkeeper":"Worker";
                Vector3 at=InquiryPlaces.Evidence(state.nodeId,0)+new Vector3(-1.45f,0,.15f);
                inquiryHelper=state.inquiry.helpRoute=="dock"
                    ?CharacterAppearanceProfiles.Spawn("dock_ashun",actors.transform,WorldFactory.CharacterGroundPosition(at))
                    :ModelLibrary.Spawn(helperModel,actors.transform,WorldFactory.CharacterGroundPosition(at));
                if(inquiryHelper){inquiryHelper.name=state.inquiry.helpRoute=="dock"?"阿顺 · 工友":state.inquiry.helpRoute=="shop"?"陈掌柜 · 旧识":"递批同工";inquiryHelper.AddComponent<CharacterIdle>();}
            }
            progress=InquiryReadCount;
        }
        void InquiryWorldLabels()
        {
            if(!InInquiryScene)return;
            for(int i=0;i<3;i++)if(Dist(InquiryPlaces.Evidence(state.nodeId,i))<16)
                Tag(InquiryPlaces.DocumentPoint(state.nodeId,i)+Vector3.up*.45f,Investigation.Evidence(state,i).title,(state.inquiry.evidenceMask&(1<<i))==0);
            if(inquiryHelper&&Dist(inquiryHelper.transform.position)<9)Tag(inquiryHelper.transform.position+Vector3.up*1.95f,inquiryHelper.name,false);
            Tag(mission.npcPosition+Vector3.up*2.1f,Investigation.IsComplete(state)?mission.npcName:"核认处 · "+mission.npcName,InquiryReadCount==3);
        }
        Rect InquiryEntryRect() { Rect safe=MobileControls?MobileUiBounds():new Rect(0,0,W,H);return new Rect(safe.xMax-330,safe.yMin+186,300,52); }
        void DrawInquiryEntry()
        {
            if(!InInquiryScene||Blocked)return;
            Rect safe=MobileControls?MobileUiBounds():new Rect(0,0,W,H);
            Rect r=InquiryEntryRect();
            if(HudAction(r,"查批簿 · "+InquiryReadCount+" / 3",false,MobileControls?25:21))OpenInquiryPanel("board");
        }
        void DrawInquiryPanel()
        {
            if(!InInquiryScene){inquiryPanel=false;return;}
            bool mobile=MobileControls;Rect safe=mobile?MobileUiBounds():new Rect(0,0,W,H);
            bool board=inquiryPanelMode=="board";
            float height=board?650:365;
            Rect r=new Rect(safe.x+32,safe.yMax-height-28,safe.width-64,height);
            PaperPanel(r,true,.94f);Box(new Rect(r.x,r.y,r.width,3),letterSeal);
            string title=board?"查批簿":inquiryPanelMode=="help"?inquiryHelper?inquiryHelper.name:"旧识的一句话":Investigation.Evidence(state,inquiryPage).title;
            Label(new Rect(r.x+30,r.y+22,r.width-190,45),title,mobile?31:29,ink,true);
            if(LetterButton(new Rect(r.xMax-135,r.y+16,105,46),"收起",false,22)){inquiryPanel=false;return;}
            if(board)DrawInquiryBoard(r,mobile);
            else if(inquiryPanelMode=="help") {
                string words=state.inquiry.helpUsed?Investigation.HelpText(state):"你认出了曾经一起办事的人。把正在核对的那笔款说清，也许他还留着那一段经手的记忆。";
                Label(new Rect(r.x+30,r.y+87,r.width-60,147),words,mobile?29:26,ink,true);
                if(!string.IsNullOrEmpty(inquiryNotice))Label(new Rect(r.x+30,r.y+238,r.width-430,75),inquiryNotice,22,sub);
                bool enabled=GUI.enabled;GUI.enabled=enabled&&!state.inquiry.helpUsed;
                if(LetterButton(new Rect(r.xMax-390,r.yMax-87,355,61),state.inquiry.helpUsed?"这句话记在查批簿里":"说起那次一起办的事",true,24))RequestInquiryHelp();
                GUI.enabled=enabled;
            } else {
                var evidence=Investigation.Evidence(state,inquiryPage);
                Label(new Rect(r.x+30,r.y+75,r.width-60,30),evidence.place+"  ·  已收录，可随时翻看",20,sub);
                Label(new Rect(r.x+30,r.y+125,r.width-60,140),evidence.body,mobile?29:26,ink,true);
                if(LetterButton(new Rect(r.x+30,r.yMax-78,290,51),"打开查批簿",false,24)){inquiryPanelMode="board";inquiryNotice="";}
                if(LetterButton(new Rect(r.xMax-280,r.yMax-78,245,51),"收好，继续走",true,24))inquiryPanel=false;
            }
        }
        void DrawInquiryBoard(Rect r,bool mobile)
        {
            float x=r.x+30,width=r.width-60;
            Label(new Rect(x,r.y+77,width,33),"同一笔款，认准三件事。没有收讫记录，就仍是待查。",mobile?25:23,sub);
            float gap=12,cell=(width-2*gap)/3;
            for(int i=0;i<3;i++) {
                bool read=(state.inquiry.evidenceMask&(1<<i))!=0;bool enabled=GUI.enabled;GUI.enabled=enabled&&read;
                if(LetterButton(new Rect(x+i*(cell+gap),r.y+122,cell,54),(read?"翻看 · ":"尚未取得 · ")+Investigation.Evidence(state,i).title,false,mobile?24:22))OpenInquiryEvidence(i,false);
                GUI.enabled=enabled;
            }
            string[] fields={"amount","date","address"};string[] titles={"款额","转交日期","旧称与新址"};
            int[] selected={state.inquiry.amount,state.inquiry.date,state.inquiry.address};
            for(int row=0;row<3;row++) {
                float y=r.y+198+row*92;
                Label(new Rect(x,y+14,138,60),titles[row],mobile?25:23,ink,true);
                var options=Investigation.Choices(state,fields[row]);float c=(width-156-2*gap)/3;
                for(int i=0;i<options.Count;i++) {
                    var option=options[i];Rect b=new Rect(x+156+i*(c+gap),y,c,78);bool chosen=selected[row]==i;
                    Box(b,new Color(.34f,.47f,.39f,chosen?.15f:.045f));
                    if(chosen){Box(new Rect(b.x,b.y,3,b.height),letterSeal);Label(new Rect(b.xMax-30,b.y+4,25,27),"✓",22,red);}
                    bool enabled=GUI.enabled;GUI.enabled=enabled&&option.enabled&&InquiryReadCount==3&&!Investigation.IsComplete(state);
                    Label(new Rect(b.x+14,b.y+9,b.width-34,b.height-14),option.title,mobile?25:23,option.enabled?ink:sub,true,TextAnchor.MiddleLeft);
                    if(GUI.Button(b,GUIContent.none,blank))ChooseInquiryValue(fields[row],i);
                    GUI.enabled=enabled;
                }
            }
            string status=!string.IsNullOrEmpty(inquiryNotice)?inquiryNotice:state.inquiry.helpUsed?Investigation.HelpText(state):InquiryReadCount<3?"先把三处原件找齐，沿纸上的内容填写。":"对照原件填好后，到核认处请经手人复核。";
            Label(new Rect(x,r.y+489,width-390,110),status,mobile?24:22,sub);
            bool canReach=FirstDayCanReach(mission.npcPosition,2.4f),active=GUI.enabled;
            GUI.enabled=active&&InquiryReadCount==3&&!Investigation.IsComplete(state)&&canReach;
            if(LetterButton(new Rect(r.xMax-385,r.yMax-105,350,69),Investigation.IsComplete(state)?"已经核认":canReach?"请经手人核认":"带回核认处",true,26))SubmitInquiry();
            GUI.enabled=active;
            if(!canReach&&!Investigation.IsComplete(state)&&LetterButton(new Rect(r.xMax-385,r.yMax-175,350,51),"在地图上找核认处",false,22)){inquiryPanel=false;map=true;mapTab=0;}
        }
    }
}
