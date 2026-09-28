using System;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class PersonalLetterChecks
    {
        public static void Run()
        {
            var state = StoryEngine.NewGame(); state.nodeId = "shore"; state.money = 16;
            var draft = PersonalLetters.EnsureDraft(state);
            const string words = "母亲：\n今夜风很大。我还记得泉州院里的桂树。\n愿平安。 <不是标签> \"原字保留\"";
            draft.body = words; draft.recipient = "sister"; draft.intent = "study"; draft.amount = 5;
            var restored = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
            Require(PersonalLetters.ValidateState(restored), "草稿存档可读回");
            Require(restored.personalLetterDraft.body == words && restored.personalLetterDraft.amount == 5 && restored.personalLetterDraft.token == draft.token, "换行、原文、附银和信封编号完整读回");
            string token = restored.personalLetterDraft.token, result;
            int letters = restored.letters.Count, family = restored.family;
            Require(PersonalLetters.TrySend(restored, token, out result), "合法侨批可寄出：" + result);
            Require(restored.money == 11 && restored.family == family + 2, "附银恰好扣除一次，亲情适度增加");
            Require(restored.letters.Count == letters + 2 && restored.letters[letters].body == words && restored.letters[letters].amount == 5, "玩家原文完整进入侨批匣并收到回批");
            Require(restored.letters[letters + 1].incoming && restored.letters[letters + 1].body.Contains("书资") && restored.flags.Contains("school_supported"), "明确读书心意产生对应回音和后续记录");
            string after = JsonUtility.ToJson(restored);
            Require(!PersonalLetters.TrySend(restored, token, out result) && after == JsonUtility.ToJson(restored), "重复封批不能扣款或重复生成信件");
            restored.personalLetterDraft.body = "再写一封";
            string current = JsonUtility.ToJson(restored);
            Require(!PersonalLetters.TrySend(restored, token, out result) && current == JsonUtility.ToJson(restored), "旧信封编号不能发送新草稿");
            restored.money = 0; restored.personalLetterDraft.amount = 8;
            current = JsonUtility.ToJson(restored);
            Require(!PersonalLetters.TrySend(restored, restored.personalLetterDraft.token, out result) && current == JsonUtility.ToJson(restored), "盘缠不足不修改任何存档字段");
            restored.personalLetterDraft.amount = 0; restored.personalLetterDraft.intent = "truth";
            Require(PersonalLetters.TrySend(restored, restored.personalLetterDraft.token, out result), "无钱也可只寄文字");
            Require(restored.flags.Contains("honest") && restored.flags.Contains("personal_letter_truth"), "如实表达留下对应记录");
            restored.personalLetterDraft.body = "平安勿念"; restored.personalLetterDraft.intent = "reassure";
            Require(PersonalLetters.TrySend(restored, restored.personalLetterDraft.token, out result), "第三封可寄出");
            restored.personalLetterDraft.body = "第四封仍是草稿";
            current = JsonUtility.ToJson(restored);
            Require(!PersonalLetters.TrySend(restored, restored.personalLetterDraft.token, out result) && current == JsonUtility.ToJson(restored), "一程最多三封，不可刷亲情");
            Require(StoryEngine.Validate(restored), "寄信后整体剧情存档保持有效");
            var beforeDeparture = StoryEngine.NewGame(); PersonalLetters.EnsureDraft(beforeDeparture).body = "尚未离家";
            current = JsonUtility.ToJson(beforeDeparture);
            Require(!PersonalLetters.TrySend(beforeDeparture, beforeDeparture.personalLetterDraft.token, out result) && current == JsonUtility.ToJson(beforeDeparture), "离乡前可存稿但不能凭空从海外寄出");
            foreach (string node in new[] { "return_choice", "end_home_lamp", "end_home_scar", "end_school_window" }) {
                var returned = StoryEngine.NewGame(); returned.nodeId = node;
                if (node == "end_school_window") returned.flags.Add("return_ticket");
                var homeDraft = PersonalLetters.EnsureDraft(returned); homeDraft.body = "回家后的这一页，留在灯下。"; homeDraft.amount = 2;
                Require(StoryEngine.Validate(returned), "归家测试存档本身有效：" + node);
                current = JsonUtility.ToJson(returned);
                Require(!PersonalLetters.CanSend(returned, out result) && result.Contains("回到泉州"), "归家后显示明确的不再跨海寄送说明：" + node);
                Require(!PersonalLetters.TrySend(returned, homeDraft.token, out result) && current == JsonUtility.ToJson(returned), "归家后不扣银、不加亲情、不伪造回批：" + node);
                var homeRestored = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(returned));
                Require(StoryEngine.Validate(homeRestored) && homeRestored.personalLetterDraft.body == homeDraft.body, "归家后仍能保留并读回草稿：" + node);
            }
            var overseasSchool = StoryEngine.NewGame(); overseasSchool.nodeId = "end_school_window";
            PersonalLetters.EnsureDraft(overseasSchool).body = "阿满，收到你亲手写的字了。";
            Require(PersonalLetters.CanSend(overseasSchool, out result), "留在海外的读书结局不被归家限制误伤");
            var empty = StoryEngine.NewGame(); empty.nodeId = "shore"; PersonalLetters.EnsureDraft(empty).body = " \n  ";
            current = JsonUtility.ToJson(empty);
            Require(!PersonalLetters.TrySend(empty, empty.personalLetterDraft.token, out result) && current == JsonUtility.ToJson(empty), "空白信不能发送且不产生副作用");
            empty.personalLetterDraft.body = new string('字', PersonalLetters.MaxBodyCharacters + 1);
            Require(!PersonalLetters.ValidateState(empty), "超长草稿被拒绝");
            var legacy = StoryEngine.NewGame(); legacy.personalLetterDraft = null; legacy.personalLetterTokens = null;
            Require(PersonalLetters.ValidateState(legacy), "旧版存档缺少自写字段可读取");
            Require(PersonalLetters.EnsureDraft(legacy).token.Length == 32 && PersonalLetters.ValidateState(legacy), "旧存档首次打开时生成可保存草稿");
            Debug.Log("QIAOPI PERSONAL LETTER CHECKS PASSED: 原文草稿读回、钱银扣款、明确心意回批、重复保护、无效操作无副作用、三封上限、归家禁寄但可存稿与旧档兼容。");
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QIAOPI PERSONAL LETTER CHECK FAILED: " + message);
        }
    }
}
