using System;
using System.Collections;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        IEnumerator RunRegionReview()
        {
            yield return new WaitForSeconds(2);
            map=true;mapTab=0;GuideToMission();
            if(map||walkRoute.Count==0)throw new Exception("REGION_GUIDE_NOT_READY");
            float until=Time.time+35;while(Dist(Target())>1.7f&&Time.time<until)yield return null;
            if(Dist(Target())>1.7f)throw new Exception("REGION_GUIDE_DID_NOT_ARRIVE");
            walkRoute.Clear();routeIndex=0;
            yield return CaptureWorld("新版大地图_陈家院落");
            map=true;mapTab=0;yield return CaptureWorld("新版大地图_地图界面");map=false;
            state=StoryEngine.NewGame();state.nodeId="passage";EnterNode();yield return null;
            controller.enabled=false;player.transform.position=new Vector3(20,.1f,22);controller.enabled=true;yield return null;yield return null;
            if(!region.IsGround(player.transform.position)||Mathf.Abs(player.transform.position.x-20)>.01f)throw new Exception("REGION_COAST_RUNTIME");
            var save=new WorldSave{story=state,missionNode="passage",layoutVersion=2,px=player.transform.position.x,pz=player.transform.position.z};
            if(!WorldSaveStore.IsValid(save))throw new Exception("REGION_COAST_SAVE_REJECTED");
            state.flags.Add("aboard_passage");EnterNode();yield return new WaitForSeconds(1);firstPersonFov=82f;UpdateCamera(true);yield return CaptureWorld("新版大地图_海船全貌");firstPersonFov=72f;UpdateCamera(true);
            state=StoryEngine.NewGame();state.nodeId="courier";EnterNode();yield return new WaitForSeconds(1);
            int startFrames=Time.frameCount;float startTime=Time.realtimeSinceStartup;yield return new WaitForSeconds(2);
            Debug.Log("REGION_POSTOFFICE_OBSERVED_FPS="+((Time.frameCount-startFrames)/(Time.realtimeSinceStartup-startTime)));
            firstPersonFov=82f;UpdateCamera(true);yield return CaptureWorld("新版大地图_侨批局全貌");firstPersonFov=72f;UpdateCamera(true);
            Debug.Log("REGION_REVIEW_ALL_PASSED: map guidance, shore position and save validity, refreshed scene captures.");
            NewGame();
        }
        IEnumerator RunRegionQA()
        {
            string[] nodes={"home","passage","passage","shore","shop","first_pay","courier"};
            for(int i=0;i<WorldRegions.All.Length;i++){
                state=StoryEngine.NewGame();state.nodeId=nodes[i];if(i==2)state.flags.Add("aboard_passage");dialogue=journal=map=help=puzzle=fieldNote=false;EnterNode();
                yield return new WaitForSeconds(1);
                if(loadedWorld!=WorldRegions.All[i])throw new Exception("REGION_RUNTIME_WRONG_WORLD");
                float savedFov=firstPersonFov;firstPersonFov=82f;UpdateCamera(true);
                yield return CaptureWorld("大地图_"+loadedWorld+"_入口远景");firstPersonFov=savedFov;UpdateCamera(true);
                yield return WalkCheck(mission.npcPosition,"Region "+loadedWorld+" destination");
                RequireFirstPerson("region navigation");
                yield return CaptureWorld("大地图_"+loadedWorld+"_实际探索");
                map=true;mapTab=0;yield return CaptureWorld("大地图_"+loadedWorld+"_地图");map=false;
                if(i==0){
                    yield return WalkCheck(region.notes[1].position,"Read village note");Interact();
                    if(!fieldNote||!state.flags.Contains("note_"+region.notes[1].id))throw new Exception("REGION_NOTE_NOT_RECORDED");
                    fieldNote=false;
                }
                Debug.Log("REGION_RUNTIME_WALK_OK "+loadedWorld);
            }
            NewGame();yield return RunWorldQA();
            Debug.Log("REGION_RUNTIME_ALL_PASSED: seven physical scene walks, moving camera, notes, two-stage boarding and full existing collect/deliver/inspect loop.");
        }
    }
}
