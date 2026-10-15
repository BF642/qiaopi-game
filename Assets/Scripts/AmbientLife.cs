using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    // Scene-local daily routines. These actors never write story or player data.
    public sealed class AmbientLife : MonoBehaviour
    {
        public string WorldId;
        public Transform Player;
        public readonly List<AmbientWalker> People = new List<AmbientWalker>();
        public bool Ready, Automatic = true;
        public int RouteFailures;
        public float SimulationSeconds;
        RegionInfo region;
        readonly HashSet<string> storyModels = new HashSet<string>();
        Vector3[] exclusions = new Vector3[0];
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Dictionary<string,Material> palette = new Dictionary<string,Material>();
        const int GroundMask = ~((1 << 2) | (1 << 29) | (1 << 30));

        public static AmbientLife Create(Transform parent, string world, Transform player, bool mobile)
        {
            var root = new GameObject("街巷生活 · " + world);
            root.transform.SetParent(parent, false);
            var life = root.AddComponent<AmbientLife>(); life.WorldId = world; life.Player = player; life.region = WorldRegions.Get(world);
            life.StartCoroutine(life.Prepare(mobile));
            return life;
        }

        public void SetExclusions(Vector3[] points) { exclusions = points ?? new Vector3[0]; }
        public void SetStoryModel(string model)
        {
            SetStoryModels(new[]{model});
        }
        public void SetStoryModels(string[] models)
        {
            storyModels.Clear(); foreach (string model in models) if (!string.IsNullOrEmpty(model)) storyModels.Add(model);
            // Distinct residents may share a wardrobe/rig with a speaking actor.
            // Only a legacy uncustomized clone still needs the old suppression.
            foreach (var person in People) if (person.Actor) {
                var appearance = person.Actor.GetComponent<AmbientAppearance>();
                person.Actor.SetActive(appearance && appearance.Ready || !storyModels.Contains(person.Schedule.model));
            }
        }

        IEnumerator Prepare(bool mobile)
        {
            var schedules = AmbientSchedules.Get(WorldId);
            int count = mobile ? Mathf.Min(6, schedules.Length) : schedules.Length;
            for (int i = 0; i < count; i++) {
                var plan = schedules[i];
                var actor = CharacterAppearanceProfiles.SpawnResident(plan, transform, WorldFactory.CharacterGroundPosition(plan.stops[plan.startIndex].position));
                if (!actor) { RouteFailures++; Debug.LogError("AMBIENT_MODEL_MISSING " + plan.model); continue; }
                actor.name = plan.title + " · " + plan.id;
                foreach (var idle in actor.GetComponentsInChildren<CharacterIdle>()) idle.enabled = false;
                foreach (var collider in actor.GetComponentsInChildren<Collider>()) collider.enabled = false;
                foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    skin.updateWhenOffscreen = false;
                    if (mobile) skin.shadowCastingMode = ShadowCastingMode.Off;
                }
                var person = new AmbientWalker(plan, actor, i);
                People.Add(person);
                actor.SetActive(true);
                var props = new GameObject("随身货物"); props.transform.SetParent(actor.transform, false);
                props.transform.localScale = Vector3.one / actor.transform.localScale.x;
                foreach (var stop in plan.stops) if (!string.IsNullOrEmpty(stop.carryAfterStop) && !person.Props.ContainsKey(stop.carryAfterStop)) {
                    var prop = MakeProp(stop.carryAfterStop, props.transform, ModelLibrary.CharacterHeight(plan.model) / 1.72f);
                    person.Props.Add(stop.carryAfterStop, prop); prop.SetActive(false);
                }
                person.SetCargo(person.InitialCargo(), false);
                yield return null;
            }
            // Imported colliders are removed by ModelLibrary at frame end.
            Physics.SyncTransforms();
            foreach (var person in People) {
                bool valid = true;
                for (int i = 0; i < person.Schedule.stops.Length; i++) {
                    Vector3 from = person.Schedule.stops[i].position;
                    Vector3 to = person.Schedule.stops[(i + 1) % person.Schedule.stops.Length].position;
                    var route = WalkPath.Find(from, to, WorldId);
                    person.Routes[i] = route;
                    if (route.Count == 0 || FlatDistance(route[route.Count - 1], to) > .85f) {
                        valid = false; RouteFailures++;
                        Debug.LogError("AMBIENT_ROUTE_FAILED " + WorldId + "/" + person.Schedule.id + " leg=" + i + " from=" + from + " to=" + to);
                    }
                    yield return null;
                }
                person.RoutesReady = valid;
                if (valid) person.Begin();
            }
            Ready = true;
            Debug.Log("AMBIENT_READY " + WorldId + " people=" + People.Count + " failedLegs=" + RouteFailures);
        }

        void Update() { if (Automatic) Step(Mathf.Min(Time.deltaTime, .12f)); }

        public void Step(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            // Substeps also make QA and low-frame-rate phones use the same
            // collision rules; a long frame must not jump through a wall.
            float remaining = Mathf.Min(dt, .5f);
            while (remaining > .00001f) {
                float step = Mathf.Min(remaining, .05f); remaining -= step; SimulationSeconds += step;
                for (int i = 0; i < People.Count; i++) if (People[i].RoutesReady && People[i].Actor.activeInHierarchy) Tick(People[i], step);
            }
        }

        void Tick(AmbientWalker person, float dt)
        {
            if (person.Phase == "working") {
                var stop = person.Schedule.stops[person.StopIndex];
                Face(person, stop.facePoint - person.Position, dt);
                person.Wait -= dt;
                person.Animate(0, SimulationSeconds, dt);
                if (person.Wait > 0) return;
                if (stop.dwellSeconds > 0) person.CompletedStops++;
                if (stop.carryAfterStop != null) person.SetCargo(stop.carryAfterStop, true);
                person.Phase = "walking"; person.SegmentIndex = 0;
            }
            var route = person.Routes[person.StopIndex];
            while (person.SegmentIndex < route.Count) {
                float gap = FlatDistance(person.Position, route[person.SegmentIndex]);
                bool pass = gap < .085f || (gap < .28f && person.SegmentIndex < route.Count - 1 &&
                    ClearShortcut(person.Position, route[person.SegmentIndex + 1]));
                if (!pass) break;
                person.SegmentIndex++;
            }
            // Grid points are guidance, not places everyone must stand on. A
            // short clear look-ahead lets people pass an occupied grid centre
            // without skipping an authored destination or cutting a wall.
            for (int next = Mathf.Min(person.SegmentIndex + 3, route.Count - 1); next > person.SegmentIndex; next--) {
                if (FlatDistance(person.Position, route[next]) <= 2.5f && ClearShortcut(person.Position, route[next])) {
                    person.SegmentIndex = next; break;
                }
            }
            if (person.SegmentIndex >= route.Count) {
                person.StopIndex = (person.StopIndex + 1) % person.Schedule.stops.Length;
                if (person.StopIndex == person.Schedule.startIndex) person.CompletedCycles++;
                person.Phase = "working"; person.Wait = person.Schedule.stops[person.StopIndex].dwellSeconds;
                person.Animate(0, SimulationSeconds, dt); return;
            }
            Vector3 toward = route[person.SegmentIndex] - person.Position; toward.y = 0;
            Vector3 forward = toward.normalized, steer = forward;
            // Keep right when passing another person. Dynamic avoidance only
            // takes small, collision-checked steps alongside the authored path.
            foreach (var other in People) if (other != person && other.Actor.activeInHierarchy) SteerAway(person.Position, other.Position, 1.25f, forward, ref steer);
            if (Player) SteerAway(person.Position, Player.position, 1.65f, forward, ref steer);
            foreach (Vector3 point in exclusions) SteerAway(person.Position, point, 1.15f, forward, ref steer);
            float speed = person.Schedule.speed * (person.HeldKind == "parcel" || person.HeldKind == "water" ? .85f : 1f);
            float distance = Mathf.Min(speed * dt, toward.magnitude);
            Vector3 candidate = person.Position + steer.normalized * distance;
            bool allowed = SafeStep(person.Position, candidate) && RoomForPerson(person, candidate);
            if (!allowed) {
                candidate = person.Position + forward * distance;
                allowed = SafeStep(person.Position, candidate) && RoomForPerson(person, candidate);
            }
            if (!allowed) {
                // A wall on the right must not make a pedestrian wait forever
                // while the left side of a stationary person is still open.
                Vector3 left = -Vector3.Cross(Vector3.up, forward);
                candidate = person.Position + (forward + left * 2f).normalized * distance;
                allowed = SafeStep(person.Position, candidate) && RoomForPerson(person, candidate);
                if (!allowed) {
                    candidate = person.Position + left * distance;
                    allowed = SafeStep(person.Position, candidate) && RoomForPerson(person, candidate);
                }
            }
            if (!allowed) { person.Phase = "yielding"; person.Animate(0, SimulationSeconds, dt); return; }
            Vector3 movement = candidate - person.Position;
            candidate.y = 0; candidate = WorldFactory.CharacterGroundPosition(candidate);
            person.Actor.transform.position = candidate; person.Phase = "walking";
            person.DistanceWalked += movement.magnitude;
            Face(person, movement, dt); person.Animate(movement.magnitude / dt, SimulationSeconds, dt);
        }

        static void SteerAway(Vector3 from, Vector3 obstacle, float range, Vector3 forward, ref Vector3 steer)
        {
            Vector3 delta = obstacle - from; delta.y = 0; float distance = delta.magnitude;
            if (distance < .01f || distance >= range || Vector3.Dot(delta / distance, forward) < -.1f) return;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float weight = Mathf.Clamp01((range - distance) / (range - .55f));
            steer += right * weight * 1.6f - delta.normalized * weight * .35f;
        }

        bool RoomForPerson(AmbientWalker person, Vector3 next)
        {
            if (Player && Approaching(person.Position, next, Player.position, .80f)) return false;
            foreach (var other in People) if (person != other && other.Actor.activeInHierarchy && Approaching(person.Position, next, other.Position, .57f)) return false;
            foreach (var point in exclusions) if (Approaching(person.Position, next, point, .65f)) return false;
            return true;
        }

        static bool Approaching(Vector3 current, Vector3 next, Vector3 other, float clearance)
        { return FlatDistance(next, other) < clearance && FlatDistance(next, other) < FlatDistance(current, other) - .0001f; }

        bool ClearShortcut(Vector3 from, Vector3 to)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(FlatDistance(from, to) / .35f));
            Vector3 previous = from;
            for (int i = 1; i <= steps; i++) {
                Vector3 point = Vector3.Lerp(from, to, (float)i / steps);
                if (!SafeStep(previous, point)) return false;
                previous = point;
            }
            return true;
        }

        bool SafeStep(Vector3 from, Vector3 to)
        {
            if (!region.IsGround(to)) return false;
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(to.x, 4, to.z), Vector3.down, out hit, 8, GroundMask, QueryTriggerInteraction.Ignore)
                || hit.point.y > .5f || hit.normal.y < .65f) return false;
            Vector3 movement = to - from; movement.y = 0; float distance = movement.magnitude;
            if (Physics.CheckCapsule(new Vector3(to.x,.55f,to.z), new Vector3(to.x,1.55f,to.z), .28f, GroundMask, QueryTriggerInteraction.Ignore)) return false;
            return distance < .0001f || !Physics.CapsuleCast(new Vector3(from.x,.55f,from.z), new Vector3(from.x,1.55f,from.z), .28f,
                movement / distance, distance, GroundMask, QueryTriggerInteraction.Ignore);
        }

        static void Face(AmbientWalker person, Vector3 direction, float dt)
        {
            direction.y = 0; if (direction.sqrMagnitude < .001f) return;
            person.Actor.transform.rotation = Quaternion.RotateTowards(person.Actor.transform.rotation, Quaternion.LookRotation(direction), dt * 155);
        }

        public AmbientWalker NearestPurpose(Vector3 position)
        {
            AmbientWalker result = null; float nearest = 3.4f;
            foreach (var person in People) {
                if(!person.Actor.activeInHierarchy)continue;
                float distance = FlatDistance(position, person.Position);
                if (distance < nearest) { result = person; nearest = distance; }
            }
            return result;
        }

        public static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a,b); }

        Material PropMaterial(string hex)
        {
            Material result; if (palette.TryGetValue(hex, out result)) return result;
            result = new Material(Resources.Load<Material>("WorldMaterial")) { name = "生活道具 " + hex };
            Color colour; ColorUtility.TryParseHtmlString("#" + hex, out colour); result.color = colour; result.SetFloat("_Glossiness", .08f);
            palette.Add(hex, result); ownedMaterials.Add(result); return result;
        }

        GameObject Part(Transform parent, string name, PrimitiveType shape, Vector3 at, Vector3 size, string colour)
        {
            var part = GameObject.CreatePrimitive(shape); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = at; part.transform.localScale = size;
            var collider = part.GetComponent<Collider>(); if (collider) { collider.enabled = false; Destroy(collider); }
            part.GetComponent<Renderer>().sharedMaterial = PropMaterial(colour); return part;
        }

        void Handle(Transform parent, float radius, float bottom, string colour)
        {
            var go = new GameObject("提梁"); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = 17;
            line.startWidth = line.endWidth = .015f; line.numCapVertices = 3; line.sharedMaterial = PropMaterial(colour);
            for (int i = 0; i < 17; i++) { float angle = i * Mathf.PI / 16; line.SetPosition(i, new Vector3(Mathf.Cos(angle)*radius, bottom+Mathf.Sin(angle)*radius, 0)); }
        }

        GameObject MakeProp(string kind, Transform parent, float stature)
        {
            var root = new GameObject(kind); root.transform.SetParent(parent, false);
            if (UsesOneHand(kind)) {
                root.transform.localPosition = new Vector3(.31f, .57f, .08f) * stature;
                Part(root.transform, "竹木提桶", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.27f,.11f,.27f), kind == "water" ? "72543B" : "9A7952");
                Part(root.transform, "桶口", PrimitiveType.Cylinder, new Vector3(0,.115f,0), new Vector3(.25f,.012f,.25f), kind == "water" ? "546D69" : "63583C");
                Handle(root.transform,.15f,.10f,"554631");
                for (int i=0;i<8;i++) { float angle=i*Mathf.PI*.25f;
                    Part(root.transform,"编条",PrimitiveType.Cube,new Vector3(Mathf.Cos(angle)*.133f,0,Mathf.Sin(angle)*.133f),new Vector3(.012f,.215f,.014f),"B29A70"); }
                if (kind == "basket") for (int i=0;i<3;i++) Part(root.transform,"青菜",PrimitiveType.Sphere,new Vector3((i-1)*.067f,.135f,.01f),new Vector3(.09f,.11f,.13f),i==1?"637653":"859268");
                if (kind == "grain") {
                    Part(root.transform,"米筐布衬",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.232f,.013f,.232f),"C3AB7D");
                    Part(root.transform,"米粮",PrimitiveType.Sphere,new Vector3(0,.135f,0),new Vector3(.22f,.045f,.22f),"D4BE8D");
                    for (int i=0;i<7;i++) {
                        float angle=i*2.39996f, radius=.028f+(.065f*(i%3)/2f);
                        var grain=Part(root.transform,"米粒",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*radius,.157f,Mathf.Sin(angle)*radius),new Vector3(.019f,.007f,.008f),"E1CFA4");
                        grain.transform.localRotation=Quaternion.Euler(0,i*31,0);
                    }
                }
                if (kind == "firewood") for (int i=0;i<5;i++) {
                    var stick=Part(root.transform,"短柴",PrimitiveType.Cylinder,new Vector3((i%3-1)*.061f,.139f+(i/3)*.043f,0),new Vector3(.037f,.15f,.037f),i%2==0?"81603F":"674C32");
                    stick.transform.localRotation=Quaternion.Euler(90,0,(i-2)*4);
                }
            } else {
                root.transform.localPosition = new Vector3(0,1.05f,.45f) * stature;
                if (kind == "parcel") {
                    Part(root.transform,"货包",PrimitiveType.Cube,Vector3.zero,new Vector3(.45f,.28f,.32f),"947A54");
                    foreach(float x in new[]{-.14f,.14f}) Part(root.transform,"麻绳",PrimitiveType.Cube,new Vector3(x,0,0),new Vector3(.018f,.287f,.327f),"574C35");
                    Part(root.transform,"缠绳",PrimitiveType.Cube,Vector3.zero,new Vector3(.457f,.018f,.327f),"574C35");
                } else if (kind == "ledger") {
                    Part(root.transform,"账簿",PrimitiveType.Cube,Vector3.zero,new Vector3(.25f,.025f,.32f),"594F40");
                    Part(root.transform,"纸页",PrimitiveType.Cube,new Vector3(.005f,.017f,0),new Vector3(.23f,.008f,.29f),"CFC5A7");
                } else {
                    for(int i=0;i<4;i++) Part(root.transform,"批封",PrimitiveType.Cube,new Vector3(0,i*.012f,0),new Vector3(.31f,.009f,.21f),i%2==0?"D6C7A5":"C2B28F");
                    Part(root.transform,"红绳",PrimitiveType.Cube,new Vector3(0,.021f,0),new Vector3(.02f,.058f,.22f),"954E3E");
                }
            }
            return root;
        }

        internal static bool UsesOneHand(string kind) { return kind == "basket" || kind == "water" || kind == "grain" || kind == "firewood"; }

        void OnDestroy() { foreach (var material in ownedMaterials) if (material) Destroy(material); }
    }

    public sealed class AmbientWalker
    {
        public AmbientSchedule Schedule;
        public GameObject Actor, HeldObject;
        public List<Vector3>[] Routes;
        public int StopIndex, SegmentIndex, CompletedStops, CargoChanges, CompletedCycles;
        public string Phase = "preparing", HeldKind = "";
        public float DistanceWalked;
        public bool RoutesReady;
        public Transform[] Joints;
        public readonly Dictionary<string,GameObject> Props = new Dictionary<string,GameObject>();
        public Vector3 Position => Actor.transform.position;
        internal float Wait;
        readonly Quaternion[] rest = new Quaternion[4];
        readonly int order;
        float gait, motion;

        public AmbientWalker(AmbientSchedule schedule, GameObject actor, int index)
        {
            Schedule = schedule; Actor = actor; order = index; StopIndex = schedule.startIndex;
            Routes = new List<Vector3>[schedule.stops.Length]; Joints = new Transform[4];
            string[] names = {"LeftArm","RightArm","LeftLeg","RightLeg"};
            for(int i=0;i<4;i++) { Joints[i] = ModelLibrary.Find(actor.transform,names[i]); if(Joints[i])rest[i]=Joints[i].localRotation; }
        }

        public string InitialCargo()
        {
            for(int back=1;back<=Schedule.stops.Length;back++) {
                string cargo = Schedule.stops[(StopIndex-back+Schedule.stops.Length)%Schedule.stops.Length].carryAfterStop;
                if(cargo!=null)return cargo;
            }
            return "";
        }

        internal void Begin() { Phase="working"; Wait=Schedule.stops[StopIndex].dwellSeconds * (.2f+(order*.29f)% .8f); }

        internal void SetCargo(string kind, bool count)
        {
            kind=kind??""; if(count&&kind!=HeldKind)CargoChanges++;
            HeldKind=kind; HeldObject=null;
            foreach(var prop in Props) { bool active=prop.Key==kind;prop.Value.SetActive(active);if(active)HeldObject=prop.Value; }
        }

        public string Purpose
        {
            get {
                if(Phase=="working"&&!string.IsNullOrEmpty(Schedule.stops[StopIndex].activity))return Schedule.stops[StopIndex].activity;
                for(int ahead=1;ahead<=Schedule.stops.Length;ahead++) {
                    string activity=Schedule.stops[(StopIndex+ahead)%Schedule.stops.Length].activity;
                    if(!string.IsNullOrEmpty(activity))return activity.StartsWith("在")||activity.StartsWith("向") ? "去"+activity.Substring(1) : activity;
                }
                return "行路";
            }
        }

        internal void Animate(float speed, float time, float dt)
        {
            motion=Mathf.MoveTowards(motion,Mathf.Clamp01(speed/1.15f),dt*5f);
            gait+=speed*dt*5.2f;
            float swing=Mathf.Sin(gait)*22f*motion;
            bool carrying=!string.IsNullOrEmpty(HeldKind), oneHand=AmbientLife.UsesOneHand(HeldKind);
            float left=-swing*.68f, right=swing*.68f;
            if(carrying) { if(oneHand)right=-6;else left=right=-54; }
            else if(Phase=="working"&&Schedule.stops[StopIndex].dwellSeconds>0) {
                string action=Schedule.stops[StopIndex].activity;
                if(action.Contains("取")||action.Contains("装")||action.Contains("收")||action.Contains("核")||action.Contains("买"))left=right=-24-Mathf.Sin(time*1.6f+order)*8;
                else {left=Mathf.Sin(time*1.3f+order)*2;right=-left;}
            }
            for(int i=0;i<4;i++)if(Joints[i]) {
                float angle=i==0?left:i==1?right:i==2?swing:-swing;
                Joints[i].localRotation=Quaternion.Slerp(Joints[i].localRotation,rest[i]*Quaternion.Euler(angle,0,0),Mathf.Min(1,dt*14));
            }
        }
    }
}
