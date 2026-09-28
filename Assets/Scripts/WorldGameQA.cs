using System;
using System.IO;
using System.Collections;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        IEnumerator WalkCheck(Vector3 destination,string label)
        {
            Physics.SyncTransforms();
            walkRoute=WalkPath.Find(player.transform.position,destination,loadedWorld);routeIndex=0;
            if(walkRoute.Count==0&&Dist(destination)>2)throw new Exception("WORLD_QA_NO_PATH "+label+" start="+player.transform.position+" target="+destination);
            float start=Time.time;
            while(Dist(destination)>1.6f&&Time.time-start<45)yield return null;
            if(Dist(destination)>2.0f)throw new Exception("WORLD_QA_STUCK "+label+" at="+player.transform.position);
            walkRoute.Clear();routeIndex=0;
            Debug.Log("WORLD_QA_WALK_OK "+label);
            yield return null;
        }
        IEnumerator CaptureWorld(string name)
        {
            yield return new WaitForSeconds(.8f);
            // iOS adds persistentDataPath internally; an absolute path is doubled there.
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?name+".png":Path.Combine(Application.persistentDataPath,name+".png"));
            yield return new WaitForSeconds(.2f);
        }
        IEnumerator RunWorldQA()
        {
            yield return new WaitForSeconds(2);
            if(!model.transform.Find("BlenderModel"))throw new Exception("WORLD_QA_BLENDER_MODEL_MISSING");
            var bounds=new Bounds();bool first=true;foreach(var r in model.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            if(bounds.size.y<1.5f||bounds.size.y>2.6f)throw new Exception("WORLD_QA_MODEL_SCALE "+bounds);
            Debug.Log("WORLD_QA_BLENDER_HEIGHT="+bounds.size.y);
            yield return CaptureWorld("泉州场景v11");
            yield return WalkCheck(restPosition,"Tea rest");int oldHealth=state.health,oldMoney=state.money;Interact();
            if(state.health!=oldHealth+8||state.money!=oldMoney-2)throw new Exception("WORLD_QA_REST_FAILED");
            Interact();if(state.money!=oldMoney-2)throw new Exception("WORLD_QA_REST_REPEATED");
            yield return WalkCheck(mission.itemPosition,"Quanzhou collect");Interact();
            if(!Complete)throw new Exception("WORLD_QA_COLLECT_FAILED");
            yield return WalkCheck(mission.npcPosition,"Quanzhou mother");Interact();
            if(!dialogue)throw new Exception("WORLD_QA_TALK_FAILED");
            Choose("ledger");if(!state.awaitingContinue)throw new Exception("WORLD_QA_CHOICE_FAILED");
            Next();if(state.nodeId!="funding")throw new Exception("WORLD_QA_NEXT_FAILED");
            Debug.Log("WORLD_QA_STORY_LINK_OK");
            state=StoryEngine.NewGame();state.nodeId="passage";dialogue=false;EnterNode();yield return new WaitForSeconds(1);
            yield return WalkCheck(mission.npcPosition,"Harbor boarding");
            yield return CaptureWorld("码头场景v11");Interact();if(loadedWorld!="ship"||dialogue)throw new Exception("WORLD_QA_BOARD_FAILED");
            yield return WalkCheck(mission.npcPosition,"Ship deck to Xusheng");Interact();if(!dialogue)throw new Exception("WORLD_QA_SHIP_TALK_FAILED");
            state=StoryEngine.NewGame();state.nodeId="courier";dialogue=false;EnterNode();yield return new WaitForSeconds(1);
            yield return WalkCheck(mission.itemPosition,"Singapore pickup");Interact();if(!carrying)throw new Exception("WORLD_QA_CARRY_FAILED");
            yield return WalkCheck(mission.destination,"Singapore delivery");Interact();if(!Complete||carrying)throw new Exception("WORLD_QA_DELIVERY_FAILED");
            yield return CaptureWorld("南洋场景v11");
            state=StoryEngine.NewGame();state.nodeId="records";dialogue=false;EnterNode();yield return new WaitForSeconds(.5f);
            for(int i=0;i<3;i++){
                yield return WalkCheck(inspectionPositions[i],"Inspect "+i);Interact();if(!puzzle)throw new Exception("WORLD_QA_INSPECT_FAILED "+i);
                var c=InspectionCases.Get(state.nodeId,i);int credit=state.trust;
                AnswerInspection((c.correct+1)%3);if(!puzzle||state.trust!=credit)throw new Exception("WORLD_QA_WRONG_ANSWER_CHANGED_STATE");
                if(i==0)yield return CaptureWorld("侨批核对v11");
                AnswerInspection(c.correct);
            }
            if(!Complete||state.trust!=45)throw new Exception("WORLD_QA_INSPECT_PROGRESS_FAILED");
            yield return WalkCheck(sidePosition,"Side job pickup");Interact();if(!sideCarrying)throw new Exception("WORLD_QA_SIDE_START");
            yield return WalkCheck(sideDestination,"Side job dropoff");int cash=state.money;Interact();if(sideCarrying||state.money!=cash+6)throw new Exception("WORLD_QA_SIDE_DROP");
            Debug.Log("WORLD_QA_ALL_PASSED: physical collect, talk, branch, continue, 3D pathfinding, harbor board, carry-deliver, three inspection points.");
            NewGame();yield return CaptureWorld("泉州侨批3D首屏v11");
        }
    }
}
