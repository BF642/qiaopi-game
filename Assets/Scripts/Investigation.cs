using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class InquiryState
    {
        public string route = "", stage = "";
        public int evidenceMask;
        public int amount = -1, date = -1, address = -1;
        public bool helpUsed;
        public string helpRoute = "", helpChoice = "";
        public int helpTurn;
    }

    public sealed class InquiryEvidence
    {
        public string id, title, place, body;
    }

    // Authored fictional records, not a reconstruction of a named historical case.
    // Only explicit runtime enrolment adds this layer. Reading a scene or an old
    // save never starts an investigation, changes a letter, or grants a reward.
    public static class Investigation
    {
        public const int CurrentVersion = 1;
        static readonly string[] Fields = { "amount", "date", "address" };

        public static bool IsTraceNode(string node)
        { return node == "trace_desk" || node == "trace_harbor" || node == "trace_family"; }
        public static bool IsEnrolled(GameState s)
        { return s != null && s.inquiryVersion == CurrentVersion && s.inquiry != null; }
        public static bool IsCurrent(GameState s)
        { return IsEnrolled(s) && s.nodeId == s.inquiry.route; }
        public static bool IsComplete(GameState s)
        { return IsEnrolled(s) && s.inquiry.stage == "complete"; }
        public static bool IsActive(GameState s)
        { return IsCurrent(s) && !s.awaitingContinue && !IsComplete(s); }
        public static bool CanUseHelp(GameState s)
        { return IsActive(s) && !s.inquiry.helpUsed && s.inquiry.helpTurn > 0; }

        public static bool Enroll(GameState s)
        {
            if (s == null || s.inquiryVersion != 0 || !IsTraceNode(s.nodeId) || s.awaitingContinue || !StoryEngine.Validate(s)) return false;
            var next = new InquiryState { route = s.nodeId, stage = "collect" };
            WorkEventRecord memory = HelpSource(s);
            if (memory != null) { next.helpRoute = memory.route; next.helpChoice = memory.choice; next.helpTurn = memory.turn; }
            s.inquiry = next; s.inquiryVersion = CurrentVersion;
            return true;
        }

        // Revisiting a physical record is useful: repeat reads succeed but never
        // advance anything twice, so the player can check their own deductions.
        public static bool ReadEvidence(GameState s, int index, out string notice)
        {
            notice = "";
            if (!IsCurrent(s) || s.awaitingContinue || index < 0 || index > 2 || !StoryEngine.Validate(s)) return false;
            notice = Evidence(s, index).body;
            if (IsComplete(s)) return true;
            s.inquiry.evidenceMask |= 1 << index;
            if (s.inquiry.evidenceMask == 7) s.inquiry.stage = "verify";
            return true;
        }

        public static InquiryEvidence Evidence(GameState s, int index)
        {
            if (!IsEnrolled(s) || index < 0 || index > 2) return null;
            string route = s.inquiry.route;
            InquiryEvidence item;
            if (route == "trace_desk") {
                if (index == 0) item = E("counter", "交寄底单", "批局柜台", "留底记着：陈文生，正月十二日交寄，家用十份。旁边八份是另一户的款项，不能因姓名相近便并在一笔里。伙计用纸条压住相邻的一行，请你连寄款人和收款住处一同看；这张底单证明交寄，还不能证明家中收到了钱。");
                else if (index == 1) item = E("archive", "转交簿页", "后堂档案架", "同一笔十份家用，正月十四日交给往泉州的经手人。十二日是交寄日期，十四日才是这一站转交日期；收讫一栏仍空着。何师傅让你把经手那一日抄在旁边，别替尚未答复的泉州收款人填上收讫。纸上有一道去向，仍要接下一道回音。");
                else item = E("address", "门牌补注", "侧桌核址处", "泉州补注写着：陈家原住东井巷十七号，修屋时暂住榕树巷三号姨家。三字后没有十字。两处都要写全，不能只留下旧称。补注末尾注明同为文生家人，由妻子托人抄送，尚请局里核对。门牌相合，是找到人家的线索，还不算领款凭据。");
            } else if (route == "trace_harbor") {
                if (index == 0) item = E("warehouse", "货栈副单", "河埠货栈", "货栈留存的转批副单记着文生家用十份。十二份是同日另一人的寄款，不能把同船的银信当成同一户。看栈的人翻开装订线，指给你看两笔各有自己的行次。副单只记货栈见过的交接，不替远在泉州的家人签收；你得继续问经手日期。");
                else if (index == 1) item = E("handover", "经手人的记日", "转运棚", "经手人指着簿页：你是十二日交到局里，十四日这笔十份家用才到我手上。先后两道日期都对，查这一站时要抄转交的那一道。他不肯凭记性说钱已经到了谁手里，只把自己见过的那一页移到亮处，让你照着原有笔迹抄清。");
                else item = E("forward", "泉州来函摘录", "河埠问讯处", "来函说陈家修屋暂住姨家，旧址东井巷十七号，新住榕树巷三号。并未收讫。经手人请你把两个地址连着写，等泉州那端核本人。他把来函折痕摊平，又请你留一份抄件：就算熟人能指路，也不能省掉收款人的核认，更不能先写成已经领妥。");
            } else {
                if (index == 0) item = E("letters", "家信里的款额", "侨居写批桌", "妻子把你的前信抄在补信旁：正月十二日已寄十份家用。母亲说这十份尚未领妥。信中八份修屋钱是另一笔用度，不是这回待查的寄款。她又写，日用先靠针线活和邻里周转，你不要因为着急，把另一笔家用也认作丢失。信末仍等你把查到的去向逐项说清。");
                else if (index == 1) item = E("copy", "信局提供的日期副本", "街坊代写处", "阿满请你向局里要来副本：正月十二日交寄，正月十四日转交，收讫日期未记。她用笔圈出十四日，说查经手的那一站，就先核这一日。她没在空白处添字，只在信末问下一站是谁接手：我们先把已经知道的写实，未知的留待答复，不要拿猜测补齐一张纸。");
                else item = E("homes", "妻子抄的两处住址", "家乡来信柜", "妻子写：家里旧称东井巷十七号；修屋时，我们暂住榕树巷三号姨家。三号不是三十号。请把这两处和我们的正式姓名一并告知局里。她请母亲复念一遍门牌，再托阿满抄清；旁人说认得陈家，也不能替一家人省去这些字。信末又问，原款还等哪一处答复。");
            }
            item.body = LifeJourney.Localize(s, item.body);
            return item;
        }

        public static List<ChoiceData> Choices(GameState s, string field)
        {
            var items = new List<ChoiceData>();
            if (!IsEnrolled(s) || Array.IndexOf(Fields, field) < 0) return items;
            string[] labels = field == "amount" ? new[] { "八份", "十份", "十二份" }
                : field == "date" ? new[] { "正月十二日 · 交寄", "尚未记录 · 收讫", "正月十四日 · 转交" }
                : new[] { "东井巷十七号 → 榕树巷三号（姨家）", "东井巷十七号 → 榕树巷三十号", "南街三号 → 北巷七号" };
            for (int i = 0; i < labels.Length; i++) {
                bool eliminated = s.inquiry.helpUsed && field == HelpField(s) && i == HelpEliminated(s);
                items.Add(new ChoiceData { id = i.ToString(), title = labels[i], hint = "", enabled = !eliminated,
                    unavailableReason = eliminated ? "相帮的人已用留存记录排除这一项" : "" });
            }
            return items;
        }

        public static bool Select(GameState s, string field, int index, out string notice)
        {
            notice = "";
            if (!IsActive(s) || s.inquiry.stage != "verify" || !StoryEngine.Validate(s)) return false;
            ChoiceData option = Choices(s, field).Find(c => c.id == index.ToString() && c.enabled);
            if (option == null) return false;
            if (field == "amount") s.inquiry.amount = index;
            else if (field == "date") s.inquiry.date = index;
            else s.inquiry.address = index;
            notice = "已记下：" + option.title + "。提交前仍可对照证据修改。";
            return true;
        }

        public static bool Submit(GameState s, out string notice)
        {
            notice = "";
            if (!IsActive(s) || !StoryEngine.Validate(s)) return false;
            InquiryState i = s.inquiry;
            if (i.evidenceMask != 7) { notice = "还有未读的凭据，先把三处材料看全。"; return false; }
            if (i.amount < 0 || i.date < 0 || i.address < 0) { notice = "请把款额、这一站的转交日期和新旧门牌都填上。"; return false; }
            if (i.amount != 1) { notice = "款额还对不上。请区分这笔家用与材料里的另一笔钱，再核留底或前信。"; return false; }
            if (i.date != 2) { notice = "日期还对不上。交寄、转交、收讫是三回事；这次要核的是经手人接到银信的日期。"; return false; }
            if (i.address != 0) { notice = "门牌还对不上。请再看泉州补注，分清旧宅、暂住处与三号、三十号。"; return false; }
            i.stage = "complete";
            notice = "查批材料已对齐：十份家用、正月十四日转交、旧宅东井巷十七号与暂住榕树巷三号。家中尚未收讫，接下来决定怎样请两端核认、补寄或继续催查。";
            return true;
        }

        public static bool UseHelp(GameState s, out string notice)
        {
            notice = "";
            if (!CanUseHelp(s) || !StoryEngine.Validate(s)) return false;
            notice = HelpText(s); s.inquiry.helpUsed = true;
            // A contradicted guess is cleared, never silently replaced by the
            // right answer. The player still chooses and submits all three fields.
            string field = HelpField(s); int eliminated = HelpEliminated(s);
            if (field == "amount" && s.inquiry.amount == eliminated) s.inquiry.amount = -1;
            if (field == "date" && s.inquiry.date == eliminated) s.inquiry.date = -1;
            if (field == "address" && s.inquiry.address == eliminated) s.inquiry.address = -1;
            return true;
        }

        public static string HelpText(GameState s)
        {
            if (!IsEnrolled(s) || s.inquiry.helpTurn == 0) return "";
            string choice = s.inquiry.helpChoice;
            if (s.inquiry.helpRoute == "dock") return choice == "pair"
                ? "工友说：那场雨你分我两份工钱，咱们各接一份担子，我记着。我陪你核这一页经手簿：收讫还没填，别把空着的收讫栏当成转交日期。副单的去处，我也愿写名作证。"
                : "新同工说：你曾慢下来教我认纸货和布匹的签，我如今认得这些记号。我帮你找出经手簿：收讫仍空着，要查的是转交日期。认清的这一页，我愿随你去说明。";
            if (s.inquiry.helpRoute == "shop") return choice == "credit"
                ? "掌柜说：那回收四、欠二，你分开记得清楚，我敢把留底交你对照。这回十二份是旁人的寄款，不能并到你家账上。需要发函时，我把副页出处注明。"
                : "掌柜说：那笔实收六、尚欠三的账，你没有写成九份现钱。这次我取出留存副页陪你核：十二份属于另一户。副页从哪本簿抄来，我会一并说明。";
            return choice == "trace"
                ? "何师傅说：阿福搬家那次，你绕到南街重新核过本人。这回也别套用旧活的门牌：南街三号、北巷七号都不是你家。泉州补注我陪你逐字校，再具名说明来源。"
                : "何师傅说：上回邻人愿意代收，你仍找到阿福本人取回执。这回南街三号、北巷七号也是旁人的地址。你家两处住址另有补注，我陪你校清再发问。";
        }

        public static string Summary(GameState s)
        {
            if (!IsEnrolled(s)) return "";
            if (IsComplete(s)) return "三处材料已对齐 · 请选择后续核认办法";
            int count = 0; for (int j = 0; j < 3; j++) if ((s.inquiry.evidenceMask & (1 << j)) != 0) count++;
            return count < 3 ? "已看 " + count + "/3 处凭据 · 查款额、转交日与门牌" : "材料已齐 · 对照三项后交回核认";
        }

        public static bool HasCompletedHelp(GameState s)
        { return IsCurrent(s) && IsComplete(s) && s.inquiry.helpUsed; }

        public static bool Validate(GameState s)
        {
            if (s == null) return false;
            InquiryState i = s.inquiry;
            if (s.inquiryVersion == 0) return i == null || (string.IsNullOrEmpty(i.route) && string.IsNullOrEmpty(i.stage) && i.evidenceMask == 0 &&
                (i.amount == -1 || i.amount == 0) && (i.date == -1 || i.date == 0) && (i.address == -1 || i.address == 0) &&
                !i.helpUsed && string.IsNullOrEmpty(i.helpRoute) && string.IsNullOrEmpty(i.helpChoice) && i.helpTurn == 0);
            if (s.inquiryVersion != CurrentVersion || i == null || !IsTraceNode(i.route) || i.evidenceMask < 0 || i.evidenceMask > 7 ||
                i.amount < -1 || i.amount > 2 || i.date < -1 || i.date > 2 || i.address < -1 || i.address > 2) return false;
            if (i.stage == "collect") {
                if (i.evidenceMask == 7 || i.amount != -1 || i.date != -1 || i.address != -1) return false;
            } else if (i.stage == "verify") { if (i.evidenceMask != 7) return false; }
            else if (i.stage == "complete") { if (i.evidenceMask != 7 || i.amount != 1 || i.date != 2 || i.address != 0) return false; }
            else return false;
            if (i.stage != "complete" && (s.nodeId != i.route || s.awaitingContinue)) return false;
            if (IsTraceNode(s.nodeId) && s.nodeId != i.route) return false;
            if (i.helpTurn == 0) return !i.helpUsed && string.IsNullOrEmpty(i.helpRoute) && string.IsNullOrEmpty(i.helpChoice);
            if (i.helpTurn < 1 || s.workMemory == null || s.workMemory.version != WorkEvents.CurrentVersion || s.workMemory.records == null) return false;
            WorkEventRecord source = s.workMemory.records.Find(r => Eligible(r) && r.route == i.helpRoute && r.choice == i.helpChoice && r.turn == i.helpTurn);
            if (source == null) return false;
            if (i.helpUsed) {
                string field = HelpField(s); int eliminated = HelpEliminated(s);
                if (field == "amount" && i.amount == eliminated || field == "date" && i.date == eliminated || field == "address" && i.address == eliminated) return false;
            }
            return true;
        }

        static string HelpField(GameState s)
        { return s.inquiry.helpRoute == "dock" ? "date" : s.inquiry.helpRoute == "shop" ? "amount" : "address"; }
        static int HelpEliminated(GameState s)
        { return s.inquiry.helpRoute == "dock" ? 1 : 2; }
        static WorkEventRecord HelpSource(GameState s)
        {
            if (s.workMemory == null || s.workMemory.version != WorkEvents.CurrentVersion || s.workMemory.records == null) return null;
            string preferred = s.nodeId == "trace_harbor" ? "dock" : s.nodeId == "trace_desk" ? "shop" : "courier";
            WorkEventRecord fallback = null;
            for (int j = s.workMemory.records.Count - 1; j >= 0; j--) {
                WorkEventRecord r = s.workMemory.records[j]; if (!Eligible(r)) continue;
                if (r.route == preferred) return r;
                if (fallback == null) fallback = r;
            }
            return fallback;
        }
        static bool Eligible(WorkEventRecord r)
        { return r != null && !r.returned && r.turn > 0 && ((r.route == "dock" && (r.level == 2 && r.choice == "pair" || r.level == 3 && r.choice == "teach")) ||
            (r.route == "shop" && (r.level == 2 && r.choice == "credit" || r.level == 3 && r.choice == "record")) ||
            (r.route == "courier" && (r.level == 2 && r.choice == "trace" || r.level == 3 && r.choice == "personal"))); }
        static InquiryEvidence E(string id, string title, string place, string body)
        { return new InquiryEvidence { id = id, title = title, place = place, body = body }; }
    }
}
