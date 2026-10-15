using System;

namespace Qiaopi
{
    [Serializable]
    public sealed class HomeChapterState
    {
        public string stage = "wife", purchase = "", word = "";
        public bool grainStored, mealShared;
    }

    // A short physical prologue, independent of StoryEngine's 17 decisions.
    // The scalar opt-in preserves older saves, including saves partway through peace.
    // Actions commit before their presentation and never grant repeatable resources.
    public static class HomeChapter
    {
        public const int CurrentVersion = 1;
        static readonly string[] Stages = { "wife", "market", "bring_home", "grain", "store", "lesson", "meal", "complete" };
        public static HomeChapterState Create() { return new HomeChapterState(); }
        public static bool IsEnrolled(GameState s)
        { return s != null && s.homeChapterVersion == CurrentVersion && s.homeChapter != null; }
        public static bool IsActive(GameState s)
        { return IsEnrolled(s) && s.nodeId == "peace" && !s.awaitingContinue && s.homeChapter.stage != "complete"; }
        public static int Progress(GameState s)
        { return IsEnrolled(s) ? Math.Max(0, Array.IndexOf(Stages, s.homeChapter.stage)) : 0; }
        public static bool HasMemory(GameState s, string memory)
        {
            if (!IsEnrolled(s) || !Validate(s)) return false;
            if (memory == "market") return Progress(s) >= 3 && !string.IsNullOrEmpty(s.homeChapter.purchase);
            if (memory == "grain") return s.homeChapter.grainStored;
            if (memory == "lesson") return !string.IsNullOrEmpty(s.homeChapter.word);
            if (memory == "meal") return s.homeChapter.mealShared;
            return false;
        }
        public static bool TryAct(GameState s, string action)
        {
            if (!IsActive(s) || !Validate(s)) return false;
            var h = s.homeChapter;
            switch (h.stage)
            {
                case "wife": if (action != "walk_market") return false; h.stage = "market"; return true;
                case "market":
                    if (action != "rice" && action != "tea") return false;
                    h.purchase = action; h.stage = "bring_home"; return true;
                case "bring_home": if (action != "put_basket") return false; h.stage = "grain"; return true;
                case "grain": if (action != "lift_grain") return false; h.stage = "store"; return true;
                case "store": if (action != "store_grain") return false; h.grainStored = true; h.stage = "lesson"; return true;
                case "lesson":
                    if (action != "home" && action != "return") return false;
                    h.word = action; h.stage = "meal"; return true;
                case "meal": if (action != "share_meal") return false; h.mealShared = true; h.stage = "complete"; return true;
                default: return false;
            }
        }
        public static bool Validate(GameState s)
        {
            if (s == null) return false;
            if (s.homeChapterVersion == 0) return true; // Ignore JsonUtility's materialized default object.
            if (!IsEnrolled(s)) return false;
            var h = s.homeChapter; int at = Array.IndexOf(Stages, h.stage);
            if (at < 0 || h.purchase == null || h.word == null) return false;
            if (at < 2 ? h.purchase != "" : h.purchase != "rice" && h.purchase != "tea") return false;
            if (at < 6 ? h.word != "" : h.word != "home" && h.word != "return") return false;
            if (h.grainStored != (at >= 5) || h.mealShared != (at == 7)) return false;
            return true;
        }
        public static string Objective(GameState s)
        {
            if (!IsActive(s)) return "到饭桌旁，听家人把今日的话说完。";
            switch (s.homeChapter.stage)
            {
                case "wife": return "到陈家院，和妻子商量今日要添的东西。";
                case "market": return "陪妻子到院门外的日用摊，挑一篮家用。";
                case "bring_home": return "把刚买的家用提回陈家院，放在灶边小案上。";
                case "grain": return "替母亲端起院里已经晒干的一簸箕谷子。";
                case "store": return "把谷子收进墙边米瓮，不必再跑第二趟。";
                case "lesson": return "到廊下的小书案，教阿满写一个她惦记的字。";
                default: return "回到饭桌旁，和家人一起把今日的饭端上桌。";
            }
        }
        public static string ShortObjective(GameState s)
        {
            switch (IsActive(s) ? s.homeChapter.stage : "")
            {
                case "wife": return "回院里找妻子";
                case "market": return "陪妻子挑一篮家用";
                case "bring_home": return "把家用提回灶边";
                case "grain": return "端起晒干的谷子";
                case "store": return "把谷子收进米瓮";
                case "lesson": return "陪阿满写一个字";
                case "meal": return "和家人一起端饭";
                default: return "听家人说今日的话";
            }
        }
    }
}
