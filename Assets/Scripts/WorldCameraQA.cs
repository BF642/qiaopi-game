using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace Qiaopi
{
    public partial class WorldGame
    {
        static void CameraRequire(bool condition,string message)
        {
            if(!condition)throw new Exception("CAMERA_QA_"+message);
        }
        void RequireFirstPerson(string context)
        {
            Vector3 eye=player.transform.position+Vector3.up*1.60f;
            CameraRequire(Vector3.Distance(cam.transform.position,eye)<.005f,"EYE_POSITION "+context);
            CameraRequire(!cam.orthographic&&cam.fieldOfView>=47.9f&&cam.fieldOfView<=82.1f,"PROJECTION "+context);
            CameraRequire(cam.nearClipPlane<.06f&&Mathf.Abs(cam.transform.eulerAngles.z)<.05f,"CLIP_OR_ROLL "+context);
            foreach(var renderer in hiddenBody)CameraRequire(renderer.shadowCastingMode==ShadowCastingMode.ShadowsOnly,"BODY_BLOCKS_VIEW "+renderer.name);
            CameraRequire((cam.cullingMask&(1<<29))==0&&heldCamera.cullingMask==(1<<29),"HELD_ITEM_LAYER");
        }
        IEnumerator WalkFirstPerson(Vector3 destination,string label)
        {
            int frames=0;IEnumerator walking=WalkCheck(destination,label);
            while(walking.MoveNext()){
                yield return walking.Current;
                RequireFirstPerson(label+" frame "+frames++);
                // The eye stays inside the collision capsule, never in a wall along the route.
                foreach(var c in Physics.OverlapSphere(cam.transform.position,.045f,~((1<<2)|(1<<29)|(1<<30)),QueryTriggerInteraction.Ignore))
                    CameraRequire(!world||!c.transform.IsChildOf(world.transform),"EYE_INSIDE_WORLD "+label+" "+c.name);
            }
            Debug.Log("FIRST_PERSON_WALK_OK "+label+" frames="+frames);
        }
        IEnumerator RunCameraQA()
        {
            yield return new WaitForSeconds(1);
            string[] nodes={"home","passage","passage","shore","shop","first_pay","courier"};
            for(int i=0;i<WorldRegions.All.Length;i++) {
                state=StoryEngine.NewGame();state.nodeId=nodes[i];if(i==2)state.flags.Add("aboard_passage");
                dialogue=journal=map=help=puzzle=fieldNote=gallery=false;EnterNode(true);ResetView();
                yield return new WaitForSeconds(.4f);
                RequireFirstPerson(loadedWorld+" entry");
                Vector3 actorBefore=player.transform.position,eyeBefore=cam.transform.position;
                var rotationBefore=cam.transform.rotation;
                RotateView(new Vector2(83,-33));
                CameraRequire(Quaternion.Angle(rotationBefore,cam.transform.rotation)>45,"MOUSELOOK_DID_NOT_TURN");
                CameraRequire(Vector3.Distance(eyeBefore,cam.transform.position)<.005f,"LOOK_ORBITED_AWAY_FROM_EYES");
                CameraRequire(Mathf.Abs(player.transform.position.x-actorBefore.x)<.005f&&Mathf.Abs(player.transform.position.z-actorBefore.z)<.005f,"LOOK_MOVED_BODY");
                RotateView(new Vector2(1440,10000));CameraRequire(Mathf.Abs(ViewPitch-75)<.01f,"PITCH_DOWN_LIMIT");RequireFirstPerson("look down");
                RotateView(new Vector2(-2880,-20000));CameraRequire(Mathf.Abs(ViewPitch+75)<.01f,"PITCH_UP_LIMIT");RequireFirstPerson("look up");
                foreach(float fov in new[]{48f,82f}){firstPersonFov=fov;UpdateCamera(true);RequireFirstPerson("field of view");}
                ResetView();CameraRequire(Mathf.Abs(cam.fieldOfView-72)<.01f,"RESET_FOV");
                float yaw=ViewYaw,pitch=ViewPitch;
                dialogue=true;RotateView(new Vector2(30,10));
                CameraRequire(Mathf.Abs(Mathf.DeltaAngle(yaw,ViewYaw))<.01f&&Mathf.Abs(pitch-ViewPitch)<.01f,"DIALOGUE_LOCK");dialogue=false;
                gallery=true;RotateView(new Vector2(30,10));CameraRequire(Mathf.Abs(Mathf.DeltaAngle(yaw,ViewYaw))<.01f,"GALLERY_LOCK");gallery=false;
                toastUntil=0;
                yield return CaptureWorld("第一人称_"+loadedWorld+"_入口");
                Debug.Log("FIRST_PERSON_REGION_OK "+loadedWorld+": eye-height position, independent look, pitch limits, FOV, dialogue and gallery locks");
            }
            state=StoryEngine.NewGame();state.nodeId="home";EnterNode(true);ResetView();
            yield return WalkFirstPerson(mission.itemPosition,"Quanzhou luggage");Interact();CameraRequire(Complete,"PICKUP");
            yield return WalkFirstPerson(mission.npcPosition,"Quanzhou mother");Interact();
            CameraRequire(dialogue,"MOTHER_TALK");EnsureConversation();UpdateConversation();
            yield return new WaitForSeconds(.2f);CameraRequire(!heldCamera.enabled,"HANDS_IN_DIALOGUE");
            yield return CaptureWorld("第一人称_与母亲交谈");
            Choose("ledger");CameraRequire(state.awaitingContinue,"CHOICE");Next();CameraRequire(state.nodeId=="funding","CONTINUE");RequireFirstPerson("story transition");
            state=StoryEngine.NewGame();state.nodeId="courier";dialogue=false;EnterNode(true);ResetView();
            yield return WalkFirstPerson(mission.itemPosition,"Letter pickup");Interact();yield return null;
            CameraRequire(carrying&&heldRig.activeSelf&&heldLetter.activeSelf&&!heldBox.activeSelf,"LETTER_IN_HANDS");
            cameraPitch=18;UpdateCamera(true);toastUntil=0;yield return CaptureWorld("第一人称_手中的侨批");
            yield return WalkFirstPerson(mission.destination,"Letter delivery");Interact();yield return null;
            CameraRequire(Complete&&!carrying&&!heldRig.activeSelf,"LETTER_DELIVERY");
            yield return WalkFirstPerson(sidePosition,"Paid work pickup");Interact();yield return null;
            CameraRequire(sideCarrying&&heldBox.activeSelf&&heldRig.activeSelf,"CRATE_IN_HANDS");
            toastUntil=0;yield return CaptureWorld("第一人称_搬货谋生");
            yield return WalkFirstPerson(sideDestination,"Paid work delivery");Interact();yield return null;
            CameraRequire(!sideCarrying&&!heldRig.activeSelf,"PAID_WORK_DELIVERY");
            // The three-way inspection and map remain usable from the eye-level view.
            state=StoryEngine.NewGame();state.nodeId="shop";EnterNode(true);ResetView();
            yield return WalkFirstPerson(inspectionPositions[0],"Inspect shop record");Interact();CameraRequire(puzzle,"INSPECTION_OPENS");
            AnswerInspection(InspectionCases.Get(state.nodeId,0).correct);CameraRequire(!puzzle&&progress==1,"INSPECTION_COMPLETES");
            map=true;yield return CaptureWorld("第一人称_地图与任务");map=false;
            CameraRequire(!soundEnabled&&!voiceEnabled&&!musicDirector.MusicEnabled,"EXPECTED_SILENT_REVIEW");
            Debug.Log("FIRST_PERSON_ALL_PASSED: seven regions; eye-level collision-free walking; physical collect, talk, story choice, letter delivery, paid work, inspection and map; all audio muted.");
            Application.Quit();
        }
    }
}
