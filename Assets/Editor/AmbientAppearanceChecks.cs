using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class AmbientAppearanceChecks
    {
        static readonly string[] Worlds = { "quanzhou", "harbor", "ship", "port", "market", "quarters", "postoffice" };
        static int checks;
        sealed class Face { public string id, model; public Vector3[] vertices; }

        public static void Run()
        {
            checks = 0; var seen = new HashSet<string>(); var faces = new List<Face>();
            var originalState = UnityEngine.Random.state;
            foreach (string world in Worlds) foreach (var plan in AmbientSchedules.Get(world)) {
                Require(seen.Add(plan.id), "UNIQUE_RESIDENT_ID_" + plan.id);
                Require(AmbientAppearance.TryProfile(plan.id, out var profile), "AUTHORED_PROFILE_" + plan.id);
                var actor = CharacterAppearanceProfiles.SpawnIndependentBase(plan.model, null, Vector3.zero);
                var repeat = CharacterAppearanceProfiles.SpawnIndependentBase(plan.model, null, new Vector3(18, 0, -21));
                repeat.transform.rotation = Quaternion.Euler(0, 71, 0);
                try {
                    MeshFilter head = Head(actor); Mesh source = head.sharedMesh; Vector3[] original = source.vertices;
                    Mesh[] body = BodyMeshes(actor);
                    var appearance = AmbientAppearance.Apply(actor, plan); var duplicate = AmbientAppearance.Apply(repeat, plan);
                    Require(appearance.Ready && appearance.ResidentId == plan.id && duplicate.ResidentId == plan.id, "IDENTITY_BOUND_" + plan.id);
                    Require(appearance.ModifiedVertices > 1000 && appearance.MaximumDisplacement > .003f && appearance.MaximumDisplacement < .037f,
                        "MEASURABLE_BOUNDED_SCULPT_" + plan.id + "_" + appearance.MaximumDisplacement);
                    Require(head.sharedMesh != source && Head(repeat).sharedMesh != head.sharedMesh, "INSTANCE_OWNERSHIP_" + plan.id);
                    Require(source.vertexCount == head.sharedMesh.vertexCount && source.subMeshCount == head.sharedMesh.subMeshCount, "TOPOLOGY_" + plan.id);
                    Equal(source.uv, head.sharedMesh.uv, "FACE_UV_PRESERVED_" + plan.id);
                    Equal(source.bindposes, head.sharedMesh.bindposes, "BINDPOSES_PRESERVED_" + plan.id);
                    Equal(original, source.vertices, "SHARED_STORY_FACE_UNCHANGED_" + plan.id);
                    var bodyAfter = BodyMeshes(actor);
                    Require(body.Length == bodyAfter.Length, "BODY_COUNT_" + plan.id);
                    for (int i = 0; i < body.Length; i++) Require(body[i] == bodyAfter[i], "BODY_WEIGHTS_AND_MESH_UNTOUCHED_" + plan.id);
                    var first = MetricVertices(actor, head); var second = MetricVertices(repeat, Head(repeat));
                    float reconstructionError = Rms(first, second);
                    Require(first.Length == second.Length && reconstructionError < .000004f,
                        "RELOAD_AND_ROTATION_STABILITY_" + plan.id + "_error=" + reconstructionError.ToString("F9") + "_scales=" + actor.transform.localScale + "/" + repeat.transform.localScale);
                    CheckRigidEyes(actor, head, original);
                    float skinDifference = SkinRms(actor, head, original, head.sharedMesh.vertices);
                    Require(skinDifference > .0013f, "REAL_FACE_NOT_JUST_HAIR_OR_DYE_" + plan.id + "_" + skinDifference);
                    foreach (var earlier in faces) if (earlier.model == plan.model) {
                        float rms = Rms(first, earlier.vertices);
                        Require(rms > .0015f, "SAME_BASE_FACES_DIFFER_" + earlier.id + "_" + plan.id + "_" + rms);
                    }
                    faces.Add(new Face { id = plan.id, model = plan.model, vertices = first });
                    Require(AmbientAppearance.Apply(actor, plan) == appearance, "IDEMPOTENT_NO_RESHUFFLE_" + plan.id);
                    CheckSerializedLifetime(actor, appearance, plan, first);
                    UnityEngine.Object.DestroyImmediate(appearance);
                    Require(head.sharedMesh == source, "REMOVAL_RESTORES_RESOURCE_" + plan.id);
                    Equal(original, source.vertices, "RESOURCE_CLEAN_AFTER_REMOVE_" + plan.id);
                    Debug.Log("AMBIENT_APPEARANCE_ID " + world + "/" + plan.id + " base=" + plan.model + " face=" + profile.description + " skinRmsMetres=" + skinDifference.ToString("F5"));
                } finally { UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(repeat); }
            }
            Require(seen.Count == 37 && AmbientAppearance.Profiles.Length == seen.Count, "ALL_37_AUTHORED_RESIDENTS");
            Require(JsonUtility.ToJson(originalState) == JsonUtility.ToJson(UnityEngine.Random.state), "NO_GLOBAL_RANDOM_OR_SAVE_DEPENDENCY");
            Debug.Log("AMBIENT_APPEARANCE_CHECKS_PASSED residents=" + seen.Count + " assertions=" + checks + " unique facial geometry; independent meshes; stable reloads; original story assets, body rigs and UVs unchanged");
        }

        [MenuItem("侨批/验收路人独立外貌")]
        public static void RenderReview()
        {
            Run();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/ambient-appearance-v280"));
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-qiaopi-ambient-appearance-output");
            if (option >= 0 && option + 1 < args.Length) output = Path.GetFullPath(args[option + 1]);
            Directory.CreateDirectory(output);
            var groups = new Dictionary<string, List<AmbientSchedule>>();
            foreach (string world in Worlds) foreach (var plan in AmbientSchedules.Get(world)) {
                if (!groups.TryGetValue(plan.model, out var plans)) { plans = new List<AmbientSchedule>(); groups.Add(plan.model, plans); }
                plans.Add(plan);
            }
            var stage = new GameObject("Ambient appearance review (temporary)"); stage.transform.position = new Vector3(1500, 1200, 1500);
            var cameraObject = new GameObject("Resident face review camera"); var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.nearClipPlane = .05f; camera.farClipPlane = 8;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .155f, .15f); camera.cullingMask = 1 << 30;
            var lights = new List<GameObject>();
            var previousAmbient = RenderSettings.ambientMode; Color previousSky = RenderSettings.ambientSkyColor, previousEquator = RenderSettings.ambientEquatorColor, previousGround = RenderSettings.ambientGroundColor;
            try {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.54f, .56f, .59f); RenderSettings.ambientEquatorColor = new Color(.41f, .43f, .41f); RenderSettings.ambientGroundColor = new Color(.23f, .22f, .19f);
                lights.Add(LightAt(stage.transform.position + new Vector3(-3, 3, 4), stage.transform.position + Vector3.up * 1.55f, new Color(1, .93f, .84f), 1.15f));
                lights.Add(LightAt(stage.transform.position + new Vector3(3, 1.6f, 3), stage.transform.position + Vector3.up * 1.55f, new Color(.77f, .86f, 1), .50f));
                var manifest = new System.Text.StringBuilder("model,column,residentId,faceDescription\n");
                foreach (var group in groups) {
                    var actors = new List<GameObject>();
                    int columns = group.Value.Count + 1; const float spacing = .39f;
                    var texture = new RenderTexture(columns * 360, 460, 24, RenderTextureFormat.ARGB32);
                    try {
                        texture.Create(); camera.targetTexture = texture; camera.aspect = (float)texture.width / texture.height; camera.orthographicSize = .249f;
                        for (int i = 0; i < columns; i++) {
                            var actor = CharacterAppearanceProfiles.SpawnIndependentBase(group.Key, stage.transform, Vector3.zero); actors.Add(actor);
                            AmbientAppearance appearance = i == 0 ? null : AmbientAppearance.Apply(actor, group.Value[i - 1]);
                            Vector3 eye = appearance ? appearance.EyeCenter : EyeMetric(actor, Head(actor));
                            actor.transform.localPosition = new Vector3(((columns - 1) * .5f - i) * spacing, 1.53f - eye.y, 0);
                            foreach (Transform child in actor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                            string id = appearance ? appearance.ResidentId : "story_reference_" + group.Key;
                            manifest.Append(group.Key).Append(',').Append(i + 1).Append(',').Append(id).Append(',').Append(appearance ? appearance.FaceDescription : "原剧情人物作对照").Append('\n');
                        }
                        Vector3 target = stage.transform.position + Vector3.up * 1.50f;
                        camera.transform.position = target + Vector3.forward * 3.5f; camera.transform.LookAt(target);
                        camera.Render(); Save(texture, Path.Combine(output, group.Key + "_residents_front.png"));
                        for (int i = 0; i < actors.Count; i++) actors[i].transform.localRotation = Quaternion.Euler(0, -26, 0);
                        camera.Render(); Save(texture, Path.Combine(output, group.Key + "_residents_threequarter.png"));
                    } finally {
                        camera.targetTexture = null; texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
                        foreach (var actor in actors) UnityEngine.Object.DestroyImmediate(actor);
                    }
                }
                File.WriteAllText(Path.Combine(output, "resident-face-columns.csv"), manifest.ToString());
                Debug.Log("AMBIENT_APPEARANCE_REVIEW_OUTPUT=" + output);
            } finally {
                RenderSettings.ambientMode = previousAmbient; RenderSettings.ambientSkyColor = previousSky; RenderSettings.ambientEquatorColor = previousEquator; RenderSettings.ambientGroundColor = previousGround;
                foreach (var light in lights) UnityEngine.Object.DestroyImmediate(light);
                UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(stage);
            }
        }

        static GameObject LightAt(Vector3 position, Vector3 target, Color colour, float intensity)
        {
            var root = new GameObject("Face review lamp"); var light = root.AddComponent<Light>();
            root.transform.position = position; root.transform.LookAt(target); light.type = LightType.Directional;
            light.color = colour; light.intensity = intensity; light.cullingMask = 1 << 30; light.shadows = LightShadows.Soft; return root;
        }
        static void CheckSerializedLifetime(GameObject actor, AmbientAppearance appearance, AmbientSchedule plan, Vector3[] expected)
        {
            // Use Unity's native serialized object/reference path. Editor JSON
            // encodes unsaved Mesh/GameObject references as instanceID:0 in
            // Unity 6.6; JSON overwrite would deliberately null valid live refs
            // and is not a model of a domain reload.
            var serialized = new SerializedObject(appearance);
            Require(serialized.FindProperty("residentId").stringValue == plan.id &&
                serialized.FindProperty("modifiedVertices").intValue == appearance.ModifiedVertices,
                "DOMAIN_RELOAD_IDENTITY_FIELDS_SERIALIZED_" + plan.id);
            var owned = serialized.FindProperty("owned"); var filters = serialized.FindProperty("filters"); var originals = serialized.FindProperty("originals");
            Require(owned != null && filters != null && originals != null && owned.arraySize == 1 && filters.arraySize == 1 && originals.arraySize == 1,
                "DOMAIN_RELOAD_OWNERSHIP_FIELDS_SERIALIZED_" + plan.id);
            Require(owned.GetArrayElementAtIndex(0).objectReferenceValue == Head(actor).sharedMesh &&
                filters.GetArrayElementAtIndex(0).objectReferenceValue == Head(actor) && originals.GetArrayElementAtIndex(0).objectReferenceValue,
                "NATIVE_SERIALIZATION_RETAINS_LIVE_REFERENCES_" + plan.id);
            var clone = UnityEngine.Object.Instantiate(actor);
            try {
                var copiedAppearance = clone.GetComponent<AmbientAppearance>();
                Require(copiedAppearance && copiedAppearance.Ready && copiedAppearance.ResidentId == plan.id, "CLONE_IDENTITY_SURVIVES_" + plan.id);
                Require(Head(clone).sharedMesh != Head(actor).sharedMesh, "CLONE_TRANSIENT_GEOMETRY_OWNED_" + plan.id);
                Require(Rms(MetricVertices(clone, Head(clone)), expected) < .000001f, "CLONE_FACE_EXACT_" + plan.id);
                Mesh copyMesh = Head(clone).sharedMesh;
                UnityEngine.Object.DestroyImmediate(copiedAppearance);
                Require(!copyMesh && appearance.Ready && Head(actor).sharedMesh, "CLONE_RELEASE_DOES_NOT_DESTROY_SOURCE_" + plan.id);
                Require(Rms(MetricVertices(actor, Head(actor)), expected) < .000001f, "SOURCE_FACE_AFTER_CLONE_RELEASE_" + plan.id);
            } finally { UnityEngine.Object.DestroyImmediate(clone); }
            // Also cover an editor tool that clones and immediately cancels
            // without asking the clone to initialize or entering another frame.
            var cancelled = UnityEngine.Object.Instantiate(actor);
            UnityEngine.Object.DestroyImmediate(cancelled);
            Require(appearance.Ready && Head(actor).sharedMesh && Rms(MetricVertices(actor, Head(actor)), expected) < .000001f,
                "UNINITIALIZED_CLONE_RELEASE_LEAVES_SOURCE_" + plan.id);
        }
        static void Save(RenderTexture render, string path)
        {
            RenderTexture previous = RenderTexture.active; var image = new Texture2D(render.width, render.height, TextureFormat.RGB24, false);
            try { RenderTexture.active = render; image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
        }
        static MeshFilter Head(GameObject actor)
        {
            foreach (var filter in actor.GetComponentsInChildren<MeshFilter>()) if (filter.name.StartsWith("Head_and_tailored_body_Mesh", StringComparison.Ordinal)) return filter;
            throw new InvalidOperationException("AMBIENT_CHECK_HEAD_MISSING " + actor.name);
        }
        static Mesh[] BodyMeshes(GameObject actor)
        {
            var skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>(); var result = new Mesh[skins.Length];
            for (int i = 0; i < skins.Length; i++) result[i] = skins[i].sharedMesh; return result;
        }
        static Matrix4x4 Metric(GameObject actor, MeshFilter head)
            => AmbientAppearance.MetricFromHead(actor.transform, head.transform);
        static Vector3[] MetricVertices(GameObject actor, MeshFilter head)
        {
            Matrix4x4 matrix = Metric(actor, head); var points = head.sharedMesh.vertices;
            for (int i = 0; i < points.Length; i++) points[i] = matrix.MultiplyPoint3x4(points[i]); return points;
        }
        static Vector3 EyeMetric(GameObject actor, MeshFilter head)
        {
            Matrix4x4 matrix = Metric(actor, head); var points = head.sharedMesh.vertices; Vector3 sum = Vector3.zero; int count = 0;
            foreach (int index in SurfaceIndices(head, "Qiaopi/CharacterEye")) { sum += matrix.MultiplyPoint3x4(points[index]); count++; }
            Require(count > 200, "REVIEW_EYE_CENTRE"); return sum / count;
        }
        static HashSet<int> SurfaceIndices(MeshFilter head, string shader)
        {
            var indices = new HashSet<int>(); var materials = head.GetComponent<MeshRenderer>().sharedMaterials;
            for (int slot = 0; slot < Mathf.Min(head.sharedMesh.subMeshCount, materials.Length); slot++)
                if (materials[slot] && materials[slot].shader && materials[slot].shader.name == shader)
                    foreach (int index in head.sharedMesh.GetIndices(slot)) indices.Add(index);
            return indices;
        }
        static float SkinRms(GameObject actor, MeshFilter head, Vector3[] before, Vector3[] after)
        {
            Matrix4x4 matrix = Metric(actor, head); double total = 0; int count = 0;
            foreach (int i in SurfaceIndices(head, "Qiaopi/CharacterSkinTexture")) { total += matrix.MultiplyVector(after[i] - before[i]).sqrMagnitude; count++; }
            Require(count > 500, "ACTUAL_ANATOMICAL_SKIN"); return Mathf.Sqrt((float)(total / count));
        }
        static void CheckRigidEyes(GameObject actor, MeshFilter head, Vector3[] original)
        {
            var points = head.sharedMesh.vertices; Matrix4x4 matrix = Metric(actor, head); Vector3 left = Vector3.zero, right = Vector3.zero; bool hasLeft = false, hasRight = false;
            foreach (int index in SurfaceIndices(head, "Qiaopi/CharacterEye")) {
                bool l = matrix.MultiplyPoint3x4(original[index]).x < 0; Vector3 displacement = matrix.MultiplyVector(points[index] - original[index]);
                if (l && !hasLeft) { left = displacement; hasLeft = true; }
                if (!l && !hasRight) { right = displacement; hasRight = true; }
                Require(Vector3.Distance(displacement, l ? left : right) < .000004f, "EYEBALL_STAYS_RIGID");
            }
            Require(hasLeft && hasRight, "BOTH_FITTED_EYES");
        }
        static float Rms(Vector3[] a, Vector3[] b)
        {
            Require(a.Length == b.Length, "COMPARABLE_VERTEX_COUNT"); double total = 0;
            for (int i = 0; i < a.Length; i++) total += (a[i] - b[i]).sqrMagnitude;
            return Mathf.Sqrt((float)(total / a.Length));
        }
        static void Equal<T>(T[] a, T[] b, string label)
        { Require(a.Length == b.Length, label + "_COUNT"); for (int i = 0; i < a.Length; i++) Require(EqualityComparer<T>.Default.Equals(a[i], b[i]), label + "_" + i); }
        static void Require(bool value, string label) { checks++; if (!value) throw new InvalidOperationException("AMBIENT_APPEARANCE_CHECK_FAILED " + label); }
    }
}
