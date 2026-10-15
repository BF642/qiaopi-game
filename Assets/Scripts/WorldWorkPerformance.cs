using System;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool PerformanceQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-performance-test") >= 0;
        bool WorkPerformanceBusy => !string.IsNullOrEmpty(workPerformanceKind);
        string WorkPerformanceKind => workPerformanceKind;
        float WorkEyeDip => WorkPerformanceBusy && (workPerformanceKind == "dock-pick" || workPerformanceKind == "dock-drop") ? Mathf.Sin(WorkPerformanceProgress * Mathf.PI) * .12f : 0;
        float WorkPerformanceProgress => WorkPerformanceBusy ? Mathf.Clamp01((Time.unscaledTime - workPerformanceStarted) / workPerformanceDuration) : 1;
        Transform WorkPerformanceVisual => heldRig ? heldRig.transform : null;
        string workPerformanceKind = "", workPerformanceShift = "", performanceDialogueKey = "";
        float workPerformanceStarted, workPerformanceDuration = 1;
        bool workPerformanceReopen, workPerformanceFramed;
        float workPerformanceRestYaw, workPerformanceRestPitch;
        int workPerformanceParcelKind, workPerformanceAmount, workPerformanceOldValue;
        Vector3 workPerformanceTarget;
        GameObject workPerformanceHidden, workShopCustomer;
        CharacterPerformance workPerformanceActor, attentiveActor;
        Transform workSignaturePen, workSignaturePaper, workReceivedLetter;

        // Rules commit and save first. This short presentation owns no rewards
        // or persistent progress, so cancelling/reloading cannot repeat a task.
        void BeginWorkPerformance(string kind, Vector3 target, int amount = 0, int item = -1, int oldValue = 0)
        {
            if ((QA && !PerformanceQA) || !InTrade || !heldRig) return;
            CancelWorkPerformance();
            workPerformanceKind = kind; workPerformanceTarget = target; workPerformanceAmount = amount;
            workPerformanceOldValue = oldValue; workPerformanceStarted = Time.unscaledTime;
            workPerformanceDuration = kind == "shop-ledger" || kind == "courier-sign" ? 1.5f : kind == "dock-pick" || kind == "dock-drop" ? 1.15f : .95f;
            workPerformanceShift = state.tradeShift.route + ":" + state.tradeShift.level + ":" + state.journey.turns;
            workPerformanceReopen = state.tradeShift.route == "shop" && state.tradeShift.stage != "done";
            if (state.tradeShift.route == "shop" && kind != "shop-ledger") {
                workPerformanceRestYaw = cameraYaw; workPerformanceRestPitch = cameraPitch; workPerformanceFramed = true;
                AimTradeCamera(target + Vector3.up * .24f);
            }
            if (item >= 0) workPerformanceParcelKind = TradeWork.DockDestination(state, item);
            if (kind == "dock-drop" && item >= 0) workPerformanceHidden = tradePlacedArt[item];
            if (kind == "courier-sign") workPerformanceHidden = tradeReceiptArt;
            if (kind == "shop-ledger") workPerformanceHidden = tradeLedgerArt;
            tradePanel = workTalkPanel = false; walkRoute.Clear(); ResetMobileControls(); CancelCameraPointer();
            GameObject actor = state.tradeShift.route == "shop" ? workShopCustomer : state.tradeShift.route == "courier"
                ? kind == "courier-return" ? npc : tradePerson >= 0 && tradePerson < 2 ? tradePeople[tradePerson + 1] : null : null;
            if (actor) {
                workPerformanceActor = CharacterPerformance.For(actor);
                workPerformanceActor.LookAt(CameraEyePoint());
                workPerformanceActor.Play(kind == "courier-sign" ? CharacterGesture.Sign : kind == "courier-offer" || kind == "courier-return" || kind == "shop-pay" ? CharacterGesture.Offer : CharacterGesture.Nod, workPerformanceDuration);
            }
            if (kind == "courier-sign" && tradePerson >= 0) SetupSignaturePen(tradePerson);
            UpdateFirstPersonPresentation();
        }

        void TickWorkPerformance()
        {
            UpdateCharacterAttention();
            UpdateReceivedLetter();
            if (!WorkPerformanceBusy) return;
            if (!InTrade || workPerformanceShift != state.tradeShift.route + ":" + state.tradeShift.level + ":" + state.journey.turns) { CancelWorkPerformance(); return; }
            float p = WorkPerformanceProgress;
            if (workPerformanceHidden) workPerformanceHidden.SetActive(false);
            if (workPerformanceKind == "shop-scoop" && tradeRiceFill) {
                float fill = Mathf.Lerp(workPerformanceOldValue, state.tradeShift.weightUnits, Mathf.SmoothStep(0, 1, Mathf.Clamp01((p - .42f) / .28f)));
                tradeRiceFill.gameObject.SetActive(fill > .01f);
                tradeRiceFill.localScale = new Vector3(1, .18f + fill * .09f, 1);
                SetWorkScaleTilt(Mathf.Clamp((fill - TradeWork.ShopWeightTarget(state)) * 4, -14, 14));
            }
            if (workPerformanceKind == "shop-weigh" && tradeScaleBeam)
                SetWorkScaleTilt(Mathf.Sin(p * Mathf.PI * 4) * 5 * (1 - p));
            if (workSignaturePen) {
                workSignaturePen.gameObject.SetActive(p < .82f);
                if (workSignaturePaper) workSignaturePaper.gameObject.SetActive(p < .82f);
                float draw = Mathf.Clamp01((p - .22f) / .55f);
                if (workPerformanceActor && workSignaturePaper) {
                    Vector3 left = workPerformanceActor.LeftHandPoint, right = workPerformanceActor.RightHandPoint;
                    workSignaturePaper.position = left + workPerformanceActor.transform.right * .10f + Vector3.up * .004f;
                    workSignaturePaper.rotation = workPerformanceActor.transform.rotation;
                    Vector3 tip = workSignaturePaper.position + Vector3.up * .004f + workPerformanceActor.transform.right * (draw * .10f - .05f)
                        + workPerformanceActor.transform.forward * (Mathf.Sin(draw * Mathf.PI * 6) * .025f);
                    workSignaturePen.position = tip;
                    workSignaturePen.rotation = Quaternion.FromToRotation(Vector3.up, right - tip);
                    workSignaturePen.localScale = new Vector3(1, Vector3.Distance(right, tip) / .21f, 1);
                }
            }
            if (p < 1) return;
            bool reopen = workPerformanceReopen;
            CancelWorkPerformance();
            if (reopen && InTrade && state.tradeShift.stage != "done" && FirstDayCanReach(TradePlaces.ShopCounter, 2.2f)) OpenTradePanel(-1, TradeShopFace());
        }

        void SetWorkScaleTilt(float angle)
        {
            Quaternion tilt = Quaternion.Euler(0, 0, angle);
            if (tradeScaleBeam) tradeScaleBeam.localRotation = tilt;
            if (tradeRicePan) tradeRicePan.localPosition = tilt * new Vector3(-.49f, 0, 0) + Vector3.down * .28f;
            if (tradeWeightPan) tradeWeightPan.localPosition = tilt * new Vector3(.49f, 0, 0) + Vector3.down * .28f;
        }

        void CancelWorkPerformance()
        {
            workPerformanceKind = ""; workPerformanceReopen = false;
            if (workPerformanceFramed) { cameraYaw = workPerformanceRestYaw; cameraPitch = workPerformanceRestPitch; workPerformanceFramed = false; if (cam && player) UpdateCamera(true); }
            if (workPerformanceHidden) workPerformanceHidden.SetActive(true);
            workPerformanceHidden = null;
            if (workSignaturePen) { workSignaturePen.parent.gameObject.SetActive(false); Destroy(workSignaturePen.parent.gameObject); workSignaturePen = workSignaturePaper = null; }
            if (workPerformanceActor) workPerformanceActor.ResetPose(); workPerformanceActor = null;
            if (heldRig) { heldRig.transform.localPosition = new Vector3(0, -.37f, .66f); heldRig.transform.localRotation = Quaternion.identity; }
            tradeArtWeight = tradeArtChange = tradeArtDebt = -1;
            UpdateTradeArt(); UpdateFirstPersonPresentation();
        }

        void PresentTradeAction(string action, int amount, int oldValue, string oldStage)
        {
            string kind = action == "add-weight" ? "shop-scoop" : action == "confirm-weight" ? "shop-weigh" :
                action == "add-change" ? "shop-coin" : action == "confirm-change" ? "shop-pay" :
                action == "confirm-debt" ? "shop-ledger" : action == "read" ? "courier-take" :
                action == "deliver" ? "courier-offer" : action == "receipt" ? "courier-sign" : action == "register-return" ? "courier-return" : "";
            if (string.IsNullOrEmpty(kind)) return;
            Vector3 target = state.tradeShift.route == "shop" ? kind == "shop-ledger" ? new Vector3(-13.4f, .98f, 13.52f) : TradeShopFace(oldStage) :
                kind == "courier-take" ? TradePlaces.CourierReadFace : kind == "courier-return" ? mission.npcPosition + Vector3.up : TradePlaces.CourierFace(Mathf.Max(0, tradePerson));
            BeginWorkPerformance(kind, target, amount, -1, oldValue);
        }

        void SetupWorkPerformanceActors()
        {
            workShopCustomer = null; workReceivedLetter = null; attentiveActor = null; performanceDialogueKey = "";
            if (InTrade && state.tradeShift.route == "courier") { UpdateReceivedLetter(); return; }
            if (!InTrade || state.tradeShift.route != "shop") return;
            workShopCustomer = CharacterAppearanceProfiles.Spawn("rice_customer", actors.transform, WorldFactory.CharacterGroundPosition(new Vector3(-16.55f, 0, 15.0f)));
            if (!workShopCustomer) return;
            workShopCustomer.name = "买米的街坊"; workShopCustomer.AddComponent<CharacterIdle>();
            var motion = CharacterPerformance.For(workShopCustomer); motion.LookAt(TradePlaces.ShopFace);
            if (!string.IsNullOrEmpty(WorkEvents.ChoiceId(state))) motion.Play(CharacterGesture.Nod, 1.1f);
        }

        void SetupSignaturePen(int person)
        {
            var root = new GameObject("当面签收 · 毛笔").transform; root.SetParent(actors.transform, false);
            workSignaturePaper = TradeScenery.Letter(root, Vector3.zero, "收件人手中的回执", true).transform;
            workSignaturePaper.localScale = new Vector3(1, .18f, 1);
            workSignaturePen = TradeScenery.Group(root, "书写中的笔", Vector3.zero);
            var g = new TradeScenery.Geometry(workSignaturePen);
            g.Beam(Vector3.up * .027f, Vector3.up * .21f, .009f, TradeScenery.Wood);
            g.Beam(Vector3.zero, Vector3.up * .027f, .007f, TradeScenery.Ink); g.Flush();
        }

        void UpdateReceivedLetter()
        {
            bool received = InTrade && state.tradeShift.route == "courier" && state.tradeShift.stage == "receipt" && state.tradeShift.recipient >= 0;
            if (!received) { if (workReceivedLetter) workReceivedLetter.gameObject.SetActive(false); return; }
            var person = tradePeople[state.tradeShift.recipient + 1]; if (!person) return;
            var pose = CharacterPerformance.For(person); pose.SetCarrying(true);
            if (!workReceivedLetter) workReceivedLetter = TradeScenery.Letter(actors.transform, Vector3.zero, "收件人已接过的银信", false).transform;
            workReceivedLetter.gameObject.SetActive(workPerformanceKind != "courier-offer" || WorkPerformanceProgress >= .74f);
            workReceivedLetter.position = (pose.LeftHandPoint + pose.RightHandPoint) * .5f + Vector3.up * .025f;
            workReceivedLetter.rotation = person.transform.rotation;
        }

        void UpdateCharacterAttention()
        {
            GameObject actor = null;
            if (tradePanel && InTrade) actor = workTalkPanel ? npc : state.tradeShift.route == "dock" ? workEventPartner : state.tradeShift.route == "shop" ? workShopCustomer : tradePerson >= 0 ? tradePeople[tradePerson + 1] : state.tradeShift.stage == "ask" ? tradePeople[0] : npc;
            else if (ConversationVisible || firstDayPanel || nanyangPanel) actor = npc;
            else if (!Blocked && npc && Dist(npc.transform.position) < 3) actor = npc;
            var next = actor ? CharacterPerformance.For(actor) : null;
            if (attentiveActor && attentiveActor != next && attentiveActor != workPerformanceActor) attentiveActor.ClearLook();
            attentiveActor = next; if (attentiveActor) attentiveActor.LookAt(CameraEyePoint());
            if (!(ConversationVisible || firstDayPanel || nanyangPanel) || !portraitActor || !portraitCamera) return;
            var portrait = CharacterPerformance.For(portraitActor); portrait.LookAt(portraitCamera.transform.position);
            string key = portraitId + ":" + conversationKey + ":" + spokenIndex;
            if (key == performanceDialogueKey) return;
            performanceDialogueKey = key; portrait.Play(CharacterGesture.Nod, 1.3f);
            if (attentiveActor) attentiveActor.Play(CharacterGesture.Nod, 1.3f);
        }

        void DrawWorkPerformanceOverlay()
        {
            if (!WorkPerformanceBusy) return;
            string title = workPerformanceKind == "dock-pick" ? "弯腰取货" : workPerformanceKind == "dock-drop" ? "放妥货物" :
                workPerformanceKind == "shop-scoop" ? workPerformanceAmount < 0 ? "取回多称的米" : "舀米入秤" : workPerformanceKind == "shop-weigh" ? "扶秤看准斤两" :
                workPerformanceKind == "shop-coin" ? "点钱入盘" : workPerformanceKind == "shop-pay" ? "当面核清货钱" : workPerformanceKind == "shop-ledger" ? "落笔记账" :
                workPerformanceKind == "courier-take" ? "收好银信" : workPerformanceKind == "courier-offer" ? "亲手交批" : workPerformanceKind == "courier-sign" ? "当面签收，取回执" : "原封交回柜台";
            HudText(new Rect(530, 865, 540, 49), title, 25, hudText, true, TextAnchor.MiddleCenter);
            if (HudAction(new Rect(660, 925, 280, 47), MobileControls ? "收起动作" : "收起动作 · Esc", false, 19)) CancelWorkPerformance();
        }
    }
}
