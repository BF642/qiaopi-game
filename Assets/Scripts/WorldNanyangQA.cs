using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        string[] nanyangQAPaths, nanyangQAFiles, nanyangQAKeys;
        int[] nanyangQAPreferences;
        float nanyangQAMusicLevel;
        string nanyangQATempFolder;
        int nanyangQASaves;
        int nanyangQAEndureCases;
        bool NanyangShortReview => NanyangQA && Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-nanyang-review") >= 0;
        bool NanyangCareerReview => NanyangQA && Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-nanyang-career-review") >= 0;
        bool NanyangEndureReview => NanyangQA && Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-nanyang-endure-review") >= 0;
        static void NanyangRequire(bool condition, string label)
        { if (!condition) throw new Exception("NANYANG_QA_" + label); }

        IEnumerator RunNanyangQA()
        {
            var pending = new Stack<IEnumerator>(); pending.Push(RunNanyangChecks());
            while (pending.Count > 0) {
                object wait = null; Exception failure = null;
                try {
                    if (!pending.Peek().MoveNext()) pending.Pop();
                    else wait = pending.Peek().Current;
                } catch (Exception error) { failure = error; }
                if (failure != null) {
                    while (pending.Count > 0) {
                        try { (pending.Pop() as IDisposable)?.Dispose(); } catch (Exception cleanup) { Debug.LogError("NANYANG_QA_ITERATOR_CLEANUP: " + cleanup); }
                    }
                    try { CheckNanyangQAIsolation(); } catch (Exception isolation) { Debug.LogError("NANYANG_QA_ISOLATION_FAILED: " + isolation); }
                    CleanupNanyangQA(); Debug.LogError("NANYANG_QA_FAILED: " + failure); Application.Quit(1); yield break;
                }
                if (wait is IEnumerator) pending.Push((IEnumerator)wait);
                else if (pending.Count > 0) yield return wait;
            }
            CleanupNanyangQA();
            Debug.Log(NanyangEndureReview
                ? "NANYANG_ENDURE_REVIEW_PASSED: synthetic already-paid wage fixtures; three actual employer choice/task interactions, four visible response options, no second wage or recovery, exact fatigue, original text, truth/concealment delayed replies, deferred draft, coworker memory, isolated disk reloads; saves/preferences unchanged and muted. Cases=" + nanyangQAEndureCases + " checkpoints=" + nanyangQASaves
                : NanyangCareerReview
                ? "NANYANG_CAREER_REVIEW_PASSED: synthetic level-two starting fixtures; three careers physically completed levels two and three, two cases and employer each; first interaction opens inspection directly, credit only on settlement, isolated save round-trips and courier station captures. This does not cover the full wage-dispute/letter routes. Save checkpoints=" + nanyangQASaves
                : NanyangShortReview
                ? "NANYANG_REVIEW_ALL_PASSED: synthetic chapter fixtures; actual first-person approaches in four furnished worlds, wage-choice and seated-letter captures; saves/preferences unchanged and audio muted. This review is not a full playthrough."
                : "NANYANG_RUNTIME_ALL_PASSED: static preflight separated from three actual first-person career/dispute/letter/reply playthroughs; level-two/three inspections, physical meal/bed, one-time transfer pay, persisted stages, no changed player saves/preferences, audio muted. Save checkpoints=" + nanyangQASaves);
            Application.Quit(0);
        }
        IEnumerator RunNanyangChecks()
        {
            NanyangRequire(NanyangQA && QA && DetailQA, "ISOLATED_ENTRY");
            nanyangQAPaths = new[] { ChapterSlotPath(false), ChapterSlotPath(false) + ".bak", ChapterSlotPath(false) + ".tmp",
                ChapterSlotPath(true), ChapterSlotPath(true) + ".bak", ChapterSlotPath(true) + ".tmp" };
            nanyangQAFiles = new string[nanyangQAPaths.Length];
            for (int i = 0; i < nanyangQAPaths.Length; i++) nanyangQAFiles[i] = CharacterFileSnapshot(nanyangQAPaths[i]);
            nanyangQAKeys = new[] { "qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized" };
            nanyangQAPreferences = new int[nanyangQAKeys.Length];
            for (int i = 0; i < nanyangQAKeys.Length; i++) nanyangQAPreferences[i] = PlayerPrefs.GetInt(nanyangQAKeys[i], -1);
            nanyangQAMusicLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            nanyangQATempFolder = Path.Combine(Path.GetTempPath(), "qiaopi-nanyang-runtime-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(nanyangQATempFolder);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false);
            if (voiceSource) voiceSource.Stop(); if (sfx) sfx.Stop();
            string folder = (NanyangEndureReview ? "NanyangEndureReview-" : NanyangCareerReview ? "NanyangCareerReview-" : NanyangShortReview ? "NanyangShortReview-" : "NanyangReview-") + Application.version + (MobileControls ? "-mobile" : "-desktop");
            string[] args=Environment.GetCommandLineArgs();
            if(!Application.isMobilePlatform)for(int i=0;i+1<args.Length;i++)if(args[i]=="-qiaopi-nanyang-output")folder=Path.Combine(Path.GetFullPath(args[i+1]),folder);
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, folder));
            Debug.Log("NANYANG_QA_OUTPUT=" + Path.Combine(Application.persistentDataPath, folder));
            if (NanyangEndureReview) { yield return RunNanyangEndureReview(folder); CheckNanyangQAIsolation(); yield break; }
            if (NanyangCareerReview) { yield return RunNanyangCareerReview(folder); CheckNanyangQAIsolation(); yield break; }
            if (NanyangShortReview) { yield return RunNanyangShortReview(folder); CheckNanyangQAIsolation(); yield break; }

            yield return PreflightNanyangQA();
            string[] routes = { "dock", "shop", "courier" }, responses = { "evidence", "together", "leave" }, destinations = { "singapore", "penang", "rangoon" };
            for (int r = 0; r < routes.Length; r++) {
                string route = routes[r], response = responses[r], prefix = folder + "/" + route;
                ClearNanyangQAPanels(); state = NanyangQAFixture(destinations[r], route, 1);
                EnterNode(true); ResetView(); yield return null;
                ResumeNanyangQASave(route + "_free"); yield return null;
                int decisions = state.decisions, histories = state.history.Count;
                int initialMoney = state.money, initialHealth = state.health;
                yield return CompleteNanyangQAJob(route, 1, prefix + "_01_first_job");
                int gross = route == "dock" ? 10 : route == "shop" ? 7 : 6;
                int fatigue = route == "dock" ? 10 : route == "shop" ? 4 : 6;
                NanyangRequire(state.money == initialMoney + gross - 4 && state.health == initialHealth - fatigue, "FIRST_PAY_DEDUCTED_ONCE_" + route);
                NanyangRequire(state.nanyang.stage == "choice" && state.nanyang.promisedWage == gross && state.nanyang.paidWage == gross - 4 && state.nanyang.owed == 4 && state.journey.turns == 1, "ORIGINAL_WAGE_LEDGER_" + route);
                ResumeNanyangQASave(route + "_choice"); yield return null;
                yield return ReachNanyangQAInteraction(mission.npcPosition, () => nanyangPanel && nanyangPanelMode == "choice", route + " discuss missing wages");
                yield return CaptureWorld(prefix + "_02_wage_choice");
                int choiceMoney = state.money, choiceHealth = state.health, choiceTrust = state.trust;
                ChooseNanyangResponse(response); yield return null;
                NanyangRequire(state.nanyang.stage == "tasks" && state.nanyang.response == response && state.nanyang.taskProgress == 0 && state.money == choiceMoney && state.health == choiceHealth && state.trust == choiceTrust, "RESPONSE_NO_PAYMENT_" + route);
                ResumeNanyangQASave(route + "_chosen_response"); yield return null;
                int count = NanyangLife.RequiredSteps(state);
                for (int step = 0; step < count; step++) {
                    NanyangRequire(state.nanyang.stage == "tasks" && state.nanyang.taskProgress == step, "STEP_ORDER_" + route + "_" + step);
                    var task = NanyangTasks.Step(state);
                    if (task.inspect) {
                        yield return ReachNanyangQAInteraction(inspectionPositions[0], () => puzzle && puzzleIndex == 0, route + " response evidence " + step, 2.2f);
                        InspectionCase entry = InspectionEntry();
                        NanyangRequire(entry != null && entry.correct >= 0 && entry.correct < entry.answers.Length, "RESPONSE_INSPECTION_CASE");
                        string unchanged = JsonUtility.ToJson(state);
                        AnswerInspection((entry.correct + 1) % entry.answers.Length);
                        NanyangRequire(puzzle && unchanged == JsonUtility.ToJson(state), "WRONG_RESPONSE_ANSWER_HAS_NO_REWARD");
                        yield return CaptureWorld(prefix + "_03_response_step_" + step);
                        AnswerInspection(entry.correct); yield return null;
                    } else {
                        yield return ReachNanyangQAInteraction(task.person, () => nanyangPanel && nanyangPanelMode == "tasks", route + " response person " + step);
                        yield return CaptureWorld(prefix + "_03_response_step_" + step);
                        FinishNanyangStep(); yield return null;
                    }
                    NanyangRequire(state.nanyang.taskProgress == step + 1, "PHYSICAL_STEP_COMMITTED_" + route + "_" + step);
                    string after = JsonUtility.ToJson(state), ignored;
                    NanyangRequire(!NanyangLife.CompleteStep(state, step, out ignored) && after == JsonUtility.ToJson(state), "STALE_STEP_CANNOT_REPAY_" + route + "_" + step);
                    if (step + 1 < count) NanyangRequire(state.money == choiceMoney && !state.journey.hardshipResolved, "NO_PREMATURE_RECOVERY");
                    ResumeNanyangQASave(route + "_response_step_" + (step + 1)); yield return null;
                }
                int recovered = response == "evidence" ? 3 : response == "together" ? 4 : 0;
                NanyangRequire(state.nanyang.stage == "letter" && state.nanyang.recovered == recovered && state.money == choiceMoney + recovered && state.journey.hardshipResolved && state.journey.log.Count == 2, "FINAL_STEP_ONLY_SETTLEMENT_" + route);
                yield return SeatAtNanyangQAWritingDesk(route + " evening letter");
                var draft = PersonalLetters.EnsureDraft(state); draft.recipient = "wife"; draft.intent = "truth"; draft.amount = 2;
                string original = "吾妻：\n这几日在" + LifeJourney.DestinationName(state) + "接了工，四份工钱曾被扣下。" + (recovered > 0 ? "如今取回" + recovered + "份，余下的账还留着。" : "旧工钱没有追回，我先问清新条件，安顿住处。") + "\n我会留够饭钱。你在家中的针线与菜圃，也写给我听。";
                draft.body = original; personalLetterPreview = true;
                CheckNanyangQASave(route + "_seated_original_letter");
                yield return CaptureWorld(prefix + "_04_seated_letter");
                int incoming = state.letters.FindAll(l => l.incoming).Count, beforeSend = state.money; string token = draft.token, notice;
                NanyangRequire(PersonalLetters.TrySend(state, token, out notice), "SEND_WIFE_LETTER_" + route + " " + notice);
                NanyangRequire(state.nanyang.stage == "waiting" && state.money == beforeSend - 2 && state.nanyang.sentToken == token && state.letters.Exists(l => !l.incoming && l.body == original) && state.letters.FindAll(l => l.incoming).Count == incoming, "EXACT_OUTGOING_NO_EARLY_REPLY_" + route);
                NanyangRequire(state.nanyang.pendingReply.signature == "妻子" && state.nanyang.pendingReply.date == NanyangLife.ReplyDate, "WIFE_REPLY_SCHEDULED_WEEKS_LATER");
                AfterNanyangLetterSent(); yield return null;
                NanyangRequire(!letterEditor && !writingSeated && state.nanyang.stage == "waiting", "STAND_AFTER_POSTING");
                ResumeNanyangQASave(route + "_waiting"); yield return null;
                yield return ReachNanyangQAInteraction(mission.npcPosition, () => nanyangPanel && nanyangPanelMode == "waiting", route + " speak before weeks pass");
                yield return CaptureWorld(prefix + "_05_weeks_transition");
                AdvanceNanyangWeeks(); yield return null;
                NanyangRequire(state.nanyang.stage == "reply" && loadedWorld == "postoffice" && state.letters.FindAll(l => l.incoming).Count == incoming, "WEEKS_PASS_BEFORE_COLLECTION");
                ResumeNanyangQASave(route + "_reply_ready"); yield return null;
                yield return ReachNanyangQAInteraction(mission.npcPosition, () => state.nanyang.stage == "complete" && journal, route + " personally collect reply");
                NanyangRequire(state.nanyang.replyCollected && state.letters.FindAll(l => l.incoming).Count == incoming + 1 && state.letters.Exists(l => l.incoming && l.signature == "妻子" && l.date == NanyangLife.ReplyDate), "ONE_WIFE_REPLY_COLLECTED");
                yield return CaptureWorld(prefix + "_06_wife_reply");
                string received = JsonUtility.ToJson(state);
                NanyangRequire(!NanyangLife.CollectReply(state) && received == JsonUtility.ToJson(state), "REPLY_CANNOT_REPEAT");
                ResumeNanyangQASave(route + "_complete"); yield return null;

                string previousCases = "";
                for (int level = 2; level <= 3; level++) {
                    int before = state.money;
                    int expected = route == "dock" ? (level == 2 ? 12 : 14) : route == "shop" ? (level == 2 ? 10 : 13) : (level == 2 ? 9 : 12);
                    yield return CompleteNanyangQAJob(route, level, prefix + "_career_" + level);
                    NanyangRequire(state.money == before + expected && state.journey.turns == level && state.nanyang.recovered == recovered, "CAREER_PAYMENT_" + route + "_" + level);
                    string cases = nanyangQALastCaseTitles;
                    NanyangRequire(cases.Length > 0 && cases != previousCases, "CAREER_CASES_ACTUALLY_CHANGE_" + route + "_" + level);
                    previousCases = cases;
                    ResumeNanyangQASave(route + "_career_level_" + level); yield return null;
                }
                NanyangRequire(LifeJourney.CanFinish(state) && state.decisions == decisions && state.history.Count == histories, "LIFE_UNLOCKS_WITHOUT_STORY_COUNTERS_" + route);
                if (response == "leave") {
                    int before = state.money; yield return CompleteNanyangQAJob("shop", 1, prefix + "_transfer_first");
                    NanyangRequire(state.money == before + 9 && state.nanyang.transferBonusUsed, "DIFFERENT_WORK_BONUS_ONCE");
                    before = state.money; yield return CompleteNanyangQAJob("dock", 1, prefix + "_transfer_second");
                    NanyangRequire(state.money == before + 10, "SECOND_DIFFERENT_WORK_NO_EXTRA_BONUS");
                    ResumeNanyangQASave(route + "_transfer_consumed"); yield return null;
                }
                if (r == 0) yield return NanyangQAMealAndBed(prefix);
                ClearNanyangQAPanels(); FinishLifePhase(); yield return null;
                NanyangRequire(state.journey.completed && !LifeJourney.IsActive(state) && StoryEngine.Validate(state), "RETURN_TO_MAIN_STORY_" + route);
                CheckNanyangQASave(route + "_main_story_available");
                CheckNanyangQAIsolation();
                Debug.Log("NANYANG_ROUTE_PASSED route=" + route + " response=" + response + " destination=" + destinations[r] + " physicalCareerLevels=1,2,3 recipient=wife");
            }
            yield return RunNanyangEndureReview(folder + "/endure");
            CheckNanyangQAIsolation();
        }

        IEnumerator RunNanyangEndureReview(string folder)
        {
            Debug.Log("NANYANG_ENDURE_REVIEW_BEGIN: already-paid rule fixtures only; actual first-person employer response and one-step follow-through for each career, then real desk/send/reply or defer. Does not claim to replay the preceding job.");
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath,folder));
            string[] routes={"dock","shop","courier"},destinations={"singapore","penang","rangoon"};
            for(int r=0;r<routes.Length;r++) {
                string route=routes[r],prefix=folder+"/"+route;string notice;
                ClearNanyangQAPanels();state=NanyangQAFixture(destinations[r],route,1);
                NanyangRequire(LifeJourney.CommitAction(state,route,out notice)&&state.nanyang.stage=="choice","ENDURE_WITHHELD_FIXTURE_"+route);
                EnterNode(true);ResetView();yield return null;
                ResumeNanyangQASave(route+"_endure_choice");yield return null;
                int money=state.money,health=state.health,trust=state.trust,turns=state.journey.turns,decisions=state.decisions,letters=state.letters.Count;
                yield return ReachNanyangQAInteraction(mission.npcPosition,()=>nanyangPanel&&nanyangPanelMode=="choice",route+" endure four-way choice");
                NanyangRequire(NanyangLife.Choices(state).Count==4,"ENDURE_FOUR_OPTIONS_"+route);
                yield return CaptureWorld(prefix+"_01_four_choices");
                ChooseNanyangResponse("endure");yield return null;
                NanyangRequire(state.nanyang.stage=="tasks"&&state.nanyang.response=="endure"&&NanyangLife.RequiredSteps(state)==1&&state.money==money&&state.health==health&&state.trust==trust,"ENDURE_CHOICE_ONLY_"+route);
                ResumeNanyangQASave(route+"_endure_chosen");yield return null;
                var step=NanyangTasks.Step(state);
                NanyangRequire(!step.inspect&&step.world==NanyangTasks.RouteWorld(route)&&step.person==NanyangTasks.Employer(route),"ENDURE_ORIGINAL_EMPLOYER_"+route);
                yield return ReachNanyangQAInteraction(step.person,()=>nanyangPanel&&nanyangPanelMode=="tasks",route+" confirm only already-paid wages");
                yield return CaptureWorld(prefix+"_02_employer_confirm");
                FinishNanyangStep();yield return null;
                NanyangRequire(state.nanyang.stage=="letter"&&state.nanyang.recovered==0&&state.money==money&&state.health==health-2&&state.trust==trust&&state.journey.turns==turns&&state.journey.log.Count==2&&state.decisions==decisions,"ENDURE_NO_REPAY_OR_RECOVERY_"+route);
                string settled=JsonUtility.ToJson(state);FinishNanyangStep();
                NanyangRequire(!NanyangLife.CompleteStep(state,0,out notice)&&settled==JsonUtility.ToJson(state),"ENDURE_STALE_STEP_NO_EFFECT_"+route);
                ResumeNanyangQASave(route+"_endure_letter");yield return null;
                yield return SeatAtNanyangQAWritingDesk(route+" endure write at real desk");
                var draft=PersonalLetters.EnsureDraft(state);draft.recipient="wife";draft.intent=r==1?"reassure":"truth";draft.amount=r==1?0:2;
                draft.body="吾妻：\n我把今日的话写在这里，亲手写的字请原样留下。\n饭钱先留着，往后的事慢慢商量。";
                string original=draft.body,token=draft.token,intent=draft.intent;
                personalLetterPreview=true;yield return CaptureWorld(prefix+"_03_letter_at_desk");
                if(r==2) {
                    string originalDraft=JsonUtility.ToJson(draft);ClosePersonalLetter();
                    yield return ReachNanyangQAInteraction(mission.npcPosition,()=>nanyangPanel&&nanyangPanelMode=="letter",route+" choose to keep the draft");
                    DeferNanyangLetter();yield return null;
                    NanyangRequire(state.nanyang.stage=="complete"&&state.nanyang.letterDeferred&&state.money==money&&state.letters.Count==letters&&JsonUtility.ToJson(state.personalLetterDraft)==originalDraft,"ENDURE_DEFER_PRESERVES_DRAFT_"+route);
                } else {
                    int amount=draft.amount,incoming=state.letters.FindAll(x=>x.incoming).Count;
                    NanyangRequire(PersonalLetters.TrySend(state,token,out notice),"ENDURE_SEND_"+route+" "+notice);
                    string reply=state.nanyang.pendingReply.body;
                    NanyangRequire(state.nanyang.stage=="waiting"&&state.money==money-amount&&state.letters.Count==letters+1&&state.letters[letters].body==original,"ENDURE_OUTGOING_ONCE_"+route);
                    NanyangRequire(intent=="truth"?reply.Contains("你暂且忍下")&&reply.Contains("工钱还未取回"):!reply.Contains("忍下")&&!reply.Contains("工钱还未取回")&&!reply.Contains("已做过"),"ENDURE_DISCLOSURE_"+route);
                    string sent=JsonUtility.ToJson(state);NanyangRequire(!PersonalLetters.TrySend(state,token,out notice)&&sent==JsonUtility.ToJson(state),"ENDURE_NO_DUPLICATE_SEND_"+route);
                    AfterNanyangLetterSent();yield return null;
                    ResumeNanyangQASave(route+"_endure_waiting");yield return null;
                    NanyangRequire(!NanyangLife.CollectReply(state)&&state.letters.FindAll(x=>x.incoming).Count==incoming,"ENDURE_NO_EARLY_REPLY_"+route);
                    yield return ReachNanyangQAInteraction(mission.npcPosition,()=>nanyangPanel&&nanyangPanelMode=="waiting",route+" wait weeks with coworker");
                    AdvanceNanyangWeeks();yield return null;
                    ResumeNanyangQASave(route+"_endure_reply_ready");yield return null;
                    yield return ReachNanyangQAInteraction(mission.npcPosition,()=>state.nanyang.stage=="complete"&&journal,route+" collect Quanzhou reply");
                    NanyangRequire(state.nanyang.replyCollected&&state.letters.Count==letters+2&&state.letters.FindAll(x=>x.incoming).Count==incoming+1&&state.letters[letters+1].body==reply,"ENDURE_ONE_SNAPSHOT_REPLY_"+route);
                    yield return CaptureWorld(prefix+"_04_received_reply");
                    string received=JsonUtility.ToJson(state);NanyangRequire(!NanyangLife.CollectReply(state)&&received==JsonUtility.ToJson(state),"ENDURE_NO_DUPLICATE_REPLY_"+route);
                }
                ResumeNanyangQASave(route+"_endure_complete");yield return null;
                ClearNanyangQAPanels();VisitNanyangPlace("quarters");yield return null;
                yield return ReachNanyangQAInteraction(mission.npcPosition,()=>nanyangPanel&&nanyangPanelMode=="memory",route+" coworker remembers unpaid wages");
                NanyangRequire(NanyangPanelBody().Contains("那四份还欠着")&&LifeJourney.Actions(state).Exists(x=>x.physical&&x.enabled),"ENDURE_MEMORY_AND_FREEDOM_"+route);
                if(r==0)yield return CaptureWorld(prefix+"_05_coworker_memory");
                CheckNanyangQAIsolation();nanyangQAEndureCases++;
                Debug.Log("NANYANG_ENDURE_ROUTE_PASSED route="+route+" destination="+destinations[r]+" actualEmployerStep=true recovery=0 physicalCost=2 letter="+(r==2?"defer":intent));
            }
        }

        string nanyangQALastCaseTitles = "";
        IEnumerator CompleteNanyangQAJob(string route, int level, string capture)
        {
            ClearNanyangQAPanels();
            NanyangRequire(NanyangLife.CareerLevel(state, route) == level, "EXPECTED_CAREER_LEVEL_" + route + "_" + level);
            int beforeTurns = state.journey.turns, beforeMoney = state.money, beforeTrust = state.trust;
            int workTrust = NanyangLife.WorkReward(state, route).trust;
            StartLifeAction(route); yield return null;
            NanyangRequire(state.journey.pendingJob == route && InNanyang && !NanyangIncident, "PHYSICAL_JOB_STARTED_" + route);
            if (level >= 2) NanyangRequire(mission.activity == "inspect" && mission.required == 2, "TWO_AUTHORED_CAREER_CASES");
            ResumeNanyangQASave(route + "_level_" + level + "_pending"); yield return null;
            nanyangQALastCaseTitles = "";
            int units = 0;
            while (!Complete && units++ < 4) {
                if (mission.activity == "inspect") {
                    int index = 0; while (index < mission.required && inspected.Contains(index)) index++;
                    NanyangRequire(index < mission.required, "INSPECTION_PENDING");
                    int expectedIndex = index;
                    NanyangPlace stationView = NanyangCareerReview && route == "courier" && level == 2 ? NanyangPlaces.Get("postoffice", index == 0 ? "archive" : "verify") : null;
                    yield return ReachNanyangQAInteraction(inspectionPositions[index], () => puzzle && puzzleIndex == expectedIndex, route + " level " + level + " inspect " + index, 2.2f,
                        stationView, capture + "_station_" + (index == 0 ? "archive" : "verify"));
                    var entry = InspectionEntry();
                    NanyangRequire(entry != null && entry.answers != null && entry.correct >= 0 && entry.correct < entry.answers.Length, "VALID_CAREER_CASE");
                    string beforeWrong = JsonUtility.ToJson(state);
                    int inspectProgress = progress, inspectCount = inspected.Count, inspectTrust = state.trust;
                    AnswerInspection((entry.correct + 1) % entry.answers.Length);
                    NanyangRequire(puzzle && progress == inspectProgress && inspected.Count == inspectCount && beforeWrong == JsonUtility.ToJson(state), "WRONG_CAREER_ANSWER_UNCHANGED");
                    nanyangQALastCaseTitles += entry.title + "|";
                    if (level >= 2 && !NanyangCareerReview) yield return CaptureWorld(capture + "_case_" + index);
                    AnswerInspection(entry.correct); yield return null;
                    NanyangRequire(!puzzle && inspected.Contains(index), "CAREER_INSPECTION_RECORDED");
                    NanyangRequire(state.trust == inspectTrust, "CORRECT_INSPECTION_NO_EARLY_CREDIT_" + route + "_" + level + "_" + index);
                    ResumeNanyangQASave(route + "_level_" + level + "_case_" + index); yield return null;
                } else {
                    NanyangRequire(mission.activity == "deliver", "DELIVERY_JOB");
                    yield return ReachNanyangQAInteraction(mission.itemPosition, () => carrying && !sideCarrying, route + " level " + level + " pick up", 2.2f);
                    ResumeNanyangQASave(route + "_level_" + level + "_carrying"); yield return null;
                    NanyangRequire(carrying && !sideCarrying, "CARRIED_JOB_RESTORED");
                    int before = progress;
                    yield return ReachNanyangQAInteraction(mission.destination, () => !carrying && progress == before + 1, route + " level " + level + " deliver", 2.2f);
                    ResumeNanyangQASave(route + "_level_" + level + "_delivered"); yield return null;
                }
            }
            NanyangRequire(Complete && state.money == beforeMoney && state.trust == beforeTrust && state.journey.turns == beforeTurns, "NO_PAYMENT_OR_CREDIT_UNTIL_EMPLOYER");
            yield return ReachNanyangQAInteraction(mission.npcPosition, () => state.journey.turns == beforeTurns + 1 && string.IsNullOrEmpty(state.journey.pendingJob), route + " level " + level + " settle employer");
            NanyangRequire(state.journey.turns == beforeTurns + 1 && NanyangLife.WorkCount(state, route) == level, "JOB_COUNT_ONCE_" + route + "_" + level);
            NanyangRequire(state.trust == Mathf.Min(100, beforeTrust + workTrust), "EXACT_CAREER_CREDIT_AT_SETTLEMENT_" + route + "_" + level);
            ClearNanyangQAPanels();
        }
        IEnumerator NanyangQAMealAndBed(string prefix)
        {
            ClearNanyangQAPanels(); VisitNanyangPlace("quarters"); yield return null;
            NanyangRequire(loadedWorld == "quarters" && state.nanyang.visitingWorld == "quarters", "VISIT_QUARTERS");
            ResumeNanyangQASave("meal_free_visit"); yield return null;
            var meal = NanyangPlaces.Get("quarters", "meal");
            yield return ReachNanyangQAInteraction(meal.position, () => nanyangPanel && nanyangPanelMode == "meal", "actual kitchen meal stall", 1.8f);
            AimNanyangQACamera(meal.facePoint); yield return CaptureWorld(prefix + "_07_meal_stall");
            int money = state.money, health = state.health, turns = state.journey.turns; string notice;
            NanyangRequire(NanyangLife.TryMeal(state, out notice) && state.money == money - 2 && state.health == Mathf.Min(100, health + 8) && state.journey.turns == turns, "MEAL_COST_AND_HEALTH");
            string fed = JsonUtility.ToJson(state);
            NanyangRequire(!NanyangLife.TryMeal(state, out notice) && fed == JsonUtility.ToJson(state), "NO_REPEAT_MEAL");
            ResumeNanyangQASave("meal_consumed"); yield return null;
            ClearNanyangQAPanels(); StartLifeAction("rest"); yield return null;
            NanyangRequire(state.journey.pendingJob == "rest" && NanyangRest && state.journey.turns == turns, "BED_REQUIRED_FOR_REST");
            ResumeNanyangQASave("bed_pending"); yield return null;
            int count = NanyangLife.WorkCount(state, "dock"), recovered = state.nanyang.recovered;
            var bed = NanyangPlaces.Get("quarters", "rest");
            yield return WalkFirstPerson(bed.position, "actual bunk bedside");
            AimNanyangQACamera(bed.facePoint); yield return CaptureWorld(prefix + "_08_actual_bed");
            yield return ReachNanyangQAInteraction(bed.position, () => state.journey.turns == turns + 1 && string.IsNullOrEmpty(state.journey.pendingJob), "sleep at actual bunk", 1.85f);
            NanyangRequire(NanyangLife.WorkCount(state, "dock") == count && state.nanyang.stage == "complete" && state.nanyang.recovered == recovered && state.journey.hardshipResolved, "REST_NO_CAREER_OR_SECOND_INCIDENT");
            ResumeNanyangQASave("bed_rest_complete"); yield return null;
        }
        IEnumerator SeatAtNanyangQAWritingDesk(string label)
        {
            ClearNanyangQAPanels(); NanyangRequire(writingPlace != null, "WRITING_PLACE_" + loadedWorld);
            yield return WalkFirstPerson(writingPlace.approach, label + " approach");
            if (!NearWritingDesk) {
                // WalkCheck normally stops 1.6 m from its destination; a writing
                // chair requires 1.25 m. Continue through the same controller
                // toward a point slightly beyond the clear standing position.
                Vector3 toward = writingPlace.approach - player.transform.position; toward.y = 0;
                if (toward.sqrMagnitude > .01f) yield return WalkFirstPerson(writingPlace.approach + toward.normalized * .75f, label + " final chair approach");
            }
            NanyangRequire(NearWritingDesk, "DESK_ACTUALLY_REACHED_" + loadedWorld);
            Interact(); yield return new WaitForSeconds(.6f);
            NanyangRequire(letterEditor && writingSeated && NearWritingDesk, "SEATED_EDITOR_" + loadedWorld);
            NanyangRequire(Vector3.Distance(cam.transform.position, writingPlace.seatEye) < .02f && Blocked && !heldCamera.enabled, "SEATED_CAMERA_AND_INPUT");
        }
        IEnumerator ReachNanyangQAInteraction(Vector3 target, Func<bool> reached, string label, float range = 2.5f,
            NanyangPlace inspectView = null, string inspectCapture = null)
        {
            int directInspection = -1;
            if (!string.IsNullOrEmpty(state.journey.pendingJob) && mission.activity == "inspect" && !Complete)
                for (int i = 0; i < mission.required; i++)
                    if (!inspected.Contains(i) && (target - inspectionPositions[i]).sqrMagnitude < .01f) { directInspection = i; break; }
            ClearNanyangQAPanels(); yield return WalkFirstPerson(target, label);
            if (inspectView != null) {
                NanyangRequire(!puzzle, "STATION_CAPTURE_BEFORE_INSPECTION");
                AimNanyangQACamera(inspectView.facePoint); yield return CaptureWorld(inspectCapture);
            }
            if (directInspection >= 0) {
                bool canReach = FirstDayCanReach(target, range);
                if (canReach) Interact();
                Debug.Log("NANYANG_DIRECT_INSPECTION firstAttempt=true label=" + label + " canReach=" + canReach + " job=" + state.journey.pendingJob + " activity=" + mission.activity + " puzzle=" + puzzle + " index=" + puzzleIndex + " panel=" + (nanyangPanel ? nanyangPanelMode : "none"));
                NanyangRequire(canReach && !string.IsNullOrEmpty(state.journey.pendingJob) && mission.activity == "inspect" && puzzle && puzzleIndex == directInspection && reached(),
                    "FIRST_INTERACTION_MUST_OPEN_WORK_INSPECTION_" + label);
                yield break;
            }
            if (FirstDayCanReach(target, range)) { Interact(); if (reached()) yield break; }
            foreach (Vector3 offset in new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward }) {
                ClearNanyangQAPanels(); Vector3 approach = target + offset * 1.0f;
                if (!region.IsGround(approach) || Physics.Linecast(approach + Vector3.up * .9f, target + Vector3.up * .9f,
                    ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore)) continue;
                if (WalkPath.Find(player.transform.position, approach, loadedWorld).Count == 0 && Dist(approach) > 2) continue;
                yield return WalkFirstPerson(approach, label + " alternate approach");
                if (FirstDayCanReach(target, range)) { Interact(); if (reached()) yield break; }
            }
            NanyangRequire(false, "INTERACTION_UNREACHABLE_" + label + " player=" + player.transform.position + " target=" + target + " stage=" + state.nanyang.stage + " panel=" + nanyangPanelMode);
        }

        IEnumerator RunNanyangCareerReview(string folder)
        {
            Debug.Log("NANYANG_CAREER_REVIEW_BEGIN: synthetic level-two rule fixtures only; then actual first-person completion of two inspections and employer for levels two and three in each career. Wage-dispute and letter paths are outside this run.");
            string[] routes = { "dock", "shop", "courier" }, destinations = { "singapore", "penang", "rangoon" };
            for (int r = 0; r < routes.Length; r++) {
                string route = routes[r]; ClearNanyangQAPanels(); state = NanyangQAFixture(destinations[r], route, 2);
                EnterNode(true); ResetView(); yield return null;
                ResumeNanyangQASave("career_review_" + route + "_fixture"); yield return null;
                string previousCases = "";
                for (int level = 2; level <= 3; level++) {
                    int money = state.money;
                    int pay = NanyangLife.WorkReward(state, route).money;
                    yield return CompleteNanyangQAJob(route, level, folder + "/" + route + "_level_" + level);
                    NanyangRequire(state.money == money + pay && state.journey.turns == level, "CAREER_REVIEW_EXACT_PAY_" + route + "_" + level);
                    NanyangRequire(nanyangQALastCaseTitles.Length > 0 && nanyangQALastCaseTitles != previousCases, "CAREER_REVIEW_NEW_CASES_" + route + "_" + level);
                    previousCases = nanyangQALastCaseTitles;
                    ResumeNanyangQASave("career_review_" + route + "_level_" + level); yield return null;
                    Debug.Log("NANYANG_CAREER_LEVEL_PASSED route=" + route + " level=" + level + " actualInspections=2 actualEmployerSettlement=true trust=" + state.trust);
                }
                CheckNanyangQAIsolation();
            }
        }

        static GameState NanyangQAFixture(string destination, string route, int level)
        {
            var s = StoryEngine.NewGame(20261008); s.nodeId = "first_pay"; s.journey.destination = destination; s.money = 100; s.health = 100; s.trust = 45;
            NanyangRequire(NanyangLife.TryBegin(s), "FIXTURE_ENROL"); string notice;
            if (level >= 2) {
                NanyangRequire(LifeJourney.CommitAction(s, route, out notice) && NanyangLife.ChooseResponse(s, "leave"), "FIXTURE_FIRST_WORK");
                for (int i = 0; i < 2; i++) NanyangRequire(NanyangLife.CompleteStep(s, i, out notice), "FIXTURE_RESOLVE");
                NanyangRequire(NanyangLife.DeferLetter(s), "FIXTURE_DEFER");
                if (level >= 3) NanyangRequire(LifeJourney.CommitAction(s, route, out notice), "FIXTURE_SECOND_WORK");
            }
            NanyangRequire(StoryEngine.Validate(s), "VALID_FIXTURE"); return s;
        }
        IEnumerator PreflightNanyangQA()
        {
            int cases = 0, targets = 0, failures = 0;
            Debug.Log("NANYANG_PREFLIGHT_BEGIN: synthetic saved-rule fixtures plus capsule path/interaction visibility checks. These checks do not claim actual player movement.");
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
                foreach (string route in new[] { "dock", "shop", "courier" }) {
                    for (int level = 1; level <= 3; level++) {
                        ClearNanyangQAPanels(); state = NanyangQAFixture(destination, route, level); state.journey.pendingJob = route;
                        EnterNode(true); yield return null; Physics.SyncTransforms(); cases++;
                        string label = destination + "/" + route + "/level" + level;
                        var points = new List<Vector3> { mission.npcPosition };
                        if (mission.activity == "inspect") for (int i = 0; i < mission.required; i++) points.Add(inspectionPositions[i]);
                        else { points.Add(mission.itemPosition); points.Add(mission.destination); }
                        foreach (Vector3 point in points) { targets++; if (!CheckNanyangQAPath(region.spawn, point, label + "/target" + targets)) failures++; }
                        Debug.Log("NANYANG_CAREER_PREFLIGHT " + label + " activity=" + mission.activity + " required=" + mission.required);
                    }
                    foreach (string response in new[] { "evidence", "together", "leave", "endure" }) {
                        ClearNanyangQAPanels(); state = NanyangQAFixture(destination, route, 1); string notice;
                        NanyangRequire(LifeJourney.CommitAction(state, route, out notice) && NanyangLife.ChooseResponse(state, response), "PREFLIGHT_RESPONSE_FIXTURE");
                        int required = NanyangLife.RequiredSteps(state);
                        for (int step = 0; step < required; step++) {
                            EnterNode(true); yield return null; Physics.SyncTransforms();
                            var task = NanyangTasks.Step(state); Vector3 target = task.inspect ? inspectionPositions[0] : task.person;
                            string label = destination + "/" + route + "/" + response + "/step" + step;
                            targets++; if (!CheckNanyangQAPath(region.spawn, target, label, task.inspect ? 2.2f : 2.5f)) failures++;
                            NanyangRequire(NanyangLife.CompleteStep(state, step, out notice), "PREFLIGHT_FIXTURE_NEXT_STEP");
                        }
                    }
                }
            ClearNanyangQAPanels(); state = NanyangQAFixture("singapore", "shop", 1); EnterNode(true); yield return null;
            foreach (string role in new[] { "meal", "rest", "colleague" }) {
                targets++; var place = NanyangPlaces.Get("quarters", role);
                if (!CheckNanyangQAPath(region.spawn, place.position, "quarters/" + role, role == "colleague" ? 2.5f : 1.8f)) failures++;
            }
            Debug.Log("NANYANG_PREFLIGHT_SUMMARY careerFixtures=" + cases + " targets=" + targets + " failures=" + failures);
            NanyangRequire(failures == 0, "PREFLIGHT_FAILURES_" + failures);
        }
        bool CheckNanyangQAPath(Vector3 from, Vector3 target, string label, float reach = 2.2f)
        {
            Physics.SyncTransforms(); List<Vector3> route = WalkPath.Find(from, target, loadedWorld);
            Vector3 endpoint = route.Count == 0 ? from : route[route.Count - 1]; Vector3 delta = endpoint - target; delta.y = 0;
            bool ground = region.IsGround(endpoint), near = delta.magnitude < reach;
            bool visible = !Physics.Linecast(endpoint + Vector3.up * .9f, target + Vector3.up * .9f, ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore);
            bool valid = ground && near && visible && (route.Count > 0 || (from - target).sqrMagnitude < reach * reach);
            foreach (Vector3 point in route) valid &= region.IsGround(point);
            if (!valid) Debug.LogError("NANYANG_PATH_UNREACHABLE " + label + " world=" + loadedWorld + " from=" + from + " target=" + target + " endpoint=" + endpoint + " waypoints=" + route.Count + " distance=" + delta.magnitude.ToString("F3") + " ground=" + ground + " visible=" + visible);
            else Debug.Log("NANYANG_PATH_OK " + label + " waypoints=" + route.Count + " distance=" + delta.magnitude.ToString("F3"));
            return valid;
        }

        IEnumerator RunNanyangShortReview(string folder)
        {
            Debug.Log("NANYANG_SHORT_REVIEW: synthetic rule fixtures; every pictured interaction approached using the character controller; not a full-route or performance result.");
            foreach (string worldId in new[] { "quarters", "port", "market", "postoffice" }) {
                ClearNanyangQAPanels(); state = NanyangQAFixture("singapore", "shop", 1);
                NanyangRequire(NanyangLife.Visit(state, worldId), "REVIEW_VISIT"); EnterNode(true); ResetView(); yield return null;
                string role = worldId == "quarters" ? "meal" : worldId == "postoffice" ? "receive" : "accounts";
                var place = NanyangPlaces.Get(worldId, role);
                yield return WalkFirstPerson(place.position, "review actual " + worldId + " station");
                AimNanyangQACamera(place.facePoint); yield return CaptureWorld(folder + "/short_world_" + worldId);
                if (worldId == "quarters") {
                    yield return ReachNanyangQAInteraction(place.position, () => nanyangPanel && nanyangPanelMode == "meal", "review kitchen meal", 1.8f);
                    yield return CaptureWorld(folder + "/short_meal_panel");
                }
                CheckNanyangQASave("short_world_" + worldId);
            }
            ClearNanyangQAPanels(); state = NanyangQAFixture("singapore", "dock", 1); string notice;
            NanyangRequire(LifeJourney.CommitAction(state, "dock", out notice), "REVIEW_WAGE_FIXTURE"); EnterNode(true); yield return null;
            yield return ReachNanyangQAInteraction(mission.npcPosition, () => nanyangPanel && nanyangPanelMode == "choice", "review wage choice");
            yield return CaptureWorld(folder + "/short_wage_choice");
            ClearNanyangQAPanels(); NanyangRequire(NanyangLife.ChooseResponse(state, "together") && NanyangLife.CompleteStep(state, 0, out notice) && NanyangLife.CompleteStep(state, 1, out notice), "REVIEW_LETTER_FIXTURE");
            EnterNode(true); yield return null;
            yield return SeatAtNanyangQAWritingDesk("review actual evening desk");
            var draft = PersonalLetters.EnsureDraft(state); draft.recipient = "wife"; draft.intent = "truth"; draft.amount = 2;
            draft.body = "吾妻：\n许生与工友陪我核了工账，欠下的四份终于拿到了。奔走半日，夜里才坐下来写这几行。\n我会留够饭钱，也想知道你手边的针线和菜圃。";
            personalLetterPreview = true; yield return CaptureWorld(folder + "/short_seated_letter");
            CheckNanyangQASave("short_seated_letter"); ClearNanyangQAPanels();
        }
        void AimNanyangQACamera(Vector3 point)
        {
            Vector3 gaze = point - CameraEyePoint(); cameraYaw = Mathf.Atan2(gaze.x, gaze.z) * Mathf.Rad2Deg;
            cameraPitch = -Mathf.Atan2(gaze.y, new Vector2(gaze.x, gaze.z).magnitude) * Mathf.Rad2Deg; UpdateCamera(true);
        }
        void ClearNanyangQAPanels()
        {
            if (letterEditor) ClosePersonalLetter();
            firstDayPanel = nanyangPanel = dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = lifePanel = false;
            walkRoute.Clear(); routeIndex = 0;
        }
        WorldSave CheckNanyangQASave(string label)
        {
            Vector3 feet = player.transform.position;
            var saved = new WorldSave { story = state, missionNode = state.nodeId, layoutVersion = 2, progress = progress,
                carrying = carrying, sideCarrying = sideCarrying, inspected = new List<int>(inspected), px = feet.x, pz = feet.z };
            var restored = JsonUtility.FromJson<WorldSave>(JsonUtility.ToJson(saved));
            NanyangRequire(WorldSaveStore.IsValid(restored), "VALID_SAVE_" + label + " world=" + loadedWorld + " stage=" + state.nanyang.stage + " progress=" + progress);
            NanyangRequire(restored.story.nanyang.stage == state.nanyang.stage && restored.story.nanyang.taskProgress == state.nanyang.taskProgress &&
                restored.story.money == state.money && restored.story.personalLettersSent == state.personalLettersSent && restored.story.letters.Count == state.letters.Count &&
                restored.progress == progress && restored.carrying == carrying && restored.sideCarrying == sideCarrying && WorldMissions.Get(restored.story).world == loadedWorld, "ROUNDTRIP_STATE_" + label);
            string path = Path.Combine(nanyangQATempFolder, "checkpoint.json"); WorldSaveStore.Write(path, restored);
            bool recovered; var loaded = WorldSaveStore.Load(path, out recovered);
            NanyangRequire(loaded != null && !recovered && WorldSaveStore.IsValid(loaded), "TEMP_DISK_SAVE_LOAD_" + label);
            nanyangQASaves++; return loaded;
        }
        void ResumeNanyangQASave(string label)
        {
            WorldSave saved = CheckNanyangQASave(label);
            ClearNanyangQAPanels(); state = saved.story; EnterNode(true);
            progress = saved.progress; carrying = saved.carrying; sideCarrying = saved.sideCarrying; inspected = saved.inspected;
            controller.enabled = false; player.transform.position = new Vector3(saved.px, Mathf.Max(.05f, player.transform.position.y), saved.pz); controller.enabled = true;
            ClearNanyangQAPanels(); RefreshProps(); FaceCurrentTask(); UpdateCamera(true);
            NanyangRequire(WorldMissions.Get(state).world == loadedWorld && StoryEngine.Validate(state), "RESUMED_WORLD_" + label);
            Debug.Log("NANYANG_SAVE_RESUMED " + label + " world=" + loadedWorld + " stage=" + state.nanyang.stage + " step=" + state.nanyang.taskProgress + " progress=" + progress);
        }
        void CheckNanyangQAIsolation()
        {
            if (nanyangQAPaths == null) return;
            for (int i = 0; i < nanyangQAPaths.Length; i++) NanyangRequire(nanyangQAFiles[i] == CharacterFileSnapshot(nanyangQAPaths[i]), "REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < nanyangQAKeys.Length; i++) NanyangRequire(nanyangQAPreferences[i] == PlayerPrefs.GetInt(nanyangQAKeys[i], -1), "PREFERENCE_UNCHANGED_" + nanyangQAKeys[i]);
            NanyangRequire(nanyangQAMusicLevel == PlayerPrefs.GetFloat("qiaopi-music-level", -1), "MUSIC_LEVEL_UNCHANGED");
            NanyangRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "ALL_AUDIO_MUTED");
        }
        void CleanupNanyangQA()
        {
            if (!string.IsNullOrEmpty(nanyangQATempFolder) && Directory.Exists(nanyangQATempFolder)) {
                try { Directory.Delete(nanyangQATempFolder, true); } catch (Exception error) { Debug.LogWarning("NANYANG_QA_TEMP_CLEANUP: " + error.Message); }
            }
        }
    }
}
