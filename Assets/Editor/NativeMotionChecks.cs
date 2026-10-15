using System;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    // Real motion takes have their own checks; do not assert the old synthetic
    // sine-wave knee/ankle angles against an imported recorded gait.
    public static class NativeMotionChecks
    {
        public static void RunWorker()
        {
            try {
                var actor = ModelLibrary.Spawn("Worker", null, Vector3.zero);
                try { Check(actor); } finally { UnityEngine.Object.DestroyImmediate(actor); }
                EditorApplication.Exit(0);
            } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void CheckWorkerTasks()
        {
            var actor=ModelLibrary.Spawn("Worker",null,Vector3.zero);
            try {
                var rig=actor.GetComponent<NativeCharacterRig>();
                Require(rig && rig.HasImportedMotion,"TASK_CHECK_REQUIRES_IMPORTED_MOTION");
                var performance=CharacterPerformance.For(actor); performance.Automatic=false;
                var meshes=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var originals=new Mesh[meshes.Length];for(int i=0;i<meshes.Length;i++) originals[i]=meshes[i].sharedMesh;
                foreach(float size in new[] {1f,.75f}) {
                    actor.transform.localScale=Vector3.one*size; performance.ResetPose();
                    performance.Play(CharacterGesture.Offer,2);
                    for(int frame=0;frame<30;frame++) performance.Step(1f/30);
                    Vector3 hand=actor.transform.InverseTransformPoint(performance.RightHandPoint);
                    Require(hand.y>.95f && hand.y<1.15f,"OFFER_HEIGHT_"+hand.ToString("F4"));
                    Require(Mathf.Abs(hand.z-.30f)<.025f,"OFFER_FORWARD_"+hand.ToString("F4"));
                    Transform wrist=rig.Joint("RightHand"),index=rig.Joint("mixamorig:RightHandIndex1"),pinky=rig.Joint("mixamorig:RightHandPinky1");
                    Vector3 palm=Vector3.Cross(index.position-wrist.position,pinky.position-wrist.position).normalized;
                    Require(Vector3.Dot(palm,actor.transform.up)>.75f,"OFFER_PALM_NOT_SUPPORTING_LETTER");
                    Debug.Log("NATIVE_OFFER_TARGET_PASSED scale="+size+" localHand="+hand.ToString("F4")+" palmUp="+Vector3.Dot(palm,actor.transform.up));
                    performance.SetSeated(true,.43f);
                    performance.Play(CharacterGesture.Sign,2);
                    for(int frame=0;frame<30;frame++) performance.Step(1f/30);
                    Require(Vector3.Distance(performance.LeftHandPoint,performance.RightHandPoint)<.40f*size,"SIGN_TASK_REGRESSION");
                    Vector3 writing=actor.transform.InverseTransformPoint(performance.RightHandPoint);
                    Require(writing.y>.85f && writing.y<1.02f && writing.z>.25f && writing.z<.42f,"SIGN_NOT_AT_DESK_"+writing.ToString("F3"));
                    Debug.Log("NATIVE_SIGN_TARGET_PASSED hand="+writing.ToString("F3"));
                }
                for(int i=0;i<meshes.Length;i++) Require(meshes[i].sharedMesh==originals[i],"TASK_CHANGED_SKIN");
                Debug.Log("NATIVE_TASK_CHECK_PASSED offer=true sign=true heightScale=true sourceMeshes=true");
            } finally {UnityEngine.Object.DestroyImmediate(actor);}
        }

        public static void Check(GameObject actor)
        {
            Require(actor, "ACTOR_MISSING");
            var rig = actor.GetComponent<NativeCharacterRig>();
            Require(rig && rig.HasImportedMotion && rig.UsesImportedAxes, "IMPORTED_MOTION_NOT_BOUND");
            rig.ValidateJoints();
            foreach (var helper in actor.GetComponentsInChildren<MeshRenderer>(true))
                if (helper.name == "Icosphere") Require(!helper.enabled, "EXPORT_PREVIEW_HELPER_VISIBLE");
            var performance = CharacterPerformance.For(actor);
            performance.Automatic = false;
            performance.ResetPose();
            var standingBounds = ModelLibrary.WorldBounds(actor.transform);
            float expectedHeight = ModelLibrary.CharacterHeight(actor.GetComponent<CharacterModelIdentity>().modelId);
            Require(Mathf.Abs(standingBounds.size.y - expectedHeight) < .005f, "STATURE_CHANGED_BY_EXPORT_HELPER");
            Require(Mathf.Abs(standingBounds.min.y - actor.transform.position.y) < .005f, "REST_FEET_NOT_ON_FLOOR");
            Require(performance.UsesNativeRig, "LEGACY_CONVERSION_USED");
            Require(!performance.HasFaceMotion, "UNEXPECTED_LEGACY_FACE_REWRITE");
            var skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var originalMeshes = new Mesh[skins.Length];
            int influences = 0, vertices = 0;
            for (int i = 0; i < skins.Length; i++) {
                var skin = skins[i]; originalMeshes[i] = skin.sharedMesh; vertices += skin.sharedMesh.vertexCount;
                Require(skin.quality == SkinQuality.Auto, "SKIN_WEIGHTS_TRUNCATED_BY_RENDERER");
                using (var counts = skin.sharedMesh.GetBonesPerVertex())
                    foreach (byte count in counts) influences = Mathf.Max(influences, count);
                foreach (var material in skin.sharedMaterials) {
                    Require(material && material.shader.name == "Qiaopi/MeshyCharacter", "MATERIAL_SHADER");
                    foreach (string name in new[] { "_MainTex", "_BumpMap", "_MetalRough" })
                        Require(material.GetTexture(name), "MISSING_TEXTURE_" + name);
                }
            }
            Require(QualitySettings.skinWeights == SkinWeights.Unlimited, "GLOBAL_SKIN_WEIGHTS_TRUNCATED");
            // Desktop-only: original dense meshes are allowed; never reduce geometry to satisfy a vertex budget.
            Require(vertices > 1000, "CHARACTER_GEOMETRY_MISSING");
            Require(influences > 0 && influences <= 32, "UNEXPECTED_BONE_INFLUENCES");
            Transform knee = rig.Joint("LeftKnee"), foot = rig.Joint("LeftFoot");
            float walkRange = MotionRange(performance, knee, 1f);
            float runRange = MotionRange(performance, knee, 1.5f);
            Debug.Log("NATIVE_MOTION_RANGES "+walkRange+" "+runRange+" knee="+knee.name);
            Require(walkRange > 8, "PURE_WALKING_CLIP_NOT_DEFORMING_KNEE");
            Require(runRange > 12, "RUNNING_CLIP_NOT_DEFORMING_KNEE");
            CheckNodIsolation(performance, rig);
            CheckCadence(performance, rig);
            performance.ResetPose();
            Vector3 hand = performance.RightHandPoint;
            performance.Play(CharacterGesture.Offer, 2);
            for (int f = 0; f < 30; f++) performance.Step(1f / 30);
            Vector3 delta = actor.transform.InverseTransformVector(performance.RightHandPoint - hand);
            Require(delta.z > .08f, "OFFER_HAND_NOT_FORWARD_" + delta.ToString("F4"));
            performance.Play(CharacterGesture.Sign, 2);
            for (int f = 0; f < 30; f++) performance.Step(1f / 30);
            Require(Vector3.Distance(performance.LeftHandPoint, performance.RightHandPoint) < .65f, "SIGN_HANDS_TOO_FAR_APART");
            performance.SetSeated(true);
            for (int f = 0; f < 40; f++) performance.Step(1f / 30);
            Require(performance.IsSeated && IsFinite(foot.position), "SEAT_INVALID");
            if (rig.Motion.HasAuthoredSeat) {
                var planarSeat = Vector3.ProjectOnPlane(rig.Joint("BodyRoot").position - actor.transform.position, actor.transform.up);
                Require(planarSeat.magnitude < .12f, "AUTHORED_SEAT_MISALIGNED_WITH_CHAIR_" + planarSeat.ToString("F4"));
            }
            performance.ResetPose();
            for (int i = 0; i < skins.Length; i++) Require(skins[i].sharedMesh == originalMeshes[i], "SOURCE_SKIN_REPLACED");
            Debug.Log("NATIVE_MOTION_CHECK_PASSED role=" + actor.name + " vertices=" + vertices + " maxInfluences=" + influences +
                " walking=" + rig.Motion.WalkingClip + " running=" + rig.Motion.RunningClip + " idle=" + rig.Motion.IdleClip +
                " walkKneeRange=" + walkRange + " runKneeRange=" + runRange + " offerDelta=" + delta.ToString("F4") +
                " fingerAnimationWeight=" + rig.Motion.FingerAnimationWeight + " seatedAnchor=" + rig.Motion.SeatedAnchorOffset.ToString("F4"));
        }

        static void CheckCadence(CharacterPerformance performance, NativeCharacterRig rig)
        {
            if(rig.Motion.WalkingReferenceSpeed<=.01f) return;
            float nominal=rig.Motion.WalkingReferenceSpeed;
            Vector3 original=performance.transform.position;
            performance.ResetPose(); performance.SetLocomotion(.425f);
            foreach(float speed in new[] {nominal,nominal*.5f,0f}) {
                for(int f=0;f<60;f++) { performance.transform.position+=Vector3.forward*(speed/30);performance.Step(1f/30); }
                float expected=speed/nominal;
                Require(Mathf.Abs(rig.Motion.PlaybackRate-expected)<.025f,"MEASURED_WALK_CADENCE "+rig.Motion.PlaybackRate);
                Require(rig.Motion.RunningWeight<.0001f,"WALK_CADENCE_LEAKED_RUNNING");
            }
            double phase=rig.Motion.GaitPhase;
            performance.Step(0); Require(rig.Motion.GaitPhase==phase,"ZERO_DT_ADVANCED_CLIP");
            performance.transform.position+=Vector3.forward*100; performance.Step(1f/30);
            Require(rig.Motion.GaitPhase-phase<.07,"TELEPORT_FAST_FORWARDED_CLIP");
            phase=rig.Motion.GaitPhase; performance.Step(30);
            Require(rig.Motion.GaitPhase-phase<.21,"LONG_PAUSE_FAST_FORWARDED_CLIP");
            phase=rig.Motion.GaitPhase; performance.Step(float.NaN);
            Require(rig.Motion.GaitPhase==phase,"NAN_DT_ADVANCED_CLIP");
            performance.transform.position=original; performance.ResetPose();
            Debug.Log("NATIVE_CADENCE_CHECK_PASSED actualSpeed=true pureWalk=true teleport=true zeroDt=true longPause=true");
        }

        static float MotionRange(CharacterPerformance performance, Transform joint, float speed)
        {
            performance.ResetPose(); performance.SetLocomotion(speed);
            for (int f = 0; f < 20; f++) performance.Step(1f / 30);
            Quaternion start = joint.localRotation; float range = 0;
            for (int f = 0; f < 90; f++) {
                performance.Step(1f / 30);
                range = Mathf.Max(range, Quaternion.Angle(start, joint.localRotation));
                Require(IsFinite(joint.position), "MOTION_NONFINITE");
            }
            return range;
        }
        static void CheckNodIsolation(CharacterPerformance performance, NativeCharacterRig rig)
        {
            string[] names = { "BodyRoot", "PerformanceTorso", "LeftArm", "RightArm", "LeftForearm", "RightForearm", "LeftHand", "RightHand", "LeftLeg", "RightLeg", "LeftKnee", "RightKnee" };
            var idle = new Quaternion[names.Length];
            performance.ResetPose();
            for (int f = 0; f < 30; f++) performance.Step(1f / 30);
            for (int j = 0; j < names.Length; j++) idle[j] = rig.Joint(names[j]).localRotation;
            performance.ResetPose(); performance.Play(CharacterGesture.Nod, 2);
            for (int f = 0; f < 30; f++) performance.Step(1f / 30);
            for (int j = 0; j < names.Length; j++)
                Require(Quaternion.Angle(idle[j], rig.Joint(names[j]).localRotation) < .1f, "DIALOGUE_NOD_OVERRODE_" + names[j]);
        }
        static bool IsFinite(Vector3 p) { return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z); }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("NATIVE_MOTION_CHECK " + message); }
    }
}
