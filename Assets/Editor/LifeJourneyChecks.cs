using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class LifeJourneyChecks
    {
        public static void Run()
        {
            var destinations = new HashSet<string>();
            for (int seed = 0; seed < 30; seed++)
            {
                GameState state = StoryEngine.NewGame(seed);
                Check(state.nodeId == "peace", "New game must start before migration");
                string destination = LifeJourney.DestinationId(state);
                destinations.Add(destination);
                Check(destination == LifeJourney.DestinationId(StoryEngine.NewGame(seed)), "Seeded destination must repeat");
                string snapshot = JsonUtility.ToJson(state);
                StoryEngine.GetScene(state); LifeJourney.DestinationIntro(state); LifeJourney.Actions(state);
                Check(snapshot == JsonUtility.ToJson(state), "Reading a scene must not mutate save");
                state = JsonUtility.FromJson<GameState>(snapshot);
                Check(destination == LifeJourney.DestinationId(state), "Destination must survive save");
                state.nodeId = "first_pay";
                int decisions = state.decisions, history = state.history.Count;
                Check(!LifeJourney.Finish(state), "Cannot skip all livelihood turns");
                string outcome;
                snapshot = JsonUtility.ToJson(state);
                Check(!LifeJourney.CommitAction(state, "not_a_job", out outcome), "Unknown action must fail");
                Check(snapshot == JsonUtility.ToJson(state), "Unknown action mutated state");
                Check(LifeJourney.CommitAction(state, "shop", out outcome), "First shop job");
                Check(LifeJourney.CommitAction(state, "shop", out outcome), "Second shop job");
                Check(state.flags.Contains("route_shop") && state.flags.Contains("shop_partner"), "Repeated practice must permit career change");
                Check(LifeJourney.PendingHardship(state) != null, "Each destination must have hardship after second turn");
                Check(!LifeJourney.CommitAction(state, "rest", out outcome), "Must address hardship before resuming");
                Check(!LifeJourney.Finish(state), "Cannot leave pending hardship");
                state = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
                Check(StoryEngine.Validate(state) && LifeJourney.PendingHardship(state) != null, "Pending hardship roundtrip");
                Check(LifeJourney.ResolveHardship(state, seed % 3 == 0 ? "evidence" : seed % 3 == 1 ? "mutual_aid" : "endure", out outcome), "Hardship choice");
                snapshot = JsonUtility.ToJson(state);
                Check(!LifeJourney.ResolveHardship(state, "evidence", out outcome), "Cannot collect hardship result twice");
                Check(snapshot == JsonUtility.ToJson(state), "Duplicate hardship action mutated state");
                Check(LifeJourney.CommitAction(state, "remit", out outcome), "Can remit during livelihood");
                Check(state.letters.Last().amount == 6 && state.letters.Last().route.StartsWith(LifeJourney.DestinationName(state), StringComparison.Ordinal), "Remittance recorded with current destination");
                Check(LifeJourney.CanFinish(state), "Three turns must offer a clear exit");
                if (seed % 2 == 0)
                {
                    Check(LifeJourney.CommitAction(state, "courier", out outcome), "Fourth optional turn");
                    Check(LifeJourney.CommitAction(state, "courier", out outcome), "Fifth optional turn");
                    Check(state.flags.Contains("route_courier") && !state.flags.Contains("route_shop"), "Latest practiced trade replaces starting career");
                    Check(LifeJourney.CommitAction(state, "rest", out outcome), "Sixth optional turn");
                    Check(!LifeJourney.CommitAction(state, "rest", out outcome), "Six-turn cap must prevent grinding");
                }
                Check(LifeJourney.Finish(state), "Finish livelihood");
                Check(!LifeJourney.IsActive(state) && StoryEngine.Validate(state), "Finished phase is valid");
                Check(state.decisions == decisions && state.history.Count == history, "Livelihood must not alter story counter invariants");
                snapshot = JsonUtility.ToJson(state);
                Check(!LifeJourney.CommitAction(state, "dock", out outcome) && !LifeJourney.Finish(state), "No replay of completed livelihood");
                Check(snapshot == JsonUtility.ToJson(state), "Completed livelihood mutation");
            }
            Check(destinations.Count == 3, "All three destinations must be reachable");
            GameState legacy = new GameState { nodeId = "shop" };
            Check(StoryEngine.Validate(legacy), "Legacy save with no journey remains valid");
            string originalNode = legacy.nodeId;
            LifeJourney.Ensure(legacy);
            Check(legacy.nodeId == originalNode && legacy.journey.completed && LifeJourney.DestinationId(legacy) == "singapore", "Legacy save must not reroll destination or restart story");
            var low = StoryEngine.NewGame(1); low.nodeId = "first_pay"; low.money = 0; low.health = 0;
            string restOutcome;
            Check(LifeJourney.Actions(low).Any(a => a.id == "rest" && a.enabled), "Impoverished exhausted player must have a safe action");
            Check(LifeJourney.CommitAction(low, "rest", out restOutcome) && low.health > 0, "Free recovery prevents softlock");
            var broken = StoryEngine.NewGame(0); broken.journey.destination = "invalid";
            Check(!StoryEngine.Validate(broken), "Reject unknown destination");
            CheckCombinedJourneys();
            Debug.Log("QIAOPI LIFE JOURNEY CHECKS PASSED: 3 seeded destinations, 30 save roundtrips, all hardship responses, 3-turn exit / 6-turn cap, career change, remittance, legacy save preservation, no counter mutation, low-resource recovery.");
        }

        // Exercise the systems together: spending silver can remove a later option,
        // working can change a career, hardship affects recovery, and the new flags
        // can change how a late story choice resolves. Each run starts at the real
        // prologue, rather than jumping directly to a convenient test state.
        private static void CheckCombinedJourneys()
        {
            const int Runs = 480;
            var destinations = new HashSet<string>();
            var endings = new HashSet<string>();
            var jobs = new HashSet<string>();
            var responses = new HashSet<string>();
            var intentions = new HashSet<string>();
            int turnsChecked = 0, pendingJobsChecked = 0, lettersChecked = 0, savesChecked = 0;
            for (int run = 0; run < Runs; run++)
            {
                int seed = 2000928 + run;
                var random = new System.Random(seed);
                GameState state = StoryEngine.NewGame(seed);
                string destination = LifeJourney.DestinationId(state);
                destinations.Add(destination);
                int targetTurns = run % 2 == 0 ? LifeJourney.MinTurns : LifeJourney.MaxTurns;
                int letterTurn = 1 + random.Next(targetTurns);
                bool lived = false, sent = false;
                for (int step = 0; step < 18; step++)
                {
                    string context = "combined seed " + seed + ", node " + state.nodeId;
                    Check(StoryEngine.Validate(state), "Invalid start of " + context);
                    SceneData scene = StoryEngine.GetScene(state);
                    if (scene.isEnding) { endings.Add(scene.endingId); break; }
                    if (LifeJourney.IsActive(state))
                    {
                        Check(!lived, "Livelihood cannot repeat in " + context);
                        int decisionsBefore = state.decisions, historyBefore = state.history.Count;
                        for (int turn = 0; turn < targetTurns; turn++)
                        {
                            LivelihoodAction[] enabled = LifeJourney.Actions(state).Where(a => a.enabled).ToArray();
                            Check(enabled.Length > 0, "No legal livelihood action in " + context + ", turn " + turn);
                            LivelihoodAction chosen = enabled[random.Next(enabled.Length)];
                            jobs.Add(chosen.id);
                            if (chosen.physical)
                            {
                                int moneyBeforeWork = state.money;
                                state.journey.pendingJob = chosen.id;
                                Roundtrip(ref state, destination, context + ", working " + chosen.id);
                                savesChecked++; pendingJobsChecked++;
                                Check(state.journey.pendingJob == chosen.id && state.money == moneyBeforeWork,
                                    "Loading unfinished work must preserve the job without paying wages in " + context);
                            }
                            string outcome;
                            Check(LifeJourney.CommitAction(state, chosen.id, out outcome), "Livelihood action failed in " + context + ": " + chosen.id);
                            Check(!string.IsNullOrEmpty(outcome) && string.IsNullOrEmpty(state.journey.pendingJob), "Job must settle exactly into its outcome in " + context);
                            Check(state.journey.turns == turn + 1, "Incorrect livelihood turn count in " + context);
                            turnsChecked++;
                            Roundtrip(ref state, destination, context + ", completed livelihood turn " + (turn + 1));
                            savesChecked++;

                            JourneyHardship hardship = LifeJourney.PendingHardship(state);
                            if (hardship != null)
                            {
                                Check(turn == 1 && !LifeJourney.CanFinish(state), "Hardship occurs after turn two and must be addressed in " + context);
                                LivelihoodAction[] available = hardship.choices.Where(a => a.enabled).ToArray();
                                Check(available.Length > 0, "Hardship must retain a legal response in " + context);
                                LivelihoodAction response = available[random.Next(available.Length)];
                                responses.Add(response.id);
                                Check(LifeJourney.ResolveHardship(state, response.id, out outcome), "Hardship response failed in " + context);
                                Check(LifeJourney.PendingHardship(state) == null, "Hardship remains pending in " + context);
                                Roundtrip(ref state, destination, context + ", hardship " + response.id);
                                savesChecked++;
                            }

                            if (turn + 1 == letterTurn)
                            {
                                PersonalLetterDraft draft = PersonalLetters.EnsureDraft(state);
                                draft.recipient = random.Next(2) == 0 ? "mother" : "sister";
                                draft.intent = new[] { "reassure", "truth", "study" }[random.Next(3)];
                                int[] affordable = PersonalLetters.Amounts.Where(a => a <= state.money).ToArray();
                                draft.amount = affordable[random.Next(affordable.Length)];
                                draft.body = "母亲、阿满：\n今日在" + LifeJourney.DestinationName(state) + "做工，也记得留些力气歇息。\n家门前那盏灯，我一直记着。";
                                string originalBody = draft.body, token = draft.token, intent = draft.intent;
                                int amount = draft.amount, silverBefore = state.money, lettersBefore = state.letters.Count;
                                intentions.Add(intent);
                                Roundtrip(ref state, destination, context + ", unsent personal draft");
                                savesChecked++;
                                Check(PersonalLetters.TrySend(state, token, out outcome), "Personal letter failed in " + context + ": " + outcome);
                                Check(state.money == silverBefore - amount && state.letters.Count == lettersBefore + 2,
                                    "Letter must deduct its exact silver and add outgoing/reply records in " + context);
                                Check(state.letters[lettersBefore].body == originalBody && state.letters[lettersBefore].amount == amount &&
                                    state.letters[lettersBefore].route.StartsWith(LifeJourney.DestinationName(state), StringComparison.Ordinal) &&
                                    state.letters[lettersBefore + 1].incoming, "Letter text, route and reply mismatch in " + context);
                                Check(intent != "truth" || state.flags.Contains("honest"), "Truthful letter must reach later story conditions in " + context);
                                Check(intent != "study" || amount == 0 || state.flags.Contains("school_supported"), "Funded study letter must reach later story conditions in " + context);
                                Roundtrip(ref state, destination, context + ", sent personal letter");
                                savesChecked++; lettersChecked++; sent = true;
                            }
                            Check(state.decisions == decisionsBefore && state.history.Count == historyBefore,
                                "Life actions and personal letters cannot alter main-story counters in " + context);
                        }
                        Check(LifeJourney.CanFinish(state) && LifeJourney.Finish(state), "Cannot finish bounded livelihood in " + context);
                        Check(state.journey.turns == targetTurns && state.journey.log.Count == targetTurns + 1,
                            "Expected each chosen action and one hardship record in " + context);
                        Roundtrip(ref state, destination, context + ", finished livelihood");
                        savesChecked++; lived = true;
                        scene = StoryEngine.GetScene(state);
                    }
                    ChoiceData[] choices = scene.choices.Where(c => c.enabled).ToArray();
                    Check(choices.Length > 0, "No enabled story choice after combined play in " + context);
                    string choice = choices[random.Next(choices.Length)].id;
                    Check(StoryEngine.Choose(state, choice), "Story choice failed in " + context + ": " + choice);
                    Roundtrip(ref state, destination, context + ", pending story result");
                    savesChecked++;
                    Check(StoryEngine.Continue(state), "Story cannot continue in " + context);
                    Roundtrip(ref state, destination, context + ", next story node");
                    savesChecked++;
                }
                Check(StoryEngine.GetScene(state).isEnding && state.decisions == 17,
                    "Combined journey did not terminate after 17 main decisions, seed " + seed);
                Check(lived && sent && state.journey.completed && state.personalLettersSent == 1,
                    "Combined journey skipped an integrated system, seed " + seed);
                Check(state.flags.Contains("final_letter"), "Combined journey lost its ending letter, seed " + seed);
            }
            Check(destinations.Count == 3 && jobs.Count == 5 && responses.Count == 3 && intentions.Count == 3,
                "Combined seeds must cover three destinations, five life actions, three hardship responses and three letter intentions");
            Debug.Log("QIAOPI COMBINED JOURNEY CHECKS PASSED: " + Runs + " seeded complete stories, " + turnsChecked +
                " livelihood turns (240 three-turn / 240 six-turn), " + pendingJobsChecked + " unfinished-job saves, " + lettersChecked +
                " personal letters, " + savesChecked + " JSON roundtrips, 3 destinations / 5 life actions / 3 hardship responses / 3 letter intentions, " +
                endings.Count + " ending types observed; all resources bounded and all paths terminated.");
        }

        private static void Roundtrip(ref GameState state, string destination, string context)
        {
            Check(StoryEngine.Validate(state), "Invalid pre-save state: " + context);
            string json = JsonUtility.ToJson(state);
            state = JsonUtility.FromJson<GameState>(json);
            Check(StoryEngine.Validate(state) && json == JsonUtility.ToJson(state), "Save roundtrip changed state: " + context);
            Check(LifeJourney.DestinationId(state) == destination, "Saved destination changed: " + context);
            Check(state.money >= 0 && state.money <= 9999 && state.health >= 0 && state.health <= 100 &&
                state.family >= 0 && state.family <= 100 && state.trust >= 0 && state.trust <= 100,
                "Resource bounds violated: " + context);
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("QIAOPI LIFE JOURNEY CHECK FAILED: " + message); }
    }
}
