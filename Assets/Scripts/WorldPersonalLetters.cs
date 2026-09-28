using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool letterEditor;
        Vector2 personalLetterScroll;
        Vector2 personalLetterBodyScroll;
        bool personalLetterPreview;
        string personalLetterNotice = "", personalLetterSnapshot = "";
        float personalLetterSaveAt;
        bool personalLetterDirty;
        TouchScreenKeyboard personalLetterKeyboard;
        GUIStyle personalLetterArea;

        // While the letter is open, none of its typed letters become game shortcuts.
        bool PersonalLetterTyping { get { return letterEditor; } }

        void OpenPersonalLetter() { RequestWritingDesk(); }

        void BeginPersonalLetterAtDesk()
        {
            if (!NearWritingDesk) return;
            PersonalLetters.EnsureDraft(state);
            letterEditor = true; journal = map = help = gallery = fieldNote = lifePanel = dialogue = false;
            BeginWritingSeat();
            personalLetterPreview = false; personalLetterScroll = personalLetterBodyScroll = Vector2.zero; personalLetterNotice = "";
            personalLetterSnapshot = JsonUtility.ToJson(state.personalLetterDraft); personalLetterDirty = false;
            walkRoute.Clear(); ResetMobileControls(); CancelCameraPointer();
            Input.imeCompositionMode = IMECompositionMode.On; if (Event.current != null) GUI.FocusControl(null); Save();
        }

        void ClosePersonalLetter()
        {
            UpdatePersonalLetterKeyboard();
            if (personalLetterKeyboard != null) { personalLetterKeyboard.active = false; personalLetterKeyboard = null; }
            Save(); personalLetterDirty = false; letterEditor = false; if (Event.current != null) GUI.FocusControl(null);
            Input.imeCompositionMode = IMECompositionMode.Auto;
            EndWritingSeat();
        }

        void UpdatePersonalLetterKeyboard()
        {
            if (!letterEditor || state == null) return;
            var draft = PersonalLetters.EnsureDraft(state);
            if (personalLetterKeyboard != null) {
                string typed = personalLetterKeyboard.text ?? "";
                draft.body = typed.Length <= PersonalLetters.MaxBodyCharacters ? typed : typed.Substring(0, PersonalLetters.MaxBodyCharacters);
                if (personalLetterKeyboard.status != TouchScreenKeyboard.Status.Visible) personalLetterKeyboard = null;
            }
            string snapshot = JsonUtility.ToJson(draft);
            if (snapshot != personalLetterSnapshot) {
                personalLetterSnapshot = snapshot; personalLetterDirty = true; personalLetterSaveAt = Time.unscaledTime + .65f;
            }
            if (personalLetterDirty && Time.unscaledTime >= personalLetterSaveAt) { Save(); personalLetterDirty = false; }
        }

        void DrawPersonalLetter()
        {
            UpdatePersonalLetterKeyboard();
            var draft = PersonalLetters.EnsureDraft(state);
            Rect safe = MobileControls ? MobileUiBounds() : new Rect(0, 0, 1600, 1000);
            float panelWidth = Mathf.Clamp(safe.width * .66f, 1000, 1240);
            Rect panel = MobileControls ? new Rect(safe.xMax - 30 - panelWidth, 100, panelWidth, 860) : new Rect(580, 100, 980, 860);
            // The uncovered left side keeps the real desk and room in view while writing.
            float captionWidth = Mathf.Max(200, panel.x - safe.xMin - 76);
            HudText(new Rect(safe.xMin + 40, 117, captionWidth, 43), writingPlace == null ? "写批处" : writingPlace.title, 28, hudGold, true);
            HudText(new Rect(safe.xMin + 40, 163, captionWidth, 35), "落座 · 写给家里", 22, hudText);
            PaperPanel(panel); DrawIllustratedIcon(new Rect(panel.x + 28, panel.y + 23, 48, 48), "write");
            Label(new Rect(panel.x + 94, panel.y + 20, panel.width - 314, 51), personalLetterPreview ? "封批确认" : "写给泉州", 30, ink, true);
            if (LetterButton(new Rect(panel.xMax - 192, panel.y + 23, 164, 54), "存稿起身", false, 25)) { ClosePersonalLetter(); return; }
            Label(new Rect(panel.x + 30, panel.y + 85, panel.width - 60, 34), "已寄 " + state.personalLettersSent + " / 3 封 · 盘缠 " + state.money + " · 草稿自动保存", 23, sub);
            Rect content = new Rect(panel.x + 30, panel.y + 132, panel.width - 60, panel.height - 284);
            if (personalLetterPreview) DrawPersonalLetterReview(content, draft);
            else DrawPersonalLetterDraft(content, draft);
            string reason = "";
            bool canSend = !personalLetterPreview || PersonalLetters.CanSend(state, out reason);
            string notice = personalLetterNotice;
            if (string.IsNullOrEmpty(notice)) notice = !canSend ? reason : "原文仅存本机；回批依据所选心意与附银生成。";
            Label(new Rect(panel.x + 30, panel.yMax - 143, panel.width - 60, 62), notice, 23, sub);
            if (!personalLetterPreview) {
                if (LetterButton(new Rect(panel.xMax - 300, panel.yMax - 73, 270, 56), "检查信封", true, 25)) {
                    UpdatePersonalLetterKeyboard(); if (personalLetterKeyboard != null) { personalLetterKeyboard.active = false; personalLetterKeyboard = null; }
                    Save(); personalLetterPreview = true; personalLetterScroll = Vector2.zero; GUI.FocusControl(null);
                }
            } else {
                if (LetterButton(new Rect(panel.x + 30, panel.yMax - 73, 180, 56), "修改", false, 25)) { personalLetterPreview = false; personalLetterNotice = ""; personalLetterScroll = Vector2.zero; }
                bool enabled = GUI.enabled; GUI.enabled = enabled && canSend;
                if (LetterButton(new Rect(panel.xMax - 300, panel.yMax - 73, 270, 56), "寄出 · 附银 " + draft.amount, true, 25)) {
                    string result;
                    if (PersonalLetters.TrySend(state, draft.token, out result)) {
                        Save(); personalLetterDirty = false; ClosePersonalLetter(); journal = true; journalScroll = Vector2.zero; Toast(result);
                    } else personalLetterNotice = result;
                }
                GUI.enabled = enabled;
            }
        }

        void DrawPersonalLetterDraft(Rect viewport, PersonalLetterDraft draft)
        {
            // A row for each envelope field keeps the narrower sheet readable.
            float x = viewport.x, y = viewport.y, width = viewport.width;
            const float fieldWidth = 82, gap = 10, rowHeight = 54;
            float choicesX = x + fieldWidth, choicesWidth = width - fieldWidth;
            Label(new Rect(x, y + 8, 72, 40), "写给", 27, ink, true);
            if (LetterButton(new Rect(choicesX, y, 136, rowHeight), "母亲", draft.recipient == "mother", 26)) draft.recipient = "mother";
            if (LetterButton(new Rect(choicesX + 150, y, 176, rowHeight), "妹妹阿满", draft.recipient == "sister", 26)) draft.recipient = "sister";
            float amountWidth = (choicesWidth - gap * 3) / 4;
            Label(new Rect(x, y + 72, 72, 40), "附银", 27, ink, true);
            for (int i = 0; i < PersonalLetters.Amounts.Length; i++) {
                int amount = PersonalLetters.Amounts[i];
                if (LetterButton(new Rect(choicesX + i * (amountWidth + gap), y + 64, amountWidth, rowHeight), amount == 0 ? "不附银" : amount + " 盘缠", draft.amount == amount, 25)) draft.amount = amount;
            }
            Label(new Rect(x, y + 136, 72, 42), "心意", 27, ink, true);
            string[] intents = { "truth", "reassure", "study" }; float chipWidth = (choicesWidth - gap * 2) / 3;
            for (int i = 0; i < intents.Length; i++) if (LetterButton(new Rect(choicesX + i * (chipWidth + gap), y + 128, chipWidth, 58), PersonalLetters.IntentName(intents[i]), draft.intent == intents[i], 25)) draft.intent = intents[i];
            Label(new Rect(x, y + 195, width, 46), PersonalLetters.IntentEffect(draft.intent), 23, sub);
            Label(new Rect(x, y + 249, width - 280, 32), "正文", 26, ink, true);
            Label(new Rect(x + width - 280, y + 249, 280, 32), draft.body.Length + " / " + PersonalLetters.MaxBodyCharacters + " 字", 23, sub, false, TextAnchor.MiddleRight);
            Rect body = new Rect(x, y + 286, width, viewport.height - 286);
            Box(body, new Color(.98f, .95f, .87f, .40f)); Stroke(body, line);
            if (MobileControls && Application.isMobilePlatform) {
                string shown = string.IsNullOrEmpty(draft.body) ? "点此写信……" : draft.body;
                float textHeight = Mathf.Max(body.height - 24, Height(shown, 27, body.width - 58, false) + 28);
                GalleryTouchScroll(body, ref personalLetterBodyScroll, textHeight);
                personalLetterBodyScroll = GUI.BeginScrollView(new Rect(body.x + 5, body.y + 5, body.width - 10, body.height - 10), personalLetterBodyScroll, new Rect(0, 0, body.width - 33, textHeight), false, true);
                Label(new Rect(10, 9, body.width - 58, textHeight - 18), shown, 27, string.IsNullOrEmpty(draft.body) ? sub : ink);
                GUI.EndScrollView();
                if (GUI.Button(new Rect(body.x, body.y, body.width - 35, body.height), GUIContent.none, blank) && Time.frameCount - galleryDragFrame > 2) {
                    TouchScreenKeyboard.hideInput = false;
                    personalLetterKeyboard = TouchScreenKeyboard.Open(draft.body, TouchScreenKeyboardType.Default, true, true, false, false, "写给泉州家人", PersonalLetters.MaxBodyCharacters);
                }
            } else {
                if (personalLetterArea == null) {
                    personalLetterArea = new GUIStyle(GUI.skin.textArea) { font = font, fontSize = 27, wordWrap = true, richText = false, padding = new RectOffset(13, 13, 11, 11) };
                    personalLetterArea.normal.textColor = personalLetterArea.focused.textColor = personalLetterArea.active.textColor = ink;
                    personalLetterArea.normal.background = personalLetterArea.focused.background = personalLetterArea.active.background = null;
                }
                GUI.SetNextControlName("personal-letter-body");
                draft.body = GUI.TextArea(body, draft.body, PersonalLetters.MaxBodyCharacters, personalLetterArea);
            }
        }

        void DrawPersonalLetterReview(Rect viewport, PersonalLetterDraft draft)
        {
            float width = viewport.width - 28;
            float bodyHeight = Mathf.Max(210, Height(draft.body, 28, width - 46, true));
            float height = bodyHeight + 250;
            GalleryTouchScroll(viewport, ref personalLetterScroll, height);
            personalLetterScroll = GUI.BeginScrollView(viewport, personalLetterScroll, new Rect(0, 0, width, height), false, true);
            string origin = LifeJourney.HasReachedOverseas(state) ? LifeJourney.DestinationName(state) : "待抵埠后";
            Label(new Rect(0, 0, width, 45), origin + " → 泉州 · 收批人：" + PersonalLetters.RecipientName(draft.recipient), 29, ink, true);
            Label(new Rect(0, 58, width, 47), "心意：" + PersonalLetters.IntentName(draft.intent) + "    附银：" + draft.amount + "    寄出后盘缠：" + (state.money - draft.amount), 26, sub);
            Box(new Rect(0, 125, width, bodyHeight + 66), new Color(.98f, .95f, .87f, .40f));
            Label(new Rect(22, 148, width - 46, bodyHeight + 6), string.IsNullOrEmpty(draft.body) ? "（请先填写正文）" : draft.body, 28, ink, true);
            Label(new Rect(0, bodyHeight + 205, width, 40), "文生 手书 · 回批随后收入侨批匣", 23, sub, false, TextAnchor.MiddleRight);
            GUI.EndScrollView();
        }

        void PersonalJournal()
        {
            bool mobile = MobileControls;
            Rect safe = mobile ? MobileUiBounds() : new Rect(0, 0, 1600, 1000);
            Rect panel = mobile ? new Rect(safe.xMin + 30, 148, safe.width - 60, 814) : new Rect(125, 125, 1350, 820);
            Box(new Rect(safe.xMin - 100, -100, safe.width + 200, 1200), new Color(.08f, .16f, .13f, .34f));
            PaperPanel(panel); DrawIllustratedIcon(new Rect(panel.x + 30, panel.y + 23, 64, 64), "letter");
            Label(new Rect(panel.x + 112, panel.y + 25, panel.width - 560, 57), "侨批匣", mobile ? 36 : 34, ink, true);
            string writeLabel = state.personalLetterDraft != null && !string.IsNullOrEmpty(state.personalLetterDraft.body) ? "到桌前续写" : "去写批处";
            if (LetterButton(new Rect(panel.xMax - 426, panel.y + 23, 238, 65), writeLabel, true, 27)) { OpenPersonalLetter(); return; }
            if (LetterButton(new Rect(panel.xMax - 174, panel.y + 23, 142, 65), "收起", false, 26)) { journal = false; return; }
            string journeyNote = LifeJourney.HasReachedOverseas(state) ? "已寄自写侨批 " + state.personalLettersSent + " / " + PersonalLetters.MaxLetters + " 封" : "抵埠后可寄出 · 现在可存稿";
            Label(new Rect(panel.x + 33, panel.y + 107, panel.width - 66, 38), journeyNote, mobile ? 25 : 23, sub);
            Rect viewport = new Rect(panel.x + 32, panel.y + 163, panel.width - 64, panel.height - 195);
            if (state.letters.Count == 0) {
                Label(new Rect(viewport.x + 45, viewport.y + 85, viewport.width - 90, 130), "尚无侨批\n写给家里的第一句话，从这里开始。", mobile ? 31 : 29, sub, true, TextAnchor.MiddleCenter);
                if (LetterButton(new Rect(viewport.center.x - 175, viewport.y + 256, 350, 72), "到桌前写信", true, 28)) OpenPersonalLetter();
                return;
            }
            float width = viewport.width - 30, bodyWidth = width - 52;
            int bodySize = mobile ? 29 : 26, metaSize = mobile ? 24 : 21, titleSize = mobile ? 33 : 30;
            float total = 0;
            foreach (LetterRecord record in state.letters) total += PersonalJournalEntryHeight(record, bodyWidth, bodySize, metaSize, titleSize) + 25;
            GalleryTouchScroll(viewport, ref journalScroll, total);
            journalScroll = GUI.BeginScrollView(viewport, journalScroll, new Rect(0, 0, width, total), false, true);
            float y = 0;
            for (int i = state.letters.Count - 1; i >= 0; i--) {
                LetterRecord letter = state.letters[i];
                string metadata = letter.date + " · " + letter.route;
                float metaHeight = Height(metadata, metaSize, bodyWidth, false) + 6;
                float titleHeight = Height(letter.title, titleSize, bodyWidth, true) + 6;
                float bodyHeight = Height(letter.body, bodySize, bodyWidth, true) + 12;
                float height = PersonalJournalEntryHeight(letter, bodyWidth, bodySize, metaSize, titleSize);
                Box(new Rect(0, y, width, height), letter.incoming ? new Color(.92f, .89f, .79f, .34f) : new Color(.98f, .95f, .87f, .36f));
                Box(new Rect(0, y, 4, height), letter.incoming ? letterInk : letterSeal);
                Label(new Rect(26, y + 22, bodyWidth, metaHeight), metadata, metaSize, sub);
                float titleY = y + 22 + metaHeight + 14;
                Label(new Rect(26, titleY, bodyWidth, titleHeight), letter.title, titleSize, ink, true);
                float bodyY = titleY + titleHeight + 22;
                Label(new Rect(26, bodyY, bodyWidth, bodyHeight), letter.body, bodySize, ink, true);
                float signatureHeight = Height(letter.signature, metaSize + 2, bodyWidth, true) + 6;
                Label(new Rect(26, bodyY + bodyHeight + 20, bodyWidth, signatureHeight), letter.signature, metaSize + 2, sub, true, TextAnchor.UpperRight);
                y += height + 25;
            }
            GUI.EndScrollView();
        }

        float PersonalJournalEntryHeight(LetterRecord letter, float width, int bodySize, int metaSize, int titleSize)
        {
            return 22 + Height(letter.date + " · " + letter.route, metaSize, width, false) + 6 + 14
                + Height(letter.title, titleSize, width, true) + 6 + 22
                + Height(letter.body, bodySize, width, true) + 12 + 20
                + Height(letter.signature, metaSize + 2, width, true) + 6 + 24;
        }
    }
}
