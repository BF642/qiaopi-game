using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class SupportingAppearanceChecks
    {
        static int checks;
        sealed class Face { public string id, model; public Vector3[] skin; }
        static readonly string[] Worlds = { "quanzhou", "harbor", "ship", "port", "market", "quarters", "postoffice" };

        public static void VerifyAndRenderAll()
        {
            AmbientAppearanceChecks.Run();
            RenderReview();
        }

        public static void Run()
        {
            checks = 0;
            var ids = new HashSet<string>(); var faces = new List<Face>();
            var random = UnityEngine.Random.state;
            foreach (var profile in CharacterAppearanceProfiles.Supporting) {
                Require(ids.Add(profile.id), "UNIQUE_SUPPORTING_ID_" + profile.id);
                GameObject source = null, actor = null, repeat = null;
                try {
                    source = CharacterAppearanceProfiles.SpawnIndependentBase(profile.model, null, Vector3.zero);
                    var sourceHead = Head(source); var shared = sourceHead.sharedMesh;
                    var original = shared.vertices;
                    var materials = sourceHead.GetComponent<MeshRenderer>().sharedMaterials;
                    var colors = new Color[materials.Length];
                    for (int i = 0; i < materials.Length; i++) colors[i] = materials[i].HasProperty("_Color") ? materials[i].GetColor("_Color") : Color.clear;
                    actor = CharacterAppearanceProfiles.Spawn(profile.id, null, Vector3.zero);
                    repeat = CharacterAppearanceProfiles.Spawn(profile.id, null, new Vector3(-31, 0, 12));
                    repeat.transform.localRotation = Quaternion.Euler(0, 123, 0);
                    var face = actor.GetComponent<AmbientAppearance>();
                    Require(face && face.Ready && face.ResidentId == profile.id && face.BaseModel == profile.model, "FIXED_IDENTITY_" + profile.id);
                    Require(!actor.GetComponent<NativeCharacterRig>(), "UNREVIEWED_NATIVE_HEAD_ISOLATED_" + profile.id);
                    Require(actor.GetComponent<CharacterModelIdentity>().modelId == profile.model, "GAMEPLAY_MODEL_PRESERVED_" + profile.id);
                    Require(Head(actor).sharedMesh != shared && Head(repeat).sharedMesh != Head(actor).sharedMesh, "PRIVATE_FACE_MESH_" + profile.id);
                    Equal(original, shared.vertices, "SHARED_MAIN_FACE_UNCHANGED_" + profile.id);
                    Equal(shared.uv, Head(actor).sharedMesh.uv, "FACE_UV_UNCHANGED_" + profile.id);
                    for (int i = 0; i < materials.Length; i++) if (materials[i].HasProperty("_Color"))
                        Require(materials[i].GetColor("_Color") == colors[i], "SHARED_PIGMENTS_UNCHANGED_" + profile.id);
                    var skin = Skin(actor);
                    Require(Rms(skin, Skin(source)) > .0013f, "REAL_FACE_DIFFERS_FROM_MAIN_" + profile.id);
                    Require(Rms(skin, Skin(repeat)) < .000004f, "STABLE_AFTER_RECREATE_AND_MOVE_" + profile.id);
                    Require(face.MaximumDisplacement < .037f && face.MaximumDisplacement > .003f, "BOUNDED_HEAD_SCULPT_" + profile.id);
                    CheckRigidEyes(source, actor);
                    CheckBodyMeshes(source, actor);
                    foreach (var earlier in faces) if (earlier.model == profile.model)
                        Require(Rms(skin, earlier.skin) > .0015f, "SUPPORTING_FACES_DISTINCT_" + profile.id + "_" + earlier.id);
                    faces.Add(new Face { id = profile.id, model = profile.model, skin = skin });
                    var clone = UnityEngine.Object.Instantiate(actor);
                    try {
                        var cloned = clone.GetComponent<AmbientAppearance>();
                        Require(cloned && cloned.Ready && cloned.ResidentId == profile.id, "CLONE_IDENTITY_" + profile.id);
                        Require(Head(clone).sharedMesh != Head(actor).sharedMesh && Rms(Skin(clone), skin) < .000004f, "CLONE_PRIVATE_EXACT_FACE_" + profile.id);
                        UnityEngine.Object.DestroyImmediate(cloned);
                        Require(face.Ready && Rms(Skin(actor), skin) < .000004f, "CLONE_RELEASE_LEAVES_ORIGINAL_" + profile.id);
                    } finally { UnityEngine.Object.DestroyImmediate(clone); }
                    Require(AmbientAppearance.Apply(actor, profile.id, profile.model, profile.face) == face, "APPLY_IDEMPOTENT_" + profile.id);
                    bool rejected = false;
                    try { AmbientAppearance.Apply(actor, "not-this-person", profile.model,
                        new AmbientFaceProfile("not-this-person", "", 0, 0, 0, 0, 0, 0, 0, 0, 0)); }
                    catch (InvalidOperationException) { rejected = true; }
                    Require(rejected, "CANNOT_SWAP_FACE_IN_PLACE_" + profile.id);
                } finally {
                    if (source) UnityEngine.Object.DestroyImmediate(source);
                    if (actor) UnityEngine.Object.DestroyImmediate(actor);
                    if (repeat) UnityEngine.Object.DestroyImmediate(repeat);
                }
            }
            foreach (string world in Worlds) foreach (var plan in AmbientSchedules.Get(world)) {
                Require(!ids.Contains(plan.id), "SUPPORTING_AND_RESIDENT_IDS_SEPARATE_" + plan.id);
                var actor = CharacterAppearanceProfiles.SpawnResident(plan, null, Vector3.zero);
                try {
                    Require(actor && actor.GetComponent<AmbientAppearance>().Ready && !actor.GetComponent<NativeCharacterRig>(), "RESIDENT_SURVIVES_MAIN_ASSET_REPLACEMENT_" + plan.id);
                    var skin = Skin(actor);
                    foreach (var face in faces) if (face.model == plan.model)
                        Require(Rms(face.skin, skin) > .0015f, "SUPPORTING_AND_RESIDENT_FACES_DISTINCT_" + face.id + "_" + plan.id);
                } finally { if (actor) UnityEngine.Object.DestroyImmediate(actor); }
            }
            Require(ids.Count == 6, "ALL_SIX_SUPPORTING_IDENTITIES");
            for (int i = 0; i < 2; i++)
                Require(CharacterAppearanceProfiles.CourierRecipientIdentity(false, i) == CharacterAppearanceProfiles.CourierRecipientIdentity(true, 1 - i), "RECIPIENT_KEEPS_FACE_AFTER_ADDRESS_MOVE_" + i);
            Require(CharacterAppearanceProfiles.CourierRecipientIdentity(false, 0) != CharacterAppearanceProfiles.CourierRecipientIdentity(false, 1), "SAME_NAME_RECIPIENTS_ARE_DIFFERENT_PEOPLE");
            Require(JsonUtility.ToJson(random) == JsonUtility.ToJson(UnityEngine.Random.state), "NO_GLOBAL_RANDOM_OR_SAVED_IDENTITY_STATE");
            Debug.Log("SUPPORTING_APPEARANCE_CHECKS_PASSED people=6 assertions=" + checks + " legacy base isolation; independent stable faces; geometry/body/UV/eye/source preservation");
        }

        [MenuItem("侨批/验收次要人物独立外貌")]
        public static void RenderReview()
        {
            Run();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/characters-v282/identity-review"));
            string[] args = Environment.GetCommandLineArgs(); int option = Array.IndexOf(args, "-qiaopi-supporting-appearance-output");
            if (option >= 0 && option + 1 < args.Length) output = Path.GetFullPath(args[option + 1]);
            Directory.CreateDirectory(output);
            var stage = new GameObject("Supporting face review — temporary"); stage.transform.position = new Vector3(1600, 1200, 1600);
            var actors = new List<GameObject>(); var lights = new List<GameObject>();
            var cameraRoot = new GameObject("Supporting face review camera"); var camera = cameraRoot.AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.nearClipPlane = .05f; camera.farClipPlane = 8;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .155f, .15f); camera.cullingMask = 1 << 30;
            var previousAmbient = RenderSettings.ambientMode; var previousSky = RenderSettings.ambientSkyColor;
            var previousEquator = RenderSettings.ambientEquatorColor; var previousGround = RenderSettings.ambientGroundColor;
            var texture = new RenderTexture(2520, 600, 24, RenderTextureFormat.ARGB32);
            try {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.54f, .56f, .59f); RenderSettings.ambientEquatorColor = new Color(.41f, .43f, .41f); RenderSettings.ambientGroundColor = new Color(.23f, .22f, .19f);
                Vector3 target = stage.transform.position + Vector3.up * 1.50f;
                lights.Add(LightAt(target + new Vector3(-3, 2, 4), target, new Color(1, .93f, .84f), 1.15f));
                lights.Add(LightAt(target + new Vector3(3, .1f, 3), target, new Color(.77f, .86f, 1), .50f));
                var manifest = new System.Text.StringBuilder("column,identity,displayName,baseModel,faceDescription,assetStage\n");
                for (int i = 0; i < CharacterAppearanceProfiles.Supporting.Length; i++) {
                    var profile = CharacterAppearanceProfiles.Supporting[i];
                    var actor = CharacterAppearanceProfiles.Spawn(profile.id, stage.transform, Vector3.zero); actors.Add(actor);
                    var face = actor.GetComponent<AmbientAppearance>();
                    actor.transform.localPosition = new Vector3((2.5f - i) * .40f, 1.53f - face.EyeCenter.y, 0);
                    foreach (Transform child in actor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                    manifest.Append(i + 1).Append(',').Append(profile.id).Append(',').Append(profile.title).Append(',').Append(profile.model).Append(',').Append(profile.face.description).Append(",legacy-independent-face\n");
                }
                texture.Create(); camera.targetTexture = texture; camera.aspect = (float)texture.width / texture.height; camera.orthographicSize = .30f;
                camera.transform.position = target + Vector3.forward * 3.5f; camera.transform.LookAt(target);
                camera.Render(); Save(texture, Path.Combine(output, "supporting-front.png"));
                foreach (var actor in actors) actor.transform.localRotation = Quaternion.Euler(0, -26, 0);
                camera.Render(); Save(texture, Path.Combine(output, "supporting-threequarter.png"));
                File.WriteAllText(Path.Combine(output, "supporting-face-columns.csv"), manifest.ToString());
                Debug.Log("SUPPORTING_APPEARANCE_REVIEW_OUTPUT=" + output);
            } finally {
                camera.targetTexture = null; texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
                RenderSettings.ambientMode = previousAmbient; RenderSettings.ambientSkyColor = previousSky; RenderSettings.ambientEquatorColor = previousEquator; RenderSettings.ambientGroundColor = previousGround;
                foreach (var light in lights) UnityEngine.Object.DestroyImmediate(light);
                UnityEngine.Object.DestroyImmediate(cameraRoot); UnityEngine.Object.DestroyImmediate(stage);
            }
        }

        static MeshFilter Head(GameObject actor)
        {
            Require(actor, "ACTOR_EXISTS");
            foreach (var filter in actor.GetComponentsInChildren<MeshFilter>(true))
                if (filter.name.StartsWith("Head_and_tailored_body_Mesh", StringComparison.Ordinal) && filter.sharedMesh && filter.sharedMesh.isReadable) return filter;
            throw new InvalidOperationException("SUPPORTING_HEAD_MISSING " + actor.name);
        }
        static HashSet<int> Indices(MeshFilter head, string shader)
        {
            var result = new HashSet<int>(); var materials = head.GetComponent<MeshRenderer>().sharedMaterials;
            for (int i = 0; i < Mathf.Min(head.sharedMesh.subMeshCount, materials.Length); i++)
                if (materials[i] && materials[i].shader && materials[i].shader.name == shader)
                    foreach (int vertex in head.sharedMesh.GetIndices(i)) result.Add(vertex);
            return result;
        }
        static Vector3[] Skin(GameObject actor)
        {
            var head = Head(actor); var indices = Indices(head, "Qiaopi/CharacterSkinTexture"); Require(indices.Count > 500, "ANATOMICAL_SKIN_REGION");
            var vertices = head.sharedMesh.vertices; var matrix = AmbientAppearance.MetricFromHead(actor.transform, head.transform);
            var result = new Vector3[indices.Count]; int at = 0;
            foreach (int vertex in indices) result[at++] = matrix.MultiplyPoint3x4(vertices[vertex]);
            return result;
        }
        static void CheckRigidEyes(GameObject source, GameObject actor)
        {
            var before = Head(source); var after = Head(actor); var original = before.sharedMesh.vertices; var vertices = after.sharedMesh.vertices;
            var matrix = AmbientAppearance.MetricFromHead(actor.transform, after.transform); var shifts = new Vector3[2]; var found = new bool[2];
            foreach (int vertex in Indices(before, "Qiaopi/CharacterEye")) {
                int side = matrix.MultiplyPoint3x4(original[vertex]).x < 0 ? 0 : 1;
                Vector3 shift = matrix.MultiplyVector(vertices[vertex] - original[vertex]);
                if (!found[side]) { shifts[side] = shift; found[side] = true; }
                Require(Vector3.Distance(shift, shifts[side]) < .000004f, "EYEBALL_SHAPE_REMAINS_RIGID");
            }
            Require(found[0] && found[1], "BOTH_EYES_FITTED");
        }
        static void CheckBodyMeshes(GameObject source, GameObject actor)
        {
            var before = source.GetComponentsInChildren<SkinnedMeshRenderer>(true); var after = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(before.Length == after.Length && before.Length > 0, "BODY_RENDERER_COUNT");
            for (int i = 0; i < before.Length; i++) Require(before[i].sharedMesh == after[i].sharedMesh, "BODY_SKIN_WEIGHTS_AND_BINDPOSE_UNTOUCHED");
        }
        static float Rms(Vector3[] a, Vector3[] b)
        {
            Require(a.Length == b.Length, "MATCHING_FACE_TOPOLOGY"); double total = 0;
            for (int i = 0; i < a.Length; i++) total += (a[i] - b[i]).sqrMagnitude;
            return Mathf.Sqrt((float)(total / a.Length));
        }
        static void Equal<T>(T[] a, T[] b, string label)
        { Require(a.Length == b.Length, label + "_COUNT"); for (int i = 0; i < a.Length; i++) Require(EqualityComparer<T>.Default.Equals(a[i], b[i]), label + "_" + i); }
        static GameObject LightAt(Vector3 position, Vector3 target, Color color, float intensity)
        {
            var root = new GameObject("Supporting face review lamp"); var light = root.AddComponent<Light>(); root.transform.position = position; root.transform.LookAt(target);
            light.type = LightType.Directional; light.color = color; light.intensity = intensity; light.cullingMask = 1 << 30; return root;
        }
        static void Save(RenderTexture render, string path)
        {
            RenderTexture previous = RenderTexture.active; var image = new Texture2D(render.width, render.height, TextureFormat.RGB24, false);
            try { RenderTexture.active = render; image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
        }
        static void Require(bool value, string label)
        { checks++; if (!value) throw new InvalidOperationException("SUPPORTING_APPEARANCE_CHECK_FAILED " + label); }
    }
}
