using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        // Opt-in runtime acceptance: synthetic career fixtures, ordinary interaction
        // entry points, real walking, and temporary save files. Never live progress.
        sealed class PerformanceCase
        {
            public string route, choice;
            public int level, money, health, trust;
            public PerformanceCase(string r, int l, string c, int m, int h, int t)
            { route = r; level = l; choice = c; money = m; health = h; trust = t; }
        }
        sealed class PerformancePropObservation
        {
            public bool removingRice, signing, emptySpoonSeen, returningRiceSeen, writerSeen, receiptTaken, lateCapture;
        }
        string performanceQATemp, performanceQAFolder, performanceQARoute;
        string[] performanceQAPaths, performanceQAFiles, performanceQAKeys;
        int[] performanceQAPreferences;
        float performanceQAMusicLevel;
        int performanceQAAssertions, performanceQAActions, performanceQASaves, performanceQACases;
        bool PerformanceShortReview => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-performance-review") >= 0;

        void PerformanceRequire(bool value, string label)
        { performanceQAAssertions++; if (!value) throw new Exception("PERFORMANCE_QA_" + label); }

        IEnumerator RunPerformanceQA()
        {
            var pending = new Stack<IEnumerator>(); pending.Push(RunPerformanceChecks());
            while (pending.Count > 0) {
                object wait = null; Exception failure = null;
                try { if (!pending.Peek().MoveNext()) pending.Pop(); else wait = pending.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null) {
                    while (pending.Count > 0) { try { (pending.Pop() as IDisposable)?.Dispose(); } catch { } }
                    try { CheckPerformanceIsolation(); } catch (Exception error) { Debug.LogError("PERFORMANCE_QA_ISOLATION_FAILED: " + error); }
                    CleanupPerformanceQA(); Debug.LogError("PERFORMANCE_QA_FAILED: " + failure); Application.Quit(1); yield break;
                }
                if (wait is IEnumerator) pending.Push((IEnumerator)wait);
                else if (pending.Count > 0) yield return wait;
            }
            CleanupPerformanceQA();
            Debug.Log((PerformanceShortReview ? "PERFORMANCE_REVIEW_ALL_PASSED" : "PERFORMANCE_RUNTIME_ALL_PASSED") +
                " cases=" + performanceQACases + " animatedActions=" + performanceQAActions + " checkpoints=" + performanceQASaves +
                " assertions=" + performanceQAAssertions + " route=" + performanceQARoute +
                "; synthetic starting states, then real walking and guarded interactions; actor and parcel motion sampled; interrupted presentation never repeats business effects; original saves/preferences and mute preserved.");
            Application.Quit(0);
        }

        IEnumerator RunPerformanceChecks()
        {
            PerformanceRequire(PerformanceQA && QA && DetailQA && !TradeQA && !WorkEventQA && !NanyangQA, "ISOLATED_ENTRY");
            performanceQAPaths = new[] { savePath, savePath + ".bak", savePath + ".tmp" };
            performanceQAFiles = new string[performanceQAPaths.Length];
            for (int i = 0; i < performanceQAPaths.Length; i++) performanceQAFiles[i] = CharacterFileSnapshot(performanceQAPaths[i]);
            performanceQAKeys = new[] { "qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized" };
            performanceQAPreferences = new int[performanceQAKeys.Length];
            for (int i = 0; i < performanceQAKeys.Length; i++) performanceQAPreferences[i] = PlayerPrefs.GetInt(performanceQAKeys[i], -1);
            performanceQAMusicLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            performanceQATemp = Path.Combine(Path.GetTempPath(), "qiaopi-performance-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(performanceQATemp);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false);
            if (voiceSource) voiceSource.Stop(); if (sfx) sfx.Stop();
            performanceQARoute = PerformanceArgument("-qiaopi-performance-route", "all").ToLowerInvariant();
            PerformanceRequire(performanceQARoute == "all" || performanceQARoute == "dock" || performanceQARoute == "shop" || performanceQARoute == "courier", "ROUTE_ARGUMENT");
            string name = (PerformanceShortReview ? "PerformanceShortReview-" : "PerformanceReview-") + Application.version + (MobileControls ? "-mobile" : "-desktop");
            performanceQAFolder = Application.isMobilePlatform ? name : Path.Combine(Path.GetFullPath(PerformanceArgument("-qiaopi-performance-output", Path.Combine(Path.GetTempPath(), "qiaopi-performance-captures"))), name);
            Directory.CreateDirectory(Application.isMobilePlatform ? Path.Combine(Application.persistentDataPath, performanceQAFolder) : performanceQAFolder);
            Debug.Log("PERFORMANCE_QA_OUTPUT=" + performanceQAFolder);
            var cases = new[] {
                new PerformanceCase("dock", 2, "pair", -2, 2, 0), new PerformanceCase("dock", 3, "teach", -1, 1, 2),
                new PerformanceCase("shop", 2, "credit", 0, 0, 1), new PerformanceCase("shop", 3, "record", 1, 0, 2),
                new PerformanceCase("courier", 2, "trace", 0, -2, 2), new PerformanceCase("courier", 2, "return", -6, 3, 0),
                new PerformanceCase("courier", 3, "personal", 0, -1, 2)
            };
            foreach (var test in cases) {
                if (performanceQARoute != "all" && performanceQARoute != test.route || PerformanceShortReview && (test.level != 2 || test.choice == "return")) continue;
                yield return RunPerformanceCase(test);
            }
            CheckPerformanceIsolation();
        }

        IEnumerator RunPerformanceCase(PerformanceCase test)
        {
            string label = test.route + "_" + test.level + "_" + test.choice;
            CancelWorkPerformance(); ClearPerformancePanels();
            state = NanyangQAFixture(WorkEventDestination(test.route), test.route, test.level); EnterNode(true); ResetView(); yield return null;
            int money = state.money, health = state.health, trust = state.trust, turns = state.journey.turns;
            var reward = NanyangLife.WorkReward(state, test.route);
            StartLifeAction(test.route); yield return null;
            PerformanceRequire(InTrade && WorkEvents.HasIncident(state), "EVENT_ENROLLED_" + label);
            if (test.route == "courier") {
                yield return OpenPerformancePanel(TradePlaces.CourierRead, label + " take batch");
                PerformanceRequire(TradeAction("read"), "TAKE_ACCEPTED");
                yield return ObservePerformance("courier-take", "read", 0, label + "_take", false);
            }
            yield return OpenPerformancePanel(WorkEventPoint(), label + " discuss work");
            PerformanceRequire(ChooseWorkEvent(test.choice), "EVENT_CHOICE_" + label); yield return null;
            PerformanceRequire(WorkEvents.ChoiceId(state) == test.choice, "CHOICE_RETAINED");
            if (test.route == "dock") {
                yield return ObservePerformanceCoworker(label);
                yield return PerformanceDock(label);
            } else if (test.route == "shop") yield return PerformanceShop(label, test.level);
            else yield return PerformanceCourier(label, test.choice);
            PerformanceRequire(Complete && !WorkPerformanceBusy, "WORK_COMPLETE_" + label);
            PerformanceRequire(state.money == money && state.health == health && state.trust == trust && state.journey.turns == turns, "NO_EARLY_REWARDS_" + label);
            yield return ReachPerformancePoint(mission.npcPosition, label + " settle", 2.5f);
            Interact(); yield return null;
            PerformanceRequire(state.journey.turns == turns + 1 && string.IsNullOrEmpty(state.journey.pendingJob), "SETTLED_ONCE_" + label);
            PerformanceRequire(state.money == money + reward.money + test.money && state.health == Mathf.Clamp(health + reward.health + test.health, 0, 100) && state.trust == Mathf.Clamp(trust + reward.trust + test.trust, 0, 100), "EXACT_SETTLEMENT_" + label);
            string settled = JsonUtility.ToJson(state); ClearPerformancePanels(); Interact(); yield return null;
            PerformanceRequire(JsonUtility.ToJson(state) == settled, "REPEATED_INTERACTION_CANNOT_PAY_TWICE_" + label);
            ResumePerformanceSave(label + "_settled"); yield return null;
            CheckPerformanceIsolation(); performanceQACases++; Debug.Log("PERFORMANCE_CASE_PASSED " + label);
        }

        IEnumerator PerformanceDock(string label)
        {
            bool interrupted = false;
            for (int count = 0; count < TradeWork.DockCount(state); count++) {
                int item = TradeWork.DockPriority(state);
                if (item < 0) { item = 0; while ((state.tradeShift.deliveredMask & (1 << item)) != 0) item++; }
                int zone = TradeWork.DockDestination(state, item);
                yield return ReachPerformancePoint(TradePlaces.DockPickup(item), label + " pick " + item);
                Interact();
                PerformanceRequire(state.tradeShift.held == item, "DOCK_PICK_COMMITTED");
                yield return ObservePerformance("dock-pick", null, 0, label + "_lift_" + item, false);
                if (!interrupted) {
                    ResumePerformanceSave(label + "_carrying"); yield return null;
                    PerformanceRequire(state.tradeShift.held == item && TradeHeldBox, "HELD_CARGO_RESTORED");
                    PerformanceRequire(workCoworkerPerformance && workCoworkerPerformance.Resumed && workCoworkerPerformance.Completed &&
                        workCoworkerPerformance.Parcel && Vector3.Distance(workCoworkerPerformance.Parcel.position, workCoworkerPerformance.DeliveryPosition) < .15f,
                        "PROGRESSED_LOAD_RECONSTRUCTS_ONE_COMPLETED_COWORKER_DELIVERY");
                    interrupted = true;
                }
                yield return ReachPerformancePoint(TradePlaces.DockDrop(zone), label + " drop " + item);
                Interact();
                PerformanceRequire((state.tradeShift.deliveredMask & (1 << item)) != 0 && state.tradeShift.held == -1, "DOCK_DROP_COMMITTED");
                yield return ObservePerformance("dock-drop", null, 0, label + "_set_down_" + item, false);
            }
        }

        IEnumerator PerformanceShop(string label, int level)
        {
            yield return OpenPerformancePanel(TradePlaces.ShopCounter, label + " weighing");
            string empty = JsonUtility.ToJson(state);
            PerformanceRequire(!TradeAction("confirm-weight") && !WorkPerformanceBusy && empty == JsonUtility.ToJson(state), "EMPTY_SCALE_HAS_NO_SUCCESS_ANIMATION");
            int target = TradeWork.ShopWeightTarget(state);
            PerformanceRequire(TradeAction("add-weight", 1), "FIRST_SCOOP");
            PerformanceRequire(WorkPerformanceBusy && state.tradeShift.weightUnits == 1, "SCOOP_COMMITS_BEFORE_PRESENTATION");
            string committed = JsonUtility.ToJson(state); PerformanceBusySpam("add-weight", 1, committed);
            yield return ObservePerformance("shop-scoop", "add-weight", 1, label + "_scoop", true);
            PerformanceRequire(state.tradeShift.weightUnits == 1 && !WorkPerformanceBusy && !tradePanel, "INTERRUPTED_SCOOP_RESTORED_ONCE");
            yield return OpenPerformancePanel(TradePlaces.ShopCounter, label + " resumed scales");
            while (state.tradeShift.weightUnits < target) {
                PerformanceRequire(TradeAction("add-weight", 1), "ADD_RICE");
                yield return ObservePerformance("shop-scoop", "add-weight", 1, "", false);
            }
            PerformanceRequire(TradeAction("add-weight", -1), "REMOVE_RICE");
            yield return ObservePerformance("shop-scoop", "add-weight", -1, label + "_take_rice_back", false);
            PerformanceRequire(TradeAction("add-weight", 1), "RESTORE_RICE");
            yield return ObservePerformance("shop-scoop", "add-weight", 1, "", false);
            PerformanceRequire(TradeAction("confirm-weight"), "WEIGH_CONFIRMED");
            yield return ObservePerformance("shop-weigh", "confirm-weight", 0, label + "_weigh", false);
            PerformanceRequire(tradePanel && tradePanelStage == "change", "CURRENT_CASH_PANEL_RESTORED");
            // These credit orders have zero change. Put in an extra coin and
            // retrieve it to exercise both directions of the tangible money tray.
            PerformanceRequire(TradeAction("add-change", 1), "ADD_COIN");
            yield return ObservePerformance("shop-coin", "add-change", 1, label + "_coin", false);
            PerformanceRequire(TradeAction("add-change", -1), "TAKE_COIN_BACK");
            yield return ObservePerformance("shop-coin", "add-change", -1, "", false);
            PerformanceRequire(TradeAction("confirm-change"), "CASH_CONFIRMED");
            yield return ObservePerformance("shop-pay", "confirm-change", 0, label + "_pay", false);
            PerformanceRequire(tradePanel && state.tradeShift.stage == "credit", "CREDIT_PANEL_RESTORED");
            int debt = TradeWork.ShopDebtTarget(state);
            for (int i = 0; i < debt; i++) {
                PerformanceRequire(TradeAction("add-debt", 1), "WRITE_DEBT");
                // Individual ledger changes are a displayed figure; signing the
                // completed page is the meaningful physical confirmation.
                if (WorkPerformanceBusy) yield return ObservePerformance("shop-ledger", "add-debt", 1, "", false);
            }
            PerformanceRequire(TradeAction("confirm-debt"), "DEBT_CONFIRMED");
            yield return ObservePerformance("shop-ledger", "confirm-debt", 0, label + "_ledger", false);
            PerformanceRequire(!tradePanel, "COMPLETED_SHOP_DOES_NOT_REOPEN_STALE_PANEL");
        }

        IEnumerator PerformanceCourier(string label, string choice)
        {
            if (choice == "return") {
                yield return OpenPerformancePanel(mission.npcPosition, label + " return counter");
                PerformanceRequire(TradeAction("register-return"), "RETURN_REGISTERED");
                yield return ObservePerformance("courier-return", "register-return", 0, label + "_return", false);
                PerformanceRequire(state.tradeShift.recipient == -1 && Complete, "RETURN_IS_NOT_A_RECEIPT"); yield break;
            }
            yield return OpenPerformancePanel(TradePlaces.CourierNeighbour, label + " ask directions");
            PerformanceRequire(TradeAction("ask"), "ASK_ACCEPTED");
            if (WorkPerformanceBusy) yield return WaitPerformanceEnd();
            yield return OpenPerformancePanel(TradePlaces.CourierRecipient(1), label + " wrong person");
            string wrong = JsonUtility.ToJson(state);
            PerformanceRequire(!TradeAction("check", 1) && !WorkPerformanceBusy && wrong == JsonUtility.ToJson(state), "WRONG_PERSON_HAS_NO_HANDOVER");
            yield return OpenPerformancePanel(TradePlaces.CourierRecipient(0), label + " verified recipient");
            PerformanceRequire(TradeAction("check", 0), "RIGHT_PERSON_CHECKED");
            if (WorkPerformanceBusy) yield return WaitPerformanceEnd();
            yield return OpenPerformancePanel(TradePlaces.CourierRecipient(0), label + " offer batch");
            PerformanceRequire(TradeAction("deliver", 0), "BATCH_HANDED_OVER");
            yield return ObservePerformance("courier-offer", "deliver", 0, label + "_offer", false);
            PerformanceRequire(state.tradeShift.stage == "receipt" && !TradeHeldLetter && !Complete, "LETTER_STAYS_DELIVERED_WAITING_FOR_RECEIPT");
            yield return OpenPerformancePanel(TradePlaces.CourierRecipient(0), label + " signed receipt");
            PerformanceRequire(TradeAction("receipt", 0), "RECEIPT_ACCEPTED");
            if (choice == "personal") {
                PerformanceRequire(WorkPerformanceBusy, "SIGN_ANIMATION_STARTED");
                string signed = JsonUtility.ToJson(state); CancelWorkPerformance();
                PerformanceRequire(!WorkPerformanceBusy && signed == JsonUtility.ToJson(state) && Complete, "CANCEL_SIGN_PRESENTATION_DOES_NOT_ROLL_BACK_RECEIPT");
                yield return new WaitForSeconds(1.5f);
                PerformanceRequire(!WorkPerformanceBusy && !tradePanel && signed == JsonUtility.ToJson(state), "CANCELLED_SIGN_HAS_NO_LATE_CALLBACK");
            } else yield return ObservePerformance("courier-sign", "receipt", 0, label + "_signature", false);
            ResumePerformanceSave(label + "_signed"); yield return null;
        }

        IEnumerator ObservePerformance(string kind, string replay, int amount, string capture, bool reloadMidway)
        {
            PerformanceRequire(WorkPerformanceBusy && WorkPerformanceKind == kind, "STARTED_" + kind);
            PerformanceRequire(!tradePanel && !workTalkPanel, "PRESENTATION_VISIBLE_WITHOUT_WORK_PANEL_" + kind);
            string committed = JsonUtility.ToJson(state); PerformanceBusySpam(replay, amount, committed);
            // During signing the player waits with hands lowered; observe the
            // recipient's real paper instead of demanding raised player arms.
            if (kind == "courier-sign") yield return null;
            Transform visual = kind == "courier-sign" ? workSignaturePaper : WorkPerformanceVisual;
            PerformanceRequire(visual, "HAS_PHYSICAL_HANDS_OR_TOOL_" + kind);
            Vector3 initialPosition = visual.localPosition; Quaternion initialRotation = visual.localRotation;
            float moved = 0, rotated = 0;
            var props = new PerformancePropObservation { removingRice = kind == "shop-scoop" && amount < 0, signing = kind == "courier-sign" };
            // Removing excess starts with an empty spoon. Observe the actual
            // tools, without prescribing an animation curve or a frame number.
            if (props.removingRice) ObservePerformanceProps(props);
            float deadline = Time.realtimeSinceStartup + 5;
            while (WorkPerformanceBusy && WorkPerformanceProgress < .38f && Time.realtimeSinceStartup < deadline) {
                yield return null;
                if (visual) { moved = Mathf.Max(moved, Vector3.Distance(initialPosition, visual.localPosition)); rotated = Mathf.Max(rotated, Quaternion.Angle(initialRotation, visual.localRotation)); }
                ObservePerformanceProps(props);
            }
            PerformanceRequire(WorkPerformanceBusy && WorkPerformanceProgress >= .2f && WorkPerformanceProgress < 1, "HAS_INTERMEDIATE_FRAMES_" + kind);
            bool visibleCamera = kind == "courier-sign" ? cam && cam.enabled : heldCamera && heldCamera.enabled;
            PerformanceRequire(visual && visual.gameObject.activeInHierarchy && visibleCamera && (moved > .008f || rotated > 2f), "VISIBLE_GEOMETRY_MOVES_" + kind);
            if (kind == "courier-sign") {
                var actorPose = workPerformanceActor;
                var arm = actorPose ? ModelLibrary.Find(actorPose.transform, "RightArm") : null;
                Debug.Log("PERFORMANCE_SIGN_RUNTIME_DIAGNOSTIC overlay=" + WorkPerformanceProgress.ToString("F3") +
                    " actor=" + (actorPose ? actorPose.name : "null") + " enabled=" + (actorPose && actorPose.enabled) +
                    " automatic=" + (actorPose && actorPose.Automatic) + " gesture=" + (actorPose ? actorPose.Gesture.ToString() : "null") +
                    " progress=" + (actorPose ? actorPose.Progress.ToString("F3") : "null") + " dt=" + Time.deltaTime.ToString("F4") +
                    " unscaledDt=" + Time.unscaledDeltaTime.ToString("F4") + " arm=" + (arm ? arm.localRotation.eulerAngles.ToString("F2") : "null") +
                    " left=" + (actorPose ? actorPose.transform.InverseTransformPoint(actorPose.LeftHandPoint).ToString("F3") : "null") +
                    " right=" + (actorPose ? actorPose.transform.InverseTransformPoint(actorPose.RightHandPoint).ToString("F3") : "null"));
                PerformanceRequire(actorPose && actorPose.enabled && actorPose.Automatic && actorPose.Gesture == CharacterGesture.Sign && actorPose.Progress > .15f,
                    "RECIPIENT_SIGN_GESTURE_RUNNING_WITH_OVERLAY");
            }
            if (!string.IsNullOrEmpty(capture)) yield return CapturePerformanceFrame(capture);
            PerformanceRequire(JsonUtility.ToJson(state) == committed, "PRESENTATION_DOES_NOT_MUTATE_RULES_" + kind);
            if (reloadMidway) {
                ResumePerformanceSave(capture + "_interrupted");
                PerformanceRequire(!WorkPerformanceBusy, "LOAD_CANCELS_PRESENTATION_" + kind);
                yield return new WaitForSeconds(1.5f);
                PerformanceRequire(!WorkPerformanceBusy && !tradePanel && JsonUtility.ToJson(state) == committed, "NO_DEFERRED_MUTATION_AFTER_LOAD_" + kind);
            } else {
                while (WorkPerformanceBusy && Time.realtimeSinceStartup < deadline) {
                    yield return null; ObservePerformanceProps(props);
                    if (!props.lateCapture && !string.IsNullOrEmpty(capture) &&
                        (props.removingRice && props.returningRiceSeen || props.signing && props.receiptTaken)) {
                        props.lateCapture = true;
                        yield return CapturePerformanceFrame(capture + (props.signing ? "_handover" : "_returning_with_rice"));
                    }
                }
                PerformanceRequire(!WorkPerformanceBusy, "ACTION_FINISHES_WITHIN_FIVE_SECONDS"); yield return null;
                if (props.removingRice) PerformanceRequire(props.emptySpoonSeen && props.returningRiceSeen, "EXCESS_RICE_LEAVES_SCALE_IN_SPOON");
                if (props.signing) PerformanceRequire(props.writerSeen && props.receiptTaken, "RECIPIENT_SIGNS_BEFORE_COURIER_TAKES_RECEIPT");
                PerformanceRequire(JsonUtility.ToJson(state) == committed, "NO_COMPLETION_REWARD_" + kind);
            }
            performanceQAActions++; Debug.Log("PERFORMANCE_ACTION_PASSED " + kind + " interrupted=" + reloadMidway);
        }

        void ObservePerformanceProps(PerformancePropObservation observed)
        {
            if (!WorkPerformanceBusy) return;
            if (observed.removingRice) {
                PerformanceRequire(workHandSpoon && workHandSpoon.gameObject.activeInHierarchy && workSpoonRice, "REMOVAL_HAS_A_VISIBLE_SPOON");
                bool full = workSpoonRice.gameObject.activeInHierarchy;
                if (!full && !observed.returningRiceSeen) observed.emptySpoonSeen = true;
                if (full) {
                    PerformanceRequire(observed.emptySpoonSeen, "REMOVAL_REACHES_WITH_EMPTY_SPOON_BEFORE_COLLECTING_RICE");
                    observed.returningRiceSeen = true;
                }
                foreach (var grain in workRiceDrops)
                    PerformanceRequire(!grain || !grain.gameObject.activeInHierarchy, "REMOVING_RICE_DOES_NOT_POUR_GRAINS_BACK_INTO_SCALE");
            }
            if (observed.signing) {
                PerformanceRequire(!workHandBrush || !workHandBrush.gameObject.activeInHierarchy, "COURIER_NEVER_SIGNS_FOR_RECIPIENT");
                bool paper = workSignaturePaper && workSignaturePaper.gameObject.activeInHierarchy;
                bool pen = workSignaturePen && workSignaturePen.gameObject.activeInHierarchy;
                bool taken = workHandReceipt && workHandReceipt.gameObject.activeInHierarchy;
                PerformanceRequire(!heldLetter || !heldLetter.activeInHierarchy, "RETURNED_RECEIPT_IS_NOT_THE_DELIVERED_BATCH");
                PerformanceRequire(!(paper && taken), "ONE_RECEIPT_CANNOT_BE_IN_BOTH_PEOPLES_HANDS");
                PerformanceRequire(!pen || paper, "RECIPIENT_WRITES_ONLY_ON_PRESENT_PAPER");
                if (paper && pen) observed.writerSeen = true;
                if (taken) {
                    PerformanceRequire(observed.writerSeen && !pen, "RECEIPT_IS_TAKEN_AFTER_RECIPIENT_STOPS_WRITING");
                    observed.receiptTaken = true;
                }
            }
        }

        void PerformanceBusySpam(string action, int amount, string committed)
        {
            PerformanceRequire(WorkPerformanceBusy, "BUSY_BEFORE_REPEATED_INPUT");
            string kind = WorkPerformanceKind;
            for (int i = 0; i < 3; i++) {
                if (action != null) PerformanceRequire(!TradeAction(action, amount), "BUSY_REJECTS_ACTION_" + action);
                Interact();
                PerformanceRequire(JsonUtility.ToJson(state) == committed, "BUSY_REPEATED_E_UNCHANGED");
            }
            PerformanceRequire(!TradeAction("add-weight", -1) && !TradeAction("deliver", 1) && !ChooseWorkEvent("solo"), "BUSY_CANNOT_SWITCH_TOOL_RECIPIENT_OR_CHOICE");
            PerformanceRequire(WorkPerformanceBusy && WorkPerformanceKind == kind && JsonUtility.ToJson(state) == committed, "BUSY_KEEPS_ORIGINAL_ACTION_AND_CONTENTS");
        }

        IEnumerator WaitPerformanceEnd()
        {
            float deadline = Time.realtimeSinceStartup + 6;
            while (WorkPerformanceBusy && Time.realtimeSinceStartup < deadline) yield return null;
            PerformanceRequire(!WorkPerformanceBusy, "ACTION_FINISHES_WITHIN_SIX_SECONDS"); yield return null;
        }

        IEnumerator ObservePerformanceCoworker(string label)
        {
            PerformanceRequire(workCoworkerPerformance && workEventPartner, "COWORKER_EXISTS_" + label);
            var worker = workCoworkerPerformance; string unchanged = JsonUtility.ToJson(state);
            Vector3 start = worker.transform.position, previous = start;
            float walked = 0, deadline = Time.realtimeSinceStartup + 55; bool carried = false, changedPose = false;
            var arm = ModelLibrary.Find(worker.transform, "RightArm"); Quaternion baseArm = arm ? arm.localRotation : Quaternion.identity;
            while (worker && !worker.Completed && Time.realtimeSinceStartup < deadline) {
                yield return null;
                Vector3 position = worker.transform.position; float step = Vector3.Distance(previous, position);
                PerformanceRequire(step < .45f + Time.deltaTime * 3, "COWORKER_NO_TELEPORT"); walked += step; previous = position;
                if (arm && Quaternion.Angle(baseArm, arm.localRotation) > 5) changedPose = true;
                if (worker.Carrying && !carried) {
                    carried = true;
                    PerformanceRequire(worker.Parcel && worker.Parcel.gameObject.activeInHierarchy && Vector3.Distance(worker.Parcel.position, worker.transform.position) < 2.5f, "COWORKER_CARRIES_REAL_PARCEL");
                    AimNanyangQACamera(worker.transform.position + Vector3.up); yield return CapturePerformanceFrame(label + "_coworker_carrying");
                }
                PerformanceRequire(JsonUtility.ToJson(state) == unchanged, "COWORKER_DOES_NOT_ADVANCE_PLAYER_WORK");
            }
            PerformanceRequire(worker && worker.Completed && !worker.Carrying, "COWORKER_COMPLETED_PATH_" + label);
            PerformanceRequire(carried && changedPose && walked > 3 && worker.DistanceWalked > 3, "COWORKER_WALKED_AND_LIFTED");
            PerformanceRequire(worker.Parcel && Vector3.Distance(worker.Parcel.position, worker.DeliveryPosition) < .15f, "CLOTH_RESTS_AT_DELIVERY");
            AimNanyangQACamera(worker.Parcel.position + Vector3.up * .3f); yield return CapturePerformanceFrame(label + "_cloth_delivered");
            Debug.Log("PERFORMANCE_COWORKER_PASSED " + label + " sampledDistance=" + walked + " reportedDistance=" + worker.DistanceWalked);
        }

        IEnumerator OpenPerformancePanel(Vector3 point, string label)
        {
            yield return ReachPerformancePoint(point, label); Interact(); yield return null;
            PerformanceRequire(tradePanel, "PANEL_OPEN_" + label);
        }
        IEnumerator ReachPerformancePoint(Vector3 point, string label, float range = 2.2f)
        {
            PerformanceRequire(!WorkPerformanceBusy, "WALK_AFTER_PRESENTATION_" + label);
            ClearPerformancePanels(); yield return WalkFirstPerson(point, label);
            if (FirstDayCanReach(point, range)) yield break;
            foreach (Vector3 offset in new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward }) {
                Vector3 alternate = point + offset;
                if (!region.IsGround(alternate) || Physics.Linecast(alternate + Vector3.up * .9f, point + Vector3.up * .9f, ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore)) continue;
                if (WalkPath.Find(player.transform.position, alternate, loadedWorld).Count == 0 && Dist(alternate) > 2) continue;
                yield return WalkFirstPerson(alternate, label + " alternate"); if (FirstDayCanReach(point, range)) yield break;
            }
            PerformanceRequire(false, "STATION_REACHABLE_" + label);
        }
        void ClearPerformancePanels()
        {
            if (letterEditor) ClosePersonalLetter();
            tradePanel = workTalkPanel = firstDayPanel = nanyangPanel = dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = lifePanel = false;
            walkRoute.Clear(); routeIndex = 0;
        }
        void ResumePerformanceSave(string label)
        {
            Vector3 feet = player.transform.position;
            var snapshot = new WorldSave { story = state, missionNode = state.nodeId, layoutVersion = 2, progress = progress, carrying = carrying, sideCarrying = sideCarrying, inspected = new List<int>(inspected), px = feet.x, pz = feet.z };
            string json = JsonUtility.ToJson(state); PerformanceRequire(WorldSaveStore.IsValid(snapshot), "VALID_CHECKPOINT_" + label);
            string path = Path.Combine(performanceQATemp, "checkpoint.json"); WorldSaveStore.Write(path, snapshot);
            bool recovered; WorldSave loaded = WorldSaveStore.Load(path, out recovered);
            PerformanceRequire(loaded != null && !recovered && json == JsonUtility.ToJson(loaded.story), "EXACT_DISK_ROUNDTRIP_" + label);
            ClearPerformancePanels(); state = loaded.story; EnterNode(true);
            progress = loaded.progress; carrying = loaded.carrying; sideCarrying = loaded.sideCarrying; inspected = loaded.inspected;
            controller.enabled = false; player.transform.position = new Vector3(loaded.px, Mathf.Max(.05f, player.transform.position.y), loaded.pz); controller.enabled = true;
            ClearPerformancePanels(); RefreshProps(); FaceCurrentTask(); UpdateCamera(true);
            PerformanceRequire(!WorkPerformanceBusy && StoryEngine.Validate(state) && json == JsonUtility.ToJson(state), "NO_TRANSIENT_ANIMATION_IN_SAVE_" + label);
            performanceQASaves++; Debug.Log("PERFORMANCE_CHECKPOINT_RESUMED " + label);
        }
        IEnumerator CapturePerformanceFrame(string label)
        {
            yield return new WaitForEndOfFrame();
            string path = performanceQAFolder + "/" + label + ".png";
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform ? path : Path.GetFullPath(path));
            yield return null;
        }
        string PerformanceArgument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key); if (at < 0) return fallback;
            PerformanceRequire(Array.LastIndexOf(args, key) == at && at + 1 < args.Length && !string.IsNullOrWhiteSpace(args[at + 1]) && !args[at + 1].StartsWith("-", StringComparison.Ordinal), "ARGUMENT_" + key); return args[at + 1];
        }
        void CheckPerformanceIsolation()
        {
            if (performanceQAPaths == null) return;
            for (int i = 0; i < performanceQAPaths.Length; i++) PerformanceRequire(performanceQAFiles[i] == CharacterFileSnapshot(performanceQAPaths[i]), "REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < performanceQAKeys.Length; i++) PerformanceRequire(performanceQAPreferences[i] == PlayerPrefs.GetInt(performanceQAKeys[i], -1), "PREFERENCES_UNCHANGED_" + performanceQAKeys[i]);
            PerformanceRequire(performanceQAMusicLevel == PlayerPrefs.GetFloat("qiaopi-music-level", -1), "MUSIC_LEVEL_UNCHANGED");
            PerformanceRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "ALL_AUDIO_MUTED");
        }
        void CleanupPerformanceQA()
        {
            CancelWorkPerformance();
            if (!string.IsNullOrEmpty(performanceQATemp) && Directory.Exists(performanceQATemp)) {
                try { Directory.Delete(performanceQATemp, true); } catch (Exception error) { Debug.LogWarning("PERFORMANCE_QA_CLEANUP: " + error.Message); }
            }
        }
    }
}
