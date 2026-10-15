using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        sealed class EventQACase
        {
            public string route, choice;
            public int level, money, health, trust, relation;
            public EventQACase(string route, int level, string choice, int money, int health, int trust, int relation)
            { this.route = route; this.level = level; this.choice = choice; this.money = money; this.health = health; this.trust = trust; this.relation = relation; }
        }
        string[] eventQAPaths, eventQAFiles, eventQAKeys;
        int[] eventQAPreferences;
        float eventQAMusicLevel;
        string eventQATemp, eventQARoute = "";
        int eventQACases, eventQAAssertions, eventQASaves;
        bool WorkEventShortReview => WorkEventQA && Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-work-event-review") >= 0;
        void EventRequire(bool condition, string label)
        { eventQAAssertions++; if (!condition) throw new Exception("WORK_EVENT_QA_" + label); }

        IEnumerator RunWorkEventQA()
        {
            var pending = new Stack<IEnumerator>(); pending.Push(RunWorkEventChecks());
            while (pending.Count > 0) {
                object wait = null; Exception failure = null;
                try { if (!pending.Peek().MoveNext()) pending.Pop(); else wait = pending.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null) {
                    while (pending.Count > 0) { try { (pending.Pop() as IDisposable)?.Dispose(); } catch { } }
                    try { CheckWorkEventQAIsolation(); } catch (Exception error) { Debug.LogError("WORK_EVENT_QA_ISOLATION_FAILED: " + error); }
                    CleanupWorkEventQA(); Debug.LogError("WORK_EVENT_QA_FAILED: " + failure); Application.Quit(1); yield break;
                }
                if (wait is IEnumerator) pending.Push((IEnumerator)wait);
                else if (pending.Count > 0) yield return wait;
            }
            CleanupWorkEventQA();
            Debug.Log((WorkEventShortReview ? "WORK_EVENT_REVIEW_ALL_PASSED" : "WORK_EVENT_RUNTIME_ALL_PASSED") +
                ": synthetic career starting states, then actual first-person choices/work/employer settlement; unfinished jobs do not award money or memories; isolated disk saves, abandonment, reply selectors, original saves/preferences and mute verified. Cases=" +
                eventQACases + " routeFilter=" + (eventQARoute == "" ? "all" : eventQARoute) + " checkpoints=" + eventQASaves + " assertions=" + eventQAAssertions);
            Application.Quit(0);
        }
        IEnumerator RunWorkEventChecks()
        {
            EventRequire(WorkEventQA && QA && DetailQA && !TradeQA && !NanyangQA, "ISOLATED_ENTRY");
            eventQAPaths = new[] { savePath, savePath + ".bak", savePath + ".tmp" };
            eventQAFiles = new string[eventQAPaths.Length];
            for (int i = 0; i < eventQAPaths.Length; i++) eventQAFiles[i] = CharacterFileSnapshot(eventQAPaths[i]);
            eventQAKeys = new[] { "qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized" };
            eventQAPreferences = new int[eventQAKeys.Length];
            for (int i = 0; i < eventQAKeys.Length; i++) eventQAPreferences[i] = PlayerPrefs.GetInt(eventQAKeys[i], -1);
            eventQAMusicLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            eventQATemp = Path.Combine(Path.GetTempPath(), "qiaopi-work-event-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(eventQATemp);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false);
            if (voiceSource) voiceSource.Stop(); if (sfx) sfx.Stop();
            eventQARoute = WorkEventQAArgument("-qiaopi-work-event-route", "").ToLowerInvariant();
            EventRequire(eventQARoute == "" || eventQARoute == "dock" || eventQARoute == "shop" || eventQARoute == "courier", "INVALID_ROUTE_EXPECTED_dock_shop_courier");
            string name = (WorkEventShortReview ? "WorkEventShortReview-" : "WorkEventReview-") + Application.version + (MobileControls ? "-mobile" : "-desktop");
            string outputBase = WorkEventQAArgument("-qiaopi-work-event-output", Path.Combine(Path.GetTempPath(), "qiaopi-work-event-captures"));
            string folder = Application.isMobilePlatform ? name : Path.Combine(Path.GetFullPath(outputBase), name);
            Directory.CreateDirectory(Application.isMobilePlatform ? Path.Combine(Application.persistentDataPath, folder) : folder);
            Debug.Log("WORK_EVENT_QA_OUTPUT=" + folder + " routeFilter=" + (eventQARoute == "" ? "all" : eventQARoute));
            var cases = new[] {
                new EventQACase("dock",2,"cover",0,-2,1,0), new EventQACase("dock",2,"pair",-2,2,0,2),
                new EventQACase("dock",3,"teach",-1,1,2,2), new EventQACase("dock",3,"solo",2,-2,0,0),
                new EventQACase("shop",2,"credit",0,0,1,1), new EventQACase("shop",2,"portion",0,0,0,0),
                new EventQACase("shop",3,"record",1,0,2,2), new EventQACase("shop",3,"cash",0,0,0,0),
                new EventQACase("courier",2,"trace",0,-2,2,2), new EventQACase("courier",2,"return",-6,3,0,0),
                new EventQACase("courier",3,"personal",0,-1,2,2), new EventQACase("courier",3,"return",-7,3,0,0)
            };
            foreach (var test in cases) {
                if (eventQARoute != "" && eventQARoute != test.route || WorkEventShortReview && test.level != 2) continue;
                yield return RunWorkEventCase(test, folder);
            }
            foreach (string route in new[] { "dock", "shop", "courier" })
                if (eventQARoute == "" || eventQARoute == route) yield return WorkEventQAAbandon(route);
            CheckWorkEventQAIsolation();
        }
        IEnumerator RunWorkEventCase(EventQACase test, string folder)
        {
            string label = test.route + "_" + test.level + "_" + test.choice, prefix = folder + "/" + label;
            ClearWorkEventQAPanels(); state = NanyangQAFixture(WorkEventDestination(test.route), test.route, test.level); EnterNode(true); ResetView(); yield return null;
            int money = state.money, health = state.health, trust = state.trust, turns = state.journey.turns, decisions = state.decisions;
            int history = state.history.Count; var reward = NanyangLife.WorkReward(state, test.route);
            string wifeBefore = WorkEventQAWife(), memoryBefore = WorkEventQAMemory();
            EventRequire(WorkRelationshipText.LetterParagraph(state, "wife") == "" && WorkEvents.LastRecord(state, test.route) == null, "FIXTURE_HAS_NO_WORK_MEMORY");
            StartLifeAction(test.route); yield return null;
            EventRequire(InTrade && WorkEvents.HasIncident(state) && state.tradeShift.level == test.level, "NEW_EVENT_ENROLLED_" + label);
            string southIdentity = test.route == "courier" && test.level == 2 ? WorkEventQAModelIdentity(tradePeople[1]) : "";
            string northIdentity = test.route == "courier" && test.level == 2 ? WorkEventQAModelIdentity(tradePeople[2]) : "";
            if (test.route == "courier" && test.level == 2) EventRequire(southIdentity != "" && northIdentity != "" && southIdentity != northIdentity, "TWO_DISTINCT_RELOCATED_IDENTITIES");
            ResumeWorkEventQASave(label + "_enrolled"); yield return null;
            yield return ReachWorkEventDecision(test.route, label);
            EventRequire(WorkEvents.Pending(state), "EVENT_PENDING_" + label);
            string before = JsonUtility.ToJson(state);
            EventRequire(!ChooseWorkEvent(test.choice) && before == JsonUtility.ToJson(state), "CLOSED_EVENT_PANEL_REJECTS");
            tradePanel = true; tradePanelStage = state.tradeShift.stage;
            EventRequire(!FirstDayCanReach(WorkEventPoint(), 2.2f) && !ChooseWorkEvent(test.choice) && before == JsonUtility.ToJson(state), "DISTANT_EVENT_CHOICE_REJECTS");
            ClearWorkEventQAPanels();
            var unsettled = JsonUtility.FromJson<GameState>(before); string ignored;
            EventRequire(!LifeJourney.CommitAction(unsettled, test.route, out ignored) && before == JsonUtility.ToJson(unsettled), "PENDING_EVENT_CANNOT_SETTLE");
            yield return OpenWorkEventQAPanel(WorkEventPoint(), label + " discuss at work station");
            EventRequire(WorkEvents.Pending(state), "EVENT_SHOWN_AT_STATION");
            yield return CaptureWorld(prefix + "_decision");
            ResumeWorkEventQASave(label + "_before_choice"); yield return null;
            yield return OpenWorkEventQAPanel(WorkEventPoint(), label + " resumed decision");
            before = JsonUtility.ToJson(state);
            EventRequire(!ChooseWorkEvent("not-an-option") && before == JsonUtility.ToJson(state), "INVALID_EVENT_CHOICE_UNCHANGED");
            EventRequire(ChooseWorkEvent(test.choice) && !WorkEvents.Pending(state) && WorkEvents.ChoiceId(state) == test.choice && !tradePanel, "CHOICE_ACCEPTED_" + label);
            AssertWorkEventUnpaid(money, health, trust, turns, memoryBefore, wifeBefore, label + "_chosen");
            before = JsonUtility.ToJson(state);
            EventRequire(!ChooseWorkEvent(test.choice) && before == JsonUtility.ToJson(state), "REPLAYED_CHOICE_UNCHANGED");
            ResumeWorkEventQASave(label + "_chosen"); yield return null;
            EventRequire(WorkEvents.ChoiceId(state) == test.choice, "CHOICE_RESTORED");
            if (test.route == "courier" && test.level == 2) EventRequire(WorkEventQAModelIdentity(tradePeople[1]) == southIdentity && WorkEventQAModelIdentity(tradePeople[2]) == northIdentity, "RELOCATED_IDENTITIES_DO_NOT_SWAP_AFTER_CHOICE_OR_RELOAD");
            CheckWorkEventTaskShape(test);
            if (test.route == "dock") yield return FinishWorkEventQADock(prefix);
            else if (test.route == "shop") yield return FinishWorkEventQAShop(prefix);
            else yield return FinishWorkEventQACourier(test, prefix);
            EventRequire(InTrade && Complete && TradeWork.IsComplete(state), "ACTUAL_WORK_COMPLETE_" + label);
            AssertWorkEventUnpaid(money, health, trust, turns, memoryBefore, wifeBefore, label + "_done_unpaid");
            ResumeWorkEventQASave(label + "_done_unpaid"); yield return null;
            yield return ReachWorkEventQAPoint(mission.npcPosition, label + " settle employer", 2.5f);
            Interact(); yield return null;
            EventRequire(!InTrade && string.IsNullOrEmpty(state.journey.pendingJob) && state.journey.turns == turns + 1, "SETTLEMENT_ONCE_" + label);
            EventRequire(state.money == money + reward.money + test.money && state.health == Mathf.Clamp(health + reward.health + test.health, 0, 100) && state.trust == Mathf.Clamp(trust + reward.trust + test.trust, 0, 100), "EXACT_EVENT_WAGES_AND_RESOURCES_" + label);
            EventRequire(state.decisions == decisions && state.history.Count == history && NanyangLife.WorkCount(state, test.route) == test.level, "NO_MAIN_STORY_COUNTER_MUTATION");
            var record = WorkEvents.LastRecord(state, test.route);
            EventRequire(state.workMemory != null && state.workMemory.records.Count == 1 && record != null && record.choice == test.choice && record.level == test.level && record.turn == turns + 1 && record.returned == (test.choice == "return"), "EXACT_SETTLED_RECORD_" + label);
            EventRequire(WorkEvents.RelationshipScore(state, test.route) == test.relation, "EXACT_RELATIONSHIP_ONCE_" + label);
            string paragraph = WorkRelationshipText.LetterParagraph(state, "wife"), greeting = WorkRelationshipText.Greeting(state, test.route);
            EventRequire(!string.IsNullOrEmpty(paragraph) && !string.IsNullOrEmpty(greeting) && WorkEventQAWife().Contains(paragraph), "WIFE_REPLY_AND_GREETING_USE_SETTLED_EVIDENCE_" + label);
            if (test.choice == "return") {
                EventRequire(paragraph.Contains("这一趟没有收讫回执") && state.journey.log[state.journey.log.Count - 1].outcome.Contains("未能交付"), "RETURN_NOT_REPORTED_AS_DELIVERY");
            } else if (test.choice == "trace") EventRequire(paragraph.Contains("绕到南街核对新住处"), "WIFE_REMEMBERS_NEW_ADDRESS");
            else if (test.choice == "personal") EventRequire(paragraph.Contains("坚持找到阿福本人"), "WIFE_REMEMBERS_PERSONAL_HANDOVER");
            ResumeWorkEventQASave(label + "_settled"); yield return null;
            EventRequire(WorkRelationshipText.LetterParagraph(state, "wife") == paragraph && WorkRelationshipText.Greeting(state, test.route) == greeting, "RELATIONSHIP_RESTORES_EXACTLY");
            string paidMemory = WorkEventQAMemory(); int paidMoney = state.money, paidTurns = state.journey.turns;
            ClearWorkEventQAPanels(); Interact(); yield return null;
            EventRequire(state.money == paidMoney && state.journey.turns == paidTurns && WorkEventQAMemory() == paidMemory && WorkEvents.RelationshipScore(state, test.route) == test.relation, "NO_REPLAYED_WAGE_OR_RELATIONSHIP");
            CheckWorkEventQAIsolation(); eventQACases++;
            Debug.Log("WORK_EVENT_CASE_PASSED route=" + test.route + " level=" + test.level + " choice=" + test.choice + " actualMovement=true wages=" + (reward.money + test.money) + " relationship=" + test.relation + " returned=" + record.returned);
        }
        IEnumerator ReachWorkEventDecision(string route, string label)
        {
            if (route != "courier") yield break;
            EventRequire(state.tradeShift.stage == "read" && !WorkEvents.Pending(state), "COURIER_READS_BEFORE_EVENT");
            yield return OpenWorkEventQAPanel(TradePlaces.CourierRead, label + " read old envelope address");
            EventRequire(TradeAction("read") && state.tradeShift.stage == "ask" && WorkEvents.Pending(state), "COURIER_EVENT_AT_NEIGHBOUR");
            ResumeWorkEventQASave(label + "_read_envelope"); yield return null;
        }
        void CheckWorkEventTaskShape(EventQACase test)
        {
            if (test.route == "dock") EventRequire(TradeWork.DockCount(state) == (test.choice == "pair" || test.choice == "teach" ? 2 : 3), "CHOICE_CHANGES_CARGO_COUNT");
            else if (test.route == "shop") {
                int weight = test.choice == "portion" ? 2 : test.choice == "record" ? 5 : 3;
                int price = test.choice == "portion" ? 4 : test.choice == "record" ? 9 : 6;
                int paid = test.level == 2 ? 4 : 6, debt = test.choice == "credit" ? 2 : test.choice == "record" ? 3 : 0;
                EventRequire(TradeWork.ShopWeightTarget(state) == weight && TradeWork.ShopPrice(state) == price && TradeWork.ShopTender(state) == paid && TradeWork.ShopDebtTarget(state) == debt && TradeWork.ShopChangeTarget(state) == 0, "CHOICE_CHANGES_SHOP_ORDER");
            } else if (test.choice == "return") EventRequire(state.tradeShift.stage == "return" && WorkEvents.Returning(state) && state.tradeShift.recipient == -1 && !Complete, "RETURN_REQUIRES_REGISTRATION");
            else EventRequire(TradeWork.CourierRecipient(state) == 0 && TradeWork.CourierAddress(state) == "南街三号", "HANDOVER_TARGET_IS_CONFIRMED_SOUTH_ADDRESS");
        }
        IEnumerator FinishWorkEventQADock(string prefix)
        {
            int count = TradeWork.DockCount(state);
            for (int n = 0; n < count; n++) {
                int item = TradeWork.DockPriority(state);
                if (item < 0) { item = 0; while ((state.tradeShift.deliveredMask & (1 << item)) != 0) item++; }
                int zone = TradeWork.DockDestination(state, item);
                yield return ReachWorkEventQAPoint(TradePlaces.DockPickup(item), "event dock pickup " + item); Interact(); yield return null;
                EventRequire(state.tradeShift.held == item, "PICKUP_AFTER_EVENT");
                ResumeWorkEventQASave(prefix.Substring(prefix.LastIndexOf('/') + 1) + "_held_" + item); yield return null;
                yield return ReachWorkEventQAPoint(TradePlaces.DockDrop(zone), "event dock warehouse " + zone); Interact(); yield return null;
                EventRequire(state.tradeShift.held == -1 && (state.tradeShift.deliveredMask & (1 << item)) != 0, "EVENT_CARGO_DELIVERED");
            }
            yield return CaptureWorld(prefix + "_work_completed");
        }
        IEnumerator FinishWorkEventQAShop(string prefix)
        {
            yield return OpenWorkEventQAPanel(TradePlaces.ShopCounter, "event shop revised order");
            for (int i = 0; i < TradeWork.ShopWeightTarget(state); i++) EventRequire(TradeAction("add-weight", 1), "EVENT_ADD_WEIGHT");
            yield return CaptureWorld(prefix + "_revised_order");
            EventRequire(TradeAction("confirm-weight") && state.tradeShift.stage == "change", "EVENT_WEIGHT_CONFIRMED");
            EventRequire(TradeAction("confirm-change"), "EVENT_ZERO_CHANGE_CONFIRMED");
            int debt = TradeWork.ShopDebtTarget(state);
            if (debt > 0) {
                EventRequire(state.tradeShift.stage == "credit", "EVENT_CREDIT_REQUIRED");
                string before = JsonUtility.ToJson(state);
                EventRequire(!TradeAction("confirm-debt") && before == JsonUtility.ToJson(state), "MISSING_EVENT_CREDIT_REJECTED");
                for (int i = 0; i < debt; i++) EventRequire(TradeAction("add-debt", 1), "EVENT_RECORD_CREDIT");
                ResumeWorkEventQASave("shop_event_credit"); yield return null;
                yield return OpenWorkEventQAPanel(TradePlaces.ShopCounter, "event shop restored credit book");
                yield return CaptureWorld(prefix + "_credit_book");
                EventRequire(TradeAction("confirm-debt"), "EVENT_CREDIT_CONFIRMED");
            }
            ClearWorkEventQAPanels(); EventRequire(Complete, "EVENT_SHOP_COMPLETE");
        }
        IEnumerator FinishWorkEventQACourier(EventQACase test, string prefix)
        {
            if (test.choice == "return") {
                string before = JsonUtility.ToJson(state); tradePanel = true; tradePanelStage = "return"; tradePerson = -1;
                EventRequire(!FirstDayCanReach(mission.npcPosition, 2.2f) && !TradeAction("register-return") && before == JsonUtility.ToJson(state), "DISTANT_RETURN_REGISTRATION_REJECTED");
                ClearWorkEventQAPanels();
                var premature = JsonUtility.FromJson<GameState>(before); string ignored;
                EventRequire(!LifeJourney.CommitAction(premature, "courier", out ignored) && before == JsonUtility.ToJson(premature), "RETURN_CANNOT_BE_PAID_BEFORE_REGISTRATION");
                yield return OpenWorkEventQAPanel(mission.npcPosition, "event return batch to employer");
                EventRequire(state.tradeShift.stage == "return" && state.tradeShift.recipient == -1, "RETURN_PANEL_DOES_NOT_SETTLE");
                yield return CaptureWorld(prefix + "_return_registration");
                EventRequire(TradeAction("register-return") && Complete && state.tradeShift.recipient == -1, "RETURN_REGISTERED_WITHOUT_RECEIPT");
                ResumeWorkEventQASave("courier_" + test.level + "_return_registered"); yield return null;
                EventRequire(WorkEvents.Returning(state) && state.tradeShift.recipient == -1 && Complete, "REGISTERED_RETURN_RESTORED");
                yield break;
            }
            yield return OpenWorkEventQAPanel(TradePlaces.CourierNeighbour, "event follow neighbourhood information");
            yield return CaptureWorld(prefix + "_confirmed_directions");
            EventRequire(TradeAction("ask") && state.tradeShift.stage == "verify", "ASK_AFTER_DECISION");
            yield return OpenWorkEventQAPanel(TradePlaces.CourierRecipient(1), "event wrong same-name recipient");
            string wrong = JsonUtility.ToJson(state);
            EventRequire(!TradeAction("check", 1) && wrong == JsonUtility.ToJson(state), "EVENT_WRONG_RECIPIENT_REJECTED");
            yield return OpenWorkEventQAPanel(TradePlaces.CourierRecipient(0), "event correct south recipient");
            yield return CaptureWorld(prefix + "_identity_and_address");
            EventRequire(TradeAction("check", 0) && state.tradeShift.stage == "deliver", "EVENT_PERSON_CONFIRMED");
            ResumeWorkEventQASave("courier_" + test.choice + "_identity"); yield return null;
            yield return OpenWorkEventQAPanel(TradePlaces.CourierRecipient(0), "event personal handover");
            EventRequire(TradeAction("deliver", 0) && state.tradeShift.stage == "receipt" && !Complete, "EVENT_STILL_NEEDS_RECEIPT");
            yield return OpenWorkEventQAPanel(TradePlaces.CourierRecipient(0), "event signed receipt");
            yield return CaptureWorld(prefix + "_receipt");
            EventRequire(TradeAction("receipt", 0) && Complete, "EVENT_RECEIPT_OBTAINED");
        }
        IEnumerator WorkEventQAAbandon(string route)
        {
            ClearWorkEventQAPanels(); state = NanyangQAFixture(WorkEventDestination(route), route, 2); EnterNode(true); yield return null;
            string wife = WorkEventQAWife(), memory = WorkEventQAMemory(); int money = state.money, health = state.health, trust = state.trust, turns = state.journey.turns;
            StartLifeAction(route); yield return null; yield return ReachWorkEventDecision(route, route + " abandon");
            yield return OpenWorkEventQAPanel(WorkEventPoint(), route + " abandon decision");
            string choice = route == "dock" ? "pair" : route == "shop" ? "credit" : "return";
            EventRequire(ChooseWorkEvent(choice), "ABANDON_CHOICE_STARTED");
            ResumeWorkEventQASave(route + "_chosen_before_abandon"); yield return null;
            ClearWorkEventQAPanels(); lifePanel = true; AbandonTradeJob(); yield return null;
            EventRequire(!InTrade && string.IsNullOrEmpty(state.journey.pendingJob), "ABANDON_CLEARS_EVENT");
            AssertWorkEventUnpaid(money, health, trust, turns, memory, wife, route + "_abandoned");
            EventRequire(WorkRelationshipText.Greeting(state, route) == "" && WorkEvents.RelationshipScore(state, route) == 0, "ABANDON_HAS_NO_RELATIONSHIP");
            ResumeWorkEventQASave(route + "_abandoned"); yield return null;
            ClearWorkEventQAPanels(); StartLifeAction(route); yield return null;
            EventRequire(WorkEvents.HasIncident(state) && WorkEvents.ChoiceId(state) == "" && !Complete, "ABANDONED_EVENT_CAN_START_FRESH");
            ClearWorkEventQAPanels(); lifePanel = true; AbandonTradeJob(); yield return null;
            CheckWorkEventQAIsolation(); Debug.Log("WORK_EVENT_ABANDON_PASSED route=" + route + " noMemory=true noMoney=true noRelationship=true");
        }
        void AssertWorkEventUnpaid(int money, int health, int trust, int turns, string memory, string wife, string label)
        {
            EventRequire(state.money == money && state.health == health && state.trust == trust && state.journey.turns == turns, "NO_EARLY_REWARDS_" + label);
            EventRequire(WorkEventQAMemory() == memory && WorkEventQAWife() == wife && WorkRelationshipText.LetterParagraph(state, "wife") == "", "NO_UNSETTLED_MEMORY_OR_WIFE_FACT_" + label);
        }
        string WorkEventQAMemory()
        { return state.workMemory == null || state.workMemory.version == 0 ? "" : JsonUtility.ToJson(state.workMemory); }
        string WorkEventQAWife()
        { return ExperienceLetters.Reply(state, new PersonalLetterDraft { recipient = "wife", intent = "truth", amount = 0, body = "家中安好" }); }
        string WorkEventQAModelIdentity(GameObject person)
        {
            if (!person) return "";
            var identity = person.GetComponent<CharacterModelIdentity>();
            if(identity != null) return identity.modelId;
            var pieces = new List<string>();
            foreach (var renderer in person.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.sharedMesh) pieces.Add(renderer.sharedMesh.name + ":" + renderer.sharedMesh.GetEntityId());
            foreach (var filter in person.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh) pieces.Add(filter.sharedMesh.name + ":" + filter.sharedMesh.GetEntityId());
            pieces.Sort(StringComparer.Ordinal); return string.Join("|", pieces.ToArray());
        }
        string WorkEventDestination(string route) { return route == "dock" ? "singapore" : route == "shop" ? "penang" : "rangoon"; }
        string WorkEventQAArgument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            if (at < 0) return fallback;
            EventRequire(Array.LastIndexOf(args, key) == at && at + 1 < args.Length && !string.IsNullOrWhiteSpace(args[at + 1]) && !args[at + 1].StartsWith("-", StringComparison.Ordinal), "INVALID_ARGUMENT_" + key);
            return args[at + 1];
        }
        IEnumerator OpenWorkEventQAPanel(Vector3 point, string label)
        {
            yield return ReachWorkEventQAPoint(point, label); Interact(); yield return null;
            EventRequire(tradePanel, "PANEL_OPEN_" + label);
        }
        IEnumerator ReachWorkEventQAPoint(Vector3 point, string label, float range = 2.2f)
        {
            ClearWorkEventQAPanels(); yield return WalkFirstPerson(point, label);
            if (FirstDayCanReach(point, range)) yield break;
            foreach (Vector3 offset in new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward }) {
                Vector3 alternative = point + offset;
                if (!region.IsGround(alternative) || Physics.Linecast(alternative + Vector3.up * .9f, point + Vector3.up * .9f,
                    ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore)) continue;
                if (WalkPath.Find(player.transform.position, alternative, loadedWorld).Count == 0 && Dist(alternative) > 2) continue;
                yield return WalkFirstPerson(alternative, label + " alternate approach");
                if (FirstDayCanReach(point, range)) yield break;
            }
            EventRequire(false, "STATION_UNREACHABLE_" + label + " player=" + player.transform.position + " target=" + point);
        }
        void ClearWorkEventQAPanels()
        {
            if (letterEditor) ClosePersonalLetter();
            tradePanel = workTalkPanel = firstDayPanel = nanyangPanel = dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = lifePanel = false;
            walkRoute.Clear(); routeIndex = 0;
        }
        void ResumeWorkEventQASave(string label)
        {
            Vector3 feet = player.transform.position;
            var saved = new WorldSave { story = state, missionNode = state.nodeId, layoutVersion = 2, progress = progress, carrying = carrying,
                sideCarrying = sideCarrying, inspected = new List<int>(inspected), px = feet.x, pz = feet.z };
            string story = JsonUtility.ToJson(state); EventRequire(WorldSaveStore.IsValid(saved), "VALID_SAVE_" + label);
            string path = Path.Combine(eventQATemp, "checkpoint.json"); WorldSaveStore.Write(path, saved);
            bool recovered; WorldSave loaded = WorldSaveStore.Load(path, out recovered);
            EventRequire(loaded != null && !recovered && story == JsonUtility.ToJson(loaded.story), "EXACT_DISK_ROUNDTRIP_" + label);
            ClearWorkEventQAPanels(); state = loaded.story; EnterNode(true);
            progress = loaded.progress; carrying = loaded.carrying; sideCarrying = loaded.sideCarrying; inspected = loaded.inspected;
            controller.enabled = false; player.transform.position = new Vector3(loaded.px, Mathf.Max(.05f, player.transform.position.y), loaded.pz); controller.enabled = true;
            ClearWorkEventQAPanels(); RefreshProps(); FaceCurrentTask(); UpdateCamera(true);
            EventRequire(story == JsonUtility.ToJson(state) && StoryEngine.Validate(state), "RESUMED_EVENT_EXACTLY_" + label);
            eventQASaves++; Debug.Log("WORK_EVENT_SAVE_RESUMED " + label + " world=" + loadedWorld + " stage=" + (InTrade ? state.tradeShift.stage : "none") + " choice=" + WorkEvents.ChoiceId(state));
        }
        void CheckWorkEventQAIsolation()
        {
            if (eventQAPaths == null) return;
            for (int i = 0; i < eventQAPaths.Length; i++) EventRequire(eventQAFiles[i] == CharacterFileSnapshot(eventQAPaths[i]), "REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < eventQAKeys.Length; i++) EventRequire(eventQAPreferences[i] == PlayerPrefs.GetInt(eventQAKeys[i], -1), "PREFERENCE_UNCHANGED_" + eventQAKeys[i]);
            EventRequire(eventQAMusicLevel == PlayerPrefs.GetFloat("qiaopi-music-level", -1), "MUSIC_LEVEL_UNCHANGED");
            EventRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "ALL_AUDIO_MUTED");
        }
        void CleanupWorkEventQA()
        {
            if (!string.IsNullOrEmpty(eventQATemp) && Directory.Exists(eventQATemp)) {
                try { Directory.Delete(eventQATemp, true); } catch (Exception error) { Debug.LogWarning("WORK_EVENT_QA_CLEANUP: " + error.Message); }
            }
        }
    }
}
