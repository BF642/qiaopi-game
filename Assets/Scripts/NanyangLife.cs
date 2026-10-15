using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class NanyangState
    {
        public string stage = "", wageRoute = "", response = "", outcome = "";
        public string visitingWorld = "quarters";
        public int wageTurn, taskProgress, owed, recovered, promisedWage, paidWage;
        public int lastMealTurn = -1;
        public bool letterDeferred, letterSent, replyCollected, transferBonusUsed;
        public string sentToken = "", recipient = "", intent = "";
        public int amount, personalLettersAtStart;
        public LetterRecord pendingReply;
    }

    // Versioned extension of the free-life interlude. Physical world interactions
    // call CompleteStep with the saved zero-based index; reads never pay rewards.
    public static class NanyangLife
    {
        public const int CurrentVersion = 1;
        public const string WorkDate = "1906年冬";
        public const string ReplyDate = "1906年冬 · 寄出数周后（叙事时间）";

        public static bool IsEnrolled(GameState s)
        { return s != null && s.nanyangVersion == CurrentVersion && s.nanyang != null; }
        public static bool IsBlocking(GameState s)
        { return IsEnrolled(s) && (s.nanyang.stage == "choice" || s.nanyang.stage == "tasks" || s.nanyang.stage == "letter" || s.nanyang.stage == "waiting" || s.nanyang.stage == "reply"); }
        public static int PendingReplyCount(GameState s)
        { return IsEnrolled(s) && s.nanyang.letterSent && !s.nanyang.replyCollected ? 1 : 0; }

        public static bool TryBegin(GameState s)
        {
            if (s == null || s.nanyangVersion != 0 || s.nodeId != "first_pay" || s.awaitingContinue ||
                FirstDayChapter.IsActive(s) || s.journeyVersion != 1 || s.journey == null || s.journey.legacy ||
                s.journey.completed || s.journey.turns != 0 || !StoryEngine.Validate(s)) return false;
            s.nanyang = new NanyangState { stage = "free", owed = 4, personalLettersAtStart = s.personalLettersSent };
            s.nanyangVersion = CurrentVersion;
            return true;
        }

        public static bool TryMeal(GameState s, out string notice)
        {
            notice = "";
            if (!IsEnrolled(s) || !LifeJourney.IsActive(s) || IsBlocking(s) || !StoryEngine.Validate(s) ||
                !string.IsNullOrEmpty(s.journey.pendingJob) || s.nanyang.lastMealTurn == s.journey.turns || s.money < 2) return false;
            s.money -= 2; s.health = Clamp(s.health + 8); s.nanyang.lastMealTurn = s.journey.turns;
            notice = "花两份盘缠吃一碗热饭，体力 +8。这一轮已吃过；饭钱不占谋生轮次。";
            return true;
        }
        public static bool Visit(GameState s, string world)
        {
            if (!World(world) || !IsEnrolled(s) || !LifeJourney.IsActive(s) || IsBlocking(s) ||
                !string.IsNullOrEmpty(s.journey.pendingJob) || !StoryEngine.Validate(s)) return false;
            s.nanyang.visitingWorld = world;
            return true;
        }

        public static int CareerLevel(GameState s, string route)
        { return Route(route) ? Math.Min(3, WorkCount(s, route) + 1) : 0; }
        public static string CareerName(GameState s, string route)
        {
            int level = CareerLevel(s, route);
            if (LifeJourney.DestinationId(s)=="semarang") {
                if(route=="dock") return level==1?"糖栈搬工":level==2?"分货熟工":"糖栈记工";
                if(route=="shop") return level==1?"华埠铺学徒":level==2?"街铺伙计":"铺中管账";
                if(route=="courier") return level==1?"银信跑街":level==2?"华埠批脚":"银信经手";
            }
            if (route == "dock") return level == 1 ? "码头搬工" : level == 2 ? "货栈熟工" : "理货记工";
            if (route == "shop") return level == 1 ? "杂货铺学徒" : level == 2 ? "柜台伙计" : "独立管账";
            if (route == "courier") return level == 1 ? "跑街学徒" : level == 2 ? "熟路批脚" : "核批经手";
            return "";
        }
        public static LivelihoodAction WorkReward(GameState s, string route)
        {
            if (!Route(route)) return null;
            int level = CareerLevel(s, route);
            string description, region;
            int money, health, trust;
            if (route == "dock") {
                money = 10 + (level - 1) * 2; health = level == 1 ? -10 : level == 2 ? -8 : -6; trust = level == 1 ? 1 : level == 2 ? 3 : 5; region = "port";
                description = level == 1 ? "把货包从埠头送入货栈，先认搬运路线和工账。" : level == 2 ? "按货签核对并分送货物，熟路可以省些肩膀。" : "核清入栈货单与搬运记工，留下可核对的交接记录。";
            } else if (route == "shop") {
                money = 7 + (level - 1) * 3; health = level == 1 ? -4 : -3; trust = 4 + level - 1; region = "market";
                description = level == 1 ? "先认货物与斤两，核对一笔进货。" : level == 2 ? "接待街坊并核清货款，练习独自照看柜台。" : "整理进货与赊欠账，查清账物不符的一笔。";
            } else {
                money = 6 + (level - 1) * 3; health = level == 1 ? -6 : level == 2 ? -5 : -4; trust = 6 + level - 1; region = "postoffice";
                description = level == 1 ? "核对批封后走一趟门牌，亲自取得交接。" : level == 2 ? "分清同名收件人和转交路线，把批送到认准的人手里。" : "核清款额、地址与收讫记录，处理一封需要复看的批。";
            }
            if (LifeJourney.DestinationId(s)=="semarang") description = route=="dock" ? (level==1?"把糖货与布匹分别送到红瓦仓栈前的交接处，留下入栈签记。":level==2?"急雨将至，先护糖货；可以和工友分担，也可以自己抢运。":"带新来的搬工认糖袋货签，按人分别登记工数。") : route=="shop"?"在华埠街铺称货、找零，区分实收和街坊赊欠。":"到银信代办处核批封，问清街坊门牌，取回本人收讫凭据。";
            description += " 议定工钱 " + money + " 份；同类经历越多，可接的职责越深。";
            return new LivelihoodAction { id = route, title = CareerName(s, route), description = description, regionId = region,
                money = money, health = health, trust = trust, physical = true };
        }
        public static int WorkCount(GameState s, string route)
        {
            int count = 0;
            if (s != null && s.journeyVersion == 1 && s.journey != null && s.journey.log != null)
                foreach (JourneyLog item in s.journey.log) if (item != null && item.actionId == route) count++;
            return count;
        }

        public static List<LivelihoodAction> Choices(GameState s)
        {
            var result = new List<LivelihoodAction>();
            if (!At(s, "choice")) return result;
            result.Add(Response("endure", "暂且忍下，核收已发工钱", "亲自核收已发的部分，四份欠款不追回；体力 −2，信用不变。", 0, -2, 0));
            result.Add(Response("evidence", "留工账，请许生作证", "取回凭据、请许生核实，再到柜台追讨。完成三步后追回三份，体力 −2、信用 +8。", 3, -2, 8));
            result.Add(Response("together", "召集工友，共同交涉", "找到工友，再一起核对欠款。完成两步后追回四份，体力 −4、信用 +5。", 4, -4, 5));
            result.Add(Response("leave", "离开旧工，问清新条件", "问清另一行的工钱与住处，再安顿下来。完成两步后体力 +6、信用 +3；下次异职业工钱另加两份，仅一次。", 0, 6, 3));
            return result;
        }
        public static bool ChooseResponse(GameState s, string id)
        {
            if (!At(s, "choice") || !ResponseId(id)) return false;
            s.nanyang.response = id; s.nanyang.stage = "tasks";
            return true;
        }
        public static int RequiredSteps(GameState s)
        { return IsEnrolled(s) ? Steps(s.nanyang.response) : 0; }

        public static bool CompleteStep(GameState s, int expectedIndex, out string notice)
        {
            notice = "";
            if (!At(s, "tasks") || expectedIndex != s.nanyang.taskProgress) return false;
            NanyangState n = s.nanyang;
            int needed = Steps(n.response);
            if (needed == 0 || expectedIndex < 0 || expectedIndex >= needed) return false;
            bool last = expectedIndex + 1 == needed;
            string responseFlag = "hardship_" + n.response;
            string extraFlag = n.response == "evidence" ? "copies" : n.response == "together" ? "crew_help" : "";
            if (last && (s.flags.Count + MissingFlag(s, responseFlag) + MissingFlag(s, extraFlag) > 80 || s.journey.log.Count >= LifeJourney.MaxTurns + 1)) return false;
            n.taskProgress++;
            if (!last) {
                notice = n.response == "evidence" ? (n.taskProgress == 1 ? "工账副本已取到，日期与数目都留着。接下来请许生核实。" : "许生确认了当日的工数。带上工账，到柜台把欠款逐项说清。")
                    : n.response == "together" ? "工友答应同去，把各自的工数先记在一处。现在一起到柜台交涉。" : "新工作的工钱与结账时间已问清。先安顿住处，再决定何时接工。";
                return true;
            }
            int recovered = n.response == "evidence" ? 3 : n.response == "together" ? 4 : 0;
            int health = n.response == "evidence" || n.response == "endure" ? -2 : n.response == "together" ? -4 : 6;
            int trust = n.response == "evidence" ? 8 : n.response == "together" ? 5 : n.response == "endure" ? 0 : 3;
            s.money = Math.Min(9999, s.money + recovered); s.health = Clamp(s.health + health); s.trust = Clamp(s.trust + trust);
            Flag(s, responseFlag); Flag(s, extraFlag);
            n.recovered = recovered;
            n.outcome = n.response == "endure" ? "你把已经领到的工钱核过一遍，暂且没有追问。四份欠款仍未拿回，也没有被记成已付。回到住处，你一遍遍想着刚才没说出口的话，体力 −2。许生没有替你做决定，只说：哪天想找人商量，我在。"
                : n.response == "evidence" ? "工账与许生的见证摆到了柜台上。对方交回三份，仍有一份没追回；你记下未清的数目，也知道以后每一轮工都要留下凭据。"
                : n.response == "together" ? "工友把各自记下的数目一一对齐，陪你共同交涉。四份欠下的工钱终于领回；奔走很累，这一次却不是你独自开口。"
                : "你没有追回这四份工钱。新工作的结账条件已问明，住处也重新安顿；身体缓过来一些。下次换到另一行做工，同乡介绍会让工钱另多两份。";
            s.journey.hardshipResolved = true;
            s.journey.log.Add(new JourneyLog { actionId = responseFlag, title = "欠下的四份工钱 · " + (n.response == "endure" ? "暂且忍下" : n.response == "evidence" ? "留证追讨" : n.response == "together" ? "共同交涉" : "另找出路"), outcome = n.outcome, turn = s.journey.turns });
            n.personalLettersAtStart = s.personalLettersSent;
            n.stage = "letter";
            notice = n.outcome + " 晚间到住处写批桌，可以把这段经历告诉家里，也可以先留作草稿。";
            return true;
        }
        public static bool DeferLetter(GameState s)
        {
            if (!At(s, "letter")) return false;
            s.nanyang.letterDeferred = true; s.nanyang.stage = "complete";
            return true;
        }

        // PersonalLetters calls after outgoing/token/count are stored, before any
        // incoming record. Failure never writes any field, so its transaction can roll back.
        public static bool RecordSentLetter(GameState s, PersonalLetterDraft sent, LetterRecord reply)
        {
            if (!IsEnrolled(s) || s.nanyang.stage != "letter" || s.nodeId != "first_pay" || s.awaitingContinue ||
                !Validate(s) || sent == null || !Token(sent.token) || !Recipient(sent.recipient) || !Intent(sent.intent) ||
                Array.IndexOf(PersonalLetters.Amounts, sent.amount) < 0 || string.IsNullOrWhiteSpace(sent.body) ||
                sent.body.Trim().Length < 2 || sent.body.Length > PersonalLetters.MaxBodyCharacters || !ValidReply(reply, false) ||
                s.personalLetterTokens == null || !s.personalLetterTokens.Contains(sent.token) ||
                s.personalLettersSent != s.nanyang.personalLettersAtStart + 1 || s.personalLettersSent > PersonalLetters.MaxLetters ||
                s.letters == null || s.letters.Count == 0 || s.letters.Count >= 40) return false;
            LetterRecord outgoing = s.letters[s.letters.Count - 1];
            if (outgoing == null || outgoing.incoming || outgoing.body != sent.body || outgoing.amount != sent.amount) return false;
            NanyangState n = s.nanyang;
            LetterRecord saved = Copy(reply); saved.date = ReplyDate;
            n.sentToken = sent.token; n.recipient = sent.recipient; n.intent = sent.intent; n.amount = sent.amount;
            n.pendingReply = saved; n.letterSent = true; n.stage = "waiting";
            return true;
        }
        public static bool AdvanceWeeks(GameState s)
        {
            if (!At(s, "waiting")) return false;
            s.nanyang.stage = "reply";
            return true;
        }
        public static bool CollectReply(GameState s)
        {
            if (!At(s, "reply") || s.letters == null || s.letters.Count >= 40) return false;
            s.letters.Add(Copy(s.nanyang.pendingReply));
            s.nanyang.replyCollected = true; s.nanyang.stage = "complete";
            return true;
        }

        // CommitAction alone invokes these while its action/log are being recorded.
        internal static bool WillWithhold(GameState s, string route)
        { return IsEnrolled(s) && s.nanyang.stage == "free" && Route(route); }
        internal static void StartDispute(GameState s, string route, int promisedWage)
        {
            NanyangState n = s.nanyang;
            n.wageRoute = route; n.wageTurn = s.journey.turns; n.stage = "choice";
            n.promisedWage = promisedWage; n.paidWage = promisedWage - n.owed;
            s.journey.hardshipId = LifeJourney.DestinationId(s); Flag(s, "livelihood_hardship");
        }
        public static int TransferBonusForWork(GameState s, string route)
        { return IsEnrolled(s) && s.nanyang.stage == "complete" && s.nanyang.response == "leave" && !s.nanyang.transferBonusUsed && Route(route) && route != s.nanyang.wageRoute ? 2 : 0; }
        internal static string ApplyTransferBonus(GameState s, string route)
        {
            if (TransferBonusForWork(s, route) == 0) return "";
            s.money = Math.Min(9999, s.money + 2); s.nanyang.transferBonusUsed = true;
            return "新工的介绍兑现，这次异职业工钱另得两份（一次）。";
        }

        public static bool Validate(GameState s)
        {
            if (s == null || s.nanyangVersion < 0 || s.nanyangVersion > CurrentVersion) return false;
            NanyangState n = s.nanyang;
            if (s.nanyangVersion == 0) return n == null || Empty(n);
            if (n == null || !Stage(n.stage) || !World(n.visitingWorld) || n.owed != 4 || n.personalLettersAtStart < 0 || n.personalLettersAtStart > PersonalLetters.MaxLetters ||
                s.personalLettersSent < n.personalLettersAtStart || s.journeyVersion != 1 || s.journey == null || s.journey.legacy ||
                s.journey.log == null || s.flags == null || FirstDayChapter.IsActive(s)) return false;
            JourneyState j = s.journey;
            if (n.lastMealTurn < -1 || n.lastMealTurn > j.turns || n.lastMealTurn > LifeJourney.MaxTurns) return false;
            if (n.stage != "complete" && (s.nodeId != "first_pay" || s.awaitingContinue || j.completed)) return false;
            if (IsBlocking(s) && !string.IsNullOrEmpty(j.pendingJob)) return false;
            int physical = 0, hardships = 0, actions = 0, firstWorkTurn = 0; string firstRoute = "";
            bool laterOtherWork = false;
            var counts = new Dictionary<string, int>();
            foreach (JourneyLog item in j.log) {
                if (item == null || string.IsNullOrEmpty(item.actionId)) return false;
                if (item.actionId.StartsWith("hardship_", StringComparison.Ordinal)) {
                    hardships++;
                    if (item.actionId != "hardship_" + n.response || item.turn != n.wageTurn || actions != n.wageTurn) return false;
                } else {
                    actions++;
                    if ((!Route(item.actionId) && item.actionId != "rest" && item.actionId != "remit") || item.turn != actions) return false;
                    int count; counts.TryGetValue(item.actionId, out count); counts[item.actionId] = count + 1;
                    if (count + 1 > (item.actionId == "remit" ? 2 : 3)) return false;
                    if (Route(item.actionId)) {
                        physical++;
                        if (physical == 1) { firstWorkTurn = item.turn; firstRoute = item.actionId; }
                        else if (hardships != 1) return false;
                        if (item.turn > n.wageTurn && item.actionId != n.wageRoute) laterOtherWork = true;
                    }
                }
            }
            if (actions != j.turns || actions > LifeJourney.MaxTurns || j.turns < 0) return false;
            bool settled = n.stage == "letter" || n.stage == "waiting" || n.stage == "reply" || n.stage == "complete";
            if (n.stage == "free") {
                if (physical != 0 || n.wageTurn != 0 || n.promisedWage != 0 || n.paidWage != 0 || !string.IsNullOrEmpty(n.wageRoute) || !string.IsNullOrEmpty(n.response) || n.taskProgress != 0 ||
                    !string.IsNullOrEmpty(j.hardshipId) || j.hardshipResolved || hardships != 0) return false;
            } else {
                if (!Route(n.wageRoute) || n.wageRoute != firstRoute || n.wageTurn != firstWorkTurn || n.wageTurn < 1 ||
                    j.hardshipId != j.destination || !s.flags.Contains("livelihood_hardship") || j.hardshipResolved != settled || hardships != (settled ? 1 : 0)) return false;
                if (n.promisedWage != (n.wageRoute == "dock" ? 10 : n.wageRoute == "shop" ? 7 : 6) || n.paidWage != n.promisedWage - n.owed) return false;
                if (n.stage != "complete" && (physical != 1 || j.turns != n.wageTurn)) return false;
                if (n.stage == "choice") { if (!string.IsNullOrEmpty(n.response) || n.taskProgress != 0) return false; }
                else if (!ResponseId(n.response) || n.taskProgress < 0 || (settled ? n.taskProgress != Steps(n.response) : n.taskProgress >= Steps(n.response))) return false;
            }
            if (settled) {
                int recovered = n.response == "evidence" ? 3 : n.response == "together" ? 4 : 0;
                if (n.recovered != recovered || !Text(n.outcome, 6000) || !s.flags.Contains("hardship_" + n.response) ||
                    (n.response == "evidence" && !s.flags.Contains("copies")) || (n.response == "together" && !s.flags.Contains("crew_help"))) return false;
            } else if (n.recovered != 0 || !string.IsNullOrEmpty(n.outcome)) return false;
            if (n.transferBonusUsed && (n.stage != "complete" || n.response != "leave" || !laterOtherWork)) return false;
            bool sentStage = n.stage == "waiting" || n.stage == "reply";
            if (sentStage && (!n.letterSent || n.letterDeferred || n.replyCollected)) return false;
            if (n.stage == "complete") { if (n.letterSent == n.letterDeferred || n.replyCollected != n.letterSent) return false; }
            else if (!sentStage && (n.letterSent || n.letterDeferred || n.replyCollected)) return false;
            if (n.letterSent) {
                if (!Token(n.sentToken) || !Recipient(n.recipient) || !Intent(n.intent) || Array.IndexOf(PersonalLetters.Amounts, n.amount) < 0 ||
                    !ValidReply(n.pendingReply, true) || s.personalLetterTokens == null || !s.personalLetterTokens.Contains(n.sentToken) ||
                    s.personalLettersSent <= n.personalLettersAtStart || s.letters == null || (!n.replyCollected && s.letters.Count > 39)) return false;
            } else if (!string.IsNullOrEmpty(n.sentToken) || !string.IsNullOrEmpty(n.recipient) || !string.IsNullOrEmpty(n.intent) || n.amount != 0 || !EmptyLetter(n.pendingReply)) return false;
            return true;
        }

        static bool At(GameState s, string stage)
        { return IsEnrolled(s) && s.nanyang.stage == stage && StoryEngine.Validate(s) && Validate(s); }
        static bool Route(string id) { return id == "dock" || id == "shop" || id == "courier"; }
        static bool ResponseId(string id) { return id == "evidence" || id == "together" || id == "leave" || id == "endure"; }
        static int Steps(string response) { return response == "endure" ? 1 : response == "evidence" ? 3 : response == "together" || response == "leave" ? 2 : 0; }
        static bool Stage(string id) { return id == "free" || id == "choice" || id == "tasks" || id == "letter" || id == "waiting" || id == "reply" || id == "complete"; }
        static bool World(string id) { return id == "quarters" || id == "port" || id == "market" || id == "postoffice"; }
        static LivelihoodAction Response(string id, string title, string text, int money, int health, int trust)
        { return new LivelihoodAction { id = id, title = title, description = text, regionId = "", money = money, health = health, trust = trust }; }
        static int MissingFlag(GameState s, string flag) { return string.IsNullOrEmpty(flag) || s.flags.Contains(flag) ? 0 : 1; }
        static void Flag(GameState s, string flag) { if (!string.IsNullOrEmpty(flag) && !s.flags.Contains(flag)) s.flags.Add(flag); }
        static int Clamp(int value) { return Math.Max(0, Math.Min(100, value)); }
        static bool Recipient(string value) { return value == "mother" || value == "wife" || value == "sister"; }
        static bool Intent(string value) { return value == "reassure" || value == "truth" || value == "study"; }
        static bool Token(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length != 32) return false;
            foreach (char c in token) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
        static bool Empty(NanyangState n)
        { return string.IsNullOrEmpty(n.stage) && (string.IsNullOrEmpty(n.visitingWorld) || n.visitingWorld == "quarters") && string.IsNullOrEmpty(n.wageRoute) && string.IsNullOrEmpty(n.response) && string.IsNullOrEmpty(n.outcome) &&
            n.wageTurn == 0 && n.taskProgress == 0 && n.owed == 0 && n.recovered == 0 && n.promisedWage == 0 && n.paidWage == 0 && (n.lastMealTurn == -1 || n.lastMealTurn == 0) && !n.letterDeferred && !n.letterSent && !n.replyCollected && !n.transferBonusUsed &&
            string.IsNullOrEmpty(n.sentToken) && string.IsNullOrEmpty(n.recipient) && string.IsNullOrEmpty(n.intent) && n.amount == 0 && n.personalLettersAtStart == 0 && EmptyLetter(n.pendingReply); }
        static bool EmptyLetter(LetterRecord l)
        { return l == null || (!l.incoming && l.amount == 0 && string.IsNullOrEmpty(l.title) && string.IsNullOrEmpty(l.date) && string.IsNullOrEmpty(l.route) && string.IsNullOrEmpty(l.body) && string.IsNullOrEmpty(l.signature)); }
        static bool ValidReply(LetterRecord l, bool dated)
        { return l != null && l.incoming && l.amount == 0 && Text(l.title, 300) && Text(l.route, 300) && Text(l.body, 8000) && Text(l.signature, 100) && Text(l.date, 200) && (!dated || l.date == ReplyDate); }
        static bool Text(string value, int max) { return !string.IsNullOrEmpty(value) && value.Length <= max; }
        static LetterRecord Copy(LetterRecord l)
        { return new LetterRecord { title = l.title, date = l.date, route = l.route, body = l.body, signature = l.signature, incoming = l.incoming, amount = l.amount }; }
    }
}
