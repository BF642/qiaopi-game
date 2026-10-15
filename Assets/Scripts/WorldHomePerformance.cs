using UnityEngine;
using System.Collections.Generic;

namespace Qiaopi
{
    public partial class WorldGame
    {
        string homePresentationAction = "";
        float homePresentationElapsed, homePresentationDuration;
        GameObject homeHeldMeal;
        bool HomePerformanceBusy => homeArt && !string.IsNullOrEmpty(homePresentationAction);

        // Progress is committed once by HomeChapter.TryAct. This is only a short
        // visual hand-off: escape, reload and scene changes cannot pay twice or
        // roll a finished household task back into an unfinished one.
        void BeginHomePresentation(string action)
        {
            if (action == "walk_market") return;
            homePresentationAction = action;
            homePresentationElapsed = 0;
            homePresentationDuration = action == "share_meal" ? .95f : action == "store_grain" ? .85f : .70f;
            walkRoute.Clear(); CancelCameraPointer(); ResetMobileControls();
            if ((action == "rice" || action == "tea") && homeWife) {
                var pose = CharacterPerformance.For(homeWife.gameObject);
                pose.LookAt(CameraEyePoint()); pose.Play(CharacterGesture.Offer, .75f);
            } else if (action == "put_basket" && homeWife) {
                CharacterPerformance.For(homeWife.gameObject).LookAt(HomeKitchen + Vector3.up * .8f);
                CharacterPerformance.For(homeWife.gameObject).Play(CharacterGesture.Nod, .8f);
            } else if ((action == "home" || action == "return") && homeSister) {
                CharacterPerformance.For(homeSister.gameObject).LookAt(HomeLesson + Vector3.up * .6f);
                CharacterPerformance.For(homeSister.gameObject).Play(CharacterGesture.Point, .8f);
            }
        }
        void TickHomePresentation()
        {
            if (!HomePerformanceBusy) return;
            homePresentationElapsed += Time.unscaledDeltaTime;
            if (homePresentationElapsed >= homePresentationDuration) CancelHomePresentation();
        }
        void CancelHomePresentation(bool finishMealConversation = true)
        {
            bool meal = homePresentationAction == "share_meal";
            homePresentationAction = ""; homePresentationElapsed = homePresentationDuration = 0;
            if (heldRig) { heldRig.transform.localRotation = Quaternion.identity; heldRig.transform.localPosition = new Vector3(0, -.37f, .66f); }
            if (homeHeldMeal) homeHeldMeal.SetActive(false);
            if (heldHands) heldHands.SetActive(true);
            if (workHandGrip) workHandGrip.SetPose(0, 0);
            if (homeHeldBasket) homeHeldBasket.transform.localPosition = new Vector3(0, -.155f, .04f);
            if (homeHeldGrain) homeHeldGrain.transform.localRotation = Quaternion.identity;
            if (finishMealConversation && meal && state != null && state.nodeId == "peace") {
                dialogue = true; textScroll = Vector2.zero;
                // Finish serving with the table and all three relatives in view,
                // rather than retaining the standing mother's former eye height.
                Vector3 towardTable = new Vector3(-23, 1.10f, (ChenCourtyardHouse.Ready ? -.3f : -4.65f)) - CameraEyePoint();
                if (towardTable.sqrMagnitude > .01f) {
                    cameraYaw = Mathf.Atan2(towardTable.x, towardTable.z) * Mathf.Rad2Deg;
                    cameraPitch = -Mathf.Atan2(towardTable.y, new Vector2(towardTable.x, towardTable.z).magnitude) * Mathf.Rad2Deg;
                    UpdateCamera(true);
                }
            }
            if (homeArt && state != null) RefreshHomeArt();
        }
        bool ApplyHomeHandPresentation()
        {
            if (!HomePerformanceBusy || !heldRig) return false;
            string action = homePresentationAction;
            bool basket = action == "rice" || action == "tea" || action == "put_basket";
            bool grain = action == "lift_grain" || action == "store_grain";
            bool meal = action == "share_meal";
            bool hasProp = basket || grain || meal;
            if (heldLetter) heldLetter.SetActive(false); if (heldBox) heldBox.SetActive(false);
            UpdateWorkHeldParcel(false);
            if (homeHeldBasket) homeHeldBasket.SetActive(basket);
            if (homeHeldGrain) homeHeldGrain.SetActive(grain);
            if (homeHeldMeal) homeHeldMeal.SetActive(meal);
            if (heldHands) heldHands.SetActive(hasProp);
            heldRig.SetActive(hasProp);
            float p = Mathf.Clamp01(homePresentationElapsed / Mathf.Max(.01f, homePresentationDuration));
            p = p * p * (3 - 2 * p);
            if(workHandGrip) {
                float grip = basket ? 50 : grain ? 22 : meal ? 16 : 0;
                if(action=="put_basket"||meal)grip*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,1,p));
                workHandGrip.SetHouseholdPose(grip);
            }
            Vector3 restAt = new Vector3(0, -.37f, .66f);
            if (action == "put_basket" || meal) {
                heldRig.transform.localPosition = Vector3.Lerp(restAt, new Vector3(0, -.54f, .83f), p);
            } else if (action == "store_grain") {
                heldRig.transform.localPosition = Vector3.Lerp(restAt, new Vector3(0, -.31f, .80f), Mathf.Sin(p * Mathf.PI));
                if (homeHeldGrain) homeHeldGrain.transform.localRotation = Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * 35, 0, 0);
            } else heldRig.transform.localPosition = Vector3.Lerp(new Vector3(0, -.64f, .52f), restAt, p);
            heldRig.transform.localRotation = Quaternion.identity;
            if (heldCamera) heldCamera.enabled = true;
            return true;
        }
        readonly Dictionary<Transform,float> homeDetourUntil = new Dictionary<Transform,float>();
        void TryHomeFamilyDetour(Transform person,List<Vector3> route,ref int index)
        {
            if(!person||route.Count==0)return;
            if(homeDetourUntil.TryGetValue(person,out float until)&&Time.unscaledTime<until)return;
            homeDetourUntil[person]=Time.unscaledTime+1.25f;
            Vector3 goal=route[route.Count-1];
            // The player may stand on an intermediate grid waypoint while
            // waiting at the stall. Replan around their real capsule footprint
            // instead of asking them to move or letting the spouse walk through.
            var obstacle=new GameObject("家庭寻路中的玩家避让范围");
            obstacle.transform.position=player.transform.position+Vector3.up*.9f;
            var collider=obstacle.AddComponent<BoxCollider>();collider.size=new Vector3(.60f,1.5f,.60f);
            List<Vector3> detour;
            try {
                Physics.SyncTransforms();WalkPath.Invalidate();
                detour=WalkPath.Find(person.position,goal,"quanzhou");
            } finally {
                collider.enabled=false;Destroy(obstacle);Physics.SyncTransforms();WalkPath.Invalidate();
            }
            if(detour.Count>0&&Vector3.Distance(detour[detour.Count-1],goal)<.36f){
                route.Clear();route.AddRange(detour);index=0;
                Debug.Log("HOME_FAMILY_DETOUR actor="+person.name+" from="+person.position.ToString("F3")+" player="+player.transform.position.ToString("F3")+" goal="+goal.ToString("F3")+" waypoints="+route.Count);
            }else Debug.LogWarning("HOME_FAMILY_DETOUR_BLOCKED actor="+person.name+" at="+person.position.ToString("F3")+" player="+player.transform.position.ToString("F3")+" goal="+goal.ToString("F3")+" routeIndex="+index+" count="+route.Count);
        }
        bool homeConversationRetreat;
        float homeConversationRetreatStarted;
        bool HomeConversationSettling => homePanel && homeConversationRetreat;
        void MakeHomeConversationSpace(Transform person)
        {
            homeConversationRetreat = false;
            if (!person || person != homeWife || (state.homeChapter.stage != "wife" && state.homeChapter.stage != "market")) return;
            Vector3 fromPlayer = person.position - player.transform.position; fromPlayer.y = 0;
            Debug.Log("HOME_CONVERSATION_SPACE_OPEN stage=" + state.homeChapter.stage + " metres=" + fromPlayer.magnitude.ToString("F3") + " player=" + player.transform.position.ToString("F3") + " wife=" + person.position.ToString("F3"));
            if (fromPlayer.magnitude >= 1.55f) { homeWifeRoute.Clear(); homeWifeRouteIndex = 0; return; }
            Vector3 away = fromPlayer.sqrMagnitude > .005f ? fromPlayer.normalized : -person.forward;
            const int mask = ~((1 << 2) | (1 << 30));
            foreach (float angle in new[] { 0f, 35f, -35f, 70f, -70f, 105f, -105f }) {
                Vector3 candidate = player.transform.position + Quaternion.Euler(0, angle, 0) * away * 1.78f;
                candidate.y = 0;
                if (state.homeChapter.stage == "market" && Vector3.Distance(candidate, HomeMarket) > 3.0f) continue;
                if (Physics.CheckCapsule(candidate + Vector3.up * .55f, candidate + Vector3.up * 1.65f, .34f, mask, QueryTriggerInteraction.Ignore)) continue;
                Vector3 move = candidate - person.position; move.y = 0;
                if (move.magnitude > 2.2f || move.sqrMagnitude < .01f) continue;
                Vector3 bottom = new Vector3(person.position.x, .55f, person.position.z), top = new Vector3(person.position.x, 1.65f, person.position.z);
                if (Physics.CapsuleCast(bottom, top, .34f, move.normalized, move.magnitude, mask, QueryTriggerInteraction.Ignore)) continue;
                homeWifeRoute.Clear(); homeWifeRoute.Add(candidate); homeWifeRouteIndex = 0;
                homeConversationRetreat = true; homeConversationRetreatStarted = Time.unscaledTime;
                Debug.Log("HOME_CONVERSATION_SPACE_STEP goal=" + candidate.ToString("F3"));
                return;
            }
            Debug.LogWarning("HOME_CONVERSATION_SPACE_NO_CLEAR_STEP metres=" + fromPlayer.magnitude.ToString("F3"));
        }
        void TickHomeConversationSpace()
        {
            if (!homeConversationRetreat) return;
            if (!homePanel || !homeWife || homeWifeRouteIndex >= homeWifeRoute.Count || Time.unscaledTime - homeConversationRetreatStarted > 2f) {
                homeConversationRetreat = false;
                if (homeWife) {
                    Vector3 delta = homeWife.position - player.transform.position; delta.y = 0;
                    Debug.Log("HOME_CONVERSATION_SPACE_SETTLED stage=" + state.homeChapter.stage + " metres=" + delta.magnitude.ToString("F3"));
                    if (homePanel) AimHomeWifeConversation();
                }
            }
        }
        void AimHomeWifeConversation()
        {
            if (!homeWife) return;
            Vector3 target = homeWife.position + Vector3.up * 1.39f;
            // The customer stands beside the front of the stall. Include the
            // counter in the same shot instead of framing the courtyard behind.
            if (state.homeChapter.stage == "market") target = Vector3.Lerp(target, new Vector3(-26.4f, 1.3f, -20.3f), .22f);
            Vector3 gaze = target - CameraEyePoint();
            cameraYaw = Mathf.Atan2(gaze.x, gaze.z) * Mathf.Rad2Deg;
            cameraPitch = -Mathf.Atan2(gaze.y, new Vector2(gaze.x, gaze.z).magnitude) * Mathf.Rad2Deg;
            UpdateCamera(true);
        }
        void BuildHomeServingTray()
        {
            if (homeHeldMeal) Destroy(homeHeldMeal);
            homeHeldMeal = new GameObject("端上饭桌的两碗饭"); homeHeldMeal.transform.SetParent(heldRig.transform, false);
            HomeBox("木托盘", homeHeldMeal.transform, new Vector3(0, .025f, .02f), new Vector3(.43f, .027f, .29f), wood);
            HomeBowl(homeHeldMeal.transform, new Vector3(-.10f, .043f, .02f), .095f);
            HomeBowl(homeHeldMeal.transform, new Vector3(.10f, .043f, .02f), .095f);
            SetWorkHeldLayer(homeHeldMeal.transform); homeHeldMeal.SetActive(false);
        }
        const float HomeChildMealLift = .16f;
        void TickHomeFamilyMeal()
        {
            if (!homeArt || !HomeChapter.IsEnrolled(state)) return;
            bool shared = state.homeChapter.mealShared;
            SeatHomeFamily(homeMother, HomeMotherDinner, homeMotherRouteIndex >= homeMotherRoute.Count, shared, .43f);
            SeatHomeFamily(homeWife, new Vector3(-21.75f, 0, (ChenCourtyardHouse.Ready ? -.25f : -4.6f)), homeWifeRouteIndex >= homeWifeRoute.Count, shared, .43f);
            SeatHomeFamily(homeSister, HomeSisterDinner, homeSisterRouteIndex >= homeSisterRoute.Count, shared, .36f);
        }
        void SeatHomeFamily(Transform person, Vector3 seat, bool arrived, bool shared, float height)
        {
            if (!person) return;
            var pose = CharacterPerformance.For(person.gameObject);
            bool atSeat = shared && arrived && Vector2.Distance(new Vector2(person.position.x, person.position.z), new Vector2(seat.x, seat.z)) <= .22f;
            // Aman sits on a raised child stool, with a matching foot platform.
            // Moving her complete actor preserves her own leg proportions and
            // the reviewed native ground/seat contacts relative to that platform.
            if (person == homeSister) {
                Vector3 at = person.position;
                at.y = Mathf.MoveTowards(at.y, atSeat ? HomeChildMealLift : 0, Time.unscaledDeltaTime * .28f);
                person.position = at;
            }
            if (!atSeat) { pose.SetSeated(false); return; }
            // Fix the hips toward the table. A seated listener turns their head,
            // not the entire stool/body every time the player moves past them.
            Vector3 toward = new Vector3(-23, 0, (ChenCourtyardHouse.Ready ? -.3f : -4.65f)) - person.position; toward.y = 0;
            if (toward.sqrMagnitude > .01f) person.rotation = Quaternion.RotateTowards(person.rotation, Quaternion.LookRotation(toward), Time.unscaledDeltaTime * 180);
            pose.SetSeated(true, height);
            if (homePanel || dialogue) pose.LookAt(CameraEyePoint());
            else pose.LookAt(new Vector3(-23, 1.10f, (ChenCourtyardHouse.Ready ? -.3f : -4.65f)));
        }
    }
}
