using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool HeroQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-hero-test") >= 0;
        bool FamilyFaceQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-family-face-test") >= 0;
        bool CharacterQA => FaceMotionQA || FamilyFaceQA || HeroQA || Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-character-test") >= 0;
        int characterQACount;
        static readonly string[] CharacterJoints = {"LeftArm", "RightArm", "LeftLeg", "RightLeg"};

        static void CharacterRequire(bool condition, string message)
        {
            if (!condition) throw new Exception("CHARACTER_QA_" + message);
        }

        // Flatten nested coroutines so a failed walking/capture check also exits with
        // an error code instead of leaving a QA game open indefinitely.
        IEnumerator RunCharacterQA()
        {
            var pending = new Stack<IEnumerator>();
            pending.Push(FaceMotionQA ? RunFaceMotionReview() : FamilyFaceQA ? RunFamilyFaceChecks() : HeroQA ? RunHeroChecks() : RunCharacterChecks());
            while (pending.Count > 0) {
                object wait = null;
                Exception failure = null;
                try {
                    if (!pending.Peek().MoveNext()) pending.Pop();
                    else wait = pending.Peek().Current;
                } catch (Exception error) { failure = error; }
                if (failure != null) {
                    Debug.LogError((FamilyFaceQA ? "FAMILY_FACE_QA_FAILED: " : HeroQA ? "HERO_QA_FAILED: " : "CHARACTER_QA_FAILED: ") + failure);
                    Application.Quit(1);
                    yield break;
                }
                var nested = wait as IEnumerator;
                if (nested != null) pending.Push(nested);
                else if (pending.Count > 0) yield return wait;
            }
            Debug.Log(FaceMotionQA ? "FACE_MOTION_QA_ALL_PASSED: actual production portraits, legacy expression captures and native static-face capture; unchanged saves, muted." : FamilyFaceQA
                ? "FAMILY_FACE_QA_ALL_PASSED: Mother, Wife and Aman imported identities, metric bodies, materials and weighted limb deformations; close front/profile renders and equal-head-size single-camera comparison; actual home/overseas family placement and wife correspondence; saves and preferences unchanged, muted."
                : HeroQA
                ? "HERO_QA_ALL_PASSED: production Wensheng, continuous weighted skin and moving joints; real walking, luggage pickup, hero dialogue portrait, parcel and cargo delivery, physical writing desk; muted, real save and preferences unchanged. Other cast and authored first-person hands are outside this test."
                : "CHARACTER_QA_ALL_PASSED: " + characterQACount + " distinct models including Wife; metric bounds, continuous weighted skin and moving joints; " + characterQACount + " live portrait PNGs; three world conversations; first-person cargo pickup, walking and delivery; saved progress and audio preferences unchanged.");
            Application.Quit(0);
        }

        IEnumerator RunFamilyFaceChecks()
        {
            CharacterRequire(QA && DetailQA && FamilyFaceQA, "FAMILY_FACE_ISOLATION_ENABLED");
            string[] saveFiles = {savePath, savePath + ".bak", savePath + ".tmp"};
            var originalSaves = new string[saveFiles.Length];
            for (int i = 0; i < saveFiles.Length; i++) originalSaves[i] = CharacterFileSnapshot(saveFiles[i]);
            string[] prefKeys = {"qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized"};
            var preferences = new int[prefKeys.Length];
            for (int i = 0; i < prefKeys.Length; i++) preferences[i] = PlayerPrefs.GetInt(prefKeys[i], -1);
            float musicLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false); voiceSource.Stop(); sfx.Stop();
            dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = lifePanel = false;
            string reviewName = "FamilyFaceReview-" + Application.version + (MobileControls ? "-mobile" : "-desktop");
            string reviewPath = Path.Combine(Application.persistentDataPath, reviewName); Directory.CreateDirectory(reviewPath);
            Debug.Log("FAMILY_FACE_QA_OUTPUT=" + reviewPath);

            string[] ids = {"mother", "wife", "aman"};
            var reviewActors = new GameObject[ids.Length]; var cameras = new Camera[ids.Length]; var centers = new Vector3[ids.Length];
            var halfHeights = new float[ids.Length];
            var single = new RenderTexture(650, 800, 24, RenderTextureFormat.ARGB32);
            var strip = new RenderTexture(1950, 800, 24, RenderTextureFormat.ARGB32);
            try {
                single.Create(); strip.Create();
                for (int i = 0; i < ids.Length; i++) {
                    CharacterInfo person = CharacterRoster.Get(ids[i]); ShowPortrait(ids[i]); yield return null;
                    CharacterRequire(portraitActor && portraitId == ids[i] && IsCharacterModel(portraitActor.transform, person.model), "FAMILY_FACE_RESOURCE_" + person.model);
                    CheckCharacterGeometry(portraitActor, person.model); CheckContinuousCharacterSkin(portraitActor, person.model);
                    foreach (string jointName in CharacterJoints)
                        CheckSkinnedCharacterJoint(portraitActor, ModelLibrary.Find(portraitActor.transform, jointName), person.model);
                    var shaders = new HashSet<string>();
                    foreach (Renderer renderer in portraitActor.GetComponentsInChildren<Renderer>())
                        foreach (Material material in renderer.sharedMaterials) {
                            shaders.Add(material.shader.name);
                            if (material.name.StartsWith("QP2_SkinTexture_") || material.name.StartsWith("QP2_EyeTexture"))
                                CharacterRequire(material.mainTexture, "FAMILY_FACE_TEXTURE_" + person.model + "_" + material.name);
                        }
                    if (portraitActor.GetComponent<NativeCharacterRig>()) {
                        CharacterRequire(shaders.Contains("Qiaopi/MeshyCharacter"), "FAMILY_NATIVE_MATERIAL_" + person.model);
                        foreach (Renderer renderer in portraitActor.GetComponentsInChildren<Renderer>())
                            foreach (Material material in renderer.sharedMaterials)
                                foreach (string property in new[] { "_MainTex", "_BumpMap", "_MetalRough" })
                                    CharacterRequire(material.HasProperty(property) && material.GetTexture(property), "FAMILY_NATIVE_TEXTURE_" + person.model + property);
                    } else
                        CharacterRequire(shaders.Contains("Qiaopi/CharacterSkinTexture") && shaders.Contains("Qiaopi/CharacterEye"), "FAMILY_FACE_SHADERS_" + person.model);

                    // Keep the production portrait actor intact. Each review copy
                    // shares its actual FBX meshes/materials and the real portrait
                    // lights; separation prevents one camera seeing another model.
                    reviewActors[i] = Instantiate(portraitActor, portraitStage.transform, false);
                    reviewActors[i].name = "Face review " + person.model;
                    reviewActors[i].transform.localPosition = Vector3.right * ((i + 1) * 8f);
                    reviewActors[i].transform.localRotation = Quaternion.identity;
                    foreach (CharacterIdle idle in reviewActors[i].GetComponents<CharacterIdle>()) idle.enabled = false;
                    float halfHeight;
                    FamilyFaceFrame(reviewActors[i], person.model, out centers[i], out halfHeight);
                    halfHeights[i] = halfHeight;
                    var cameraObject = new GameObject("Face review camera " + person.model);
                    cameras[i] = cameraObject.AddComponent<Camera>(); cameras[i].CopyFrom(portraitCamera); cameras[i].enabled = false;
                    cameras[i].orthographic = true; cameras[i].orthographicSize = halfHeight; cameras[i].aspect = 650f / 800f;
                    cameras[i].farClipPlane = 6; // Exclude the neighbouring actor behind a side-view camera.
                    cameras[i].transform.position = centers[i] + Vector3.forward * 3.2f; cameras[i].transform.LookAt(centers[i]);
                    cameras[i].targetTexture = single; cameras[i].rect = new Rect(0, 0, 1, 1); cameras[i].Render();
                    SaveFamilyReviewTexture(single, Path.Combine(reviewPath, person.model + "_face_front.png"));
                    Debug.Log("FAMILY_FACE_QA_MODEL_PASSED " + person.model + " frameCenter=" + centers[i] + " halfHeight=" + halfHeight.ToString("F4"));
                }
                // Finish all single-character captures before moving the review
                // copies into the shared comparison scene.
                for (int i = 0; i < cameras.Length; i++) {
                    cameras[i].targetTexture = single; cameras[i].rect = new Rect(0, 0, 1, 1); cameras[i].aspect = 650f / 800f;
                    cameras[i].clearFlags = CameraClearFlags.SolidColor;
                    cameras[i].transform.position = centers[i] + Vector3.right * 3.2f; cameras[i].transform.LookAt(centers[i]); cameras[i].Render();
                    SaveFamilyReviewTexture(single, Path.Combine(reviewPath, CharacterRoster.Get(ids[i]).model + "_face_profile.png"));
                }
                // One camera renders three physical copies side by side. Camera
                // rect is not used for subviews of a targetTexture: Unity can
                // render each pass over the full target, stretching/overwriting
                // earlier faces. No screenshot stitching or image editing is used.
                Camera boardCamera = cameras[0];
                Vector3 boardCenter = centers[1]; // The copy at stage-local x=16.
                const float boardHalfHeight = .24f;
                boardCamera.targetTexture = strip; boardCamera.rect = new Rect(0, 0, 1, 1);
                boardCamera.aspect = (float)strip.width / strip.height;
                boardCamera.orthographicSize = boardHalfHeight;
                boardCamera.clearFlags = CameraClearFlags.SolidColor;
                boardCamera.transform.position = boardCenter + Vector3.forward * 3.2f;
                boardCamera.transform.LookAt(boardCenter);
                float columnWidth = 2f * boardHalfHeight * boardCamera.aspect / ids.Length;
                for (int i = 0; i < reviewActors.Length; i++) {
                    Transform actor = reviewActors[i].transform;
                    Vector3 localFaceCenter = actor.InverseTransformPoint(centers[i]);
                    CharacterRequire(halfHeights[i] > .01f, "FAMILY_FACE_COMPARISON_SIZE_" + ids[i]);
                    float scale = boardHalfHeight / halfHeights[i];
                    actor.localScale *= scale;
                    // Camera right is world -X when looking toward these faces;
                    // placing along it preserves Mother/Wife/Aman image order.
                    Vector3 targetCenter = boardCenter + boardCamera.transform.right * ((i - 1f) * columnWidth);
                    actor.position += targetCenter - actor.TransformPoint(localFaceCenter);
                    centers[i] = actor.TransformPoint(localFaceCenter);
                    Vector3 viewport = boardCamera.WorldToViewportPoint(centers[i]);
                    CharacterRequire(Mathf.Abs(viewport.x - (i + .5f) / ids.Length) < .001f &&
                        Mathf.Abs(viewport.y - .5f) < .001f && viewport.z > boardCamera.nearClipPlane && viewport.z < boardCamera.farClipPlane,
                        "FAMILY_FACE_COMPARISON_ALIGNMENT_" + ids[i]);
                    Debug.Log("FAMILY_FACE_QA_BOARD_ACTOR " + ids[i] + " scale=" + scale.ToString("F4") + " viewport=" + viewport);
                }
                yield return null; // Update the moved/scaled continuous skins.
                boardCamera.Render();
                SaveFamilyReviewTexture(strip, Path.Combine(reviewPath, "family_faces_front.png"));
                Debug.Log("FAMILY_FACE_QA_COMPARISON columns=Mother,Wife,Aman resolution=1950x800 method=single-camera-physical-lineup aspect=" + boardCamera.aspect.ToString("F4"));
            } finally {
                foreach (Camera camera in cameras) if (camera) { camera.targetTexture = null; Destroy(camera.gameObject); }
                foreach (GameObject actor in reviewActors) if (actor) { actor.SetActive(false); Destroy(actor); }
                single.Release(); strip.Release(); Destroy(single); Destroy(strip);
            }
            yield return CheckWorldFamilyForQA(reviewName);
            Save();
            for (int i = 0; i < saveFiles.Length; i++) CharacterRequire(originalSaves[i] == CharacterFileSnapshot(saveFiles[i]), "FAMILY_FACE_REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < prefKeys.Length; i++) CharacterRequire(PlayerPrefs.GetInt(prefKeys[i], -1) == preferences[i], "FAMILY_FACE_PREFERENCE_UNCHANGED_" + prefKeys[i]);
            CharacterRequire(PlayerPrefs.GetFloat("qiaopi-music-level", -1) == musicLevel, "FAMILY_FACE_MUSIC_LEVEL_UNCHANGED");
            CharacterRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "FAMILY_FACE_ALL_AUDIO_MUTED");
        }

        static void FamilyFaceFrame(GameObject actor, string modelName, out Vector3 center, out float halfHeight)
        {
            float height = ModelLibrary.CharacterHeight(modelName);
            center = actor.transform.position + Vector3.up * (height * .87f); halfHeight = .235f;
            Bounds skinBounds = new Bounds(); bool first = true;
            foreach (MeshFilter filter in actor.GetComponentsInChildren<MeshFilter>()) {
                var renderer = filter.GetComponent<MeshRenderer>(); Mesh mesh = filter.sharedMesh;
                if (!renderer || !mesh || !mesh.isReadable) continue;
                Material[] materials = renderer.sharedMaterials; Vector3[] points = mesh.vertices;
                for (int submesh = 0; submesh < Mathf.Min(mesh.subMeshCount, materials.Length); submesh++) {
                    if (!materials[submesh] || !materials[submesh].name.StartsWith("QP2_SkinTexture_")) continue;
                    foreach (int index in mesh.GetIndices(submesh)) {
                        Vector3 point = filter.transform.TransformPoint(points[index]);
                        if (first) { skinBounds = new Bounds(point, Vector3.zero); first = false; } else skinBounds.Encapsulate(point);
                    }
                }
            }
            string source = "height-fallback";
            if (!first && skinBounds.size.y > .14f && skinBounds.size.y < height * .42f) {
                center = skinBounds.center;
                // Head/neck occupies the same fraction of each image. The extra
                // margin keeps ears, hairline and chin inside front and side views.
                halfHeight = Mathf.Max(skinBounds.size.y * .64f, Mathf.Max(skinBounds.size.x, skinBounds.size.z) / (650f / 800f) * .62f);
                source = "static-skin-submesh";
            }
            Transform faceCenter = ModelLibrary.Find(actor.transform, "QA_FaceCenter");
            Transform faceTop = ModelLibrary.Find(actor.transform, "QA_FaceTop"), faceChin = ModelLibrary.Find(actor.transform, "QA_FaceChin");
            if (faceCenter) { center = faceCenter.position; source = "QA_FaceCenter"; }
            if (faceTop && faceChin) {
                if (!faceCenter) center = (faceTop.position + faceChin.position) * .5f;
                halfHeight = Mathf.Max(halfHeight, Vector3.Distance(faceTop.position, faceChin.position) * .68f); source += "+markers";
            }
            Debug.Log("FAMILY_FACE_QA_FRAME " + modelName + " source=" + source + " skinHeight=" + (first ? 0 : skinBounds.size.y).ToString("F4"));
        }

        static void SaveFamilyReviewTexture(RenderTexture texture, string path)
        {
            RenderTexture previous = RenderTexture.active; Texture2D image = null;
            try {
                RenderTexture.active = texture;
                image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply();
                byte[] png = image.EncodeToPNG(); CharacterRequire(png != null && png.Length > 3000, "FAMILY_FACE_RENDER_PIXELS"); File.WriteAllBytes(path, png);
            } finally { RenderTexture.active = previous; if (image) Destroy(image); }
        }

        IEnumerator RunCharacterChecks()
        {
            CharacterRequire(QA && DetailQA, "ISOLATION_ENABLED");
            string originalSave = File.Exists(savePath) ? Convert.ToBase64String(File.ReadAllBytes(savePath)) : null;
            string[] prefKeys = {"qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled"};
            int[] preferences = new int[prefKeys.Length];
            for (int i = 0; i < prefKeys.Length; i++) preferences[i] = PlayerPrefs.GetInt(prefKeys[i], -1);
            AudioListener.pause = true;
            soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false);
            musicDirector.SetEnabled(false, false);
            voiceSource.Stop(); sfx.Stop();
            string reviewName = "CharacterReview-" + Application.version + (MobileControls ? "-mobile" : "-desktop");
            string reviewPath = Path.Combine(Application.persistentDataPath, reviewName);
            Directory.CreateDirectory(reviewPath);
            Debug.Log("CHARACTER_QA_OUTPUT=" + reviewPath);
            yield return new WaitForSeconds(.3f);

            CharacterRequire(heldHands, "FIRST_PERSON_HANDS_PRESENT");
            bool authoredHands = heldHands.transform.Find("BlenderModel");
            CharacterRequire(!Resources.Load<GameObject>("Models/FirstPersonHands") || authoredHands, "AVAILABLE_AUTHORED_HANDS_LOADED");
            CharacterRequire(heldHands.transform.parent == heldRig.transform && heldHands.transform.localPosition == Vector3.zero &&
                heldHands.transform.localScale == Vector3.one, "FIRST_PERSON_HANDS_METRIC_POSE");
            int handVertices = 0;
            foreach (MeshFilter mesh in heldHands.GetComponentsInChildren<MeshFilter>(true))
                if (mesh.sharedMesh) handVertices += mesh.sharedMesh.vertexCount;
            foreach (SkinnedMeshRenderer mesh in heldHands.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (mesh.sharedMesh) handVertices += mesh.sharedMesh.vertexCount;
            CharacterRequire(handVertices > 0, "FIRST_PERSON_HANDS_HAVE_MESH");
            foreach (Renderer renderer in heldHands.GetComponentsInChildren<Renderer>(true))
                CharacterRequire(renderer.gameObject.layer == HeldItemLayer && renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off,
                    "FIRST_PERSON_HANDS_RENDER_LAYER");
            Debug.Log("CHARACTER_QA_HANDS_PASSED imported=" + authoredHands + " vertices=" + handVertices);

            dialogue = journal = map = help = puzzle = fieldNote = gallery = lifePanel = false;
            var checkedModels = new HashSet<string>();
            var checkedIds = new HashSet<string>();
            var people = CharacterRoster.All().FindAll(person => person.id != "narrator");
            characterQACount = people.Count;
            CharacterRequire(characterQACount > 0, "ROSTER_NOT_EMPTY");
            int index = 0;
            foreach (CharacterInfo person in people) {
                CharacterRequire(checkedIds.Add(person.id), "DUPLICATE_SPEAKER_" + person.id);
                CharacterRequire(checkedModels.Add(person.model), "DUPLICATE_IDENTITY_" + person.id);
                ShowPortrait(person.id);
                yield return null; // Let the imported actor and its idle component initialize.
                CharacterRequire(portraitActor && portraitId == person.id, "PORTRAIT_LOAD_" + person.id);
                CheckCharacterGeometry(portraitActor, person.model);
                CheckContinuousCharacterSkin(portraitActor, person.model);
                foreach (string jointName in CharacterJoints) CheckCharacterJoint(portraitActor, jointName, person.model);
                portraitActor.transform.localRotation = Quaternion.identity;
                portraitCamera.Render();
                string path = Path.Combine(reviewPath, (++index).ToString("D2") + "_" + person.model + ".png");
                SaveCharacterPortrait(path);
                Debug.Log("CHARACTER_QA_MODEL_PASSED " + person.model + " height=" + ModelLibrary.CharacterHeight(person.model).ToString("F2") + " portrait=" + path);
            }
            CharacterRequire(index == characterQACount && checkedIds.Contains("wife") && checkedModels.Contains("Wife"), "COMPLETE_ROSTER_WITH_WIFE");
            Debug.Log("CHARACTER_QA_ROSTER_PASSED count=" + index + " independentModels=" + checkedModels.Count + " portraits=" + index);
            yield return CheckWorldFamilyForQA(reviewName);

            // These scenes exercise a family, an overseas peer and an older mentor
            // in actual world lighting and the normal dialogue UI.
            foreach (string node in new[] {"home", "first_pay", "courier"}) {
                state = StoryEngine.NewGame(20261007); state.nodeId = node;
                // Use the supported migrated story for this pre-livelihood
                // conversation, rather than forging an invalid completed run.
                if (node == "first_pay") { state.journeyVersion = 0; state.journey = null; LifeJourney.Ensure(state); }
                CharacterRequire(StoryEngine.Validate(state) && !LifeJourney.IsActive(state), "WORLD_STORY_FIXTURE_" + node);
                dialogue = journal = map = help = puzzle = fieldNote = gallery = lifePanel = false;
                EnterNode(true); ResetView();
                progress = mission.required; // Only skip plot chores for this isolated visual review.
                RefreshProps();
                yield return WalkFirstPerson(mission.npcPosition, "character conversation " + node);
                FaceConversationPartner(); firstPersonFov = 48; UpdateCamera(true);
                toastUntil = 0;
                yield return CaptureWorld(reviewName + "/world_" + loadedWorld);
                Interact();
                CharacterRequire(dialogue, "WORLD_DIALOGUE_OPEN_" + node);
                EnsureConversation(); UpdateConversation();
                yield return new WaitForSeconds(.25f);
                CharacterRequire(portraitActor && portraitId == CurrentLine.speakerId, "WORLD_SPEAKER_" + node);
                CharacterRequire(!heldCamera.enabled, "HANDS_HIDDEN_IN_DIALOGUE_" + node);
                yield return CaptureWorld(reviewName + "/dialogue_" + loadedWorld);
                if (node == "home") {
                    while (CurrentLine != null && CurrentLine.speakerId != "wife" && !LastDialogueLine) {
                        NextDialogueLine(); UpdateConversation(); yield return null;
                    }
                    CharacterRequire(CurrentLine != null && CurrentLine.speakerId == "wife" && portraitId == "wife", "WIFE_IN_FAMILY_DIALOGUE");
                    yield return CaptureWorld(reviewName + "/dialogue_wife");
                }
                Debug.Log("CHARACTER_QA_WORLD_PASSED " + loadedWorld + " speaker=" + portraitId);
                dialogue = false;
            }

            // Real interaction and collision-aware walking, including the animated
            // hidden body and visible first-person hands; no teleport or direct reward.
            state = StoryEngine.NewGame(20261007); state.nodeId = "dock";
            dialogue = journal = map = help = puzzle = fieldNote = gallery = lifePanel = false;
            EnterNode(true); ResetView();
            yield return WalkFirstPerson(mission.itemPosition, "character cargo pickup");
            Interact(); yield return null;
            CharacterRequire(carrying && heldRig.activeSelf && heldBox.activeSelf, "CARGO_PICKUP_AND_HANDS");
            CheckHeroCarryPose();
            cameraPitch = 18; UpdateCamera(true); toastUntil = 0;
            yield return CaptureWorld(reviewName + "/first_person_cargo");
            yield return WalkFirstPerson(mission.destination, "character cargo delivery");
            Interact(); yield return null;
            CharacterRequire(!carrying && progress == 1 && !heldRig.activeSelf, "CARGO_DELIVERY");
            RequireFirstPerson("character cargo complete");

            Save(); // Confirm the QA guard does not create or alter the real save.
            string finalSave = File.Exists(savePath) ? Convert.ToBase64String(File.ReadAllBytes(savePath)) : null;
            CharacterRequire(originalSave == finalSave, "REAL_SAVE_UNCHANGED");
            for (int i = 0; i < prefKeys.Length; i++)
                CharacterRequire(PlayerPrefs.GetInt(prefKeys[i], -1) == preferences[i], "AUDIO_PREFERENCE_UNCHANGED_" + prefKeys[i]);
            CharacterRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "ALL_AUDIO_MUTED");
        }

        IEnumerator CheckWorldFamilyForQA(string reviewName)
        {
            foreach (string node in new[] {"peace", "pressure", "home", "farewell", "return_choice", "end_home_lamp"}) {
                EnterHeroQANode(node); yield return null;
                CharacterRequire(loadedWorld == "quanzhou", "FAMILY_HOME_WORLD_" + node);
                foreach (string modelName in new[] {"Mother", "Aman", "Wife"})
                    CharacterRequire(CountWorldCharacterModel(modelName) == 1, "ONE_FAMILY_ACTOR_" + node + "_" + modelName);
                string primaryModel = CharacterRoster.Get(CharacterRoster.ForNode(node)).model;
                CharacterRequire(npc && npc.transform.parent == actors.transform && IsCharacterModel(npc.transform, primaryModel) &&
                    CountWorldCharacterModel(primaryModel) == 1, "PRIMARY_NPC_NOT_DUPLICATED_" + node);
                if (node == "home") SaveWorldFamilyForQA(Path.Combine(Application.persistentDataPath, reviewName, "family_home.png"));
                Debug.Log("CHARACTER_QA_FAMILY_HOME_PASSED " + node + " Mother=1 Aman=1 Wife=1 primary=" + primaryModel);
            }
            foreach (string node in new[] {"reply", "trace_family", "shore", "courier", "end_school_window"}) {
                EnterHeroQANode(node); yield return null;
                CharacterRequire(loadedWorld != "quanzhou", "FAMILY_OVERSEAS_WORLD_" + node);
                foreach (string modelName in new[] {"Mother", "Aman", "Wife"})
                    CharacterRequire(CountWorldCharacterModel(modelName) == 0, "NO_OVERSEAS_FAMILY_ACTOR_" + node + "_" + modelName);
                if (node == "reply" || node == "trace_family") {
                    dialogue = true; EnsureConversation(); UpdateConversation();
                    while (CurrentLine != null && CurrentLine.speakerId != "wife" && !LastDialogueLine) {
                        NextDialogueLine(); UpdateConversation(); yield return null;
                    }
                    CharacterRequire(CurrentLine != null && CurrentLine.speakerId == "wife" && portraitId == "wife" &&
                        portraitActor && IsCharacterModel(portraitActor.transform, "Wife") && CountWorldCharacterModel("Wife") == 0,
                        "OVERSEAS_WIFE_IS_CORRESPONDENT_" + node);
                    if (node == "reply") yield return CaptureWorld(reviewName + "/overseas_wife_letter_portrait");
                    dialogue = false;
                }
                Debug.Log("CHARACTER_QA_FAMILY_OVERSEAS_PASSED " + node + " localFamilyActors=0");
            }
            CharacterRequire(CharacterRoster.Get("wife").model == "Wife" && PersonalLetters.RecipientName("wife") == "妻子", "WIFE_SPEAKER_RECIPIENT_MAPPING");
            var draft = PersonalLetters.EnsureDraft(state); draft.recipient = "wife"; draft.body = "给妻子：\n今天的日常，也想慢慢说给你听。";
            string result;
            CharacterRequire(PersonalLetters.TrySend(state, draft.token, out result), "WIFE_CORRESPONDENCE_CAN_SEND");
            var outgoing = state.letters[state.letters.Count - 2]; var reply = state.letters[state.letters.Count - 1];
            CharacterRequire(outgoing.title.StartsWith("写给妻子") && outgoing.body == draft.body && reply.title.StartsWith("妻子的回批") &&
                reply.signature == "妻子" && reply.incoming && reply.body.StartsWith("文生："), "WIFE_CORRESPONDENCE_ATTRIBUTION");
            Debug.Log("CHARACTER_QA_FAMILY_ALL_PASSED homeNodes=6 overseasNodes=5 wifePortrait=true wifeRecipient=true");
        }

        void SaveWorldFamilyForQA(string path)
        {
            Bounds family = new Bounds(); bool first = true;
            foreach (Transform actor in actors.transform) {
                if (!IsCharacterModel(actor, "Mother") && !IsCharacterModel(actor, "Aman") && !IsCharacterModel(actor, "Wife")) continue;
                Bounds bounds = ModelLibrary.WorldBounds(actor);
                if (first) { family = bounds; first = false; } else family.Encapsulate(bounds);
            }
            CharacterRequire(!first, "FAMILY_GROUP_CAPTURE_ACTORS");
            // Photograph the actual three actors in the actual Quanzhou scene.
            // A disabled temporary camera renders once; the playable camera,
            // position, controls and shadow-only player body are left intact.
            Vector3 originalPosition = cam.transform.position; Quaternion originalRotation = cam.transform.rotation;
            var cameraObject = new GameObject("Family QA group camera");
            Camera camera = cameraObject.AddComponent<Camera>(); camera.CopyFrom(cam); camera.enabled = false;
            var target = new RenderTexture(1440, 900, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active; Texture2D image = null;
            try {
                camera.fieldOfView = 38; camera.aspect = 1440f / 900f; camera.nearClipPlane = .05f; camera.farClipPlane = 120;
                camera.cullingMask = ~((1 << HeldItemLayer) | (1 << PortraitLayer));
                float distance = Mathf.Max(4.1f, family.size.x * 1.45f);
                camera.transform.position = family.center + Vector3.back * distance + Vector3.up * .65f;
                camera.transform.LookAt(family.center);
                target.Create(); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                byte[] png = image.EncodeToPNG(); CharacterRequire(png != null && png.Length > 3000, "FAMILY_GROUP_CAPTURE_PIXELS");
                File.WriteAllBytes(path, png); Debug.Log("CHARACTER_QA_FAMILY_GROUP_PNG=" + path);
            } finally {
                RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(cameraObject); if (image) Destroy(image);
            }
            CharacterRequire(cam.transform.position == originalPosition && cam.transform.rotation == originalRotation, "FAMILY_CAPTURE_PRESERVES_PLAY_CAMERA");
        }

        int CountWorldCharacterModel(string modelName)
        {
            int count = 0;
            foreach (Transform actor in actors.transform)
                if (actor.gameObject.activeInHierarchy && IsCharacterModel(actor, modelName)) count++;
            return count;
        }

        static bool IsCharacterModel(Transform actor, string modelName)
        {
            var identity = actor.GetComponent<CharacterModelIdentity>();
            if (identity) return identity.modelId == modelName;
            // Display names can name the whole family. The instantiated FBX
            // shares its imported mesh asset with precisely one roster resource.
            Transform visual = actor.Find("BlenderModel");
            var prefab = Resources.Load<GameObject>("Models/" + modelName);
            if (!visual || !prefab) return false;
            var reference = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (reference && reference.sharedMesh) {
                foreach (var skinRenderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (skinRenderer.sharedMesh == reference.sharedMesh) return true;
                return false;
            }
            var staticReference = prefab.GetComponentInChildren<MeshFilter>(true);
            if (!staticReference || !staticReference.sharedMesh) return false;
            foreach (var mesh in visual.GetComponentsInChildren<MeshFilter>(true))
                if (mesh.sharedMesh == staticReference.sharedMesh) return true;
            return false;
        }

        // Integration gate for replacing Wensheng alone. The rest of the cast and
        // the optional authored first-person hands deliberately retain their own
        // release schedule; the existing full-cast gate remains available above.
        IEnumerator RunHeroChecks()
        {
            CharacterRequire(QA && DetailQA && HeroQA, "HERO_ISOLATION_ENABLED");
            string[] saveFiles = {savePath, savePath + ".bak", savePath + ".tmp"};
            var originalSaves = new string[saveFiles.Length];
            for (int i = 0; i < saveFiles.Length; i++) originalSaves[i] = CharacterFileSnapshot(saveFiles[i]);
            string[] prefKeys = {"qiaopi-sound-enabled", "qiaopi-music-enabled", "qiaopi-voice-enabled", "qiaopi-mobile-initialized"};
            var preferences = new int[prefKeys.Length];
            for (int i = 0; i < prefKeys.Length; i++) preferences[i] = PlayerPrefs.GetInt(prefKeys[i], -1);
            float musicLevel = PlayerPrefs.GetFloat("qiaopi-music-level", -1);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false); voiceSource.Stop(); sfx.Stop();
            string reviewName = "HeroReview-" + Application.version + (MobileControls ? "-mobile" : "-desktop");
            string reviewPath = Path.Combine(Application.persistentDataPath, reviewName);
            Directory.CreateDirectory(reviewPath);
            Debug.Log("HERO_QA_OUTPUT=" + reviewPath);

            EnterHeroQANode("home");
            yield return null;
            AnimateWalk(0);
            CharacterRequire(model.name == "Wensheng", "PRODUCTION_WENSHENG_RESOURCE");
            CharacterRequire(CharacterRoster.Get("wensheng").model == "Wensheng", "HERO_ROSTER_RESOURCE");
            foreach (CharacterInfo person in CharacterRoster.All())
                if (person.id != "wensheng" && person.id != "narrator")
                    CharacterRequire(person.model != "Wensheng", "NPC_IDENTITY_NOT_REPLACED_" + person.id);
            CheckCharacterGeometry(model, "Wensheng"); CheckHeroSkin(model);
            foreach (string name in CharacterJoints) CheckSkinnedCharacterJoint(model, ModelLibrary.Find(model.transform, name), "Wensheng");
            RequireFirstPerson("hero entry");
            SaveHeroBody(Path.Combine(reviewPath, "hero_body.png"));
            Debug.Log("HERO_QA_RIG_PASSED height=" + ModelLibrary.WorldBounds(model.transform).size.y.ToString("F3"));

            yield return WalkHeroForQA(mission.itemPosition, "luggage", reviewPath);
            Interact(); yield return null;
            CharacterRequire(Complete && !carrying, "LUGGAGE_STOWED");
            yield return WalkHeroForQA(mission.npcPosition, "mother", reviewPath);
            Interact(); CharacterRequire(dialogue, "HOME_DIALOGUE");
            EnsureConversation();
            while (!LastDialogueLine) { NextDialogueLine(); UpdateConversation(); yield return null; }
            CharacterRequire(ConversationVisible && LastDialogueLine && StoryEngine.Validate(state), "HERO_READY_FOR_STORY_CHOICE");
            ChoiceData ledger = StoryEngine.GetScene(state).choices.Find(choice => choice.id == "ledger");
            CharacterRequire(ledger != null && ledger.enabled, "HERO_LEDGER_CHOICE_AVAILABLE");
            Choose("ledger"); CharacterRequire(state.awaitingContinue, "HERO_STORY_CHOICE");
            EnsureConversation(); UpdateConversation(); yield return null;
            CharacterRequire(CurrentLine != null && CurrentLine.speakerId == "wensheng" && portraitId == "wensheng" && portraitActor,
                "ACTUAL_HERO_SPEAKING_PORTRAIT");
            CheckCharacterGeometry(portraitActor, "Wensheng"); CheckHeroSkin(portraitActor);
            CharacterRequire(!heldCamera.enabled, "HANDS_HIDDEN_DURING_HERO_DIALOGUE");
            portraitCamera.Render(); SaveCharacterPortrait(Path.Combine(reviewPath, "hero_portrait.png"));
            yield return CaptureWorld(reviewName + "/hero_dialogue");
            Debug.Log("HERO_QA_DIALOGUE_PASSED speaker=" + portraitId);

            EnterHeroQANode("courier"); yield return null;
            yield return WalkHeroForQA(mission.itemPosition, "parcel_pickup", reviewPath);
            Interact(); yield return null;
            CharacterRequire(carrying && heldRig.activeSelf && heldLetter.activeSelf && !heldBox.activeSelf, "PARCEL_VISIBLE_IN_FIRST_PERSON");
            CheckHeroCarryPose();
            cameraPitch = 18; UpdateCamera(true); toastUntil = 0;
            yield return CaptureWorld(reviewName + "/hero_first_person_parcel");
            yield return WalkHeroForQA(mission.destination, "parcel_delivery", reviewPath);
            Interact(); yield return null;
            CharacterRequire(Complete && !carrying && !heldRig.activeSelf, "PARCEL_DELIVERED");

            PersonalLetters.EnsureDraft(state).body = "母亲：\n今日在批局桌前写信，家中一切可好？";
            string draft = state.personalLetterDraft.body;
            int money = state.money, health = state.health, taskProgress = progress, turns = state.journey.turns;
            yield return WalkToWritingDeskForQA();
            CharacterRequire(state.personalLetterDraft.body == draft && state.money == money && state.health == health &&
                progress == taskProgress && state.journey.turns == turns, "WRITING_PRESERVES_DRAFT_AND_PROGRESS");
            yield return CaptureWorld(reviewName + "/hero_writing_desk");
            Vector3 feet = player.transform.position; ClosePersonalLetter();
            CharacterRequire(!letterEditor && !writingSeated && Vector3.Distance(feet, player.transform.position) < .001f,
                "WRITING_STANDS_WITHOUT_TELEPORT");
            RequireFirstPerson("hero after writing"); Debug.Log("HERO_QA_WRITING_PASSED");

            EnterHeroQANode("dock"); yield return null;
            yield return WalkHeroForQA(mission.itemPosition, "cargo_pickup", reviewPath);
            Interact(); yield return null;
            CharacterRequire(carrying && heldRig.activeSelf && heldBox.activeSelf && !heldLetter.activeSelf, "CARGO_VISIBLE_IN_FIRST_PERSON");
            CheckHeroCarryPose(); SaveHeroBody(Path.Combine(reviewPath, "hero_carry_body.png"));
            cameraPitch = 18; UpdateCamera(true); toastUntil = 0;
            yield return CaptureWorld(reviewName + "/hero_first_person_cargo");
            yield return WalkHeroForQA(mission.destination, "cargo_delivery", reviewPath);
            Interact(); yield return null;
            CharacterRequire(!carrying && progress == 1 && !heldRig.activeSelf, "CARGO_DELIVERED");
            RequireFirstPerson("hero cargo complete"); Debug.Log("HERO_QA_CARRY_PASSED parcel=true cargo=true");

            Save();
            for (int i = 0; i < saveFiles.Length; i++) CharacterRequire(originalSaves[i] == CharacterFileSnapshot(saveFiles[i]), "REAL_SAVE_UNCHANGED_" + i);
            for (int i = 0; i < prefKeys.Length; i++) CharacterRequire(PlayerPrefs.GetInt(prefKeys[i], -1) == preferences[i], "PREFERENCE_UNCHANGED_" + prefKeys[i]);
            CharacterRequire(PlayerPrefs.GetFloat("qiaopi-music-level", -1) == musicLevel, "MUSIC_LEVEL_UNCHANGED");
            CharacterRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled && musicDirector.AudibleVolume <= .001f, "HERO_ALL_AUDIO_MUTED");
            Debug.Log("HERO_QA_ISOLATION_PASSED save=true backup=true preferences=true muted=true");
        }

        static string CharacterFileSnapshot(string path) => File.Exists(path) ? Convert.ToBase64String(File.ReadAllBytes(path)) : null;

        void EnterHeroQANode(string node)
        {
            state = StoryEngine.NewGame(20261007); state.nodeId = node;
            // These nodes are outside the first_pay livelihood loop. Marking a
            // fresh journey completed without its three turns invalidates the
            // story state and makes ordinary choices fail validation.
            LifeJourney.Ensure(state);
            CharacterRequire(StoryEngine.Validate(state) && !LifeJourney.IsActive(state), "HERO_VALID_TASK_STATE_" + node);
            dialogue = journal = map = help = resetAsk = puzzle = fieldNote = gallery = lifePanel = false;
            EnterNode(true); ResetView();
        }

        static void CheckHeroSkin(GameObject actor)
        {
            CheckContinuousCharacterSkin(actor, "Wensheng");
            if (actor.GetComponent<NativeCharacterRig>()) {
                foreach (var renderer in actor.GetComponentsInChildren<Renderer>())
                    foreach (var material in renderer.sharedMaterials) {
                        CharacterRequire(material && material.shader && material.shader.name == "Qiaopi/MeshyCharacter", "HERO_NATIVE_MATERIAL");
                        CharacterRequire(material.GetTexture("_MainTex") && material.GetTexture("_BumpMap") && material.GetTexture("_MetalRough"), "HERO_NATIVE_THREE_ORIGINAL_MAPS");
                    }
                CharacterRequire(!CharacterPerformance.For(actor).HasFaceMotion, "HERO_NO_LEGACY_UV_FACE_DEFORMATION");
                return;
            }
            var shaders = new HashSet<string>();
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                    if (material && material.shader) shaders.Add(material.shader.name);
            foreach (string shader in new[] {"CharacterSkinTexture", "CharacterEye", "CharacterHair", "CharacterClothing"})
                CharacterRequire(shaders.Contains("Qiaopi/" + shader), "HERO_APPROVED_MATERIAL_" + shader);
        }

        static void CheckContinuousCharacterSkin(GameObject actor, string modelName)
        {
            var skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>();
            CharacterRequire(skins.Length > 0, "CONTINUOUS_SKIN_" + modelName);
            bool fiveBoneBody = false;
            foreach (var skinRenderer in skins) {
                CharacterRequire(skinRenderer.sharedMesh && skinRenderer.sharedMesh.isReadable, "READABLE_SKIN_" + modelName);
                CharacterRequire(skinRenderer.updateWhenOffscreen, "OFFSCREEN_SKIN_UPDATES_" + modelName);
                // Performance NPCs retain these original five weighted bones and
                // add a torso joint; the playable hero still uses exactly five.
                if (skinRenderer.bones.Length < 5) continue;
                Transform rootBone = ModelLibrary.Find(actor.transform, "BodyRoot");
                bool completeRig = rootBone && Array.IndexOf(skinRenderer.bones, rootBone) >= 0;
                foreach (string name in CharacterJoints) {
                    Transform bone = ModelLibrary.Find(actor.transform, name);
                    completeRig &= bone && Array.IndexOf(skinRenderer.bones, bone) >= 0;
                }
                fiveBoneBody |= completeRig;
            }
            CharacterRequire(fiveBoneBody, "FIVE_BONE_WEIGHTED_BODY_" + modelName);
            foreach (var animator in actor.GetComponentsInChildren<Animator>()) CharacterRequire(!animator.enabled, "MANUAL_BONES_NOT_OVERRIDDEN_" + modelName);
        }

        void CheckHeroCarryPose()
        {
            if (nativePlayerPerformance) {
                CharacterRequire(nativePlayerPerformance.IsCarrying && nativePlayerPerformance.HasElbowRig, "HERO_NATIVE_CARRY_WITH_ELBOWS");
                CharacterRequire(nativePlayerPerformance.LeftHandPoint.y > player.transform.position.y + .65f &&
                    nativePlayerPerformance.RightHandPoint.y > player.transform.position.y + .65f, "HERO_NATIVE_HANDS_RAISED");
                return;
            }
            CharacterRequire(Quaternion.Angle(leftArm.localRotation, baseLeftArm * Quaternion.Euler(-65, 0, 0)) < .2f &&
                Quaternion.Angle(rightArm.localRotation, baseRightArm * Quaternion.Euler(-65, 0, 0)) < .2f, "HERO_CARRY_ARM_POSE");
        }

        IEnumerator WalkHeroForQA(Vector3 destination, string label, string reviewPath)
        {
            Vector3 start = player.transform.position;
            float swing = 0; bool captured = false;
            IEnumerator walking = WalkFirstPerson(destination, "hero " + label);
            while (walking.MoveNext()) {
                yield return walking.Current;
                swing = Mathf.Max(swing, Quaternion.Angle(leftLeg.localRotation, baseLeftLeg));
                if (carrying || sideCarrying) CheckHeroCarryPose();
                if (!captured && swing > 20) {
                    // Synchronous capture restores every shadow mode before the
                    // next gameplay frame and does not move the player's camera.
                    SaveHeroBody(Path.Combine(reviewPath, "hero_walk_" + label + ".png")); captured = true;
                }
            }
            CharacterRequire(Vector3.Distance(start, player.transform.position) > 1 && swing > 15, "HERO_WALK_MOVES_BODY_AND_LEGS_" + label);
            Debug.Log("HERO_QA_WALK_PASSED " + label + " distance=" + Vector3.Distance(start, player.transform.position).ToString("F2") + " legSwing=" + swing.ToString("F1"));
        }

        void SaveHeroBody(string path)
        {
            var modes = new UnityEngine.Rendering.ShadowCastingMode[hiddenBody.Length];
            var cameraObject = new GameObject("Hero QA pose camera");
            Camera camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            var target = new RenderTexture(800, 1000, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active; Texture2D image = null;
            try {
                for (int i = 0; i < hiddenBody.Length; i++) { modes[i] = hiddenBody[i].shadowCastingMode; hiddenBody[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                camera.cullingMask = ~((1 << HeldItemLayer) | (1 << PortraitLayer));
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = cam.backgroundColor;
                camera.nearClipPlane = .05f; camera.farClipPlane = 50; camera.fieldOfView = 36;
                Vector3 position = player.transform.position;
                camera.transform.position = position + player.transform.forward * 3.8f + player.transform.right * .55f + Vector3.up * 1.15f;
                camera.transform.LookAt(position + Vector3.up * .90f);
                target.Create(); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                byte[] png = image.EncodeToPNG(); CharacterRequire(png != null && png.Length > 3000, "HERO_POSE_CAPTURE"); File.WriteAllBytes(path, png);
            } finally {
                for (int i = 0; i < hiddenBody.Length; i++) hiddenBody[i].shadowCastingMode = modes[i];
                RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(cameraObject); if (image) Destroy(image);
            }
        }

        static void CheckCharacterGeometry(GameObject actor, string modelName)
        {
            CharacterRequire(actor.transform.Find("BlenderModel"), "IMPORTED_MODEL_" + modelName);
            Renderer[] renderers = actor.GetComponentsInChildren<Renderer>();
            CharacterRequire(renderers.Length > 0, "EMPTY_MODEL_" + modelName);
            Bounds bounds = ModelLibrary.WorldBounds(actor.transform);
            foreach (Renderer renderer in renderers) {
                CharacterRequire(renderer.sharedMaterials.Length > 0, "NO_MATERIALS_" + modelName + "_" + renderer.name);
                foreach (Material material in renderer.sharedMaterials)
                    CharacterRequire(material && material.shader && material.shader.isSupported && material.shader.name != "Hidden/InternalErrorShader", "MATERIAL_" + modelName + "_" + renderer.name);
            }
            float height = ModelLibrary.CharacterHeight(modelName);
            CharacterRequire(!float.IsNaN(bounds.size.y) && Mathf.Abs(bounds.size.y - height) < .018f, "METRIC_HEIGHT_" + modelName + "_" + bounds.size.y);
            CharacterRequire(Mathf.Abs(bounds.min.y - actor.transform.position.y) < .018f, "FEET_AT_GROUND_" + modelName);
            CharacterRequire(bounds.size.x > .15f && bounds.size.x < height * 1.1f && bounds.size.z > .1f && bounds.size.z < height, "BODY_BOUNDS_" + modelName + "_" + bounds.size);
        }

        static void CheckCharacterJoint(GameObject actor, string jointName, string modelName)
        {
            Transform joint = ModelLibrary.Find(actor.transform, jointName);
            CharacterRequire(joint, "MISSING_JOINT_" + modelName + "_" + jointName);
            MeshFilter probe = null;
            Vector3 point = Vector3.zero;
            float distance = 0;
            foreach (MeshFilter mesh in joint.GetComponentsInChildren<MeshFilter>()) {
                if (!mesh.sharedMesh || mesh.sharedMesh.vertexCount == 0) continue;
                Vector3 corner = mesh.sharedMesh.bounds.max;
                float candidate = Vector3.Distance(joint.position, mesh.transform.TransformPoint(corner));
                if (candidate > distance) { distance = candidate; point = corner; probe = mesh; }
            }
            if (!probe) {
                CheckSkinnedCharacterJoint(actor, joint, modelName);
                return;
            }
            CharacterRequire(probe && distance > .03f, "JOINT_HAS_LIMB_" + modelName + "_" + jointName);
            Quaternion original = joint.localRotation;
            Vector3 before = probe.transform.TransformPoint(point);
            joint.localRotation = original * Quaternion.Euler(20, 0, 0);
            Vector3 after = probe.transform.TransformPoint(point);
            joint.localRotation = original;
            CharacterRequire(Vector3.Distance(before, after) > .006f, "JOINT_SWINGS_LIMB_" + modelName + "_" + jointName);
        }

        static void CheckSkinnedCharacterJoint(GameObject actor, Transform joint, string modelName)
        {
            bool foundSkin = false;
            float movement = 0;
            int affectedVertices = 0;
            foreach (SkinnedMeshRenderer skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                if (!skin.sharedMesh) continue;
                int boneIndex = Array.IndexOf(skin.bones, joint);
                if (boneIndex < 0) continue;
                foundSkin = true;
                BoneWeight[] weights = skin.sharedMesh.boneWeights;
                CharacterRequire(weights.Length == skin.sharedMesh.vertexCount, "SKIN_WEIGHTS_" + modelName + "_" + joint.name);
                var snapshot = new Mesh();
                Quaternion original = joint.localRotation;
                try {
                    skin.BakeMesh(snapshot, false);
                    Vector3[] before = snapshot.vertices;
                    Matrix4x4 beforeWorld = skin.transform.localToWorldMatrix;
                    joint.localRotation = original * Quaternion.Euler(20, 0, 0);
                    skin.BakeMesh(snapshot, false);
                    Vector3[] after = snapshot.vertices;
                    Matrix4x4 afterWorld = skin.transform.localToWorldMatrix;
                    CharacterRequire(after.Length == before.Length, "SKIN_TOPOLOGY_STABLE_" + modelName);
                    for (int i = 0; i < weights.Length; i++) {
                        BoneWeight weight = weights[i];
                        float influence = (weight.boneIndex0 == boneIndex ? weight.weight0 : 0) +
                            (weight.boneIndex1 == boneIndex ? weight.weight1 : 0) +
                            (weight.boneIndex2 == boneIndex ? weight.weight2 : 0) +
                            (weight.boneIndex3 == boneIndex ? weight.weight3 : 0);
                        if (influence <= .10f) continue;
                        affectedVertices++;
                        movement = Mathf.Max(movement, Vector3.Distance(beforeWorld.MultiplyPoint3x4(before[i]), afterWorld.MultiplyPoint3x4(after[i])));
                    }
                } finally {
                    joint.localRotation = original;
                    if (Application.isPlaying) Destroy(snapshot); else DestroyImmediate(snapshot);
                }
            }
            CharacterRequire(foundSkin && affectedVertices > 0, "JOINT_HAS_SKIN_WEIGHTS_" + modelName + "_" + joint.name);
            CharacterRequire(movement > .006f, "JOINT_DEFORMS_SKIN_" + modelName + "_" + joint.name);
            Debug.Log("CHARACTER_QA_SKIN_JOINT " + modelName + "/" + joint.name + " vertices=" + affectedVertices + " displacement=" + movement.ToString("F4"));
        }

        void SaveCharacterPortrait(string path)
        {
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try {
                RenderTexture.active = portraitTexture;
                image = new Texture2D(portraitTexture.width, portraitTexture.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                byte[] png = image.EncodeToPNG();
                CharacterRequire(png != null && png.Length > 3000, "PORTRAIT_PIXELS_" + portraitId);
                File.WriteAllBytes(path, png);
            } finally {
                RenderTexture.active = previous;
                if (image) Destroy(image);
            }
        }
    }
}
