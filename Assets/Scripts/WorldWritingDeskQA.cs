using System;
using System.Collections;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool WritingQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-writing-test") >= 0;

        IEnumerator WalkToWritingDeskForQA()
        {
            CameraRequire(writingPlace != null, "WRITING_PLACE_AVAILABLE_" + loadedWorld);
            Vector3 before = player.transform.position;
            OpenPersonalLetter();
            CameraRequire(Vector3.Distance(before, player.transform.position) < .001f, "WRITING_REQUEST_NO_TELEPORT");
            if (!letterEditor) {
                CameraRequire(writingRequested && !Blocked, "WRITING_WALK_STARTED_" + loadedWorld);
                float deadline = Time.time + 50;
                while (Time.time < deadline && routeIndex < walkRoute.Count) { yield return null; RequireFirstPerson("walking to writing desk"); }
                CameraRequire(NearWritingDesk, "WRITING_DESK_REACHABLE_" + loadedWorld + " position=" + player.transform.position);
                CameraRequire(!letterEditor, "WRITING_REQUIRES_SEATED_INTERACTION");
                Interact();
            }
            CameraRequire(letterEditor && writingSeated, "WRITING_SEATED_" + loadedWorld);
            yield return new WaitForSeconds(.6f);
            CameraRequire(Vector3.Distance(cam.transform.position, writingPlace.seatEye) < .02f, "WRITING_CAMERA_AT_SEAT");
            CameraRequire(Blocked && !heldCamera.enabled && PersonalLetterTyping, "WRITING_LOCKS_MOVEMENT_AND_SHORTCUTS");
            foreach (var c in Physics.OverlapSphere(cam.transform.position, .06f, ~((1<<2)|(1<<29)|(1<<30)), QueryTriggerInteraction.Ignore))
                CameraRequire(!world || !c.transform.IsChildOf(world.transform), "WRITING_EYE_INSIDE_SCENERY_" + c.name);
        }

        IEnumerator RunWritingDeskQA()
        {
            yield return new WaitForSeconds(.5f);
            string[] nodes = {"peace", "passage", "shore", "shop", "first_pay", "first_letter"};
            for (int i = 0; i < nodes.Length; i++) {
                if (letterEditor) ClosePersonalLetter();
                state = StoryEngine.NewGame(20260928); state.nodeId = nodes[i];
                state.journey.destination = i % 3 == 0 ? "singapore" : i % 3 == 1 ? "penang" : "rangoon";
                dialogue = journal = map = help = gallery = lifePanel = false; EnterNode(true); ResetView();
                PersonalLetters.EnsureDraft(state).body = "母亲：\n今晚在桌前给家里写一封信。";
                string originalDraft = state.personalLetterDraft.body;
                int money = state.money, health = state.health, steps = progress, turns = state.journey.turns;
                yield return WalkToWritingDeskForQA();
                CameraRequire(state.personalLetterDraft.body == originalDraft, "WRITING_PRESERVES_DRAFT");
                CameraRequire(state.money == money && state.health == health && progress == steps && state.journey.turns == turns, "WRITING_NO_GAMEPLAY_COST");
                yield return CaptureWorld("2.0.4_落座写批_" + loadedWorld + (MobileControls ? "_手机" : ""));
                Vector3 feet = player.transform.position;
                var saved = new WorldSave { story = state, missionNode = state.nodeId, layoutVersion = 2, progress = progress, carrying = carrying, sideCarrying = sideCarrying, inspected = inspected, px = feet.x, pz = feet.z };
                CameraRequire(WorldSaveStore.IsValid(JsonUtility.FromJson<WorldSave>(JsonUtility.ToJson(saved))), "WRITING_SEATED_SAVE_VALID");
                ClosePersonalLetter();
                CameraRequire(!letterEditor && !writingSeated && Vector3.Distance(player.transform.position, feet) < .001f, "WRITING_STANDS_WITHOUT_TELEPORT");
                RequireFirstPerson("after writing desk");
                Debug.Log("WRITING_PLACE_PASSED " + loadedWorld + " " + state.journey.destination);
            }
            // A carried job must never be discarded by the global writing shortcut.
            state = StoryEngine.NewGame(20260928); state.nodeId = "courier";
            dialogue = journal = map = help = lifePanel = false; EnterNode(true);
            carrying = true; journal = true; RefreshProps(); Vector3 parcelPosition = player.transform.position;
            OpenPersonalLetter(); CameraRequire(carrying && !writingRequested && !letterEditor && !Blocked && toastUntil > Time.time && player.transform.position == parcelPosition, "WRITING_CARRIED_JOB_PRESERVED");
            carrying = false; sideCarrying = true; OpenPersonalLetter(); CameraRequire(sideCarrying && !writingRequested && !letterEditor, "WRITING_SIDE_JOB_PRESERVED");
            sideCarrying = false; RefreshProps();
            OpenPersonalLetter(); CameraRequire(writingRequested, "WRITING_ROUTE_CAN_START"); CancelWritingWalk();
            CameraRequire(!writingRequested && walkRoute.Count == 0, "WRITING_ROUTE_CAN_CANCEL");
            state.nodeId = "passage"; state.flags.Add("aboard_passage"); EnterNode(true);
            OpenPersonalLetter(); CameraRequire(writingPlace == null && !letterEditor && !writingRequested, "WRITING_SHIP_NO_ROADSIDE_EDITOR");
            CameraRequire(!soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled, "WRITING_ALL_MUTED");
            Debug.Log("WRITING_DESK_ALL_PASSED: six physical writing places, three destinations, safe paths, explicit sitting, camera restore, unchanged task/turn/money, preserved drafts and valid saves, carried work, cancellation and ship restriction.");
            Application.Quit();
        }
    }
}
