using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class JourneyState
    {
        public int seed;
        public string destination = "singapore";
        public bool legacy, completed;
        public int turns;
        public string hardshipId = "";
        public bool hardshipResolved;
        public string primaryTrade = "";
        public string pendingJob = "";
        public List<JourneyLog> log = new List<JourneyLog>();
    }

    [Serializable]
    public sealed class JourneyLog
    {
        public string actionId, title, outcome;
        public int turn;
    }

    public sealed class LivelihoodAction
    {
        public string id, title, description, regionId, unavailableReason = "";
        public int money, health, family, trust;
        public bool physical, enabled = true;
    }

    public sealed class JourneyHardship
    {
        public string title, body;
        public List<LivelihoodAction> choices = new List<LivelihoodAction>();
    }

    /// <summary>
    /// A bounded working-life interlude. It owns a separate action log; it never
    /// advances the story node or edits StoryEngine's choice/history counters.
    /// Destination is rolled once and serialized, including across pending saves.
    /// Hardships are fictional composite incidents, not named historical events.
    /// </summary>
    public static class LifeJourney
    {
        public const int MinTurns = 3, MaxTurns = 6;
        private static readonly string[] Destinations = { "singapore", "penang", "rangoon" };

        public static JourneyState Create(int seed)
        {
            return new JourneyState { seed = seed, destination = Destinations[new Random(seed).Next(3)] };
        }

        public static JourneyState Ensure(GameState s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (!Validate(s)) throw new ArgumentException("Invalid journey version or records", "s");
            // Inline null is not a reliable presence marker in Unity JsonUtility.
            // An explicit zero version denotes the old story, even if Unity has
            // silently created a default JourneyState object while serializing it.
            if (s.journeyVersion == 0)
            {
                s.journey = new JourneyState { destination = "singapore", legacy = true, completed = true };
                s.journeyVersion = 1;
            }
            return s.journey;
        }

        public static string DestinationId(GameState s)
        { return s != null && s.journeyVersion == 1 && s.journey != null ? s.journey.destination : "singapore"; }
        public static string DestinationName(GameState s)
        {
            switch (DestinationId(s)) { case "penang": return "槟榔屿"; case "rangoon": return "仰光"; default: return "新加坡"; }
        }
        public static string DestinationIntro(GameState s)
        {
            switch (DestinationId(s))
            {
                case "penang": return "转船的空位把你带到槟榔屿。沿海货栈里有米粮、布匹和香料，街头通向一排排骑楼；同乡替你找到床位，却不能替你决定靠什么活下去。";
                case "rangoon": return "等到的转船把你带到仰光。潮湿的河埠挨着米粮货栈，陌生的话语在船边来往；同乡带你认路，也提醒你把工钱和住处问清再落笔。";
                default: return "候到的船在新加坡靠泊。河边货栈与街市挤满找工的人，岸上没有现成的金子，只有搬货的哨声、店铺的算盘声与催着送达的乡信。";
            }
        }
        public static string Localize(GameState s, string text)
        { return string.IsNullOrEmpty(text) ? text : text.Replace("新加坡", DestinationName(s)); }

        public static bool HasReachedOverseas(GameState s)
        {
            if (s == null) return false;
            switch (s.nodeId)
            { case "peace": case "pressure": case "home": case "funding": case "farewell": case "passage": return false; default: return true; }
        }
        public static bool IsActive(GameState s)
        { return s != null && s.journeyVersion == 1 && s.nodeId == "first_pay" && !s.awaitingContinue && s.journey != null && !s.journey.completed; }
        public static bool CanFinish(GameState s)
        { return IsActive(s) && s.journey.turns >= MinTurns && PendingHardship(s) == null; }
        public static bool Finish(GameState s)
        { if (!CanFinish(s)) return false; s.journey.completed = true; return true; }

        public static List<LivelihoodAction> Actions(GameState s)
        {
            var result = new List<LivelihoodAction>();
            if (!IsActive(s) || s.journey.turns >= MaxTurns || PendingHardship(s) != null) return result;
            result.Add(Action("dock", "码头扛货", "收入高，肩膀吃力。接两轮可把码头定为主业。", "port", 10, -10, 0, 1, true));
            result.Add(Action("shop", "街市理货", "整理货物、核清账目。接两轮可转做柜台经营。", "market", 7, -4, 0, 4, true));
            result.Add(Action("courier", "信局送批", "核对批封后亲自送达。接两轮可转入侨批行当。", "postoffice", 6, -6, 0, 6, true));
            result.Add(Action("rest", "留一日养身", s.money >= 2 ? "留两份吃热饭、歇肩膀。休息也算一轮生活安排。" : "先到同乡屋里喝粥歇脚，不因身无分文堵住去路。", "quarters", s.money >= 2 ? -2 : 0, 16, 0, 0, false));
            result.Add(Action("remit", "先寄六份家用", "附上简短家书，留下汇出记录；自写长信可去写批桌。", "postoffice", -6, 0, 7, 2, false));
            foreach (LivelihoodAction action in result)
            {
                if (s.money + action.money < 0) Disable(action, "盘缠不足 " + -action.money + " 份");
                else if (action.physical && s.health < 18) Disable(action, "体力不足 18，先歇一日");
                else if (action.id == "dock" && s.health < 24) Disable(action, "扛货需要至少 24 体力");
                else if (Count(s, action.id) >= (action.id == "remit" ? 2 : 3)) Disable(action, "这段日子已做足，试试别的安排");
            }
            return result;
        }

        public static bool CommitAction(GameState s, string actionId, out string outcome)
        {
            outcome = "";
            if (!StoryEngine.Validate(s)) return false;
            LivelihoodAction action = Actions(s).Find(a => a.id == actionId && a.enabled);
            if (action == null) return false;
            Apply(s, action); s.journey.turns++; s.journey.pendingJob = "";
            switch (action.id)
            {
                case "dock": outcome = "货包入栈，你把十份工钱攥在手里，肩膀也添了一层酸痛。明日是否再接这份活，由你决定。"; break;
                case "shop": outcome = "你将进出货逐项记清，领到七份工钱。掌柜开始让你看见柜台后面怎样算一笔长久的账。"; break;
                case "courier": outcome = "收件人核清批封，你才领到六份工钱。你亲自跑过的门牌，成了下回少走弯路的经验。"; break;
                case "rest": outcome = "你在合住屋吃饭歇脚，没有赚到新工钱，却让身体重新有了余地。少接一天活，也是一种自己作主。"; break;
                default:
                    outcome = "六份家用与一页短信托往泉州，盘缠少了，家里的等待却有了一个明确的数。凭据已收进信匣。";
                    s.letters.Add(new LetterRecord { title = "谋生日里的六份家用", date = "1906年冬", route = DestinationName(s) + " → 厦门 → 泉州晋江县", body = "母亲、阿满：随批寄家用六份，近来做工与休息都在安排。此款先应家中急需，收妥请回批告知；儿会留足自己的饭钱，量力再寄。", signature = "文生 敬上", amount = 6, incoming = false });
                    Flag(s, "livelihood_remit"); break;
            }
            s.journey.log.Add(new JourneyLog { actionId = action.id, title = action.title, outcome = outcome, turn = s.journey.turns });
            if (action.physical && Count(s, action.id) >= 2)
            {
                s.journey.primaryTrade = action.id;
                s.flags.Remove("route_dock"); s.flags.Remove("route_shop"); s.flags.Remove("route_courier");
                Flag(s, "route_" + action.id);
                if (action.id == "shop") Flag(s, "shop_partner");
                if (action.id == "courier") Flag(s, "verified");
                if (action.id == "dock") Flag(s, "crew_help");
                outcome += " 这两轮经历让你能以这行作为往后的主业。";
                s.journey.log[s.journey.log.Count - 1].outcome = outcome;
            }
            if (s.journey.turns == 2)
            {
                s.journey.hardshipId = DestinationId(s);
                s.money = Math.Max(0, s.money - 4); s.health = Clamp(s.health - 3);
                Flag(s, "livelihood_hardship");
                outcome += " 街口的一场刁难又让你损失四份盘缠、三点体力；接下来必须决定怎样应对。";
                s.journey.log[s.journey.log.Count - 1].outcome = outcome;
            }
            return true;
        }

        public static JourneyHardship PendingHardship(GameState s)
        {
            if (!IsActive(s) || string.IsNullOrEmpty(s.journey.hardshipId) || s.journey.hardshipResolved) return null;
            var h = new JourneyHardship();
            switch (s.journey.hardshipId)
            {
                case "penang":
                    h.title = "街口的一笔勒索";
                    h.body = "替人搬货经过街口时，收取地头钱的人拦住你，要你交一笔没有收据的费用。你问了一句缘由，便被推搡，连带丢了四份盘缠。一个异乡雇工的名字似乎不值他们停步。旁边店家看见了这件事：你可以留证找见证人，也可以先结伴换路。"; break;
                case "rangoon":
                    h.title = "雨棚下的逐客令";
                    h.body = "连日落雨，住处的包租人借口你是新来的外乡人，临时加收押钱，还叫人把包袱扔到湿地上。争执中四份盘缠已被拿走，身体也受了寒。对方威胁不许你再回来住。你必须先找一个安全落脚处，再决定如何追问那笔钱。"; break;
                default:
                    h.title = "被扣下的工钱";
                    h.body = "替货栈补工后，包工头无故扣下四份工钱。你拿出记下的数目，他叫人推开你，还威胁让别处也不用你。异乡雇工并不能靠一句道理就拿回报酬。许生记得当日的货数，你可以请他作证，也可以结伴换工，不必把受欺负当成自己的过错。"; break;
            }
            h.body += "\n此处人物与事件为虚构，表现异乡劳工的困境，不对应某一次真实事件。";
            h.choices.Add(Action("evidence", "留证，请同乡一起追问", "同乡先垫奔走费，追回后扣还：净盘缠 +1 · 体力 −2 · 信用 +8。", "", 1, -2, 0, 8, false));
            h.choices.Add(Action("mutual_aid", "结伴换工，先保住落脚处", "盘缠 −2 · 体力 +6 · 信用 +5；接受照应，另找出路。", "", -2, 6, 0, 5, false));
            h.choices.Add(Action("endure", "暂且忍下，多做一轮急工", "盘缠 +4 · 体力 −10 · 信用 −3；眼前有钱，身体付出代价。", "", 4, -10, 0, -3, false));
            if (s.money < 2) Disable(h.choices[1], "需要两份搬住处与热食的盘缠");
            return h;
        }

        public static bool ResolveHardship(GameState s, string choiceId, out string outcome)
        {
            outcome = "";
            if (!StoryEngine.Validate(s)) return false;
            JourneyHardship hardship = PendingHardship(s);
            LivelihoodAction a = hardship == null ? null : hardship.choices.Find(x => x.id == choiceId && x.enabled);
            if (a == null) return false;
            Apply(s, a); s.journey.hardshipResolved = true; Flag(s, "hardship_" + choiceId);
            if (choiceId == "evidence")
            {
                Flag(s, "copies");
                outcome = "你把日期、数目与见证人姓名记清，同乡先垫下奔走费，陪你去追问。最后只追回三份，扣还两份路费，受辱的事也没有因此消失；但留底与有人作证，让你第一次不必独自承受。";
            }
            else if (choiceId == "mutual_aid")
            { Flag(s, "crew_help"); outcome = "同乡替你腾出一处床位，你留两份作搬住处与热食的钱。旧损失没有追回，你却离开了那处欺压人的关系，准备按自己的条件再找活。"; }
            else outcome = "你没有当场再争，把急工做完才领到四份。钱袋补回了一点，受累的身体却不能抵销；你把经过记进生活簿，往后仍可换工、歇息或向同乡求助。";
            s.journey.log.Add(new JourneyLog { actionId = "hardship_" + choiceId, title = hardship.title + " · " + a.title, outcome = outcome, turn = s.journey.turns });
            return true;
        }

        public static bool Validate(GameState s)
        {
            if (s == null || s.journeyVersion < 0 || s.journeyVersion > 1) return false;
            JourneyState j = s.journey;
            if (s.journeyVersion == 0)
            {
                // Default inline object fields have no gameplay meaning in an old
                // save. Real attached progress without its schema marker is rejected.
                return j == null || (j.turns == 0 && !j.hardshipResolved &&
                    string.IsNullOrEmpty(j.hardshipId) && string.IsNullOrEmpty(j.primaryTrade) &&
                    string.IsNullOrEmpty(j.pendingJob) && (j.log == null || j.log.Count == 0));
            }
            if (j == null) return false;
            if (Array.IndexOf(Destinations, j.destination) < 0 || j.turns < 0 || j.turns > MaxTurns || j.log == null || j.log.Count > MaxTurns + 1) return false;
            if (j.primaryTrade != "" && j.primaryTrade != "dock" && j.primaryTrade != "shop" && j.primaryTrade != "courier") return false;
            if (!string.IsNullOrEmpty(j.pendingJob) && (j.pendingJob != "dock" && j.pendingJob != "shop" && j.pendingJob != "courier" || !IsActive(s) || PendingHardship(s) != null || j.turns >= MaxTurns)) return false;
            if (j.hardshipId != "" && j.hardshipId != j.destination) return false;
            if (j.turns < 2 && !string.IsNullOrEmpty(j.hardshipId)) return false;
            if (j.turns >= 2 && string.IsNullOrEmpty(j.hardshipId)) return false;
            if (j.hardshipResolved && string.IsNullOrEmpty(j.hardshipId)) return false;
            if (j.completed && !j.legacy && (j.turns < MinTurns || !j.hardshipResolved)) return false;
            int actions = 0, hardships = 0;
            foreach (JourneyLog item in j.log)
            {
                if (item == null || string.IsNullOrEmpty(item.actionId) || string.IsNullOrEmpty(item.title) || string.IsNullOrEmpty(item.outcome) || item.turn < 1 || item.turn > MaxTurns) return false;
                if (item.actionId.StartsWith("hardship_", StringComparison.Ordinal)) hardships++; else actions++;
            }
            return actions == j.turns && hardships == (j.hardshipResolved ? 1 : 0);
        }

        private static LivelihoodAction Action(string id, string title, string description, string region, int money, int health, int family, int trust, bool physical)
        { return new LivelihoodAction { id = id, title = title, description = description, regionId = region, money = money, health = health, family = family, trust = trust, physical = physical }; }
        private static void Disable(LivelihoodAction a, string reason) { a.enabled = false; a.unavailableReason = reason; }
        private static int Count(GameState s, string id) { return s.journey.log.FindAll(x => x.actionId == id).Count; }
        private static int Clamp(int n) { return Math.Max(0, Math.Min(100, n)); }
        private static void Apply(GameState s, LivelihoodAction a)
        { s.money = Math.Max(0, Math.Min(9999, s.money + a.money)); s.health = Clamp(s.health + a.health); s.family = Clamp(s.family + a.family); s.trust = Clamp(s.trust + a.trust); }
        private static void Flag(GameState s, string flag) { if (!s.flags.Contains(flag)) s.flags.Add(flag); }
    }
}
