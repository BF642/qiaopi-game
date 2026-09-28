using System;
using System.Collections;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        bool LifeQA=>Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-life-test")>=0;
        IEnumerator RunLifeQA(){yield return RunLifeReview();Debug.Log("LIFE_RUNTIME_ALL_PASSED");Application.Quit();}
        IEnumerator RunLifeReview()
        {
            if(letterEditor)ClosePersonalLetter();
            state=StoryEngine.NewGame(20260928);dialogue=journal=map=help=gallery=lifePanel=false;EnterNode(true);ResetView();
            yield return new WaitForSeconds(.4f);yield return CaptureWorld("2.0_安稳的家");
            yield return WalkFirstPerson(mission.npcPosition,"peace family");Interact();CameraRequire(dialogue,"PEACE_TALK");
            Choose("meal");Next();CameraRequire(state.nodeId=="pressure"&&loadedWorld=="quanzhou","PRESSURE_AT_HOME");
            Choose("neighbors");Next();CameraRequire(state.nodeId=="home","PROLOGUE_TRANSITION");
            foreach(string destination in new[]{"singapore","penang","rangoon"}){
                state=StoryEngine.NewGame(20260928);state.journey.destination=destination;state.nodeId="shore";
                dialogue=lifePanel=false;EnterNode(true);ResetView();yield return new WaitForSeconds(.4f);
                CameraRequire(region.title.Contains(LifeJourney.DestinationName(state)),"DESTINATION_TITLE");
                yield return CaptureWorld("2.0_抵达_"+destination);
            }
            state=StoryEngine.NewGame(20260928);state.journey.destination="penang";state.nodeId="first_pay";
            dialogue=false;EnterNode(true);lifePanel=true;yield return CaptureWorld("2.0_自主谋生");
            foreach(string job in new[]{"dock","shop","courier"}){
                StartLifeAction(job);CameraRequire(state.journey.pendingJob==job,"JOB_SELECTED");
                if(mission.activity=="inspect"){
                    yield return WalkFirstPerson(inspectionPositions[0],"life shop tally");Interact();CameraRequire(puzzle,"LIFE_TALLY_OPENS");
                    AnswerInspection(InspectionCases.Get(TaskNode,0).correct);CameraRequire(Complete,"LIFE_TALLY_DONE");
                }else{
                    yield return WalkFirstPerson(mission.itemPosition,"life "+job+" pickup");Interact();CameraRequire(carrying,"LIFE_WORK_ITEM");
                    var s=new WorldSave{story=state,missionNode=state.nodeId,layoutVersion=2,progress=progress,carrying=carrying,inspected=inspected,px=player.transform.position.x,pz=player.transform.position.z};
                    CameraRequire(WorldSaveStore.IsValid(JsonUtility.FromJson<WorldSave>(JsonUtility.ToJson(s))),"LIFE_HELD_SAVE");
                    yield return WalkFirstPerson(mission.destination,"life "+job+" delivery");Interact();CameraRequire(Complete&&!carrying,"LIFE_WORK_DELIVERED");
                }
                yield return WalkFirstPerson(mission.npcPosition,"life "+job+" wages");Interact();
                CameraRequire(lifePanel&&string.IsNullOrEmpty(state.journey.pendingJob),"LIFE_WAGES_SETTLED");
                if(LifeJourney.PendingHardship(state)!=null){
                    yield return CaptureWorld("2.0_谋生受挫");
                    CameraRequire(LifeJourney.ResolveHardship(state,"evidence",out lifeNotice),"LIFE_HARDSHIP_RESPONSE");
                }
            }
            CameraRequire(LifeJourney.CanFinish(state),"LIFE_FINISH_AFTER_THREE");
            lifePanel=false;OpenPersonalLetter();state.personalLetterDraft.body="母亲：\n街口的委屈还在心里，同乡陪我留了凭据。今天送到一封批，才知道每个门牌后面都有人等。\n我会留好饭钱，也记着家里的灯。";
            state.personalLetterDraft.intent="truth";state.personalLetterDraft.amount=2;personalLetterPreview=true;
            yield return CaptureWorld("2.0_自己的侨批");string ownText=state.personalLetterDraft.body;
            CameraRequire(PersonalLetters.TrySend(state,state.personalLetterDraft.token,out var message),"PERSONAL_LETTER_SENT");
            CameraRequire(state.letters.Exists(l=>l.body==ownText),"PERSONAL_LETTER_EXACT_TEXT");
            ClosePersonalLetter();journal=true;yield return CaptureWorld("2.0_侨批与家人回音");journal=false;
            FinishLifePhase();CameraRequire(!LifeJourney.IsActive(state)&&state.nodeId=="first_pay","LIFE_RETURN_TO_STORY");
            CameraRequire(!soundEnabled&&!voiceEnabled&&!musicDirector.MusicEnabled,"LIFE_MUTED");
            Debug.Log("LIFE_REVIEW_PASSED: prologue, 3 destination scenes, physical dock/shop/courier work, hardship, held save, exact personal letter, continue story; all muted.");
        }
    }
}
