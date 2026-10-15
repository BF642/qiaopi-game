using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi.Editor
{
    // All disk operations are confined to a fresh OS temporary directory. These
    // checks never consult the player's save path, preferences, scene or audio.
    public static class FirstDayChecks
    {
        sealed class Branch
        {
            public string route, choice, flag, activity;
            public int money, health, family, trust, required, bonusMoney;
            public Branch(string r, string c, string f, int m, int h, int fa, int t, string a, int count, int bonus)
            { route=r; choice=c; flag=f; money=m; health=h; family=fa; trust=t; activity=a; required=count; bonusMoney=bonus; }
        }
        static readonly Branch[] Branches = {
            new Branch("dock","extra","earned_dock",17,-13,0,0,"deliver",1,2),
            new Branch("dock","help","crew_help",9,-4,0,8,"deliver",1,0),
            new Branch("dock","tally","marked_cargo",10,0,0,10,"inspect",2,0),
            new Branch("shop","learn","shop_partner",11,-4,0,8,"inspect",2,2),
            new Branch("shop","wages","cash_worker",16,-7,-2,0,"inspect",1,2),
            new Branch("shop","read","read_letters",8,0,3,10,"inspect",1,0),
            new Branch("courier","verify","verified",9,-4,0,12,"inspect",1,0),
            new Branch("courier","rush","hurried",15,-8,0,-7,"deliver",1,2),
            new Branch("courier","craft","clerk_skill",7,0,0,12,"inspect",2,0)
        };
        static int assertions, checkpoints;
        static string folder;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("侨批/检查南洋第一天")]
#endif
        public static void Run()
        {
            assertions=0; checkpoints=0;
            folder=Path.Combine(Path.GetTempPath(),"qiaopi-firstday-checks-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                CheckLegacyAndInvalid();
                string[] destinations={"singapore","penang","rangoon"};
                string[] dinners={"meal","together","sleep"};
                foreach(var branch in Branches)
                    for(int i=0;i<destinations.Length;i++) CheckBranch(branch,destinations[i],dinners[i]);
                CheckDinnerBounds();
                CheckLetters();
                CheckLetterStageGate();
                CheckCapacityAndLimit();
                CheckInterruptedSettlement();
                CheckRecovery();
                Debug.Log("FIRST_DAY_CHECKS_PASSED: 9 work branches × 3 destinations, physical-task saves, dinner affordability, delayed personal replies, stale-token/capacity protection, one-time settlement/referral, legacy and interrupted saves; assertions="+assertions+", file checkpoints="+checkpoints+". Player saves/preferences untouched.");
            } finally {
                if(Directory.Exists(folder)) Directory.Delete(folder,true);
                folder=null;
            }
        }

        static GameState AtJob(string route,string destination="singapore")
        {
            var s=StoryEngine.NewGame(19060923); s.journey.destination=destination;
            foreach(string choice in ("meal neighbors ledger guarantor mother share "+route).Split(' ')) {
                Require(StoryEngine.Choose(s,choice),"prologue choose "+s.nodeId+"/"+choice);
                Require(StoryEngine.Continue(s),"prologue continue "+choice);
            }
            Require(s.nodeId==route&&StoryEngine.Validate(s),"real prologue reaches "+route);
            s.money=40; s.health=50; s.family=50; s.trust=60;
            return s;
        }
        static GameState Chosen(Branch b,string destination="singapore")
        {
            var s=AtJob(b.route,destination);
            Require(FirstDayChapter.TryBegin(s),"begin "+b.route);
            Require(FirstDayChapter.TriggerEncounter(s,1),"encounter "+b.route);
            Require(FirstDayChapter.ChooseWork(s,b.choice),"choose "+b.route+"/"+b.choice);
            return s;
        }
        static GameState AtLetter(Branch b=null)
        {
            var s=Chosen(b??Branches[5]);
            Require(FirstDayChapter.SettleWork(s),"letter fixture settle");
            string notice; Require(FirstDayChapter.TryEvening(s,"sleep",out notice),"letter fixture dinner");
            return s;
        }
        static void CheckBranch(Branch b,string destination,string dinner)
        {
            string tag=b.route+"/"+b.choice+"/"+destination;
            var s=AtJob(b.route,destination); var originalMission=WorldMissions.Get(s);
            Require(FirstDayChapter.TryBegin(s),tag+" begins");
            int decisions=s.decisions, history=s.history.Count;
            RejectUnchanged(s,()=>FirstDayChapter.TryBegin(s),tag+" duplicate begin");
            RejectUnchanged(s,()=>FirstDayChapter.TriggerEncounter(s,0),tag+" no encounter before physical progress");
            RejectUnchanged(s,()=>FirstDayChapter.ChooseWork(s,b.choice),tag+" cannot choose before encounter");
            RejectUnchanged(s,()=>FirstDayChapter.SettleWork(s),tag+" cannot settle before branch");
            Require(WorldMissions.Get(s).activity==originalMission.activity&&WorldMissions.Get(s).required==originalMission.required,tag+" initial task preserved");
            Checkpoint(ref s,tag+"/work-initial");
            CheckMissionProgress(s,tag+"/initial-task");
            Require(FirstDayChapter.TriggerEncounter(s,1),tag+" encounter after first task");
            Checkpoint(ref s,tag+"/encounter");
            RejectUnchanged(s,()=>FirstDayChapter.ChooseWork(s,"unknown"),tag+" unknown work");
            string before=JsonUtility.ToJson(s);
            var choices=FirstDayChapter.WorkChoices(s);
            Require(choices.Count==3&&choices.Exists(c=>c.id==b.choice&&c.enabled),tag+" exact branch menu");
            Require(before==JsonUtility.ToJson(s),tag+" choice menu read-only");
            Require(FirstDayChapter.ChooseWork(s,b.choice),tag+" record choice");
            Require(s.money==40&&s.health==50&&s.family==50&&s.trust==60&&s.decisions==decisions&&s.history.Count==history,tag+" choice does not settle");
            RejectUnchanged(s,()=>FirstDayChapter.ChooseWork(s,b.choice),tag+" duplicate branch choice");
            RejectUnchanged(s,()=>FirstDayChapter.TriggerEncounter(s,1),tag+" encounter not repeated");
            var m=WorldMissions.Get(s);
            Require(m.activity==b.activity&&m.required==b.required,tag+" correct new physical task");
            if(m.activity=="inspect") for(int i=0;i<m.required;i++) {
                var inspection=FirstDayTasks.Inspection(s,i);
                Require(inspection!=null&&inspection.answers!=null&&inspection.answers.Length==3&&inspection.correct>=0&&inspection.correct<3&&!string.IsNullOrEmpty(inspection.question),tag+" authored inspection "+i);
            }
            Checkpoint(ref s,tag+"/work-chosen"); CheckMissionProgress(s,tag+"/branch-task");
            Require(FirstDayChapter.SettleWork(s),tag+" settle");
            Require(s.nodeId=="first_pay"&&!s.awaitingContinue&&s.firstDay.stage=="evening"&&s.firstDay.workSettled,tag+" settles to evening");
            Require(s.money==40+b.money&&s.health==50+b.health&&s.family==50+b.family&&s.trust==60+b.trust,tag+" original exact reward");
            Require(s.decisions==decisions+1&&s.history.Count==history+1&&s.flags.Contains(b.flag),tag+" one story decision and flag");
            Require(!LifeJourney.IsActive(s)&&LifeJourney.Actions(s).Count==0,tag+" free living waits for first day");
            RejectUnchanged(s,()=>FirstDayChapter.SettleWork(s),tag+" duplicate settlement");
            Checkpoint(ref s,tag+"/evening");
            string notice; int money=s.money, health=s.health, trust=s.trust;
            Require(FirstDayChapter.TryEvening(s,dinner,out notice)&&!string.IsNullOrEmpty(notice),tag+" dinner");
            Require(s.money==money-(dinner=="meal"?2:dinner=="together"?3:0)&&s.health==health+(dinner=="meal"?8:dinner=="together"?6:4)&&s.trust==trust+(dinner=="together"?1:0),tag+" dinner exact costs");
            Require(s.decisions==decisions+1&&s.history.Count==history+1,tag+" evening is not extra main-story decision");
            Checkpoint(ref s,tag+"/letter");
            var draft=PersonalLetters.EnsureDraft(s); draft.recipient="wife"; draft.intent="truth"; draft.amount=5; draft.body="今日仍未落笔完。\n回头再把这一天说给你听。";
            string draftBefore=JsonUtility.ToJson(draft);
            Require(FirstDayChapter.DeferLetter(s),tag+" can keep draft");
            Require(JsonUtility.ToJson(s.personalLetterDraft)==draftBefore&&s.personalLettersSent==0,tag+" defer preserves draft and tokens");
            Checkpoint(ref s,tag+"/bed"); health=s.health;
            Require(FirstDayChapter.Sleep(s)&&s.health==health,tag+" physical sleep adds no duplicate health");
            RejectUnchanged(s,()=>FirstDayChapter.Sleep(s),tag+" cannot sleep twice");
            Checkpoint(ref s,tag+"/morning");
            CheckMorningPreview(s,tag);
            string feedback=FirstDayChapter.AdvanceMorning(s);
            Require(!string.IsNullOrEmpty(feedback)&&s.firstDay.stage=="complete"&&LifeJourney.IsActive(s),tag+" morning completes deferred chapter");
            Require(JsonUtility.ToJson(s.personalLetterDraft)==draftBefore&&s.decisions==decisions+1,tag+" morning preserves draft/main history");
            Checkpoint(ref s,tag+"/complete");
            CheckBonus(ref s,b,tag);
        }

        static void CheckBonus(ref GameState s,Branch b,string tag)
        {
            string snapshot=JsonUtility.ToJson(s);
            var bonus=FirstDayChapter.BonusForWork(s,b.route);
            Require(bonus.available&&bonus.money==b.bonusMoney&&bonus.trust==(b.bonusMoney==0?2:0),tag+" promised referral");
            Require(snapshot==JsonUtility.ToJson(s),tag+" bonus preview read-only");
            string other=b.route=="dock"?"shop":"dock";
            Require(!FirstDayChapter.BonusForWork(s,other).available&&string.IsNullOrEmpty(FirstDayChapter.ApplyWorkBonus(s,other))&&snapshot==JsonUtility.ToJson(s),tag+" wrong route cannot consume referral");
            int money=s.money,trust=s.trust,decisions=s.decisions,history=s.history.Count,turns=s.journey.turns,logs=s.journey.log.Count;
            int baseMoney=b.route=="dock"?10:b.route=="shop"?7:6;
            int baseTrust=b.route=="dock"?1:b.route=="shop"?4:6;
            string outcome;
            Require(LifeJourney.CommitAction(s,b.route,out outcome),tag+" real later work receives referral");
            Require(s.money==money+baseMoney+bonus.money&&s.trust==Math.Min(100,trust+baseTrust+bonus.trust)&&s.firstDay.bonusUsed,tag+" actual later-work reward");
            Require(s.decisions==decisions&&s.history.Count==history&&s.journey.turns==turns+1&&s.journey.log.Count==logs+1,tag+" referral creates no extra turn or history");
            Checkpoint(ref s,tag+"/bonus-used");
            snapshot=JsonUtility.ToJson(s);
            Require(!FirstDayChapter.BonusForWork(s,b.route).available&&string.IsNullOrEmpty(FirstDayChapter.ApplyWorkBonus(s,b.route))&&snapshot==JsonUtility.ToJson(s),tag+" referral cannot repeat after reload");
        }

        static void CheckDinnerBounds()
        {
            foreach(string id in new[]{"meal","together","sleep"}) for(int money=0;money<=3;money++) {
                var s=Chosen(Branches[0]); Require(FirstDayChapter.SettleWork(s),"dinner fixture"); s.money=money;
                int cost=id=="meal"?2:id=="together"?3:0; string notice;
                string before=JsonUtility.ToJson(s);
                bool ok=FirstDayChapter.TryEvening(s,id,out notice);
                Require(ok==(money>=cost),"dinner affordability "+id+"/"+money);
                if(!ok) Require(before==JsonUtility.ToJson(s),"unaffordable meal does not spend or change stage");
                else { Require(s.money==money-cost&&s.money>=0,"dinner does not overspend"); RejectUnchanged(s,()=>FirstDayChapter.TryEvening(s,id,out notice),"duplicate dinner"); }
                Require(StoryEngine.Validate(s),"dinner boundary valid");
            }
        }

        static void CheckLetters()
        {
            foreach(string recipient in new[]{"mother","wife","sister"})
            foreach(string intent in new[]{"reassure","truth","study"})
            foreach(int amount in PersonalLetters.Amounts) {
                var s=AtLetter(); var draft=PersonalLetters.EnsureDraft(s);
                draft.recipient=recipient; draft.intent=intent; draft.amount=amount; draft.body="今日第一天。\n我还在学着安顿自己。<原文> \"不要改写\"";
                string body=draft.body, token=draft.token,notice,tag="letter/"+recipient+"/"+intent+"/"+amount;
                Checkpoint(ref s,tag+"/draft");
                int count=s.letters.Count,incoming=Incoming(s),money=s.money,decisions=s.decisions,family=s.family;
                RejectUnchanged(s,()=>PersonalLetters.TrySend(s,new string('a',32),out notice),tag+" stale token");
                Require(PersonalLetters.TrySend(s,token,out notice),tag+" send");
                Require(s.money==money-amount&&s.family==family+(amount==0?1:2)&&s.personalLettersSent==1&&s.personalLetterTokens.Contains(token),tag+" pay and tokenize once");
                Require(s.letters.Count==count+1&&Incoming(s)==incoming&&s.letters[count].body==body&&!s.letters[count].incoming,tag+" only outgoing enters inbox now");
                Require(s.firstDay.stage=="bed"&&s.firstDay.letterSent&&s.firstDay.sentToken==token&&s.firstDay.recipient==recipient&&s.firstDay.intent==intent&&s.firstDay.amount==amount,tag+" saved attribution");
                Require(s.firstDay.pendingReply.incoming&&s.firstDay.pendingReply.signature==PersonalLetters.RecipientName(recipient)&&s.firstDay.pendingReply.date==FirstDayChapter.ReplyDate,tag+" pending authored reply/date");
                Require(s.personalLetterDraft.token!=token&&s.personalLetterDraft.body==""&&s.personalLetterDraft.recipient==recipient,tag+" new envelope created");
                string reply=JsonUtility.ToJson(s.firstDay.pendingReply);
                s.personalLetterDraft.body="这封新稿先留着，等下回再寄。"; string nextDraft=JsonUtility.ToJson(s.personalLetterDraft);
                RejectUnchanged(s,()=>PersonalLetters.TrySend(s,token,out notice),tag+" old token cannot send new draft");
                RejectUnchanged(s,()=>FirstDayChapter.CollectReply(s),tag+" cannot collect before morning");
                Checkpoint(ref s,tag+"/in-transit");
                int health=s.health; Require(FirstDayChapter.Sleep(s)&&s.health==health,tag+" sleep");
                Checkpoint(ref s,tag+"/morning");
                CheckMorningPreview(s,tag);
                Require(!string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(s))&&s.firstDay.stage=="reply",tag+" next morning feedback");
                Require(s.letters.Count==count+1&&Incoming(s)==incoming&&!FirstDayChapter.BonusForWork(s,s.firstDay.route).available,tag+" morning does not deliver weeks-later reply");
                Checkpoint(ref s,tag+"/reply-pending");
                Require(FirstDayChapter.CollectReply(s),tag+" collect at weeks-later scene");
                Require(s.letters.Count==count+2&&Incoming(s)==incoming+1&&JsonUtility.ToJson(s.letters[count+1])==reply&&s.firstDay.replyCollected,tag+" exact reply received once");
                Require(s.decisions==decisions&&JsonUtility.ToJson(s.personalLetterDraft)==nextDraft,tag+" reply preserves next draft and story history");
                Checkpoint(ref s,tag+"/reply-collected");
                RejectUnchanged(s,()=>FirstDayChapter.CollectReply(s),tag+" duplicate reply after reload");
                string snapshot=JsonUtility.ToJson(s);
                Require(string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(s))&&snapshot==JsonUtility.ToJson(s),tag+" duplicate morning after reload");
            }
        }

        static void CheckLetterStageGate()
        {
            foreach(var b in new[]{Branches[1],Branches[3],Branches[6]}) {
                var s=AtJob(b.route); string notice;
                var draft=PersonalLetters.EnsureDraft(s);
                draft.recipient="wife"; draft.intent="truth"; draft.amount=2;
                draft.body="这一封先留在信匣。\n今天的活做完，再坐下慢慢寄。";
                string keptDraft=JsonUtility.ToJson(draft);
                Require(FirstDayChapter.TryBegin(s),"letter gate begins "+b.route);
                CheckSendBlocked(ref s,b.route+"/work-initial");
                Require(FirstDayChapter.TriggerEncounter(s,1),"letter gate encounter");
                CheckSendBlocked(ref s,b.route+"/encounter");
                Require(FirstDayChapter.ChooseWork(s,b.choice),"letter gate choice");
                CheckSendBlocked(ref s,b.route+"/work-chosen");
                Require(FirstDayChapter.SettleWork(s),"letter gate settle");
                CheckSendBlocked(ref s,b.route+"/evening");
                Require(FirstDayChapter.TryEvening(s,"sleep",out notice),"letter gate dinner");
                Require(JsonUtility.ToJson(s.personalLetterDraft)==keptDraft,"blocked stages preserve original draft envelope and words");
                string before=JsonUtility.ToJson(s);
                Require(PersonalLetters.CanSend(s,out notice)&&before==JsonUtility.ToJson(s),"letter stage permits send without CanSend side effects");

                // Both ways of completing the chapter must restore ordinary
                // correspondence; deferring must not permanently lock the draft.
                var deferred=JsonUtility.FromJson<GameState>(before);
                Require(FirstDayChapter.DeferLetter(deferred),"letter gate defer branch");
                CheckSendBlocked(ref deferred,b.route+"/bed-deferred");
                Require(FirstDayChapter.Sleep(deferred),"letter gate deferred sleep");
                CheckSendBlocked(ref deferred,b.route+"/morning-deferred");
                Require(!string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(deferred)),"letter gate deferred morning");
                CheckOrdinarySendAfterFirstDay(ref deferred,b.route+"/complete-deferred");

                int incoming=Incoming(s),letters=s.letters.Count;
                Require(PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice)&&s.firstDay.stage=="bed", "letter gate sends first letter at permitted stage");
                Require(Incoming(s)==incoming&&s.letters.Count==letters+1,"permitted first-day send still defers its reply");
                var nextDraft=PersonalLetters.EnsureDraft(s); nextDraft.amount=2;
                nextDraft.body="另一封新稿先收好，不能跳过今晚和数周的等待。";
                CheckSendBlocked(ref s,b.route+"/bed-sent");
                Require(FirstDayChapter.Sleep(s),"letter gate sent sleep");
                CheckSendBlocked(ref s,b.route+"/morning-sent");
                Require(!string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(s)),"letter gate sent morning");
                CheckSendBlocked(ref s,b.route+"/reply");
                Require(FirstDayChapter.CollectReply(s),"letter gate receipt completes chapter");
                CheckOrdinarySendAfterFirstDay(ref s,b.route+"/complete-received");
            }
        }
        static void CheckSendBlocked(ref GameState s,string tag)
        {
            Checkpoint(ref s,"send-gate/"+tag);
            string before=JsonUtility.ToJson(s),reason;
            Require(!PersonalLetters.CanSend(s,out reason)&&!string.IsNullOrEmpty(reason),tag+" CanSend explains blocked phase");
            Require(before==JsonUtility.ToJson(s),tag+" CanSend preserves draft and every state field");
            bool sent=PersonalLetters.TrySend(s,s.personalLetterDraft.token,out reason);
            Require(!sent&&before==JsonUtility.ToJson(s),tag+" current envelope cannot bypass phase or spend money");
        }
        static void CheckOrdinarySendAfterFirstDay(ref GameState s,string tag)
        {
            Checkpoint(ref s,tag);
            Require(s.firstDay.stage=="complete"&&!FirstDayChapter.IsActive(s),tag+" complete phase");
            int money=s.money,letters=s.letters.Count,incoming=Incoming(s),sent=s.personalLettersSent;
            int amount=s.personalLetterDraft.amount; string body=s.personalLetterDraft.body,notice;
            string chapter=JsonUtility.ToJson(s.firstDay),before=JsonUtility.ToJson(s);
            Require(PersonalLetters.CanSend(s,out notice)&&before==JsonUtility.ToJson(s),tag+" ordinary send available read-only");
            Require(PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),tag+" ordinary send succeeds");
            Require(s.money==money-amount&&s.personalLettersSent==sent+1&&s.letters.Count==letters+2&&Incoming(s)==incoming+1&&s.letters[letters].body==body,tag+" normal outgoing and authored reply resume");
            Require(chapter==JsonUtility.ToJson(s.firstDay)&&StoryEngine.Validate(s),tag+" later send does not overwrite first-day letter or restart chapter");
        }

        static void CheckCapacityAndLimit()
        {
            var s=AtLetter(); FillLetters(s,38); PersonalLetters.EnsureDraft(s).body="信箱将满也把这一封的回程位置留好。";
            string notice; Require(PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),"38 records permits reserved outgoing/reply pair");
            Require(s.letters.Count==39,"deferred reply is not in collection yet");
            var overflow=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s)); FillLetters(overflow,40);
            Require(!StoryEngine.Validate(overflow),"pending reply rejects an inbox that consumes its reserved slot");
            s.personalLetterDraft.body="这封仍应当留作草稿。";
            RejectUnchanged(s,()=>PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),"pending reply reserves final capacity");
            Checkpoint(ref s,"capacity/pending39");
            Require(FirstDayChapter.Sleep(s)&&!string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(s))&&FirstDayChapter.CollectReply(s),"reserved reply fills last slot");
            Require(s.letters.Count==40&&StoryEngine.Validate(s),"full inbox with received reply remains valid");
            Checkpoint(ref s,"capacity/full40");
            s=AtLetter(); FillLetters(s,39); PersonalLetters.EnsureDraft(s).body="没有回程空位不能先扣银寄出。";
            RejectUnchanged(s,()=>PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),"39 records rejects pair atomically");
            Require(FirstDayChapter.DeferLetter(s),"capacity limit still allows defer");

            s=AtJob("shop");
            for(int i=0;i<PersonalLetters.MaxLetters;i++) { var d=PersonalLetters.EnsureDraft(s); d.body="此前寄出的第"+i+"封。"; Require(PersonalLetters.TrySend(s,d.token,out notice),"legacy pre-chapter send "+i); }
            Require(FirstDayChapter.TryBegin(s)&&FirstDayChapter.TriggerEncounter(s,1)&&FirstDayChapter.ChooseWork(s,"learn")&&FirstDayChapter.SettleWork(s),"cap fixture work");
            Require(FirstDayChapter.TryEvening(s,"sleep",out notice),"cap fixture evening");
            var kept=PersonalLetters.EnsureDraft(s); kept.body="第四封只留作草稿，不应卡住首日。"; string draftBefore=JsonUtility.ToJson(kept);
            RejectUnchanged(s,()=>PersonalLetters.TrySend(s,kept.token,out notice),"three sends cap rejects extra");
            Require(FirstDayChapter.DeferLetter(s)&&FirstDayChapter.Sleep(s)&&!string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(s)),"three-letter cap can complete chapter");
            Require(s.firstDay.stage=="complete"&&s.personalLettersSent==3&&JsonUtility.ToJson(s.personalLetterDraft)==draftBefore,"cap completion retains unsent draft");
            Checkpoint(ref s,"capacity/three-sent-deferred");
        }

        static void CheckInterruptedSettlement()
        {
            foreach(var b in Branches) {
                var s=Chosen(b); Require(StoryEngine.Choose(s,b.choice),"simulate interrupted legacy settlement");
                int money=s.money,health=s.health,family=s.family,trust=s.trust,decisions=s.decisions,history=s.history.Count;
                Checkpoint(ref s,"interrupted/"+b.route+"/"+b.choice);
                Require(FirstDayChapter.SettleWork(s),"resume paid pending settlement");
                Require(s.money==money&&s.health==health&&s.family==family&&s.trust==trust&&s.decisions==decisions&&s.history.Count==history&&s.firstDay.stage=="evening","resume does not pay twice");
                Checkpoint(ref s,"resumed/"+b.route+"/"+b.choice);
            }
            var disabled=AtJob("dock"); disabled.flags.Remove("ledger"); disabled.trust=55;
            Require(FirstDayChapter.TryBegin(disabled)&&FirstDayChapter.TriggerEncounter(disabled,1),"disabled tally fixture");
            RejectUnchanged(disabled,()=>FirstDayChapter.ChooseWork(disabled,"tally"),"disabled tally does not record choice");
            disabled.trust=56; Require(FirstDayChapter.ChooseWork(disabled,"tally"),"tally exact trust threshold");
            disabled.trust=55;
            RejectUnchanged(disabled,()=>FirstDayChapter.SettleWork(disabled),"tally qualification lost before payout is atomic");
            var nearCap=Chosen(Branches[0]); nearCap.money=9998;
            Require(FirstDayChapter.SettleWork(nearCap)&&nearCap.money==9999&&StoryEngine.Validate(nearCap),"near-cap salary settles once without invalid save");
            var craft=AtJob("courier"); craft.flags.Remove("ledger");
            Require(FirstDayChapter.TryBegin(craft)&&FirstDayChapter.TriggerEncounter(craft,1)&&FirstDayChapter.ChooseWork(craft,"craft")&&FirstDayChapter.SettleWork(craft)&&craft.trust==70,"craft without ledger uses original +10 trust");
        }

        static void CheckLegacyAndInvalid()
        {
            foreach(string route in new[]{"dock","shop","courier"}) {
                var s=AtJob(route); s.firstDay=null;
                Checkpoint(ref s,"legacy/"+route);
                Require(s.firstDayVersion==0&&!FirstDayChapter.IsActive(s),"old save does not auto-start during read");
                Require(FirstDayChapter.TryBegin(s)&&s.firstDay.route==route,"old job can explicitly start first day");
                Checkpoint(ref s,"legacy-start/"+route);
            }
            var later=StoryEngine.NewGame(); later.nodeId="first_pay";
            RejectUnchanged(later,()=>FirstDayChapter.TryBegin(later),"existing free-living save does not restart chapter");
            var future=AtJob("dock"); future.firstDayVersion=99; Require(!StoryEngine.Validate(future),"future chapter version rejected");
            var invalid=Chosen(Branches[0]); invalid.firstDay.choice="craft"; Require(!StoryEngine.Validate(invalid),"cross-route choice rejected");
            invalid=Chosen(Branches[0]); invalid.firstDay.stage="missing"; Require(!StoryEngine.Validate(invalid),"unknown phase rejected");
            invalid=Chosen(Branches[0]); invalid.firstDayVersion=0; Require(!StoryEngine.Validate(invalid),"unversioned nonempty chapter rejected");
            invalid=AtLetter(); invalid.firstDay.workSettled=false; Require(!StoryEngine.Validate(invalid),"evening requires actual settled work");
        }

        static void CheckRecovery()
        {
            var s=AtLetter(); var draft=PersonalLetters.EnsureDraft(s); draft.body="停电前写到这里。\n再打开时字还应当在。";
            string path=Path.Combine(folder,"recovery.json"); WorldSaveStore.Write(path,MakeSave(s));
            Require(FirstDayChapter.DeferLetter(s),"recovery next stage"); WorldSaveStore.Write(path,MakeSave(s));
            Require(File.Exists(path+".bak")&&!File.Exists(path+".tmp"),"atomic write leaves backup without temporary file");
            File.WriteAllText(path,"{broken first-day save"); bool recovered;
            var restored=WorldSaveStore.Load(path,out recovered);
            Require(restored!=null&&recovered&&restored.story.firstDay.stage=="letter"&&restored.story.personalLetterDraft.body==draft.body,"corrupt main recovers valid draft checkpoint");
        }

        static void CheckMissionProgress(GameState s,string tag)
        {
            var m=WorldMissions.Get(s);
            for(int progress=0;progress<=m.required;progress++) {
                var save=MakeSave(s); save.progress=progress;
                if(m.activity=="inspect") for(int i=0;i<progress;i++) save.inspected.Add(i);
                CheckWorldRoundTrip(save,tag+"/progress"+progress);
                if(m.activity=="deliver"&&progress<m.required) { save.carrying=true; CheckWorldRoundTrip(save,tag+"/carrying"+progress); }
            }
            var bad=MakeSave(s); bad.progress=m.required+1; Require(!WorldSaveStore.IsValid(bad),tag+" cannot restore excess progress");
            if(m.activity=="inspect") { bad=MakeSave(s); bad.progress=1; Require(!WorldSaveStore.IsValid(bad),tag+" inspection count must match restored progress"); }
        }
        static WorldSave MakeSave(GameState s)
        {
            var m=WorldMissions.Get(s); var spawn=WorldRegions.Get(m.world).spawn;
            return new WorldSave{story=s,missionNode=s.nodeId,layoutVersion=2,px=spawn.x,pz=spawn.z,inspected=new List<int>()};
        }
        static void Checkpoint(ref GameState s,string tag)
        {
            Require(StoryEngine.Validate(s)&&FirstDayChapter.Validate(s),tag+" state validates");
            var save=CheckWorldRoundTrip(MakeSave(s),tag);
            s=save.story;
            Require(StoryEngine.Validate(s)&&FirstDayChapter.Validate(s),tag+" restored state validates");
            if(FirstDayChapter.IsActive(s)&&s.firstDay.stage!="work"&&s.firstDay.stage!="encounter") {
                var m=WorldMissions.Get(s); Require(m.world=="quarters"&&m.activity=="talk"&&m.required==0,tag+" evening-stage physical district");
            }
        }
        static WorldSave CheckWorldRoundTrip(WorldSave save,string tag)
        {
            Require(WorldSaveStore.IsValid(save),tag+" world save valid");
            // Unity may materialize absent inline objects. Once normalized, the
            // complete serialized payload must remain byte-for-byte stable.
            var normalized=JsonUtility.FromJson<WorldSave>(JsonUtility.ToJson(save));
            string expected=JsonUtility.ToJson(normalized);
            Require(WorldSaveStore.IsValid(normalized),tag+" JSON restore valid");
            string path=Path.Combine(folder,"checkpoint.json"); WorldSaveStore.Write(path,normalized); bool recovered;
            var loaded=WorldSaveStore.Load(path,out recovered); checkpoints++;
            Require(loaded!=null&&!recovered&&WorldSaveStore.IsValid(loaded),tag+" disk restore uses main save");
            Require(expected==JsonUtility.ToJson(loaded),tag+" all checkpoint fields survive disk restore");
            return loaded;
        }
        static int Incoming(GameState s) { return s.letters.FindAll(l=>l.incoming).Count; }
        static void CheckMorningPreview(GameState s,string tag)
        {
            string before=JsonUtility.ToJson(s);
            Require(!string.IsNullOrEmpty(FirstDayChapter.MorningPreview(s))&&before==JsonUtility.ToJson(s),tag+" morning preview neither grants reward nor advances time");
            Require(!FirstDayChapter.BonusForWork(s,s.firstDay.route).available,tag+" preview does not unlock reward early");
        }
        static void FillLetters(GameState s,int count)
        {
            while(s.letters.Count<count) s.letters.Add(new LetterRecord{title="既有信件",date="此前",route="泉州",body="既有原文",signature="家人",incoming=true});
        }
        static void RejectUnchanged(GameState s,Func<bool> action,string tag)
        {
            string before=JsonUtility.ToJson(s); Require(!action(),tag+" rejected"); Require(before==JsonUtility.ToJson(s),tag+" leaves every field unchanged");
        }
        static void Require(bool ok,string message)
        {
            assertions++; if(!ok) throw new InvalidOperationException("FIRST_DAY_CHECK_FAILED: "+message);
        }
    }
}
