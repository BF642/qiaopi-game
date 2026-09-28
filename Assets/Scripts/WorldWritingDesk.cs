using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool writingRequested, writingArrivalShown, writingSeated;
        float writingSeatStarted, writingReturnYaw, writingReturnPitch, writingReturnFov;
        Vector3 writingCameraFrom;
        Quaternion writingRotationFrom;

        bool NearWritingDesk
        {
            get {
                if (writingPlace == null || carrying || sideCarrying || Dist(writingPlace.approach) > 1.25f) return false;
                Vector3 start = player.transform.position + Vector3.up * .85f;
                Vector3 end = writingPlace.approach + Vector3.up * .85f;
                return !Physics.Linecast(start, end, ~((1 << 2) | (1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore);
            }
        }

        string WritingEntryTitle => writingPlace == null ? "上岸后写批" : NearWritingDesk ? "落座写批" : writingRequested ? "取消前往书桌" : "去写批处";
        string WritingObjective => "前往" + (writingPlace == null ? "写批处" : writingPlace.title);

        void RequestWritingDesk()
        {
            if (carrying || sideCarrying) { ExplainWritingDeskUnavailable("先交付手中的货物，再落座写批。草稿会保留。"); return; }
            if (writingPlace == null) { ExplainWritingDeskUnavailable("船上没有写批桌。等上岸后，再找桌案写信；草稿仍会保留。"); return; }
            if (NearWritingDesk) { BeginPersonalLetterAtDesk(); return; }
            var route = WalkPath.Find(player.transform.position, writingPlace.approach, loadedWorld);
            if (route.Count == 0) { ExplainWritingDeskUnavailable("这里暂时走不到书桌。先回到附近的道路，再试一次。"); return; }
            journal = map = help = gallery = fieldNote = lifePanel = dialogue = false;
            writingRequested = true; writingArrivalShown = false;
            walkRoute = route; routeIndex = 0;
            CancelCameraPointer(); ResetMobileControls();
            Toast("前往" + writingPlace.title + "。到桌前后" + (MobileControls ? "点「落座」" : "按 E") + "写批。");
        }

        void ExplainWritingDeskUnavailable(string reason)
        {
            journal = map = help = gallery = fieldNote = lifePanel = dialogue = false;
            Toast(reason);
        }

        void CancelWritingWalk()
        {
            writingRequested = writingArrivalShown = false;
            walkRoute.Clear(); routeIndex = 0;
        }

        void UpdateWritingDeskArrival()
        {
            if (!writingRequested || writingArrivalShown || !NearWritingDesk) return;
            // Finish the safe path to the chair, rather than stopping outside a wall.
            if (routeIndex < walkRoute.Count && Dist(writingPlace.approach) > .35f) return;
            writingArrivalShown = true; walkRoute.Clear(); routeIndex = 0;
            Toast("到了" + writingPlace.title + "，" + (MobileControls ? "点「落座」" : "按 E") + "开始写批。");
        }

        bool InteractWithWritingDesk()
        {
            if (!NearWritingDesk) return false;
            BeginPersonalLetterAtDesk(); return true;
        }

        void BeginWritingSeat()
        {
            writingReturnYaw = cameraYaw; writingReturnPitch = cameraPitch; writingReturnFov = firstPersonFov;
            writingCameraFrom = cam.transform.position; writingRotationFrom = cam.transform.rotation;
            writingSeatStarted = Time.unscaledTime; writingSeated = true;
            writingRequested = writingArrivalShown = false;
            AnimateWalk(0); UpdateFirstPersonPresentation();
        }

        void EndWritingSeat()
        {
            if (!writingSeated) return;
            writingSeated = false;
            cameraYaw = writingReturnYaw; cameraPitch = writingReturnPitch; firstPersonFov = writingReturnFov;
            UpdateCamera(true); UpdateFirstPersonPresentation();
        }

        bool PositionWritingCamera()
        {
            if (!writingSeated || !letterEditor || writingPlace == null) return false;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - writingSeatStarted) / .45f));
            Quaternion rotation = Quaternion.LookRotation(writingPlace.lookAt - writingPlace.seatEye, Vector3.up);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(writingCameraFrom, writingPlace.seatEye, t), Quaternion.Slerp(writingRotationFrom, rotation, t));
            cam.fieldOfView = Mathf.Lerp(writingReturnFov, 64f, t);
            cam.nearClipPlane = .035f;
            if (heldCamera) heldCamera.enabled = false;
            return true;
        }

        void ResetWritingDeskState()
        {
            if (letterEditor) {
                UpdatePersonalLetterKeyboard();
                if (personalLetterKeyboard != null) { personalLetterKeyboard.active = false; personalLetterKeyboard = null; }
                letterEditor = false; personalLetterDirty = false;
                Input.imeCompositionMode = IMECompositionMode.Auto;
            }
            EndWritingSeat(); writingRequested = writingArrivalShown = false;
        }
    }
}
