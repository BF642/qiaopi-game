using System;
using UnityEngine;

namespace Qiaopi.Editor
{
    // Pure-rule checks. Serialization is kept in memory; never reads a player save.
    public static class NanyangChecks
    {
        static int assertions, roundTrips;
        public static void Run()
        {
            assertions = roundTrips = 0;
            Legacy();
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
                foreach (string route in new[] { "dock", "shop", "courier" })
                    foreach (string response in new[] { "evidence", "together", "leave", "endure" }) {
                        Branch(destination, route, response, false);
                        Branch(destination, route, response, true);
                    }
            CareerGrowth(); MealAndRest(); Capacity(); Transfer(); FirstDayReferral(); Invalid();
            Debug.Log("NANYANG_CHECKS_PASSED: 3 destinations × 3 careers × 4 responses × send/defer, step/save idempotency, 3 career levels, six-turn cap, real withheld wages, meals/rest, delayed replies, transfer bonus and version-zero compatibility; assertions=" + assertions + ", in-memory save round-trips=" + roundTrips + ". Player saves/preferences untouched.");
        }
        static void Require(bool condition, string message)
        { assertions++; if (!condition) throw new Exception("Nanyang checks: " + message); }
        static string Snapshot(GameState s) { return JsonUtility.ToJson(s); }
        static void Reject(GameState s, Func<bool> action, string message)
        { string before = Snapshot(s); Require(!action(), message + " rejects"); Require(before == Snapshot(s), message + " leaves state intact"); }
        static void Reload(ref GameState s, string message)
        {
            Require(StoryEngine.Validate(s), message + " valid before save");
            s = JsonUtility.FromJson<GameState>(Snapshot(s)); roundTrips++;
            Require(StoryEngine.Validate(s) && NanyangLife.Validate(s), message + " valid after save");
        }
        static GameState Fresh(string destination = "singapore")
        {
            var s = StoryEngine.NewGame(2100); s.nodeId = "first_pay"; s.journey.destination = destination;
            s.money = 100; s.health = 80; s.family = 50; s.trust = 50;
            Require(StoryEngine.Validate(s), "baseline valid");
            Require(NanyangLife.TryBegin(s), "begin");
            return s;
        }
        static GameState AtLetter(string route = "shop", string response = "evidence")
        {
            var s = Fresh(); string notice;
            Require(LifeJourney.CommitAction(s, route, out notice), "fixture work");
            Require(NanyangLife.ChooseResponse(s, response), "fixture response");
            int count = NanyangLife.RequiredSteps(s);
            for (int i = 0; i < count; i++) Require(NanyangLife.CompleteStep(s, i, out notice), "fixture step");
            return s;
        }
        static void Branch(string destination, string route, string response, bool send)
        {
            string tag = destination + "/" + route + "/" + response + (send ? "/send" : "/defer");
            var s = Fresh(destination); string notice;
            int storyCount = s.decisions, historyCount = s.history.Count;
            Reject(s, () => NanyangLife.TryBegin(s), tag + " cannot re-enrol");
            Reject(s, () => LifeJourney.Finish(s), tag + " cannot skip work");
            Require(NanyangLife.CareerLevel(s, route) == 1, tag + " starts first career level");
            s.journey.pendingJob = route; Reload(ref s, tag + " pending physical work");
            int pay = route == "dock" ? 10 : route == "shop" ? 7 : 6;
            int fatigue = route == "dock" ? 10 : route == "shop" ? 4 : 6;
            int credit = route == "dock" ? 1 : route == "shop" ? 4 : 6;
            Require(LifeJourney.CommitAction(s, route, out notice), tag + " first work pays");
            Require(s.money == 100 + pay - 4 && s.health == 80 - fatigue && s.trust == 50 + credit, tag + " actual first wages withheld without old extra damage");
            Require(s.nanyang.stage == "choice" && s.nanyang.wageRoute == route && s.nanyang.wageTurn == 1 && s.nanyang.promisedWage == pay && s.nanyang.paidWage == pay - 4 && s.nanyang.owed == 4, tag + " original wage snapshot");
            Require(s.journey.turns == 1 && s.journey.log.Count == 1 && s.journey.hardshipId == destination && !s.journey.hardshipResolved && s.journey.pendingJob == "", tag + " single trigger");
            Require(LifeJourney.PendingHardship(s) == null && LifeJourney.Actions(s).Count == 0 && NanyangLife.Choices(s).Count == 4, tag + " only new panel can respond");
            Reject(s, () => LifeJourney.CommitAction(s, route, out notice), tag + " blocked work cannot repay");
            Reject(s, () => NanyangLife.TryMeal(s, out notice), tag + " blocked meal");
            Reject(s, () => NanyangLife.Visit(s, "market"), tag + " blocked visit");
            Reload(ref s, tag + " choice");
            Reject(s, () => NanyangLife.ChooseResponse(s, "mutual_aid"), tag + " legacy response not accepted");
            int money = s.money, health = s.health, trust = s.trust;
            Require(NanyangLife.ChooseResponse(s, response), tag + " response chosen");
            Require(s.money == money && s.health == health && s.trust == trust, tag + " choosing does not settle");
            Reject(s, () => NanyangLife.ChooseResponse(s, response), tag + " duplicate response");
            int steps = response == "endure" ? 1 : response == "evidence" ? 3 : 2;
            Require(NanyangLife.RequiredSteps(s) == steps, tag + " authored physical steps");
            for (int i = 0; i < steps; i++) {
                int step = i;
                Reject(s, () => NanyangLife.CompleteStep(s, step + 1, out notice), tag + " out-of-order step");
                Require(NanyangLife.CompleteStep(s, step, out notice), tag + " physical step " + step);
                if (i < steps - 1) Require(s.money == money && s.health == health && s.trust == trust && !s.journey.hardshipResolved, tag + " no premature pay");
                Reject(s, () => NanyangLife.CompleteStep(s, step, out notice), tag + " replayed step");
                Reload(ref s, tag + " task progress " + (i + 1));
            }
            int recovered = response == "evidence" ? 3 : response == "together" ? 4 : 0;
            int healthChange = response == "evidence" || response == "endure" ? -2 : response == "together" ? -4 : 6;
            int trustChange = response == "evidence" ? 8 : response == "together" ? 5 : response == "endure" ? 0 : 3;
            Require(s.nanyang.stage == "letter" && s.nanyang.recovered == recovered && s.money == money + recovered && s.health == health + healthChange && s.trust == trust + trustChange, tag + " exact response rewards");
            Require(s.journey.hardshipResolved && s.journey.log.Count == 2 && s.journey.turns == 1, tag + " only one hardship log; no extra work turn");
            if (send) {
                var draft = PersonalLetters.EnsureDraft(s); draft.body = "把这几日的工钱与难处告诉家里。"; draft.recipient = response == "evidence" ? "mother" : response == "together" ? "wife" : "sister"; draft.intent = "truth"; draft.amount = 2;
                string token = draft.token; int letters = s.letters.Count; money = s.money;
                Require(PersonalLetters.TrySend(s, token, out notice), tag + " outgoing letter");
                Require(s.nanyang.stage == "waiting" && s.nanyang.letterSent && s.money == money - 2 && s.letters.Count == letters + 1 && !s.letters[s.letters.Count - 1].incoming && NanyangLife.PendingReplyCount(s) == 1, tag + " reply stays offstage");
                Require(s.nanyang.pendingReply.date == NanyangLife.ReplyDate && s.nanyang.pendingReply.body.Contains(recovered > 0 ? "已取回 " + recovered + " 份" : "工钱还未取回"), tag + " delayed reply remembers actual recovery");
                Reload(ref s, tag + " waiting");
                Reject(s, () => PersonalLetters.TrySend(s, token, out notice), tag + " old token cannot resend");
                Reject(s, () => NanyangLife.CollectReply(s), tag + " cannot collect before weeks pass");
                Require(NanyangLife.AdvanceWeeks(s), tag + " explicit weeks transition");
                Require(s.letters.Count == letters + 1 && s.nanyang.stage == "reply", tag + " transition does not collect");
                Reload(ref s, tag + " reply available");
                Reject(s, () => NanyangLife.AdvanceWeeks(s), tag + " repeated weeks transition");
                Require(NanyangLife.CollectReply(s), tag + " receive at desk");
                Require(s.letters.Count == letters + 2 && s.nanyang.replyCollected && NanyangLife.PendingReplyCount(s) == 0, tag + " exactly one reply");
                Reload(ref s, tag + " collected");
                Reject(s, () => NanyangLife.CollectReply(s), tag + " cannot duplicate received letter");
            } else {
                PersonalLetters.EnsureDraft(s).body = "暂未写完的家书"; string draft = JsonUtility.ToJson(s.personalLetterDraft);
                Require(NanyangLife.DeferLetter(s), tag + " defer");
                Require(draft == JsonUtility.ToJson(s.personalLetterDraft) && s.personalLettersSent == 0 && s.letters.Count == 0, tag + " retains draft without fake letter");
                Reload(ref s, tag + " defer save");
                Reject(s, () => NanyangLife.DeferLetter(s), tag + " cannot defer twice");
            }
            Require(s.nanyang.stage == "complete" && !NanyangLife.IsBlocking(s) && !LifeJourney.CanFinish(s), tag + " chapter done but minimum work interval remains");
            Require(LifeJourney.CommitAction(s, "rest", out notice), tag + " second round");
            Require(s.journey.turns == 2 && s.nanyang.recovered == recovered && s.journey.hardshipResolved, tag + " no old turn-two event");
            Require(LifeJourney.CommitAction(s, route, out notice), tag + " third round");
            Require(LifeJourney.CanFinish(s) && LifeJourney.Finish(s), tag + " may advance after three rounds");
            Reload(ref s, tag + " life complete");
            Require(s.decisions == storyCount && s.history.Count == historyCount, tag + " story counters untouched");
            Require(StoryEngine.Choose(s, "rest") && StoryEngine.Continue(s) && StoryEngine.Validate(s), tag + " later main story remains valid");
            Reload(ref s, tag + " next story node");
        }
        static void CareerGrowth()
        {
            foreach (string route in new[] { "dock", "shop", "courier" }) {
                var s = AtLetter(route, "leave"); Require(NanyangLife.DeferLetter(s), "growth defer"); string notice;
                Require(NanyangLife.CareerLevel(s, route) == 2, "second level after first completed work");
                int pay2 = route == "dock" ? 12 : route == "shop" ? 10 : 9;
                int pay3 = route == "dock" ? 14 : route == "shop" ? 13 : 12;
                var second = LifeJourney.Actions(s).Find(a => a.id == route); int money = s.money;
                Require(second.money == pay2 && LifeJourney.CommitAction(s, route, out notice) && s.money == money + pay2, "second-level wages");
                Require(NanyangLife.CareerLevel(s, route) == 3, "third level after two works");
                var third = LifeJourney.Actions(s).Find(a => a.id == route); money = s.money;
                Require(third.title != second.title && third.description != second.description && third.money == pay3, "third-level role actually differs");
                Require(LifeJourney.CommitAction(s, route, out notice) && s.money == money + pay3, "third-level wages");
                Require(!LifeJourney.Actions(s).Find(a => a.id == route).enabled, "same work capped at three");
                for (int i = 0; i < 3; i++) Require(LifeJourney.CommitAction(s, "rest", out notice), "fill remaining round");
                Require(s.journey.turns == 6 && LifeJourney.Actions(s).Count == 0 && LifeJourney.CanFinish(s), "six-round hard cap");
                Reject(s, () => LifeJourney.CommitAction(s, "rest", out notice), "cannot add seventh round");
                Reload(ref s, "six rounds");
            }
        }
        static void MealAndRest()
        {
            var s = Fresh(); string notice; int health = s.health;
            Require(s.nanyang.visitingWorld == "quarters", "starts visiting shared quarters");
            foreach (string world in new[] { "port", "market", "postoffice", "quarters" }) {
                Require(NanyangLife.Visit(s, world) && s.nanyang.visitingWorld == world && s.money == 100 && s.health == health && s.journey.turns == 0, "free visit changes no resources or turns");
                Reload(ref s, "visiting " + world);
            }
            Reject(s, () => NanyangLife.Visit(s, "quanzhou"), "cannot visit outside this chapter");
            Require(NanyangLife.TryMeal(s, out notice) && s.money == 98 && s.health == health + 8 && s.journey.turns == 0, "meal is paid and adds no round");
            Reload(ref s, "meal"); Reject(s, () => NanyangLife.TryMeal(s, out notice), "same-turn meal replay");
            s.journey.pendingJob = "rest"; Reload(ref s, "new physical bed pending");
            Reject(s, () => NanyangLife.Visit(s, "market"), "pending job owns destination");
            Reject(s, () => NanyangLife.TryMeal(s, out notice), "pending bed blocks meal");
            Reject(s, () => LifeJourney.CommitAction(s, "shop", out notice), "different job cannot replace pending bed");
            Require(LifeJourney.CommitAction(s, "rest", out notice) && s.journey.turns == 1 && s.nanyang.stage == "free" && s.journey.hardshipId == "" && NanyangLife.CareerLevel(s, "shop") == 1, "bed does not create wages or experience");
            Require(NanyangLife.TryMeal(s, out notice), "next round may eat");
            Require(LifeJourney.CommitAction(s, "rest", out notice) && LifeJourney.CommitAction(s, "rest", out notice), "three rests");
            Require(!LifeJourney.CanFinish(s), "cannot rest through mandatory first work");
            s.money = 0; Reject(s, () => NanyangLife.TryMeal(s, out notice), "meal cannot overspend");
            Require(LifeJourney.CommitAction(s, "shop", out notice) && s.nanyang.wageTurn == 4 && s.money == 3, "first actual work after rests is withheld exactly once");
            Reload(ref s, "late first job");
        }
        static void Capacity()
        {
            var s = AtLetter(); string notice;
            for (int i = 0; i < 38; i++) s.letters.Add(new LetterRecord { title = "旧批", date = "1906", route = "泉州", body = "存下的旧批", signature = "家人" });
            PersonalLetters.EnsureDraft(s).body = "家中平安否";
            Require(PersonalLetters.TrySend(s, s.personalLetterDraft.token, out notice) && s.letters.Count == 39, "pending reply reserves fortieth slot");
            Reload(ref s, "39 plus pending");
            s.letters.Add(new LetterRecord { body = "占用回批位置" }); Require(!StoryEngine.Validate(s), "reject filled reserved slot"); s.letters.RemoveAt(39);
            Require(NanyangLife.AdvanceWeeks(s) && NanyangLife.CollectReply(s) && s.letters.Count == 40 && StoryEngine.Validate(s), "collect safely fills last slot");
            Require(!LifeJourney.Actions(s).Find(a => a.id == "remit").enabled, "ordinary remit cannot overflow inbox");
            var rich = Fresh(); rich.money = 9999;
            Require(LifeJourney.CommitAction(rich, "shop", out notice) && rich.money == 9999, "withheld wage applied before money cap");
            Require(NanyangLife.ChooseResponse(rich, "together"), "rich choose");
            Require(NanyangLife.CompleteStep(rich, 0, out notice) && NanyangLife.CompleteStep(rich, 1, out notice) && rich.money == 9999 && StoryEngine.Validate(rich), "recovery caps safely");
            var fullFlags = Fresh(); while (fullFlags.flags.Count < 80) fullFlags.flags.Add("padding_" + fullFlags.flags.Count);
            Reject(fullFlags, () => LifeJourney.CommitAction(fullFlags, "shop", out notice), "trigger flag capacity is atomic");
            var resolveFlags = Fresh(); Require(LifeJourney.CommitAction(resolveFlags, "shop", out notice) && NanyangLife.ChooseResponse(resolveFlags, "evidence"), "final capacity fixture");
            Require(NanyangLife.CompleteStep(resolveFlags, 0, out notice) && NanyangLife.CompleteStep(resolveFlags, 1, out notice), "capacity early steps");
            while (resolveFlags.flags.Count < 80) resolveFlags.flags.Add("padding_" + resolveFlags.flags.Count);
            Reject(resolveFlags, () => NanyangLife.CompleteStep(resolveFlags, 2, out notice), "final flag capacity is atomic");
            var maxLetters = AtLetter(); for (int i = 0; i < 3; i++) { maxLetters.personalLetterTokens.Add(i.ToString("x32")); maxLetters.personalLettersSent++; }
            Require(NanyangLife.DeferLetter(maxLetters) && StoryEngine.Validate(maxLetters), "three sent letters can defer chapter");
        }
        static void Transfer()
        {
            var s = AtLetter("shop", "leave"); Require(NanyangLife.DeferLetter(s), "transfer defer"); string notice;
            int money = s.money;
            Require(LifeJourney.CommitAction(s, "shop", out notice) && s.money == money + 10 && !s.nanyang.transferBonusUsed, "same career does not consume transfer");
            money = s.money;
            Require(LifeJourney.CommitAction(s, "courier", out notice) && s.money == money + 8 && s.nanyang.transferBonusUsed, "first different career gets two");
            Reload(ref s, "used transfer bonus"); money = s.money;
            Require(LifeJourney.CommitAction(s, "dock", out notice) && s.money == money + 10, "second different career gets no repeated bonus");
            Require(StoryEngine.Validate(s), "transfer chronology valid");
        }
        static void Invalid()
        {
            var s = Fresh(); s.nanyang.lastMealTurn = 1; Require(!StoryEngine.Validate(s), "cannot eat in future turn");
            s = Fresh(); s.nanyang.pendingReply = new LetterRecord { body = "提前回批" }; Require(!StoryEngine.Validate(s), "no reply before sent token");
            s = AtLetter(); s.nanyang.recovered = 4; Require(!StoryEngine.Validate(s), "cannot change partial recovery to full");
            s = AtLetter("dock", "leave"); Require(NanyangLife.DeferLetter(s), "invalid bonus fixture"); s.nanyang.transferBonusUsed = true; Require(!StoryEngine.Validate(s), "cannot consume bonus before changing work");
            s = Fresh(); string notice; Require(LifeJourney.CommitAction(s, "shop", out notice), "wrong wage fixture"); s.nanyang.paidWage++; Require(!StoryEngine.Validate(s), "cannot alter wage snapshot");
        }
        static void FirstDayReferral()
        {
            foreach (string route in new[] { "shop", "courier" }) {
                var s = StoryEngine.NewGame(2100); s.nodeId = route; s.money = 50; s.trust = 50;
                Require(FirstDayChapter.TryBegin(s), "first-day referral begin");
                Reject(s, () => NanyangLife.TryBegin(s), "first day still owns chapter");
                Require(FirstDayChapter.TriggerEncounter(s, 1) && FirstDayChapter.ChooseWork(s, route == "shop" ? "wages" : "verify") && FirstDayChapter.SettleWork(s), "first-day referral job");
                string notice;
                Require(FirstDayChapter.TryEvening(s, "sleep", out notice) && FirstDayChapter.DeferLetter(s) && FirstDayChapter.Sleep(s) && FirstDayChapter.AdvanceMorning(s).Length > 0, "first-day referral complete");
                Require(NanyangLife.TryBegin(s), "enrol after first day");
                int money = s.money, trust = s.trust;
                Require(LifeJourney.CommitAction(s, route, out notice), "new first free job with referral");
                Require(s.money == money + (route == "shop" ? 5 : 2) && s.trust == trust + (route == "shop" ? 4 : 8) && s.firstDay.bonusUsed, "original referral added once outside wage ledger");
                Require(s.nanyang.promisedWage == (route == "shop" ? 7 : 6) && s.nanyang.paidWage == (route == "shop" ? 3 : 2), "referral not confused with withheld wages");
                Reload(ref s, "first-day referral and new chapter coexist");
            }
        }
        static void Legacy()
        {
            var s = StoryEngine.NewGame(2100); s.nodeId = "first_pay"; s.money = 100; s.health = 80; s.trust = 50;
            s.nanyang = new NanyangState { lastMealTurn = 0, pendingReply = new LetterRecord() };
            Require(StoryEngine.Validate(s) && !NanyangLife.IsEnrolled(s), "version-zero inline defaults");
            Require(LifeJourney.Actions(s)[0].title == "码头扛货" && LifeJourney.Actions(s)[0].money == 10, "old names/rewards unchanged");
            string notice; Require(LifeJourney.CommitAction(s, "shop", out notice) && s.money == 107 && s.health == 76 && s.journey.hardshipId == "", "legacy first pay unchanged");
            Require(LifeJourney.CommitAction(s, "shop", out notice) && s.money == 110 && s.health == 69 && LifeJourney.PendingHardship(s) != null, "legacy turn-two loss retained");
            Require(LifeJourney.ResolveHardship(s, "evidence", out notice) && s.money == 111 && s.health == 67, "legacy evidence net reward retained");
            Require(StoryEngine.Validate(s), "old journey remains valid");
            Reject(s, () => NanyangLife.TryBegin(s), "part-played old journey never enrolled");
            s.nanyang.stage = "free"; Require(!StoryEngine.Validate(s), "attached progress needs version marker");
            var rest = StoryEngine.NewGame(2100); rest.nodeId = "first_pay"; rest.journey.pendingJob = "rest"; Require(!StoryEngine.Validate(rest), "legacy pending rest rule unchanged");
        }
    }
}
