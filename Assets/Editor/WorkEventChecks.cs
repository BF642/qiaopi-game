using System;
using UnityEngine;

namespace Qiaopi.Editor
{
    // No disk saves, preferences, scene access or random event retries.
    public static class WorkEventChecks
    {
        static int assertions, roundTrips;
        public static void Run()
        {
            assertions = roundTrips = 0;
            Compatibility();
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
                foreach (string route in new[] { "dock", "shop", "courier" })
                    for (int level = 2; level <= 3; level++)
                        foreach (string choice in Ids(route, level)) Branch(destination, route, level, choice);
            Abandon(); Relationships(); Invalid(); Caps();
            Debug.Log("WORK_EVENT_CHECKS_PASSED: 12 choices × 3 destinations, physical task differences, unchanged resources before payroll, exact single settlement, returned-qiaopi registration, saved event decisions, completed-only relationships, abandoned-work isolation and 2.2 compatibility; assertions=" + assertions + ", in-memory save round-trips=" + roundTrips + ". Player saves/preferences untouched.");
        }
        static string[] Ids(string route, int level)
        { return route == "dock" ? (level == 2 ? new[] { "cover", "pair" } : new[] { "teach", "solo" }) : route == "shop" ? (level == 2 ? new[] { "credit", "portion" } : new[] { "record", "cash" }) : level == 2 ? new[] { "trace", "return" } : new[] { "personal", "return" }; }
        static void Require(bool condition, string message)
        { assertions++; if (!condition) throw new Exception("Work event checks: " + message); }
        static string Json(GameState s) { return JsonUtility.ToJson(s); }
        static string OutsideShift(GameState s)
        { var copy = JsonUtility.FromJson<GameState>(Json(s)); copy.tradeShift = null; return Json(copy); }
        static void Reject(GameState s, Func<bool> action, string message)
        { string before = Json(s); Require(!action(), message + " rejects"); Require(Json(s) == before, message + " changes nothing"); }
        static void Move(GameState s, Func<bool> action, string message)
        { string before = OutsideShift(s); Require(action(), message + " succeeds"); Require(OutsideShift(s) == before, message + " changes only current work"); Require(StoryEngine.Validate(s), message + " remains valid"); }
        static void Reload(ref GameState s, string message)
        {
            Require(StoryEngine.Validate(s) && WorkEvents.Validate(s) && TradeWork.Validate(s), message + " valid before JSON");
            string before = Json(s); s = JsonUtility.FromJson<GameState>(before); roundTrips++;
            Require(StoryEngine.Validate(s) && WorkEvents.Validate(s) && TradeWork.Validate(s), message + " valid after JSON");
            Require(Json(s) == before, message + " exact JSON state");
        }
        static GameState Fresh(string destination = "singapore")
        {
            var s = StoryEngine.NewGame(2300); s.nodeId = "first_pay"; s.journey.destination = destination;
            s.money = 100; s.health = 100; s.family = 50; s.trust = 25;
            Require(NanyangLife.TryBegin(s), "fresh Nanyang enrolment"); return s;
        }
        static void OldWork(GameState s, string route)
        {
            string notice; Require(LifeJourney.CommitAction(s, route, out notice), "previous work fixture");
            if (s.nanyang.stage == "choice") {
                Require(NanyangLife.ChooseResponse(s, "evidence"), "previous work dispute");
                for (int i = 0; i < 3; i++) Require(NanyangLife.CompleteStep(s, i, out notice), "previous dispute step");
                Require(NanyangLife.DeferLetter(s), "previous letter defer");
            }
        }
        static GameState Fixture(string route, int level, string destination = "singapore", bool enroll = true)
        {
            var s = Fresh(destination);
            for (int i = 1; i < level; i++) OldWork(s, route);
            s.money = 100; s.health = 70; s.trust = 30;
            Start(s, route, enroll); return s;
        }
        static void Start(GameState s, string route, bool enroll = true)
        {
            s.journey.pendingJob = route;
            Require(TradeWork.Begin(s, route), "start physical work");
            if (enroll) Move(s, () => WorkEvents.Enroll(s), "enrol event");
        }
        static void AtChoice(ref GameState s)
        {
            string notice; GameState state = s;
            Require(WorkEvents.HasIncident(s), "shift has an authored event");
            Require(WorkEvents.Choices(s).Count == 2 && WorkEvents.Title(s).Length > 0 && WorkEvents.Body(s).Length > 0, "two contextual choices");
            Reject(state, () => WorkEvents.Enroll(state), "cannot enrol twice");
            Reject(state, () => LifeJourney.CommitAction(state, state.tradeShift.route, out notice), "cannot claim pay before decision");
            if (s.tradeShift.route == "courier") {
                Require(!WorkEvents.Pending(s), "courier first reads envelope");
                Reject(state, () => WorkEvents.Choose(state, "trace", out notice), "courier cannot decide before reading");
                Move(state, () => TradeWork.CourierRead(state, out notice), "read before learning of delivery problem");
                Reload(ref s, "courier envelope read"); state = s;
                Reject(state, () => TradeWork.CourierAsk(state, out notice), "courier must choose how to handle problem");
            } else if (s.tradeShift.route == "dock") {
                int paper = TradeWork.DockPriority(s);
                Reject(state, () => TradeWork.DockPick(state, paper, out notice), "cannot carry before choosing division of work");
            } else Reject(state, () => TradeWork.AdjustWeight(state, 1), "cannot measure before agreeing order");
            Require(WorkEvents.Pending(s), "choice waits at authored work stage");
            Reload(ref s, "pending decision");
        }
        static void Branch(string destination, string route, int level, string id)
        {
            var s = Fixture(route, level, destination); AtChoice(ref s); GameState state = s; string notice;
            Reject(state, () => WorkEvents.Choose(state, "missing", out notice), "unknown event choice");
            WorkEventChoice choice = WorkEvents.Choices(s).Find(c => c.id == id);
            Require(choice != null, "authored choice exists");
            int expectedMoney, expectedHealth, expectedTrust, expectedRelation;
            Expected(route, level, id, out expectedMoney, out expectedHealth, out expectedTrust, out expectedRelation);
            Require(choice.money == expectedMoney && choice.health == expectedHealth && choice.trust == expectedTrust && choice.relation == expectedRelation, "choice effects match authored contract");
            Move(state, () => WorkEvents.Choose(state, id, out notice), "make decision once");
            Require(!WorkEvents.Pending(s) && WorkEvents.ChoiceId(s) == id, "decision retained");
            Reject(state, () => WorkEvents.Choose(state, Ids(route, level)[0], out notice), "cannot reroll choice");
            Require(WorkEvents.RelationshipScore(s, route) == 0 && WorkEvents.LastRecord(s, route) == null, "choice does not award friendship");
            Reload(ref s, "chosen event"); state = s;
            Reject(state, () => LifeJourney.CommitAction(state, route, out notice), "choice alone cannot claim wages");
            AssertTask(s, route, level, id);
            Complete(ref s);
            Require(TradeWork.IsComplete(s), "actual operations complete");
            Require(WorkEvents.RelationshipScore(s, route) == 0 && WorkEvents.LastRecord(s, route) == null, "work before payroll still unrecorded");
            Reload(ref s, "completed before payroll");
            Settle(ref s, route, level, id, expectedMoney, expectedHealth, expectedTrust, expectedRelation);
        }
        static void AssertTask(GameState s, string route, int level, string id)
        {
            if (route == "dock") {
                Require(TradeWork.DockCount(s) == (id == "pair" || id == "teach" ? 2 : 3), "division of cargo changes actual count");
                Require(TradeWork.DockPriority(s) == (level == 2 ? 0 : 1), "paper priority preserves parcel ordering");
                if (TradeWork.DockCount(s) == 2) Require(TradeWork.DockDestination(s, 0) != 1 && TradeWork.DockDestination(s, 1) != 1, "colleague handles cloth only");
            } else if (route == "shop") {
                int weight = id == "portion" ? 2 : id == "record" ? 5 : 3;
                int price = id == "portion" ? 4 : id == "record" ? 9 : 6;
                int tender = level == 2 ? 4 : 6;
                int debt = id == "credit" ? 2 : id == "record" ? 3 : 0;
                Require(TradeWork.ShopWeightTarget(s) == weight && TradeWork.ShopPrice(s) == price && TradeWork.ShopTender(s) == tender && TradeWork.ShopChangeTarget(s) == 0 && TradeWork.ShopDebtTarget(s) == debt, "order matches cash agreement");
            } else {
                Require(s.tradeShift.stage == (id == "return" ? "return" : "ask"), "courier decision selects real task");
                if (id != "return") Require(TradeWork.CourierRecipient(s) == 0 && TradeWork.CourierAddress(s) == "南街三号", "trace reaches new personal address");
            }
        }
        static void Complete(ref GameState s)
        {
            string notice = ""; GameState state = s;
            if (s.tradeShift.route == "dock") {
                int count = TradeWork.DockCount(s);
                for (int k = 0; k < count; k++) {
                    int index = TradeWork.DockPriority(s);
                    if (index < 0) { index = 0; while ((s.tradeShift.deliveredMask & (1 << index)) != 0) index++; }
                    int parcel = index; state = s;
                    Move(state, () => TradeWork.DockPick(state, parcel, out notice), "take agreed cargo");
                    Reload(ref s, "carrying agreed cargo"); state = s;
                    int zone = TradeWork.DockDestination(s, parcel);
                    Move(state, () => TradeWork.DockDrop(state, zone, out notice), "deliver agreed cargo");
                    Reload(ref s, "agreed cargo delivered");
                }
            } else if (s.tradeShift.route == "shop") {
                for (int k = 0; k < TradeWork.ShopWeightTarget(s); k++) Move(state, () => TradeWork.AdjustWeight(state, 1), "measure agreed quantity");
                Move(state, () => TradeWork.ConfirmWeight(state, out notice), "confirm agreed quantity");
                Reload(ref s, "revised shop order"); state = s;
                for (int k = 0; k < TradeWork.ShopChangeTarget(s); k++) Move(state, () => TradeWork.AdjustChange(state, 1), "count shop change");
                Move(state, () => TradeWork.ConfirmChange(state, out notice), "confirm actual cash");
                Reload(ref s, "actual shop cash confirmed"); state = s;
                int debt = TradeWork.ShopDebtTarget(s);
                if (debt > 0) {
                    Require(s.tradeShift.stage == "credit", "unpaid balance requires ledger entry at either level");
                    for (int k = 0; k < debt; k++) Move(state, () => TradeWork.AdjustDebt(state, 1), "record actual balance");
                    Move(state, () => TradeWork.ConfirmDebt(state, out notice), "confirm actual balance");
                    Require(notice.Contains("现钱与赊欠"), "credit result distinguishes collected money");
                    Reload(ref s, "credit ledger");
                } else Require(s.tradeShift.stage == "done", "cash agreement removes unnecessary credit step");
            } else if (WorkEvents.Returning(s)) {
                Reject(state, () => TradeWork.CourierReceipt(state, 0, out notice), "return has no received letter receipt");
                Reject(state, () => TradeWork.CourierDeliver(state, 0, out notice), "return cannot deliver along the way");
                Reject(state, () => LifeJourney.CommitAction(state, "courier", out notice), "return must first register at desk");
                Move(state, () => TradeWork.CourierReturn(state, out notice), "register original unopened return");
                Require(s.tradeShift.recipient == -1 && notice.Contains("未交"), "return does not invent successful recipient");
                Reject(state, () => TradeWork.CourierReturn(state, out notice), "return registration cannot repeat");
                Reload(ref s, "registered return");
            } else {
                int recipient = TradeWork.CourierRecipient(s);
                Move(state, () => TradeWork.CourierAsk(state, out notice), "follow agreed address enquiry");
                Reload(ref s, "followed enquiry"); state = s;
                Reject(state, () => TradeWork.CourierCheck(state, 1 - recipient, out notice), "same name at wrong address rejected");
                Move(state, () => TradeWork.CourierCheck(state, recipient, out notice), "verify person at confirmed residence");
                Reload(ref s, "verified delivery"); state = s;
                Move(state, () => TradeWork.CourierDeliver(state, recipient, out notice), "personally hand over letter");
                Reload(ref s, "personal delivery"); state = s;
                Reject(state, () => LifeJourney.CommitAction(state, "courier", out notice), "pay waits for signed receipt");
                Move(state, () => TradeWork.CourierReceipt(state, recipient, out notice), "receive signed receipt");
                Reload(ref s, "signed receipt");
            }
        }
        static void Settle(ref GameState s, string route, int level, string id, int dm, int dh, int dt, int dr)
        {
            var basePay = NanyangLife.WorkReward(s, route); string notice;
            int money = s.money, health = s.health, trust = s.trust, turn = s.journey.turns;
            int count = s.workMemory == null || s.workMemory.records == null ? 0 : s.workMemory.records.Count;
            int relation = WorkEvents.RelationshipScore(s, route);
            Require(LifeJourney.CommitAction(s, route, out notice), "settle completed incident");
            Require(s.money == Math.Min(9999, money + basePay.money + dm) && s.health == Clamp(health + basePay.health + dh) && s.trust == Clamp(trust + basePay.trust + dt), "single combined resource settlement");
            Require(s.journey.turns == turn + 1 && !TradeWork.IsActive(s), "settlement advances one turn and clears shift");
            Require(s.workMemory.version == 1 && s.workMemory.records.Count == count + 1, "one event record appended");
            WorkEventRecord record = WorkEvents.LastRecord(s, route);
            Require(record != null && record.route == route && record.level == level && record.choice == id && record.turn == turn + 1 && record.returned == (route == "courier" && id == "return"), "record is actual completed choice");
            Require(WorkEvents.RelationshipScore(s, route) == relation + dr, "relationship derived only from settled evidence");
            if (record.returned) Require(notice.Contains("未能交付") && notice.Contains("没有收件人回执"), "returned payroll never describes delivery success");
            Reload(ref s, "settled event memory"); GameState state = s;
            Reject(state, () => WorkEvents.Choose(state, id, out notice), "old event action cannot pay twice");
            Reject(state, () => TradeWork.CourierReturn(state, out notice), "old return action cannot pay twice");
        }
        static void Expected(string route, int level, string id, out int money, out int health, out int trust, out int relation)
        {
            money = health = trust = relation = 0;
            if (route == "dock") {
                if (id == "cover") { health = -2; trust = 1; }
                if (id == "pair") { money = -2; health = 2; relation = 2; }
                if (id == "teach") { money = -1; health = 1; trust = 2; relation = 2; }
                if (id == "solo") { money = 2; health = -2; }
            } else if (route == "shop") {
                if (id == "credit") { trust = 1; relation = 1; }
                if (id == "record") { money = 1; trust = 2; relation = 2; }
            } else if (id == "return") { money = level == 2 ? -6 : -7; health = 3; }
            else { health = level == 2 ? -2 : -1; trust = 2; relation = 2; }
        }
        static void Compatibility()
        {
            Require(!WorkEvents.Validate(null) && !WorkEvents.HasIncident(null) && !WorkEvents.Pending(null), "null state safely rejected");
            var old = StoryEngine.NewGame(2300);
            Require(WorkEvents.Validate(old), "old state without memory allowed");
            old.workMemory = new WorkMemory(); Require(WorkEvents.Validate(old), "materialized version-zero memory allowed");
            old.workMemory.records = null; Require(WorkEvents.Validate(old), "old null records allowed");
            Reload(ref old, "empty legacy memory");
            foreach (string route in new[] { "dock", "shop", "courier" }) {
                var first = Fixture(route, 1, "singapore", false); string before = Json(first);
                Require(!WorkEvents.Enroll(first) && Json(first) == before, "first career level has no incident");
                for (int level = 2; level <= 3; level++) {
                    var s = Fixture(route, level, "singapore", false);
                    Require(!WorkEvents.HasIncident(s) && !WorkEvents.Pending(s), "2.2 existing shift not auto-upgraded");
                    s.tradeShift.incident = new WorkShiftEvent();
                    Reload(ref s, "2.2 zero-version incident");
                    if (route == "courier") { string n; Require(TradeWork.CourierRead(s, out n), "legacy courier reads"); }
                    Complete(ref s);
                    string notice; Require(LifeJourney.CommitAction(s, route, out notice), "legacy shift pays normally");
                    Require(WorkEvents.LastRecord(s, route) == null && WorkEvents.RelationshipScore(s, route) == 0, "legacy work creates no event memories");
                }
            }
        }
        static void Abandon()
        {
            foreach (string route in new[] { "dock", "shop", "courier" }) {
                var s = Fixture(route, 2); AtChoice(ref s); string notice; GameState state = s;
                string id = route == "dock" ? "pair" : route == "shop" ? "credit" : "trace";
                Move(state, () => WorkEvents.Choose(state, id, out notice), "abandon fixture choice");
                string before = OutsideShift(s); TradeWork.Clear(s);
                Require(OutsideShift(s) == before && WorkEvents.LastRecord(s, route) == null && WorkEvents.RelationshipScore(s, route) == 0, "clearing abandoned work creates no resources or relationship");
                s.journey.pendingJob = ""; Reload(ref s, "abandoned work");
                Start(s, route); AtChoice(ref s);
                Require(WorkEvents.Choices(s).Count == 2 && WorkEvents.ChoiceId(s) == "", "restart has same authored choices and no rerolled reward");
            }
        }
        static void Relationships()
        {
            var s = Fixture("dock", 2); AtChoice(ref s); string notice;
            Require(WorkEvents.Choose(s, "pair", out notice), "first shared work"); Complete(ref s);
            Settle(ref s, "dock", 2, "pair", -2, 2, 0, 2);
            Start(s, "dock"); AtChoice(ref s); Require(WorkEvents.Choose(s, "teach", out notice), "second shared work"); Complete(ref s);
            Require(WorkEvents.RelationshipScore(s, "dock") == 2, "unfinished second payroll cannot grow relation");
            Settle(ref s, "dock", 3, "teach", -1, 1, 2, 2);
            Require(WorkEvents.RelationshipScore(s, "dock") == 4 && WorkEvents.RelationshipScore(s, "shop") == 0 && WorkEvents.RelationshipLabel(s, "dock").Contains("照应"), "relationship accumulates only for relevant career");
        }
        static void Bad(GameState original, Action<GameState> change, string label)
        { var copy = JsonUtility.FromJson<GameState>(Json(original)); change(copy); Require(!StoryEngine.Validate(copy), label); }
        static void Invalid()
        {
            var dock = Fixture("dock", 2); AtChoice(ref dock);
            Bad(dock, s => s.tradeShift.incident.version = 2, "unknown event version rejected");
            Bad(dock, s => s.tradeShift.incident.choice = "trace", "cross-route choice rejected");
            Bad(dock, s => s.tradeShift.held = 0, "unresolved event cannot have made progress");
            Bad(dock, s => { s.tradeShift.incident.version = 0; s.tradeShift.incident.choice = "pair"; }, "nonempty zero-version incident rejected");
            var courier = Fixture("courier", 2);
            Bad(courier, s => s.tradeShift.incident.choice = "trace", "courier choice before reading rejected");
            Bad(courier, s => { s.tradeShift.incident.choice = "return"; s.tradeShift.stage = "deliver"; s.tradeShift.recipient = 1; }, "return cannot contain delivered recipient");
            Bad(courier, s => s.workMemory = new WorkMemory { version = 2 }, "unknown memory version rejected");
            Bad(courier, s => { s.workMemory = new WorkMemory(); s.workMemory.records.Add(new WorkEventRecord()); }, "old version cannot hold records");
            string notice; var settled = Fixture("shop", 2); AtChoice(ref settled); Require(WorkEvents.Choose(settled, "credit", out notice), "invalid memory fixture choice"); Complete(ref settled);
            Require(LifeJourney.CommitAction(settled, "shop", out notice), "invalid memory fixture pay");
            Bad(settled, s => s.workMemory.records[0].turn++, "future payroll record rejected");
            Bad(settled, s => s.workMemory.records[0].level = 3, "record level must match actual experience");
            Bad(settled, s => s.workMemory.records[0].route = "dock", "record must match actual work log");
            Bad(settled, s => s.workMemory.records[0].returned = true, "shop is not returned qiaopi");
            Bad(settled, s => s.workMemory.records.Add(s.workMemory.records[0]), "duplicate settlement record rejected");
            Bad(settled, s => { for (int i = 0; i < 6; i++) s.workMemory.records.Add(s.workMemory.records[0]); }, "event record capacity bounded by six rounds");
        }
        static void Caps()
        {
            var s = Fixture("dock", 2); AtChoice(ref s); string notice;
            Require(WorkEvents.Choose(s, "pair", out notice), "cap fixture"); Complete(ref s);
            s.money = 9999; s.health = 100; s.trust = 100;
            Settle(ref s, "dock", 2, "pair", -2, 2, 0, 2);
            Require(s.money == 9999 && s.health == 94, "base work and incident are combined before clamping");
        }
        static int Clamp(int value) { return Math.Max(0, Math.Min(100, value)); }
    }
}
