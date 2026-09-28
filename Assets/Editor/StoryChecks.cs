using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class StoryChecks
    {
        private static readonly HashSet<string> SeenNodes = new HashSet<string>();
        private static readonly HashSet<string> SeenEndings = new HashSet<string>();
        private static int choicesChecked;

        public static void Run()
        {
            SeenNodes.Clear(); SeenEndings.Clear(); choicesChecked = 0;
            Check(StoryEngine.Validate(StoryEngine.NewGame()), "New game must validate");
            LifeJourneyChecks.Run();
            string[] endings = { "home_lamp", "home_scar", "school_window", "shop_bridge", "trusted_route", "new_harbor", "unsent_letter", "silver_thread" };
            string[] paths = {
                "ledger guarantor mother share dock tally rest honest details copies desk reconcile settle return home",
                "ledger borrow mother berth dock extra overtime reassure medicine copies family hide hard_work return home",
                "ledger guarantor sister share shop learn overtime honest details copies desk reconcile hard_work return school",
                "ledger guarantor mother share shop learn rest honest details copies desk reconcile settle stay steady",
                "ledger guarantor mother share courier verify rest honest details copies desk reconcile settle stay steady",
                "ledger borrow mother share dock extra rest honest details copies desk reconcile settle farther migrate",
                "ledger borrow mother share dock extra rest reassure details copies family hide hard_work return confess",
                "ledger guarantor mother share dock tally rest honest details copies desk reconcile settle stay steady"
            };
            for (int i = 0; i < paths.Length; i++)
            {
                var state = StoryEngine.NewGame();
                foreach (string choice in ("meal neighbors " + paths[i]).Split(' ')) Play(ref state, choice, true);
                SceneData scene = StoryEngine.GetScene(state);
                Check(scene.isEnding && scene.endingId == endings[i], "Explicit path failed for " + endings[i] + ": " + scene.id);
                Check(state.decisions == 17, "Every new story takes exactly 17 choices, plus the separately logged livelihood phase");
                Check(state.flags.Contains("final_letter") && state.letters.Count >= 4, "Ending must include its final letter");
                Debug.Log("QIAOPI ENDING VERIFIED: " + endings[i] + " | " + paths[i]);
            }
            var random = new System.Random(19060923);
            for (int run = 0; run < 4000; run++)
            {
                var state = StoryEngine.NewGame();
                for (int step = 0; step < 18; step++)
                {
                    SceneData scene = StoryEngine.GetScene(state);
                    SeenNodes.Add(scene.id);
                    if (scene.isEnding) { SeenEndings.Add(scene.endingId); break; }
                    ChoiceData[] enabled = scene.choices.Where(x => x.enabled).ToArray();
                    Check(enabled.Length > 0, "No enabled choice at " + scene.id);
                    Play(ref state, enabled[random.Next(enabled.Length)].id, run < 24);
                }
                Check(StoryEngine.GetScene(state).isEnding, "Random path did not terminate after 17 decisions");
            }
            Check(SeenNodes.Count == 30, "Expected all 30 nodes, got " + SeenNodes.Count);
            foreach (string ending in endings) Check(SeenEndings.Contains(ending), "Missing ending coverage: " + ending);
            var corrupt = StoryEngine.NewGame(); corrupt.money = -1;
            Check(!StoryEngine.Validate(corrupt), "Reject negative money");
            corrupt = StoryEngine.NewGame(); corrupt.nodeId = "missing_node";
            Check(!StoryEngine.Validate(corrupt), "Reject unknown node");
            corrupt = StoryEngine.NewGame(); corrupt.version = 99;
            Check(!StoryEngine.Validate(corrupt), "Reject unsupported version");
            corrupt = StoryEngine.NewGame(); corrupt.awaitingContinue = true; corrupt.nextNodeId = "missing_node";
            Check(!StoryEngine.Validate(corrupt), "Reject unknown pending destination");
            Debug.Log("QIAOPI STORY CHECKS PASSED: 30/30 nodes, 8/8 explicit ending paths, 4000 random complete runs, " + choicesChecked + " choices, 17 choices per new story, JSON pending roundtrips, disabled/invalid choices unchanged, valid stat bounds.");
        }

        private static void Play(ref GameState state, string choiceId, bool roundtrip)
        {
            Check(StoryEngine.Validate(state), "Invalid state before choice");
            SceneData scene = StoryEngine.GetScene(state);
            SeenNodes.Add(scene.id);
            string before = JsonUtility.ToJson(state);
            Check(!StoryEngine.Choose(state, "invalid_choice"), "Unknown choice must fail");
            Check(before == JsonUtility.ToJson(state), "Unknown choice mutated state");
            Check(!StoryEngine.Continue(state), "Continue before choosing must fail");
            Check(before == JsonUtility.ToJson(state), "Premature continue mutated state");
            foreach (ChoiceData disabled in scene.choices.Where(c => !c.enabled))
            {
                Check(!StoryEngine.Choose(state, disabled.id), "Disabled choice must fail");
                Check(before == JsonUtility.ToJson(state), "Disabled choice mutated state");
            }
            Check(scene.choices.Any(c => c.id == choiceId && c.enabled), "Unavailable scripted choice " + choiceId + " at " + scene.id);
            Check(StoryEngine.Choose(state, choiceId), "Choose failed at " + scene.id + "/" + choiceId);
            choicesChecked++;
            Check(state.awaitingContinue && state.nodeId == scene.id, "Choosing must keep current scene until continue");
            Check(StoryEngine.Validate(state), "Invalid pending state");
            string pending = JsonUtility.ToJson(state);
            Check(!StoryEngine.Choose(state, choiceId), "Reject a second choice while pending");
            Check(pending == JsonUtility.ToJson(state), "Repeated pending choice mutated state");
            if (roundtrip)
            {
                state = JsonUtility.FromJson<GameState>(pending);
                Check(StoryEngine.Validate(state), "JSON pending save failed validation");
                Check(pending == JsonUtility.ToJson(state), "JSON roundtrip changed pending state");
            }
            Check(StoryEngine.Continue(state), "Continue failed");
            Check(StoryEngine.Validate(state), "Invalid continued state");
            Check(state.money >= 0 && state.health >= 0 && state.health <= 100 && state.family >= 0 && state.family <= 100 && state.trust >= 0 && state.trust <= 100, "Stat bounds failed");
            SceneData next = StoryEngine.GetScene(state);
            SeenNodes.Add(next.id);
            if (next.isEnding)
            {
                SeenEndings.Add(next.endingId);
                string end = JsonUtility.ToJson(state);
                Check(!StoryEngine.Choose(state, choiceId) && !StoryEngine.Continue(state), "Ending must be terminal");
                Check(end == JsonUtility.ToJson(state), "Terminal action mutated state");
            }
            else Check(next.choices.Any(c => c.enabled), "No enabled choice after " + scene.id);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QIAOPI STORY CHECK FAILED: " + message);
        }
    }
}
