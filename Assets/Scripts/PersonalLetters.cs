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

    // Letters stay inside the player's save. Reactions use recorded experiences,
    // intention and remittance; free-form text is never uploaded or interpreted.
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

        public static string RecipientName(string recipient) { return recipient == "wife" ? "妻子" : recipient == "sister" ? "阿满" : "母亲"; }
        public static string IntentName(string intent)
        {
            return intent == "truth" ? "如实说出难处" : intent == "study" ? "只谈读书打算" : "只报平安";
        }
        public static string IntentEffect(string intent)
        {
            if (intent == "truth") return "把已发生的工作与难处告诉家人，回批会回应这些事。";
            if (intent == "study") return "暂不说工作难处；附银作为阿满书资，回批回应读书打算。";
            return "暂不说工作难处；家人只按平安与家常回话。";
        }

        public static bool CanSend(GameState state, out string reason)
        {
            reason = "";
            if (state == null || !StoryEngine.Validate(state))
            { reason = "这份草稿暂时不能寄出，请重新打开侨批匣。"; return false; }
            if (!LifeJourney.HasReachedOverseas(state))
            { reason = "还未在海外落脚。可以先写草稿，抵埠后再托批局寄回泉州。"; return false; }
            if (FirstDayChapter.IsActive(state) && state.firstDay.stage != "letter")
            { reason = state.firstDay.letterSent ? "这封回批还在路上。新写的字先存作草稿，等这次往来结束后再交寄。" : "草稿会留着。先办完眼前的事，今晚到住处的写批桌再交寄。"; return false; }
            if (!FirstDayChapter.IsActive(state) && NanyangLife.IsBlocking(state) && state.nanyang.stage != "letter")
            { reason = state.nanyang.letterSent ? "这封批还在跨海往返。新写的字先留作草稿，等回批收妥后再寄。" : "草稿会留着。先把这次欠薪的应对办妥，再到写批桌慢慢交寄。"; return false; }
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
            int waiting = state.firstDayVersion == 1 && state.firstDay != null && state.firstDay.letterSent && !state.firstDay.replyCollected ? 1 : 0;
            waiting += NanyangLife.PendingReplyCount(state);
            if (state.letters.Count + waiting > 38 || state.flags.Count + flagsNeeded > 80)
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
            bool firstDayDelayed = FirstDayChapter.IsActive(state) && state.firstDay.stage == "letter";
            bool nanyangDelayed = !firstDayDelayed && NanyangLife.IsBlocking(state) && state.nanyang.stage == "letter";
            bool delayed = firstDayDelayed || nanyangDelayed;
            string year = firstDayDelayed ? FirstDayChapter.WorkDate : nanyangDelayed ? NanyangLife.WorkDate : StoryEngine.GetScene(state).year;
            var before = new SendSnapshot(state);
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
                body = ExperienceLetters.Reply(state, draft), signature = RecipientName(draft.recipient),
                incoming = true, amount = 0
            };
            state.money -= draft.amount;
            state.family = Math.Min(100, state.family + familyGain);
            AddFlag(state, "personal_letter_" + draft.intent);
            if (draft.intent == "truth") AddFlag(state, "honest");
            if (draft.intent == "study" && draft.amount > 0) AddFlag(state, "school_supported");
            state.letters.Add(outgoing);
            if (state.personalLetterTokens == null) state.personalLetterTokens = new List<string>();
            state.personalLetterTokens.Add(draft.token);
            state.personalLettersSent++;
            bool recorded = !delayed || (firstDayDelayed
                ? FirstDayChapter.RecordSentLetter(state, draft, reply)
                : NanyangLife.RecordSentLetter(state, draft, reply));
            if (!recorded) {
                before.Restore(state);
                result = "这封信还没有交寄成功，草稿与附银已保留。"; return false;
            }
            if (!delayed) state.letters.Add(reply);
            state.personalLetterDraft = new PersonalLetterDraft {
                token = Guid.NewGuid().ToString("N"), recipient = draft.recipient, intent = draft.intent
            };
            result = "这一封已随批寄出。附银 " + draft.amount + "，亲情 +" + familyGain +
                (firstDayDelayed ? "。今晚先睡下，跨海回批还需等数周。" : nanyangDelayed ? "。信已上路，数周后的泉州回批还待领取。" : "。数周后的回批也已收入侨批匣。");
            return true;
        }

        // Chapter RecordSentLetter methods validate first and never partially
        // mutate their chapter on failure. Restore every shared send field here,
        // including a legacy null token list, while retaining existing list identity.
        sealed class SendSnapshot
        {
            readonly int money, family, sent;
            readonly List<string> flagList, tokenList, flags, tokens;
            readonly List<LetterRecord> letterList, letters;
            readonly PersonalLetterDraft draft;
            public SendSnapshot(GameState s)
            {
                money=s.money; family=s.family; sent=s.personalLettersSent; draft=s.personalLetterDraft;
                flagList=s.flags; flags=new List<string>(s.flags);
                letterList=s.letters; letters=new List<LetterRecord>(s.letters);
                tokenList=s.personalLetterTokens; tokens=tokenList==null?null:new List<string>(tokenList);
            }
            public void Restore(GameState s)
            {
                s.money=money; s.family=family; s.personalLettersSent=sent; s.personalLetterDraft=draft;
                s.flags=flagList; flagList.Clear(); flagList.AddRange(flags);
                s.letters=letterList; letterList.Clear(); letterList.AddRange(letters);
                s.personalLetterTokens=tokenList;
                if(tokenList!=null){tokenList.Clear();tokenList.AddRange(tokens);}
            }
        }

        static void AddFlag(GameState state, string flag) { if (!state.flags.Contains(flag)) state.flags.Add(flag); }
        static bool KnownRecipient(string value) { return value == "mother" || value == "sister" || value == "wife"; }
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
