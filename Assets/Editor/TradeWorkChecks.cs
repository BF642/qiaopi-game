using System;
using UnityEngine;

namespace Qiaopi.Editor
{
    // In-memory rule and serializer checks only; no player's saves or preferences.
    public static class TradeWorkChecks
    {
        static int assertions, roundTrips;
        public static void Run()
        {
            assertions = roundTrips = 0;
            Legacy();
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
                foreach (string route in new[] { "dock", "shop", "courier" })
                    for (int level = 1; level <= 3; level++) {
                        var s = Fixture(route, level, destination);
                        if (route == "dock") Dock(ref s); else if (route == "shop") Shop(ref s); else Courier(ref s);
                        Settlement(ref s, route);
                    }
            Invalid(); Boundaries();
            Debug.Log("TRADE_WORK_CHECKS_PASSED: 3 destinations × 3 careers × 3 levels; dock priority and delivery, weight/change/credit, read/ask/verify/deliver/receipt, rejected/replayed actions, serialized progress, single settlement and old pending compatibility; assertions=" + assertions + ", in-memory save round-trips=" + roundTrips + ". Player saves/preferences untouched.");
        }
        static void Require(bool condition, string message)
        { assertions++; if (!condition) throw new Exception("Trade work checks: " + message); }
        static string Snapshot(GameState s) { return JsonUtility.ToJson(s); }
        static string OutsideShift(GameState s)
        { var copy = JsonUtility.FromJson<GameState>(Snapshot(s)); copy.tradeShift = null; return Snapshot(copy); }
        static void Reject(GameState s, Func<bool> action, string message)
        { string before = Snapshot(s); Require(!action(), message + " rejects"); Require(Snapshot(s) == before, message + " leaves entire state intact"); }
        static void Move(GameState s, Func<bool> action, string message)
        { string before = OutsideShift(s); Require(action(), message + " succeeds"); Require(OutsideShift(s) == before, message + " changes only work progress"); Require(TradeWork.Validate(s), message + " valid"); }
        static void Reload(ref GameState s, string message)
        {
            Require(StoryEngine.Validate(s) && TradeWork.Validate(s), message + " valid before save");
            string before = Snapshot(s);
            s = JsonUtility.FromJson<GameState>(before); roundTrips++;
            Require(StoryEngine.Validate(s) && TradeWork.Validate(s), message + " valid after save");
            Require(Snapshot(s) == before, message + " exact round trip");
        }
        static GameState Fresh(string destination = "singapore")
        {
            var s = StoryEngine.NewGame(2200); s.nodeId = "first_pay"; s.journey.destination = destination;
            s.money = 100; s.health = 100; s.family = 50; s.trust = 30;
            Require(NanyangLife.TryBegin(s), "enrol fresh chapter");
            Require(TradeWork.Validate(s), "null work allowed"); return s;
        }
        static void LegacyWork(GameState s, string route)
        {
            string notice; Require(LifeJourney.CommitAction(s, route, out notice), "fixture previous work");
            if (s.nanyang.stage == "choice") {
                Require(NanyangLife.ChooseResponse(s, "evidence"), "fixture response");
                for (int i = 0; i < 3; i++) Require(NanyangLife.CompleteStep(s, i, out notice), "fixture response step");
                Require(NanyangLife.DeferLetter(s), "fixture defer");
            }
        }
        static GameState Fixture(string route, int level, string destination = "singapore")
        {
            var s = Fresh(destination);
            for (int i = 1; i < level; i++) LegacyWork(s, route);
            s.journey.pendingJob = route;
            string before = OutsideShift(s);
            Require(TradeWork.Begin(s, route), "new " + route + " level " + level);
            Require(OutsideShift(s) == before && s.tradeShift.level == level, "begin only creates expected-level work");
            Require(TradeWork.IsActive(s) && !TradeWork.IsComplete(s), "work begins active, unfinished");
            Reject(s, () => TradeWork.Begin(s, route), "cannot reset active work");
            string notice;
            Reject(s, () => LifeJourney.CommitAction(s, route, out notice), "cannot settle before physical work");
            Reload(ref s, "new shift"); return s;
        }
        static void Dock(ref GameState s)
        {
            GameState state = s; string notice;
            int count = TradeWork.DockCount(s), priority = TradeWork.DockPriority(s);
            Require(count == (s.tradeShift.level == 1 ? 2 : 3), "cargo count");
            Require(priority == (s.tradeShift.level == 1 ? -1 : s.tradeShift.level == 2 ? 0 : 1), "weather priority");
            Reject(state, () => TradeWork.DockPick(state, -1, out notice), "negative parcel");
            Reject(state, () => TradeWork.DockPick(state, count, out notice), "missing parcel");
            Reject(state, () => TradeWork.DockDrop(state, 0, out notice), "cannot drop empty hands");
            Reject(state, () => TradeWork.ConfirmWeight(state, out notice), "dock cannot run shop operation");
            if (priority >= 0) Reject(state, () => TradeWork.DockPick(state, (priority + 1) % count, out notice), "paper goes first");
            for (int n = 0; n < count; n++) {
                int index = TradeWork.DockPriority(s);
                if (index < 0) { index = 0; while ((s.tradeShift.deliveredMask & (1 << index)) != 0) index++; }
                int current = index; state = s;
                Require(TradeWork.DockLabel(s, index).Contains(TradeWork.DockDestination(s, index) == 0 ? "米粮" : TradeWork.DockDestination(s, index) == 1 ? "布匹" : "纸货"), "cargo label matches destination");
                Move(state, () => TradeWork.DockPick(state, current, out notice), "pick parcel");
                Reject(state, () => TradeWork.DockPick(state, current, out notice), "cannot pick again while carrying");
                Reload(ref s, "held parcel"); state = s;
                int zone = TradeWork.DockDestination(s, index);
                Reject(state, () => TradeWork.DockDrop(state, (zone + 1) % 3, out notice), "wrong warehouse");
                Move(state, () => TradeWork.DockDrop(state, zone, out notice), "deliver parcel");
                Reject(state, () => TradeWork.DockDrop(state, zone, out notice), "cannot drop same parcel twice");
                Reject(state, () => TradeWork.DockPick(state, current, out notice), "cannot take delivered parcel");
                Reload(ref s, "delivered parcel");
            }
            Require(s.tradeShift.held == -1 && s.tradeShift.deliveredMask == (1 << count) - 1 && TradeWork.DockPriority(s) == -1 && TradeWork.IsComplete(s), "all cargo complete");
        }
        static void Shop(ref GameState s)
        {
            GameState state = s; string notice;
            Reject(state, () => TradeWork.ConfirmWeight(state, out notice), "empty scale rejected");
            Reject(state, () => TradeWork.AdjustWeight(state, -1), "no negative weight");
            Reject(state, () => TradeWork.AdjustWeight(state, int.MaxValue), "weight overflow rejected");
            Reject(state, () => TradeWork.AdjustChange(state, 1), "cannot make change before weighing");
            Move(state, () => TradeWork.AdjustWeight(state, 2), "add one jin scoop");
            Move(state, () => TradeWork.AdjustWeight(state, -2), "remove one jin scoop");
            int weight = TradeWork.ShopWeightTarget(s);
            for (int i = 0; i <= weight; i++) Move(state, () => TradeWork.AdjustWeight(state, 1), "add half jin");
            Reject(state, () => TradeWork.ConfirmWeight(state, out notice), "overweight rejected");
            Move(state, () => TradeWork.AdjustWeight(state, -1), "remove extra half jin");
            Reload(ref s, "weighed amount"); state = s;
            Move(state, () => TradeWork.ConfirmWeight(state, out notice), "confirm weighed order");
            Reject(state, () => TradeWork.ConfirmWeight(state, out notice), "weighing cannot advance twice");
            Reject(state, () => TradeWork.AdjustWeight(state, 1), "confirmed weight locked");
            Reject(state, () => TradeWork.AdjustChange(state, -1), "no negative change");
            Reject(state, () => TradeWork.AdjustChange(state, 0), "zero coin invalid");
            Reject(state, () => TradeWork.AdjustChange(state, int.MaxValue), "coin overflow rejected");
            int change = TradeWork.ShopChangeTarget(s);
            Require(change == (s.tradeShift.level == 1 ? 6 : s.tradeShift.level == 2 ? 4 : 0), "correct change for tender");
            Move(state, () => TradeWork.AdjustChange(state, 5), "put five in change tray");
            Reject(state, () => TradeWork.ConfirmChange(state, out notice), "wrong change rejected");
            Move(state, () => TradeWork.AdjustChange(state, -5), "take five back");
            Move(state, () => TradeWork.AdjustChange(state, 2), "put two in change tray");
            Move(state, () => TradeWork.AdjustChange(state, -2), "take two back");
            for (int i = 0; i < change; i++) Move(state, () => TradeWork.AdjustChange(state, 1), "count change");
            Reload(ref s, "counted change"); state = s;
            Move(state, () => TradeWork.ConfirmChange(state, out notice), "hand over accurate change");
            Reject(state, () => TradeWork.ConfirmChange(state, out notice), "cannot hand over change twice");
            Reject(state, () => TradeWork.AdjustChange(state, -1), "confirmed change locked");
            if (s.tradeShift.level == 3) {
                Require(s.tradeShift.stage == "credit" && !TradeWork.IsComplete(s) && TradeWork.ShopPrice(s) - TradeWork.ShopTender(s) == TradeWork.ShopDebtTarget(s), "third level records actual unpaid balance");
                Reject(state, () => TradeWork.ConfirmDebt(state, out notice), "empty debt ledger rejected");
                Reject(state, () => TradeWork.AdjustDebt(state, -1), "no negative debt");
                Reject(state, () => TradeWork.AdjustDebt(state, int.MinValue), "debt overflow rejected");
                for (int i = 0; i < TradeWork.ShopDebtTarget(s); i++) Move(state, () => TradeWork.AdjustDebt(state, 1), "record credit separately");
                Reload(ref s, "credit draft"); state = s;
                Move(state, () => TradeWork.ConfirmDebt(state, out notice), "confirm credit amount");
                Reject(state, () => TradeWork.ConfirmDebt(state, out notice), "cannot record credit twice");
            } else Reject(state, () => TradeWork.AdjustDebt(state, 1), "junior shop shift has no invented credit task");
            Require(TradeWork.IsComplete(s), "shop finished"); Reload(ref s, "finished shop shift");
        }
        static void Courier(ref GameState s)
        {
            GameState state = s; string notice;
            int correct = TradeWork.CourierRecipient(s), wrong = 1 - correct;
            Require(correct == (s.tradeShift.level == 2 ? 1 : 0), "recipient varies by shift");
            Require(TradeWork.CourierAddress(s) == (correct == 0 ? "南街三号" : "北巷七号"), "recipient matches real address");
            Reject(state, () => TradeWork.CourierAsk(state, out notice), "cannot skip reading envelope");
            Reject(state, () => TradeWork.CourierCheck(state, correct, out notice), "cannot skip asking neighbour");
            Reject(state, () => TradeWork.CourierDeliver(state, correct, out notice), "cannot deliver before verification");
            Reject(state, () => TradeWork.CourierReceipt(state, correct, out notice), "cannot take receipt before delivery");
            Move(state, () => TradeWork.CourierRead(state, out notice), "read envelope");
            Reject(state, () => TradeWork.CourierRead(state, out notice), "reading cannot advance twice");
            Reload(ref s, "read envelope"); state = s;
            Move(state, () => TradeWork.CourierAsk(state, out notice), "ask neighbour for address");
            Reject(state, () => TradeWork.CourierAsk(state, out notice), "asking cannot advance twice");
            Reload(ref s, "asked neighbour"); state = s;
            Reject(state, () => TradeWork.CourierCheck(state, -1, out notice), "no missing recipient");
            Reject(state, () => TradeWork.CourierCheck(state, 2, out notice), "no unknown recipient");
            Reject(state, () => TradeWork.CourierCheck(state, wrong, out notice), "same name wrong address rejected");
            Move(state, () => TradeWork.CourierCheck(state, correct, out notice), "verify identity and residence");
            Reject(state, () => TradeWork.CourierCheck(state, correct, out notice), "verification cannot advance twice");
            Reload(ref s, "verified recipient"); state = s;
            Reject(state, () => TradeWork.CourierDeliver(state, wrong, out notice), "cannot swap person after verification");
            Move(state, () => TradeWork.CourierDeliver(state, correct, out notice), "hand over qiaopi");
            Reject(state, () => TradeWork.CourierDeliver(state, correct, out notice), "cannot deliver twice");
            Reload(ref s, "delivered qiaopi"); state = s;
            Require(!TradeWork.IsComplete(s), "delivery alone is not completed shift");
            Reject(state, () => TradeWork.CourierReceipt(state, wrong, out notice), "cannot take another person's receipt");
            Move(state, () => TradeWork.CourierReceipt(state, correct, out notice), "receive signed receipt");
            Reject(state, () => TradeWork.CourierReceipt(state, correct, out notice), "cannot take receipt twice");
            Require(TradeWork.IsComplete(s), "courier finished"); Reload(ref s, "receipt held");
        }
        static void Settlement(ref GameState s, string route)
        {
            string notice; int money = s.money, turn = s.journey.turns;
            int wage = NanyangLife.WorkReward(s, route).money - (s.nanyang.stage == "free" ? 4 : 0);
            int decisions = s.decisions, history = s.history.Count;
            Require(TradeWork.IsComplete(s) && LifeJourney.CommitAction(s, route, out notice), "finished shift settles");
            Require(s.money == money + wage && s.journey.turns == turn + 1 && s.journey.pendingJob == "", "wage and turn applied once by life settlement");
            Require(!TradeWork.IsActive(s) && !TradeWork.IsComplete(s) && (s.tradeShift == null || s.tradeShift.version == 0), "settlement clears work state");
            Require(s.decisions == decisions && s.history.Count == history, "story choice counters unchanged");
            GameState state = s;
            Reject(state, () => TradeWork.DockDrop(state, 0, out notice), "old dock action cannot pay again");
            Reject(state, () => TradeWork.ConfirmChange(state, out notice), "old shop action cannot pay again");
            Reject(state, () => TradeWork.CourierReceipt(state, 0, out notice), "old courier action cannot pay again");
            Reload(ref s, "settled shift");
        }
        static void Legacy()
        {
            Require(!TradeWork.Validate(null) && !TradeWork.IsActive(null) && !TradeWork.Begin(null, "dock"), "null game rejected");
            var s = StoryEngine.NewGame(7); Require(TradeWork.Validate(s) && !TradeWork.IsActive(s), "ordinary old state remains inert");
            s.tradeShift = new TradeShift(); Require(TradeWork.Validate(s) && !TradeWork.IsActive(s), "materialized empty object remains inert");
            s.tradeShift.held = s.tradeShift.recipient = 0; Require(TradeWork.Validate(s), "zero initialized inline object allowed");
            Reload(ref s, "old version-zero object");
            s = Fresh(); s.journey.pendingJob = "dock";
            Reload(ref s, "old pending dock with null work field");
            Require(!TradeWork.IsActive(s), "old pending job not implicitly opted in");
            string notice; Require(LifeJourney.CommitAction(s, "dock", out notice), "old pending job keeps original settlement");
            Require(StoryEngine.Validate(s), "old settlement remains valid");
            s = Fresh(); Reject(s, () => TradeWork.Begin(s, "dock"), "begin requires already selected matching job");
            s.journey.pendingJob = "shop"; Reject(s, () => TradeWork.Begin(s, "dock"), "begin cannot replace selected route");
            s.journey.pendingJob = "rest"; Reject(s, () => TradeWork.Begin(s, "rest"), "rest is not trade work");
            TradeWork.Clear(s); TradeWork.Clear(s); TradeWork.Clear(null);
            Require(TradeWork.Validate(s), "clear is repeat safe");
        }
        static void Bad(GameState original, Action<GameState> corrupt, string message)
        { var copy = JsonUtility.FromJson<GameState>(Snapshot(original)); corrupt(copy); Require(!TradeWork.Validate(copy), message); }
        static void Invalid()
        {
            var dock = Fixture("dock", 2);
            Bad(dock, s => s.tradeShift.version = 2, "unknown work version rejected");
            Bad(dock, s => s.tradeShift.route = "rest", "invalid route rejected");
            Bad(dock, s => s.journey.pendingJob = "shop", "stale route rejected");
            Bad(dock, s => s.tradeShift.level = 1, "forged career level rejected");
            Bad(dock, s => s.tradeShift.stage = "receipt", "cross-route stage rejected");
            Bad(dock, s => s.tradeShift.deliveredMask = -1, "negative mask rejected");
            Bad(dock, s => s.tradeShift.deliveredMask = 8, "out-of-range parcel bit rejected");
            Bad(dock, s => s.tradeShift.deliveredMask = 2, "delivered nonpriority before paper rejected");
            Bad(dock, s => s.tradeShift.held = 1, "held nonpriority before paper rejected");
            Bad(dock, s => s.tradeShift.stage = "done", "incomplete cargo cannot be marked done");
            Bad(dock, s => s.tradeShift.weightUnits = 1, "foreign shift counters rejected");
            Bad(dock, s => s.journey.completed = true, "work after completed life rejected");
            Bad(dock, s => s.nodeId = "reply", "work at wrong story node rejected");
            Bad(dock, s => s.nanyang.stage = "choice", "work during blocking incident rejected");
            var shop = Fixture("shop", 3);
            Bad(shop, s => s.tradeShift.weightUnits = -1, "negative scale rejected");
            Bad(shop, s => s.tradeShift.weightUnits = TradeWork.MaxWeightUnits + 1, "oversized scale rejected");
            Bad(shop, s => s.tradeShift.change = 1, "change before weighing rejected");
            Bad(shop, s => s.tradeShift.debt = 1, "credit before change rejected");
            Bad(shop, s => s.tradeShift.stage = "change", "change without completed weight rejected");
            Bad(shop, s => s.tradeShift.stage = "done", "unfinished shop cannot be done");
            var courier = Fixture("courier", 2);
            Bad(courier, s => s.tradeShift.recipient = 1, "recipient before verification rejected");
            Bad(courier, s => { s.tradeShift.stage = "deliver"; s.tradeShift.recipient = 0; }, "incorrect verified recipient rejected");
            Bad(courier, s => s.tradeShift.stage = "done", "finished delivery without recipient rejected");
            var empty = Fresh(); empty.tradeShift = new TradeShift { route = "dock" };
            Require(!TradeWork.Validate(empty) && !TradeWork.Begin(empty, "dock"), "version-zero nonempty shift rejected");
        }
        static void Boundaries()
        {
            string notice; var s = Fixture("shop", 1);
            for (int i = 0; i < TradeWork.MaxWeightUnits; i++) Require(TradeWork.AdjustWeight(s, 1), "scale upper bound setup");
            Reject(s, () => TradeWork.AdjustWeight(s, 1), "scale upper bound");
            while (s.tradeShift.weightUnits > TradeWork.ShopWeightTarget(s)) Require(TradeWork.AdjustWeight(s, -1), "restore scale");
            Require(TradeWork.ConfirmWeight(s, out notice), "boundary weight confirmed");
            for (int i = 0; i < 6; i++) Require(TradeWork.AdjustChange(s, 5), "coin upper bound setup");
            Reject(s, () => TradeWork.AdjustChange(s, 1), "coin upper bound");
            s = Fixture("shop", 3);
            for (int i = 0; i < TradeWork.ShopWeightTarget(s); i++) Require(TradeWork.AdjustWeight(s, 1), "credit fixture weight");
            Require(TradeWork.ConfirmWeight(s, out notice), "credit fixture scale");
            for (int i = 0; i < TradeWork.ShopChangeTarget(s); i++) Require(TradeWork.AdjustChange(s, 1), "credit fixture change");
            Require(TradeWork.ConfirmChange(s, out notice), "credit fixture cash");
            for (int i = 0; i < 4; i++) Require(TradeWork.AdjustDebt(s, 5), "ledger upper bound setup");
            Reject(s, () => TradeWork.AdjustDebt(s, 1), "ledger upper bound");
            s = Fresh(); for (int i = 0; i < 3; i++) LegacyWork(s, "shop");
            s.journey.pendingJob = "shop"; Reject(s, () => TradeWork.Begin(s, "shop"), "existing three-shift career cap retained");
        }
    }
}
