using System;

namespace Qiaopi
{
    // A fresh, explicitly labelled preview background. It has no disk or prefs
    // access and never reads the user's ongoing character or archived letters.
    // Every remembered job is actually settled through the production rules.
    public static class InquiryDemo
    {
        public static GameState Create() { return Create("singapore"); }

        public static GameState Create(string destination)
        {
            var s = StoryEngine.NewGame(20261011);
            s.journey.destination = destination;
            Require(StoryEngine.Validate(s), "destination");
            foreach (string choice in new[] { "meal", "protect", "quilt", "borrow", "mother", "berth", "shop", "learn" }) Advance(s, choice);
            Require(NanyangLife.TryBegin(s), "working life");
            Work(s, "dock", "");
            Require(NanyangLife.ChooseResponse(s, "leave"), "change employer after withheld wages");
            string notice;
            for (int i = 0; i < 2; i++) Require(NanyangLife.CompleteStep(s, i, out notice), "settle wage response");
            Require(NanyangLife.DeferLetter(s), "close the first work episode");
            Work(s, "dock", "pair");
            Work(s, "shop", ""); Work(s, "shop", "credit");
            Work(s, "courier", ""); Work(s, "courier", "trace");
            Require(LifeJourney.Finish(s), "finish six work rounds");
            foreach (string choice in new[] { "reserve", "reassure", "medicine", "shop_record" }) Advance(s, choice);
            Require(s.nodeId == "missing" && s.inquiryVersion == 0 && s.workMemory.records.Count == 3 && StoryEngine.Validate(s), "ready preview");
            return s;
        }

        static void Advance(GameState s, string id)
        { Require(StoryEngine.Choose(s, id) && StoryEngine.Continue(s), "story choice " + id); }

        static void Work(GameState s, string route, string choice)
        {
            string notice;
            s.journey.pendingJob = route;
            Require(TradeWork.Begin(s, route), "accept " + route);
            if (!string.IsNullOrEmpty(choice)) Require(WorkEvents.Enroll(s), "enroll experienced work");
            if (route == "courier") Require(TradeWork.CourierRead(s, out notice), "read batch");
            if (!string.IsNullOrEmpty(choice)) Require(WorkEvents.Choose(s, choice, out notice), "choose " + choice);
            if (route == "dock") {
                int count = TradeWork.DockCount(s);
                for (int j = 0; j < count; j++) {
                    int index = TradeWork.DockPriority(s);
                    if (index < 0) { index = 0; while ((s.tradeShift.deliveredMask & (1 << index)) != 0) index++; }
                    Require(TradeWork.DockPick(s, index, out notice), "pick agreed cargo");
                    Require(TradeWork.DockDrop(s, TradeWork.DockDestination(s, index), out notice), "deliver agreed cargo");
                }
            } else if (route == "shop") {
                for (int j = 0; j < TradeWork.ShopWeightTarget(s); j++) Require(TradeWork.AdjustWeight(s, 1), "weigh");
                Require(TradeWork.ConfirmWeight(s, out notice), "confirm weight");
                for (int j = 0; j < TradeWork.ShopChangeTarget(s); j++) Require(TradeWork.AdjustChange(s, 1), "count change");
                Require(TradeWork.ConfirmChange(s, out notice), "confirm change");
                if (s.tradeShift.stage == "credit") {
                    for (int j = 0; j < TradeWork.ShopDebtTarget(s); j++) Require(TradeWork.AdjustDebt(s, 1), "note debt");
                    Require(TradeWork.ConfirmDebt(s, out notice), "confirm debt");
                }
            } else {
                int recipient = TradeWork.CourierRecipient(s);
                Require(TradeWork.CourierAsk(s, out notice), "ask address");
                Require(TradeWork.CourierCheck(s, recipient, out notice), "verify recipient");
                Require(TradeWork.CourierDeliver(s, recipient, out notice), "deliver personally");
                Require(TradeWork.CourierReceipt(s, recipient, out notice), "take receipt");
            }
            Require(TradeWork.IsComplete(s) && LifeJourney.CommitAction(s, route, out notice), "settle " + route);
        }
        static void Require(bool ok, string label)
        { if (!ok) throw new InvalidOperationException("Inquiry preview background: " + label); }
    }
}
