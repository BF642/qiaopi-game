using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class PersonalLetterDraft
    {
        public string token = "";
        public string recipient = "mother";
        public string intent = "reassure";
        public string body = "";
        public int amount;
    }

    // Letters stay inside the player's save. Reactions are authored for the selected
    // intention; the game neither uploads nor claims to interpret free-form text.
    public static class PersonalLetters
    {
        public const int MaxBodyCharacters = 1200;
        public const int MaxLetters = 3;
        public static readonly int[] Amounts = { 0, 2, 5, 8 };

        public static PersonalLetterDraft EnsureDraft(GameState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (state.personalLetterDraft == null) state.personalLetterDraft = new PersonalLetterDraft();
            if (state.personalLetterTokens == null) state.personalLetterTokens = new List<string>();
            if (string.IsNullOrEmpty(state.personalLetterDraft.token)) state.personalLetterDraft.token = Guid.NewGuid().ToString("N");
            return state.personalLetterDraft;
        }

        public static string RecipientName(string recipient) { return recipient == "sister" ? "阿满" : "母亲"; }
        public static string IntentName(string intent)
        {
            return intent == "truth" ? "如实说出难处" : intent == "study" ? "支持阿满读书" : "报平安、宽家心";
        }
        public static string IntentEffect(string intent)
        {
            if (intent == "truth") return "家人会认真回应你的难处；留下如实往来的记录。";
            if (intent == "study") return "家人会回应读书的约定；寄出书资会留下支持学业的记录。";
            return "家人会回报家中近况；这次往来以彼此安心为重。";
        }

        public static bool CanSend(GameState state, out string reason)
        {
            reason = "";
            if (state == null || !StoryEngine.Validate(state))
            { reason = "这份草稿暂时不能寄出，请重新打开侨批匣。"; return false; }
            if (!LifeJourney.HasReachedOverseas(state))
            { reason = "还未在海外落脚。可以先写草稿，抵埠后再托批局寄回泉州。"; return false; }
            if (state.nodeId == "return_choice" || state.nodeId == "end_home_lamp" || state.nodeId == "end_home_scar" ||
                (state.nodeId.StartsWith("end_", StringComparison.Ordinal) && state.flags.Contains("return_ticket")))
            { reason = "你已回到泉州，家人就在身边。这页可以留作草稿，不再当作跨海侨批寄送。"; return false; }
            if (state.personalLettersSent >= MaxLetters)
            { reason = "这一程的三次自写侨批都已寄出。仍可写下草稿，寄出的信和回批留在侨批匣里。"; return false; }
            var draft = state.personalLetterDraft;
            if (draft == null || string.IsNullOrEmpty(draft.token) || string.IsNullOrWhiteSpace(draft.body) || draft.body.Trim().Length < 2)
            { reason = "给家人写至少两个字，再把信封好。"; return false; }
            if (state.personalLetterTokens != null && state.personalLetterTokens.Contains(draft.token))
            { reason = "这一封已经寄出，请写一封新的。"; return false; }
            if (draft.amount > state.money)
            { reason = "盘缠不足，先减少附银，或去谋生后再寄。"; return false; }
            int flagsNeeded = state.flags.Contains("personal_letter_" + draft.intent) ? 0 : 1;
            if (draft.intent == "truth" && !state.flags.Contains("honest")) flagsNeeded++;
            if (draft.intent == "study" && draft.amount > 0 && !state.flags.Contains("school_supported")) flagsNeeded++;
            if (state.letters.Count > 38 || state.flags.Count + flagsNeeded > 80)
            { reason = "这份行旅记录已满，草稿仍会保留。"; return false; }
            return true;
        }

        public static bool TrySend(GameState state, string expectedToken, out string result)
        {
            // Validate every condition before touching money, flags, lists, or draft.
            if (!CanSend(state, out result)) return false;
            var draft = state.personalLetterDraft;
            if (string.IsNullOrEmpty(expectedToken) || expectedToken != draft.token)
            { result = "信封已变更；请检查眼前这封信后再寄。"; return false; }
            string place = LifeJourney.DestinationName(state);
            string year = StoryEngine.GetScene(state).year;
            int familyGain = draft.amount == 0 ? 1 : 2;
            var outgoing = new LetterRecord {
                title = "写给" + RecipientName(draft.recipient) + " · 自写侨批",
                date = year + " · 第 " + (state.personalLettersSent + 1) + " 次自写",
                route = place + " → 泉州 · " + IntentName(draft.intent) + " · 附银 " + draft.amount,
                body = draft.body, signature = "文生 手书", incoming = false, amount = draft.amount
            };
            var reply = new LetterRecord {
                title = RecipientName(draft.recipient) + "的回批 · 自写侨批回音",
                date = year + " · 数周后（叙事时间）",
                route = "泉州 → " + place,
                body = Reply(draft), signature = draft.recipient == "sister" ? "阿满" : "母亲",
                incoming = true, amount = 0
            };
            state.money -= draft.amount;
            state.family = Math.Min(100, state.family + familyGain);
            AddFlag(state, "personal_letter_" + draft.intent);
            if (draft.intent == "truth") AddFlag(state, "honest");
            if (draft.intent == "study" && draft.amount > 0) AddFlag(state, "school_supported");
            state.letters.Add(outgoing);
            state.letters.Add(reply);
            if (state.personalLetterTokens == null) state.personalLetterTokens = new List<string>();
            state.personalLetterTokens.Add(draft.token);
            state.personalLettersSent++;
            state.personalLetterDraft = new PersonalLetterDraft {
                token = Guid.NewGuid().ToString("N"), recipient = draft.recipient, intent = draft.intent
            };
            result = "这一封已随批寄出。附银 " + draft.amount + "，亲情 +" + familyGain + "。数周后的回批也已收入侨批匣。";
            return true;
        }

        static string Reply(PersonalLetterDraft draft)
        {
            string salutation = draft.recipient == "sister" ? "阿兄：\n" : "文生吾儿：\n";
            string text;
            if (draft.intent == "truth") text = draft.recipient == "sister"
                ? "你在信里说的难处，我与母亲商量过了。不要怕让我们担心，饭要吃、伤要养，受了委屈也要找可信的同乡。家里的事不是你一人扛。"
                : "你愿意把难处告诉家里，母亲心疼，也放心你没有一人硬撑。若工钱被扣，先把工账留好；身体要紧，不必为了多寄一点银，舍不得吃饭看病。";
            else if (draft.intent == "study") text = draft.recipient == "sister"
                ? "你在批里郑重提到我的读书事，我把这份约定记着了。每天学的几个字，我都写给母亲看。愿有一天，我能自己读懂账簿和来信，也能选择自己的路。"
                : "阿满知道你还惦记她读书，连晚饭后也要练几个字。家里会把学费另记一页，不让你在海外猜着急。她将来想做什么，也该听她自己说。";
            else text = draft.recipient == "sister"
                ? "收到你的平安批了，母亲在灯下看了两遍。家中小事照旧，雨后院里的树又抽了新叶。你多写几句日常，吃了什么、见了什么，我们就像也陪你走了一段。"
                : "平安批已到，家中都安。你从前坐的那张凳子还在，灯也照常点着。莫只记得寄银，吃饭、睡觉和身边的人，也写来让我们知道。";
            string money = draft.amount > 0
                ? "\n附来的 " + draft.amount + " 份银已收讫，" + (draft.intent == "study" ? "单列为阿满的书资，记在家用簿上。" : "已记入家用簿，请先顾好自己。")
                : "\n这次虽未附银，字已经到了。家书本身也能使人安心。";
            return salutation + text + money;
        }

        static void AddFlag(GameState state, string flag) { if (!state.flags.Contains(flag)) state.flags.Add(flag); }
        static bool KnownRecipient(string value) { return value == "mother" || value == "sister"; }
        static bool KnownIntent(string value) { return value == "truth" || value == "reassure" || value == "study"; }
        static bool ValidToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length != 32) return false;
            foreach (char c in token) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
        public static bool ValidateState(GameState state)
        {
            if (state == null || state.personalLettersSent < 0 || state.personalLettersSent > MaxLetters) return false;
            if (state.personalLetterTokens == null) { if (state.personalLettersSent != 0) return false; }
            else {
                if (state.personalLetterTokens.Count != state.personalLettersSent) return false;
                var seen = new HashSet<string>();
                foreach (string token in state.personalLetterTokens) if (!ValidToken(token) || !seen.Add(token)) return false;
            }
            var draft = state.personalLetterDraft;
            // Missing fields are a supported migration from saves before self-written letters.
            if (draft == null) return true;
            return (string.IsNullOrEmpty(draft.token) || ValidToken(draft.token)) &&
                KnownRecipient(draft.recipient) && KnownIntent(draft.intent) &&
                draft.body != null && draft.body.Length <= MaxBodyCharacters && Array.IndexOf(Amounts, draft.amount) >= 0;
        }
    }
}
