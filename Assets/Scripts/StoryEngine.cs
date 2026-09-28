using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class GameState
    {
        public int version = 1;
        public string nodeId = "home";
        public int money = 16, health = 76, family = 58, trust = 42;
        public int decisions;
        public List<string> flags = new List<string>();
        public List<LetterRecord> letters = new List<LetterRecord>();
        public List<HistoryRecord> history = new List<HistoryRecord>();
        // JsonUtility may materialize an inline object even when the old save
        // omitted it or the source value was null. This scalar is the migration marker.
        public int journeyVersion;
        public JourneyState journey;
        public PersonalLetterDraft personalLetterDraft = new PersonalLetterDraft();
        public int personalLettersSent;
        public List<string> personalLetterTokens = new List<string>();
        public bool awaitingContinue;
        public string lastOutcome = "", lastChanges = "", nextNodeId = "";
    }

    [Serializable]
    public sealed class LetterRecord
    {
        public string title, date, route, body, signature;
        public bool incoming;
        public int amount;
    }

    [Serializable]
    public sealed class HistoryRecord
    {
        public string title, choice, outcome, location, year;
    }

    [Serializable]
    public sealed class SceneData
    {
        public string id, year, location, title, body, quote, art;
        public int chapter;
        public List<ChoiceData> choices = new List<ChoiceData>();
        public bool isEnding;
        public string endingId = "";
    }

    [Serializable]
    public sealed class ChoiceData
    {
        public string id, title, hint, unavailableReason;
        public bool enabled = true;
    }

    /// <summary>
    /// Fictional family history inspired by Quanzhou qiaopi practices. Currency is an
    /// intentionally simplified game resource, not a historical exchange rate.
    /// Scene construction is read-only; only Choose and Continue mutate a save.
    /// </summary>
    public static class StoryEngine
    {
        private sealed class Decision
        {
            public ChoiceData data;
            public Action<GameState> action;
        }
        private sealed class Node
        {
            public SceneData scene;
            public List<Decision> actions = new List<Decision>();
        }
        private static readonly string[] NodeIds = {
            "peace", "pressure", "home", "funding", "farewell", "passage", "shore",
            "dock", "shop", "courier", "first_pay", "first_letter", "reply", "records",
            "missing", "trace_desk", "trace_harbor", "trace_family", "storms", "horizon",
            "return_choice", "stay_choice",
            "end_home_lamp", "end_home_scar", "end_school_window", "end_shop_bridge",
            "end_trusted_route", "end_new_harbor", "end_unsent_letter", "end_silver_thread"
        };

        public static GameState NewGame() { return NewGame(Guid.NewGuid().GetHashCode()); }
        public static GameState NewGame(int seed) { return new GameState { nodeId = "peace", journeyVersion = 1, journey = LifeJourney.Create(seed) }; }

        public static SceneData GetScene(GameState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            return Localized(state, Build(state)).scene;
        }

        public static bool Choose(GameState state, string choiceId)
        {
            if (!Validate(state) || state.awaitingContinue || string.IsNullOrEmpty(choiceId)) return false;
            Node node = Localized(state, Build(state));
            if (node.scene.isEnding) return false;
            Decision selected = node.actions.Find(x => x.data.id == choiceId);
            if (selected == null || !selected.data.enabled) return false;
            int money = state.money, health = state.health, family = state.family, trust = state.trust;
            selected.action(state);
            state.decisions++;
            state.lastChanges = Changes(money, health, family, trust, state);
            state.history.Add(new HistoryRecord {
                title = node.scene.title, choice = selected.data.title, outcome = state.lastOutcome,
                location = node.scene.location, year = node.scene.year
            });
            state.awaitingContinue = true;
            return true;
        }

        public static bool Continue(GameState state)
        {
            if (!Validate(state) || !state.awaitingContinue || !Known(state.nextNodeId)) return false;
            state.nodeId = state.nextNodeId;
            state.nextNodeId = "";
            state.awaitingContinue = false;
            state.lastOutcome = "";
            state.lastChanges = "";
            OnEnter(state);
            return true;
        }

        public static bool Validate(GameState state)
        {
            if (state == null || state.version != 1 || !Known(state.nodeId)) return false;
            if (state.money < 0 || state.money > 9999 || state.health < 0 || state.health > 100 ||
                state.family < 0 || state.family > 100 || state.trust < 0 || state.trust > 100) return false;
            if (state.decisions < 0 || state.decisions > 17 || state.flags == null ||
                state.letters == null || state.history == null || state.flags.Count > 80 ||
                state.letters.Count > 40 || state.history.Count != state.decisions) return false;
            if (state.flags.Exists(x => string.IsNullOrEmpty(x) || x.Length > 80)) return false;
            if (state.letters.Exists(x => x == null || x.amount < 0 || x.amount > 9999 || x.body == null)) return false;
            if (state.history.Exists(x => x == null || x.title == null || x.choice == null || x.outcome == null)) return false;
            if (state.awaitingContinue && (!Known(state.nextNodeId) || string.IsNullOrEmpty(state.lastOutcome))) return false;
            if (!state.awaitingContinue && !string.IsNullOrEmpty(state.nextNodeId)) return false;
            if (state.nodeId.StartsWith("end_", StringComparison.Ordinal) && state.awaitingContinue) return false;
            if (!LifeJourney.Validate(state) || !PersonalLetters.ValidateState(state)) return false;
            return true;
        }

        private static Node Localized(GameState s, Node n)
        {
            n.scene.location = LifeJourney.Localize(s, n.scene.location);
            n.scene.body = LifeJourney.Localize(s, n.scene.body);
            n.scene.quote = LifeJourney.Localize(s, n.scene.quote);
            foreach (ChoiceData c in n.scene.choices)
            {
                c.title = LifeJourney.Localize(s, c.title);
                c.hint = LifeJourney.Localize(s, c.hint);
            }
            return n;
        }

        private static bool Known(string id) { return id != null && Array.IndexOf(NodeIds, id) >= 0; }
        private static bool Has(GameState s, string f) { return s.flags.Contains(f); }
        private static bool Any(GameState s, params string[] fs)
        {
            foreach (string f in fs) if (Has(s, f)) return true;
            return false;
        }
        private static void Flag(GameState s, string f) { if (!Has(s, f)) s.flags.Add(f); }
        private static int Limit(int value) { return Math.Max(0, Math.Min(100, value)); }
        private static void Apply(GameState s, int money = 0, int health = 0, int family = 0, int trust = 0, params string[] flags)
        {
            s.money = Math.Max(0, s.money + money);
            s.health = Limit(s.health + health);
            s.family = Limit(s.family + family);
            s.trust = Limit(s.trust + trust);
            foreach (string flag in flags) Flag(s, flag);
        }
        private static void Result(GameState s, string outcome, string next)
        {
            s.lastOutcome = LifeJourney.Localize(s, outcome);
            s.nextNodeId = next;
        }
        private static Node Scene(GameState s, int chapter, string year, string location, string title, string body, string quote, string art)
        {
            return new Node { scene = new SceneData { id = s.nodeId, chapter = chapter, year = year,
                location = location, title = title, body = body, quote = quote, art = art } };
        }
        private static void Option(Node n, string id, string title, string hint, Action<GameState> action,
            bool enabled = true, string reason = "")
        {
            var data = new ChoiceData { id = id, title = title, hint = hint, enabled = enabled,
                unavailableReason = enabled ? "" : reason };
            n.scene.choices.Add(data);
            n.actions.Add(new Decision { data = data, action = action });
        }
        private static void Letter(GameState s, string title, string date, string route, string body, string signature, bool incoming, int amount)
        {
            s.letters.Add(new LetterRecord { title = title, date = date, route = LifeJourney.Localize(s, route), body = LifeJourney.Localize(s, body),
                signature = signature, incoming = incoming, amount = amount });
        }
        private static string Changes(int money, int health, int family, int trust, GameState s)
        {
            var values = new List<string>();
            Change(values, "盘缠", s.money - money);
            Change(values, "体力", s.health - health);
            Change(values, "亲情", s.family - family);
            Change(values, "信用", s.trust - trust);
            return values.Count == 0 ? "这次选择已记入行旅。" : string.Join("　·　", values.ToArray());
        }
        private static void Change(List<string> changes, string name, int difference)
        {
            if (difference != 0) changes.Add(name + " " + (difference > 0 ? "+" : "") + difference);
        }
        private static bool School(GameState s) { return Any(s, "sister_school", "twin_promise", "ask_school", "school_supported"); }
        private static bool Debt(GameState s) { return Has(s, "debt") && !Has(s, "debt_paid"); }
        private static bool Unsettled(GameState s) { return !Has(s, "resolved") || Has(s, "concealed") || Debt(s); }
        private static string Belonging(GameState s)
        {
            if (Has(s, "ledger")) return "那本旧账簿还在包底，数字旁留着父亲当年的笔迹。";
            if (Has(s, "quilt")) return "母亲缝的薄被已经磨白，被角那针线仍牢牢拢着。";
            return "小木盒里收着妹妹写的门牌，几个字歪着，却一个也没有少。";
        }
        private static string HealthLine(GameState s)
        {
            if (s.health < 30) return "你的咳嗽拖了很久，走过一条街便要停下来；往后的路，得给身体留一点余地。";
            if (s.health < 50) return "你揉了揉酸痛的肩膀，终于明白，工钱不能替人把觉睡回来。";
            return "你尚有力气安排下一程，却已懂得把休息也算进日子里。";
        }

        private static Node Build(GameState s)
        {
            Node n;
            switch (s.nodeId)
            {
                case "peace":
                    n = Scene(s, 0, "1905年秋", "泉州府晋江县 · 榕溪村（虚构）", "还没有远行的日子",
                        "晒谷的竹席铺在屋前，母亲正把一锅热饭盛出来。阿满把刚学的字写在石板上，非要你认。你叫陈文生，今年十九岁；那时候，天色一暗就能回到这张桌前，明日的打算只是修一段篱笆、去圩上卖一点货。村里日子并不富裕，却还有一家人在一起慢慢商量的余地。午后的空闲，你想怎样度过？",
                        "后来越过海的那些字，最早只是家门前的一句闲话。", "quanzhou");
                    Option(n, "meal", "陪母亲吃饭，听阿满念字", "亲情 +3；把团圆的这一日记住", x => {
                        Apply(x, 0, 0, 3, 0, "peace_family"); Result(x, "阿满把你的名字念得格外响。母亲笑着添饭，你答应收工后还教她两个字。多年以后想起家，先想起的是这顿不必赶着吃的饭。", "pressure"); });
                    Option(n, "errand", "帮邻里送米，顺路逛一回圩", "信用 +3；记住有人认得你的街巷", x => {
                        Apply(x, 0, 0, 0, 3, "peace_neighbors"); Result(x, "你把米送到门口，邻人留你喝茶。圩上有人叫得出你的名字，你绕了一点远路，傍晚仍赶得上家中的热饭。", "pressure"); });
                    return n;

                case "pressure":
                    n = Scene(s, 0, "1906年春", "榕溪村 · 雨后的陈家", "屋檐下的风雨",
                        "一季暴雨损了田里的收成，陈家的屋瓦也塌了一角。修屋、买粮与母亲的药叠在一起，原先够一家过日子的收入断了。更难的是，替债主催账的打手闯到门口，掀翻竹席，逼你们立即还钱。邻里劝住争执，却不能替所有人填平缺口。你不是为了发财才想到离乡，而是要给家里找一条能继续生活的路。接下来这几日，你先做什么？",
                        "这一家人的灾后困境与遭遇为虚构，不对应某一场具体历史事件。", "quanzhou");
                    Option(n, "protect", "先护住母亲，把账目逐项记下", "体力 −3 · 亲情 +3；带着明确的家用缺口出门", x => {
                        Apply(x, 0, -3, 3, 0, "home_pressure", "family_account"); Result(x, "你把母亲扶回屋，拾起被踢散的碗与纸。你没有认下口说无凭的加价，只记清本来欠着多少。族亲提起南洋有工可找，你终于坐下来听。", "home"); });
                    Option(n, "neighbors", "请邻里作证，先商量宽限的日子", "信用 +4；有人照看家里，离乡也有了牵系", x => {
                        Apply(x, 0, 0, 0, 4, "home_pressure", "home_witness"); Result(x, "邻里陪你把欠账与宽限的日子写清，答应你外出后常来看看母亲。危机没有一夜过去，但你不必再把一切独自扛着。族亲递来南洋招工的口信。", "home"); });
                    return n;

                case "home":
                    n = Scene(s, 0, "1906年春", "泉州府晋江县 · 榕溪村（虚构）", "门槛以外",
                        "雨水漫进村里后，屋檐修了一半，欠账却先修成一摞。如今你二十岁了；母亲替你束好包袱，妹妹阿满蹲在门槛上，问南洋是否远过天边。族亲说，可以先到厦门，再寻有空位的船南下；靠泊与转船未定，未必就是最初听说的埠头。包袱已很沉，只能再添一样家里的东西。它未必值钱，却会陪你辨认陌生的路。",
                        "离乡时，人总想带走一点不必寄回的家。", "quanzhou");
                    Option(n, "ledger", "带上父亲的旧账簿", "盘缠 −2 · 信用 +4；账目与地址也许能派上用场", x => {
                        Apply(x, -2, 0, 0, 4, "ledger"); Result(x, "你买了一小束纸，把父亲的旧账簿夹在里面。第一页写着家中门牌，最后一页仍空白，像在等你的账。", "funding"); });
                    Option(n, "quilt", "带上母亲缝的薄被", "体力 +6 · 亲情 +3；海上的夜里有一层暖意", x => {
                        Apply(x, 0, 6, 3, 0, "quilt"); Result(x, "母亲又补了一针，才把薄被递来。你没有说重，接过时只说：南边下雨，也不会凉着。", "funding"); });
                    Option(n, "address", "带上妹妹写的门牌", "亲情 +8；让故乡的地址一直贴近身边", x => {
                        Apply(x, 0, 0, 8, 0, "address"); Result(x, "阿满把纸折进小木盒，说等她会写更多字，就能亲自给你回信。你把木盒收进贴身口袋。", "funding"); });
                    return n;

                case "funding":
                    n = Scene(s, 0, "1906年春", "榕溪村 · 陈家堂屋", "一程的盘缠",
                        "桌上两盏油灯，一盏照着借据，一盏照着族亲带来的介绍信。借足旅费，到了南洋就能缓一缓，只是家里的旧债上又要添一笔。跟同乡走，凑来的钱少些，却有人肯把名字写在你的名字旁边，作一句担保。母亲把两张纸都推到你面前，没有替你落笔。你明白，无论拿走哪一份，都有东西留在这间堂屋里，等着以后偿还。",
                        "盘缠能数清，人情却没有现成的算盘。", "quanzhou");
                    Option(n, "borrow", "立据借足旅费", "盘缠 +20 · 信用 −5；以后要还这一笔债", x => {
                        Apply(x, 20, 0, 0, -5, "debt"); Result(x, "你按下手印，把归还的期限记在心里。母亲将借据收入米瓮旁的匣子，说：有钱便还，先要平安。", "farewell"); });
                    Option(n, "guarantor", "接受同乡担保同行", "盘缠 +9 · 信用 +8；少些现钱，多一份托付", x => {
                        Apply(x, 9, 0, 0, 8, "guarantor"); Result(x, "族亲把介绍信交给你。钱只够谨慎走这一程，但南洋各埠的同乡间有往来，落脚后可请他们替你问一份工。", "farewell"); });
                    return n;

                case "farewell":
                    n = Scene(s, 0, "1906年春", "榕溪村 · 通往厦门的路口", "没说完的叮嘱",
                        "天还没亮，母亲已经把灶灰拨平。她近来总说膝头疼，却把药铺的账单塞在碗柜后面。阿满举着写错的字，想问村里教书的人肯不肯收她。车夫催着上路，你忽然觉得，走出去容易，答应家里一句话却很重。" + (Debt(s) ? "那张新借据像在包袱里压着，尽管它明明留在家中。" : "同乡正在前面等你，介绍信也等着你给它一个交代。") + "你蹲下身，决定把哪一句话说得清楚些。",
                        "没有写在纸上的，也会成为以后每封信的题目。", "quanzhou");
                    Option(n, "mother", "先答应母亲，按时寄药钱", "亲情 +8；家里的回信会记着这句承诺", x => {
                        Apply(x, 0, 0, 8, 0, "mother_remedy"); Result(x, "母亲说不用，你仍把药铺名字记了下来。阿满安静地收好字纸，替你提起包袱的一角。", "passage"); });
                    Option(n, "sister", "给妹妹留下第一笔学资", "盘缠 −3 · 亲情 +5；她会试着自己写回批", x => {
                        Apply(x, -3, 0, 5, 0, "sister_school"); Result(x, "你把三份盘缠放进阿满掌心，请她先去问学。她说，第一封信不让别人代写。母亲替她点了头。", "passage"); }, s.money >= 3, "需要 3 盘缠");
                    Option(n, "both", "把两件心事都记下", "盘缠 −5 · 亲情 +7；药钱与学资要一起张罗", x => {
                        Apply(x, -5, 0, 7, 0, "twin_promise"); Result(x, "你留下五份盘缠，分作药钱与笔墨。两个约定都不大，放在远行的人身上，却各有分量。", "passage"); }, s.money >= 5, "需要 5 盘缠");
                    return n;

                case "passage":
                    n = Scene(s, 1, "1906年初夏", "厦门 · 候船处与南下海路", "海上没有门牌",
                        "你从晋江县乡村来到厦门，在同乡指点下候到南下的船。码头上有人递行李，有人把姓名反复念给送行人听。船离岸后，潮湿的热气贴着舱板，身边一个姓许的年轻人晕得喝不下水。宽些的铺位还空着，换过去得再花钱；留在这里，就要学着同陌生人分一只水壶。" + Belonging(s) + "故乡已看不见，下一程的同行者倒近在眼前。",
                        "此段海路为虚构行程，不对应某一班历史航船。", "harbor");
                    Option(n, "share", "留下来，与许生互相照应", "盘缠 −4 · 信用 +6；舱里拥挤，也有人相扶", x => {
                        Apply(x, -4, Has(x, "quilt") ? -3 : -7, 0, 6, "ship_friend"); Result(x,
                            Has(x, "quilt") ? "你把薄被挪出半边，和许生轮流照看包袱。他说，到了码头，有事可去找他。那一针家里的线，竟先替你缝起一段异乡情分。" : "你买来清水，守着许生熬过摇晃的夜。他记下你的名字，说到了码头，有事可去找他。疲倦之外，你多了一个相识。", "shore"); }, s.money >= 4, "需要 4 盘缠");
                    Option(n, "berth", "添钱换一个能睡好的铺位", "盘缠 −9 · 体力 +3；为上岸后的谋生留力气", x => {
                        Apply(x, -9, 3, 0, 0, "better_berth"); Result(x, "一夜里你终于睡了片刻。醒来时，舱门外的人已换了位置。你把剩下的盘缠重新数过，打算上岸便找工。", "shore"); }, s.money >= 9, "需要 9 盘缠");
                    return n;

                case "shore":
                    n = Scene(s, 1, "1906年夏", "新加坡 · 河口街巷", "三个落脚处",
                        LifeJourney.DestinationIntro(s) + "你跟着同乡进了一间合住的屋子，床位只占墙边一角。三处地方愿让你试工：码头按力气给钱，杂货铺要人守账，办理侨批的信局则缺一名跑腿学徒。所谓批，就是信；可这里的批，往往还连着要送回家中的银钱。先选一份试工，之后你可以在几轮生活中自主接活、换行、歇息与寄钱。",
                        "落脚，不只是找到一张床，也是找到明天起身的缘由。", "singapore");
                    Option(n, "dock", "到河边码头做工", "盘缠 +1 · 体力 −2；工钱来得快，靠肩膀换", x => {
                        Apply(x, 1, -2, 0, 0, "route_dock"); Result(x, "工头让你先搬一趟空筐。你把介绍姓名的话咽回去，用肩膀应了这份工。", "dock"); });
                    Option(n, "shop", "进同乡杂货铺学柜台", "信用 +3；账目与人情要一起学", x => {
                        Apply(x, 0, 0, 0, 3, "route_shop"); Result(x, "陈掌柜把一把旧算盘放在你面前，说先认货，再认人，最后才认得一笔账为什么欠着。", "shop"); });
                    Option(n, "courier", "在侨批信局做学徒", "盘缠 −2 · 信用 +4；从核对批封与门牌学起", x => {
                        Apply(x, -2, 0, 0, 4, "route_courier"); Result(x, "何师傅先教你核对姓名、籍贯、收款地址与款额。他把一封批递来：这不是一张纸，是人家门里等着的日子。", "courier"); }, s.money >= 2, "需要 2 盘缠");
                    return n;

                case "dock":
                    n = Scene(s, 2, "1906年秋", "新加坡 · 河岸货栈", "肩上的分量",
                        "码头一天的时间，以货包起落来算。你渐渐认得绳结，认得哪一处木板下雨后最滑。今天一批货催着入栈，工头许了额外的工钱，年长的搬工却伤了脚，若没人接手，他这一日就算白来。货单上还有两处数目对不上，需要有人坐下来核清。" + (Has(s, "ship_friend") ? "许生隔着货堆朝你点头，他也在这里讨生活。" : "你在人群里寻找熟面孔，暂时只找到与你一样疲惫的眼睛。") + "手头的这一日，将成为以后别人记住你的样子。",
                        "一包货有重量，一份相帮也有。", "harbor");
                    Option(n, "extra", "接下加急的重货", "盘缠 +17 · 体力 −13；先攒下家里用得上的钱", x => {
                        Apply(x, 17, -13, 0, 0, "earned_dock"); Result(x, "傍晚你握不稳饭碗，却把额外工钱一份不少地收进袋里。肩上的红印提醒你，这样的日子不能没有尽头。", "first_pay"); });
                    Option(n, "help", "替伤脚的老搬工分一趟", "盘缠 +9 · 体力 −4 · 信用 +8；码头有人记你的情", x => {
                        Apply(x, 9, -4, 0, 8, "crew_help"); Result(x, "少挣了几份钱，老搬工却替你指清河口各处转货的门路。他说，以后找人找货，先来问他们。", "first_pay"); });
                    Option(n, "tally", "替货栈核清两张货单", "盘缠 +10 · 信用 +10；留下有用的往来记录", x => {
                        Apply(x, 10, 0, 0, 10, "marked_cargo"); Result(x, "你把货名、日期与经手人一项项抄清。账终于合上，货栈让你留下副本，说下一次还找你。", "first_pay"); }, Has(s, "ledger") || s.trust >= 56, "带着旧账簿，或信用达到 56 后可接手");
                    return n;

                case "shop":
                    n = Scene(s, 2, "1906年秋", "新加坡 · 陈记杂货铺（虚构）", "柜台这边",
                        "杂货铺的门板每日天不亮就卸下。盐、米、灯油，都得先记清斤两，才轮到自己的饭。陈掌柜问你，想领较高的现钱，还是少领一些，跟着学进货与赊账。门外又坐着一位同乡，握着家信，不认得上面的字，想找人念。你发现，这间铺子卖的固然是日用物件，街坊来去之间，也在交换谁能信得过、哪一条路能把话送到家。",
                        "柜台上的小算盘，也拨着海那边的米粮。", "singapore");
                    Option(n, "learn", "少领现钱，跟掌柜学经营", "盘缠 +11 · 体力 −4 · 信用 +8；为长远的生计铺路", x => {
                        Apply(x, 11, -4, 0, 8, "shop_partner"); Result(x, "你学会分清账上欠的钱和人家一时过不去的坎。陈掌柜把进货名册交给你保管，第一次让你独自开门。", "first_pay"); });
                    Option(n, "wages", "多守晚班，先把工资攒起来", "盘缠 +16 · 体力 −7 · 亲情 −2；写家信的时间少了", x => {
                        Apply(x, 16, -7, -2, 0, "cash_worker"); Result(x, "你一直守到街灯稀落。工资足了一些，给家里的草稿却压在枕下，连着几夜没能写完。", "first_pay"); });
                    Option(n, "read", "抽空替同乡念信、写回话", "盘缠 +8 · 亲情 +3 · 信用 +10；先学会听懂别人的家事", x => {
                        Apply(x, 8, 0, 3, 10, "read_letters"); Result(x, "你念到家中收成时，同乡忽然不出声了。落笔代写回话前，你等了很久。你开始明白，信里的空白也有意思。", "first_pay"); });
                    return n;

                case "courier":
                    n = Scene(s, 2, "1906年秋", "新加坡 · 同安泰信局（虚构）", "批封上的一行字",
                        "信局桌上的批封来自不同村落，写着名字、住处与款额。何师傅要你重新核对一封地址不清的批，另一位伙计却催你快些跑完今日的收件。有人怕寄错钱，也有人只怕信迟一旬；你夹在中间，才知道快慢都能成为别人的难处。" + (Has(s, "ledger") ? "父亲旧账簿的整齐栏线，恰好教你把来去次序抄得分明。" : "你在废纸上画出几道栏线，练习把细处记清。") + "做学徒的第一回取舍，就在这张桌上。",
                        "写对一个门牌，是让一户人家少等一阵。", "singapore");
                    Option(n, "verify", "逐项复核地址与经手记录", "盘缠 +9 · 体力 −4 · 信用 +12；留下日后可查的线索", x => {
                        Apply(x, 9, -4, 0, 12, "verified"); Result(x, "你问清村名与收款人的别名，照录经手日期。何师傅没有夸你，只把需要仔细核对的下一封也递过来。", "first_pay"); });
                    Option(n, "rush", "多跑几户，把这一日的件收齐", "盘缠 +15 · 体力 −8 · 信用 −7；多得报酬，少了核对时间", x => {
                        Apply(x, 15, -8, 0, -7, "hurried"); Result(x, "你一口气跑遍街巷，领到加给。两张未核完的底单留待明日，师傅提醒你：送得快，也要让后来人查得清。", "first_pay"); });
                    Option(n, "craft", "留下来练抄批封、辨款额", "盘缠 +7 · 信用 +10；慢慢学会承担一封批", x => {
                        Apply(x, 7, 0, 0, Has(x, "ledger") ? 12 : 10, "clerk_skill"); Result(x, "你把姓名、地址与款額抄了几遍，请师傅逐项复看。他说，手艺先是认真，之后才谈熟练。", "first_pay"); });
                    return n;

                case "first_pay":
                    n = Scene(s, 2, "1907年正月", "新加坡 · 合住屋", "工钱铺在席上",
                        "头一段日子总算熬过去，你把工钱铺在草席上，每一小堆都已经有了去处。屋租和饭钱扣过，家中的屋瓦、母亲的药、妹妹的笔墨仍在等。后天可以寄第一封侨批，明天也可以再接一轮活；而今晚，你的身体只想躺下。" + HealthLine(s) + "窗外有人试念写给孩子的信，反复删去一句辛苦。你把眼前的工钱又挪了一遍。",
                        "赚到手里，并不等于可以只为自己打算。", "singapore");
                    Option(n, "reserve", "先封好十二份家用", "盘缠 −12 · 亲情 +5；这笔钱专门留给第一封侨批", x => {
                        Apply(x, -12, 0, 5, 0, "remit_reserved"); Result(x, "你把十二份家用单独包好，再也不从里面抽钱。寄批那天，这一包会和你的字一起往泉州去。", "first_letter"); }, s.money >= 12, "需要 12 盘缠");
                    Option(n, "rest", "吃一顿热饭，歇好这两日", "盘缠 −5 · 体力 +15；家用仍从剩余盘缠中寄出", x => {
                        Apply(x, -5, 15, 0, 0, "rested"); Result(x, "你终于吃完一碗没有凉透的饭。睡醒时，手不再发抖，你能稳稳地把家人的名字写出来。", "first_letter"); }, s.money >= 5, "需要 5 盘缠");
                    Option(n, "overtime", "再接一轮活，凑厚一点的家用", "盘缠 +11 · 体力 −14；银钱添了，疲惫也添了", x => {
                        Apply(x, 11, -14, 0, 0, "overworked"); Result(x, "多出的工钱叠在席边，你却靠着墙睡着了。醒来第一件事，是擦去信纸上被汗洇开的那一笔。", "first_letter"); });
                    return n;

                case "first_letter":
                    int amount = Has(s, "remit_reserved") ? 12 : Math.Min(8, s.money);
                    n = Scene(s, 3, "1907年正月", "新加坡 · 信局写批桌", "银钱与一页家书",
                        "你第一次以寄批人的身份坐在桌前。伙计再问一遍泉州府晋江县的村名、收款人姓名与门牌，才把款项和家书的去向记下。这一回随批寄出的家用是" + amount + "份；游戏中的份数只为记账，不对应历史币值。信纸很薄，你写完称呼，还不知该把什么留在上面。" + (Has(s, "overworked") ? "酸痛的手指按着纸角，想写一句平安，却想起昨夜没有吃完的饭。" : "你想起家中吃饭的桌子，忽然能听见阿满挪凳子的声音。") + "银钱有数，话却难定。",
                        "侨批把钱送进灶间，也把人的声音送回屋里。", "singapore");
                    Option(n, "honest", "如实写下工钱、疲惫与打算", "亲情 +7 · 信用 +4；让家里知道真实的近况", x => SendFirst(x, "honest", "把实话写进第一封批", "母亲、阿满：儿在新加坡已找到工。工钱来得不易，有时肩酸，有时想家，但饭尚能吃饱。随批家用请先补屋瓦、添药，不必替我攒着。往后若寄得少，会把缘故说清。请回批告知款项是否收妥，也写写你们真正缺什么。", 7, 4));
                    Option(n, "reassure", "只报平安，说一切都好", "亲情 +4 · 信用 −2；家里安心，却少知道一些实情", x => SendFirst(x, "reassure", "把难处留在纸外", "母亲、阿满：此间一切安好，吃住皆便，工作也顺手，不必挂念。随批家用收到后，请回批告知。儿在外会照顾自己，你们只管安心过日子。家中若有需要，写信来便是。", 4, -2));
                    Option(n, "school", "细问药钱，也问妹妹读书的事", "亲情 +8 · 信用 +1；把回信要回答的事一一写明", x => SendFirst(x, "ask_school", "让第一封批带去几个问题", "母亲、阿满：家用随批寄上。母亲的药是否续着？阿满可曾问到入学的门路，笔墨尚缺多少？请把各样用度写明，不必只说够用。阿满若能写，就亲自回我几个字；写错也无妨，我认得你的字。", 8, 1));
                    return n;

                case "reply":
                    string voice = School(s) ? "阿满的字挤在纸边，写她已会把一家人的名字连着写下来，想再添一本书。" : "信由村中识字的人代写，阿满只在末尾添了一小圈，说那是家门前的月亮。";
                    n = Scene(s, 3, "1907年春", "新加坡 · 合住屋门口", "回批到了",
                        "一封从晋江县来的回批递到你手上。母亲说款项已收到，屋瓦补好了两行，药铺还容她缓几日。" + voice + (Has(s, "reassure") ? "末尾添着一句：既然你一切都好，家中就不必为你留钱了。你盯着这行字，忽然有些不敢再看。" : "母亲叮嘱，往后不必每次都寄这么多，留一点吃饭歇脚。") + "纸上的每样需要都不算大，合在一起却超过你眼下的余裕。下一笔钱与下一句话，该先照应哪里？",
                        "回批是收款后的回信，也是家里给远行人的回声。", "singapore");
                    Option(n, "medicine", "先补母亲的药钱", "盘缠 −7 · 亲情 +10 · 体力 −2；家里的疼痛少一分", x => {
                        Apply(x, -7, -2, 10, 0, "medicine_sent");
                        Letter(x, "添寄药钱", "1907年春", "新加坡 → 厦门 → 泉州晋江县", "母亲：药请按时取。此笔家用专供药资，阿满替我记着取药日期。儿会量力做工，莫再把自己的疼痛写作无事。", "文生 敬上", false, 7);
                        Result(x, "药钱先寄走了。你多接了一点零活补贴自己，回信里却终于敢说：这一次，先把母亲的事办妥。", "records"); }, s.money >= 7, "需要 7 盘缠");
                    Option(n, "education", "寄去妹妹的书资", "盘缠 −7 · 亲情 +9 · 信用 +3；以后会收到她亲笔的回信", x => {
                        Apply(x, -7, 0, 9, 3, "school_supported");
                        Letter(x, "给阿满的书资", "1907年春", "新加坡 → 厦门 → 泉州晋江县", "阿满：这笔钱添书与笔墨。你想读书，就认真去读，不必把每个字都写成谢我。请替母亲认清药单，往后家中的事，也听听你自己的主意。", "兄 文生", false, 7);
                        Result(x, "你把书资交到信局。很久以后，阿满会记得的不是这笔钱有多少，而是你没有把她想读书的话当作孩子话。", "records"); }, s.money >= 7, "需要 7 盘缠");
                    Option(n, "details", "先回信，把家中用度和地址问清", "亲情 +4 · 信用 +5；暂不加寄，先把消息接稳", x => {
                        Apply(x, 0, 0, 4, 5, "careful_reply");
                        Letter(x, "请把细处告诉我", "1907年春", "新加坡 → 厦门 → 泉州晋江县", "母亲、阿满：家中药资、屋债和笔墨各需多少，请逐样告诉我。旧门牌若改过，或借住过亲戚家，也请写明。儿这一回先寄信，待工钱到手再安排家用，不让你们空等一个说不准的数。", "文生 敬上", false, 0);
                        Result(x, "你没把手头不够的事遮过去，也没仓促许下数目。信寄出时仍有惭愧，但往来的地址和家中的用度，从此有了可核对的细节。", "records"); });
                    return n;

                case "records":
                    n = Scene(s, 3, "1908年初", "新加坡 · 信局柜台", "留底的一张纸",
                        "日子渐渐有了秩序，你又领过一回工钱，准备寄出十份家用。伙计摊开批封，请你复核收款姓名、村名、款额与日期，提醒你把凭据收好。母亲在前一封回批中提到，修屋时曾暂住姨家，乡里也有人把旧门牌喊成另一个名字。你想到路途中那些经手的人，每人都只能看见一小段。若将来需要追查，这一刻留下的字，或许能把断开的路重新接上。",
                        "钱交出去之后，记录还要留在手边。", "singapore");
                    Option(n, "copies", "自己抄存姓名、款额与经手日期", "盘缠净变化 0 · 信用 +9；领薪寄款后，另花两份留底", x => {
                        Apply(x, 0, 0, 0, 9, "copies"); SendSecond(x);
                        Result(x, "你把底单与回批并在一起，又抄了一份新旧地址对照。两份纸不厚，夹进包里却让人踏实。", "missing"); });
                    Option(n, "shop_record", "请熟悉的店家代存凭据", "盘缠 +2 · 信用 +4 · 亲情 +2；有一处可回去查的柜台", x => {
                        Apply(x, 2, 0, 2, 4, "shop_record"); SendSecond(x);
                        Result(x, "店家在凭据旁记下你的住处，答应替你存放。你与他核过一遍款额，才放心离开柜台。", "missing"); });
                    Option(n, "reply_slip", "随批附纸，请家里签回收款细节", "盘缠 +1 · 亲情 +6；请回批写清收款日期与现住地址", x => {
                        Apply(x, 1, 0, 6, 0, "signed_reply"); SendSecond(x);
                        Result(x, "你另附一页纸，嘱家里收到后把日期、款额与现住地址写清。阿满认得的字越多，这张纸也越容易填完整。", "missing"); });
                    return n;

                case "missing":
                    n = Scene(s, 4, "1908年夏", "新加坡 · 雨后的街口", "迟迟没有回声",
                        "该到的回批迟了。又过些日子，家中托熟人捎来一句短话：只见前信，尚未收到新近那笔钱。你想起这批家用走过的柜台、码头与村路，心里先是一紧，随后才慢慢分清：眼下只能确定未收妥，还不能断定银钱遗失，更不能随口指认谁。" + (Has(s, "copies") ? "包里那份留底，日期与经手姓名仍很清楚。" : "你得从自己留下的凭据、人情和家信中，找一个可以问起的地方。") + "这一次，要先追哪一段？",
                        "一次迟到，考验的不只是钱，也考验人愿不愿意把话问明白。", "singapore");
                    Option(n, "desk", "拿凭据回信局，按经手次序查", "盘缠 −2 · 信用 +3；沿着记录寻找停下的那一站", x => {
                        Apply(x, -2, 0, 0, 3, "trace_desk"); Result(x, "你重新说明日期、款额与收款人，没有先责问。伙计把旧簿搬到桌上，愿意陪你从第一笔查起。", "trace_desk"); }, s.money >= 2, "需要 2 盘缠");
                    Option(n, "harbor", "去码头打听转交的人", "盘缠 −4 · 体力 −4；从货栈与同行者处找线索", x => {
                        Apply(x, -4, -4, 0, 0, "trace_harbor"); Result(x, Any(x, "ship_friend", "crew_help", "marked_cargo") ? "你在熟悉的货栈门前被认了出来。有人记得经手的那一批单据，肯陪你再问一处。" : "你从一家货栈问到另一家，鞋底沾满河边的泥。没多少人认得你，好在有人指了一个可问的名字。", "trace_harbor"); }, s.money >= 4, "需要 4 盘缠");
                    Option(n, "family", "先请家里复查门牌与代收人", "亲情 +2；让故乡那一端也加入查找", x => {
                        Apply(x, 0, 0, 2, 0, "trace_family"); Result(x, "你请家里先问姨家与村中代收的人，并把新旧门牌逐字写来。事情暂未解决，至少不再只有你一个人隔海猜测。", "trace_family"); });
                    return n;

                case "trace_desk":
                    n = Scene(s, 4, "1908年秋", "新加坡 · 信局后堂", "账簿里的停顿",
                        "旧簿摊开，一行行款项都有来处。你们发现，转往泉州的记录与收款人使用的门牌写法不尽相同，有一项仍等故乡那端核认。只有把日期、款额和旧地址拼在一起，才能说明等着的是哪一户。何师傅提醒你，查批需要往返时间；一时恼怒，既不能补齐记录，也不能替家里添米。" + (Any(s, "ledger", "copies", "verified", "shop_record") ? "你带来的线索足够落在纸上。" : "你发现自己当时记下的细节，还是少了一些。") + "你要如何把这件事接下去？",
                        "记录的用处，是让焦急终于有了可以落脚的地方。", "singapore");
                    Option(n, "reconcile", "逐项对照，发函请泉州那端核认", "盘缠 −1 · 亲情 +6 · 信用 +8；凭线索等一份明确答复", x => {
                        Apply(x, -1, 0, 6, 8, "resolved"); ResolutionLetter(x, "核认旧门牌后，家用已经领妥。并非银钱凭空不见，是姓名与住处未能及时对上。");
                        Result(x, "往返核认后，滞在转交处的款项找到了主人。回批终于写明收款日期。你松开握紧许久的手，也学会下回把旧称与新址一并写上。", "storms"); }, s.money >= 1 && (Any(s, "ledger", "copies", "verified", "shop_record") || s.trust >= 60), "需要 1 盘缠，以及留底记录、核对经验或信用达到 60");
                    Option(n, "replace", "先另寄十份，不能让家里断粮", "盘缠 −10 · 亲情 +8；家用先补上，原款继续待查", x => {
                        Apply(x, -10, 0, 8, 0, "replaced"); ReplacementLetter(x);
                        Result(x, "你另寄了一笔应急家用，旧款继续挂在待查簿上。家里不必立刻挨过这道难处，你自己却得多省几个月。", "storms"); }, s.money >= 10, "需要 10 盘缠");
                    Option(n, "complaint", "留下书面催查，暂且回去做工", "盘缠 +2 · 信用 −3；留下疑问，生活还要继续", x => {
                        Apply(x, 2, 0, 0, -3, "pending"); Result(x, "你把要求答复的话写下，领完短工的工钱才回住处。这封批仍未核结，往后作每一个决定时，它都会留在心里。", "storms"); });
                    return n;

                case "trace_harbor":
                    n = Scene(s, 4, "1908年秋", "新加坡 · 货栈与转运柜台", "去问记得的人",
                        "河边的货栈记着日期，人却未必总在原处。打听几回，你听说同一批的转交记录已送去核查，疑处仍在收款地址的旧称。若有人肯替你引见，便能把那一段经手记录接起来；若没有，只能再花钱请人往返探问。" + (Any(s, "ship_friend", "crew_help", "marked_cargo") ? "曾经受过你照应的人没有忘记你，叫你先坐下喝口水。" : "你没有熟人可托，站在门外等到卸货结束，才轮到说明来意。") + "潮水来回，等信的人却要按日子吃饭。",
                        "在人情里留下姓名，也是在远路上留一个问讯处。", "harbor");
                    Option(n, "witness", "请旧识引见，核清那一段转交", "亲情 +6 · 信用 +9；过去的相帮接成今天的线索", x => {
                        Apply(x, 0, 0, 6, 9, "resolved"); ResolutionLetter(x, "你托人核来的转交日期已对上，旧门牌也辨清了。家用领到了，请代我们谢过相帮的人。");
                        Result(x, "许多零散的话终于对上了日期，泉州那端据此核明住处，家里领到了款项。你记下帮过忙的人，知道这一回不能只说一句多谢。", "storms"); }, Any(s, "ship_friend", "crew_help", "marked_cargo"), "海上旧识、码头同伴或货单记录可以帮助引见");
                    Option(n, "runner", "请可靠的跑腿逐处核实", "盘缠 −6 · 亲情 +4 · 信用 +3；花钱把缺失的线索补齐", x => {
                        Apply(x, -6, 0, 4, 3, "resolved"); ResolutionLetter(x, "转交记录与现住地址已核清，十份家用收到。你请人查问花了钱，往后请把自己也顾好。");
                        Result(x, "你说明只按实际跑腿付费，请对方把每处答复抄回。几番往返后，地址得以确认，家用终于送到家里。", "storms"); }, s.money >= 6, "需要 6 盘缠");
                    Option(n, "wait", "留下姓名，先回去养好身体", "盘缠 +2 · 体力 +3；暂未查清，保存继续生活的力气", x => {
                        Apply(x, 2, 3, 0, 0, "pending"); Result(x, "你留下准确的住处和待查款额，回到能领短工的地方。迟批还在等消息，但你没有把最后一点力气也留在河边。", "storms"); });
                    return n;

                case "trace_family":
                    n = Scene(s, 4, "1908年秋", "新加坡 · 读信的窗边", "两个门牌，一户人家",
                        "家里的补信来了：修屋时暂住的姨家与原来的住处，乡里各有一种叫法。母亲以为送批的人都认得，便没在前封回批里写全。阿满问过代收的人，说若能把旧门牌、正式姓名和那一笔款额并写，便容易核认。你翻看自己的旧信：是否问过细节，是否请家中写清收款日期，此刻都在纸上留下不同的路。把事情说清，或是另拿一笔钱补过去，或把难处继续藏着，都要付出代价。",
                        "同一个家，也可能需要两边的人一起把地址写完整。", "singapore");
                    Option(n, "match", "拼起两边的信，把新旧住处说明白", "亲情 +7 · 信用 +5；曾写下的实话与细节能接住这一次", x => {
                        Apply(x, 0, 0, 7, 5, "resolved"); ResolutionLetter(x, "旧称、新址与你寄来的款额已核清。十份家用收到。这回我们知道了，不是熟人认得就可以少写几笔。");
                        Result(x, "你把两封信中的地址与用度逐项列清，请信局转告。后来寄到的回批上，收款日期写得清清楚楚，末尾郑重添了两个字：已妥。", "storms"); }, Any(s, "careful_reply", "signed_reply", "honest", "ask_school", "address"), "需要旧信中的细节、如实往来、签回记录或随身门牌");
                    Option(n, "replace", "另寄十份应急，原款继续查", "盘缠 −10 · 亲情 +10；承担眼前的家用缺口", x => {
                        Apply(x, -10, 0, 10, 0, "replaced"); ReplacementLetter(x);
                        Result(x, "你另寄家用，并把旧款仍在核查的事说清。母亲终于知道这次等待也压在你的肩上，回信叫你不要一个人扛。", "storms"); }, s.money >= 10, "需要 10 盘缠");
                    Option(n, "hide", "说钱还没寄，把这一回难处藏下", "亲情 −12 · 信用 −8；暂避追问，却让家里误会你的沉默", x => {
                        Apply(x, 0, 0, -12, -8, "concealed", "pending");
                        Letter(x, "没有说明的缘故", "1908年秋", "新加坡 → 厦门 → 泉州晋江县", "母亲：这回家用晚些再寄，先别等。儿一切尚好。", "文生", false, 0);
                        Result(x, "短短几句话寄出后，家里没有再催。你原想让他们少担忧，却也让他们少了一条能走近你的路。", "storms"); });
                    return n;

                case "storms":
                    n = Scene(s, 4, "1911年", "新加坡 · 多雨的街巷", "各有一笔旧账",
                        "几年里，有过工钱迟发，也有雨天停工，生活没有照你最初的算盘走。迟批一事" + (Has(s, "resolved") ? "总算有了明确回音，你把核认的记录夹在最前面。" : "仍留着一处空白，翻过旧信时总会碰见。") + (Debt(s) ? "这些年，日常工钱扣去吃住后，你陆续还过八份旅费；当年借下的二十份，如今尚欠十二份。亲人不催，也不能一直替你等。" : "当年担保的同乡需要人帮着整理往来账，你知道这不是可以永远拖着的人情。") + HealthLine(s) + "手头的余裕，只够先办一件要紧事。",
                        "把旧账认下来，才知道下一程究竟有多宽。", "singapore");
                    Option(n, "settle", Debt(s) ? "先还离乡时的借债" : "回报担保的同乡，帮他渡过忙时", Debt(s) ? "盘缠 −12 · 信用 +14；把借据上的名字结清" : "盘缠 −4 · 体力 −3 · 信用 +8；把曾承的情认真还上", x => {
                        if (Debt(x)) { Apply(x, -12, 0, 0, 14, "debt_paid"); Result(x, "你把还款交清，托家里收回借据。母亲在回批中写：纸已收好，你往后走路，心里可以轻一些。", "horizon"); }
                        else { Apply(x, -4, -3, 0, 8, "honor_guarantor"); Result(x, "你照看账目，也分担几笔急用。同乡说当年的介绍信没有写错人。人情仍在，只是不再单向压在你心上。", "horizon"); }
                    }, s.money >= (Debt(s) ? 12 : 4), "需要 " + (Debt(s) ? "12" : "4") + " 盘缠");
                    Option(n, "family_fund", "留一笔家中的急用钱", "盘缠 −8 · 亲情 +9 · 体力 +1；以后不必每次临急借钱", x => {
                        Apply(x, -8, 1, 9, 0, "family_fund");
                        Letter(x, "留作家中急用", "1911年", "新加坡 → 厦门 → 泉州晋江县", "母亲、阿满：此笔另存，药资、屋用有急便取，不必事事等我的下一封批。儿已学着量入为出，也请你们替自己作主。", "文生 敬上", false, 8);
                        Result(x, "这一回寄出去的，不只是眼前的用度，也是一点不必惊动远方的余裕。你想到家中或能睡得安稳些，自己也缓了一口气。", "horizon"); }, s.money >= 8, "需要 8 盘缠");
                    Option(n, "hard_work", "再接夜工，为下一程攒钱", "盘缠 +14 · 体力 −16 · 信用 +2；攒出余地，身体却更疲惫", x => {
                        Apply(x, 14, -16, 0, 2, "hard_years"); Result(x, "你又把夜晚换成工钱。手头终于宽了一些，早起时却要扶着床沿坐很久。以后再走远路，不能假装身体没有说话。", "horizon"); });
                    return n;

                case "horizon":
                    n = Scene(s, 5, "1913年春", "新加坡 · 旧住处", "下一张船票",
                        "母亲来信说，门槛已经换过一次，仍留着你小时候量身高的那道痕。" + (School(s) ? "阿满在信尾添了自己的打算：想继续读书，也想替邻里写信。" : "阿满已能独自料理许多家事，信里问你还记不记得离家那年的路。") + "与此同时，熟人递来更远埠头的做工机会，要不要再走一程，得由你自己答。" + Belonging(s) + "你可以买票回乡，也可以在这里稳住生计，或试着往更远处去；每一条路都将改变下一封批的落款。",
                        "归途有时是一段海路，有时是把一句话终于写完整。", "singapore");
                    Option(n, "return", "买票回厦门，再回晋江县", "盘缠 −8 · 体力 +5 · 亲情 +6；沿来路回到家门前", x => {
                        Apply(x, -8, 5, 6, 0, "return_ticket"); Result(x, "你写信告知归期，没有把日期许得太满。包袱重新收起，旧信放在最上层；这一回，银钱之外，还有你自己要过海。", "return_choice"); }, s.money >= 8, "需要 8 盘缠购买归程船票");
                    Option(n, "stay", "先留在新加坡，把日子安顿稳", "盘缠 +5 · 体力 −2；继续用侨批把两岸连起来", x => {
                        Apply(x, 5, -2, 0, 0, "staying"); Result(x, "你续下住处与手头的工作，准备给家里写一封有明确安排的信。暂不回去，也该让他们知道你为何留下。", "stay_choice"); });
                    Option(n, "farther", "去打听更远埠头的机会", "盘缠 −6 · 信用 +5；先核清条件，再决定是否启程", x => {
                        Apply(x, -6, 0, 0, 5, "farther"); Result(x, "你把工钱、住处和船费问清，请熟人留下书面地址。那片更远的海尚未成为承诺，只是一条终于看清代价的路。", "stay_choice"); }, s.money >= 6 && s.health >= 28, s.health < 28 ? "体力至少 28，才能筹划更远的海路" : "需要 6 盘缠");
                    return n;

                case "return_choice":
                    n = Scene(s, 5, "1913年夏", "泉州 · 晋江县村口", "走到门前",
                        "经厦门回到故乡，你在村口停了一会儿。门前的树高了，母亲的背弯了，阿满走出来时，你几乎认不出她已经长大的样子。" + (Has(s, "resolved") ? "那封写着已妥的回批被母亲收在匣中，连同这些年寄来的家用凭据。" : "旧匣里仍有一笔没有讲清的款项，家人未必忘了，只是在等你开口。") + (Debt(s) ? "离乡时的借据也还在，你不能把它留给一句回来就好。" : "有些旧账已清，有些话却仍要当面说。") + "走进这道门以前，你给往后的日子选了一个起头。",
                        "门开着。回家的人，还要学着重新住进彼此的生活。", "quanzhou");
                    Option(n, "home", "进门陪母亲，把往后慢慢说清", "以这些年留下的亲情、体力与旧账，接住归来的日子", x => {
                        bool warm = x.family >= 65 && x.health >= 45 && !Debt(x) && Has(x, "resolved");
                        Apply(x, 0, 3, 5, 0, "chose_home"); Result(x, warm ? "你跨过门槛，母亲把热汤推过来。许多话还有时间慢慢说，今晚先不必隔着海。" : "你进了门，也把这些年的疲惫与心事带进屋里。团聚不是把过去擦去，而是终于可以同家人慢慢说清。", warm ? "end_home_lamp" : "end_home_scar"); });
                    Option(n, "school", "把余钱交给阿满，让她继续读书", "盘缠 −8 · 亲情 +5；兑现曾经写进信里的学业约定", x => {
                        Apply(x, -8, 0, 5, 0, "school_future"); Result(x, "你请阿满自己把学资记进账簿，再写下她想读什么。她落笔很稳，这次不是替你代写家信。", "end_school_window"); }, School(s) && s.money >= 8, !School(s) ? "此前需关心或支持阿满读书" : "需要 8 盘缠");
                    Option(n, "confess", "先说出那笔旧款与藏下的难处", "信用 +6 · 亲情 +3；把未结的事当面接回来", x => {
                        Apply(x, 0, 0, 3, 6, "confessed"); Result(x, "你把迟批、借债或曾经没说出口的辛苦，一件件说给家里听。母亲没有替你免去责任，只把另一张凳子搬到桌边。", "end_unsent_letter"); }, Unsettled(s), "家用已核妥、旧债已清，也没有被藏下的迟批");
                    return n;

                case "stay_choice":
                    n = Scene(s, 5, "1913年夏", "新加坡 · 写信的桌前", "此后如何落款",
                        "你又坐到写批的桌前，称呼写得比初来时熟练，句子却更谨慎了。" + (Has(s, "route_courier") ? "信局愿让有信用的人独立照应往来批件，但你知道，一封未核清的批不能轻轻翻过。" : Has(s, "route_shop") ? "杂货铺仍有一盏灯等人点起，柜台后可以是一份雇工，也可以是更长久的生计。" : "码头的绳索又换了一批，你已经认得许多人，也认得自己力气的边界。") + (Has(s, "farther") ? "更远埠头的条件已经问清，要不要启程，只差你的决定。" : "更远处的机会尚未核实，眼前也有值得认真经营的生活。") + "你把新的住处写在纸上，准备让家里知道以后该往哪里回信。",
                        "漂泊不是没有地址，是愿意把新的地址告诉等你的人。", "singapore");
                    Option(n, "steady", Has(s, "route_courier") ? "接稳侨批往来，让每一封都有回音" : Has(s, "route_shop") ? "守住柜台，把生计与家信一起安顿" : "守住眼前的工，按实情定期寄批", "信用 +4；此前的工作、查批与偿债经历决定留下的路", x => {
                        string ending = "end_silver_thread";
                        if (Has(x, "route_courier") && x.trust >= 64 && Has(x, "resolved") && !Debt(x)) ending = "end_trusted_route";
                        else if (Has(x, "route_shop") && Has(x, "shop_partner") && x.trust >= 58 && !Debt(x)) ending = "end_shop_bridge";
                        Apply(x, 0, 0, 0, 4, "steady_future"); Result(x, "你把能够做到的事写清，把暂时做不到的事也写清。日子未必富足，但下一封批从哪里来、何时来，不再只靠一句放心。", ending); });
                    Option(n, "migrate", "启程去更远的埠头，再立一处地址", "盘缠 −8 · 体力 −5；带着来往记录去开新生活", x => {
                        Apply(x, -8, -5, 0, 0, "new_harbor"); Result(x, "你把新住处与托信人姓名寄回家，又给旧识留下转信的办法。船还没开，下一封回批的路已经先铺好了。", "end_new_harbor"); }, Has(s, "farther") && s.money >= 8 && s.health >= 28,
                        !Has(s, "farther") ? "先在上一程核清更远埠头的机会" : s.health < 28 ? "体力至少 28，才能继续远航" : "需要 8 盘缠");
                    Option(n, "school", "另留书资，支持阿满走自己的路", "盘缠 −8 · 亲情 +5；人在异乡，也能守住一个读书的约定", x => {
                        Apply(x, -8, 0, 5, 0, "school_future"); Result(x, "你在批中专列书资，写下请她自己决定的那一句话。阿满回来的字会越来越多，写出的路也不必与你相同。", "end_school_window"); }, School(s) && s.money >= 8, !School(s) ? "此前需关心或支持阿满读书" : "需要 8 盘缠");
                    return n;
            }
            return Ending(s);
        }

        private static void SendFirst(GameState s, string flag, string title, string body, int family, int trust)
        {
            int amount = Has(s, "remit_reserved") ? 12 : Math.Min(8, s.money);
            int cost = Has(s, "remit_reserved") ? 0 : amount;
            Apply(s, -cost, 0, family, trust, flag);
            Flag(s, "first_sent_" + amount);
            Letter(s, title, "1907年正月", "新加坡 → 厦门 → 泉州晋江县", body, "不孝儿 文生 敬上", false, amount);
            Result(s, "你再核一遍批封，将" + amount + "份家用与这页家书一并托出。银钱和文字走的是同一段归路，家中收到后，还会有一封回批往你这里来。", "reply");
        }
        private static void SendSecond(GameState s)
        {
            Flag(s, "second_sent");
            Letter(s, "第二年的家用", "1908年初", "新加坡 → 厦门 → 泉州晋江县", "母亲、阿满：随批家用十份，仍请按家中急需安排。收妥请回批告知。若住处变动，务必写清新旧门牌，儿在此间一切会量力而行。", "文生 敬上", false, 10);
        }
        private static void ResolutionLetter(GameState s, string detail)
        {
            Letter(s, "迟来的收妥回批", "1908年冬", "泉州晋江县 → 厦门 → 新加坡", detail + (School(s) ? "这回由阿满写明：旧屋与姨家是两处住址，同是一家人。母亲说请你安心，也记得吃饭。" : "母亲托人把收款日期与现住地址逐字写清，请你安心，也记得吃饭。"), School(s) ? "母亲口述，阿满执笔" : "母亲口述，邻人代笔", true, 0);
        }
        private static void ReplacementLetter(GameState s)
        {
            Letter(s, "先补眼前家用", "1908年秋", "新加坡 → 厦门 → 泉州晋江县", "母亲、阿满：另寄家用十份，先应眼前所需。原款还在查，我已留下款额与地址，请勿重复领款而不告知。若有新消息，两边都及时写信。儿手头会紧些，但这次把实情说明，免得彼此乱猜。", "文生 敬上", false, 10);
        }
        private static void OnEnter(GameState s)
        {
            if (s.nodeId == "reply" && !Has(s, "reply_received"))
            {
                Flag(s, "reply_received");
                string body = "文生：前批家用已收到，屋瓦补了两行。药铺容我再缓几日，你在外也要顾自己。";
                body += School(s) ? "阿满已去问学，笔墨还缺一些，她如今会把一家人的名字写在一起了。" : "阿满托我问，你那里夜里是不是也看得见月亮。";
                body += Has(s, "reassure") ? "你既说吃住顺心，我们也放心些，暂不另留你的用钱。" : "你信里写的难处，家里都记着。家用量力寄，切莫饿着、累着。";
                body += "修屋这阵暂住姨家，村里人都认得，往后有信仍要告诉你。";
                Letter(s, "母亲的第一封回批", "1907年春", "泉州晋江县 → 厦门 → 新加坡", body,
                    School(s) ? "母亲口述，阿满添字" : "母亲口述，邻人代笔", true, 0);
            }
            if (s.nodeId.StartsWith("end_", StringComparison.Ordinal) && !Has(s, "final_letter"))
            {
                Flag(s, "final_letter");
                FinalLetter(s);
            }
        }

        private static Node Ending(GameState s)
        {
            string title, body, quote;
            string art = Has(s, "return_ticket") ? "quanzhou" : "singapore";
            switch (s.nodeId)
            {
                case "end_home_lamp":
                    title = "家门灯火";
                    body = "你终于能在母亲添饭时伸手接过碗。旧债结清，迟批收妥，身体还撑得起故乡的日常；这并不意味着余生不再缺钱，只是接下来的难处可以坐在同一张桌边商量。阿满把你这些年的信按年月排好，第一封的纸已经发黄，末尾那句平安仍认得。夜里你给新加坡的旧识写信，告知归家地址。海路没有消失，它从讨生活的远方，变成你愿意记得、也有人记得你的地方。";
                    quote = "这一晚，家书可以放下，人就在灯下。"; break;
                case "end_home_scar":
                    title = "迟归的春天";
                    body = "回到家中，你才允许自己承认这些年确实很累。"
                        + (Has(s, "resolved") ? "迟批已收妥，凭据不必再翻来覆去地核问。" : "迟批仍有一笔未核清，你把旧信交给家人，准备接着查。")
                        + (Debt(s) ? "旅费尚欠十二份，也要重新安排归还。" : "离乡的借款已无未还之数。")
                        + (s.health < 48 ? "身体的亏欠却还在，母亲先替你铺好床，叫你明日再打算。" : "你接起家中力所能及的活，重新熟悉离开多年的日常。")
                        + (Has(s, "concealed") ? "曾藏下的实情不能只靠归来抹平，你从头说起，也听她们讲当年的误会。" : s.family < 70 ? "长久的分离让你们有些生疏，阿满把旧批拿来，你们试着重新说话。" : "阿满把旧批拿来，与你慢慢补上当年纸外的日子。")
                        + "归来不是把所有难处一笔勾销，是仍愿与亲人重新开始。";
                    quote = "门槛接住了你，也接住了你没能独自背完的事。"; break;
                case "end_school_window":
                    title = "灯下新字";
                    body = "阿满收下书资，先把数目写进账簿，再把自己想读的书列在旁边。后来她能替母亲辨认药单，也替不识字的邻里写信；她不再只替远行的兄长报告家中平安，还在信中说出自己的主意。" + (Has(s, "return_ticket") ? "你坐在同一盏灯下听她念，偶尔也有不认得的字要问她。" : "你仍在海那边做工，收到的回批却一封比一封写得开阔。") + "你寄回的钱有限，没能替一家人解决所有难题，却让一个曾蹲在门槛上的孩子，拥有了选择下一步的本领。";
                    quote = "银钱终会花尽，识得的字还会往前走。"; break;
                case "end_shop_bridge":
                    title = "两岸家书";
                    body = "陈掌柜让你独当一面，后来又与你议定一起经营。铺子没有一夜兴旺，仍要照看库存、赊账和每一日的饭钱；但你终于能把寄批的日子列进稳定的安排。柜台旁留出一小块干净地方，供同乡核对地址、读信写信。你知道店铺不是信局，款项仍交给可靠的经手处，却愿意把寄出以前的细节陪人看清。家里的回批来时，灯总还亮着。你在异乡有了可以守住的门，也没有把故乡留在门外。";
                    quote = "一盏柜台灯，照得见两岸人的字。"; break;
                case "end_trusted_route":
                    title = "递批人";
                    body = "何师傅终于把一段往来的事务交给你。你先问姓名、住处与款额，再问那些看似多余的旧称和别名；谁家迟了回批，也不让一句再等几日就算答复。那次查批留下的记录一直在手边，提醒你经手的钱各有用处，经手的信各有等候的人。你依旧给母亲与阿满寄家用，只是桌上还多了许多陌生人的牵挂。你没有因此成为传奇，名字却留在一些人的纸上，成为他们肯把一封批交出来的理由。";
                    quote = "你曾等过一封回批，于是懂得替别人接住回声。"; break;
                case "end_new_harbor":
                    title = "更远的潮";
                    body = "下一张船票把你带往更远的埠头，新的工作仍有未能预知的难处。不同的是，这回你没有只写一切安好：工钱如何算，病了找谁，回信先寄到哪里，都让家里知道。旧批、凭据与那件从故乡带来的东西一并收进包袱。" + (Unsettled(s) ? "未清的旧事也有记录跟随，你知道远行不能代替偿还与解释。" : "已经理清的往事给了你一点底气，却没有替你免去新路的代价。") + "岸线再次退远，你已学会让家人参与自己的选择，而不只在故事结束后听一句结果。";
                    quote = "海再远，先把回信的地址写清。"; art = "harbor"; break;
                case "end_unsent_letter":
                    title = "迟到的真话";
                    body = "你从匣里抽出一页纸，决定把仍压在心头的事逐项写清。"
                        + (Has(s, "resolved") ? "迟批已收妥，你先将这件查清的事好好记下。" : "迟批尚未核结，你列出已问过的人与仍缺的答复。")
                        + (Debt(s) ? "旅费还欠十二份，你与家里议定接下来怎样偿还。" : "借款已无未还之数，不必再让家人猜测。")
                        + (Has(s, "concealed") ? "你承认曾用短短几句话藏过实情，也听她们说那时的误会与委屈。" : "有些担心从前说得太少，如今你们把各自记得的日子慢慢接起来。")
                        + (s.health < 45 ? "疲惫的身体也不再被一句平安带过。" : "往后能承担多少，你愿意据实商量。")
                        + (s.family < 68 ? "亲近还须慢慢修补，至少这次没有谁先把话咽下去。" : "家人认真听完，把另一张凳子搬到桌边。")
                        + "这一页先递给身边的人，往后写给远方的说明，也不再独自作答。";
                    quote = "那封最难寄的信，先要递到身边人的手里。"; break;
                case "end_silver_thread":
                    title = "银信不断";
                    body = "你留下做熟悉的工，没有一间自己的铺，也没把异乡过成传说中的锦绣。往后寄回的家用有多有少，信上却逐渐有了明确的日期与缘故。" + (s.health < 40 ? "你削去一些夜工，承认身体需要休息；少寄一点，也好过让家里只收到无法兑现的保证。" : "你在工钱里留出吃饭与歇息的一份，不再把照顾自己当成对家人的亏欠。") + (Unsettled(s) ? "未了的款项与旧账仍须追问，你把它们留在每年的安排里。" : "往来的旧批收进小匣，平凡的履约一点点积成信任。") + "海两岸的日子依然辛苦，但彼此不再只靠猜测生活。";
                    quote = "一封接一封，普通人的日子也能越过海。"; break;
                default: throw new ArgumentException("Unknown story node: " + s.nodeId);
            }
            if (s.journey != null && s.journey.turns >= LifeJourney.MinTurns)
            {
                body += " 初到" + LifeJourney.DestinationName(s) + "时那几轮自己安排的日子，也留在生活簿里。";
                if (Has(s, "hardship_evidence")) body += "受了刁难后留下的见证与记录，教你往后不轻易放弃追问。";
                else if (Has(s, "hardship_mutual_aid")) body += "有人陪你另找住处与工作，让你知道求助不等于失去自己的主意。";
                else if (Has(s, "hardship_endure")) body += "曾经忍下的欺压与做过的急工，让你更清楚身体不能一直替沉默付账。";
            }
            Node n = Scene(s, 5, "1913年及以后", Has(s, "return_ticket") ? "泉州 · 晋江县榕溪村" : "南洋与泉州之间", title, body, quote, art);
            n.scene.isEnding = true;
            n.scene.endingId = s.nodeId.Substring(4);
            return n;
        }

        private static void FinalLetter(GameState s)
        {
            string body, signature = "文生";
            string route = Has(s, "return_ticket") ? "泉州晋江县 → 新加坡" : "南洋 → 厦门 → 泉州晋江县";
            string title = "此后的一封信";
            switch (s.nodeId)
            {
                case "end_home_lamp":
                    title = "报归家平安";
                    body = "诸位旧识：我已由厦门回到晋江县，母亲与阿满俱安。旧日照应，逐样记在心里。往来款项核清，归途平顺，如今能坐在家中写这一封信，才知从前每封回批等得多重。以后来信请寄榕溪村陈家，若有需我在乡里核问之事，也请写明。"; break;
                case "end_home_scar":
                    title = "把归来后的打算写清";
                    body = "新加坡旧识：我已归家，接下来的日子要慢慢安排。"
                        + (Has(s, "resolved") ? "迟批款项已收妥，相关凭据也已留好。" : "迟批尚未核清，我会照旧簿继续问，请有消息便告知。")
                        + (Debt(s) ? "旅费尚欠十二份，已与家里重新列过还款次序。" : "离乡借款已无未还之数。")
                        + (s.health < 48 ? "身体仍须休养，往后不再拿逞强作平安。" : "身体尚能应付日常，我会量力接活。")
                        + (Has(s, "concealed") ? "从前藏下的实情已向家里说明，误会还须慢慢解开。" : s.family < 70 ? "久别之后尚有生疏，我会多听母亲与阿满说话。" : "母亲与阿满都在身边，我们会一起打算。")
                        + "以后请按此址回信。"; break;
                case "end_school_window":
                    title = "阿满写给哥哥";
                    body = "哥哥：书资收妥，我已把数目记清。昨日替母亲认药单，又替邻家写了一封问安信；写到想家二字，才知道你以前为何写得那么慢。我还想继续读下去，也会把自己的打算告诉你。你寄来的钱我会慎用，你留在身边吃饭歇息的钱，也请不要再省。";
                    signature = "妹 阿满 亲笔"; route = Has(s, "return_ticket") ? "榕溪村 · 灯下手递" : "泉州晋江县 → 厦门 → 新加坡"; break;
                case "end_shop_bridge":
                    title = "柜台亮灯以后";
                    body = "母亲、阿满：我如今能独自照看铺中往来，生计渐有次序，仍须谨慎。柜台旁留一角替乡人读信，我不代信局收款，只帮他们把姓名住处看清。以后家用按实收安排，多少会写明，急事请照新住处来信。我在这里有一扇门，每晚关门时，总想起家中的门槛。"; break;
                case "end_trusted_route":
                    title = "递批人的家批";
                    body = "母亲、阿满：师傅让我独自照应一段往来。我记得家里等迟批那一回，故每件都核清姓名、款额、住处，经手有记，收妥有回。以后我会接触更多人的家事，也更懂你们每一封回批的分量。家用仍量力寄，请写你们真正的近况，不必只为我安心而说一切都好。"; break;
                case "end_new_harbor":
                    title = "下一处回信地址";
                    body = "母亲、阿满：我准备去更远处做工，工钱、住处与托信的人已问清。这一次不瞒你们：新路仍有未知，身体也须照顾。请暂把回批寄原处转交，落脚后我即发新址；未收到我的新信前，不要轻信旁人代我添要银钱。旧事未妥的，我会继续处理，走远不是忘记。"; break;
                case "end_unsent_letter":
                    title = "终于没有省去的那一页";
                    body = "母亲、阿满：这一次，我愿把仍须承担的事逐项说明。"
                        + (Has(s, "resolved") ? "迟批已收妥，这件事不再让你们悬心。" : "迟批仍待核认，已查到哪里、还须问谁，都写在旧簿旁。")
                        + (Debt(s) ? "借下的旅费已还八份，尚欠十二份，我会与你们商量还款次序。" : "旅费已无未还之数，不必为此再留钱。")
                        + (Has(s, "concealed") ? "对不起，从前怕你们担心，反让你们猜过我的沉默。藏下的实情，今后不再省去。" : "从前没来得及细说的打算，今后我愿多讲，也愿听你们怎样想。")
                        + (s.health < 45 ? "身体的疲惫我也认下，先休养，再量力做事。" : "往后做多少、能还多少，都据实写清。")
                        + (s.family < 68 ? "有些误会不能一日消去，我会耐心把话说完、听完。" : "能坐在一起商量，已比独自担心好得多。")
                        + "这一页先交到你们手中，往后给远方的说明，也请你们一起看过。";
                    route = "榕溪村 · 家中手递"; break;
                default:
                    title = "照实寄来的平常日子";
                    body = "母亲、阿满：这一季工钱有多有少，我把自己的饭钱、住处与休息先留妥，再安排家用。没有大富贵，也不再把做不到的事写成保证。旧账与待问的款项，我都记在簿上。请告诉我你们真实的需要，也写些不为要钱的小事；屋前的树、饭后的闲话，我都想听。"; break;
            }
            Letter(s, title, "1913年夏", route, body, signature, s.nodeId == "end_school_window", 0);
        }
    }
}
