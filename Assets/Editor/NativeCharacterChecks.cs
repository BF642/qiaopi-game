using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Qiaopi.Editor
{
    public static class NativeCharacterChecks
    {
        sealed class SourceSkin
        {
            public SkinnedMeshRenderer renderer;
            public Mesh mesh;
            public Transform[] bones;
            public Vector3[] vertices;
            public Vector2[] uv;
            public BoneWeight1[] weights;
            public byte[] influenceCounts;
            public Matrix4x4[] bindposes;
            public int shapes;
        }

        sealed class JointPose
        {
            public Transform joint, parent;
            public Vector3 position, scale;
            public Quaternion rotation;
        }

        sealed class SoleProbe
        {
            public readonly List<int>[] vertices = { new List<int>(), new List<int>() };
            public float metresPerUnit, ground;
            public int vertexCount;
        }

        [MenuItem("侨批/检查新文生原生骨架")]
        public static void Run()
        {
            var actor = ModelLibrary.Spawn("Wensheng", null, Vector3.zero);
            Require(actor, "PRODUCTION_MODEL");
            try { Check(actor); }
            finally { Object.DestroyImmediate(actor); }
        }

        // Uses the supplied real imported actor. It deliberately exercises driver
        // removal as well; callers may add CharacterPerformance again afterwards.
        public static void Check(GameObject actor)
        {
            Require(actor, "ACTOR");
            var rig = actor.GetComponent<NativeCharacterRig>();
            Require(rig, "EXPLICIT_NATIVE_RIG"); rig.ValidateJoints();
            Require(rig.strideScale >= .3f && rig.strideScale <= 1f, "VALID_NATIVE_STRIDE_SCALE");
            var existing = actor.GetComponent<CharacterPerformance>();
            if (existing) existing.ResetPose();
            var source = CaptureSources(actor);
            Require(source.Count > 0, "REAL_SKINNED_GEOMETRY");
            foreach (var skin in source) foreach (var material in skin.renderer.sharedMaterials) {
                Require(material && material.shader && material.shader.name == "Qiaopi/MeshyCharacter", "NATIVE_CHARACTER_MATERIAL");
                foreach (string property in new[] { "_MainTex", "_MetalRough", "_BumpMap" }) {
                    Texture texture = material.GetTexture(property);
                    Require(texture && texture.width == 2048 && texture.height == 2048, "ORIGINAL_2048_TEXTURE_BOUND_" + property);
                }
            }
            var poses = CaptureJoints(actor);
            Vector3[] neutral = Capture(actor);
            var identity = actor.GetComponent<CharacterModelIdentity>();
            if (identity) {
                float supportedTurn = identity.modelId == "Mother" ? 0f : identity.modelId == "Wife" ? 90f : 180f;
                Require(Mathf.Abs(rig.signWristTurn - supportedTurn) < .01f, "ROLE_WRIST_CONFIGURATION");
            }
            bool skirtedFamily = identity && (identity.modelId == "Wife" || identity.modelId == "Mother");
            SoleProbe soles = skirtedFamily ? CreateSoleProbe(actor, rig, neutral) : null;
            if (soles != null) {
                CheckSoles(soles, neutral, "STANDING");
                CheckSoleDetection(actor, rig, soles, neutral);
            }
            var performance = CharacterPerformance.For(actor);
            performance.Automatic = false; performance.TurnBodyToLook = false;
            Require(performance.UsesNativeRig && performance.HasUpperRig && performance.HasElbowRig && performance.HasSeatedRig, "FULL_NATIVE_INTERFACE");
            Require(!performance.HasFaceMotion, "STATIC_FACE_DECLARED");
            CheckSources(source); CheckJoints(poses); EqualGeometry(neutral, Capture(actor), "INITIALIZATION_PRESERVES_SOURCE_POSE");
            performance.SetFaceExpression(100, 100);
            EqualGeometry(neutral, Capture(actor), "OLD_FACE_UV_DELTAS_NEVER_APPLIED");

            Vector3 rootPosition = actor.transform.position;
            Quaternion rootRotation = actor.transform.rotation;
            Quaternion headRest = performance.HeadJoint.localRotation;
            performance.LookAt(performance.HeadJoint.position + actor.transform.forward * 2 + actor.transform.right);
            Advance(performance, 60);
            Require(Quaternion.Angle(headRest, performance.HeadJoint.localRotation) > 2, "HEAD_FOLLOWS_LOOK_TARGET");
            Require(MaxMovement(neutral, Capture(actor)) > .001f, "LOOK_HAS_REAL_SKIN_DEFORMATION");
            Reset(performance, actor, neutral, poses, "LOOK_RESET");

            Vector3 leftHand = performance.LeftHandPoint, rightHand = performance.RightHandPoint;
            Quaternion leftElbow = performance.LeftForearmJoint.localRotation, rightElbow = performance.RightForearmJoint.localRotation;
            performance.SetCarrying(true); Advance(performance, 75);
            Require(Vector3.Distance(leftHand, performance.LeftHandPoint) > .08f && Vector3.Distance(rightHand, performance.RightHandPoint) > .08f, "BOTH_HANDS_MOVE_FOR_CARRY");
            Require(Quaternion.Angle(leftElbow, performance.LeftForearmJoint.localRotation) > 50 && Quaternion.Angle(rightElbow, performance.RightForearmJoint.localRotation) > 50, "CARRY_BENDS_NATIVE_ELBOWS");
            CheckHands(rig, performance);
            Require(MaxMovement(neutral, Capture(actor)) > .02f, "CARRY_DEFORMS_IMPORTED_BODY");
            Reset(performance, actor, neutral, poses, "CARRY_RESET");

            var lowerBody = poses.FindAll(p => p.joint.name == "BodyRoot" || p.joint.name == "LeftLeg" || p.joint.name == "RightLeg" ||
                p.joint.name == "LeftKnee" || p.joint.name == "RightKnee" || p.joint.name == "LeftFoot" || p.joint.name == "RightFoot");
            float walkTravel = CheckWalking(actor, rig, performance, neutral, 1f);
            performance.SetLocomotion(0); Advance(performance, 60);
            CheckJoints(lowerBody);
            Reset(performance, actor, neutral, poses, "WALK_IDLE_RESET");
            float runTravel = CheckWalking(actor, rig, performance, neutral, 1.5f);
            Require(runTravel > walkTravel * 1.25f, "RUN_HAS_FASTER_CADENCE_THAN_WALK");
            performance.SetLocomotion(0); Advance(performance, 60);
            CheckJoints(lowerBody);
            Reset(performance, actor, neutral, poses, "RUN_IDLE_RESET");

            Vector3 pelvis = rig.Joint("BodyRoot").position;
            Vector3 knee = rig.Joint("LeftKnee").position;
            Vector3 foot = rig.Joint("LeftFoot").position;
            Quaternion leftFootRest = rig.Joint("LeftFoot").localRotation, rightFootRest = rig.Joint("RightFoot").localRotation;
            performance.SetLocomotion(1.5f); Advance(performance, 45);
            performance.SetSeated(true, .43f); Advance(performance, 90);
            Require(performance.IsSeated, "SEATED_STATE");
            Require(pelvis.y - rig.Joint("BodyRoot").position.y > .08f, "PELVIS_REACHES_SEAT");
            Require(Vector3.Dot(rig.Joint("LeftKnee").position - knee, actor.transform.forward) > .10f, "SEATED_KNEE_MOVES_FORWARD");
            Require(Mathf.Abs(rig.Joint("LeftFoot").position.y - foot.y) < .035f, "SEATED_FOOT_RETAINS_GROUND_HEIGHT");
            if (soles != null) CheckSoles(soles, Capture(actor), "SEATED_SETTLED");
            CheckHands(rig, performance);
            Require(MaxMovement(neutral, Capture(actor)) > .08f, "SEATING_DEFORMS_IMPORTED_BODY");
            Quaternion leftSeatedKnee = rig.Joint("LeftKnee").localRotation, rightSeatedKnee = rig.Joint("RightKnee").localRotation;
            Advance(performance, 60);
            if (soles != null) CheckSoles(soles, Capture(actor), "SEATED_HOLD");
            Require(Quaternion.Angle(leftSeatedKnee, rig.Joint("LeftKnee").localRotation) < .01f && Quaternion.Angle(rightSeatedKnee, rig.Joint("RightKnee").localRotation) < .01f, "SEATED_KNEES_HAVE_NO_RESIDUAL_GAIT");
            Require(Quaternion.Angle(leftFootRest, rig.Joint("LeftFoot").localRotation) < .01f && Quaternion.Angle(rightFootRest, rig.Joint("RightFoot").localRotation) < .01f, "SEATED_ANKLES_HAVE_NO_RESIDUAL_GAIT");
            performance.SetSeated(false); Advance(performance, 90);
            if (soles != null) CheckSoles(soles, Capture(actor), "STOOD_UP");
            CheckJoints(lowerBody);
            Reset(performance, actor, neutral, poses, "SEATED_RESET");

            Quaternion leftWristRest = rig.Joint("LeftHand").localRotation, rightWristRest = rig.Joint("RightHand").localRotation;
            foreach (CharacterGesture gesture in Enum.GetValues(typeof(CharacterGesture))) {
                if (gesture == CharacterGesture.None) continue;
                performance.Play(gesture, .8f);
                float changed = 0;
                for (int frame = 0; frame < 60; frame++) {
                    performance.Step(1f / 60f); CheckHands(rig, performance);
                    if (frame == 14 || frame == 29) changed = Mathf.Max(changed, MaxMovement(neutral, Capture(actor)));
                    if (gesture == CharacterGesture.Sign && frame == 29) {
                        Vector3 handGap = actor.transform.InverseTransformDirection(performance.RightHandPoint - performance.LeftHandPoint);
                        float height = ModelLibrary.WorldBounds(actor.transform).size.y;
                        Require(Mathf.Abs(handGap.x) * Mathf.Abs(actor.transform.lossyScale.x) < height * .30f, "SIGN_HANDS_FIT_PAPER_WIDTH");
                        float wristTurn = Quaternion.Angle(rightWristRest, rig.Joint("RightHand").localRotation);
                        if (rig.signWristTurn > 0)
                            Require(wristTurn > rig.signWristTurn * .90f && wristTurn <= rig.signWristTurn + .1f, "SIGN_USES_AUTHORED_WRIST_RANGE");
                        else Require(wristTurn < .01f, "SIGN_RETAINS_WIDE_CUFF_WRIST");
                        Require(Quaternion.Angle(leftWristRest, rig.Joint("LeftHand").localRotation) < .01f, "SIGN_RETAINS_SUPPORTING_PALM");
                    }
                }
                Require(changed > .001f, "GESTURE_DEFORMS_SKIN_" + gesture);
                Require(!performance.Busy && Mathf.Abs(performance.Progress - 1) < .0001f, "GESTURE_FINISHES_" + gesture);
                Reset(performance, actor, neutral, poses, "GESTURE_RESET_" + gesture);
            }
            performance.Play(CharacterGesture.Sign, 1.5f); performance.Step(.57f);
            Require(performance.Busy && Mathf.Abs(performance.Progress - .38f) < .001f, "SLOW_FRAME_GESTURE_PROGRESS");
            performance.Step(.94f); Require(!performance.Busy, "SLOW_FRAME_GESTURE_FINISHES");
            Reset(performance, actor, neutral, poses, "SLOW_FRAME_RESET");
            Require(Vector3.Distance(rootPosition, actor.transform.position) < .00001f && Quaternion.Angle(rootRotation, actor.transform.rotation) < .01f, "PRESENTATION_LEAVES_NAVIGATION_ROOT");
            CheckSources(source);

            // Clone an active stride, so serialized joint/rest references must
            // remap and restore the clone without touching the source skeleton.
            performance.SetLocomotion(1.5f); Advance(performance, 60);
            performance.Play(CharacterGesture.Sign, .8f); Advance(performance, 20);
            var clone = Object.Instantiate(actor);
            try {
                var cloneRig = clone.GetComponent<NativeCharacterRig>();
                var clonePose = CharacterPerformance.For(clone); clonePose.Automatic = false;
                clonePose.ResetPose(); cloneRig.ValidateJoints();
                EqualGeometry(neutral, Capture(clone), "CLONE_NEUTRAL_GEOMETRY");
                Object.DestroyImmediate(performance);
                rig.ValidateJoints(); CheckJoints(poses); CheckSources(source);
                EqualGeometry(neutral, Capture(actor), "REMOVING_DRIVER_RETAINS_NATIVE_BODY");
                clonePose.SetCarrying(true); clonePose.SetLocomotion(1.5f); Advance(clonePose, 60);
                CheckHands(cloneRig, clonePose);
                Require(MaxMovement(neutral, Capture(clone)) > .02f, "CLONE_WORKS_AFTER_SOURCE_DRIVER_REMOVAL");
                Object.DestroyImmediate(clonePose);
                cloneRig.ValidateJoints();
                EqualGeometry(neutral, Capture(clone), "CLONE_DRIVER_REMOVAL_RESTORES_NATIVE_POSE");
            } finally { Object.DestroyImmediate(clone); }
            rig.ValidateJoints(); CheckSources(source); CheckJoints(poses);
            EqualGeometry(neutral, Capture(actor), "SOURCE_SURVIVES_CLONE_DESTRUCTION");
            Debug.Log("NATIVE_CHARACTER_CHECKS_PASSED " + actor.name + " source meshes/UVs/weights/bindposes unchanged; native head/wrists/elbows/knees/ankles/seat; walking and faster running, idle/seat/reset clear gait; eight gestures and reset; active-stride clone/removal preserve authored bones; face intentionally static");
        }

        static float CheckWalking(GameObject actor, NativeCharacterRig rig, CharacterPerformance performance, Vector3[] neutral, float speed)
        {
            Transform[] knees = { rig.Joint("LeftKnee"), rig.Joint("RightKnee") };
            Transform[] feet = { rig.Joint("LeftFoot"), rig.Joint("RightFoot") };
            Quaternion[] kneeRest = { knees[0].localRotation, knees[1].localRotation };
            Quaternion[] footRest = { feet[0].localRotation, feet[1].localRotation };
            float[] kneePeak = new float[2], footPeak = new float[2];
            Vector3 pelvis = rig.Joint("BodyRoot").localPosition;
            float belowKnee = actor.transform.InverseTransformPoint(knees[0].position).y;
            performance.SetLocomotion(speed); Advance(performance, 45);
            var hip = rig.Joint("LeftLeg");
            Quaternion previous = hip.localRotation;
            float travel = 0, skinMovement = 0;
            for (int frame = 0; frame < 240; frame++) {
                performance.Step(1f / 60f);
                travel += Quaternion.Angle(previous, hip.localRotation); previous = hip.localRotation;
                for (int side = 0; side < 2; side++) {
                    kneePeak[side] = Mathf.Max(kneePeak[side], Quaternion.Angle(kneeRest[side], knees[side].localRotation));
                    footPeak[side] = Mathf.Max(footPeak[side], Quaternion.Angle(footRest[side], feet[side].localRotation));
                }
                Require(Vector3.Distance(pelvis, rig.Joint("BodyRoot").localPosition) < .00001f, "GAIT_DOES_NOT_BOUNCE_PELVIS");
                if (frame == 60 || frame == 110) {
                    Vector3[] moved = Capture(actor);
                    Require(neutral.Length == moved.Length, "GAIT_VERTEX_COUNT_PRESERVED");
                    for (int i = 0; i < neutral.Length; i++) if (neutral[i].y < belowKnee)
                        skinMovement = Mathf.Max(skinMovement, Vector3.Distance(neutral[i], moved[i]));
                }
            }
            for (int side = 0; side < 2; side++) {
                Require(kneePeak[side] > 20 * rig.strideScale && kneePeak[side] < 36 * rig.strideScale, "GAIT_BENDS_NATIVE_KNEE_" + side);
                Require(footPeak[side] > 8 * rig.strideScale && footPeak[side] < 35 * rig.strideScale, "GAIT_ROTATES_NATIVE_ANKLE_" + side);
            }
            Require(skinMovement > .03f, "GAIT_DEFORMS_REAL_LOWER_LEG_GEOMETRY");
            return travel;
        }

        static SoleProbe CreateSoleProbe(GameObject actor, NativeCharacterRig rig, Vector3[] neutral)
        {
            Require(neutral.Length > 0, "SOLE_REAL_GEOMETRY");
            Bounds body = new Bounds(neutral[0], Vector3.zero);
            foreach (Vector3 point in neutral) body.Encapsulate(point);
            float height = ModelLibrary.CharacterHeight(actor.GetComponent<CharacterModelIdentity>().modelId);
            Require(height > 1 && body.size.y > .1f, "SOLE_CALIBRATION_HEIGHT");
            // FBX armatures contain unit transforms which BakeMesh can include
            // despite useScale=false. Calibrate ONCE from the neutral full body
            // and the documented metric stature. Never refit a posed mesh or
            // subtract its moving minimum: that would hide floating shoes.
            var probe = new SoleProbe { metresPerUnit = height / body.size.y, ground = body.min.y, vertexCount = neutral.Length };
            float leftX = actor.transform.InverseTransformPoint(rig.Joint("LeftFoot").position).x;
            float rightX = actor.transform.InverseTransformPoint(rig.Joint("RightFoot").position).x;
            Require(Mathf.Abs(leftX - rightX) > .04f, "SOLE_SEPARATE_FEET");
            float leftSign = Mathf.Sign(leftX - rightX);
            float middle = body.center.x;
            float[] minima = { float.PositiveInfinity, float.PositiveInfinity };
            // The authored skirt hems end above the ankles. Select the bottom
            // shoe surfaces by rest geometry, independent of weights: a wrongly
            // weighted shoe must still be followed and fail the contact test.
            for (int i = 0; i < neutral.Length; i++) {
                Vector3 point = neutral[i];
                if ((point.y - probe.ground) * probe.metresPerUnit > .08f) continue;
                int side = (point.x - middle) * leftSign >= 0 ? 0 : 1;
                minima[side] = Mathf.Min(minima[side], point.y);
            }
            for (int side = 0; side < 2; side++)
                Require(!float.IsInfinity(minima[side]), "SOLE_REST_FOOT_REGION_" + side);
            for (int i = 0; i < neutral.Length; i++) {
                Vector3 point = neutral[i];
                int side = (point.x - middle) * leftSign >= 0 ? 0 : 1;
                if ((point.y - minima[side]) * probe.metresPerUnit <= .012f)
                    probe.vertices[side].Add(i);
            }
            for (int side = 0; side < 2; side++)
                Require(probe.vertices[side].Count >= 20, "SOLE_SURFACE_SAMPLE_" + side);
            return probe;
        }

        static void SoleRange(SoleProbe probe, Vector3[] points, int side, out float bottom, out float top)
        {
            Require(points.Length == probe.vertexCount, "SOLE_VERTEX_TOPOLOGY_UNCHANGED");
            bottom = float.PositiveInfinity; top = float.NegativeInfinity;
            foreach (int index in probe.vertices[side]) {
                float height = (points[index].y - probe.ground) * probe.metresPerUnit;
                Require(!float.IsNaN(height) && !float.IsInfinity(height), "SOLE_FINITE_GEOMETRY");
                bottom = Mathf.Min(bottom, height); top = Mathf.Max(top, height);
            }
        }

        static bool SoleTouchesGround(float bottom, float top)
        {
            // Both feet must contact the floor in the settled chair pose. The
            // entire selected sole is checked, catching heel/toe distortion as
            // well as a whole shoe hovering or penetrating the floor.
            return bottom >= -.018f && bottom <= .030f && top <= .050f;
        }

        static void CheckSoles(SoleProbe probe, Vector3[] points, string pose)
        {
            for (int side = 0; side < 2; side++) {
                SoleRange(probe, points, side, out float bottom, out float top);
                string detail = pose + "_" + (side == 0 ? "LEFT" : "RIGHT") +
                    " bottom_mm=" + (bottom * 1000).ToString("F1") + " top_mm=" + (top * 1000).ToString("F1");
                Require(SoleTouchesGround(bottom, top), "REAL_SHOE_GROUND_CONTACT_" + detail);
                Debug.Log("NATIVE_SHOE_CONTACT_PASSED " + detail + " sampledVertices=" + probe.vertices[side].Count);
            }
        }

        static void CheckSoleDetection(GameObject actor, NativeCharacterRig rig, SoleProbe probe, Vector3[] neutral)
        {
            // Negative controls exercise actual skinning, not the comparison
            // helper with invented values. A detached shoe or swapped side set
            // cannot accidentally make this detector pass every pose.
            string[] joints = { "LeftFoot", "RightFoot" };
            for (int side = 0; side < 2; side++) {
                Transform joint = rig.Joint(joints[side]);
                Vector3 rest = joint.localPosition;
                try {
                    foreach (float offset in new[] { .08f, -.08f }) {
                        joint.localPosition = rest;
                        joint.position += Vector3.up * offset;
                        SoleRange(probe, Capture(actor), side, out float bottom, out float top);
                        Require(!SoleTouchesGround(bottom, top), "SOLE_DETECTS_REAL_" + joints[side] + (offset > 0 ? "_LIFT" : "_PENETRATION"));
                    }
                } finally { joint.localPosition = rest; }
            }
            EqualGeometry(neutral, Capture(actor), "SOLE_NEGATIVE_CONTROLS_RESTORE_GEOMETRY");
            Debug.Log("NATIVE_SHOE_DETECTOR_PASSED " + actor.name + " real left/right shoe lift and penetration rejected; authored pose restored");
        }

        static void Advance(CharacterPerformance performance, int frames)
        {
            for (int i = 0; i < frames; i++) performance.Step(1f / 60f);
        }
        static void CheckHands(NativeCharacterRig rig, CharacterPerformance performance)
        {
            Require(Vector3.Distance(performance.LeftHandPoint, rig.Joint("LeftHand").position) < .00001f &&
                Vector3.Distance(performance.RightHandPoint, rig.Joint("RightHand").position) < .00001f, "HAND_ANCHORS_ARE_AUTHORED_WRISTS");
        }
        static void Reset(CharacterPerformance performance, GameObject actor, Vector3[] neutral, List<JointPose> poses, string code)
        {
            performance.ResetPose(); CheckJoints(poses);
            Require(!performance.Busy && !performance.IsCarrying && !performance.IsSeated, code + "_STATE");
            EqualGeometry(neutral, Capture(actor), code);
        }
        static List<JointPose> CaptureJoints(GameObject actor)
        {
            var result = new List<JointPose>();
            foreach (var joint in actor.GetComponentsInChildren<Transform>(true))
                result.Add(new JointPose { joint = joint, parent = joint.parent, position = joint.localPosition, rotation = joint.localRotation, scale = joint.localScale });
            return result;
        }
        static void CheckJoints(List<JointPose> poses)
        {
            foreach (var pose in poses) {
                Require(pose.joint && pose.joint.parent == pose.parent, "AUTHORED_JOINT_AND_PARENT_PRESERVED");
                Require(Vector3.Distance(pose.joint.localPosition, pose.position) < .00001f && Quaternion.Angle(pose.joint.localRotation, pose.rotation) < .01f && Vector3.Distance(pose.joint.localScale, pose.scale) < .00001f, "AUTHORED_REST_TRANSFORM_RESTORED_" + pose.joint.name);
            }
        }
        static List<SourceSkin> CaptureSources(GameObject actor)
        {
            var result = new List<SourceSkin>();
            foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                var mesh = renderer.sharedMesh;
                Require(mesh && mesh.isReadable && mesh.vertexCount > 0, "READABLE_SOURCE_GEOMETRY");
                result.Add(new SourceSkin { renderer = renderer, mesh = mesh, bones = renderer.bones, vertices = mesh.vertices, uv = mesh.uv,
                    weights = mesh.GetAllBoneWeights().ToArray(), influenceCounts = mesh.GetBonesPerVertex().ToArray(), bindposes = mesh.bindposes, shapes = mesh.blendShapeCount });
            }
            return result;
        }
        static void CheckSources(List<SourceSkin> source)
        {
            foreach (var item in source) {
                Require(item.renderer && item.renderer.sharedMesh == item.mesh, "SOURCE_MESH_REFERENCE_UNCHANGED");
                Equal(item.bones, item.renderer.bones, "SOURCE_BONE_BINDING_UNCHANGED");
                Equal(item.vertices, item.mesh.vertices, "SOURCE_VERTICES_UNCHANGED");
                Equal(item.uv, item.mesh.uv, "SOURCE_UV_UNCHANGED");
                Equal(item.weights, item.mesh.GetAllBoneWeights().ToArray(), "SOURCE_WEIGHTS_UNCHANGED");
                Equal(item.influenceCounts, item.mesh.GetBonesPerVertex().ToArray(), "SOURCE_INFLUENCE_COUNTS_UNCHANGED");
                Equal(item.bindposes, item.mesh.bindposes, "SOURCE_BINDPOSES_UNCHANGED");
                Require(item.shapes == item.mesh.blendShapeCount, "NO_LEGACY_FACE_SHAPES_ADDED");
            }
        }
        static void Equal<T>(T[] first, T[] second, string code)
        {
            Require(first.Length == second.Length, code + "_COUNT");
            for (int i = 0; i < first.Length; i++) Require(EqualityComparer<T>.Default.Equals(first[i], second[i]), code);
        }
        static Vector3[] Capture(GameObject actor)
        {
            var result = new List<Vector3>();
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                var baked = new Mesh();
                try {
                    skin.BakeMesh(baked, false);
                    Matrix4x4 matrix = actor.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                    foreach (var point in baked.vertices) result.Add(matrix.MultiplyPoint3x4(point));
                } finally { Object.DestroyImmediate(baked); }
            }
            return result.ToArray();
        }
        static float MaxMovement(Vector3[] before, Vector3[] after)
        {
            Require(before.Length == after.Length, "BAKED_VERTEX_COUNT_PRESERVED");
            float result = 0;
            for (int i = 0; i < before.Length; i++) result = Mathf.Max(result, Vector3.Distance(before[i], after[i]));
            return result;
        }
        static void EqualGeometry(Vector3[] before, Vector3[] after, string code) { Require(MaxMovement(before, after) < .0002f, code); }
        static void Require(bool value, string code) { if (!value) throw new InvalidOperationException("NATIVE_CHARACTER_CHECK_FAILED " + code); }
    }
}
