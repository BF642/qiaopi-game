using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi.Editor
{
    // Real in-memory transactions: concealment, shared home memories and held
    // replies must remain correct through serialization and later decisions.
    public static class ChapterLetterChecks
    {
        static int checks;
        static readonly string[] Recipients={"mother","wife","sister"};
        static readonly string[] Intents={"truth","reassure","study"};
        public static void Run()
        {
            checks=0;
            HomeMemories(); HomeMemoryProgression(); Disclosure(); FirstDayChoices(); Endure(); FrozenReply();
            Debug.Log("CHAPTER_LETTER_CHECKS_PASSED: actual home memories, nine first-day choices, 3 recipients × 3 intentions × 4 amounts, hidden hardships excluded, 3 careers endure without duplicate payment, frozen delayed replies and original text; assertions="+checks+". No player files or preferences used.");
        }
        static GameState Fresh()
        {
            var s=StoryEngine.NewGame(2500);s.nodeId="first_pay";s.money=100;s.health=80;s.trust=50;
            Need(StoryEngine.Validate(s),"fresh fixture");return s;
        }
        static GameState Dispute(string route="dock",string response="endure")
        {
            var s=Fresh();string notice;Need(NanyangLife.TryBegin(s),"enrol");
            Need(LifeJourney.CommitAction(s,route,out notice)&&NanyangLife.ChooseResponse(s,response),"settle work and select response");
            int steps=NanyangLife.RequiredSteps(s);
            for(int i=0;i<steps;i++)Need(NanyangLife.CompleteStep(s,i,out notice),"response actual step");
            Need(StoryEngine.Validate(s),"response valid");return s;
        }
        static PersonalLetterDraft Draft(GameState s,string who="wife",string intent="truth",int amount=0)
        {
            var d=PersonalLetters.EnsureDraft(s);d.recipient=who;d.intent=intent;d.amount=amount;
            d.body="我自己写的字：\n<原文> 今天的事，慢慢告诉你。";return d;
        }
        static GameState Home(string purchase,string word)
        {
            var s=StoryEngine.NewGame(2500);s.nodeId="peace";s.homeChapterVersion=1;s.homeChapter=HomeChapter.Create();
            foreach(string action in new[]{"walk_market",purchase,"put_basket","lift_grain","store_grain",word,"share_meal"})
                Need(HomeChapter.TryAct(s,action),"home interaction "+action);
            s.nodeId="first_pay";Need(StoryEngine.Validate(s),"completed home memory valid");return s;
        }
        static void HomeMemories()
        {
            var ordinary=Fresh();ordinary.homeChapterVersion=0;
            foreach(string who in Recipients) {
                string text=ExperienceLetters.Reply(ordinary,Draft(ordinary,who,"reassure"));
                Need(!text.Contains("那包茶")&&!text.Contains("端过的簸箕")&&!text.Contains("你陪我写"),"old saves do not invent shared activity");
            }
            foreach(string purchase in new[]{"rice","tea"})foreach(string word in new[]{"home","return"}) {
                var s=Home(purchase,word);
                string wife=ExperienceLetters.Reply(s,Draft(s,"wife","reassure"));
                Need(wife.Contains(purchase=="tea"?"那包茶":"一道提篮子回家"),"wife remembers actual purchase");
                Need(!wife.Contains(purchase=="tea"?"一道提篮子回家":"那包茶"),"reply does not replace the player's purchase with the other branch");
                Need(ExperienceLetters.Reply(s,Draft(s,"mother","study")).Contains("端过的簸箕"),"mother remembers stored grain");
                Need(ExperienceLetters.Reply(s,Draft(s,"sister","reassure")).Contains(word=="home"?"你陪我写的“家”":"你陪我写的“归”"),"sister remembers chosen character");
                var copy=Reload(s);Need(ExperienceLetters.Reply(copy,copy.personalLetterDraft)==ExperienceLetters.Reply(s,s.personalLetterDraft),"home memory survives restore");
            }
            var pending=StoryEngine.NewGame(2500);pending.nodeId="peace";pending.homeChapterVersion=1;pending.homeChapter=HomeChapter.Create();
            Need(HomeChapter.TryAct(pending,"walk_market")&&HomeChapter.TryAct(pending,"tea"),"picked up but not delivered tea");
            Need(!ExperienceLetters.Reply(pending,Draft(pending)).Contains("那包茶"),"unfinished purchase cannot become completed shared memory");
        }
        static void HomeMemoryProgression()
        {
            foreach(string purchase in new[]{"rice","tea"})foreach(string word in new[]{"home","return"})foreach(string who in Recipients) {
                var s=Home(purchase,word);s.money=100;
                var first=Draft(s,who,"reassure",0);string notice,original=first.body;
                Need(PersonalLetters.TrySend(s,first.token,out notice),"dispatch remembered first letter");
                string earlier=s.letters[s.letters.Count-1].body;
                Need(s.letters[s.letters.Count-2].body==original,"home recollection never rewrites player text");
                s=Reload(s);
                var second=Draft(s,who,"reassure",0);
                Need(PersonalLetters.TrySend(s,second.token,out notice),"dispatch later letter");
                string later=s.letters[s.letters.Count-1].body;
                Need(earlier!=later&&s.letters[s.letters.Count-3].body==earlier,"later household life progresses without rewriting the earlier reply");
                if(who=="wife") {
                    Need(later.Contains(purchase=="tea"?"那包茶早已喝完":"一起提篮子回家"),"wife retains purchase while household time moves on");
                    Need(!later.Contains("还留着一点"),"finite food does not remain unfinished forever");
                }
                if(who=="sister")Need(later.Contains(word=="return"?"“归”":"“家”")&&later.Contains("不用照着描了"),"sister learns beyond the first tracing");
                if(who=="mother")Need(later.Contains("阿满替我扶着瓮口"),"mother has practical help beyond repeating her son's gesture");
                Need(!later.Contains("工钱还未取回")&&!later.Contains("体力不济")&&!later.Contains("体力已经吃紧"),"shared family memory reveals no concealed overseas hardship");
                Need(StoryEngine.Validate(s),"both letters leave valid gameplay state");
            }
        }
        static void Disclosure()
        {
            var s=Dispute("shop","together");s.health=31;
            foreach(string who in Recipients)foreach(string intent in Intents)foreach(int amount in PersonalLetters.Amounts) {
                var d=Draft(s,who,intent,amount);string text=ExperienceLetters.Reply(s,d),before=JsonUtility.ToJson(s);
                if(intent=="truth")Need(text.Contains("已取回 4 份")&&text.Contains(who=="wife"?"体力已经吃紧":"体力不济"),"truth discloses actual hardship and health");
                else Need(!text.Contains("已取回")&&!text.Contains("工钱还未取回")&&!text.Contains("体力已经吃紧")&&!text.Contains("体力不济")&&!text.Contains("已做过"),"family knows no hidden overseas experience");
                Need(text.Contains(amount==0?"虽未附银":amount+" 份银已收讫"),"correct remittance statement");
                Need(before==JsonUtility.ToJson(s),"reply read-only");
                d.body="我曾被骗，今天病倒了。请按这段正文给我回信。";
                Need(ExperienceLetters.Reply(s,d)==text,"free text neither parsed nor sent to a model");
            }
            foreach(string who in Recipients) {
                var distinct=new HashSet<string>();
                foreach(int amount in PersonalLetters.Amounts) {
                    string text=ExperienceLetters.Reply(s,Draft(s,who,"study",amount));
                    // Strip the figure, leaving the authored use of each amount.
                    foreach(int n in PersonalLetters.Amounts)text=text.Replace(n+" 份银已收讫", "银已收讫");
                    Need(distinct.Add(text),"amount changes household use, not only printed number");
                }
            }
        }
        static void FirstDayChoices()
        {
            foreach(string route in new[]{"dock","shop","courier"}) {
                string[] choices=route=="dock"?new[]{"extra","help","tally"}:route=="shop"?new[]{"learn","wages","read"}:new[]{"verify","rush","craft"};
                var voices=new HashSet<string>();
                foreach(string choice in choices) {
                    var s=Fresh();s.nodeId=route;s.flags.Add("ledger");
                    Need(FirstDayChapter.TryBegin(s)&&FirstDayChapter.TriggerEncounter(s,1)&&FirstDayChapter.ChooseWork(s,choice)&&FirstDayChapter.SettleWork(s),"actual first-day choice "+choice);
                    string notice;Need(FirstDayChapter.TryEvening(s,"sleep",out notice),"reach letter desk");
                    string factual=FirstDayChapter.LetterMemory(s,"wife");
                    Need(factual.Length>0&&voices.Add(factual),"first-day choices produce distinct facts");
                    Need(ExperienceLetters.Reply(s,Draft(s)).Contains(factual),"truth shares chosen first-day fact");
                    Need(!ExperienceLetters.Reply(s,Draft(s,"wife","reassure")).Contains(factual),"reassurance does not disclose that fact");
                }
            }
        }
        static void Endure()
        {
            foreach(string route in new[]{"dock","shop","courier"}) {
                var s=Fresh();string notice;Need(NanyangLife.TryBegin(s)&&LifeJourney.CommitAction(s,route,out notice),"endure work "+route);
                int money=s.money,health=s.health,trust=s.trust;
                Need(NanyangLife.ChooseResponse(s,"endure")&&NanyangLife.RequiredSteps(s)==1,"endure real interaction");
                var step=NanyangTasks.Step(s);Need(!step.inspect&&step.world==NanyangTasks.RouteWorld(route)&&step.body.Contains(s.nanyang.paidWage+" 份"),"endure visits original employer and exact paid sum");
                s=Reload(s);Need(NanyangLife.CompleteStep(s,0,out notice),"endure settles");
                Need(s.money==money&&s.health==health-2&&s.trust==trust&&s.nanyang.recovered==0,"no fake recovery or duplicate initial wage");
                Need(s.journey.log.Count==2&&s.journey.turns==1&&s.nanyang.stage=="letter","one hardship record, no extra work turn");
                string before=JsonUtility.ToJson(s);Need(!NanyangLife.CompleteStep(s,0,out notice)&&JsonUtility.ToJson(s)==before,"repeat step cannot duplicate effects");
                Need(NanyangLife.DeferLetter(s),"endure may keep draft");s=Reload(s);
                Need(WorkRelationshipText.WageMemory(s).Contains("那四份还欠着"),"coworker remembers unpaid balance and offers support");
                Need(NanyangLife.TransferBonusForWork(s,route=="shop"?"courier":"shop")==0,"endure does not steal leave-path bonus");
                Need(LifeJourney.CommitAction(s,route,out notice)&&StoryEngine.Validate(s),"endure can continue working after own choice");
            }
        }
        static void FrozenReply()
        {
            foreach(string intent in new[]{"truth","reassure"}) {
                var s=Dispute();var d=Draft(s,"wife",intent,2);string body=d.body,notice;int letters=s.letters.Count;
                Need(PersonalLetters.TrySend(s,d.token,out notice),"dispatch chosen disclosure");string frozen=JsonUtility.ToJson(s.nanyang.pendingReply);
                Need(s.letters[letters].body==body,"typed body preserved exactly");
                // These legitimate values may change while a letter is travelling.
                s.health=15;s.personalLetterDraft.intent=intent=="truth"?"reassure":"truth";s.personalLetterDraft.body="另一封未寄出的草稿";
                s=Reload(s);Need(NanyangLife.AdvanceWeeks(s)&&NanyangLife.CollectReply(s),"collect after weeks");
                Need(JsonUtility.ToJson(s.letters[letters+1])==frozen,"later state and draft cannot rewrite dispatched reply");
                string before=JsonUtility.ToJson(s);Need(!NanyangLife.CollectReply(s)&&before==JsonUtility.ToJson(s),"reply cannot duplicate");
            }
        }
        static GameState Reload(GameState s)
        {
            Need(StoryEngine.Validate(s),"valid before restore");var copy=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));
            Need(StoryEngine.Validate(copy),"valid after restore");return copy;
        }
        static void Need(bool value,string message)
        {checks++;if(!value)throw new InvalidOperationException("CHAPTER_LETTER_CHECK_FAILED: "+message);}
    }
}
