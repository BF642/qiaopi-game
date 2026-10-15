using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        string[] firstDayQAPaths, firstDayQAFiles, firstDayQAKeys;
        int[] firstDayQAPreferences;
        float firstDayQAMusicLevel;
        bool FirstDayShortReview => FirstDayQA && Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-firstday-review") >= 0;

        static void FirstDayRequire(bool condition, string label)
        { if (!condition) throw new Exception("FIRSTDAY_QA_" + label); }

        IEnumerator RunFirstDayQA()
        {
            // Flatten nested iterators so exceptions from walking and captures
            // produce a failing process exit rather than a stranded player.
            var pending = new Stack<IEnumerator>(); pending.Push(RunFirstDayChecks());
            while (pending.Count > 0) {
                object wait = null; Exception failure = null;
                try {
                    if (!pending.Peek().MoveNext()) pending.Pop();
                    else wait = pending.Peek().Current;
                } catch (Exception error) { failure = error; }
                if (failure != null) {
                    while (pending.Count > 0) (pending.Pop() as IDisposable)?.Dispose();
                    try { CheckFirstDayQAIsolation(); }
                    catch (Exception isolation) { Debug.LogError("FIRSTDAY_QA_ISOLATION_FAILED: " + isolation); }
                    Debug.LogError("FIRSTDAY_QA_FAILED: " + failure);
                    Application.Quit(1); yield break;
                }
                if (wait is IEnumerator) pending.Push((IEnumerator)wait);
                else if (pending.Count > 0) yield return wait;
            }
            Debug.Log(FirstDayShortReview
                ? "FIRSTDAY_REVIEW_ALL_PASSED: synthetic interrupted settlement resumes through actual Next without duplicate rewards; conversation marker hidden; evening and actual seated writing captures; saves/preferences unchanged, muted. This short review does not replace the full three-route playthrough."
                : "FIRSTDAY_RUNTIME_ALL_PASSED: three jobs, physical branch tasks, gated wages, dinner, seated original letters, bed, morning, delayed replies, livelihood access, resumed stage saves; real saves and preferences unchanged, muted.");
            Application.Quit(0);
        }

        IEnumerator RunFirstDayChecks()
        {
            FirstDayRequire(FirstDayQA && QA && DetailQA, "ISOLATED_ENTRY");
            firstDayQAPaths = new[] { savePath, savePath + ".bak", savePath + ".tmp" };
            firstDayQAFiles = new string[firstDayQAPaths.Length];
            for (int i = 0; i < firstDayQAPaths.Length; i++) firstDayQAFiles[i] = CharacterFileSnapshot(firstDayQAPaths[i]);
            firstDayQAKeys = new[] { "qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized" };
            firstDayQAPreferences = new int[firstDayQAKeys.Length];
            for (int i = 0; i < firstDayQAKeys.Length; i++) firstDayQAPreferences[i] = PlayerPrefs.GetInt(firstDayQAKeys[i], -1);
            firstDayQAMusicLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false);
            if (voiceSource) voiceSource.Stop(); if (sfx) sfx.Stop();
            string folder = "FirstDayReview-" + Application.version + (MobileControls ? "-mobile" : "-desktop");
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, folder));
            Debug.Log("FIRSTDAY_QA_OUTPUT=" + Path.Combine(Application.persistentDataPath, folder));
            if (FirstDayShortReview) {
                yield return RunFirstDayShortReview(folder);
                CheckFirstDayQAIsolation();
                yield break;
            }
            string[] routes = { "dock", "shop", "courier" }, choices = { "help", "learn", "verify" };
            string[] destinations = { "singapore", "penang", "rangoon" }, recipients = { "mother", "wife", "sister" };

            yield return PreflightFirstDayQABranches();

            for (int r = 0; r < routes.Length; r++) {
                string route = routes[r], prefix = folder + "/" + route;
                ClearFirstDayQAPanels();
                state = StoryEngine.NewGame(20261008 + r); state.nodeId = route;
                state.journey.destination = destinations[r];
                FirstDayRequire(FirstDayChapter.TryBegin(state), "BEGIN_" + route);
                EnterNode(true); ResetView(); yield return null;
                Color daySky = RenderSettings.ambientSkyColor, dayEquator = RenderSettings.ambientEquatorColor, dayGround = RenderSettings.ambientGroundColor;
                var daySunObject = GameObject.Find("Afternoon Sun");
                Light daySun = daySunObject ? daySunObject.GetComponent<Light>() : null;
                FirstDayRequire(daySun, "WORLD_SUN_EXISTS");
                float daySunIntensity = daySun.intensity; Color daySunColor = daySun.color; Quaternion daySunRotation = daySun.transform.rotation;
                FirstDayRequire(state.firstDay.stage == "work" && firstDayCompanion, "WORK_ACTOR_" + route);
                FirstDayRequire(!LifeJourney.IsActive(state), "NO_LIFE_PANEL_DURING_FIRST_DAY");
                CheckFirstDayCompanionClearance();
                ResumeFirstDayQASave("work_before_choice"); yield return null;

                int initialMoney = state.money, initialDecisions = state.decisions;
                yield return WalkFirstPerson(mission.npcPosition, route + " early wages");
                HandleFirstDayInteract();
                FirstDayRequire(state.money == initialMoney && state.decisions == initialDecisions && !state.firstDay.workSettled, "NO_EARLY_WAGES_" + route);
                ClearFirstDayQAPanels();
                yield return CompleteFirstDayQAUnit(route + " initial work");
                TickFirstDay(); yield return null;
                FirstDayRequire(state.firstDay.stage == "encounter", "ENCOUNTER_AFTER_ACTION_" + route);
                ResumeFirstDayQASave("encounter"); yield return null;
                yield return ReachFirstDayQAInteraction(FirstDayContact, route + " meet Xusheng");
                FirstDayRequire(firstDayPanel, "CHOICE_PANEL_" + route);
                yield return CaptureWorld(prefix + "_01_work_choice");

                int choiceMoney = state.money, choiceHealth = state.health, choiceTrust = state.trust, choiceDecisions = state.decisions;
                SelectFirstDayWork(choices[r]);
                FirstDayRequire(state.firstDay.stage == "work" && state.firstDay.choice == choices[r] && progress == 0 && !state.awaitingContinue, "CHOICE_ASSIGNMENT_" + route);
                FirstDayRequire(state.money == choiceMoney && state.health == choiceHealth && state.trust == choiceTrust && state.decisions == choiceDecisions, "CHOICE_HAS_NO_REWARD_" + route);
                CheckFirstDayCompanionClearance();
                ResumeFirstDayQASave("chosen_work"); yield return null;
                yield return WalkFirstPerson(mission.npcPosition, route + " unfinished branch wages");
                HandleFirstDayInteract();
                FirstDayRequire(state.firstDay.stage == "work" && !state.firstDay.workSettled && state.money == choiceMoney, "BRANCH_MUST_BE_DONE_" + route);
                ClearFirstDayQAPanels();
                int units = 0;
                while (!Complete && units++ < 4) yield return CompleteFirstDayQAUnit(route + " branch " + units);
                FirstDayRequire(Complete && state.firstDay.stage == "work", "PHYSICAL_BRANCH_FINISHED_" + route);
                ResumeFirstDayQASave("work_complete"); yield return null;
                yield return ReachFirstDayQAInteraction(mission.npcPosition, route + " settle wages");
                FirstDayRequire(state.firstDay.stage == "evening" && state.firstDay.workSettled && state.nodeId == "first_pay" && loadedWorld == "quarters", "WAGES_TO_EVENING_" + route);
                FirstDayRequire(state.money == choiceMoney + (route == "shop" ? 11 : 9) && state.decisions == choiceDecisions + 1, "SINGLE_STORY_PAYMENT_" + route);
                FirstDayRequire(!firstDayCompanion, "NO_EXTRA_XUSHENG_IN_QUARTERS");
                ResumeFirstDayQASave("evening"); yield return null;
                yield return ReachFirstDayQAInteraction(mission.npcPosition, route + " dinner");
                FirstDayRequire(firstDayPanel, "DINNER_PANEL_" + route);
                yield return CaptureWorld(prefix + "_02_evening");
                int dinnerMoney = state.money;
                ChooseFirstDayEvening("meal");
                FirstDayRequire(state.firstDay.stage == "letter" && state.money == dinnerMoney - 2, "DINNER_TO_LETTER_" + route);
                ResumeFirstDayQASave("letter"); yield return null;

                int incomingBefore = state.letters.FindAll(l => l.incoming).Count;
                yield return WalkToWritingDeskForQA();
                FirstDayRequire(letterEditor && writingSeated && NearWritingDesk, "ACTUAL_WRITING_SEAT_" + route);
                string ownText = PersonalLetters.RecipientName(recipients[r]) + "：\n今天在" + LifeJourney.DestinationName(state) + "做了第一份工，也认得了能说话的同乡。\n我会留够饭钱，把每天的实情慢慢写给你。";
                var draft = PersonalLetters.EnsureDraft(state);
                draft.recipient = recipients[r]; draft.intent = "truth"; draft.amount = 2; draft.body = ownText;
                personalLetterPreview = true;
                CheckFirstDayQASave("seated_letter");
                yield return CaptureWorld(prefix + "_03_seated_letter");
                int letterMoney = state.money; string token = draft.token, notice;
                FirstDayRequire(PersonalLetters.TrySend(state, token, out notice), "SEND_" + route + " " + notice);
                FirstDayRequire(state.firstDay.stage == "bed" && state.firstDay.letterSent && state.firstDay.sentToken == token && state.money == letterMoney - 2, "SEALED_ONCE_" + route);
                FirstDayRequire(state.letters.Exists(l => !l.incoming && l.body == ownText) && state.letters.FindAll(l => l.incoming).Count == incomingBefore, "EXACT_TEXT_NO_INSTANT_REPLY_" + route);
                FirstDayRequire(state.firstDay.pendingReply.signature == PersonalLetters.RecipientName(recipients[r]), "CORRECT_FAMILY_REPLY_" + route);
                AfterFirstDayLetterSent(); yield return null;
                FirstDayRequire(!letterEditor && !writingSeated && state.firstDay.stage == "bed", "STAND_AFTER_POSTING");
                ResumeFirstDayQASave("bed"); yield return null;
                FirstDayRequire(CheckFirstDayQAPath(region.spawn, FirstDayBedPoint, route + "/bed/from entrance", 1.8f) &&
                    CheckFirstDayQAPath(writingPlace.approach, FirstDayBedPoint, route + "/bed/from writing room", 1.8f), "BED_APPROACHES_" + route);
                HandleFirstDayInteract();
                FirstDayRequire(state.firstDay.stage == "bed", "NO_SLEEP_AT_WRITING_DESK");
                yield return WalkFirstPerson(FirstDayBedPoint, route + " reach actual bed");
                yield return CaptureWorld(prefix + "_04_bedside");
                yield return ReachFirstDayQAInteraction(FirstDayBedPoint, route + " sleep");
                FirstDayRequire(state.firstDay.stage == "morning" && state.letters.FindAll(l => l.incoming).Count == incomingBefore, "NEXT_MORNING_NOT_CROSS_SEA_REPLY");
                ResumeFirstDayQASave("morning"); yield return null;
                yield return CaptureWorld(prefix + "_05_morning_light");
                yield return ReachFirstDayQAInteraction(mission.npcPosition, route + " morning coworker");
                FirstDayRequire(firstDayPanel, "MORNING_PANEL_" + route);
                yield return CaptureWorld(prefix + "_06_morning_feedback");
                FinishFirstDayMorning(); yield return null;
                FirstDayRequire(state.firstDay.stage == "reply" && !string.IsNullOrEmpty(state.firstDay.morningFeedback) && state.letters.FindAll(l => l.incoming).Count == incomingBefore, "WEEKS_LATER_BEFORE_COLLECTION");
                ResumeFirstDayQASave("reply"); yield return null;
                yield return ReachFirstDayQAInteraction(mission.npcPosition, route + " weeks later reply");
                FirstDayRequire(state.firstDay.stage == "complete" && state.firstDay.replyCollected && !FirstDayChapter.IsActive(state) && journal, "COLLECTED_REPLY_" + route);
                FirstDayRequire(state.letters.FindAll(l => l.incoming).Count == incomingBefore + 1 && state.letters.Exists(l => l.incoming && l.date == FirstDayChapter.ReplyDate), "ONE_DATED_REPLY_" + route);
                FirstDayRequire(firstDayLightState == null, "DAYLIGHT_RESTORED_" + route);
                FirstDayRequire(RenderSettings.ambientSkyColor == daySky && RenderSettings.ambientEquatorColor == dayEquator && RenderSettings.ambientGroundColor == dayGround &&
                    Mathf.Abs(daySun.intensity - daySunIntensity) < .001f && daySun.color == daySunColor && Quaternion.Angle(daySun.transform.rotation, daySunRotation) < .01f,
                    "EXACT_BASE_LIGHTING_RESTORED_" + route);
                yield return CaptureWorld(prefix + "_07_weeks_later_reply");
                ResumeFirstDayQASave("complete"); yield return null;
                FirstDayRequire(LifeJourney.IsActive(state) && LifeJourney.Actions(state).FindAll(a => a.physical && a.enabled).Count == 3, "FREE_WORK_REOPENED_" + route);
                FirstDayRequire(FirstDayChapter.BonusForWork(state, route).available && !state.firstDay.bonusUsed, "NEXT_JOB_REMEMBERS_CHOICE_" + route);
                ClearFirstDayQAPanels(); StartLifeAction(route);
                FirstDayRequire(state.journey.pendingJob == route && !FirstDayChapter.IsActive(state), "NEXT_JOB_SELECTABLE_" + route);
                ResumeFirstDayQASave("next_job");
                Debug.Log("FIRSTDAY_ROUTE_PASSED route=" + route + " choice=" + choices[r] + " destination=" + destinations[r] + " recipient=" + recipients[r]);
                CheckFirstDayQAIsolation();
            }
            CheckFirstDayQAIsolation();
        }

        IEnumerator RunFirstDayShortReview(string folder)
        {
            Debug.Log("FIRSTDAY_SHORT_REVIEW: synthetic scene fixtures; normal-speed physical writing-desk approach; no full-route timing or performance claim.");
            ClearFirstDayQAPanels();
            state = StoryEngine.NewGame(20261008); state.nodeId = "dock"; state.journey.destination = "singapore";
            FirstDayRequire(StoryEngine.Validate(state) && FirstDayChapter.TryBegin(state) &&
                FirstDayChapter.TriggerEncounter(state, 1) && FirstDayChapter.ChooseWork(state, "help"), "REVIEW_PENDING_SETUP");
            EnterNode(true); progress = mission.required; RefreshProps();
            int beforeMoney = state.money, beforeDecisions = state.decisions;
            FirstDayRequire(StoryEngine.Choose(state, "help") && state.awaitingContinue && StoryEngine.Validate(state), "REVIEW_VALID_INTERRUPTED_CHOICE");
            int paidMoney = state.money, paidDecisions = state.decisions, paidHistory = state.history.Count;
            FirstDayRequire(paidMoney == beforeMoney + 9 && paidDecisions == beforeDecisions + 1, "REVIEW_REWARD_ALREADY_APPLIED");
            ResumeFirstDayQASave("review_interrupted_settlement"); yield return null;
            FirstDayRequire(state.awaitingContinue && state.firstDay.stage == "work", "REVIEW_PENDING_RESTORED");
            dialogue = true; Next(); yield return null;
            FirstDayRequire(state.firstDay.stage == "evening" && loadedWorld == "quarters" && !state.awaitingContinue && state.firstDay.workSettled,
                "REVIEW_ACTUAL_NEXT_REACHES_EVENING");
            FirstDayRequire(state.money == paidMoney && state.decisions == paidDecisions && state.history.Count == paidHistory && StoryEngine.Validate(state),
                "REVIEW_NEXT_NO_DOUBLE_REWARD");
            Next();
            FirstDayRequire(state.money == paidMoney && state.decisions == paidDecisions && state.history.Count == paidHistory && state.firstDay.stage == "evening",
                "REVIEW_REPEATED_NEXT_NO_REWARD");
            string eveningState = JsonUtility.ToJson(state);
            Debug.Log("FIRSTDAY_SHORT_PENDING_NEXT_PASSED money=" + state.money + " decisions=" + state.decisions);

            ClearFirstDayQAPanels();
            state = StoryEngine.NewGame(20261008); state.nodeId = "dock"; state.journey.destination = "singapore";
            FirstDayRequire(FirstDayChapter.TryBegin(state) && FirstDayChapter.TriggerEncounter(state, 1), "REVIEW_ENCOUNTER_SETUP");
            EnterNode(true); progress = 1; RefreshProps(); yield return null;
            PlaceFirstDayReviewFixture(FirstDayContact, "encounter");
            OpenFirstDayPanel(); yield return null; yield return null;
            FirstDayRequire(firstDayPanel && targetMarker && !targetMarker.activeInHierarchy, "REVIEW_CONVERSATION_MARKER_HIDDEN");
            Vector3 gaze = firstDayCompanion.transform.position + Vector3.up * (ModelLibrary.CharacterHeight("Xusheng") * .9f) - cam.transform.position;
            FirstDayRequire(Vector3.Dot(cam.transform.forward, gaze.normalized) > .97f, "REVIEW_CONVERSATION_FACES_PERSON");
            yield return CaptureWorld(folder + "/short_01_work_choice");
            FirstDayRequire(!targetMarker.activeInHierarchy, "REVIEW_MARKER_STAYS_HIDDEN");

            ClearFirstDayQAPanels(); state = JsonUtility.FromJson<GameState>(eveningState);
            FirstDayRequire(StoryEngine.Validate(state), "REVIEW_EVENING_FIXTURE_VALID");
            EnterNode(true); yield return null;
            PlaceFirstDayReviewFixture(mission.npcPosition, "evening");
            OpenFirstDayPanel(); yield return null;
            yield return CaptureWorld(folder + "/short_02_evening");
            int dinnerMoney = state.money;
            ChooseFirstDayEvening("meal");
            FirstDayRequire(state.firstDay.stage == "letter" && state.money == dinnerMoney - 2, "REVIEW_DINNER_OPENS_LETTER");
            var draft = PersonalLetters.EnsureDraft(state);
            draft.recipient = "wife"; draft.intent = "truth"; draft.amount = 2;
            draft.body = "吾妻：\n今日做完第一份工，许生邀我吃了热饭。夜里坐在住处的灯下，才有工夫把这一天写给你。\n我会留够饭钱，平安和难处都慢慢说。";
            string ownText = draft.body;
            yield return WalkToWritingDeskForQA();
            FirstDayRequire(letterEditor && writingSeated && NearWritingDesk && state.personalLetterDraft.body == ownText, "REVIEW_REAL_SEATED_WRITING");
            FirstDayRequire(targetMarker && !targetMarker.activeInHierarchy, "REVIEW_WRITING_MARKER_HIDDEN");
            CheckFirstDayQASave("review_seated_letter");
            yield return CaptureWorld(folder + "/short_03_seated_letter");
            personalLetterPreview = true;
            yield return CaptureWorld(folder + "/short_04_letter_review");
            ClosePersonalLetter(); RestoreFirstDayLighting();
            CheckFirstDayQAIsolation();
        }

        void PlaceFirstDayReviewFixture(Vector3 contact, string label)
        {
            // This shortcut only stages a short visual review. Complete-route QA
            // continues to reach every contact through the character controller.
            Vector3 point = contact + Vector3.back * 1.65f;
            FirstDayRequire(CheckFirstDayQAPath(region.spawn, point, "review_fixture/" + label), "REVIEW_FIXTURE_PATH_" + label);
            FirstDayRequire(!Physics.CheckCapsule(point + Vector3.up * .55f, point + Vector3.up * 1.65f, .28f,
                ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore), "REVIEW_FIXTURE_CLEAR_" + label);
            controller.enabled = false;
            player.transform.position = WorldFactory.CharacterGroundPosition(point) + Vector3.up * .05f;
            controller.enabled = true; walkRoute.Clear(); routeIndex = 0; UpdateCamera(true);
            FirstDayRequire(FirstDayCanReach(contact), "REVIEW_FIXTURE_CAN_REACH_" + label);
        }

        IEnumerator PreflightFirstDayQABranches()
        {
            string[] routes = { "dock", "shop", "courier" };
            string[][] choices = { new[] { "extra", "help", "tally" }, new[] { "learn", "wages", "read" }, new[] { "verify", "rush", "craft" } };
            int failed = 0, checkedTargets = 0;
            for (int r = 0; r < routes.Length; r++) {
                GameObject routeWorld = null;
                foreach (string choice in choices[r]) {
                    ClearFirstDayQAPanels();
                    state = StoryEngine.NewGame(20261008); state.nodeId = routes[r];
                    state.journey.destination = "singapore";
                    state.flags.Add("ledger"); // The tally branch requires this existing prerequisite.
                    FirstDayRequire(StoryEngine.Validate(state) && FirstDayChapter.TryBegin(state) &&
                        FirstDayChapter.TriggerEncounter(state, 1) && FirstDayChapter.ChooseWork(state, choice), "PREFLIGHT_BRANCH_STATE_" + routes[r] + "/" + choice);
                    EnterNode(true); yield return null; Physics.SyncTransforms();
                    if (routeWorld) FirstDayRequire(routeWorld == world, "PREFLIGHT_REUSES_WORLD_" + routes[r]);
                    routeWorld = world;
                    string label = routes[r] + "/" + choice;
                    var targets = new List<Vector3> { mission.npcPosition, FirstDayContact };
                    var names = new List<string> { "employer", "coworker" };
                    if (mission.activity == "inspect") {
                        Vector3[] stops = FirstDayTasks.InspectionPositions(state);
                        for (int i = 0; i < mission.required; i++) { targets.Add(stops[i]); names.Add("inspection_" + i); }
                    } else {
                        targets.Add(mission.itemPosition); names.Add("pickup");
                        targets.Add(mission.destination); names.Add("delivery");
                    }
                    for (int i = 0; i < targets.Count; i++) {
                        if (!CheckFirstDayQAPath(region.spawn, targets[i], label + "/" + names[i])) failed++;
                        checkedTargets++;
                    }
                    // The loaded geometry changes at each choice, but this is a
                    // path/visibility preflight, not a teleport-based playthrough.
                    Debug.Log("FIRSTDAY_BRANCH_PREFLIGHT " + label + " activity=" + mission.activity + " targets=" + targets.Count);
                }
            }
            Debug.Log("FIRSTDAY_PREFLIGHT_SUMMARY branches=9 targets=" + checkedTargets + " failures=" + failed);
            FirstDayRequire(failed == 0, "BRANCH_PREFLIGHT_FAILURES_" + failed);
        }

        bool CheckFirstDayQAPath(Vector3 from, Vector3 target, string label, float reach = 2.2f)
        {
            Physics.SyncTransforms();
            List<Vector3> route = WalkPath.Find(from, target, loadedWorld);
            Vector3 endpoint = route.Count == 0 ? from : route[route.Count - 1];
            Vector3 delta = endpoint - target; delta.y = 0;
            bool ground = region.IsGround(endpoint);
            bool near = delta.magnitude < reach;
            bool visible = !Physics.Linecast(endpoint + Vector3.up * .9f, target + Vector3.up * .9f,
                ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore);
            bool clear = ground && near && visible && (route.Count > 0 || (from - target).sqrMagnitude < reach * reach);
            // Find already capsule-sweeps grid edges; also check that its output
            // never relies on world clamping to cross a non-walkable boundary.
            foreach (Vector3 point in route) clear &= region.IsGround(point);
            if (!clear) Debug.LogError("FIRSTDAY_PATH_UNREACHABLE " + label + " world=" + loadedWorld +
                " start=" + from + " target=" + target + " endpoint=" + endpoint + " waypoints=" + route.Count +
                " distance=" + delta.magnitude.ToString("F3") + " ground=" + ground + " visible=" + visible);
            else Debug.Log("FIRSTDAY_PATH_OK " + label + " waypoints=" + route.Count + " distance=" + delta.magnitude.ToString("F3"));
            return clear;
        }

        IEnumerator CompleteFirstDayQAUnit(string label)
        {
            if (mission.activity == "inspect") {
                int index = 0; while (index < mission.required && inspected.Contains(index)) index++;
                FirstDayRequire(index < mission.required, "PENDING_INSPECTION_" + label);
                yield return WalkFirstPerson(FirstDayTasks.InspectionPositions(state)[index], label + " inspect");
                Interact(); FirstDayRequire(puzzle && puzzleIndex == index, "INSPECTION_OPENS_" + label);
                var entry = InspectionEntry();
                FirstDayRequire(entry != null && entry.correct >= 0 && entry.correct < entry.answers.Length, "INSPECTION_CASE_" + label);
                AnswerInspection(entry.correct); yield return null;
                FirstDayRequire(!puzzle && inspected.Contains(index), "INSPECTION_RECORDED_" + label);
            } else {
                FirstDayRequire(mission.activity == "deliver", "PHYSICAL_TASK_" + label);
                yield return WalkFirstPerson(mission.itemPosition, label + " pickup");
                Interact(); FirstDayRequire(carrying && !sideCarrying, "CARRY_" + label);
                ResumeFirstDayQASave("carrying_" + label); yield return null;
                FirstDayRequire(carrying, "CARRY_RESUMED_" + label);
                yield return WalkFirstPerson(mission.destination, label + " deliver");
                int before = progress; Interact(); yield return null;
                FirstDayRequire(!carrying && progress == before + 1, "DELIVERED_" + label);
            }
        }

        IEnumerator ReachFirstDayQAInteraction(Vector3 target, string label)
        {
            ClearFirstDayQAPanels();
            yield return WalkFirstPerson(target, label);
            if (AttemptFirstDayQAInteraction()) yield break;
            // A path may stop around a table corner. Walk to an unobstructed
            // approach rather than bypassing the same reach/line-of-sight gate.
            foreach (Vector3 offset in new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward }) {
                Vector3 approach = target + offset * 1.1f;
                if (!region.IsGround(approach) || Physics.Linecast(approach + Vector3.up * 1.1f, target + Vector3.up * 1.1f,
                    ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore)) continue;
                walkRoute = WalkPath.Find(player.transform.position, approach, loadedWorld); routeIndex = 0;
                if (walkRoute.Count == 0 && Dist(approach) > .6f) continue;
                float deadline = Time.time + 45;
                while (routeIndex < walkRoute.Count && Time.time < deadline) {
                    yield return null; RequireFirstPerson(label + " approach");
                    FirstDayRequire(region.IsGround(player.transform.position), "APPROACH_ON_GROUND_" + label);
                }
                walkRoute.Clear(); routeIndex = 0;
                if (AttemptFirstDayQAInteraction()) yield break;
            }
            FirstDayRequire(false, "INTERACTION_UNREACHABLE_" + label + " player=" + player.transform.position + " target=" + target);
        }

        bool AttemptFirstDayQAInteraction()
        {
            string before = state.firstDay.stage;
            // HandleFirstDayInteract's bool means the key was consumed, including
            // an out-of-range reminder. Only an actual panel/phase change passes.
            HandleFirstDayInteract();
            return state.firstDay.stage != before || firstDayPanel || journal;
        }

        void CheckFirstDayCompanionClearance()
        {
            FirstDayRequire(firstDayCompanion && (FirstDayContact - mission.npcPosition).sqrMagnitude > 4, "COMPANION_CLEAR_OF_EMPLOYER");
            if (mission.activity == "deliver") FirstDayRequire((FirstDayContact - mission.destination).sqrMagnitude > 4, "COMPANION_CLEAR_OF_DELIVERY");
        }

        void ClearFirstDayQAPanels()
        {
            if (letterEditor) ClosePersonalLetter();
            firstDayPanel = dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = lifePanel = false;
        }

        WorldSave CheckFirstDayQASave(string label)
        {
            Vector3 feet = player.transform.position;
            var saved = new WorldSave { story = state, missionNode = state.nodeId, layoutVersion = 2,
                progress = progress, carrying = carrying, sideCarrying = sideCarrying, inspected = new List<int>(inspected), px = feet.x, pz = feet.z };
            var restored = JsonUtility.FromJson<WorldSave>(JsonUtility.ToJson(saved));
            FirstDayRequire(WorldSaveStore.IsValid(restored), "VALID_SAVE_" + label);
            FirstDayRequire(restored.story.firstDay.stage == state.firstDay.stage && restored.story.firstDay.choice == state.firstDay.choice &&
                restored.story.money == state.money && restored.progress == progress && restored.carrying == carrying &&
                WorldMissions.Get(restored.story).world == loadedWorld, "ROUNDTRIP_STATE_" + label);
            return restored;
        }

        void ResumeFirstDayQASave(string label)
        {
            WorldSave saved = CheckFirstDayQASave(label);
            ClearFirstDayQAPanels(); state = saved.story; EnterNode(true);
            progress = saved.progress; carrying = saved.carrying; sideCarrying = saved.sideCarrying; inspected = saved.inspected;
            controller.enabled = false;
            player.transform.position = new Vector3(saved.px, Mathf.Max(.05f, player.transform.position.y), saved.pz);
            controller.enabled = true;
            ClearFirstDayQAPanels(); RefreshProps(); FaceCurrentTask(); UpdateCamera(true);
            FirstDayRequire(WorldMissions.Get(state).world == loadedWorld && state.firstDay.stage == saved.story.firstDay.stage, "RESUMED_WORLD_" + label);
            Debug.Log("FIRSTDAY_SAVE_RESUMED " + label + " world=" + loadedWorld + " stage=" + state.firstDay.stage + " progress=" + progress);
        }

        void CheckFirstDayQAIsolation()
        {
            if (firstDayQAPaths == null) return;
            for (int i = 0; i < firstDayQAPaths.Length; i++) FirstDayRequire(firstDayQAFiles[i] == CharacterFileSnapshot(firstDayQAPaths[i]), "REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < firstDayQAKeys.Length; i++) FirstDayRequire(firstDayQAPreferences[i] == PlayerPrefs.GetInt(firstDayQAKeys[i], -1), "PREFERENCE_UNCHANGED_" + firstDayQAKeys[i]);
            FirstDayRequire(firstDayQAMusicLevel == PlayerPrefs.GetFloat("qiaopi-music-level", -1), "MUSIC_LEVEL_UNCHANGED");
            FirstDayRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "ALL_AUDIO_MUTED");
        }
    }
}
