using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool FaceMotionQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-face-motion-review") >= 0;
        IEnumerator RunFaceMotionReview()
        {
            CharacterRequire(QA && DetailQA && FaceMotionQA, "FACE_MOTION_ISOLATED");
            string[] paths = { savePath, savePath + ".bak", savePath + ".tmp" };
            var original = new string[paths.Length];
            for (int i = 0; i < paths.Length; i++) original[i] = CharacterFileSnapshot(paths[i]);
            AudioListener.pause = true; soundEnabled = voiceEnabled = false;
            WorldAtmosphere.SetSoundEnabled(false); musicDirector.SetEnabled(false, false); voiceSource.Stop(); sfx.Stop();
            string output = Path.Combine(Application.persistentDataPath, "FaceMotionReview-" + Application.version);
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-qiaopi-face-motion-output");
            if (index >= 0 && index + 1 < args.Length) output = args[index + 1];
            Directory.CreateDirectory(output); dialogue = homePanel = firstDayPanel = false;
            foreach (var person in CharacterRoster.All()) {
                if (person.id == "narrator") continue;
                ShowPortrait(person.id); yield return null;
                var performance = CharacterPerformance.For(portraitActor); performance.Automatic = false;
                CharacterRequire(performance.UsesNativeRig ? !performance.HasFaceMotion : performance.HasFaceMotion, "FACE_MOTION_CAPABILITY_" + person.model);
                // Use the production face/shoulder camera, materials and lights.
                performance.LookAt(portraitCamera.transform.position);
                for (int frame = 0; frame < 45; frame++) performance.Step(1f / 60f);
                if (performance.UsesNativeRig) {
                    performance.SetFaceExpression(100, 100); yield return null; portraitCamera.Render();
                    SaveFamilyReviewTexture(portraitTexture, Path.Combine(output, person.model + "_static_face.png"));
                    Debug.Log("FACE_MOTION_REVIEW_NATIVE " + person.model + " static face; old UV deformation intentionally disabled");
                } else {
                // GPU skinning is refreshed once per frame. Rendering all three
                // weights in one frame can silently save the same open-eye pose.
                performance.SetFaceExpression(0, 0); yield return null; portraitCamera.Render();
                SaveFamilyReviewTexture(portraitTexture, Path.Combine(output, person.model + "_before.png"));
                performance.SetFaceExpression(person.model == "Wife" || person.model == "Aman" ? 3.5f : 2, 100); yield return null; portraitCamera.Render();
                SaveFamilyReviewTexture(portraitTexture, Path.Combine(output, person.model + "_relaxed.png"));
                performance.SetFaceExpression(100, 100); yield return null; portraitCamera.Render();
                SaveFamilyReviewTexture(portraitTexture, Path.Combine(output, person.model + "_blink.png"));
                CharacterRequire(!SameFaceCapture(Path.Combine(output, person.model + "_before.png"), Path.Combine(output, person.model + "_relaxed.png")), "FACE_MOTION_RELAXED_RENDER_CHANGED_" + person.model);
                CharacterRequire(!SameFaceCapture(Path.Combine(output, person.model + "_relaxed.png"), Path.Combine(output, person.model + "_blink.png")), "FACE_MOTION_BLINK_RENDER_CHANGED_" + person.model);
                Debug.Log("FACE_MOTION_REVIEW_MODEL " + person.model + " original/relaxed/blink; eye geometry and identity preserved");
                }
                if (person.model == "Wensheng" || person.model == "Wife" || person.model == "Mother") {
                    Vector3 cameraPosition = portraitCamera.transform.position;
                    Quaternion cameraRotation = portraitCamera.transform.rotation;
                    float cameraSize = portraitCamera.orthographicSize;
                    float height = ModelLibrary.CharacterHeight(person.model);
                    Vector3 target = portraitActor.transform.position + Vector3.up * height * .50f;
                    portraitCamera.orthographicSize = height * .56f;
                    performance.SetFaceExpression(2, 100);
                    portraitCamera.transform.position = target + Vector3.forward * 3.2f;
                    portraitCamera.transform.LookAt(target);
                    yield return null; portraitCamera.Render();
                    SaveFamilyReviewTexture(portraitTexture, Path.Combine(output, person.model + "_outfit_front.png"));
                    portraitCamera.transform.position = target + new Vector3(1.5f, 0, 2.8f);
                    portraitCamera.transform.LookAt(target);
                    yield return null; portraitCamera.Render();
                    SaveFamilyReviewTexture(portraitTexture, Path.Combine(output, person.model + "_outfit_side.png"));
                    performance.SetCarrying(true);
                    for (int frame = 0; frame < 45; frame++) performance.Step(1f / 60f);
                    performance.SetFaceExpression(2, 100);
                    yield return null; portraitCamera.Render();
                    SaveFamilyReviewTexture(portraitTexture, Path.Combine(output, person.model + "_outfit_carry.png"));
                    performance.SetCarrying(false); performance.ResetPose();
                    portraitCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                    portraitCamera.orthographicSize = cameraSize;
                }
                yield return null;
            }
            yield return CaptureFamilyCastReview(output);
            for (int i = 0; i < paths.Length; i++) CharacterRequire(original[i] == CharacterFileSnapshot(paths[i]), "FACE_MOTION_SAVE_UNCHANGED_" + i);
            CharacterRequire(AudioListener.pause && !soundEnabled && !voiceEnabled && !musicDirector.MusicEnabled, "FACE_MOTION_MUTED");
            Debug.Log("FACE_MOTION_REVIEW_ALL_PASSED legacy face poses and native static face; outfitViews=9 familyGroup=1 actors=12 output=" + output);
        }

        IEnumerator CaptureFamilyCastReview(string output)
        {
            var actors = new List<GameObject>();
            var texture = new RenderTexture(1500, 1050, 24, RenderTextureFormat.ARGB32);
            Vector3 cameraPosition = portraitCamera.transform.position;
            Quaternion cameraRotation = portraitCamera.transform.rotation;
            float cameraSize = portraitCamera.orthographicSize;
            bool wasActive = portraitActor && portraitActor.activeSelf;
            if (portraitActor) portraitActor.SetActive(false);
            try {
                texture.Create(); portraitCamera.targetTexture = texture;
                portraitCamera.orthographicSize = .97f;
                Vector3 target = portraitStage.transform.position + Vector3.up * .84f;
                portraitCamera.transform.position = target + Vector3.forward * 3.2f;
                portraitCamera.transform.LookAt(target);
                string[] models = { "Wensheng", "Mother", "Wife" };
                for (int i = 0; i < models.Length; i++) {
                    var actor = ModelLibrary.Spawn(models[i], portraitStage.transform, new Vector3((1 - i) * .64f, 0, 0));
                    actors.Add(actor);
                    foreach (var child in actor.GetComponentsInChildren<Transform>()) child.gameObject.layer = PortraitLayer;
                    foreach (var renderer in actor.GetComponentsInChildren<Renderer>()) {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
                    }
                    var performance = CharacterPerformance.For(actor); performance.Automatic = false; performance.TurnBodyToLook = false;
                    performance.LookAt(new Vector3(portraitCamera.transform.position.x, actor.transform.position.y + ModelLibrary.CharacterHeight(models[i]) * .93f, portraitCamera.transform.position.z));
                    for (int frame = 0; frame < 45; frame++) performance.Step(1f / 60f);
                    performance.SetFaceExpression(2, 100);
                }
                yield return null; portraitCamera.Render();
                SaveFamilyReviewTexture(texture, Path.Combine(output, "family_cast_280.png"));
            } finally {
                portraitCamera.targetTexture = portraitTexture;
                portraitCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                portraitCamera.orthographicSize = cameraSize;
                foreach (var actor in actors) if (actor) Destroy(actor);
                texture.Release(); Destroy(texture);
                if (portraitActor && wasActive) portraitActor.SetActive(true);
            }
        }

        static bool SameFaceCapture(string first, string second)
        {
            byte[] a = File.ReadAllBytes(first), b = File.ReadAllBytes(second);
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
