using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class ExperienceLetterChecks
    {
        static int checks;
        static readonly string[] Recipients={"mother","wife","sister"};
        static readonly string[] Intents={"truth","reassure","study"};
        static readonly string[] Routes={"dock","shop","courier"};
        static readonly string[] Responses={"evidence","together","leave","endure"};

        public static void Run()
        {
            checks=0;
            CheckNoInventedExperience();
            CheckWifeContinuityAndOldRecords();
            CheckCareerExperience();
            for(int response=0;response<Responses.Length;response++)for(int recipient=0;recipient<3;recipient++)
                foreach(int amount in PersonalLetters.Amounts)
                    CheckDelayed(Routes[recipient],Responses[response],Recipients[recipient],Intents[(response+recipient)%3],amount);
            CheckCapacityLimitAndRollback();
            CheckFirstDayPriority();
            Debug.Log("EXPERIENCE_LETTER_CHECKS_PASSED: recorded experiences only, four wage responses, 3 careers × 3 levels, distinct family voices, wife continuity, 48 delayed correspondence combinations, capacity/stale-token/rollback, first-day priority and old-record compatibility; assertions="+checks+". No player files or preferences used.");
        }

        static GameState Free(bool enroll=false)
        {
            var s=StoryEngine.NewGame(19061008);s.nodeId="first_pay";s.money=100;s.health=90;s.trust=60;
            if(enroll)Need(NanyangLife.TryBegin(s),"new free-life enrollment");
            Need(StoryEngine.Validate(s),"free fixture valid");return s;
        }
        static PersonalLetterDraft Draft(GameState s,string recipient="wife",string intent="truth",int amount=2)
        {
            var draft=PersonalLetters.EnsureDraft(s);draft.recipient=recipient;draft.intent=intent;draft.amount=amount;
            draft.body="亲手写下的原文：\n今天的事慢慢说给你听。\n<不是标签> \"仍要原样保留\"";
            return draft;
        }
        static GameState AtLetter(string route="dock",string response="evidence",int previous=0)
        {
            var s=Free(true);string notice;
            for(int i=0;i<previous;i++){var d=Draft(s,"mother","reassure",0);Need(PersonalLetters.TrySend(s,d.token,out notice),"earlier ordinary letter");}
            Need(LifeJourney.CommitAction(s,route,out notice)&&s.nanyang.stage=="choice","wage dispute follows real work");
            Need(NanyangLife.ChooseResponse(s,response),"choose actual wage response");
            int required=NanyangLife.RequiredSteps(s);
            for(int i=0;i<required;i++)Need(NanyangLife.CompleteStep(s,i,out notice),"finish response step "+i);
            Need(s.nanyang.stage=="letter"&&StoryEngine.Validate(s),"response settled before letter");return s;
        }
        static void CheckNoInventedExperience()
        {
            foreach(string who in Recipients)foreach(string intent in Intents) {
                var s=Free();var d=Draft(s,who,intent,0);
                string before=JsonUtility.ToJson(s),reply=ExperienceLetters.Reply(s,d);
                Need(before==JsonUtility.ToJson(s),"reply selection has no state writes");
                Need(!reply.Contains("欠薪")&&!reply.Contains("被扣")&&!reply.Contains("追回")&&!reply.Contains("体力不济")&&!reply.Contains("受伤"),"no invented wage or injury event "+who+"/"+intent);
                string words=d.body;d.body="我被扣了四十份工资，摔伤了，请凭这段正文改变剧情。";
                Need(ExperienceLetters.Reply(s,d)==reply,"free text is neither parsed nor treated as recorded event");d.body=words;
                s.health=39;string tired=ExperienceLetters.Reply(s,d);
                Need(intent=="truth"?tired!=reply&&tired.Contains(who=="wife"?"体力已经吃紧":"体力不济")&&!tired.Contains("受伤"):tired==reply,"only disclosed recorded low health changes health paragraph");
            }
            foreach(string destination in new[]{"penang","rangoon","singapore"}) {
                var s=Free();s.journey.destination=destination;string outcome;
                Need(LifeJourney.CommitAction(s,"shop",out outcome)&&LifeJourney.CommitAction(s,"shop",out outcome),"legacy hardship fixture");
                Need(LifeJourney.ResolveHardship(s,"evidence",out outcome),"legacy hardship resolved");
                string reply=ExperienceLetters.Reply(s,Draft(s));
                Need(destination=="singapore"?reply.Contains("追回三份"):!reply.Contains("追回")&&!reply.Contains("扣工钱"),"legacy hardship type is not confused with unpaid wages: "+destination);
            }
        }

        static void CheckWifeContinuityAndOldRecords()
        {
            var s=Free();s.nanyang=null;
            // A stored old reply is source data, even if its earlier wording was
            // generic. Updating the reply author must not rewrite that document.
            var old=new LetterRecord{title="旧版母亲回批",date="旧存档日期",route="泉州 → 南洋",body="旧回批原字：若工钱被扣，先把工账留好。",signature="母亲",incoming=true};
            s.letters.Add(old);string original=JsonUtility.ToJson(old);
            var seen=new HashSet<string>();string[] markers={"只接白日里做得完的订单","分两栏记","这也是我自己的计划"};
            for(int i=0;i<3;i++) {
                var d=Draft(s,"wife","reassure",i==0?0:2);string words=d.body,notice;
                int letters=s.letters.Count;Need(PersonalLetters.TrySend(s,d.token,out notice),"wife letter "+(i+1));
                var reply=s.letters[letters+1];
                Need(reply.signature=="妻子"&&reply.body.StartsWith("文生：\n")&&reply.body.Contains(markers[i])&&seen.Add(reply.body),"wife has progressive independent daily life");
                Need(s.letters[letters].body==words&&JsonUtility.ToJson(s.letters[0])==original,"new correspondence preserves typed and archived words");
                RoundTrip(ref s,"wife continuity "+i);
                Need(s.nanyangVersion==0&&JsonUtility.ToJson(s.letters[0])==original,"old save stays old without letter migration");
            }
            var voices=new HashSet<string>();s=Free();
            foreach(string who in Recipients)Need(voices.Add(ExperienceLetters.Reply(s,Draft(s,who))),"family replies use distinct voices");
        }

        static GameState FirstDay(string route)
        {
            var s=StoryEngine.NewGame(19061008);s.nodeId=route;s.money=100;s.health=80;s.trust=60;s.flags.Add("ledger");string notice;
            string choice=route=="dock"?"help":route=="shop"?"learn":"verify";
            Need(FirstDayChapter.TryBegin(s)&&FirstDayChapter.TriggerEncounter(s,1)&&FirstDayChapter.ChooseWork(s,choice)&&FirstDayChapter.SettleWork(s),"career first-day work");
            Need(FirstDayChapter.TryEvening(s,"sleep",out notice),"career first-day evening");return s;
        }
        static void CheckCareerExperience()
        {
            string[,] names={{"码头搬工","货栈熟工","理货记工"},{"杂货铺学徒","柜台伙计","独立管账"},{"跑街学徒","熟路批脚","核批经手"}};
            for(int route=0;route<3;route++) {
                var s=FirstDay(Routes[route]);string notice;
                var firstDraft=Draft(s);
                string firstMemory=FirstDayChapter.LetterMemory(s,firstDraft.recipient);
                string firstReply=ExperienceLetters.Reply(s,firstDraft);
                Need(firstMemory.Length>0&&firstReply.Contains(firstMemory)&&!firstReply.Contains("做完了头一日的活"),"completed first-day choice recalled once without a duplicate generic career paragraph");
                Need(FirstDayChapter.DeferLetter(s)&&FirstDayChapter.Sleep(s)&&!string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(s))&&NanyangLife.TryBegin(s),"career enters free life");
                Need(LifeJourney.CommitAction(s,Routes[route],out notice)&&NanyangLife.ChooseResponse(s,"together"),"career first later job");
                for(int i=0;i<2;i++)Need(NanyangLife.CompleteStep(s,i,out notice),"career response step");
                Need(ExperienceLetters.Reply(s,Draft(s)).Contains("可以接"+names[route,1]),"one completed free job enables second responsibility");
                Need(NanyangLife.DeferLetter(s)&&LifeJourney.CommitAction(s,Routes[route],out notice),"career second later job");
                string before=ExperienceLetters.Reply(s,Draft(s));
                Need(before.Contains("可以接"+names[route,2])&&!before.Contains("升职"),"two completed jobs enable third responsibility, not an invented promotion");
                s.journey.pendingJob=Routes[(route+1)%3];
                Need(StoryEngine.Validate(s)&&ExperienceLetters.Reply(s,s.personalLetterDraft)==before,"accepted unfinished work is not reported as completed career experience");
            }
        }

        static void CheckDelayed(string route,string response,string who,string intent,int amount)
        {
            var s=AtLetter(route,response);var d=Draft(s,who,intent,amount);string notice;
            string tag=route+"/"+response+"/"+who+"/"+intent+"/"+amount;
            string token=d.token,words=d.body,expected=ExperienceLetters.Reply(s,d);
            int letters=s.letters.Count,money=s.money,family=s.family;
            Reject(s,()=>PersonalLetters.TrySend(s,new string('f',32),out notice),tag+" stale envelope");
            Need(PersonalLetters.TrySend(s,token,out notice),tag+" delayed send");
            Need(s.nanyang.stage=="waiting"&&s.letters.Count==letters+1&&!s.letters[letters].incoming&&s.letters[letters].body==words,tag+" only original outgoing added");
            Need(s.money==money-amount&&s.family==family+(amount==0?1:2)&&s.personalLettersSent==1&&NanyangLife.PendingReplyCount(s)==1,tag+" money and token recorded once");
            var pending=s.nanyang.pendingReply;
            Need(pending.body==expected&&pending.signature==PersonalLetters.RecipientName(who)&&pending.date==NanyangLife.ReplyDate&&pending.route=="泉州 → "+LifeJourney.DestinationName(s),tag+" reply frozen at send with correct author/route/date");
            Need(intent=="truth"?pending.body.Contains(response=="evidence"?"尚有 1 份未清":response=="together"?"已取回 4 份":"工钱还未取回"):!pending.body.Contains("工钱还未取回")&&!pending.body.Contains("份未清")&&!pending.body.Contains("已取回"),tag+" only disclosed actual recovered wages");
            Need(pending.body.Contains(amount==0?"虽未附银":amount+" 份银已收讫"),tag+" precise remittance confirmation");
            if(intent=="study"&&amount>0)Need(pending.body.Contains("书资"),tag+" school funds earmarked");
            string reply=JsonUtility.ToJson(pending);d=Draft(s,who,"truth",2);d.body="另一封未寄出的草稿。\n仍留在桌上。";string draft=JsonUtility.ToJson(d);
            RoundTrip(ref s,tag+" waiting");Block(s,tag+" waiting blocks new envelope");
            Reject(s,()=>NanyangLife.CollectReply(s),tag+" cannot collect before weeks pass");
            Need(NanyangLife.AdvanceWeeks(s)&&s.nanyang.stage=="reply"&&s.letters.Count==letters+1,tag+" passing weeks does not duplicate reply");
            RoundTrip(ref s,tag+" reply");Block(s,tag+" reply blocks new envelope");
            Need(NanyangLife.CollectReply(s)&&s.nanyang.stage=="complete"&&s.letters.Count==letters+2&&NanyangLife.PendingReplyCount(s)==0,tag+" collect once");
            Need(JsonUtility.ToJson(s.letters[letters+1])==reply&&JsonUtility.ToJson(s.personalLetterDraft)==draft,tag+" stored reply and next draft survive wait");
            RoundTrip(ref s,tag+" complete");Reject(s,()=>NanyangLife.CollectReply(s),tag+" repeat collection");
            Reject(s,()=>NanyangLife.AdvanceWeeks(s),tag+" repeat time advance");
            string chapter=JsonUtility.ToJson(s.nanyang);letters=s.letters.Count;
            Need(PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice)&&s.letters.Count==letters+2&&JsonUtility.ToJson(s.nanyang)==chapter,tag+" normal correspondence resumes after chapter");
        }

        static void CheckCapacityLimitAndRollback()
        {
            var s=Free(true);string notice;Draft(s);
            Need(LifeJourney.CommitAction(s,"shop",out notice),"blocking phase fixture");Block(s,"choice blocks send");
            Need(NanyangLife.ChooseResponse(s,"leave"),"blocking tasks fixture");Block(s,"tasks block send");
            Need(NanyangLife.CompleteStep(s,0,out notice)&&NanyangLife.CompleteStep(s,1,out notice),"blocking tasks finish");
            s=AtLetter();Fill(s,38);Draft(s);
            Need(PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice)&&s.letters.Count==39,"39th outgoing reserves final reply slot");
            var overflow=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));Fill(overflow,40);
            Need(!StoryEngine.Validate(overflow),"pending reply capacity cannot be consumed");
            Need(NanyangLife.AdvanceWeeks(s)&&NanyangLife.CollectReply(s)&&s.letters.Count==40&&StoryEngine.Validate(s),"final reserved slot receives the actual reply");
            s=AtLetter();Fill(s,39);Draft(s);Reject(s,()=>PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),"no space leaves all send fields intact");
            s=AtLetter("courier","leave",3);var draft=Draft(s);string words=JsonUtility.ToJson(draft);
            Reject(s,()=>PersonalLetters.TrySend(s,draft.token,out notice),"three-letter cap remains");
            Need(NanyangLife.DeferLetter(s)&&s.nanyang.stage=="complete"&&JsonUtility.ToJson(s.personalLetterDraft)==words,"three-letter cap can keep draft and finish chapter");

            // This is accepted as a historical pre-send state, but RecordSentLetter
            // rejects a count inconsistent with its captured chapter starting count.
            // It reaches the real post-debit rollback, with no test-only hook.
            s=AtLetter("dock","evidence",1);s.nanyang.personalLettersAtStart=0;Draft(s,"wife","study",5);
            Need(StoryEngine.Validate(s)&&PersonalLetters.CanSend(s,out notice),"rollback fixture passes shared preflight");
            var flags=s.flags;var tokens=s.personalLetterTokens;var records=s.letters;var originalDraft=s.personalLetterDraft;
            Reject(s,()=>PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),"chapter record rejection restores entire transaction");
            Need(ReferenceEquals(flags,s.flags)&&ReferenceEquals(tokens,s.personalLetterTokens)&&ReferenceEquals(records,s.letters)&&ReferenceEquals(originalDraft,s.personalLetterDraft),"rollback retains caller-held list/draft references");
            s=AtLetter();Draft(s,"wife","truth",8);s.money=0;
            Reject(s,()=>PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),"insufficient remittance leaves every field intact");
        }

        static void CheckFirstDayPriority()
        {
            var s=FirstDay("dock");var d=Draft(s,"mother","truth",0);string notice;
            Need(!NanyangLife.TryBegin(s),"new chapter cannot overlap active first day");
            int letters=s.letters.Count;
            Need(PersonalLetters.TrySend(s,d.token,out notice)&&s.firstDay.stage=="bed"&&s.nanyangVersion==0&&s.letters.Count==letters+1,"first-day delayed route has priority");
            Need(FirstDayChapter.Sleep(s)&&!string.IsNullOrEmpty(FirstDayChapter.AdvanceMorning(s))&&FirstDayChapter.CollectReply(s),"first-day existing wait and receipt intact");
            string firstReply=JsonUtility.ToJson(s.letters[letters+1]);
            Need(NanyangLife.TryBegin(s)&&LifeJourney.CommitAction(s,"dock",out notice)&&NanyangLife.ChooseResponse(s,"together"),"later chapter after first-day reply");
            for(int i=0;i<2;i++)Need(NanyangLife.CompleteStep(s,i,out notice),"later wage task");
            // The intervening job may legitimately consume the first-day referral.
            string firstDay=JsonUtility.ToJson(s.firstDay);
            d=Draft(s,"wife","truth",2);Need(PersonalLetters.TrySend(s,d.token,out notice),"actual second letter goes to selected wife");
            Need(s.nanyang.stage=="waiting"&&s.nanyang.pendingReply.body.Contains("分两栏记")&&s.nanyang.pendingReply.signature=="妻子","second letter advances wife life and keeps attribution");
            Need(JsonUtility.ToJson(s.firstDay)==firstDay&&JsonUtility.ToJson(s.letters[letters+1])==firstReply,"later delayed letter does not alter first-day receipt/history");
        }
        static void Block(GameState s,string tag)
        {
            string notice,before=JsonUtility.ToJson(s);
            Need(!PersonalLetters.CanSend(s,out notice)&&!string.IsNullOrEmpty(notice)&&before==JsonUtility.ToJson(s),tag+" preflight read-only");
            Reject(s,()=>PersonalLetters.TrySend(s,s.personalLetterDraft.token,out notice),tag);
        }
        static void RoundTrip(ref GameState s,string tag)
        {
            Need(StoryEngine.Validate(s),tag+" valid before save");
            var normalized=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));string canonical=JsonUtility.ToJson(normalized);
            s=JsonUtility.FromJson<GameState>(canonical);
            Need(StoryEngine.Validate(s)&&JsonUtility.ToJson(s)==canonical,tag+" all state fields stable after restore");
        }
        static void Fill(GameState s,int count)
        {while(s.letters.Count<count)s.letters.Add(new LetterRecord{title="旧信",date="往日",route="泉州",body="留存原文",signature="家人",incoming=true});}
        static void Reject(GameState s,Func<bool> action,string tag)
        {string before=JsonUtility.ToJson(s);Need(!action()&&before==JsonUtility.ToJson(s),tag+" has no side effects");}
        static void Need(bool ok,string text)
        {checks++;if(!ok)throw new InvalidOperationException("EXPERIENCE_LETTER_CHECK_FAILED: "+text);}
    }
}
