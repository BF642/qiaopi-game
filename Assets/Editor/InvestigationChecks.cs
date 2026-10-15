using System;
using System.Linq;
using UnityEngine;

namespace Qiaopi.Editor
{
    // Pure state checks: never opens player saves, preferences or archived letters.
    public static class InvestigationChecks
    {
        static int assertions, roundTrips;
        static readonly string[] Routes = { "desk", "harbor", "family" };

        public static void Run()
        {
            assertions = roundTrips = 0;
            Compatibility(); InvalidStates(); HelpProvenance();
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
                foreach (string route in Routes)
                    foreach (bool useHelp in new[] { false, true }) Branch(destination, route, useHelp);
            Endings(); OutcomeDistinctions();
            Debug.Log("INVESTIGATION_CHECKS_PASSED: 3 routes x 3 destinations x help/no-help, physical-evidence state gates, repeat-read/reward isolation, wrong-answer recovery, settled-work provenance, legacy pending choice compatibility, exact JSON round-trips and 8 endings; assertions=" + assertions + ", round-trips=" + roundTrips + ". Player saves/preferences untouched.");
        }

        static void Require(bool value, string label)
        { assertions++; if (!value) throw new InvalidOperationException("Investigation check: " + label); }
        static string Json(GameState s) { return JsonUtility.ToJson(s); }
        static GameState Copy(GameState s) { return JsonUtility.FromJson<GameState>(Json(s)); }
        static string OutsideInquiry(GameState s)
        { GameState copy = Copy(s); copy.inquiryVersion = 0; copy.inquiry = null; return Json(copy); }
        static void Reject(GameState s, Func<bool> action, string label)
        { string before = Json(s); Require(!action(), label + " rejected"); Require(Json(s) == before, label + " has no state effects"); }
        static void Change(GameState s, Func<bool> action, string label)
        {
            string before = OutsideInquiry(s);
            Require(action(), label + " accepted");
            Require(OutsideInquiry(s) == before, label + " cannot change money, stats, story, work memory or letters");
            Require(StoryEngine.Validate(s), label + " remains valid");
        }
        static void Reload(ref GameState s, string label)
        {
            string before = Json(s); Require(StoryEngine.Validate(s), label + " valid before save");
            s = JsonUtility.FromJson<GameState>(before); roundTrips++;
            Require(StoryEngine.Validate(s) && Json(s) == before, label + " exact valid JSON round-trip");
        }
        static void Advance(GameState s, string id)
        {
            Require(StoryEngine.Choose(s, id), "choose " + s.nodeId + "/" + id);
            Require(StoryEngine.Validate(s) && StoryEngine.Continue(s) && StoryEngine.Validate(s), "continue " + id);
        }
        static string Resolve(string route)
        { return route == "desk" ? "reconcile" : route == "harbor" ? "witness" : "match"; }
        static string Pending(string route)
        { return route == "desk" ? "complaint" : route == "harbor" ? "wait" : "hide"; }
        static string Trade(string route)
        { return route == "desk" ? "shop" : route == "harbor" ? "dock" : "courier"; }
        static GameState Fixture(string destination, string route, bool clearOldEvidence = false)
        {
            GameState s = InquiryDemo.Create(destination);
            Require(StoryEngine.Validate(s) && s.nodeId == "missing" && s.decisions == 12, "demo starts before route decision");
            Require(s.journey.completed && s.journey.turns == 6 && s.workMemory.records.Count == 3, "demo has six settled jobs and three actual memories");
            Require(s.letters.Any(l => !l.incoming && l.title == "第二年的家用" && l.amount == 10), "fictional inquiry refers to the actual second remittance");
            if (clearOldEvidence) {
                s.flags.RemoveAll(f => new[] { "ledger", "copies", "verified", "shop_record", "ship_friend", "crew_help", "marked_cargo", "careful_reply", "signed_reply", "honest", "ask_school", "address" }.Contains(f));
                s.trust = 20;
                Require(StoryEngine.Validate(s), "low-evidence fixture retains valid settled work");
            }
            Advance(s, route);
            Change(s, () => Investigation.Enroll(s), "explicit runtime enrollment");
            Require(s.inquiry.route == "trace_" + route && s.inquiry.helpRoute == Trade(route), "route and preferred work provenance selected");
            return s;
        }
        static void ReadAll(GameState s)
        {
            string notice;
            foreach (int i in new[] { 2, 0, 1 }) Change(s, () => Investigation.ReadEvidence(s, i, out notice), "read record " + i);
        }
        static void Correct(GameState s)
        {
            string notice;
            Change(s, () => Investigation.Select(s, "amount", 1, out notice), "record amount");
            Change(s, () => Investigation.Select(s, "date", 2, out notice), "record transfer date");
            Change(s, () => Investigation.Select(s, "address", 0, out notice), "record old and temporary addresses");
        }

        static void Branch(string destination, string route, bool useHelp)
        {
            GameState s = Fixture(destination, route, true); GameState live = s; string notice = "";
            string before = Json(s);
            for (int i = 0; i < 3; i++) {
                InquiryEvidence e = Investigation.Evidence(s, i);
                Require(e != null && !string.IsNullOrEmpty(e.id) && e.body.Length > 50, "three substantive records");
                Investigation.Choices(s, new[] { "amount", "date", "address" }[i]); StoryEngine.GetScene(s); Investigation.HelpText(s); Investigation.Summary(s);
            }
            Require(Json(s) == before, "presenting records, advice and scene is read-only");
            Require(StoryEngine.GetScene(s).choices.All(c => !c.enabled), "all story choices wait for actual inquiry");
            Reject(s, () => StoryEngine.Choose(live, Pending(route)), "cannot skip unfinished inquiry");
            Reject(s, () => Investigation.Enroll(live), "cannot enroll twice");
            Reject(s, () => Investigation.Select(live, "amount", 1, out notice), "cannot guess before reading");
            Reject(s, () => Investigation.Submit(live, out notice), "cannot submit without evidence");
            Reject(s, () => Investigation.ReadEvidence(live, 3, out notice), "unknown record");
            foreach (int i in new[] { 2, 0, 1 }) {
                Change(s, () => Investigation.ReadEvidence(live, i, out notice), "visit record " + i);
                string once = Json(s); Require(Investigation.ReadEvidence(s, i, out notice) && Json(s) == once, "repeat visit adds no progress");
                Reload(ref s, destination + "/" + route + " record " + i); live = s;
            }
            Require(s.inquiry.stage == "verify" && s.inquiry.evidenceMask == 7, "all three records open comparison");
            if (useHelp) {
                string helpField = route == "desk" ? "amount" : route == "harbor" ? "date" : "address";
                int rejected = route == "harbor" ? 1 : 2;
                Change(s, () => Investigation.Select(live, helpField, rejected, out notice), "tentative guess later disproved by helper");
                Change(s, () => Investigation.UseHelp(live, out notice), "ask actual former colleague");
                Require(notice.Length > 40 && Investigation.Choices(s, helpField).Count(c => !c.enabled) == 1, "one sourced contradiction only");
                Require((helpField == "amount" ? s.inquiry.amount : helpField == "date" ? s.inquiry.date : s.inquiry.address) == -1, "help clears wrong guess without supplying answer");
                Reject(s, () => Investigation.Select(live, helpField, rejected, out notice), "contradicted choice unavailable");
                Reject(s, () => Investigation.UseHelp(live, out notice), "helper cannot pay out twice");
                Require(!Investigation.HasCompletedHelp(s), "help alone does not bypass verification");
                Reload(ref s, "recorded help"); live = s;
            }
            Reject(s, () => Investigation.Select(live, "unknown", 1, out notice), "unknown comparison field");
            Reject(s, () => Investigation.Select(live, "amount", -1, out notice), "negative comparison option");
            Reject(s, () => Investigation.Submit(live, out notice), "all three answers required");
            Change(s, () => Investigation.Select(live, "amount", 0, out notice), "wrong amount remains a player guess");
            Change(s, () => Investigation.Select(live, "date", 0, out notice), "wrong date remains a player guess");
            Change(s, () => Investigation.Select(live, "address", 1, out notice), "wrong address remains a player guess");
            Reject(s, () => Investigation.Submit(live, out notice), "wrong amount must be corrected");
            Require(notice.Contains("款额"), "amount feedback names what to reread");
            Change(s, () => Investigation.Select(live, "amount", 1, out notice), "correct amount");
            Reject(s, () => Investigation.Submit(live, out notice), "sending day cannot masquerade as transfer day");
            Require(notice.Contains("转交"), "date feedback distinguishes transfer from receipt");
            Change(s, () => Investigation.Select(live, "date", 2, out notice), "correct transfer day");
            Reject(s, () => Investigation.Submit(live, out notice), "thirty is not three");
            Require(notice.Contains("门牌"), "address feedback names uncertainty");
            Change(s, () => Investigation.Select(live, "address", 0, out notice), "correct forwarding address");
            Reload(ref s, "ready to submit"); live = s;
            Change(s, () => Investigation.Submit(live, out notice), "verify three facts");
            Require(Investigation.IsComplete(s) && !s.flags.Contains("resolved") && notice.Contains("尚未收讫"), "verifying evidence is not inventing delivery");
            Require(s.inquiry.helpUsed == useHelp && Investigation.HasCompletedHelp(s) == useHelp, "only used help unlocks its alternative condition");
            Require(StoryEngine.GetScene(s).choices.Single(c => c.id == Resolve(route)).enabled == useHelp, "verified source helps when earlier story evidence is missing");
            Reject(s, () => Investigation.Submit(live, out notice), "no repeated completion reward");
            Reject(s, () => Investigation.UseHelp(live, out notice), "finished inquiry cannot backfill help");
            Reject(s, () => Investigation.Select(live, "amount", 0, out notice), "verified evidence cannot be rewritten");
            Reload(ref s, "complete inquiry"); live = s;
            string repeated = Json(s); Require(Investigation.ReadEvidence(s, 0, out notice) && Json(s) == repeated, "completed evidence remains readable without effects");
            int decisions = s.decisions, letters = s.letters.Count;
            Require(StoryEngine.Choose(s, useHelp ? Resolve(route) : Pending(route)), "original consequence choice remains player decision");
            Require(s.decisions == decisions + 1 && s.flags.Contains(useHelp ? "resolved" : "pending"), "one original decision and correct consequence");
            Require(s.letters.Count == letters + (useHelp || route == "family" ? 1 : 0), "only original outcome adds its letter");
            Reload(ref s, "pending original consequence"); live = s;
            Reject(s, () => StoryEngine.Choose(live, Resolve(route)), "no repeated consequence choice");
            Reject(s, () => Investigation.ReadEvidence(live, 0, out notice), "result awaiting continuation cannot mutate inquiry");
            Require(StoryEngine.Continue(s) && s.nodeId == "storms" && StoryEngine.Validate(s), "story continues without a second investigation");
            Reject(s, () => Investigation.Enroll(live), "cannot re-enroll downstream");
            Reject(s, () => Investigation.ReadEvidence(live, 0, out notice), "cannot operate previous scene evidence");
        }

        static void Compatibility()
        {
            Require(!Investigation.Validate(null) && !Investigation.IsActive(null) && !Investigation.CanUseHelp(null), "null state safely rejected");
            foreach (string route in Routes) {
                GameState old = new GameState { nodeId = "trace_" + route };
                Require(StoryEngine.Validate(old), "old save without inquiry is valid");
                string unchanged = Json(old); StoryEngine.GetScene(old);
                Require(Json(old) == unchanged && old.inquiryVersion == 0, "reading old scene never auto-enrolls");
                old.inquiry = new InquiryState { amount = 0, date = 0, address = 0 };
                Reload(ref old, "legacy materialized default inquiry");
                Require(StoryEngine.Choose(old, Pending(route)), "legacy choices remain playable");
                GameState pending = old;
                Reject(pending, () => Investigation.Enroll(pending), "pending legacy consequence never restarts investigation");
                Reload(ref old, "legacy pending decision");
                Require(StoryEngine.Continue(old) && StoryEngine.Validate(old) && old.nodeId == "storms", "old pending outcome remains intact");
            }
            var home = StoryEngine.NewGame(2700);
            Reject(home, () => Investigation.Enroll(home), "home cannot enroll into future plot");
        }

        static void HelpProvenance()
        {
            // Old saves have no evidence of an eligible settled choice: reputation
            // or a future work assignment alone is not enough to invent a helper.
            GameState old = new GameState { nodeId = "trace_harbor", trust = 100 };
            Require(Investigation.Enroll(old) && old.inquiry.helpTurn == 0 && !Investigation.CanUseHelp(old), "no work history means no invented old colleague");
            string notice;
            Reject(old, () => Investigation.UseHelp(old, out notice), "high reputation alone does not create a memory");
            foreach (string trade in new[] { "dock", "shop", "courier" }) {
                GameState s = InquiryDemo.Create();
                s.workMemory.records.RemoveAll(r => r.route != trade);
                Advance(s, trade == "shop" ? "harbor" : "desk");
                Require(Investigation.Enroll(s) && s.inquiry.helpRoute == trade && Investigation.CanUseHelp(s), "fallback helper follows the actually settled career " + trade);
                Change(s, () => Investigation.UseHelp(s, out notice), "fallback colleague gives matching help");
                Require(Investigation.HelpText(s).Contains(trade == "dock" ? "工钱" : trade == "shop" ? "掌柜" : "阿福"), "helper remembers the correct job");
            }
            GameState forged = Fixture("singapore", "desk");
            forged.inquiry.helpTurn++;
            Require(!StoryEngine.Validate(forged), "helper provenance cannot name another turn");
            forged = Fixture("singapore", "desk"); forged.workMemory.records.RemoveAll(r => r.route == "shop");
            Require(!StoryEngine.Validate(forged), "helper cannot survive deletion of its settled evidence");
        }

        static void InvalidStates()
        {
            GameState baseline = new GameState { nodeId = "trace_family" };
            Require(Investigation.Enroll(baseline), "enroll isolated legacy fixture");
            Action<GameState>[] corruptions = {
                s => s.inquiryVersion = 99, s => s.inquiry.route = "other", s => s.inquiry.stage = "other",
                s => s.inquiry.evidenceMask = 8, s => s.inquiry.amount = 1, s => s.inquiry.stage = "verify",
                s => s.inquiry.stage = "complete", s => s.nodeId = "trace_desk", s => s.nodeId = "storms",
                s => s.inquiry.helpUsed = true, s => s.inquiry.helpRoute = "dock", s => s.inquiry.helpTurn = -1
            };
            foreach (Action<GameState> corrupt in corruptions) {
                GameState bad = Copy(baseline); corrupt(bad);
                Require(!StoryEngine.Validate(bad), "reject inconsistent inquiry state");
                string notice;
                Reject(bad, () => Investigation.ReadEvidence(bad, 0, out notice), "invalid state cannot collect");
                Reject(bad, () => Investigation.Select(bad, "amount", 1, out notice), "invalid state cannot select");
                Reject(bad, () => Investigation.Submit(bad, out notice), "invalid state cannot submit");
            }
            GameState missingVersion = Copy(baseline); missingVersion.inquiryVersion = 0;
            Require(!StoryEngine.Validate(missingVersion), "unversioned attached progress rejected");
            ReadAll(baseline); Correct(baseline); string result;
            Require(Investigation.Submit(baseline, out result), "complete baseline");
            baseline.inquiry.amount = 0;
            Require(!StoryEngine.Validate(baseline), "completion requires exact corroborated facts");
        }

        static void Endings()
        {
            string[] expected = { "home_lamp", "home_scar", "school_window", "shop_bridge", "trusted_route", "new_harbor", "unsent_letter", "silver_thread" };
            string[] paths = {
                "ledger guarantor mother share dock tally rest honest details copies desk reconcile settle return home",
                "ledger borrow mother berth dock extra overtime reassure medicine copies family hide hard_work return home",
                "ledger guarantor sister share shop learn overtime honest details copies desk reconcile hard_work return school",
                "ledger guarantor mother share shop learn rest honest details copies desk reconcile settle stay steady",
                "ledger guarantor mother share courier verify rest honest details copies desk reconcile settle stay steady",
                "ledger borrow mother share dock extra rest honest details copies desk reconcile settle farther migrate",
                "ledger borrow mother share dock extra rest reassure details copies family hide hard_work return confess",
                "ledger guarantor mother share dock tally rest honest details copies desk reconcile settle stay steady"
            };
            for (int i = 0; i < paths.Length; i++) {
                GameState s = StoryEngine.NewGame(2700 + i);
                foreach (string id in ("meal neighbors " + paths[i]).Split(' ')) {
                    if (Investigation.IsTraceNode(s.nodeId)) {
                        Require(Investigation.Enroll(s), "ending route enrolls once");
                        ReadAll(s); Correct(s); string notice;
                        Require(Investigation.Submit(s, out notice), "ending route physically verified");
                    }
                    Advance(s, id);
                }
                SceneData end = StoryEngine.GetScene(s);
                Require(end.isEnding && end.endingId == expected[i] && s.decisions == 17 && Investigation.IsComplete(s), "existing ending survives inquiry: " + expected[i]);
                Require(end.body.Contains("查批纸"), "ending remembers investigation evidence");
                Require(s.flags.Contains("resolved") ? end.body.Contains("收妥的回批") : end.body.Contains("没有收到这笔原款的收讫"), "ending reflects actual result, not collection alone");
                Reload(ref s, "ending " + expected[i]);
            }
        }

        static void OutcomeDistinctions()
        {
            foreach (string route in new[] { "desk", "family" }) {
                GameState s = Fixture("singapore", route); ReadAll(s); Correct(s); string notice;
                Require(Investigation.Submit(s, out notice), "replacement inquiry verified");
                int money = s.money;
                Advance(s, "replace");
                Require(s.money == money - 10 && s.flags.Contains("replaced") && !s.flags.Contains("resolved"), "replacement is another payment, not recovery of original");
                Advance(s, "hard_work"); Advance(s, "stay"); Advance(s, "steady");
                Require(StoryEngine.GetScene(s).body.Contains("没有替原款补上一道收讫"), "replacement ending leaves original remittance unresolved");
            }
        }
    }
}
