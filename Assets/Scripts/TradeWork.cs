using System;

namespace Qiaopi
{
    [Serializable]
    public sealed class TradeShift
    {
        // Zero is deliberately inert, including when JsonUtility materializes an
        // empty inline object from an older save. Only Begin opts a new job in.
        public int version;
        public string route = "", stage = "";
        public int level;
        public int held = -1, deliveredMask;
        public int weightUnits, change, debt;
        public int recipient = -1;
        public WorkShiftEvent incident;
    }

    // These operations record physical work only. The existing LifeJourney
    // settlement remains the sole authority for wages, fatigue and story turns.
    public static class TradeWork
    {
        public const int CurrentVersion = 1;
        public const int MaxWeightUnits = 10, MaxChange = 30, MaxDebt = 20;

        public static bool IsActive(GameState s)
        { return s != null && s.tradeShift != null && s.tradeShift.version == CurrentVersion && Context(s, s.tradeShift.route); }
        public static bool IsComplete(GameState s)
        { return IsActive(s) && Validate(s) && s.tradeShift.stage == "done"; }

        public static bool Begin(GameState s, string route)
        {
            if (s == null || !Route(route) || !Empty(s.tradeShift) || !Context(s, route)) return false;
            var action = LifeJourney.Actions(s).Find(a => a.id == route && a.enabled);
            if (action == null) return false;
            s.tradeShift = new TradeShift { version = CurrentVersion, route = route,
                level = NanyangLife.CareerLevel(s, route), stage = route == "dock" ? "carry" : route == "shop" ? "weigh" : "read" };
            return true;
        }

        public static void Clear(GameState s) { if (s != null) s.tradeShift = null; }

        public static int DockCount(GameState s)
        { return Has(s, "dock") ? (s.tradeShift.level == 1 || WorkEvents.ChoiceId(s) == "pair" || WorkEvents.ChoiceId(s) == "teach" ? 2 : 3) : 0; }
        public static string DockLabel(GameState s, int index)
        {
            int destination = DockDestination(s, index);
            if (LifeJourney.DestinationId(s)=="semarang") return destination==0?"糖袋 · 暂存点":destination==1?"布匹箱 · 交接区":destination==2?"糖货包 · 怕潮，先入棚":"";
            return destination == 0 ? "米粮包 · 粮栈" : destination == 1 ? "布匹箱 · 布栈" : destination == 2 ? "纸货箱 · 怕潮，先入棚" : "";
        }
        public static int DockDestination(GameState s, int index)
        {
            if (!Has(s, "dock") || index < 0 || index >= DockCount(s)) return -1;
            int level = s.tradeShift.level;
            return level == 1 ? index : level == 2 ? (index == 0 ? 2 : index - 1) : (index == 1 ? 2 : index == 2 ? 1 : 0);
        }
        public static int DockPriority(GameState s)
        {
            if (!Has(s, "dock") || s.tradeShift.level < 2) return -1;
            int index = s.tradeShift.level == 2 ? 0 : 1;
            return (s.tradeShift.deliveredMask & (1 << index)) == 0 ? index : -1;
        }
        public static bool DockPick(GameState s, int index, out string notice)
        {
            notice = "先认货签，再把货送到对应的卸货处。";
            if (!At(s, "dock", "carry") || index < 0 || index >= DockCount(s)) return false;
            TradeShift t = s.tradeShift;
            if (t.held != -1) { notice = "肩上已有货物，先把这一件安放好。"; return false; }
            if ((t.deliveredMask & (1 << index)) != 0) { notice = "这件货已经入栈。"; return false; }
            int priority = DockPriority(s);
            if (priority >= 0 && priority != index) { notice = LifeJourney.DestinationId(s)=="semarang" ? "急雨将至，糖货怕潮；先送带“怕潮”货签的糖包到避雨区。" : "雨气上来了，纸货最怕受潮；先把写着“怕潮”的箱子送入棚。"; return false; }
            t.held = index; notice = "已扛起" + DockLabel(s, index) + "。"; return true;
        }
        public static bool DockDrop(GameState s, int zone, out string notice)
        {
            notice = "先扛起一件货物，再送到相应卸货处。";
            if (!At(s, "dock", "carry") || zone < 0 || zone > 2 || s.tradeShift.held < 0) return false;
            TradeShift t = s.tradeShift;
            if (DockDestination(s, t.held) != zone) { notice = "这处不收这类货。再看货签：" + DockLabel(s, t.held) + "。"; return false; }
            t.deliveredMask |= 1 << t.held; t.held = -1;
            if (t.deliveredMask == (1 << DockCount(s)) - 1) t.stage = "done";
            notice = t.stage == "done" ? "货物已逐件入栈，去找记工的人结这一轮工钱。" : "这件已安放妥当，回去取下一件。";
            return true;
        }

        // A weight unit is half a jin. Coin counts are the game's simplified
        // shop counter tokens, not a claim about a historical exchange rate.
        public static int ShopWeightTarget(GameState s)
        { return Has(s, "shop") ? (WorkEvents.ChoiceId(s) == "portion" ? 2 : WorkEvents.ChoiceId(s) == "cash" ? 3 : s.tradeShift.level == 1 ? 2 : s.tradeShift.level == 2 ? 3 : 5) : 0; }
        public static int ShopPrice(GameState s)
        { return Has(s, "shop") ? (WorkEvents.ChoiceId(s) == "portion" ? 4 : WorkEvents.ChoiceId(s) == "cash" ? 6 : s.tradeShift.level == 1 ? 4 : s.tradeShift.level == 2 ? 6 : 9) : 0; }
        public static int ShopTender(GameState s)
        { return Has(s, "shop") ? (WorkEvents.ChoiceId(s) == "credit" || WorkEvents.ChoiceId(s) == "portion" ? 4 : s.tradeShift.level == 3 ? 6 : 10) : 0; }
        public static int ShopChangeTarget(GameState s) { return Math.Max(0, ShopTender(s) - ShopPrice(s)); }
        public static int ShopDebtTarget(GameState s)
        { return Has(s, "shop") ? Math.Max(0, ShopPrice(s) - ShopTender(s)) : 0; }
        public static bool AdjustWeight(GameState s, int delta)
        {
            if (!At(s, "shop", "weigh") || (delta != 1 && delta != -1 && delta != 2 && delta != -2)) return false;
            int amount = s.tradeShift.weightUnits + delta;
            if (amount < 0 || amount > MaxWeightUnits) return false;
            s.tradeShift.weightUnits = amount; return true;
        }
        public static bool ConfirmWeight(GameState s, out string notice)
        {
            notice = "按客人要的斤两称足，再交到柜台。";
            if (!At(s, "shop", "weigh")) return false;
            if (s.tradeShift.weightUnits != ShopWeightTarget(s)) { notice = s.tradeShift.weightUnits < ShopWeightTarget(s) ? "还没称足，再添一些货。" : "称得多了，取回一些再看秤。"; return false; }
            s.tradeShift.stage = "change"; notice = "斤两已称准。货钱 " + ShopPrice(s) + "，客人交来 " + ShopTender(s) +
                (ShopDebtTarget(s) > 0 ? "；这笔不需找零，欠下的 " + ShopDebtTarget(s) + " 份要另记赊账。" : "；把应找的钱放到找零盘。"); return true;
        }
        public static bool AdjustChange(GameState s, int denomination)
        {
            if (!At(s, "shop", "change") || !Coin(denomination)) return false;
            int amount = s.tradeShift.change + denomination;
            if (amount < 0 || amount > MaxChange) return false;
            s.tradeShift.change = amount; return true;
        }
        public static bool ConfirmChange(GameState s, out string notice)
        {
            notice = "收来的钱减去货钱，才是该找给客人的数。";
            if (!At(s, "shop", "change")) return false;
            if (s.tradeShift.change != ShopChangeTarget(s)) { notice = s.tradeShift.change < ShopChangeTarget(s) ? "找零盘里的钱还少了。" : "找零盘里的钱多了，收回多放的部分。"; return false; }
            s.tradeShift.stage = ShopDebtTarget(s) > 0 ? "credit" : "done";
            notice = s.tradeShift.stage == "credit" ? "货钱 " + ShopPrice(s) + " 份，实收 " + ShopTender(s) + " 份，不需找零。尚欠的 " + ShopDebtTarget(s) + " 份请记入赊欠栏，不能算作已收到的钱。" : "货和找零都交清了，去找掌柜结这一轮工钱。"; return true;
        }
        public static bool AdjustDebt(GameState s, int delta)
        {
            if (!At(s, "shop", "credit") || (delta != 1 && delta != -1 && delta != 5 && delta != -5)) return false;
            int amount = s.tradeShift.debt + delta;
            if (amount < 0 || amount > MaxDebt) return false;
            s.tradeShift.debt = amount; return true;
        }
        public static bool ConfirmDebt(GameState s, out string notice)
        {
            notice = "这笔 " + ShopDebtTarget(s) + " 份赊欠还未收现钱，记在赊欠栏。";
            if (!At(s, "shop", "credit") || s.tradeShift.debt != ShopDebtTarget(s)) return false;
            s.tradeShift.stage = "done"; notice = "现钱与赊欠分开记清了，去找掌柜交账领工钱。"; return true;
        }

        public static int CourierRecipient(GameState s)
        { return Has(s, "courier") ? (WorkEvents.ChoiceId(s) == "trace" ? 0 : s.tradeShift.level == 2 ? 1 : 0) : -1; }
        public static string CourierAddress(GameState s)
        { return !Has(s, "courier") ? "" : CourierRecipient(s) == 0 ? "南街三号" : "北巷七号"; }
        public static bool CourierRead(GameState s, out string notice)
        {
            notice = ""; if (!At(s, "courier", "read")) return false;
            s.tradeShift.stage = "ask"; notice = "批封记着收件处：“" + CourierAddress(s) + "”。先向熟悉这条街的街坊问清门牌。"; return true;
        }
        public static bool CourierAsk(GameState s, out string notice)
        {
            notice = "先看清批封上的姓名和门牌。"; if (!At(s, "courier", "ask")) return false;
            s.tradeShift.stage = "verify";
            notice = WorkEvents.ChoiceId(s) == "trace" ? "街坊核实：批封的北巷七号是旧址，陈阿福已迁到南街三号。把迁居线索记下，再到南街三号与本人核清姓名、旧址和新住处。"
                : "这条街有两位同名的人，不能只凭姓名交批。拿批封的门牌与住处核一遍：" + CourierAddress(s) + "。"; return true;
        }
        public static bool CourierCheck(GameState s, int recipient, out string notice)
        {
            notice = "先向街坊问清门牌，再核收件人。";
            if (!At(s, "courier", "verify") || recipient < 0 || recipient > 1) return false;
            if (recipient != CourierRecipient(s)) { notice = WorkEvents.ChoiceId(s) == "trace" ? "这一位不是街坊确认迁居的陈阿福。批仍在你手里，到南街三号核本人，不凭同名交批。" : "姓名虽然相同，住处却与批封不合。批还在你手里，去找门牌相符的那一位。"; return false; }
            s.tradeShift.recipient = recipient; s.tradeShift.stage = "deliver";
            notice = WorkEvents.ChoiceId(s) == "trace" ? "陈阿福本人核清了姓名、北巷七号旧址与南街三号新住处。迁居记录已对上，可以亲手交批，再取他的收讫回执。" : "本人确认住处与批封相符。可以把批交给他，再等他写收讫回执。"; return true;
        }
        public static bool CourierDeliver(GameState s, int recipient, out string notice)
        {
            notice = "只能把批交给刚刚核实过的收件人。";
            if (!At(s, "courier", "deliver") || recipient != s.tradeShift.recipient) return false;
            s.tradeShift.stage = "receipt"; notice = "侨批已亲手交到本人手中。等他签好回执，带回信局留底。"; return true;
        }
        public static bool CourierReceipt(GameState s, int recipient, out string notice)
        {
            notice = "先完成交批，再从收件人手里取回执。";
            if (!At(s, "courier", "receipt") || recipient != s.tradeShift.recipient) return false;
            s.tradeShift.stage = "done"; notice = "收讫回执已收妥。回信局交回执，结这一轮工钱。"; return true;
        }
        public static bool CourierReturn(GameState s, out string notice)
        {
            notice = "带着原封侨批回信局柜台，登记未能交付的情况。";
            if (!At(s, "courier", "return") || !WorkEvents.Returning(s)) return false;
            s.tradeShift.stage = "done"; notice = "原封退批已经登记，批还未交给收件人。到柜台按退批这一轮的工钱结账。"; return true;
        }

        public static bool Validate(GameState s)
        {
            if (s == null) return false;
            TradeShift t = s.tradeShift;
            if (Empty(t)) return true;
            if (!WorkEvents.ValidateIncident(s)) return false;
            if (t == null || t.version != CurrentVersion || !Context(s, t.route) || t.level < 1 || t.level > 3 ||
                t.level != NanyangLife.CareerLevel(s, t.route) || NanyangLife.WorkCount(s, t.route) >= 3 ||
                s.journey.turns < 0 || s.journey.turns >= LifeJourney.MaxTurns) return false;
            if (t.route == "dock") {
                int all = (1 << DockCount(s)) - 1;
                if (t.weightUnits != 0 || t.change != 0 || t.debt != 0 || t.recipient != -1 || t.deliveredMask < 0 ||
                    (t.deliveredMask & ~all) != 0 || t.held < -1 || t.held >= DockCount(s) ||
                    (t.held >= 0 && (t.deliveredMask & (1 << t.held)) != 0)) return false;
                int priority = DockPriority(s);
                if (priority >= 0 && (t.deliveredMask != 0 || (t.held >= 0 && t.held != priority))) return false;
                return t.stage == "carry" ? t.deliveredMask != all : t.stage == "done" && t.deliveredMask == all && t.held == -1;
            }
            if (t.held != -1 || t.deliveredMask != 0) return false;
            if (t.route == "shop") {
                if (t.recipient != -1 || t.weightUnits < 0 || t.weightUnits > MaxWeightUnits || t.change < 0 || t.change > MaxChange || t.debt < 0 || t.debt > MaxDebt) return false;
                if (t.stage == "weigh") return t.change == 0 && t.debt == 0;
                if (t.weightUnits != ShopWeightTarget(s)) return false;
                if (t.stage == "change") return t.debt == 0;
                if (t.change != ShopChangeTarget(s)) return false;
                if (t.stage == "credit") return ShopDebtTarget(s) > 0;
                return t.stage == "done" && t.debt == ShopDebtTarget(s);
            }
            if (t.weightUnits != 0 || t.change != 0 || t.debt != 0) return false;
            if (WorkEvents.Returning(s)) return (t.stage == "return" || t.stage == "done") && t.recipient == -1;
            if (t.stage == "read" || t.stage == "ask" || t.stage == "verify") return t.recipient == -1;
            return (t.stage == "deliver" || t.stage == "receipt" || t.stage == "done") && t.recipient == CourierRecipient(s);
        }

        static bool At(GameState s, string route, string stage)
        { return IsActive(s) && Validate(s) && !WorkEvents.Pending(s) && s.tradeShift.route == route && s.tradeShift.stage == stage; }
        static bool Has(GameState s, string route)
        { return s != null && s.tradeShift != null && s.tradeShift.version == CurrentVersion && s.tradeShift.route == route && s.tradeShift.level >= 1 && s.tradeShift.level <= 3; }
        static bool Context(GameState s, string route)
        { return Route(route) && NanyangLife.IsEnrolled(s) && LifeJourney.IsActive(s) && !NanyangLife.IsBlocking(s) && s.journey.pendingJob == route; }
        static bool Route(string route) { return route == "dock" || route == "shop" || route == "courier"; }
        static bool Coin(int value) { return value == 1 || value == -1 || value == 2 || value == -2 || value == 5 || value == -5; }
        static bool Empty(TradeShift t)
        { return t == null || (t.version == 0 && string.IsNullOrEmpty(t.route) && string.IsNullOrEmpty(t.stage) && t.level == 0 &&
            (t.held == -1 || t.held == 0) && t.deliveredMask == 0 && t.weightUnits == 0 && t.change == 0 && t.debt == 0 && (t.recipient == -1 || t.recipient == 0) && WorkEvents.EmptyIncident(t.incident)); }
    }
}
