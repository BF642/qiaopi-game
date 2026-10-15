using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class FirstDayState
    {
        public string route = "", stage = "", choice = "", outcome = "";
        public string eveningChoice = "", morningFeedback = "";
        public int personalLettersAtStart;
        public bool workSettled, letterSent, letterDeferred, replyCollected, bonusUsed;
        public string sentToken = "", recipient = "", intent = "";
        public int amount;
        public LetterRecord pendingReply;
        public int bonusMoney, bonusTrust;
    }

    public sealed class FirstDayBonus
    {
        public int money, trust;
        public string notice = "";
        public bool available;
    }

    // A small scene-independent chapter layered over the existing first-job choice.
    // StoryEngine alone owns that job's payment, flags and decision/history count.
    // Letters and the later work bonus are separate, persisted one-shot events.
    public static class FirstDayChapter
    {
        public const int CurrentVersion = 1;
        public const string WorkDate = "1906年秋";
        public const string ReplyDate = "1906年秋 · 寄出数周后（叙事时间）";

        sealed class Memory
        {
            public string route, choice, flag, text;
            public int money, trust;
            public Memory(string r, string c, string f, string t, int m, int tr)
            { route = r; choice = c; flag = f; text = t; money = m; trust = tr; }
        }
        static readonly Memory[] Memories = {
            new Memory("dock", "extra", "earned_dock", "次日一早，肩头仍酸。工头记得你昨日接过急货，问你还愿不愿来；你先揉了揉肩，才答应量力接工。", 2, 0),
            new Memory("dock", "help", "crew_help", "次日一早，伤脚的老搬工给你留了靠墙的位置。他把昨日那趟相帮记在心里，愿意带你认下一处货栈。", 0, 2),
            new Memory("dock", "tally", "marked_cargo", "次日一早，货栈的人拿着你留的副单来问货数。这次他先听你核清，再招呼旁人搬货。", 0, 2),
            new Memory("shop", "learn", "shop_partner", "次日开铺，掌柜让你先试着排一回货。昨日记下的进货顺序派上了用场，你开始能独自接住一小笔生意。", 2, 0),
            new Memory("shop", "wages", "cash_worker", "次日开铺，你仍有些困。掌柜记得你昨夜守到最后，留下一班可另领工钱的活；接不接，要连同饭和觉一起算。", 2, 0),
            new Memory("shop", "read", "read_letters", "次日，那位请你念信的同乡来买米，先叫了你的名字。他说，下回有认不清的字，还想请你坐下来慢慢念。", 0, 2),
            new Memory("courier", "verify", "verified", "次日，何师傅翻到你补齐的门牌和经手日期，让你一起复看下一封批。昨日多问的一句，今天少了一处猜测。", 0, 2),
            new Memory("courier", "rush", "hurried", "次日，两张未核完的底单仍在桌角。伙计记得你认路快，愿把一趟熟路交给你；何师傅提醒你，领件前先把昨日的数目核清。", 2, 0),
            new Memory("courier", "craft", "clerk_skill", "次日，何师傅把一张新批封推到你面前。你下笔仍慢，却能自己看出姓名和款额之间该留哪一栏。", 0, 2)
        };

        public static bool IsActive(GameState s)
        { return s != null && s.firstDayVersion == CurrentVersion && s.firstDay != null && KnownStage(s.firstDay.stage) && s.firstDay.stage != "complete"; }

        public static bool TryBegin(GameState s)
        {
            if (s == null || s.firstDayVersion != 0 || s.awaitingContinue || !Route(s.nodeId) || !StoryEngine.Validate(s)) return false;
            s.firstDay = new FirstDayState { route = s.nodeId, stage = "work", personalLettersAtStart = s.personalLettersSent };
            s.firstDayVersion = CurrentVersion;
            return true;
        }

        public static bool TriggerEncounter(GameState s, int progress)
        {
            if (!At(s, "work") || s.awaitingContinue || !string.IsNullOrEmpty(s.firstDay.choice) || progress < 1) return false;
            s.firstDay.stage = "encounter";
            return true;
        }

        public static List<ChoiceData> WorkChoices(GameState s)
        {
            if (s == null || s.firstDayVersion != CurrentVersion || s.firstDay == null || s.nodeId != s.firstDay.route ||
                (s.firstDay.stage != "work" && s.firstDay.stage != "encounter")) return new List<ChoiceData>();
            return StoryEngine.GetScene(s).choices;
        }

        public static bool ChooseWork(GameState s, string id)
        {
            if (!At(s, "encounter") || s.awaitingContinue || string.IsNullOrEmpty(id)) return false;
            ChoiceData chosen = WorkChoices(s).Find(c => c.id == id && c.enabled);
            if (chosen == null || Find(s.firstDay.route, id) == null) return false;
            s.firstDay.choice = id;
            s.firstDay.stage = "work";
            return true;
        }

        public static bool SettleWork(GameState s)
        {
            if (!At(s, "work") || string.IsNullOrEmpty(s.firstDay.choice)) return false;
            FirstDayState day = s.firstDay;
            if (!s.awaitingContinue) {
                if (day.workSettled || s.decisions >= 17) return false;
                Memory memory = Find(day.route, day.choice);
                if (s.flags.Count >= 80 && !s.flags.Contains(memory.flag)) return false;
                // Availability can change after the encounter. Reject before any
                // write, instead of retaining the old UI's enabled value.
                if (!WorkChoices(s).Exists(c => c.id == day.choice && c.enabled)) return false;
            }
            var before = new WorkSettlementSnapshot(s);
            // A save taken between Choose and Continue can resume here without
            // running the original reward action a second time.
            if (!s.awaitingContinue && !StoryEngine.Choose(s, day.choice)) { before.Restore(s); return false; }
            if (s.nextNodeId != "first_pay") { before.Restore(s); return false; }
            day.outcome = s.lastOutcome;
            day.workSettled = true;
            // Story's original action does not cap money at its save maximum.
            // Clamp before Continue validates the resulting transaction.
            s.money = Math.Min(9999, s.money);
            if (!StoryEngine.Continue(s)) { before.Restore(s); return false; }
            day.stage = "evening";
            return true;
        }

        // The nine existing first-job actions change only resources, flags and
        // history. Preserve list identity while rolling back a failed continuation.
        sealed class WorkSettlementSnapshot
        {
            readonly int money, health, family, trust, decisions;
            readonly bool awaiting, settled;
            readonly string node, next, lastOutcome, lastChanges, stage, outcome;
            readonly List<string> flags;
            readonly List<HistoryRecord> history;
            public WorkSettlementSnapshot(GameState s)
            {
                money = s.money; health = s.health; family = s.family; trust = s.trust; decisions = s.decisions;
                awaiting = s.awaitingContinue; settled = s.firstDay.workSettled;
                node = s.nodeId; next = s.nextNodeId; lastOutcome = s.lastOutcome; lastChanges = s.lastChanges;
                stage = s.firstDay.stage; outcome = s.firstDay.outcome;
                flags = new List<string>(s.flags); history = new List<HistoryRecord>(s.history);
            }
            public void Restore(GameState s)
            {
                s.money = money; s.health = health; s.family = family; s.trust = trust; s.decisions = decisions;
                s.awaitingContinue = awaiting; s.nodeId = node; s.nextNodeId = next;
                s.lastOutcome = lastOutcome; s.lastChanges = lastChanges;
                s.firstDay.workSettled = settled; s.firstDay.stage = stage; s.firstDay.outcome = outcome;
                s.flags.Clear(); s.flags.AddRange(flags); s.history.Clear(); s.history.AddRange(history);
            }
        }

        public static bool TryEvening(GameState s, string id, out string notice)
        {
            notice = "";
            if (!At(s, "evening") || s.awaitingContinue) return false;
            int cost, health, trust = 0;
            if (id == "meal") { cost = 2; health = 8; notice = "花两份盘缠吃了一顿热饭，体力 +8。灯下还有时间写几句家常。"; }
            else if (id == "together") { cost = 3; health = 6; trust = 1; notice = "花三份盘缠与同乡围坐吃饭，体力 +6、信用 +1。你记住了能互相照应的名字。"; }
            else if (id == "sleep") { cost = 0; health = 4; notice = "你留着盘缠，先喝水歇肩，体力 +4。草稿可以慢慢写，稍后到床边睡下。"; }
            else return false;
            if (s.money < cost) { notice = "盘缠不够这一餐，可以先喝水歇肩。"; return false; }
            s.money -= cost; s.health = Clamp(s.health + health); s.trust = Clamp(s.trust + trust);
            s.firstDay.eveningChoice = id; s.firstDay.stage = "letter";
            return true;
        }

        public static bool DeferLetter(GameState s)
        {
            if (!At(s, "letter") || s.awaitingContinue) return false;
            s.firstDay.letterDeferred = true;
            s.firstDay.stage = "bed";
            return true; // Never replace, clear, charge for, or tokenize the draft.
        }

        // Call after PersonalLetters has stored the outgoing record and its token,
        // and incremented personalLettersSent. Do not add reply to letters first.
        public static bool RecordSentLetter(GameState s, PersonalLetterDraft sent, LetterRecord reply)
        {
            if (s == null || s.firstDayVersion != CurrentVersion || s.firstDay == null || s.firstDay.stage != "letter" ||
                s.nodeId != "first_pay" || s.awaitingContinue || !Validate(s) || sent == null || !Token(sent.token) ||
                !Recipient(sent.recipient) || !Intent(sent.intent) || Array.IndexOf(PersonalLetters.Amounts, sent.amount) < 0 ||
                string.IsNullOrWhiteSpace(sent.body) || sent.body.Trim().Length < 2 || sent.body.Length > PersonalLetters.MaxBodyCharacters ||
                !ValidReply(reply, false) || s.personalLetterTokens == null || !s.personalLetterTokens.Contains(sent.token) ||
                s.personalLettersSent <= s.firstDay.personalLettersAtStart || s.personalLettersSent > PersonalLetters.MaxLetters ||
                s.letters == null || s.letters.Count >= 40) return false;
            bool outgoing = s.letters.Exists(l => l != null && !l.incoming && l.body == sent.body && l.amount == sent.amount);
            if (!outgoing) return false;
            FirstDayState day = s.firstDay;
            day.sentToken = sent.token; day.recipient = sent.recipient; day.intent = sent.intent; day.amount = sent.amount;
            day.pendingReply = CopyReply(reply); day.pendingReply.date = ReplyDate;
            day.letterSent = true; day.stage = "bed";
            return true;
        }

        public static bool Sleep(GameState s)
        {
            if (!At(s, "bed") || s.awaitingContinue) return false;
            s.firstDay.stage = "morning";
            return true;
        }

        // UI can display the next-day reaction before the player acknowledges
        // it. Reading this method never advances time, unlocks or consumes a bonus.
        public static string MorningPreview(GameState s)
        {
            if (!At(s, "morning") || s.awaitingContinue) return "";
            Memory memory = Find(s.firstDay.route, s.firstDay.choice);
            return memory.text + "\n" + BonusNotice(s.firstDay.route, memory.money, memory.trust);
        }

        // Only the disclosed first-day letter uses this. Later work letters have
        // their own settled experience and should not repeat the first day's facts.
        public static string LetterMemory(GameState s, string recipient)
        {
            if(s==null||s.firstDayVersion!=CurrentVersion||s.firstDay==null||!s.firstDay.workSettled||
                (s.journeyVersion==1&&s.journey!=null&&s.journey.turns>0))return "";
            var d=s.firstDay;
            string fact="";
            if(d.route=="dock")fact=d.choice=="extra"?"头一天你接下急货，肩头酸也做到了收工。":d.choice=="help"?"头一天你放慢自己的活，帮了那位伤脚的搬工。":d.choice=="tally"?"头一天你把货数写在副单上，留住了可核对的一笔。":"";
            else if(d.route=="shop")fact=d.choice=="learn"?"头一天你留心看铺里进货，想慢慢学会独自打理。":d.choice=="wages"?"头一天你守铺到晚些，多挣了工钱，睡觉却也晚了。":d.choice=="read"?"头一天你坐下来，给认不清字的同乡念了家信。":"";
            else if(d.route=="courier")fact=d.choice=="verify"?"头一天你把门牌和经手日期重新核清，没有只凭一个姓名交批。":d.choice=="rush"?"头一天你为了多跑一趟，留下两张尚未核完的底单。":d.choice=="craft"?"头一天你慢慢学着写批封，姓名和款额都要落在各自的位置。":"";
            if(fact=="")return "";
            return fact+(recipient=="wife"?"你肯把这一天细说给我，我便知道海那边的你怎样过日子；以后的打算，我们隔着海也能商量。"
                :recipient=="sister"?"我在灯下读这几句，仿佛也跟着你走了一趟。下回信里，愿意再说一点给我听吗？"
                :"母亲读到这里，停了一会儿。头一天能安顿下来就好，往后不必样样逞强。");
        }

        public static string AdvanceMorning(GameState s)
        {
            if (!At(s, "morning") || s.awaitingContinue) return "";
            FirstDayState day = s.firstDay; Memory memory = Find(day.route, day.choice);
            string preview = MorningPreview(s);
            day.bonusMoney = memory.money; day.bonusTrust = memory.trust;
            day.morningFeedback = preview;
            day.morningFeedback += day.letterSent
                ? "\n昨夜寄往泉州的批还在路上。日子照常过，数周后才有这一封的回音。"
                : "\n草稿还在，你可以等想清楚，再去写批桌前落笔。";
            day.stage = day.letterSent ? "reply" : "complete";
            return day.morningFeedback;
        }

        public static bool CollectReply(GameState s)
        {
            if (!At(s, "reply") || s.awaitingContinue || s.letters == null || s.letters.Count >= 40) return false;
            FirstDayState day = s.firstDay;
            if (!ValidReply(day.pendingReply, true) || day.replyCollected) return false;
            // The persisted stage/token owns idempotency. Identical wording in
            // an earlier, separate correspondence is not this receipt.
            s.letters.Add(CopyReply(day.pendingReply));
            day.replyCollected = true; day.stage = "complete";
            return true;
        }

        public static FirstDayBonus BonusForWork(GameState s, string route)
        {
            var result = new FirstDayBonus();
            if (s == null || !Validate(s) || s.firstDayVersion != CurrentVersion || s.firstDay == null) return result;
            FirstDayState day = s.firstDay;
            if (day.stage != "complete" || day.bonusUsed || route != day.route || !Route(route)) return result;
            result.money = day.bonusMoney; result.trust = day.bonusTrust;
            result.available = result.money + result.trust > 0;
            if (result.available) result.notice = BonusNotice(route, result.money, result.trust);
            return result;
        }

        // LifeJourney calls this only while committing a successful physical job.
        // It adds no story decisions, journey turns, or extra JourneyLog entries.
        public static string ApplyWorkBonus(GameState s, string route)
        {
            FirstDayBonus bonus = BonusForWork(s, route);
            if (!bonus.available || s.nodeId != "first_pay" || s.awaitingContinue) return "";
            s.money = Math.Min(9999, s.money + bonus.money); s.trust = Clamp(s.trust + bonus.trust);
            s.firstDay.bonusUsed = true;
            return "首日留下的照应派上用场：" + (bonus.money > 0 ? "这次工钱多了两份。" : "这次同工的人更信得过你，信用 +2。");
        }

        public static bool Validate(GameState s)
        {
            if (s == null || s.firstDayVersion < 0 || s.firstDayVersion > CurrentVersion) return false;
            FirstDayState d = s.firstDay;
            if (s.firstDayVersion == 0) return d == null || Empty(d);
            if (d == null || !Route(d.route) || !KnownStage(d.stage) || d.personalLettersAtStart < 0 ||
                d.personalLettersAtStart > PersonalLetters.MaxLetters || s.personalLettersSent < d.personalLettersAtStart) return false;
            bool early = d.stage == "work" || d.stage == "encounter";
            Memory memory = Find(d.route, d.choice);
            if (early) {
                if (s.nodeId != d.route || (d.stage == "encounter" && (!string.IsNullOrEmpty(d.choice) || s.awaitingContinue))) return false;
                if (!string.IsNullOrEmpty(d.choice) && memory == null) return false;
                if (s.awaitingContinue && (d.stage != "work" || memory == null || s.nextNodeId != "first_pay")) return false;
                if (d.workSettled && (!s.awaitingContinue || string.IsNullOrEmpty(d.outcome))) return false;
                if (!d.workSettled && !string.IsNullOrEmpty(d.outcome)) return false;
                if (!string.IsNullOrEmpty(d.eveningChoice) || d.letterSent || d.letterDeferred) return false;
            } else {
                if (memory == null || !d.workSettled || string.IsNullOrEmpty(d.outcome)) return false;
                if (d.stage != "complete" && (s.nodeId != "first_pay" || s.awaitingContinue)) return false;
                if (d.stage == "evening") { if (!string.IsNullOrEmpty(d.eveningChoice)) return false; }
                else if (!Dinner(d.eveningChoice)) return false;
            }
            if (d.outcome != null && d.outcome.Length > 6000) return false;
            bool afterLetter = d.stage == "bed" || d.stage == "morning" || d.stage == "reply" || d.stage == "complete";
            if (afterLetter ? d.letterSent == d.letterDeferred : d.letterSent || d.letterDeferred) return false;
            if (d.letterSent) {
                // The delayed incoming letter already owns one of the 40 slots.
                if (!d.replyCollected && (s.letters == null || s.letters.Count > 39)) return false;
                if (!Token(d.sentToken) || !Recipient(d.recipient) || !Intent(d.intent) || Array.IndexOf(PersonalLetters.Amounts, d.amount) < 0 ||
                    !ValidReply(d.pendingReply, true) || s.personalLettersSent <= d.personalLettersAtStart ||
                    s.personalLetterTokens == null || !s.personalLetterTokens.Contains(d.sentToken)) return false;
            } else if (!string.IsNullOrEmpty(d.sentToken) || !string.IsNullOrEmpty(d.recipient) || !string.IsNullOrEmpty(d.intent) || d.amount != 0 || !EmptyLetter(d.pendingReply) || d.replyCollected) return false;
            bool morningRead = d.stage == "reply" || d.stage == "complete";
            if (morningRead) {
                if (string.IsNullOrEmpty(d.morningFeedback) || d.morningFeedback.Length > 6000 || d.bonusMoney != memory.money || d.bonusTrust != memory.trust) return false;
            } else if (!string.IsNullOrEmpty(d.morningFeedback) || d.bonusMoney != 0 || d.bonusTrust != 0 || d.bonusUsed) return false;
            if (d.stage == "reply" && (!d.letterSent || d.replyCollected)) return false;
            if (d.replyCollected != (d.stage == "complete" && d.letterSent)) return false;
            if (d.bonusUsed && d.stage != "complete") return false;
            return true;
        }

        static bool At(GameState s, string stage)
        { return s != null && s.firstDayVersion == CurrentVersion && s.firstDay != null && s.firstDay.stage == stage && StoryEngine.Validate(s) && Validate(s); }
        static Memory Find(string route, string choice)
        { foreach (var memory in Memories) if (memory.route == route && memory.choice == choice) return memory; return null; }
        static bool Route(string value) { return value == "dock" || value == "shop" || value == "courier"; }
        static bool Dinner(string value) { return value == "meal" || value == "together" || value == "sleep"; }
        static bool KnownStage(string value)
        { return value == "work" || value == "encounter" || value == "evening" || value == "letter" || value == "bed" || value == "morning" || value == "reply" || value == "complete"; }
        static bool Recipient(string value) { return value == "mother" || value == "wife" || value == "sister"; }
        static bool Intent(string value) { return value == "reassure" || value == "truth" || value == "study"; }
        static int Clamp(int value) { return Math.Max(0, Math.Min(100, value)); }
        static string BonusNotice(string route, int money, int trust)
        { return "下次再做" + (route == "dock" ? "码头的工" : route == "shop" ? "铺里的工" : "信局的工") + "，" + (money > 0 ? "盘缠额外 +2" : "信用额外 +2") + "（只生效一次）。"; }
        static bool Token(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length != 32) return false;
            foreach (char c in token) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
        static bool Empty(FirstDayState d)
        { return string.IsNullOrEmpty(d.route) && string.IsNullOrEmpty(d.stage) && string.IsNullOrEmpty(d.choice) && string.IsNullOrEmpty(d.outcome) &&
            string.IsNullOrEmpty(d.eveningChoice) && string.IsNullOrEmpty(d.morningFeedback) && d.personalLettersAtStart == 0 &&
            !d.workSettled && !d.letterSent && !d.letterDeferred && !d.replyCollected && !d.bonusUsed &&
            string.IsNullOrEmpty(d.sentToken) && string.IsNullOrEmpty(d.recipient) && string.IsNullOrEmpty(d.intent) &&
            d.amount == 0 && EmptyLetter(d.pendingReply) && d.bonusMoney == 0 && d.bonusTrust == 0; }
        static bool EmptyLetter(LetterRecord l)
        { return l == null || (!l.incoming && l.amount == 0 && string.IsNullOrEmpty(l.title) && string.IsNullOrEmpty(l.date) &&
            string.IsNullOrEmpty(l.route) && string.IsNullOrEmpty(l.body) && string.IsNullOrEmpty(l.signature)); }
        static bool ValidReply(LetterRecord l, bool dated)
        { return l != null && l.incoming && l.amount == 0 && Text(l.title, 300) && Text(l.route, 300) && Text(l.body, 8000) &&
            Text(l.signature, 100) && Text(l.date, 200) && (!dated || l.date == ReplyDate); }
        static bool Text(string value, int max) { return !string.IsNullOrEmpty(value) && value.Length <= max; }
        static LetterRecord CopyReply(LetterRecord l)
        { return new LetterRecord { title = l.title, date = l.date, route = l.route, body = l.body, signature = l.signature, incoming = l.incoming, amount = l.amount }; }
        static bool SameLetter(LetterRecord a, LetterRecord b)
        { return a != null && b != null && a.incoming == b.incoming && a.amount == b.amount && a.title == b.title && a.date == b.date &&
            a.route == b.route && a.body == b.body && a.signature == b.signature; }
    }
}
