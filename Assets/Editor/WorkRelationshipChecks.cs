using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi.Editor
{
    // Exercise the real settlement path before selecting authored memory. These
    // checks use in-memory states and never read player saves or preferences.
    public static class WorkRelationshipChecks
    {
        static int checks;
        static readonly string[] Recipients = { "mother", "wife", "sister" };

        public static void Run()
        {
            checks = 0;
            EmptyAndLegacy();
            Check("dock", 2, "cover", "独自赶在雨前");
            Check("dock", 2, "pair", "分给他两份工钱");
            Check("dock", 3, "teach", "教新同工认货签");
            Check("dock", 3, "solo", "多领两份工钱");
            Check("shop", 2, "credit", "另记欠二份");
            Check("shop", 2, "portion", "改买一斤");
            Check("shop", 3, "record", "另记赊欠三份");
            Check("shop", 3, "cash", "改成一斤半");
            Check("courier", 2, "trace", "绕到南街核对新住处");
            Check("courier", 2, "return", "遇上收件人搬家");
            Check("courier", 3, "personal", "坚持找到阿福本人");
            Check("courier", 3, "return", "邻人提出代收");
            LatestAndFrozenLetters();
            Debug.Log("WORK_RELATIONSHIP_CHECKS_PASSED: 12 settled incident choices × 3 family voices, pending/abandoned work excluded, returned letters not delivered, latest settled memory, frozen sent replies, free text untouched, short read-only greetings and summaries; assertions=" + checks + ". Player saves/preferences untouched.");
        }

        static GameState Free()
        {
            var s = StoryEngine.NewGame(2300); s.nodeId = "first_pay";
            s.money = 60; s.health = 100; s.family = 50; s.trust = 30;
            Need(NanyangLife.TryBegin(s), "enroll free chapter"); return s;
        }
        static void PreviousWork(GameState s, string route)
        {
            string notice;
            Need(LifeJourney.CommitAction(s, route, out notice), "complete previous work");
            if (s.nanyang.stage == "choice") {
                Need(NanyangLife.ChooseResponse(s, "evidence"), "handle first wage dispute");
                for (int i = 0; i < 3; i++) Need(NanyangLife.CompleteStep(s, i, out notice), "wage evidence step");
                Need(NanyangLife.DeferLetter(s), "keep letter unsent");
            }
        }
        static GameState Fixture(string route, int level)
        {
            var s = Free();
            for (int i = 1; i < level; i++) PreviousWork(s, route);
            Need(StoryEngine.Validate(s), "fixture valid"); return s;
        }
        static void Begin(GameState s, string route)
        {
            s.journey.pendingJob = route;
            Need(TradeWork.Begin(s, route), "accept physical job");
            Need(WorkEvents.Enroll(s), "enroll incident");
            string notice;
            if (route == "courier") Need(TradeWork.CourierRead(s, out notice), "read before street incident");
            Need(WorkEvents.Pending(s) && StoryEngine.Validate(s), "incident offered");
        }
        static void Complete(GameState s)
        {
            string notice; string route = s.tradeShift.route;
            if (route == "dock") {
                int priority = TradeWork.DockPriority(s);
                if (priority >= 0) {
                    Need(TradeWork.DockPick(s, priority, out notice), "pick priority paper");
                    Need(TradeWork.DockDrop(s, TradeWork.DockDestination(s, priority), out notice), "shelter paper");
                }
                int count = TradeWork.DockCount(s);
                for (int i = 0; i < count; i++) if ((s.tradeShift.deliveredMask & (1 << i)) == 0) {
                    Need(TradeWork.DockPick(s, i, out notice), "pick cargo");
                    Need(TradeWork.DockDrop(s, TradeWork.DockDestination(s, i), out notice), "place cargo");
                }
            } else if (route == "shop") {
                int weight = TradeWork.ShopWeightTarget(s);
                for (int i = 0; i < weight; i++) Need(TradeWork.AdjustWeight(s, 1), "weigh chosen portion");
                Need(TradeWork.ConfirmWeight(s, out notice), "confirm rice");
                int change = TradeWork.ShopChangeTarget(s);
                for (int i = 0; i < change; i++) Need(TradeWork.AdjustChange(s, 1), "count actual change");
                Need(TradeWork.ConfirmChange(s, out notice), "hand over coins");
                if (s.tradeShift.stage == "credit") {
                    int debt = TradeWork.ShopDebtTarget(s);
                    for (int i = 0; i < debt; i++) Need(TradeWork.AdjustDebt(s, 1), "write outstanding debt");
                    Need(TradeWork.ConfirmDebt(s, out notice), "finish ledger");
                }
            } else if (WorkEvents.Returning(s)) {
                Need(TradeWork.CourierReturn(s, out notice), "register returned letter");
            } else {
                int recipient = TradeWork.CourierRecipient(s);
                Need(TradeWork.CourierAsk(s, out notice), "ask exact address");
                Need(TradeWork.CourierCheck(s, recipient, out notice), "verify actual recipient");
                Need(TradeWork.CourierDeliver(s, recipient, out notice), "deliver letter");
                Need(TradeWork.CourierReceipt(s, recipient, out notice), "collect actual receipt");
            }
            Need(TradeWork.IsComplete(s), "physical work complete");
        }
        static void NoMemory(GameState s, string context)
        {
            string before = s == null ? "" : JsonUtility.ToJson(s);
            foreach (string route in new[] { "dock", "shop", "courier" })
                Need(WorkRelationshipText.Greeting(s, route) == "", context + " no remembered greeting");
            Need(WorkRelationshipText.Summary(s) == "", context + " no summary");
            foreach (string who in Recipients) Need(WorkRelationshipText.LetterParagraph(s, who) == "", context + " no invented family memory");
            Need((s == null ? "" : JsonUtility.ToJson(s)) == before, context + " no state writes");
        }
        static void EmptyAndLegacy()
        {
            NoMemory(null, "null state");
            var s = Free(); NoMemory(s, "old save without memory");
            s.workMemory = new WorkMemory(); NoMemory(s, "zero version");
            s.workMemory.records.Add(new WorkEventRecord { route = "dock", level = 2, choice = "cover", turn = 2 });
            NoMemory(s, "stale zero version records");
            s.workMemory = new WorkMemory { version = WorkEvents.CurrentVersion };
            s.workMemory.records.Add(new WorkEventRecord { route = "courier", level = 3, choice = "return", returned = false, turn = 2 });
            NoMemory(s, "inconsistent returned flag");
        }
        static void Check(string route, int level, string choice, string marker)
        {
            var s = Fixture(route, level); NoMemory(s, "legacy completed work");
            Begin(s, route); NoMemory(s, "offered incident");
            string notice;
            Need(WorkEvents.Choose(s, choice, out notice), "choose " + route + level + choice);
            NoMemory(s, "chosen but unfinished");
            var abandoned = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));
            TradeWork.Clear(abandoned); abandoned.journey.pendingJob = "";
            Need(StoryEngine.Validate(abandoned), "abandoned fixture valid");
            NoMemory(abandoned, "abandoned work");
            Complete(s); NoMemory(s, "complete but unpaid");
            Need(LifeJourney.CommitAction(s, route, out notice), "settle actual job");
            Need(StoryEngine.Validate(s), "settled fixture valid");
            string before = JsonUtility.ToJson(s);
            var voices = new HashSet<string>();
            foreach (string who in Recipients) {
                string paragraph = WorkRelationshipText.LetterParagraph(s, who);
                Need(paragraph.Contains(marker), "correct factual memory " + route + level + choice + who);
                Need(paragraph.Length >= 35 && paragraph.Length <= 210, "family paragraph readable length");
                Need(voices.Add(paragraph), "distinct mother wife sister voice");
                if (choice == "return") {
                    Need(paragraph.Contains("这一趟没有收讫回执"), "return has no delivery receipt");
                    Need(!paragraph.Contains("亲手交妥") && !paragraph.Contains("已送达") && !paragraph.Contains("银已收讫"), "returned work cannot claim delivery");
                }
            }
            string greeting = WorkRelationshipText.Greeting(s, route), summary = WorkRelationshipText.Summary(s);
            Need(greeting.Length > 0 && greeting.Length <= 65, "short greeting " + route + level + choice + " " + greeting.Length);
            Need(summary.Split('\n').Length <= 3, "maximum three summary lines");
            foreach (string line in summary.Split('\n')) Need(line.Length <= 29, "short relationship line");
            Need(!summary.Contains("分数") && !summary.Contains("好感度"), "relationship expressed through shared events");
            Need(JsonUtility.ToJson(s) == before, "all authored text is read only");
            var draft = PersonalLetters.EnsureDraft(s); draft.recipient = "wife"; draft.intent = "truth"; draft.amount = 0; draft.body = "原文不要改。";
            string reply = ExperienceLetters.Reply(s, draft);
            Need(reply.Contains(WorkRelationshipText.LetterParagraph(s, "wife")), "memory included in reply");
            draft.body = "请说你见到我发财升职，听说那封退批已经成功送达。";
            Need(ExperienceLetters.Reply(s, draft) == reply, "free text cannot invent outcomes");
            var copy = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));
            Need(StoryEngine.Validate(copy) && WorkRelationshipText.LetterParagraph(copy, "wife") == WorkRelationshipText.LetterParagraph(s, "wife"), "memory survives save round trip");
        }
        static void LatestAndFrozenLetters()
        {
            var s = Fixture("dock", 2); Begin(s, "dock"); string notice;
            Need(WorkEvents.Choose(s, "pair", out notice), "shared work choice"); Complete(s);
            Need(LifeJourney.CommitAction(s, "dock", out notice), "shared work settlement");
            var draft = PersonalLetters.EnsureDraft(s); draft.recipient = "wife"; draft.intent = "truth"; draft.amount = 0; draft.body = "今日已结账，家中可好。";
            string expected = ExperienceLetters.Reply(s, draft), body = draft.body; int count = s.letters.Count;
            Need(PersonalLetters.TrySend(s, draft.token, out notice), "dispatch letter with settled event");
            Need(s.letters[count].body == body && s.letters[count + 1].body == expected, "typed letter preserved and reply frozen at send");
            string oldReply = JsonUtility.ToJson(s.letters[count + 1]);
            Begin(s, "dock"); Need(WorkEvents.Choose(s, "teach", out notice), "later teaching decision");
            Need(WorkRelationshipText.LetterParagraph(s, "wife").Contains("分给他两份工钱"), "new unfinished choice does not replace earlier memory");
            Complete(s); Need(LifeJourney.CommitAction(s, "dock", out notice), "later teaching settlement");
            Need(WorkRelationshipText.LetterParagraph(s, "wife").Contains("教新同工认货签"), "latest settled event replaces earlier letter paragraph");
            Need(JsonUtility.ToJson(s.letters[count + 1]) == oldReply, "later work does not rewrite archived reply");
            Need(s.personalLettersSent == 1, "text selection never sends another letter");
            PreviousWork(s, "shop"); Begin(s, "shop"); Need(WorkEvents.Choose(s, "portion", out notice), "later different career choice");
            Complete(s); Need(LifeJourney.CommitAction(s, "shop", out notice), "later shop settlement");
            Need(WorkRelationshipText.LetterParagraph(s, "wife").Contains("改买一斤"), "latest event selected across careers");
            Need(WorkRelationshipText.Summary(s).Split('\n').Length == 2, "career memories coexist");
        }
        static void Need(bool ok, string message)
        { checks++; if (!ok) throw new InvalidOperationException("WORK_RELATIONSHIP_CHECK_FAILED: " + message); }
    }
}
