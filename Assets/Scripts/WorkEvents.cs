using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class WorkShiftEvent
    {
        public int version;
        public string choice = "";
    }
    [Serializable]
    public sealed class WorkEventRecord
    {
        public string route = "", choice = "";
        public int level, turn;
        public bool returned;
    }
    [Serializable]
    public sealed class WorkMemory
    {
        public int version;
        public List<WorkEventRecord> records = new List<WorkEventRecord>();
    }
    public sealed class WorkEventChoice
    {
        public string id, title, description;
        public int money, health, trust, relation;
    }

    // Optional, versioned incidents for newly accepted experienced work. Choices
    // change the task, but all rewards and relationship evidence wait for payroll.
    public static class WorkEvents
    {
        public const int CurrentVersion = 1;

        public static bool HasIncident(GameState s)
        { return TradeWork.IsActive(s) && s.tradeShift.level >= 2 && s.tradeShift.incident != null && s.tradeShift.incident.version == CurrentVersion; }
        public static bool Pending(GameState s)
        { return HasIncident(s) && string.IsNullOrEmpty(s.tradeShift.incident.choice) &&
            (s.tradeShift.route == "courier" ? s.tradeShift.stage == "ask" : s.tradeShift.route == "dock" ? s.tradeShift.stage == "carry" : s.tradeShift.stage == "weigh"); }
        public static bool Enroll(GameState s)
        {
            if (!TradeWork.IsActive(s) || !TradeWork.Validate(s) || !Validate(s) || s.tradeShift.level < 2 || !EmptyIncident(s.tradeShift.incident)) return false;
            TradeShift t = s.tradeShift;
            if (!Pristine(t) || t.stage != (t.route == "dock" ? "carry" : t.route == "shop" ? "weigh" : "read")) return false;
            t.incident = new WorkShiftEvent { version = CurrentVersion }; return true;
        }
        public static bool Choose(GameState s, string id, out string notice)
        {
            notice = "";
            if (!Pending(s) || !TradeWork.Validate(s) || !Validate(s)) return false;
            WorkEventChoice choice = Option(s.tradeShift.route, s.tradeShift.level, id);
            if (choice == null) return false;
            s.tradeShift.incident.choice = id;
            if (s.tradeShift.route == "courier" && id == "return") s.tradeShift.stage = "return";
            notice = choice.description + " 完成这一轮并结账后，才会记下工钱与彼此的交情。";
            return true;
        }
        public static List<WorkEventChoice> Choices(GameState s)
        { return HasIncident(s) ? Options(s.tradeShift.route, s.tradeShift.level) : new List<WorkEventChoice>(); }
        public static string Title(GameState s)
        {
            if (!HasIncident(s)) return "";
            if (LifeJourney.DestinationId(s)=="semarang" && s.tradeShift.route=="dock") return s.tradeShift.level==2?"急雨前的糖货":"糖栈新来的同工";
            if (s.tradeShift.route == "dock") return s.tradeShift.level == 2 ? "雨前护纸" : "新来的同工";
            if (s.tradeShift.route == "shop") return s.tradeShift.level == 2 ? "熟客的钱不够" : "独立管账";
            return s.tradeShift.level == 2 ? "收件人搬家了" : "邻人愿意代收";
        }
        public static string Body(GameState s)
        {
            if (!HasIncident(s)) return "";
            if (LifeJourney.DestinationId(s)=="semarang" && s.tradeShift.route=="dock") return s.tradeShift.level==2
                ? "急雨逼近，带怕潮货签的糖包还在装卸处。许生愿意接走布匹箱，这一轮分给他两份工钱。你可以自己抢运，也可以先说清分工；完成交接后再结账。"
                : "新来的工友把糖袋和布箱的签记弄混了。你可以教他认清收货区，请他接走布箱；也可以独自完成三件货的交接。对人的照应会留在往后的交情里。";
            if (s.tradeShift.route == "dock") return s.tradeShift.level == 2
                ? "雨气逼近，纸货还在埠头。工友愿意替你接走布匹箱，只是这一轮要分给他两份工钱。你可以自己抢运，也可以先说好怎样分工。"
                : "新来的同工还认不全货签。你可以花些时间教他分货，请他接走布匹箱；也可以把三件都接在自己肩上。";
            if (s.tradeShift.route == "shop") return s.tradeShift.level == 2
                ? "熟客想买一斤半货，共六份货钱，手里却只有四份。掌柜准你自己商量：记下两份赊欠，或按四份现钱改称一斤。"
                : "这回由你独立管账。街坊要两斤半货，货钱九份，现有六份；你可以把三份赊欠记清，也可以与他商量改买一斤半、六份当场结清。";
            return s.tradeShift.level == 2
                ? "批封仍写着北巷七号，街坊却说陈阿福搬到了南街三号。是跟着线索重新核本人，还是把未能确认送达的批原封退回信局登记？"
                : "邻人说可以替陈阿福收下这封批，但你还没有得到本人的收讫。是坚持找到南街三号的本人，还是原封退回信局，把未交付的情况记清？";
        }
        public static int RelationshipScore(GameState s, string route)
        {
            int score = 0;
            if (s == null || s.workMemory == null || s.workMemory.version != CurrentVersion || s.workMemory.records == null) return score;
            foreach (WorkEventRecord record in s.workMemory.records) {
                if (record == null || record.route != route) continue;
                WorkEventChoice choice = Option(record.route, record.level, record.choice);
                if (choice != null) score += choice.relation;
            }
            return score;
        }
        public static string RelationshipLabel(GameState s, string route)
        {
            int score = RelationshipScore(s, route);
            if (score >= 4) return route == "dock" ? "工友愿意互相照应" : route == "shop" ? "街坊放心托付账目" : "街坊信得过你的交批";
            if (score >= 2) return route == "dock" ? "一起分过肩上的担子" : route == "shop" ? "街坊记得你的周全" : "街坊记得你的认真";
            if (score > 0) return "多了一份熟悉";
            return LastRecord(s, route) == null ? "尚无共同办事的经历" : "各自记得这趟来往";
        }
        public static WorkEventRecord LastRecord(GameState s, string route)
        {
            if (s == null || s.workMemory == null || s.workMemory.version != CurrentVersion || s.workMemory.records == null) return null;
            for (int i = s.workMemory.records.Count - 1; i >= 0; i--) {
                WorkEventRecord record = s.workMemory.records[i];
                if (record != null && record.route == route) return record;
            }
            return null;
        }
        public static string ChoiceId(GameState s)
        { return HasIncident(s) ? s.tradeShift.incident.choice ?? "" : ""; }
        public static bool Returning(GameState s)
        { return HasIncident(s) && s.tradeShift.route == "courier" && ChoiceId(s) == "return"; }

        public static bool Validate(GameState s)
        {
            if (s == null || !ValidateIncident(s)) return false;
            WorkMemory memory = s.workMemory;
            if (memory == null || (memory.version == 0 && (memory.records == null || memory.records.Count == 0))) return true;
            if (memory.version != CurrentVersion || memory.records == null || memory.records.Count > LifeJourney.MaxTurns ||
                s.journeyVersion != 1 || s.journey == null || s.journey.log == null || !NanyangLife.IsEnrolled(s)) return false;
            int previous = 0;
            foreach (WorkEventRecord record in memory.records) {
                if (record == null || record.turn <= previous || record.turn > s.journey.turns || record.turn > LifeJourney.MaxTurns ||
                    Option(record.route, record.level, record.choice) == null || record.returned != (record.route == "courier" && record.choice == "return")) return false;
                bool found = false; int previousJobs = 0;
                foreach (JourneyLog log in s.journey.log) {
                    if (log == null || log.actionId != record.route) continue;
                    if (log.turn < record.turn) previousJobs++;
                    if (log.turn == record.turn) { if (found) return false; found = true; }
                }
                if (!found || Math.Min(3, previousJobs + 1) != record.level) return false;
                previous = record.turn;
            }
            return true;
        }
        internal static bool ValidateIncident(GameState s)
        {
            TradeShift t = s == null ? null : s.tradeShift;
            if (t == null || EmptyIncident(t.incident)) return true;
            if (t.version != TradeWork.CurrentVersion || t.level < 2 || t.level > 3 || t.incident.version != CurrentVersion || !TradeWork.IsActive(s)) return false;
            if (string.IsNullOrEmpty(t.incident.choice))
                return Pristine(t) && (t.route == "courier" ? t.stage == "read" || t.stage == "ask" : t.route == "dock" ? t.stage == "carry" : t.stage == "weigh");
            if (Option(t.route, t.level, t.incident.choice) == null) return false;
            if (t.route == "courier") {
                if (t.incident.choice == "return") return (t.stage == "return" || t.stage == "done") && t.recipient == -1;
                return t.stage == "ask" || t.stage == "verify" || t.stage == "deliver" || t.stage == "receipt" || t.stage == "done";
            }
            return true;
        }
        internal static bool EmptyIncident(WorkShiftEvent incident)
        { return incident == null || (incident.version == 0 && string.IsNullOrEmpty(incident.choice)); }
        internal static bool CanSettle(GameState s)
        {
            if (!HasIncident(s)) return true;
            return Validate(s) && TradeWork.IsComplete(s) && Option(s.tradeShift.route, s.tradeShift.level, ChoiceId(s)) != null &&
                (s.workMemory == null || s.workMemory.records == null || s.workMemory.records.Count < LifeJourney.MaxTurns);
        }
        internal static WorkEventChoice SettlementEffect(GameState s)
        { return HasIncident(s) ? Option(s.tradeShift.route, s.tradeShift.level, ChoiceId(s)) : null; }
        // CommitAction calls only after every gate passes. This records evidence
        // immediately before its uninterrupted existing resource/log settlement.
        internal static string Settle(GameState s)
        {
            if (!HasIncident(s)) return "";
            TradeShift t = s.tradeShift;
            WorkEventChoice choice = Option(t.route, t.level, t.incident.choice);
            if (s.workMemory == null || s.workMemory.version == 0) s.workMemory = new WorkMemory { version = CurrentVersion };
            s.workMemory.records.Add(new WorkEventRecord { route = t.route, level = t.level, choice = choice.id, turn = s.journey.turns + 1, returned = Returning(s) });
            string result = Returning(s) ? "这封批未交付，已原封退局登记；没有收件人回执，也不记作成功交批。" : "这一趟你选择了“" + choice.title + "”，实际分工与交接已记下。";
            if (choice.relation > 0) result += " 一起办过这件事的人记住了你的做法。";
            return result;
        }
        static bool Pristine(TradeShift t)
        { return t.held == -1 && t.deliveredMask == 0 && t.weightUnits == 0 && t.change == 0 && t.debt == 0 && t.recipient == -1; }
        static WorkEventChoice Option(string route, int level, string id)
        { return Options(route, level).Find(c => c.id == id); }
        static List<WorkEventChoice> Options(string route, int level)
        {
            var options = new List<WorkEventChoice>();
            if (route == "dock" && level == 2) {
                options.Add(C("cover", "自己先把纸货抢入棚", "先运怕潮纸货，再把米粮和布匹都送入栈；这一轮自己搬三件。", 0, -2, 1, 0));
                options.Add(C("pair", "与工友分工，分他两份工钱", "你先送纸货，再送米粮；布匹交给工友，自己搬两件。", -2, 2, 0, 2));
            } else if (route == "dock" && level == 3) {
                options.Add(C("teach", "教新同工认货签", "你把纸货与米粮安放好，新同工按你教的货签接走布匹；自己搬两件。", -1, 1, 2, 2));
                options.Add(C("solo", "自己接完三件货", "这轮三件都由你来搬，工钱多两份，也更费肩膀。", 2, -2, 0, 0));
            } else if (route == "shop" && level == 2) {
                options.Add(C("credit", "照原量称货，记两份赊欠", "称一斤半，货钱六份、实收四份；把欠下的两份记清。", 0, 0, 1, 1));
                options.Add(C("portion", "商量改称一斤，当场结清", "称一斤，货钱四份、实收四份；这笔不留赊欠。", 0, 0, 0, 0));
            } else if (route == "shop" && level == 3) {
                options.Add(C("record", "把现钱和三份赊欠分开记清", "称两斤半，货钱九份、实收六份；另记三份赊欠。", 1, 0, 2, 2));
                options.Add(C("cash", "改称一斤半，六份现钱结清", "称一斤半，货钱六份、实收六份；不用补记赊账。", 0, 0, 0, 0));
            } else if (route == "courier" && level == 2) {
                options.Add(C("trace", "问清新住处，再核本人", "把迁居线索记下，去南街三号重新核收件人；亲手交批并取回执。", 0, -2, 2, 2));
                options.Add(C("return", "原封退局，登记尚未交付", "带批回信局柜台登记，少领六份工钱，免得把不确定的去向写成已送达。", -6, 3, 0, 0));
            } else if (route == "courier" && level == 3) {
                options.Add(C("personal", "坚持找到本人，取得收讫", "去南街三号当面核清，再交批、收回执，不请邻人代签。", 0, -1, 2, 2));
                options.Add(C("return", "原封退局，说明未能亲交", "带批回信局柜台登记，少领七份工钱；把未交付的情况如实留下。", -7, 3, 0, 0));
            }
            return options;
        }
        static WorkEventChoice C(string id, string title, string description, int money, int health, int trust, int relation)
        { return new WorkEventChoice { id = id, title = title, description = description, money = money, health = health, trust = trust, relation = relation }; }
    }
}
