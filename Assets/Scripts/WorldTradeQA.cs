using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        string[] tradeQAPaths, tradeQAFiles, tradeQAKeys;
        int[] tradeQAPreferences;
        float tradeQAMusicLevel;
        string tradeQATempFolder, tradeQARoute = "";
        int tradeQASaves, tradeQAAssertions, tradeQACases;
        bool TradeShortReview => TradeQA && Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-trade-review") >= 0;

        void TradeRequire(bool condition, string label)
        {
            tradeQAAssertions++;
            if (!condition) throw new Exception("TRADE_QA_" + label);
        }

        IEnumerator RunTradeQA()
        {
            var pending = new Stack<IEnumerator>(); pending.Push(RunTradeChecks());
            while (pending.Count > 0) {
                object wait = null; Exception failure = null;
                try {
                    if (!pending.Peek().MoveNext()) pending.Pop();
                    else wait = pending.Peek().Current;
                } catch (Exception error) { failure = error; }
                if (failure != null) {
                    while (pending.Count > 0) {
                        try { (pending.Pop() as IDisposable)?.Dispose(); } catch (Exception cleanup) { Debug.LogError("TRADE_QA_ITERATOR_CLEANUP: " + cleanup); }
                    }
                    try { CheckTradeQAIsolation(); } catch (Exception isolation) { Debug.LogError("TRADE_QA_ISOLATION_FAILED: " + isolation); }
                    CleanupTradeQA(); Debug.LogError("TRADE_QA_FAILED: " + failure); Application.Quit(1); yield break;
                }
                if (wait is IEnumerator) pending.Push((IEnumerator)wait);
                else if (pending.Count > 0) yield return wait;
            }
            CleanupTradeQA();
            Debug.Log((TradeShortReview ? "TRADE_REVIEW_ALL_PASSED" : "TRADE_RUNTIME_ALL_PASSED") +
                ": synthetic career starting fixtures; actual first-person work, mistaken actions, employer payment, abandonment, isolated disk save/reload, original saves/preferences unchanged and audio muted. Career cases=" +
                tradeQACases + " routeFilter=" + (string.IsNullOrEmpty(tradeQARoute) ? "all" : tradeQARoute) + " checkpoints=" + tradeQASaves + " assertions=" + tradeQAAssertions);
            Application.Quit(0);
        }

        IEnumerator RunTradeChecks()
        {
            TradeRequire(TradeQA && QA && DetailQA, "ISOLATED_ENTRY");
            tradeQAPaths = new[] { savePath, savePath + ".bak", savePath + ".tmp" };
            tradeQAFiles = new string[tradeQAPaths.Length];
            for (int i = 0; i < tradeQAPaths.Length; i++) tradeQAFiles[i] = CharacterFileSnapshot(tradeQAPaths[i]);
            tradeQAKeys = new[] { "qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized" };
            tradeQAPreferences = new int[tradeQAKeys.Length];
            for (int i = 0; i < tradeQAKeys.Length; i++) tradeQAPreferences[i] = PlayerPrefs.GetInt(tradeQAKeys[i], -1);
            tradeQAMusicLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            tradeQATempFolder = Path.Combine(Path.GetTempPath(), "qiaopi-trade-runtime-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tradeQATempFolder);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false);
            if (voiceSource) voiceSource.Stop(); if (sfx) sfx.Stop();
            string folderName = (TradeShortReview ? "TradeShortReview-" : "TradeReview-") + Application.version + (MobileControls ? "-mobile" : "-desktop");
            // Desktop QA defaults to an isolated temp directory. The command-line
            // override lets a restricted runner keep every capture in its workspace.
            // CaptureWorld preserves absolute desktop paths; iOS needs a relative one.
            string folder = Application.isMobilePlatform ? folderName : TradeQAOutputFolder(folderName);
            string output = Application.isMobilePlatform ? Path.Combine(Application.persistentDataPath, folder) : folder;
            Directory.CreateDirectory(output);
            Debug.Log("TRADE_QA_OUTPUT=" + output);
            tradeQARoute = TradeQARequestedRoute();
            Debug.Log("TRADE_QA_FILTER route=" + (string.IsNullOrEmpty(tradeQARoute) ? "all" : tradeQARoute) + " levels=1.." + (TradeShortReview ? 1 : 3));
            string[] routes = { "dock", "shop", "courier" }, destinations = { "singapore", "penang", "rangoon" };
            for (int r = 0; r < routes.Length; r++) {
                string route = routes[r];
                if (!string.IsNullOrEmpty(tradeQARoute) && route != tradeQARoute) continue;
                for (int level = 1; level <= (TradeShortReview ? 1 : 3); level++) {
                    ClearTradeQAPanels(); state = NanyangQAFixture(destinations[r], route, level);
                    EnterNode(true); ResetView(); yield return null;
                    TradeRequire(StoryEngine.Validate(state), "VALID_STARTING_FIXTURE_" + route + "_" + level);
                    int money = state.money, health = state.health, trust = state.trust, turns = state.journey.turns;
                    int decisions = state.decisions, histories = state.history.Count;
                    var reward = NanyangLife.WorkReward(state, route);
                    StartLifeAction(route); yield return null;
                    TradeRequire(InTrade && state.journey.pendingJob == route && state.tradeShift.level == level && mission.activity == "trade", "NEW_PHYSICAL_SHIFT_" + route + "_" + level);
                    TradeRequire(!Complete && progress == 0 && !carrying && !sideCarrying && inspected.Count == 0, "SEPARATE_TRADE_PROGRESS");
                    ResumeTradeQASave(route + "_" + level + "_start"); yield return null;
                    string start = JsonUtility.ToJson(state);
                    TradeRequire(!TradeAction("confirm-weight") && start == JsonUtility.ToJson(state), "CLOSED_PANEL_NO_ACTION");
                    bool distant = FirstDayCanReach(mission.npcPosition, 2.5f) == false;
                    foreach (Vector3 station in TradePlaces.ForWorld(loadedWorld)) distant &= !FirstDayCanReach(station, 2.2f);
                    if (distant) {
                        Interact(); yield return null;
                        TradeRequire(start == JsonUtility.ToJson(state) && !tradePanel, "DISTANT_E_CANNOT_WORK_" + route);
                    }
                    if (route == "shop" && !FirstDayCanReach(TradePlaces.ShopCounter, 2.2f)) {
                        tradePanel = true; tradePanelStage = state.tradeShift.stage;
                        TradeRequire(!TradeAction("add-weight", 1) && start == JsonUtility.ToJson(state), "CANNOT_WEIGH_AWAY_FROM_COUNTER");
                        tradePanel = false;
                    }
                    var unsettled = JsonUtility.FromJson<GameState>(start); string ignored;
                    TradeRequire(!LifeJourney.CommitAction(unsettled, route, out ignored) && start == JsonUtility.ToJson(unsettled), "RULES_REJECT_UNFINISHED_SETTLEMENT");
                    if (level == 1) {
                        yield return ReachTradeQAPoint(mission.npcPosition, route + " early employer", 2.5f);
                        Interact(); yield return null;
                        TradeRequire(state.money == money && state.health == health && state.trust == trust && state.journey.turns == turns && !Complete, "EARLY_EMPLOYER_NO_REWARD_" + route);
                        ClearTradeQAPanels();
                    }
                    string prefix = folder + "/" + route + "_level_" + level;
                    if (route == "dock") yield return TradeQADock(prefix);
                    else if (route == "shop") yield return TradeQAShop(prefix);
                    else yield return TradeQACourier(prefix);
                    TradeRequire(InTrade && Complete && TradeWork.IsComplete(state), "PHYSICAL_WORK_DONE_" + route + "_" + level);
                    TradeRequire(state.money == money && state.health == health && state.trust == trust && state.journey.turns == turns && state.decisions == decisions && state.history.Count == histories, "WORK_HAS_NO_EARLY_REWARDS");
                    ResumeTradeQASave(route + "_" + level + "_done_before_pay"); yield return null;
                    yield return ReachTradeQAPoint(mission.npcPosition, route + " completed employer", 2.5f);
                    Interact(); yield return null;
                    TradeRequire(!InTrade && string.IsNullOrEmpty(state.journey.pendingJob) && state.journey.turns == turns + 1, "SINGLE_SETTLEMENT_" + route + "_" + level);
                    TradeRequire(state.money == money + reward.money - (level == 1 ? 4 : 0) && state.health == Mathf.Clamp(health + reward.health, 0, 100) && state.trust == Mathf.Clamp(trust + reward.trust, 0, 100), "EXACT_SETTLEMENT_" + route + "_" + level);
                    TradeRequire(NanyangLife.WorkCount(state, route) == level && state.decisions == decisions && state.history.Count == histories, "CAREER_RECORD_WITHOUT_MAIN_STORY_COUNTERS");
                    if (level == 1) TradeRequire(NanyangStage == "choice" && state.nanyang.owed == 4, "FIRST_WAGES_CONTINUE_DISPUTE");
                    ResumeTradeQASave(route + "_" + level + "_settled"); yield return null;
                    int paid = state.money, paidTurns = state.journey.turns;
                    ClearTradeQAPanels(); Interact(); yield return null;
                    TradeRequire(state.money == paid && state.journey.turns == paidTurns, "REPEATED_INTERACT_NO_DUPLICATE_PAY");
                    CheckTradeQAIsolation();
                    tradeQACases++;
                    Debug.Log("TRADE_CAREER_PASSED route=" + route + " level=" + level + " destination=" + destinations[r] + " actualMovement=true actualEmployer=true");
                }
                yield return TradeQAAbandon(route, destinations[r]);
            }
            CheckTradeQAIsolation();
        }

        IEnumerator TradeQADock(string prefix)
        {
            int level = state.tradeShift.level, priority = TradeWork.DockPriority(state);
            if (priority >= 0) {
                int wrong = priority == 0 ? 1 : 0;
                yield return ReachTradeQAPoint(TradePlaces.DockPickup(wrong), "dock non-priority cargo");
                string before = JsonUtility.ToJson(state); Interact(); yield return null;
                TradeRequire(state.tradeShift.held == -1 && before == JsonUtility.ToJson(state), "PAPER_CARGO_PRIORITY_REQUIRED_" + level);
            }
            int count = TradeWork.DockCount(state);
            for (int delivered = 0; delivered < count; delivered++) {
                int item = TradeWork.DockPriority(state);
                if (item < 0) { item = 0; while ((state.tradeShift.deliveredMask & (1 << item)) != 0) item++; }
                int zone = TradeWork.DockDestination(state, item);
                yield return ReachTradeQAPoint(TradePlaces.DockPickup(item), "dock pickup " + item);
                Interact(); yield return null;
                TradeRequire(state.tradeShift.held == item && !carrying && !sideCarrying, "CARGO_IN_TRADE_HANDS");
                if (level == 1 && delivered == 0) {
                    OpenPersonalLetter(); yield return null;
                    TradeRequire(!letterEditor && !writingRequested && state.tradeShift.held == item, "SET_DOWN_CARGO_BEFORE_WRITING");
                }
                if (delivered == 0) { AimNanyangQACamera(TradePlaces.DockDropFace(zone)); yield return CaptureWorld(prefix + "_cargo_in_hands"); }
                ResumeTradeQASave("dock_" + level + "_held_" + item); yield return null;
                TradeRequire(state.tradeShift.held == item, "CARRIED_CARGO_RESTORED");
                if (delivered == 0) {
                    int wrong = (zone + 1) % 3;
                    yield return ReachTradeQAPoint(TradePlaces.DockDrop(wrong), "dock incorrect warehouse");
                    string before = JsonUtility.ToJson(state); Interact(); yield return null;
                    TradeRequire(before == JsonUtility.ToJson(state) && state.tradeShift.held == item, "WRONG_WAREHOUSE_KEEPS_CARGO");
                }
                yield return ReachTradeQAPoint(TradePlaces.DockDrop(zone), "dock matching warehouse " + zone);
                Interact(); yield return null;
                TradeRequire(state.tradeShift.held == -1 && (state.tradeShift.deliveredMask & (1 << item)) != 0, "CARGO_DELIVERED_TO_MATCHING_ZONE");
                ResumeTradeQASave("dock_" + level + "_delivered_" + item); yield return null;
            }
            AimNanyangQACamera(TradePlaces.DockDropFace(2)); yield return CaptureWorld(prefix + "_sorted_warehouses");
        }

        IEnumerator TradeQAShop(string prefix)
        {
            int level = state.tradeShift.level;
            yield return OpenTradeQAPanel(TradePlaces.ShopCounter, "shop scales");
            TradeQAReject("confirm-weight", 0, "EMPTY_SCALE_REJECTED");
            int target = TradeWork.ShopWeightTarget(state);
            for (int i = 0; i < target + 1; i++) TradeRequire(TradeAction("add-weight", 1), "ADD_WEIGHT");
            TradeQAReject("confirm-weight", 0, "OVERWEIGHT_REJECTED");
            TradeRequire(TradeAction("add-weight", -1) && state.tradeShift.weightUnits == target, "REMOVE_WEIGHT");
            yield return CaptureWorld(prefix + "_weighing");
            ResumeTradeQASave("shop_" + level + "_balanced_scale"); yield return null;
            yield return OpenTradeQAPanel(TradePlaces.ShopCounter, "shop restored scales");
            TradeRequire(TradeAction("confirm-weight") && state.tradeShift.stage == "change", "WEIGHING_CONFIRMED");
            int change = TradeWork.ShopChangeTarget(state);
            if (change > 0) TradeQAReject("confirm-change", 0, "MISSING_CHANGE_REJECTED");
            for (int i = 0; i < change + 1; i++) TradeRequire(TradeAction("add-change", 1), "ADD_CHANGE");
            TradeQAReject("confirm-change", 0, "EXCESS_CHANGE_REJECTED");
            TradeRequire(TradeAction("add-change", -1) && state.tradeShift.change == change, "REMOVE_CHANGE");
            yield return CaptureWorld(prefix + "_change");
            ResumeTradeQASave("shop_" + level + "_change_counted"); yield return null;
            yield return OpenTradeQAPanel(TradePlaces.ShopCounter, "shop change handover");
            TradeRequire(TradeAction("confirm-change"), "CHANGE_CONFIRMED");
            if (level == 3) {
                TradeRequire(state.tradeShift.stage == "credit", "SEPARATE_CREDIT_BOOK");
                TradeQAReject("confirm-debt", 0, "UNRECORDED_DEBT_REJECTED");
                for (int i = 0; i < 5; i++) TradeRequire(TradeAction("add-debt", 1), "ADD_TOO_MUCH_DEBT");
                TradeQAReject("confirm-debt", 0, "EXCESS_DEBT_REJECTED");
                TradeRequire(TradeAction("add-debt", -1) && TradeAction("add-debt", -1), "CORRECT_DEBT_AMOUNT");
                yield return CaptureWorld(prefix + "_credit_ledger");
                ResumeTradeQASave("shop_3_debt_recorded"); yield return null;
                yield return OpenTradeQAPanel(TradePlaces.ShopCounter, "shop credit book close");
                TradeRequire(TradeAction("confirm-debt") && Complete, "CREDIT_CONFIRMED");
            }
            ClearTradeQAPanels();
        }

        IEnumerator TradeQACourier(string prefix)
        {
            int level = state.tradeShift.level, recipient = TradeWork.CourierRecipient(state), wrong = 1 - recipient;
            yield return OpenTradeQAPanel(TradePlaces.CourierRead, "courier read addressed envelope");
            TradeQAReject("deliver", recipient, "CANNOT_DELIVER_UNREAD_LETTER");
            yield return CaptureWorld(prefix + "_envelope");
            TradeRequire(TradeAction("read") && state.tradeShift.stage == "ask", "ENVELOPE_READ");
            if (level == 1) {
                OpenPersonalLetter(); yield return null;
                TradeRequire(!letterEditor && !writingRequested && TradeHeldLetter, "DELIVER_BATCH_BEFORE_PERSONAL_WRITING");
            }
            ResumeTradeQASave("courier_" + level + "_read"); yield return null;
            ClearTradeQAPanels();
            string away = JsonUtility.ToJson(state); tradePanel = true; tradePanelStage = state.tradeShift.stage; tradePerson = -1;
            TradeRequire(!TradeAction("ask") && away == JsonUtility.ToJson(state), "CANNOT_ASK_NEIGHBOUR_FROM_POSTOFFICE");
            tradePanel = false;
            yield return OpenTradeQAPanel(TradePlaces.CourierNeighbour, "courier neighbourhood directions");
            TradeRequire(TradeAction("ask") && state.tradeShift.stage == "verify", "NEIGHBOUR_GIVES_DIRECTIONS");
            ResumeTradeQASave("courier_" + level + "_directions"); yield return null;
            yield return OpenTradeQAPanel(TradePlaces.CourierRecipient(wrong), "courier same name wrong address");
            TradeQAReject("check", wrong, "SAME_NAME_WRONG_DOOR_KEEPS_LETTER");
            yield return CaptureWorld(prefix + "_wrong_address");
            yield return OpenTradeQAPanel(TradePlaces.CourierRecipient(recipient), "courier correct address");
            TradeRequire(TradeAction("check", recipient) && state.tradeShift.stage == "deliver" && state.tradeShift.recipient == recipient, "RECIPIENT_CONFIRMED_BY_ADDRESS");
            ResumeTradeQASave("courier_" + level + "_identity_checked"); yield return null;
            if (level == 1) {
                yield return OpenTradeQAPanel(TradePlaces.CourierRecipient(wrong), "courier cannot switch recipient after checking");
                TradeQAReject("deliver", wrong, "VERIFIED_LETTER_CANNOT_BE_GIVEN_TO_OTHER_PERSON");
                yield return CaptureWorld(prefix + "_wrong_person_after_checking");
            }
            yield return OpenTradeQAPanel(TradePlaces.CourierRecipient(recipient), "courier recipient handover");
            TradeQAReject("receipt", recipient, "NO_RECEIPT_BEFORE_DELIVERY");
            TradeRequire(TradeAction("deliver", recipient) && state.tradeShift.stage == "receipt" && !Complete, "HANDOVER_REQUIRES_RECEIPT");
            ResumeTradeQASave("courier_" + level + "_delivered_without_receipt"); yield return null;
            if (level == 1) {
                yield return OpenTradeQAPanel(TradePlaces.CourierRecipient(wrong), "courier cannot take another person's receipt");
                TradeQAReject("receipt", wrong, "RECEIPT_MUST_BE_FROM_ACTUAL_RECIPIENT");
            }
            yield return OpenTradeQAPanel(TradePlaces.CourierRecipient(recipient), "courier collect signed receipt");
            AimNanyangQACamera(TradePlaces.CourierFace(recipient)); yield return CaptureWorld(prefix + "_receipt");
            TradeRequire(TradeAction("receipt", recipient) && Complete, "SIGNED_RECEIPT_COLLECTED");
            ClearTradeQAPanels();
        }

        IEnumerator TradeQAAbandon(string route, string destination)
        {
            ClearTradeQAPanels(); state = NanyangQAFixture(destination, route, 1); EnterNode(true); yield return null;
            int money = state.money, health = state.health, trust = state.trust, turns = state.journey.turns;
            StartLifeAction(route); yield return null;
            if (route == "dock") {
                yield return ReachTradeQAPoint(TradePlaces.DockPickup(0), "abandon carried cargo"); Interact(); yield return null;
                TradeRequire(state.tradeShift.held == 0, "ABANDON_CARGO_STARTED");
            } else {
                yield return OpenTradeQAPanel(route == "shop" ? TradePlaces.ShopCounter : TradePlaces.CourierRead, "abandon started " + route);
                TradeRequire(TradeAction(route == "shop" ? "add-weight" : "read", route == "shop" ? 1 : 0), "ABANDON_PARTIAL_WORK_STARTED");
            }
            ResumeTradeQASave(route + "_before_abandon"); yield return null;
            ClearTradeQAPanels(); lifePanel = true; AbandonTradeJob(); yield return null;
            TradeRequire(!InTrade && !TradeWork.IsActive(state) && string.IsNullOrEmpty(state.journey.pendingJob), "ABANDON_REMOVES_SHIFT_" + route);
            TradeRequire(state.money == money && state.health == health && state.trust == trust && state.journey.turns == turns && NanyangLife.WorkCount(state, route) == 0, "ABANDON_NO_REWARD_OR_PENALTY_" + route);
            ResumeTradeQASave(route + "_after_abandon"); yield return null;
            ClearTradeQAPanels(); StartLifeAction(route); yield return null;
            TradeRequire(InTrade && !Complete && state.tradeShift.deliveredMask == 0 && state.tradeShift.held == -1 && state.tradeShift.weightUnits == 0 && state.tradeShift.change == 0 && state.tradeShift.debt == 0 && state.tradeShift.recipient == -1, "NEW_SHIFT_AFTER_ABANDON_IS_FRESH_" + route);
            ClearTradeQAPanels(); lifePanel = true; AbandonTradeJob(); yield return null;
            CheckTradeQAIsolation(); Debug.Log("TRADE_ABANDON_PASSED route=" + route + " partialWorkReloaded=true noRewards=true freshRestart=true");
        }

        void TradeQAReject(string action, int amount, string label)
        {
            string before = JsonUtility.ToJson(state);
            TradeRequire(!TradeAction(action, amount) && before == JsonUtility.ToJson(state), label);
        }
        string TradeQAOutputFolder(string folderName)
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-qiaopi-trade-output");
            if (at >= 0) {
                TradeRequire(at + 1 < args.Length && !string.IsNullOrWhiteSpace(args[at + 1]) && !args[at + 1].StartsWith("-", StringComparison.Ordinal), "OUTPUT_DIRECTORY_ARGUMENT");
                return Path.Combine(Path.GetFullPath(args[at + 1]), folderName);
            }
            return Path.Combine(Path.GetTempPath(), "qiaopi-trade-captures", folderName);
        }
        string TradeQARequestedRoute()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-qiaopi-trade-route");
            if (at < 0) return "";
            TradeRequire(Array.LastIndexOf(args, "-qiaopi-trade-route") == at, "DUPLICATE_ROUTE_ARGUMENT_USE_ONCE");
            TradeRequire(at + 1 < args.Length && !string.IsNullOrWhiteSpace(args[at + 1]), "MISSING_ROUTE_ARGUMENT_EXPECTED_dock_shop_courier");
            string route = args[at + 1].ToLowerInvariant();
            TradeRequire(route == "dock" || route == "shop" || route == "courier", "INVALID_ROUTE_" + route + "_EXPECTED_dock_shop_courier");
            return route;
        }
        IEnumerator OpenTradeQAPanel(Vector3 point, string label)
        {
            yield return ReachTradeQAPoint(point, label);
            Interact(); yield return null;
            TradeRequire(tradePanel, "PANEL_OPENS_" + label);
        }
        IEnumerator ReachTradeQAPoint(Vector3 point, string label, float reach = 2.2f)
        {
            ClearTradeQAPanels(); yield return WalkFirstPerson(point, label);
            if (FirstDayCanReach(point, reach)) yield break;
            foreach (Vector3 offset in new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward }) {
                Vector3 alternative = point + offset;
                if (!region.IsGround(alternative) || Physics.Linecast(alternative + Vector3.up * .9f, point + Vector3.up * .9f,
                    ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore)) continue;
                if (WalkPath.Find(player.transform.position, alternative, loadedWorld).Count == 0 && Dist(alternative) > 2) continue;
                yield return WalkFirstPerson(alternative, label + " alternate approach");
                if (FirstDayCanReach(point, reach)) yield break;
            }
            TradeRequire(false, "UNREACHABLE_STATION_" + label + " feet=" + player.transform.position + " target=" + point);
        }
        void ClearTradeQAPanels()
        {
            if (letterEditor) ClosePersonalLetter();
            tradePanel = firstDayPanel = nanyangPanel = dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = lifePanel = false;
            walkRoute.Clear(); routeIndex = 0;
        }
        void ResumeTradeQASave(string label)
        {
            Vector3 feet = player.transform.position;
            var saved = new WorldSave { story = state, missionNode = state.nodeId, layoutVersion = 2, progress = progress,
                carrying = carrying, sideCarrying = sideCarrying, inspected = new List<int>(inspected), px = feet.x, pz = feet.z };
            string json = JsonUtility.ToJson(saved), storyBefore = JsonUtility.ToJson(state);
            TradeRequire(WorldSaveStore.IsValid(saved), "VALID_CHECKPOINT_" + label);
            if (InTrade) {
                var inconsistent = JsonUtility.FromJson<WorldSave>(json); inconsistent.progress = 1;
                TradeRequire(!WorldSaveStore.IsValid(inconsistent), "TRADE_CANNOT_BYPASS_WITH_OLD_PROGRESS");
                inconsistent = JsonUtility.FromJson<WorldSave>(json); inconsistent.carrying = true;
                TradeRequire(!WorldSaveStore.IsValid(inconsistent), "TRADE_CANNOT_USE_LEGACY_CARRYING");
            }
            string path = Path.Combine(tradeQATempFolder, "checkpoint.json");
            WorldSaveStore.Write(path, JsonUtility.FromJson<WorldSave>(json));
            bool recovered; WorldSave restored = WorldSaveStore.Load(path, out recovered);
            TradeRequire(restored != null && !recovered && WorldSaveStore.IsValid(restored) && storyBefore == JsonUtility.ToJson(restored.story), "EXACT_DISK_ROUNDTRIP_" + label);
            ClearTradeQAPanels(); state = restored.story; EnterNode(true);
            progress = restored.progress; carrying = restored.carrying; sideCarrying = restored.sideCarrying; inspected = restored.inspected;
            controller.enabled = false; player.transform.position = new Vector3(restored.px, Mathf.Max(.05f, player.transform.position.y), restored.pz); controller.enabled = true;
            ClearTradeQAPanels(); RefreshProps(); FaceCurrentTask(); UpdateCamera(true);
            TradeRequire(storyBefore == JsonUtility.ToJson(state) && StoryEngine.Validate(state) && WorldMissions.Get(state).world == loadedWorld, "EXACT_RESUMED_STATE_" + label);
            tradeQASaves++; Debug.Log("TRADE_SAVE_RESUMED " + label + " world=" + loadedWorld + " stage=" + (InTrade ? state.tradeShift.stage : "none"));
        }
        void CheckTradeQAIsolation()
        {
            if (tradeQAPaths == null) return;
            for (int i = 0; i < tradeQAPaths.Length; i++) TradeRequire(tradeQAFiles[i] == CharacterFileSnapshot(tradeQAPaths[i]), "REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < tradeQAKeys.Length; i++) TradeRequire(tradeQAPreferences[i] == PlayerPrefs.GetInt(tradeQAKeys[i], -1), "PREFERENCE_UNCHANGED_" + tradeQAKeys[i]);
            TradeRequire(tradeQAMusicLevel == PlayerPrefs.GetFloat("qiaopi-music-level", -1), "MUSIC_LEVEL_UNCHANGED");
            TradeRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "ALL_AUDIO_MUTED");
        }
        void CleanupTradeQA()
        {
            if (!string.IsNullOrEmpty(tradeQATempFolder) && Directory.Exists(tradeQATempFolder)) {
                try { Directory.Delete(tradeQATempFolder, true); } catch (Exception error) { Debug.LogWarning("TRADE_QA_TEMP_CLEANUP: " + error.Message); }
            }
        }
    }
}
