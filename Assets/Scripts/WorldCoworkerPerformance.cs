using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        WorkCoworkerPerformance workCoworkerPerformance;

        // Scene presentation only. The two player parcels and the existing
        // employer settlement remain the complete source of gameplay progress.
        void SetupCoworkerPerformance()
        {
            workCoworkerPerformance = null;
            string choice = WorkEvents.ChoiceId(state);
            if (!workEventPartner || !tradeArtRoot || (choice != "pair" && choice != "teach")) return;
            Transform parcel = tradeArtRoot.Find("工友接手的布匹");
            if (!parcel) return;
            workCoworkerPerformance = workEventPartner.AddComponent<WorkCoworkerPerformance>();
            bool resume = state.tradeShift.held >= 0 || state.tradeShift.deliveredMask != 0 || state.tradeShift.stage == "done";
            workCoworkerPerformance.Initialize(parcel, player.transform, choice == "teach", resume,
                () => Blocked, CoworkerObstacles);
        }

        IEnumerable<Vector3> CoworkerObstacles()
        {
            if (npc) yield return npc.transform.position;
            yield return sidePosition;
            if (nanyangNeighbour) yield return nanyangNeighbour.transform.position;
            if (ambientLife) foreach (var person in ambientLife.People)
                if (person.Actor && person.Actor.activeInHierarchy) yield return person.Position;
        }
    }

    /// <summary>A physical, unscored counterpart to the player's dock work.
    /// Restoring a progressed shift reconstructs one settled parcel; this actor
    /// never reads or writes wages, story turns, or the player's delivery mask.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WorkCoworkerPerformance : MonoBehaviour
    {
        public string Phase { get { return yielding ? "yielding" : phase; } }
        public bool Completed { get { return phase == "waiting"; } }
        public bool Carrying { get; private set; }
        public bool Paused { get; private set; }
        public float DistanceWalked { get; private set; }
        public int PathFailures { get; private set; }
        public Transform Parcel { get; private set; }
        public Vector3 PickupPosition { get; private set; }
        public Vector3 DeliveryPosition { get; private set; }
        public Vector3 DeliveryStandingPosition { get { return deliveryStand; } }
        public bool Resumed { get; private set; }

        const int GroundMask = ~((1 << 2) | (1 << 29) | (1 << 30));
        const float WalkSpeed = 1.08f, CarrySpeed = .87f, ClothScale = .70f;
        readonly List<Vector3> route = new List<Vector3>();
        Transform player;
        CharacterPerformance pose;
        Func<bool> paused;
        Func<IEnumerable<Vector3>> obstacles;
        Vector3 pickupStand, deliveryStand, goal, motionStart;
        Quaternion parcelRotation;
        string phase = "uninitialized";
        int routeIndex;
        float elapsed, retryAt, walkClock;
        bool teaching, initialized, yielding, externalPause;

        public void Initialize(Transform cloth, Transform playerTransform, bool teach, bool resume,
            Func<bool> isPaused = null, Func<IEnumerable<Vector3>> otherPeople = null)
        {
            if (initialized || !cloth) return;
            initialized = true; Parcel = cloth; player = playerTransform;
            // This co-worker handles a smaller cloth bundle. Its .60 m width
            // fits the two palms instead of hiding both hands inside the crate.
            Parcel.localScale *= ClothScale;
            teaching = teach; paused = isPaused; obstacles = otherPeople;
            pose = CharacterPerformance.For(gameObject);
            PickupPosition = cloth.position; parcelRotation = cloth.rotation;
            pickupStand = WorldFactory.CharacterGroundPosition(new Vector3(-15.45f, 0, 11.75f));
            // Approach the near side of the cart, away from the player's west
            // standing point and the signpost. The bundle rests on its planks.
            deliveryStand = WorldFactory.CharacterGroundPosition(new Vector3(-7.55f, 0, 12.55f));
            DeliveryPosition = WorldFactory.CharacterGroundPosition(new Vector3(-7.55f, 0, 13.62f)) + Vector3.up * .70f;
            if (resume) {
                // A load is not another performance and not another delivery.
                // Reconstruct the completed visual state before the first frame.
                Resumed = true; Parcel.position = DeliveryPosition; Parcel.rotation = Quaternion.identity;
                if (GroundClear(deliveryStand)) transform.position = deliveryStand;
                Face(DeliveryPosition - transform.position, 1);
                SetPhase("waiting"); pose.SetCarrying(false); return;
            }
            SetPhase(teaching ? "briefing" : "acknowledging");
            pose.LookAt(PickupPosition + Vector3.up * .4f);
            pose.Play(teaching ? CharacterGesture.Point : CharacterGesture.Nod, teaching ? 1.15f : .7f);
        }

        public void SetPaused(bool value) { externalPause = value; }

        void Update()
        {
            if (!initialized || !Parcel || !pose) return;
            Paused = externalPause || (paused != null && paused());
            pose.Automatic = !Paused;
            if (Paused) { pose.SetLocomotion(0); UpdateCarriedParcel(); return; }
            float dt = Mathf.Min(Time.deltaTime, .05f);
            elapsed += dt; walkClock += dt; yielding = false;
            switch (phase) {
                case "briefing":
                    Face(PickupPosition - transform.position, dt);
                    if (elapsed >= 1.25f) { SetPhase("acknowledging"); pose.Play(CharacterGesture.Nod, .65f); }
                    break;
                case "acknowledging":
                    Face(PickupPosition - transform.position, dt);
                    if (elapsed >= .75f) BeginWalk(pickupStand, "walking-to-parcel");
                    break;
                case "walking-to-parcel":
                    if (Walk(dt)) {
                        Face(PickupPosition - transform.position, 1); SetPhase("lifting");
                        pose.LookAt(PickupPosition + Vector3.up * .4f); pose.Play(CharacterGesture.Lift, 1.25f);
                        motionStart = Parcel.position;
                    }
                    break;
                case "lifting":
                    Face(PickupPosition - transform.position, dt);
                    // Fingers arrive before the parcel leaves the skid. The
                    // same physical object rises into the two-arm carry pose.
                    if (elapsed >= .43f) {
                        // Start the supporting arms during the lift, not on the
                        // first carrying frame (which may immediately pause).
                        pose.SetCarrying(true);
                        float t = Mathf.SmoothStep(0, 1, (elapsed - .43f) / .70f);
                        Parcel.position = Vector3.Lerp(motionStart, CarryPosition(), t);
                        Parcel.rotation = Quaternion.Slerp(parcelRotation, transform.rotation, t);
                    }
                    if (elapsed >= 1.3f) { Carrying = true; pose.SetCarrying(true); BeginWalk(deliveryStand, "carrying"); }
                    break;
                case "carrying":
                    if (Walk(dt)) {
                        Face(DeliveryPosition - transform.position, 1); UpdateCarriedParcel();
                        motionStart = Parcel.position; parcelRotation = Parcel.rotation;
                        Carrying = false; pose.SetCarrying(false); SetPhase("setting-down");
                        pose.LookAt(DeliveryPosition + Vector3.up * .2f); pose.Play(CharacterGesture.SetDown, 1.2f);
                    }
                    break;
                case "setting-down":
                    float drop = Mathf.SmoothStep(0, 1, elapsed / .77f);
                    Parcel.position = Vector3.Lerp(motionStart, DeliveryPosition, drop);
                    Parcel.rotation = Quaternion.Slerp(parcelRotation, Quaternion.identity, drop);
                    if (elapsed >= 1.25f) {
                        Parcel.position = DeliveryPosition; Parcel.rotation = Quaternion.identity;
                        SetPhase("waiting"); pose.ClearLook(); pose.Play(CharacterGesture.Nod, .75f);
                    }
                    break;
                case "waiting":
                    // He has finished his share and keeps the player's route
                    // clear. Close conversation gets a small natural head turn.
                    if (player && FlatDistance(player.position, transform.position) < 3.8f)
                        pose.LookAt(player.position + Vector3.up * 1.5f);
                    else pose.ClearLook();
                    break;
            }
            UpdateCarriedParcel();
        }

        void LateUpdate()
        {
            if (!initialized || !Parcel || !pose) return;
            // CharacterPerformance updates the arms in LateUpdate. Following
            // them afterwards prevents the bundle trailing a frame behind hands.
            if (phase == "lifting" && elapsed >= .43f) {
                float t = Mathf.SmoothStep(0, 1, (elapsed - .43f) / .70f);
                Parcel.position = Vector3.Lerp(motionStart, CarryPosition(), t);
                Parcel.rotation = Quaternion.Slerp(parcelRotation, transform.rotation, t);
            }
            else UpdateCarriedParcel();
        }

        void SetPhase(string value)
        { phase = value; elapsed = 0; yielding = false; pose.SetLocomotion(0); }

        void BeginWalk(Vector3 point, string movingPhase)
        {
            goal = point; SetPhase(movingPhase); pose.ClearLook(); RebuildPath();
        }

        void RebuildPath()
        {
            route.Clear(); route.AddRange(WalkPath.Find(transform.position, goal, "port")); routeIndex = 0;
            retryAt = walkClock + 1.8f;
            if (route.Count == 0 && FlatDistance(transform.position, goal) > .13f) PathFailures++;
        }

        bool Walk(float dt)
        {
            if (FlatDistance(transform.position, goal) <= .13f) { pose.SetLocomotion(0); return true; }
            while (routeIndex < route.Count && FlatDistance(transform.position, route[routeIndex]) < .13f) routeIndex++;
            if (routeIndex >= route.Count) {
                pose.SetLocomotion(0); yielding = true;
                if (walkClock >= retryAt) RebuildPath();
                return false;
            }
            // A short collision-checked look-ahead removes grid-centre foot
            // shuffling while preserving the authored pickup and drop points.
            for (int i = Mathf.Min(routeIndex + 2, route.Count - 1); i > routeIndex; i--)
                if (FlatDistance(transform.position, route[i]) < 2.2f && ClearSegment(transform.position, route[i])) { routeIndex = i; break; }
            Vector3 forward = route[routeIndex] - transform.position; forward.y = 0;
            float speed = Carrying ? CarrySpeed : WalkSpeed;
            float step = Mathf.Min(speed * dt, forward.magnitude);
            if (step <= .0001f) return false;
            forward.Normalize(); Vector3 steer = forward;
            if (player) Avoid(player.position, 1.5f, forward, ref steer);
            if (obstacles != null) foreach (Vector3 person in obstacles()) Avoid(person, 1.1f, forward, ref steer);
            Vector3 next = transform.position + steer.normalized * step;
            bool clear = SafeStep(next);
            if (!clear) { next = transform.position + forward * step; clear = SafeStep(next); }
            if (!clear) {
                Vector3 left = -Vector3.Cross(Vector3.up, forward);
                next = transform.position + (forward + left * 1.7f).normalized * step; clear = SafeStep(next);
            }
            if (!clear) {
                pose.SetLocomotion(0); yielding = true;
                if (walkClock >= retryAt) RebuildPath();
                return false;
            }
            Vector3 moved = next - transform.position;
            next.y = 0; transform.position = WorldFactory.CharacterGroundPosition(next);
            DistanceWalked += moved.magnitude; Face(moved, dt);
            pose.SetLocomotion(speed / WalkSpeed); return false;
        }

        bool SafeStep(Vector3 next)
        {
            if (!GroundClear(next) || !ClearSegment(transform.position, next)) return false;
            // The carried box has width of its own: a clear body capsule must
            // not permit the cloth bundle to cut into a post or warehouse wall.
            if (Carrying && Physics.CheckBox(next + (CarryPosition() - transform.position) + Vector3.up * (.32f * ClothScale),
                new Vector3(.43f, .31f, .36f) * ClothScale, transform.rotation, GroundMask, QueryTriggerInteraction.Ignore)) return false;
            if (player && Approaching(next, player.position, .86f)) return false;
            if (obstacles != null) foreach (Vector3 person in obstacles()) if (Approaching(next, person, .72f)) return false;
            return true;
        }

        bool Approaching(Vector3 next, Vector3 other, float clearance)
        { return FlatDistance(next, other) < clearance && FlatDistance(next, other) < FlatDistance(transform.position, other) - .0001f; }

        void Avoid(Vector3 other, float range, Vector3 forward, ref Vector3 steer)
        {
            Vector3 delta = other - transform.position; delta.y = 0; float distance = delta.magnitude;
            if (distance < .01f || distance >= range || Vector3.Dot(delta / distance, forward) < -.1f) return;
            float weight = Mathf.Clamp01((range - distance) / (range - .6f));
            steer += Vector3.Cross(Vector3.up, forward) * weight * 1.4f - delta.normalized * weight * .3f;
        }

        static bool GroundClear(Vector3 point)
        {
            if (!WorldRegions.Get("port").IsGround(point)) return false;
            if (!Physics.Raycast(new Vector3(point.x, 4, point.z), Vector3.down, out var hit, 8, GroundMask, QueryTriggerInteraction.Ignore)
                || hit.point.y > .5f || hit.normal.y < .65f) return false;
            return !Physics.CheckCapsule(new Vector3(point.x, .55f, point.z), new Vector3(point.x, 1.55f, point.z), .28f, GroundMask, QueryTriggerInteraction.Ignore);
        }

        static bool ClearSegment(Vector3 from, Vector3 to)
        {
            Vector3 movement = to - from; movement.y = 0; float distance = movement.magnitude;
            return distance < .0001f || !Physics.CapsuleCast(new Vector3(from.x, .55f, from.z), new Vector3(from.x, 1.55f, from.z),
                .28f, movement / distance, distance, GroundMask, QueryTriggerInteraction.Ignore);
        }

        Vector3 CarryPosition()
        {
            if (!pose) return transform.position + Vector3.up * .82f + transform.forward * .48f;
            Vector3 palms = (pose.LeftHandPoint + pose.RightHandPoint) * .5f;
            return palms + Vector3.up * .01f + transform.forward * .10f;
        }
        void UpdateCarriedParcel()
        {
            if (!Carrying || !Parcel) return;
            Parcel.position = CarryPosition(); Parcel.rotation = transform.rotation;
        }
        void Face(Vector3 direction, float dt)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 155 * dt);
        }
        static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        void OnDisable() { if (pose) { pose.Automatic = true; pose.SetLocomotion(0); pose.SetCarrying(false); pose.ClearLook(); } }
    }
}
