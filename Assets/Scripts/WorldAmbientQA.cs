using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        const int AmbientGroundMask = ~((1 << 2) | (1 << 29) | (1 << 30));
        bool AmbientRoutesOnly => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-ambient-routes") >= 0;

        static void AmbientRequire(bool condition, string label)
        {
            if (!condition) throw new Exception("AMBIENT_QA_" + label);
        }

        IEnumerator RunAmbientQA()
        {
            var pending = new Stack<IEnumerator>(); pending.Push(RunAmbientChecks());
            while (pending.Count > 0) {
                object wait = null; Exception failure = null;
                try {
                    if (!pending.Peek().MoveNext()) pending.Pop();
                    else wait = pending.Peek().Current;
                } catch (Exception error) { failure = error; }
                if (failure != null) {
                    while (pending.Count > 0) (pending.Pop() as IDisposable)?.Dispose();
                    Debug.LogError("AMBIENT_QA_FAILED: " + failure); Application.Quit(1); yield break;
                }
                var nested = wait as IEnumerator;
                if (nested != null) pending.Push(nested);
                else if (pending.Count > 0) yield return wait;
            }
            Debug.Log(AmbientRoutesOnly
                ? "AMBIENT_ROUTES_QA_ALL_PASSED: all seven worlds prepared and every scheduled route built; unchanged saves/preferences, muted. Full movement and cargo checks are a separate run."
                : "AMBIENT_QA_ALL_PASSED: seven real worlds, complete closed routes, ground and wall clearance, " +
                "working stops and cargo changes, actual weighted limb deformation, duplicate/lifetime checks, " +
                "ten-second live walking captures, unchanged story/saves/preferences, muted.");
            Application.Quit(0);
        }

        IEnumerator RunAmbientChecks()
        {
            AmbientRequire(AmbientQA && QA && DetailQA, "ISOLATED_ENTRY");
            string[] paths = {savePath, savePath + ".bak", savePath + ".tmp"};
            var originalFiles = new string[paths.Length];
            for (int i = 0; i < paths.Length; i++) originalFiles[i] = CharacterFileSnapshot(paths[i]);
            string[] keys = {"qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized"};
            var originalPrefs = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++) originalPrefs[i] = PlayerPrefs.GetInt(keys[i], -1);
            float originalLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false); voiceSource.Stop(); sfx.Stop();
            string output = Path.Combine(Application.persistentDataPath, "AmbientReview-" + Application.version + (MobileControls ? "-mobile" : "-desktop"));
            Directory.CreateDirectory(output); Debug.Log("AMBIENT_QA_OUTPUT=" + output);
            string[] nodes = {"home", "passage", "passage", "shore", "shop", "first_pay", "courier"};
            if (AmbientRoutesOnly) {
                var failures = new int[1];
                yield return RunAmbientRoutePreflight(nodes, output, failures);
                CheckAmbientIsolation(paths, originalFiles, keys, originalPrefs, originalLevel);
                AmbientRequire(failures[0] == 0, "ROUTES_PREFLIGHT_FAILURES_" + failures[0]);
                yield break;
            }
            var checkedModels = new HashSet<string>();
            AmbientLife previous = null;
            int allPeople = 0, allRoutes = 0;
            for (int regionIndex = 0; regionIndex < WorldRegions.All.Length; regionIndex++) {
                string place = WorldRegions.All[regionIndex];
                EnterAmbientQANode(nodes[regionIndex], place == "ship");
                yield return WaitForAmbientReady(place);
                if (previous) AmbientRequire(!previous.gameObject.activeInHierarchy, "OLD_WORLD_INACTIVE_" + place);
                AmbientLife life = ambientLife; previous = life;
                life.Automatic = false; life.Player = null; life.SetExclusions(new Vector3[0]);
                CheckAmbientOwnership(life, place);
                AmbientSchedule[] schedules = AmbientSchedules.Get(place);
                int expectedPeople = MobileControls ? Mathf.Min(6, schedules.Length) : schedules.Length;
                AmbientRequire(life.People.Count == expectedPeople && expectedPeople > 0, "SCHEDULE_COUNT_" + place);
                var ids = new HashSet<string>();
                var probes = new List<AmbientPoseProbe>();
                var stopTargets = new int[life.People.Count];
                var cycleTargets = new int[life.People.Count];
                var cargoTargets = new int[life.People.Count];
                var distanceStart = new float[life.People.Count];
                var previousPositions = new Vector3[life.People.Count];
                var previousPhases = new string[life.People.Count];
                var previousStops = new int[life.People.Count];
                var workingSeen = new bool[life.People.Count];
                var heldSeen = new bool[life.People.Count];
                var emptySeen = new bool[life.People.Count];
                float longestCycle = 0;
                var timer = System.Diagnostics.Stopwatch.StartNew();
                try {
                    for (int i = 0; i < life.People.Count; i++) {
                        AmbientWalker person = life.People[i]; AmbientSchedule schedule = person.Schedule;
                        AmbientRequire(schedule != null && ids.Add(schedule.id) && person.Actor && person.RoutesReady, "READY_UNIQUE_ACTOR_" + place + "_" + i);
                        AmbientRequire(IsCharacterModel(person.Actor.transform, schedule.model), "REAL_MODEL_" + schedule.id);
                        CheckAmbientAppearance(person);
                        AmbientRequire(person.Joints != null && person.Joints.Length == 4, "FOUR_JOINTS_" + schedule.id);
                        foreach (var idle in person.Actor.GetComponentsInChildren<CharacterIdle>())
                            AmbientRequire(!idle.enabled, "NO_COMPETING_IDLE_" + schedule.id);
                        foreach (Collider collider in person.Actor.GetComponentsInChildren<Collider>(true))
                            AmbientRequire(!collider.enabled || collider.isTrigger, "NPC_CANNOT_BLOCK_PLAYER_" + schedule.id);
                        if (checkedModels.Add(schedule.model)) {
                            CheckAmbientRig(person);
                            for (int j = 0; j < 4; j++) CheckSkinnedCharacterJoint(person.Actor, person.Joints[j], schedule.model);
                        }
                        longestCycle = Mathf.Max(longestCycle, CheckAmbientRoutes(person, place)); allRoutes += person.Routes.Length;
                        int workStops = 0;
                        foreach (AmbientStop stop in schedule.stops) if (stop.dwellSeconds > 0) workStops++;
                        AmbientRequire(workStops > 0, "PURPOSEFUL_STOPS_" + schedule.id);
                        stopTargets[i] = person.CompletedStops + workStops + 1;
                        cycleTargets[i] = person.CompletedCycles + 1;
                        cargoTargets[i] = person.CargoChanges + AmbientCargoTransitions(schedule);
                        distanceStart[i] = person.DistanceWalked;
                        probes.Add(new AmbientPoseProbe(person));
                    }
                    yield return null;
                    string storyBefore = JsonUtility.ToJson(state);
                    int progressBefore = progress; bool carryingBefore = carrying, sideBefore = sideCarrying;
                    string inspectedBefore = string.Join(",", inspected);
                    bool complete = false; const float dt = .05f;
                    int maxSteps = Mathf.CeilToInt((longestCycle * 2f + 45f) / dt);
                    for (int step = 0; step < maxSteps; step++) {
                        for (int i = 0; i < life.People.Count; i++) {
                            previousPositions[i] = life.People[i].Position;
                            previousPhases[i] = life.People[i].Phase; previousStops[i] = life.People[i].StopIndex;
                        }
                        life.Step(dt);
                        AmbientRequire(life.RouteFailures == 0, "ROUTE_FAILURE_" + place);
                        complete = true;
                        for (int i = 0; i < life.People.Count; i++) {
                            AmbientWalker person = life.People[i];
                            float moved = AmbientFlatDistance(previousPositions[i], person.Position);
                            AmbientRequire(moved <= person.Schedule.speed * dt + .002f, "NO_TELEPORT_" + person.Schedule.id);
                            if (previousPhases[i] == "working" && person.Phase == "working" && previousStops[i] == person.StopIndex)
                                AmbientRequire(moved < .0001f, "STATIONARY_WHILE_WORKING_" + person.Schedule.id);
                            if (step % 13 == 0) {
                                CheckAmbientLiveState(person, place);
                                if (!probes[i].Complete) probes[i].Sample();
                                workingSeen[i] |= person.Phase == "working";
                                heldSeen[i] |= !string.IsNullOrEmpty(person.HeldKind);
                                emptySeen[i] |= string.IsNullOrEmpty(person.HeldKind);
                            }
                            complete &= person.CompletedCycles >= cycleTargets[i] && person.CompletedStops >= stopTargets[i] && person.CargoChanges >= cargoTargets[i];
                        }
                        if (complete) break;
                        if (step % 32 == 0) yield return null;
                    }
                    if (!complete) LogAmbientCycleTimeout(life, stopTargets, cycleTargets, cargoTargets);
                    AmbientRequire(complete, "FULL_CYCLE_TIMEOUT_" + place + "_sim=" + life.SimulationSeconds);
                    for (int i = 0; i < life.People.Count; i++) {
                        AmbientWalker person = life.People[i]; probes[i].Sample();
                        AmbientRequire(workingSeen[i], "ACTUALLY_WORKED_" + person.Schedule.id);
                        AmbientRequire(person.DistanceWalked - distanceStart[i] > 2, "ACTUALLY_WALKED_" + person.Schedule.id);
                        AmbientRequire(probes[i].Complete, "ANIMATED_WEIGHTED_LIMBS_" + person.Schedule.id + "_" + probes[i].Summary);
                        if (AmbientHasCarry(person.Schedule)) AmbientRequire(heldSeen[i], "HELD_PROP_SEEN_" + person.Schedule.id);
                        if (AmbientHasUnload(person.Schedule)) AmbientRequire(emptySeen[i], "UNLOADED_PROP_SEEN_" + person.Schedule.id);
                        Debug.Log("AMBIENT_QA_PERSON_PASSED " + place + "/" + person.Schedule.id + " stops=" + person.CompletedStops +
                            " cycles=" + person.CompletedCycles + " cargoChanges=" + person.CargoChanges + " metres=" + person.DistanceWalked.ToString("F1") + " limbMetres=" + probes[i].Summary);
                    }
                    AmbientRequire(storyBefore == JsonUtility.ToJson(state) && progressBefore == progress && carryingBefore == carrying && sideBefore == sideCarrying &&
                        inspectedBefore == string.Join(",", inspected), "AMBIENT_DOES_NOT_CHANGE_STORY_" + place);
                    allPeople += life.People.Count;
                    timer.Stop();
                    Debug.Log("AMBIENT_QA_REGION_PASSED " + place + " people=" + life.People.Count + " renderers=" + life.GetComponentsInChildren<Renderer>().Length +
                        " simulatedSeconds=" + life.SimulationSeconds.ToString("F1") + " validationWallSeconds=" + timer.Elapsed.TotalSeconds.ToString("F2"));
                    if (place == "market") {
                        yield return CheckAmbientPlayerAvoidance(life);
                        yield return CaptureAmbientWalking(life, output);
                    }
                    // Re-entering another story beat in the same place must not
                    // create another controller or a second copy of its residents.
                    int countBefore = life.People.Count; EnterNode(true); yield return null;
                    AmbientRequire(ambientLife == life && life.People.Count == countBefore, "SAME_WORLD_NO_RESPAWN_" + place);
                    CheckAmbientOwnership(life, place);
                } finally { foreach (AmbientPoseProbe probe in probes) probe.Dispose(); }
            }
            // Rebuild a previously visited world once, catching stale static
            // roots or actor registries as well as same-world duplication.
            EnterAmbientQANode("home", false); yield return WaitForAmbientReady("quanzhou");
            ambientLife.Automatic = false; CheckAmbientOwnership(ambientLife, "quanzhou");
            int homeCount = AmbientSchedules.Get("quanzhou").Length;
            AmbientRequire(ambientLife.People.Count == (MobileControls ? Mathf.Min(6, homeCount) : homeCount), "RETURN_WORLD_NO_DUPLICATES");
            CheckAmbientIsolation(paths, originalFiles, keys, originalPrefs, originalLevel);
            Debug.Log("AMBIENT_QA_COUNTS worlds=7 people=" + allPeople + " routeSegments=" + allRoutes + " models=" + checkedModels.Count);
        }

        void CheckAmbientIsolation(string[] paths, string[] originalFiles, string[] keys, int[] originalPrefs, float originalLevel)
        {
            Save();
            for (int i = 0; i < paths.Length; i++) AmbientRequire(originalFiles[i] == CharacterFileSnapshot(paths[i]), "REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < keys.Length; i++) AmbientRequire(originalPrefs[i] == PlayerPrefs.GetInt(keys[i], -1), "PREFERENCE_UNCHANGED_" + keys[i]);
            AmbientRequire(originalLevel == PlayerPrefs.GetFloat("qiaopi-music-level", -1), "MUSIC_LEVEL_UNCHANGED");
            AmbientRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "STAYS_MUTED");
        }

        IEnumerator RunAmbientRoutePreflight(string[] nodes, string output, int[] failures)
        {
            var report = new System.Text.StringBuilder();
            int people = 0, expectedTotal = 0, routes = 0, failedLegs = 0;
            for (int regionIndex = 0; regionIndex < WorldRegions.All.Length; regionIndex++) {
                string place = WorldRegions.All[regionIndex];
                int expected = AmbientSchedules.Get(place).Length;
                if (MobileControls) expected = Mathf.Min(6, expected);
                expectedTotal += expected;
                EnterAmbientQANode(nodes[regionIndex], place == "ship");
                float deadline = Time.realtimeSinceStartup + 15f;
                while ((!ambientLife || !ambientLife.Ready) && Time.realtimeSinceStartup < deadline) yield return null;
                AmbientLife life = ambientLife;
                if (!life || !life.Ready || life.WorldId != place || loadedWorld != place) {
                    string error = "AMBIENT_ROUTES_QA_NOT_READY world=" + place + " expectedPeople=" + expected;
                    Debug.LogError(error); report.AppendLine(error); failures[0]++;
                    if (life) life.Automatic = false;
                    continue;
                }
                life.Automatic = false; yield return null;
                people += life.People.Count; failedLegs += life.RouteFailures; failures[0] += life.RouteFailures;
                if (life.People.Count != expected) failures[0]++;
                foreach (AmbientWalker person in life.People) {
                    AmbientSchedule schedule = person.Schedule; int bad = 0;
                    CheckAmbientAppearance(person);
                    for (int i = 0; i < schedule.stops.Length; i++) {
                        List<Vector3> path = person.Routes != null && i < person.Routes.Length ? person.Routes[i] : null;
                        routes++;
                        Vector3 from = schedule.stops[i].position, to = schedule.stops[(i + 1) % schedule.stops.Length].position;
                        float gap = path != null && path.Count > 0 ? AmbientFlatDistance(path[path.Count - 1], to) : float.PositiveInfinity;
                        if (path == null || path.Count == 0 || gap > .85f) {
                            bad++;
                            string error = "AMBIENT_ROUTES_QA_BAD_LEG " + place + "/" + schedule.id + " leg=" + i + " from=" + from.ToString("F3") +
                                " to=" + to.ToString("F3") + " points=" + (path == null ? 0 : path.Count) + " endpointGap=" + gap.ToString("F3");
                            Debug.LogError(error); report.AppendLine(error);
                        }
                    }
                    // A broken route should have been counted by the controller;
                    // record an inconsistent ready/result contract independently.
                    if ((!person.RoutesReady || bad > 0) && life.RouteFailures == 0) failures[0]++;
                    string line = "AMBIENT_ROUTES_QA_PERSON " + place + "/" + schedule.id + " ready=" + person.RoutesReady +
                        " legs=" + schedule.stops.Length + " badLegs=" + bad + " model=" + schedule.model;
                    Debug.Log(line); report.AppendLine(line);
                }
                string summary = "AMBIENT_ROUTES_QA_REGION world=" + place + " people=" + life.People.Count + "/" + expected + " routeFailures=" + life.RouteFailures;
                Debug.Log(summary); report.AppendLine(summary);
            }
            string total = "AMBIENT_ROUTES_QA_SUMMARY worlds=7 people=" + people + "/" + expectedTotal + " legs=" + routes +
                " failedLegs=" + failedLegs + " totalIssues=" + failures[0];
            Debug.Log(total); report.AppendLine(total);
            File.WriteAllText(Path.Combine(output, "route-preflight.txt"), report.ToString());
            // Report every world before RunAmbientChecks verifies isolation and
            // raises one combined failure; ordinary full QA skips this extra pass.
        }

        static void LogAmbientCycleTimeout(AmbientLife life, int[] stopTargets, int[] cycleTargets, int[] cargoTargets)
        {
            for (int i = 0; i < life.People.Count; i++) {
                AmbientWalker person = life.People[i];
                List<Vector3> path = person.Routes != null && person.StopIndex >= 0 && person.StopIndex < person.Routes.Length ? person.Routes[person.StopIndex] : null;
                string nextPoint = path != null && person.SegmentIndex >= 0 && person.SegmentIndex < path.Count
                    ? path[person.SegmentIndex].ToString("F4") : "(route-end)";
                string nextStop = person.Schedule.stops[(person.StopIndex + 1) % person.Schedule.stops.Length].position.ToString("F4");
                Debug.LogError("AMBIENT_QA_CYCLE_STATE " + life.WorldId + "/" + person.Schedule.id +
                    " stopIndex=" + person.StopIndex + " segmentIndex=" + person.SegmentIndex + "/" + (path == null ? 0 : path.Count) +
                    " phase=" + person.Phase + " position=" + person.Position.ToString("F4") + " wait=" + person.Wait.ToString("F3") +
                    " nextWaypoint=" + nextPoint + " nextStop=" + nextStop + " completedStops=" + person.CompletedStops + "/" + stopTargets[i] +
                    " cycles=" + person.CompletedCycles + "/" + cycleTargets[i] + " cargoChanges=" + person.CargoChanges + "/" + cargoTargets[i] +
                    " held=" + person.HeldKind + " distance=" + person.DistanceWalked.ToString("F3") + " simulatedSeconds=" + life.SimulationSeconds.ToString("F2"));
            }
        }

        void EnterAmbientQANode(string node, bool aboard)
        {
            state = StoryEngine.NewGame(20260928); state.nodeId = node; state.journeyVersion = 0; state.journey = null; LifeJourney.Ensure(state);
            if (aboard) state.flags.Add("aboard_passage");
            dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = letterEditor = lifePanel = false;
            EnterNode(true); ResetView(); toastUntil = 0;
            if (ambientLife) ambientLife.Automatic = false;
        }

        IEnumerator WaitForAmbientReady(string place)
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while ((!ambientLife || !ambientLife.Ready) && Time.realtimeSinceStartup < deadline) yield return null;
            AmbientRequire(ambientLife && ambientLife.Ready && ambientLife.WorldId == place && loadedWorld == place, "READY_TIMEOUT_" + place);
            ambientLife.Automatic = false; yield return null; Physics.SyncTransforms();
        }

        void CheckAmbientOwnership(AmbientLife life, string place)
        {
            AmbientRequire(life && life.gameObject.activeInHierarchy && life.transform.IsChildOf(world.transform), "CURRENT_WORLD_OWNER_" + place);
            var controllers = UnityEngine.Object.FindObjectsByType<AmbientLife>();
            AmbientRequire(controllers.Length == 1 && controllers[0] == life, "ONE_ACTIVE_CONTROLLER_" + place);
            var instances = new HashSet<GameObject>(); var residentIds = new HashSet<string>(); var faceMeshes = new HashSet<Mesh>();
            foreach (AmbientWalker person in life.People) {
                AmbientRequire(person.Actor && person.Actor.activeInHierarchy && person.Actor.transform.IsChildOf(life.transform) && instances.Add(person.Actor), "ONE_OWNED_ACTOR_" + place);
                CheckAmbientAppearance(person);
                var face = person.Actor.GetComponent<AmbientAppearance>();
                AmbientRequire(residentIds.Add(face.ResidentId), "ONE_FACE_IDENTITY_" + place + "_" + face.ResidentId);
                foreach (var filter in person.Actor.GetComponentsInChildren<MeshFilter>()) if (filter.name.StartsWith("Head_and_tailored_body_Mesh", StringComparison.Ordinal))
                    AmbientRequire(filter.sharedMesh && faceMeshes.Add(filter.sharedMesh), "EXCLUSIVE_RESIDENT_FACE_MESH_" + face.ResidentId);
            }
        }

        static void CheckAmbientAppearance(AmbientWalker person)
        {
            var face = person.Actor ? person.Actor.GetComponent<AmbientAppearance>() : null;
            AmbientRequire(face && face.Ready && face.ResidentId == person.Schedule.id && face.BaseModel == person.Schedule.model,
                "FIXED_RESIDENT_IDENTITY_" + person.Schedule.id);
            AmbientRequire(face.ModifiedVertices > 1000 && face.MaximumDisplacement > .003f && face.MaximumDisplacement < .037f,
                "RESIDENT_FACE_HAS_REAL_GEOMETRY_" + person.Schedule.id);
            AmbientRequire(person.Actor.activeInHierarchy, "DISTINCT_RESIDENT_NOT_HIDDEN_BY_STORY_BASE_" + person.Schedule.id);
        }

        static void CheckAmbientRig(AmbientWalker person)
        {
            bool body = false;
            foreach (var skin in person.Actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                if (!skin.sharedMesh || !skin.sharedMesh.isReadable || skin.bones.Length != 5) continue;
                bool complete = Array.IndexOf(skin.bones, ModelLibrary.Find(person.Actor.transform, "BodyRoot")) >= 0;
                foreach (Transform joint in person.Joints) complete &= joint && Array.IndexOf(skin.bones, joint) >= 0;
                body |= complete;
            }
            AmbientRequire(body, "FIVE_BONE_READABLE_SKIN_" + person.Schedule.id);
            foreach (var animator in person.Actor.GetComponentsInChildren<Animator>())
                AmbientRequire(!animator.enabled, "NO_COMPETING_ANIMATOR_" + person.Schedule.id);
            // Ambient figures intentionally stop rendering offscreen for cost.
            // BakeMesh below still checks their actual posed weighted vertices.
        }

        float CheckAmbientRoutes(AmbientWalker person, string place)
        {
            AmbientSchedule schedule = person.Schedule;
            AmbientRequire(schedule.stops != null && schedule.stops.Length >= 2 && person.Routes != null && person.Routes.Length == schedule.stops.Length, "CLOSED_ROUTE_ARRAY_" + schedule.id);
            float length = 0, dwell = 0;
            for (int i = 0; i < person.Routes.Length; i++) {
                List<Vector3> path = person.Routes[i];
                AmbientRequire(path != null && path.Count > 0, "NONEMPTY_ROUTE_" + schedule.id + "_" + i);
                Vector3 goal = schedule.stops[(i + 1) % schedule.stops.Length].position;
                AmbientRequire(AmbientFlatDistance(path[path.Count - 1], goal) <= .85f, "REACHES_REAL_STOP_" + schedule.id + "_" + i);
                Vector3 from = schedule.stops[i].position;
                foreach (Vector3 to in path) {
                    float distance = AmbientFlatDistance(from, to); length += distance;
                    int samples = Mathf.Max(1, Mathf.CeilToInt(distance / .4f));
                    for (int s = 0; s <= samples; s++) CheckAmbientGround(Vector3.Lerp(from, to, (float)s / samples), place, schedule.id);
                    Vector3 direction = to - from; direction.y = 0;
                    if (direction.sqrMagnitude > .0001f)
                        AmbientRequire(!Physics.CapsuleCast(new Vector3(from.x, .55f, from.z), new Vector3(from.x, 1.65f, from.z), .30f,
                            direction.normalized, direction.magnitude, AmbientGroundMask, QueryTriggerInteraction.Ignore), "NO_WALL_CROSSING_" + schedule.id + "_" + i);
                    from = to;
                }
                dwell += Mathf.Max(0, schedule.stops[i].dwellSeconds);
            }
            return length / Mathf.Max(.1f, schedule.speed) + dwell;
        }

        static float AmbientFlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }

        static void CheckAmbientGround(Vector3 point, string place, string id)
        {
            AmbientRequire(!float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z) &&
                !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z) && WorldRegions.Get(place).IsGround(point), "IN_REGION_" + id + "_" + point);
            RaycastHit hit;
            AmbientRequire(Physics.Raycast(new Vector3(point.x, 4, point.z), Vector3.down, out hit, 8, AmbientGroundMask, QueryTriggerInteraction.Ignore) &&
                hit.point.y <= .5f && hit.normal.y >= .65f, "SOLID_GROUND_NOT_SEA_" + id + "_" + point);
        }

        static void CheckAmbientLiveState(AmbientWalker person, string place)
        {
            CheckAmbientGround(person.Position, place, person.Schedule.id);
            AmbientRequire(person.StopIndex >= 0 && person.StopIndex < person.Schedule.stops.Length, "VALID_STOP_INDEX_" + person.Schedule.id);
            if (person.Phase == "working")
                AmbientRequire(AmbientFlatDistance(person.Position, person.Schedule.stops[person.StopIndex].position) <= .9f, "WORK_AT_REAL_STOP_" + person.Schedule.id);
            if (!string.IsNullOrEmpty(person.HeldKind)) {
                AmbientRequire(person.HeldObject && person.HeldObject.activeInHierarchy && person.HeldObject.GetComponentInChildren<Renderer>(), "VISIBLE_HELD_PROP_" + person.Schedule.id);
                foreach (Collider collider in person.HeldObject.GetComponentsInChildren<Collider>(true))
                    AmbientRequire(!collider.enabled || collider.isTrigger, "PROP_CANNOT_BLOCK_PLAYER_" + person.Schedule.id);
            } else AmbientRequire(!person.HeldObject || !person.HeldObject.activeInHierarchy, "UNLOADED_PROP_HIDDEN_" + person.Schedule.id);
        }

        static int AmbientCargoTransitions(AmbientSchedule schedule)
        {
            var states = new List<string>();
            foreach (AmbientStop stop in schedule.stops) if (stop.carryAfterStop != null) states.Add(stop.carryAfterStop);
            int changes = 0;
            for (int i = 0; i < states.Count; i++) if (states[i] != states[(i + 1) % states.Count]) changes++;
            return changes;
        }
        static bool AmbientHasCarry(AmbientSchedule schedule) { foreach (AmbientStop stop in schedule.stops) if (!string.IsNullOrEmpty(stop.carryAfterStop)) return true; return false; }
        static bool AmbientHasUnload(AmbientSchedule schedule) { foreach (AmbientStop stop in schedule.stops) if (stop.carryAfterStop == "") return true; return false; }

        IEnumerator CheckAmbientPlayerAvoidance(AmbientLife life)
        {
            Vector3 originalPlayerPosition = player.transform.position;
            Quaternion originalPlayerRotation = player.transform.rotation;
            Transform originalPlayerReference = life.Player;
            bool originalAutomatic = life.Automatic;
            string originalStory = JsonUtility.ToJson(state);
            try {
                // Restore the production player and mission/workbench/family
                // exclusion points; the preceding full-cycle check clears them.
                EnsureAmbientLife(); life.Automatic = false;
                AmbientRequire(life == ambientLife && life.Player == player.transform, "LIVE_PLAYER_AVOIDANCE_ENABLED");
                AmbientWalker selected = null; Vector3 obstacle = Vector3.zero, forward = Vector3.zero;
                for (int step = 0; step < 2400 && selected == null; step++) {
                    foreach (AmbientWalker person in life.People) {
                        if (person.Phase != "walking") continue;
                        Vector3 ahead;
                        if (!AmbientPointAhead(person, 1.3f, out ahead)) continue;
                        Vector3 direction = ahead - person.Position; direction.y = 0;
                        if (direction.magnitude < 1.2f) continue; // Avoid a bend or a near-complete route.
                        direction.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, direction);
                        if (!AmbientOpenSpot(ahead, life.WorldId) ||
                            (!AmbientOpenSpot(ahead + right * .95f, life.WorldId) && !AmbientOpenSpot(ahead - right * .95f, life.WorldId))) continue;
                        if (Physics.CapsuleCast(new Vector3(person.Position.x, .55f, person.Position.z), new Vector3(person.Position.x, 1.55f, person.Position.z),
                            .28f, direction, AmbientFlatDistance(person.Position, ahead), AmbientGroundMask, QueryTriggerInteraction.Ignore)) continue;
                        bool clear = AmbientFlatDistance(ahead, mission.npcPosition) > 2 && AmbientFlatDistance(ahead, sidePosition) > 2;
                        foreach (Vector3 point in inspectionPositions) clear &= AmbientFlatDistance(ahead, point) > 2;
                        if (writingPlace != null) clear &= AmbientFlatDistance(ahead, writingPlace.approach) > 2;
                        foreach (AmbientWalker other in life.People) if (other != person) clear &= AmbientFlatDistance(ahead, other.Position) > 1.5f;
                        if (!clear) continue;
                        selected = person; obstacle = ahead; forward = direction; break;
                    }
                    if (selected == null) { life.Step(.05f); if (step % 32 == 0) yield return null; }
                }
                AmbientRequire(selected != null, "PLAYER_AVOIDANCE_OPEN_ROUTE_FOUND");
                Vector3 begin = selected.Position;
                controller.enabled = false;
                player.transform.position = WorldFactory.CharacterGroundPosition(obstacle) + Vector3.up * .1f;
                controller.enabled = true; Physics.SyncTransforms();
                Vector3 rightOfRoute = Vector3.Cross(Vector3.up, forward);
                float closest = float.PositiveInfinity, lateral = 0; bool yielded = false;
                for (int step = 0; step < 80; step++) {
                    life.Step(.05f);
                    foreach (AmbientWalker person in life.People) {
                        float gap = AmbientFlatDistance(person.Position, player.transform.position);
                        AmbientRequire(gap >= .75f, "NEVER_ENTER_PLAYER_BODY_" + person.Schedule.id + "_gap=" + gap.ToString("F4"));
                    }
                    closest = Mathf.Min(closest, AmbientFlatDistance(selected.Position, player.transform.position));
                    lateral = Mathf.Max(lateral, Mathf.Abs(Vector3.Dot(selected.Position - begin, rightOfRoute)));
                    yielded |= selected.Phase == "yielding";
                    if (step % 13 == 0) CheckAmbientLiveState(selected, life.WorldId);
                    if (step % 16 == 0) yield return null;
                }
                AmbientRequire(closest < 1.3f && (yielded || lateral > .18f), "PLAYER_CAUSES_YIELD_OR_DETOUR_" + selected.Schedule.id);
                int completedBeforeRemoval = selected.CompletedStops;
                float distanceBeforeRemoval = selected.DistanceWalked;
                controller.enabled = false; player.transform.position = originalPlayerPosition; controller.enabled = true; Physics.SyncTransforms();
                // Keep Player and all real task exclusions enabled after the
                // player leaves; do not make resuming easier by clearing them.
                int limit = Mathf.CeilToInt((AmbientSecondsToNextWork(selected) * 2f + 20f) / .05f);
                bool resumed = false;
                for (int step = 0; step < limit; step++) {
                    life.Step(.05f);
                    AmbientRequire(life.RouteFailures == 0, "PLAYER_AVOIDANCE_ROUTES_REMAIN_VALID");
                    foreach (AmbientWalker person in life.People)
                        AmbientRequire(AmbientFlatDistance(person.Position, player.transform.position) >= .75f, "PLAYER_BODY_AFTER_REMOVAL_" + person.Schedule.id);
                    if (step % 13 == 0) CheckAmbientLiveState(selected, life.WorldId);
                    resumed = selected.CompletedStops > completedBeforeRemoval && selected.DistanceWalked - distanceBeforeRemoval > 2;
                    if (resumed) break;
                    if (step % 32 == 0) yield return null;
                }
                AmbientRequire(resumed, "RESUMES_WORK_AFTER_PLAYER_LEAVES_" + selected.Schedule.id + "_phase=" + selected.Phase +
                    "_stop=" + selected.StopIndex + "_segment=" + selected.SegmentIndex + "_position=" + selected.Position.ToString("F4"));
                AmbientRequire(originalStory == JsonUtility.ToJson(state), "PLAYER_AVOIDANCE_STORY_UNCHANGED");
                Debug.Log("AMBIENT_QA_PLAYER_AVOIDANCE_PASSED actor=" + selected.Schedule.id + " playerBlock=" + obstacle.ToString("F3") +
                    " minimumGap=" + closest.ToString("F3") + " lateralMetres=" + lateral.ToString("F3") + " yielded=" + yielded +
                    " resumedStops=" + (selected.CompletedStops - completedBeforeRemoval) + " resumedMetres=" + (selected.DistanceWalked - distanceBeforeRemoval).ToString("F2") +
                    " taskExclusions=production playerReference=enabled");
            } finally {
                controller.enabled = false; player.transform.SetPositionAndRotation(originalPlayerPosition, originalPlayerRotation); controller.enabled = true;
                Physics.SyncTransforms(); UpdateCamera(true);
                life.Automatic = originalAutomatic; life.Player = originalPlayerReference; life.SetExclusions(new Vector3[0]);
            }
        }

        static bool AmbientPointAhead(AmbientWalker person, float distance, out Vector3 result)
        {
            result = person.Position; List<Vector3> route = person.Routes[person.StopIndex];
            for (int i = person.SegmentIndex; i < route.Count; i++) {
                float span = AmbientFlatDistance(result, route[i]);
                if (span >= distance) { result = Vector3.Lerp(result, route[i], distance / span); return true; }
                distance -= span; result = route[i];
            }
            return false;
        }

        static bool AmbientOpenSpot(Vector3 point, string place)
        {
            RaycastHit hit;
            return WorldRegions.Get(place).IsGround(point) &&
                Physics.Raycast(new Vector3(point.x, 4, point.z), Vector3.down, out hit, 8, AmbientGroundMask, QueryTriggerInteraction.Ignore) &&
                hit.point.y <= .5f && hit.normal.y >= .65f &&
                !Physics.CheckCapsule(new Vector3(point.x, .55f, point.z), new Vector3(point.x, 1.55f, point.z), .30f, AmbientGroundMask, QueryTriggerInteraction.Ignore);
        }

        static float AmbientSecondsToNextWork(AmbientWalker person)
        {
            float distance = 0; Vector3 point = person.Position;
            for (int leg = 0; leg < person.Routes.Length; leg++) {
                int index = (person.StopIndex + leg) % person.Routes.Length; List<Vector3> route = person.Routes[index];
                int first = leg == 0 ? Mathf.Clamp(person.SegmentIndex, 0, route.Count) : 0;
                for (int i = first; i < route.Count; i++) { distance += AmbientFlatDistance(point, route[i]); point = route[i]; }
                AmbientStop destination = person.Schedule.stops[(index + 1) % person.Routes.Length];
                if (destination.dwellSeconds > 0) return distance / Mathf.Max(.1f, person.Schedule.speed * .85f) + destination.dwellSeconds;
            }
            return 120;
        }

        sealed class AmbientPoseProbe : IDisposable
        {
            readonly AmbientWalker person;
            readonly SkinnedMeshRenderer skin;
            readonly Mesh baked = new Mesh();
            readonly int[] vertices = {-1, -1, -1, -1};
            readonly Vector3[] initial = new Vector3[4];
            readonly float[] movement = new float[4];
            public bool Complete { get { foreach (float value in movement) if (value < .004f) return false; return true; } }
            public string Summary => string.Join(",", Array.ConvertAll(movement, value => value.ToString("F4")));

            public AmbientPoseProbe(AmbientWalker person)
            {
                this.person = person;
                foreach (var candidate in person.Actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    bool hasLimbs = candidate.sharedMesh && candidate.sharedMesh.isReadable;
                    for (int j = 0; j < 4 && hasLimbs; j++) hasLimbs &= Array.IndexOf(candidate.bones, person.Joints[j]) >= 0;
                    if (hasLimbs) { skin = candidate; break; }
                }
                AmbientRequire(skin, "POSE_PROBE_SKIN_" + person.Schedule.id);
                skin.BakeMesh(baked, false); Vector3[] points = baked.vertices; BoneWeight[] weights = skin.sharedMesh.boneWeights;
                for (int j = 0; j < 4; j++) {
                    int bone = Array.IndexOf(skin.bones, person.Joints[j]); float largest = 0;
                    for (int i = 0; i < weights.Length; i++) {
                        BoneWeight w = weights[i]; float influence = (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0) +
                            (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0);
                        float distance = Vector3.Distance(skin.transform.TransformPoint(points[i]), person.Joints[j].position);
                        if (influence > .5f && distance > largest) { largest = distance; vertices[j] = i; }
                    }
                    AmbientRequire(vertices[j] >= 0, "POSE_PROBE_WEIGHT_" + person.Schedule.id + "_" + j);
                    initial[j] = person.Actor.transform.InverseTransformPoint(skin.transform.TransformPoint(points[vertices[j]]));
                }
            }
            public void Sample()
            {
                skin.BakeMesh(baked, false); Vector3[] points = baked.vertices;
                for (int j = 0; j < 4; j++) {
                    Vector3 local = person.Actor.transform.InverseTransformPoint(skin.transform.TransformPoint(points[vertices[j]]));
                    // Compare in actor space, then convert the displacement to
                    // metres: moving the entire actor cannot fake limb motion.
                    movement[j] = Mathf.Max(movement[j], person.Actor.transform.TransformVector(local - initial[j]).magnitude);
                }
            }
            public void Dispose() { if (baked) UnityEngine.Object.Destroy(baked); }
        }

        IEnumerator CaptureAmbientWalking(AmbientLife life, string output)
        {
            AmbientWalker walker = null;
            for (int step = 0; step < 6000 && walker == null; step++) {
                foreach (AmbientWalker candidate in life.People) {
                    if (candidate.Phase != "walking") continue;
                    List<Vector3> route = candidate.Routes[candidate.StopIndex];
                    float remaining = 0; Vector3 point = candidate.Position;
                    for (int i = Mathf.Clamp(candidate.SegmentIndex, 0, route.Count); i < route.Count; i++) { remaining += AmbientFlatDistance(point, route[i]); point = route[i]; }
                    if (remaining > 3.5f) { walker = candidate; break; }
                }
                if (walker == null) { life.Step(.05f); if (step % 32 == 0) yield return null; }
            }
            AmbientRequire(walker != null, "LIVE_CAPTURE_WALKER");
            string storyBefore = JsonUtility.ToJson(state);
            var go = new GameObject("Ambient QA walking camera"); Camera capture = go.AddComponent<Camera>(); capture.CopyFrom(cam); capture.enabled = false;
            var texture = new RenderTexture(1400, 900, 24, RenderTextureFormat.ARGB32);
            var positions = new System.Text.StringBuilder("seconds,id,phase,x,y,z,distanceWalked,heldKind\n");
            bool automatic = life.Automatic; float distanceBefore = walker.DistanceWalked;
            try {
                texture.Create(); capture.targetTexture = texture; capture.rect = new Rect(0, 0, 1, 1); capture.aspect = 1400f / 900f;
                capture.fieldOfView = 52; capture.nearClipPlane = .05f; capture.farClipPlane = 100;
                capture.cullingMask = ~((1 << HeldItemLayer) | (1 << PortraitLayer));
                life.Automatic = true;
                float start = Time.realtimeSinceStartup;
                int startFrame = Time.frameCount;
                for (int frame = 0; frame <= 5; frame++) {
                    while (Time.realtimeSinceStartup - start < frame * 2f) yield return null;
                    Vector3 target = walker.Position + Vector3.up * .92f;
                    Vector3 offset = new Vector3(4.4f, 1.9f, -5.8f);
                    for (int attempt = 0; attempt < 8; attempt++) {
                        Vector3 candidate = Quaternion.Euler(0, attempt * 45f, 0) * offset;
                        if (!Physics.Linecast(target, target + candidate, AmbientGroundMask, QueryTriggerInteraction.Ignore)) { offset = candidate; break; }
                    }
                    capture.transform.position = target + offset; capture.transform.LookAt(target); capture.Render();
                    SaveFamilyReviewTexture(texture, Path.Combine(output, "market_walk_" + (frame * 2).ToString("D2") + "s.png"));
                    Vector3 p = walker.Position;
                    positions.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2},{1},{2},{3:F4},{4:F4},{5:F4},{6:F3},{7}\n",
                        Time.realtimeSinceStartup - start, walker.Schedule.id, walker.Phase, p.x, p.y, p.z, walker.DistanceWalked, walker.HeldKind);
                }
                AmbientRequire(walker.DistanceWalked - distanceBefore > 1.5f, "LIVE_TEN_SECOND_MOVEMENT");
                AmbientRequire(storyBefore == JsonUtility.ToJson(state), "LIVE_CAPTURE_STORY_UNCHANGED");
                File.WriteAllText(Path.Combine(output, "market_walk_positions.csv"), positions.ToString());
                Debug.Log("AMBIENT_QA_LIVE_CAPTURE_PASSED seconds=10 frames=6 actor=" + walker.Schedule.id + " metres=" + (walker.DistanceWalked - distanceBefore).ToString("F2") +
                    " observedFps=" + ((Time.frameCount - startFrame) / (Time.realtimeSinceStartup - start)).ToString("F1"));
            } finally {
                life.Automatic = automatic; capture.targetTexture = null; texture.Release(); Destroy(texture); Destroy(go);
            }
        }
    }
}
