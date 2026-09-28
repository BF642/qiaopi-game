using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool lifePanel, lifeLog;
        string lifeNotice="";
        string renderedDestination="";
        Vector2 lifeScroll,lifeNoticeScroll;
        string TaskNode => LifeJourney.IsActive(state)&&!string.IsNullOrEmpty(state.journey.pendingJob)?state.journey.pendingJob:state.nodeId;
        void Journal(){PersonalJournal();}
        List<HistoryRecord> FullJourneyHistory()
        {
            var result=new List<HistoryRecord>();bool inserted=false;
            foreach(var entry in state.history){
                if(!inserted&&entry.title=="工钱铺在席上"){AppendLifeHistory(result);inserted=true;}
                result.Add(entry);
            }
            if(!inserted)AppendLifeHistory(result);
            return result;
        }
        void AppendLifeHistory(List<HistoryRecord> result)
        {
            if(state.journey==null)return;
            foreach(var item in state.journey.log)result.Add(new HistoryRecord{title=item.title,choice=item.actionId.StartsWith("hardship_")?"应对眼前的欺压":item.title,outcome=item.outcome,location=LifeJourney.DestinationName(state)+" · 自主谋生",year="1906年冬 · 第 "+item.turn+" 轮"});
        }
        Rect LifeEntryRect(){var s=MobileUiBounds();return MobileControls?new Rect(s.xMin+30,339,390,72):new Rect(30,263,258,53);}
        void DrawLifeEntry()
        {
            if(!LifeJourney.HasReachedOverseas(state))return;
            var r=LifeEntryRect();
            if(LifeJourney.IsActive(state)){
                if(LetterButton(r,"安排生活 · "+state.journey.turns+" / 6",true,MobileControls?27:22)){lifePanel=true;lifeLog=false;walkRoute.Clear();}
            }else if(LetterButton(r,"写一封自己的侨批",false,MobileControls?27:21))OpenPersonalLetter();
        }
        void StartLifeAction(string id)
        {
            var action=LifeJourney.Actions(state).Find(a=>a.id==id&&a.enabled);
            if(action==null||!string.IsNullOrEmpty(state.journey.pendingJob))return;
            lifeNotice="";lifeLog=false;
            if(action.physical){
                state.journey.pendingJob=id;lifePanel=false;dialogue=false;EnterNode();Save();
                Toast("已到招工处。亲自完成眼前的工作，再去结账；可以收起安排，沿街探索。");
            }else if(LifeJourney.CommitAction(state,id,out var outcome)){
                lifeNotice=outcome;dialogue=false;EnterNode();lifePanel=true;Save();
            }
        }
        bool HandleLifeInteract()
        {
            if(!LifeJourney.IsActive(state)||Dist(mission.npcPosition)>=2.5f||carrying||sideCarrying)return false;
            if(string.IsNullOrEmpty(state.journey.pendingJob)){lifePanel=true;lifeLog=false;dialogue=false;return true;}
            if(!Complete)return false;
            string job=state.journey.pendingJob;
            if(LifeJourney.CommitAction(state,job,out var outcome)){
                dialogue=false;lifeNotice=outcome;EnterNode();lifePanel=true;Save();
            }
            return true;
        }
        void FinishLifePhase()
        {
            if(!string.IsNullOrEmpty(state.journey.pendingJob)||!LifeJourney.Finish(state))return;
            lifePanel=false;dialogue=false;EnterNode();Save();Toast("这段日子已记下。回住处和乡亲聊聊积蓄，继续家书主线。");
        }
        void DrawLifePanel()
        {
            if(!LifeJourney.IsActive(state)){lifePanel=false;return;}
            bool mobile=MobileControls;Rect safe=mobile?MobileUiBounds():new Rect(0,0,W,H);
            Rect r=new Rect(safe.xMin+55,145,safe.width-110,810);int fs=mobile?28:24;
            Box(new Rect(safe.xMin-100,-100,safe.width+200,1200),new Color(.08f,.16f,.13f,.7f));PaperPanel(r);
            Stamp(new Rect(r.x+28,r.y+22,62,62),"谋生",26);
            Label(new Rect(r.x+114,r.y+21,r.width-310,57),LifeJourney.DestinationName(state)+" · 日子由自己安排",34,ink,true);
            if(LetterButton(new Rect(r.xMax-155,r.y+24,127,60),"沿街走走",false,23)){lifePanel=false;return;}
            Label(new Rect(r.x+30,r.y+98,r.width-60,53),"已过 "+state.journey.turns+" 轮 / 最多 6 轮 · 做满 3 轮可继续剧情 · 盘缠 "+state.money+" · 身体 "+state.health,fs,sub);
            var hardship=LifeJourney.PendingHardship(state);
            if(hardship!=null){DrawLifeHardship(r,hardship,mobile);return;}
            bool working=!string.IsNullOrEmpty(state.journey.pendingJob);
            if(LetterButton(new Rect(r.x+30,r.y+158,200,53),lifeLog?"回到安排":"查看生活簿",false,23)){lifeLog=!lifeLog;lifeScroll=Vector2.zero;}
            if(working){
                Label(new Rect(r.x+255,r.y+163,r.width-520,46),"手头的活尚未结账 · "+mission.itemName,fs,red);
                if(LetterButton(new Rect(r.xMax-240,r.y+158,210,53),"放弃这趟，另找活",false,22)){
                    state.journey.pendingJob="";EnterNode();lifePanel=true;Save();lifeNotice="已退掉未结账的工作；没有领取工钱，也没有消耗谋生轮次。";
                }
            }
            if(lifeLog){DrawLifeLog(new Rect(r.x+30,r.y+228,r.width-60,384),mobile);}
            else {
                var actions=LifeJourney.Actions(state);float width=(r.width-76)/2;
                for(int i=0;i<actions.Count;i++){
                    var a=actions[i];Rect card=new Rect(r.x+30+(i%2)*(width+16),r.y+225+(i/2)*128,width,116);
                    Box(card,C("EADFCA"));Box(new Rect(card.x,card.y,3,card.height),letterSeal);
                    Label(new Rect(card.x+17,card.y+9,card.width-34,38),a.title+(a.money>=0?"  ＋":"  ")+a.money+" 盘缠",fs,a.enabled&&!working?ink:sub,true);
                    Label(new Rect(card.x+17,card.y+52,card.width-34,58),working?"先完成手头的活，或在上方放弃后换工。":a.enabled?a.description:a.unavailableReason,mobile?24:21,sub);
                    bool previous=GUI.enabled;GUI.enabled=previous&&a.enabled&&!working;
                    if(GUI.Button(card,GUIContent.none,blank)){StartLifeAction(a.id);GUI.enabled=previous;return;}GUI.enabled=previous;
                }
                if(actions.Count==0)Label(new Rect(r.x+40,r.y+290,r.width-80,150),"这段自主谋生已做满六轮。\n把眼前的收获与委屈写下来，再继续家书的故事。",32,ink,true);
                if(actions.Count>0){Rect note=new Rect(r.x+46+width,r.y+481,width,105);Label(note,"每次接活都能换行。\n同一行做两轮，会改变往后的主业。",mobile?26:23,sub,true);}
            }
            string hint=string.IsNullOrEmpty(lifeNotice)?"这一段不设现实倒计时。选择和劳动推进日子，探索与写信不会额外消耗轮次。":lifeNotice;
            var noticeRect=new Rect(r.x+30,r.y+620,r.width-60,86);float noticeH=Height(hint,mobile?25:22,noticeRect.width-26,false);
            // Notices can contain consequences from a job and its following hardship.
            GalleryTouchScroll(noticeRect,ref lifeNoticeScroll,noticeH);
            lifeNoticeScroll=GUI.BeginScrollView(noticeRect,lifeNoticeScroll,new Rect(0,0,noticeRect.width-26,Mathf.Max(86,noticeH)),false,false);
            Label(new Rect(0,0,noticeRect.width-26,noticeH+4),hint,mobile?25:22,sub);GUI.EndScrollView();
            if(LetterButton(new Rect(r.x+30,r.yMax-81,300,57),"亲手写一封侨批",false,fs)){lifePanel=false;OpenPersonalLetter();return;}
            bool enabled=GUI.enabled;GUI.enabled=enabled&&LifeJourney.CanFinish(state)&&!working;
            if(LetterButton(new Rect(r.xMax-450,r.yMax-81,420,57),LifeJourney.CanFinish(state)?"收好这段生活，继续剧情":"再过 "+Mathf.Max(0,3-state.journey.turns)+" 轮可继续剧情",true,fs))FinishLifePhase();GUI.enabled=enabled;
        }
        void DrawLifeHardship(Rect r,JourneyHardship h,bool mobile)
        {
            Label(new Rect(r.x+34,r.y+157,r.width-68,53),h.title,34,red,true);
            Rect content=new Rect(r.x+34,r.y+220,r.width-68,228);float height=Height(h.body,mobile?29:26,content.width-28,true);
            GalleryTouchScroll(content,ref lifeScroll,height);
            lifeScroll=GUI.BeginScrollView(content,lifeScroll,new Rect(0,0,content.width-28,Mathf.Max(height,228)),false,false);
            Label(new Rect(0,0,content.width-28,height+5),h.body,mobile?29:26,ink,true);GUI.EndScrollView();
            for(int i=0;i<h.choices.Count;i++){
                var a=h.choices[i];Rect c=new Rect(r.x+34,r.y+462+i*101,r.width-68,89);Box(c,C("EADFCA"));
                Label(new Rect(c.x+18,c.y+9,c.width-36,34),a.title,mobile?29:26,a.enabled?ink:sub,true);
                Label(new Rect(c.x+18,c.y+49,c.width-36,35),a.enabled?a.description:a.unavailableReason,mobile?24:22,sub);
                bool previous=GUI.enabled;GUI.enabled=previous&&a.enabled;
                if(GUI.Button(c,GUIContent.none,blank)&&LifeJourney.ResolveHardship(state,a.id,out var outcome)){lifeNotice=outcome;lifeScroll=Vector2.zero;Save();GUI.enabled=previous;return;}GUI.enabled=previous;
            }
        }
        void DrawLifeLog(Rect rect,bool mobile)
        {
            float w=rect.width-28,total=0;int fs=mobile?27:23;
            foreach(var item in state.journey.log)total+=Height(item.outcome,fs,w-24,true)+89;
            GalleryTouchScroll(rect,ref lifeScroll,total);lifeScroll=GUI.BeginScrollView(rect,lifeScroll,new Rect(0,0,w,Mathf.Max(total,rect.height)),false,false);
            float y=0;foreach(var item in state.journey.log){float h=Height(item.outcome,fs,w-24,true);Label(new Rect(10,y,w-20,45),"第 "+item.turn+" 轮 · "+item.title,fs,red,true);Label(new Rect(10,y+50,w-24,h+5),item.outcome,fs,ink,true);y+=h+89;}
            if(state.journey.log.Count==0)Label(new Rect(20,65,w-40,120),"刚到这里，你还可以自己决定第一份安排。",30,sub,true);
            GUI.EndScrollView();
        }
    }
}
