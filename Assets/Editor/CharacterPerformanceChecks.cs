using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class CharacterPerformanceChecks
    {
        [MenuItem("侨批/检查人物姿态与中性恢复")]
        public static void Run()
        {
            int checkedActors = 0;
            foreach (string name in new[] { "Wensheng", "Mother", "Wife", "Aman", "Uncle", "Xusheng", "Guide", "Foreman", "Shopkeeper", "MasterHe", "Clerk", "Worker" }) {
                var actor = ModelLibrary.Spawn(name, null, Vector3.zero);
                Require(actor, "MODEL_" + name);
                try {
                    var nativeRig = actor.GetComponent<NativeCharacterRig>();
                    if (nativeRig) {
                        if (nativeRig.HasImportedMotion) NativeMotionChecks.Check(actor);
                        else NativeCharacterChecks.Check(actor);
                        checkedActors++;
                        continue;
                    }
                    CheckPresentation(name, actor);
                    // Baseline is actual baked production geometry, before adding
                    // the runtime pivots, including the static face/accessories.
                    var baseline = Capture(actor);
                    var originals = actor.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var originalMeshes = new Mesh[originals.Length];
                    var originalBones = new Transform[originals.Length][];
                    var originalWeights = new BoneWeight[originals.Length][];
                    for (int i = 0; i < originals.Length; i++) {
                        originalMeshes[i] = originals[i].sharedMesh; originalBones[i] = originals[i].bones;
                        originalWeights[i] = originalMeshes[i].boneWeights;
                    }
                    var leftLeg = ModelLibrary.Find(actor.transform, "LeftLeg");
                    var rightLeg = ModelLibrary.Find(actor.transform, "RightLeg");
                    Vector3 footL = leftLeg.position, footR = rightLeg.position;
                    Quaternion legL = leftLeg.rotation, legR = rightLeg.rotation;
                    foreach (var part in actor.GetComponentsInChildren<Transform>()) part.gameObject.layer = 27;
                    var performance = CharacterPerformance.For(actor); performance.Automatic = false;
                    foreach (var part in actor.GetComponentsInChildren<Transform>()) Require(part.gameObject.layer == 27, name + "_PORTRAIT_LAYER");
                    Require(Vector3.Distance(performance.LeftHandPoint, ModelLibrary.Find(actor.transform, "LeftArm").position) > .25f &&
                        Vector3.Distance(performance.RightHandPoint, ModelLibrary.Find(actor.transform, "RightArm").position) > .25f, name + "_HAND_ANCHORS_AT_HANDS");
                    Require(performance.HasUpperRig, name + "_UPPER_RIG");
                    Require(performance.HasElbowRig, name + "_ELBOW_RIG");
                    CheckPresentation(name, actor);
                    Quaternion elbowLRest = performance.LeftForearmJoint.localRotation;
                    Quaternion elbowRRest = performance.RightForearmJoint.localRotation;
                    EqualGeometry(baseline, Capture(actor), name + "_NEUTRAL");
                    CheckFaceMotion(name, actor, performance, baseline);
                    Quaternion neutralTorso = performance.TorsoJoint.localRotation;
                    CheckRestingPose(name, actor, performance, baseline);
                    CheckSeatedPose(name, actor, performance, baseline);
                    for (int cycle = 0; cycle < 12; cycle++) {
                        performance.SetCarrying(true); performance.Play(CharacterGesture.Lift, 1);
                        for (int frame = 0; frame < 30; frame++) performance.Step(1f / 60f);
                        Require(Quaternion.Angle(performance.TorsoJoint.localRotation, neutralTorso) > 10, name + "_ACTIVE");
                        Require((leftLeg.position - footL).sqrMagnitude < .000001f && (rightLeg.position - footR).sqrMagnitude < .000001f, name + "_HIPS_STAY_PUT");
                        Require(Quaternion.Angle(leftLeg.rotation, legL) < .01f && Quaternion.Angle(rightLeg.rotation, legR) < .01f, name + "_FEET_NOT_TILTED");
                        performance.ResetPose(); EqualGeometry(baseline, Capture(actor), name + "_RESET_" + cycle);
                        Require(!performance.Busy && !performance.IsCarrying, name + "_RESET_STATE");
                    }
                    performance.ResetPose();
                    Vector3 neutralLeftHand = performance.LeftHandPoint, neutralRightHand = performance.RightHandPoint;
                    performance.SetCarrying(true);
                    for (int frame = 0; frame < 60; frame++) performance.Step(1f / 60f);
                    Require(Quaternion.Angle(elbowLRest, performance.LeftForearmJoint.localRotation) > 55 &&
                        Quaternion.Angle(elbowRRest, performance.RightForearmJoint.localRotation) > 55, name + "_CARRY_USES_BENT_ELBOWS");
                    Require(Vector3.Distance(performance.LeftHandPoint, SkinHand(actor, "LeftArm")) < .014f &&
                        Vector3.Distance(performance.RightHandPoint, SkinHand(actor, "RightArm")) < .014f, name + "_CARRY_PALMS_FOLLOW_DEFORMED_SKIN");
                    Vector3 leftReach = actor.transform.InverseTransformDirection(performance.LeftHandPoint - neutralLeftHand);
                    Vector3 rightReach = actor.transform.InverseTransformDirection(performance.RightHandPoint - neutralRightHand);
                    if (!(leftReach.z > .16f && rightReach.z > .16f && leftReach.y > .08f && rightReach.y > .08f)) Debug.LogError("CHARACTER_CARRY_DIAGNOSTIC " + name + " leftDelta=" + leftReach.ToString("F5") + " rightDelta=" + rightReach.ToString("F5") + " before=" + actor.transform.InverseTransformPoint(neutralRightHand).ToString("F5") + " after=" + actor.transform.InverseTransformPoint(performance.RightHandPoint).ToString("F5") + " bone=" + ModelLibrary.Find(actor.transform, "RightArm").rotation.eulerAngles.ToString("F4"));
                    Require(leftReach.z > .16f && rightReach.z > .16f && leftReach.y > .08f && rightReach.y > .08f, name + "_CARRY_HANDS_REACH_FORWARD_AND_UP");
                    if (name == "Clerk") Debug.Log("CHARACTER_HAND_GRIP Clerk left=" + actor.transform.InverseTransformPoint(performance.LeftHandPoint).ToString("F4") + " right=" + actor.transform.InverseTransformPoint(performance.RightHandPoint).ToString("F4"));
                    performance.ResetPose();
                    foreach (CharacterGesture gesture in Enum.GetValues(typeof(CharacterGesture))) {
                        if (gesture == CharacterGesture.None) continue;
                        performance.ResetPose();
                        Quaternion bodyRest = performance.TorsoJoint.localRotation, headRest = performance.HeadJoint.localRotation;
                        Quaternion armRest = ModelLibrary.Find(actor.transform, "RightArm").localRotation;
                        Vector3 beforeHand = performance.RightHandPoint, beforeSkinHand = SkinHand(actor, "RightArm");
                        performance.Play(gesture, .8f);
                        float changed = 0;
                        for (int frame = 0; frame < 60; frame++) {
                            performance.Step(1f / 60f);
                            if (gesture == CharacterGesture.Sign && frame == 23) {
                                Require(Quaternion.Angle(elbowLRest, performance.LeftForearmJoint.localRotation) > 65 &&
                                    Quaternion.Angle(elbowRRest, performance.RightForearmJoint.localRotation) > 65, name + "_SIGN_BENDS_ELBOWS");
                                Require(Vector3.Distance(performance.LeftHandPoint, SkinHand(actor, "LeftArm")) < .014f &&
                                    Vector3.Distance(performance.RightHandPoint, SkinHand(actor, "RightArm")) < .014f, name + "_SIGN_PALMS_FOLLOW_DEFORMED_SKIN");
                                Vector3 handChange = actor.transform.InverseTransformDirection(performance.RightHandPoint - beforeHand);
                                Vector3 skinChange = actor.transform.InverseTransformDirection(SkinHand(actor, "RightArm") - beforeSkinHand);
                                Require(handChange.z > .12f && skinChange.z > .10f && Vector3.Dot(handChange.normalized, skinChange.normalized) > .95f, name + "_SIGN_ANCHOR_AND_SKIN_REACH_FORWARD");
                                Vector3 signLeft = actor.transform.InverseTransformPoint(performance.LeftHandPoint);
                                Vector3 signRight = actor.transform.InverseTransformPoint(performance.RightHandPoint);
                                float actorHeight = ModelLibrary.CharacterHeight(name) / actor.transform.lossyScale.y;
                                float heightGap = signRight.y - signLeft.y, width = signRight.x - signLeft.x;
                                if (!(signLeft.y > actorHeight * .60f && heightGap > actorHeight * .045f && heightGap < actorHeight * .125f && width > actorHeight * .07f && width < actorHeight * .20f))
                                    Debug.LogError("CHARACTER_SIGN_POSE_DIAGNOSTIC " + name + " left=" + signLeft.ToString("F4") + " right=" + signRight.ToString("F4") + " height=" + actorHeight.ToString("F4"));
                                Require(signLeft.y > actorHeight * .60f, name + "_SIGN_SUPPORT_HAND_ABOVE_WAIST");
                                Require(heightGap > actorHeight * .045f && heightGap < actorHeight * .125f, name + "_SIGN_WRITING_HAND_ABOVE_PAPER");
                                Require(width > actorHeight * .07f && width < actorHeight * .20f, name + "_SIGN_HANDS_CLOSE_AROUND_PAPER");
                                if (name == "Clerk" || name == "Uncle") Debug.Log("CHARACTER_SIGN_HAND " + name + " handDelta=" + handChange.ToString("F4") + " skinDelta=" + skinChange.ToString("F4") + " left=" + signLeft.ToString("F4") + " right=" + signRight.ToString("F4"));
                            }
                            changed = Mathf.Max(changed, Quaternion.Angle(bodyRest, performance.TorsoJoint.localRotation),
                                Quaternion.Angle(headRest, performance.HeadJoint.localRotation), Quaternion.Angle(armRest, ModelLibrary.Find(actor.transform, "RightArm").localRotation));
                        }
                        Require(changed > 1, name + "_GESTURE_MOVES_" + gesture);
                        Require(!performance.Busy, name + "_GESTURE_ENDS_" + gesture);
                    }
                    performance.ResetPose();
                    for (int i = 0; i < originals.Length; i++) {
                        BoneWeight[] after = originalMeshes[i].boneWeights;
                        Require(after.Length == originalWeights[i].Length, name + "_SOURCE_LENGTH");
                        for (int v = 0; v < after.Length; v++) Require(after[v].Equals(originalWeights[i][v]), name + "_SOURCE_IMMUTABLE_" + i);
                    }
                    // A low frame rate must not finish the work overlay before
                    // the recipient has visibly reached the signature pose.
                    performance.ResetPose(); Vector3 slowStart = performance.RightHandPoint;
                    performance.Play(CharacterGesture.Sign, 1.5f); performance.Step(.57f);
                    Require(performance.Gesture == CharacterGesture.Sign && Mathf.Abs(performance.Progress - .38f) < .001f,
                        name + "_SIGN_WALL_CLOCK_AFTER_SLOW_FRAME");
                    Require(Vector3.Dot(performance.RightHandPoint - slowStart, actor.transform.forward) > .12f,
                        name + "_SIGN_HAND_VISIBLE_AFTER_SLOW_FRAME");
                    performance.Step(.94f); Require(!performance.Busy, name + "_SIGN_FINISHES_WITH_OVERLAY_CLOCK");
                    performance.ResetPose();
                    var reviewCopy = UnityEngine.Object.Instantiate(actor);
                    try {
                        var copyPerformance = CharacterPerformance.For(reviewCopy); copyPerformance.Automatic = false; copyPerformance.ResetPose();
                        EqualGeometry(baseline, Capture(reviewCopy), name + "_PORTRAIT_COPY");
                        UnityEngine.Object.DestroyImmediate(performance);
                        for (int i = 0; i < originals.Length; i++) Require(originals[i].sharedMesh == originalMeshes[i] && originals[i].bones.Length == originalBones[i].Length, name + "_REMOVE_RESTORES_BINDING");
                        EqualGeometry(baseline, Capture(actor), name + "_REMOVE_RESTORES_GEOMETRY");
                        EqualGeometry(baseline, Capture(reviewCopy), name + "_COPY_OWNS_MESH_AFTER_SOURCE_DELETION");
                    } finally { UnityEngine.Object.DestroyImmediate(reviewCopy); }
                    checkedActors++;
                } finally { UnityEngine.Object.DestroyImmediate(actor); }
            }
            CheckFirstPersonHands();
            Debug.Log("CHARACTER_PERFORMANCE_CHECKS_PASSED actors=" + checkedActors + " neutral geometry, 12 reset cycles, eight gestures, real elbow bends and palm contact, grounded lower body, seated knees and feet, restored skirt drape and source mesh immutability");
        }

        static void CheckPresentation(string name, GameObject actor)
        {
            int skinCount = 0, eyeCount = 0, hairCount = 0, clothCount = 0;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) {
                if (!renderer.enabled) continue;
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++) {
                    var material = materials[index]; if (!material || !material.shader) continue;
                    block.Clear(); renderer.GetPropertyBlock(block, index);
                    switch (material.shader.name) {
                        case "Qiaopi/CharacterSkinTexture":
                            Require(Mathf.Abs(block.GetFloat("_SoftFill") - .038f) < .0001f, name + "_FACE_FILL_SURVIVES_RIG");
                            Require(block.GetFloat("_SpecularStrength") < material.GetFloat("_SpecularStrength"), name + "_FACE_NO_WAX_HIGHLIGHT");
                            Require(Mathf.Abs(material.GetFloat("_SoftFill") - .022f) < .0001f, name + "_SOURCE_FACE_MATERIAL_UNTOUCHED");
                            skinCount++; break;
                        case "Qiaopi/CharacterEye":
                            Require(block.GetFloat("_SpecularStrength") > .04f && block.GetFloat("_SpecularStrength") < .08f, name + "_EYE_CATCHLIGHT_RESTRAINED");
                            eyeCount++; break;
                        case "Qiaopi/CharacterHair":
                            Require(block.GetFloat("_Glossiness") > .15f && block.GetFloat("_Glossiness") < .27f, name + "_HAIR_NOT_PLASTIC");
                            hairCount++; break;
                        case "Qiaopi/CharacterClothing":
                            Require(block.GetFloat("_RoughnessBase") > .9f && block.GetFloat("_BumpScale") > .3f, name + "_CLOTH_RETAINS_TEXTURE");
                            Require(material.mainTexture, name + "_CLOTH_ORIGINAL_ATLAS");
                            clothCount++; break;
                    }
                }
            }
            Require(skinCount > 0 && eyeCount > 0 && hairCount > 0 && clothCount > 0, name + "_ALL_PRESENTATION_SURFACES");
        }

        static void CheckFaceMotion(string name, GameObject actor, CharacterPerformance performance, Dictionary<string, Vector3[]> baseline)
        {
            Require(performance.HasFaceMotion, name + "_FACE_MOTION_UV_MAPPING");
            foreach (bool blink in new[] { true, false }) {
                performance.SetFaceExpression(blink ? 100 : 0, blink ? 0 : 100);
                var posed = Capture(actor); int changed = 0, left = 0, right = 0; float maximum = 0;
                foreach (var part in baseline) for (int i = 0; i < part.Value.Length; i++) {
                    float distance = Vector3.Distance(part.Value[i], posed[part.Key][i]); maximum = Mathf.Max(maximum, distance);
                    if (distance > .00001f) { changed++; if (part.Value[i].x < actor.transform.position.x) left++; else right++; }
                }
                Require(changed > 300 && left > 100 && right > 100, name + (blink ? "_REAL_BILATERAL_EYELID_MOTION" : "_REAL_GENTLE_MOUTH_RELAXATION"));
                Require(maximum > (blink ? .002f : .00005f) && maximum < (blink ? .027f : .003f), name + "_FACE_DISPLACEMENT_LIMIT_" + maximum);
                foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    var mesh = skin.sharedMesh; int shape = mesh.GetBlendShapeIndex(blink ? CharacterFaceMotion.ShapeName : CharacterFaceMotion.MouthShapeName);
                    if (shape < 0) continue;
                    var deltas = new Vector3[mesh.vertexCount]; mesh.GetBlendShapeFrameVertices(shape, 0, deltas, null, null);
                    var materials = skin.sharedMaterials;
                    for (int slot = 0; slot < Mathf.Min(materials.Length, mesh.subMeshCount); slot++) if (materials[slot] && materials[slot].name.StartsWith("QP2_EyeTexture", StringComparison.Ordinal))
                        foreach (int index in mesh.GetIndices(slot)) Require(deltas[index] == Vector3.zero, name + "_EYE_SPHERES_NEVER_ENLARGED_OR_SHIFTED");
                }
                performance.ResetPose(); EqualGeometry(baseline, Capture(actor), name + (blink ? "_BLINK_RESET_EXACT" : "_MOUTH_RESET_EXACT"));
                Debug.Log("CHARACTER_FACE_MOTION_CHECK " + name + " kind=" + (blink ? "lids" : "mouth") + " changed=" + changed + " maxMetres=" + maximum.ToString("F6"));
            }
            int mostlyClosed = 0; float peak = 0;
            for (int frame = 0; frame < 720; frame++) { float weight = CharacterFaceMotion.Weight(name, frame / 60f); peak = Mathf.Max(peak, weight); if (weight > 50) mostlyClosed++; }
            Require(peak > 99 && mostlyClosed > 4 && mostlyClosed < 90, name + "_NATURAL_BRIEF_BLINK_NOT_PERMANENT_CLOSURE");
        }

        static void CheckRestingPose(string name, GameObject actor, CharacterPerformance performance, Dictionary<string, Vector3[]> baseline)
        {
            Vector3 root = actor.transform.position;
            Quaternion facing = actor.transform.rotation;
            var leg = ModelLibrary.Find(actor.transform, "LeftLeg");
            Vector3 planted = leg.position; Quaternion legRotation = leg.rotation;
            var head = performance.HeadJoint;
            Quaternion first = head.localRotation;
            Vector3 leftBefore = performance.LeftHandPoint, rightBefore = performance.RightHandPoint;
            float headMotion = 0;
            for (int frame = 0; frame < 360; frame++) {
                performance.Step(1f / 60f);
                headMotion = Mathf.Max(headMotion, Quaternion.Angle(first, head.localRotation));
            }
            Require(headMotion > .05f && headMotion < 3, name + "_RESTING_ATTENTION_SUBTLE");
            Require(Vector3.Distance(root, actor.transform.position) < .00001f && Quaternion.Angle(facing, actor.transform.rotation) < .01f,
                name + "_RESTING_LEAVES_NAV_ROOT");
            Require(Vector3.Distance(planted, leg.position) < .00001f && Quaternion.Angle(legRotation, leg.rotation) < .01f,
                name + "_RESTING_FEET_PLANTED");
            float leftMoved = Vector3.Distance(leftBefore, performance.LeftHandPoint), rightMoved = Vector3.Distance(rightBefore, performance.RightHandPoint);
            Require(leftMoved > .004f && rightMoved > .004f && leftMoved < .16f && rightMoved < .16f, name + "_RESTING_RELAXED_HANDS");
            performance.ResetPose(); EqualGeometry(baseline, Capture(actor), name + "_RESTING_RESET_EXACT");
            Debug.Log("CHARACTER_PRESENTATION_CHECK " + name + " per-instance surface polish survives gaze rig; resting attention, relaxed arms, grounded feet and exact reset");
        }

        static void CheckSeatedPose(string name, GameObject actor, CharacterPerformance performance, Dictionary<string, Vector3[]> baseline)
        {
            Require(performance.HasSeatedRig, name + "_SEATED_KNEE_RIG");
            var body = ModelLibrary.Find(actor.transform, "BodyRoot");
            Vector3 rootBefore = actor.transform.position, bodyBefore = body.position;
            Vector3 kneeBefore = performance.LeftKneeJoint.position;
            Quaternion calfBefore = performance.LeftKneeJoint.rotation;
            var neutralMetric = MetricSkinPoints(actor);
            float ground = float.PositiveInfinity;
            foreach (var part in neutralMetric) foreach (var point in part.Value) ground = Mathf.Min(ground, point.y);
            for (int cycle = 0; cycle < 3; cycle++) {
                performance.SetSeated(true, name == "Aman" ? .36f : .43f);
                for (int frame = 0; frame < 75; frame++) performance.Step(1f / 60f);
                Require(performance.IsSeated, name + "_SITTING_STATE");
                Require(Vector3.Distance(actor.transform.position, rootBefore) < .00001f, name + "_SIT_LEAVES_NAV_ROOT");
                Require(bodyBefore.y - body.position.y > .10f && bodyBefore.y - body.position.y < .70f, name + "_HIP_LOWERED_TO_STOOL");
                Vector3 kneeMove = performance.LeftKneeJoint.position - kneeBefore;
                if (!(Mathf.Abs(kneeMove.y) < .006f && Vector3.Dot(kneeMove, actor.transform.forward) > .15f))
                    Debug.LogError("SEATED_KNEE_DIAGNOSTIC " + name + " delta=" + kneeMove.ToString("F5") + " angle=" + Quaternion.Angle(calfBefore, performance.LeftKneeJoint.rotation));
                Require(Mathf.Abs(kneeMove.y) < .006f, name + "_KNEE_RETAINS_SHIN_HEIGHT");
                Require(Vector3.Dot(kneeMove, actor.transform.forward) > .15f, name + "_THIGHS_EXTEND_FORWARD");
                Require(Quaternion.Angle(calfBefore, performance.LeftKneeJoint.rotation) < .05f, name + "_SHINS_VERTICAL_AFTER_BENDING");
                var seatedMetric = MetricSkinPoints(actor);
                int feet = 0; float drift = 0;
                foreach (var part in neutralMetric) for (int i = 0; i < part.Value.Length; i++) if (part.Value[i].y < ground + .035f) {
                    drift = Mathf.Max(drift, Mathf.Abs(seatedMetric[part.Key][i].y - part.Value[i].y)); feet++;
                }
                if (drift >= .035f) Debug.LogError("SEATED_FOOT_DIAGNOSTIC " + name + " vertices=" + feet + " drift=" + drift.ToString("F5"));
                Require(feet > 8 && drift < .035f, name + "_REAL_FOOT_GEOMETRY_STAYS_AT_GROUND");
                performance.SetSeated(false);
                for (int frame = 0; frame < 75; frame++) performance.Step(1f / 60f);
                Require(!performance.IsSeated, name + "_STANDS_UP");
                performance.ResetPose(); EqualGeometry(baseline, Capture(actor), name + "_SIT_RESET_" + cycle);
            }
            Debug.Log("CHARACTER_SEATED_CHECK " + name + " knee folds, vertical shins, real feet remain grounded, authored skirt weights restored");
        }
        // Measure the renderer's actual bone/bind skin mapping. FBX import scale
        // is already in these matrices; applying a renderer scale again would
        // falsely report foot drift for Mother, Wife and Aman.
        static Dictionary<string, Vector3[]> MetricSkinPoints(GameObject actor)
        {
            var result = new Dictionary<string, Vector3[]>();
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                if (!skin.enabled || !skin.sharedMesh) continue;
                var mesh = skin.sharedMesh; var weights = mesh.boneWeights; var vertices = mesh.vertices;
                var matrices = new Matrix4x4[skin.bones.Length]; var bindings = mesh.bindposes;
                for (int i = 0; i < matrices.Length; i++) matrices[i] = skin.bones[i].localToWorldMatrix * bindings[i];
                for (int i = 0; i < vertices.Length; i++) {
                    var w = weights[i]; var v = vertices[i];
                    vertices[i] = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 +
                        matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                }
                result.Add(skin.name, vertices);
            }
            return result;
        }

        public static void CheckFirstPersonHands()
        {
            var resource = Resources.Load<GameObject>("Models/FirstPersonHands");
            Require(resource, "FIRST_PERSON_HANDS_RESOURCE_REQUIRED_NO_FALLBACK");
            var hands = ModelLibrary.Spawn("FirstPersonHands", null, Vector3.zero);
            Require(hands, "FIRST_PERSON_HANDS_SPAWNS");
            try {
                int vertices = 0, meshes = 0;
                foreach (var filter in hands.GetComponentsInChildren<MeshFilter>()) if (filter.sharedMesh) { vertices += filter.sharedMesh.vertexCount; meshes++; }
                foreach (var skin in hands.GetComponentsInChildren<SkinnedMeshRenderer>()) if (skin.sharedMesh) { vertices += skin.sharedMesh.vertexCount; meshes++; }
                Require(meshes > 0 && vertices > 0 && hands.GetComponentsInChildren<Renderer>().Length > 0, "FIRST_PERSON_HANDS_HAVE_REAL_GEOMETRY");
                Require(Vector3.Distance(hands.transform.localScale, Vector3.one) < .0001f, "FIRST_PERSON_HANDS_METRIC_ROOT_SCALE");
                Bounds bounds = ModelLibrary.LocalBounds(hands.transform); Vector3 size = bounds.size;
                // The approved hands have full fingertips plus long sleeves:
                // about .80 m wide, .66 m high and .93 m deep in held-rig space.
                Require(size.x >= .55f && size.x <= 1.1f && size.y >= .25f && size.y <= .95f && size.z >= .50f && size.z <= 1.25f,
                    "FIRST_PERSON_HANDS_METRIC_BOUNDS_" + size.ToString("F4"));
                Require(bounds.center.magnitude < .85f, "FIRST_PERSON_HANDS_NEAR_HELD_ORIGIN");
                var neutralHands = Capture(hands);
                var filters = hands.GetComponentsInChildren<MeshFilter>();
                var sourceMeshes = new Mesh[filters.Length];
                var sourceVertices = new Vector3[filters.Length][];
                for (int i = 0; i < filters.Length; i++) { sourceMeshes[i] = filters[i].sharedMesh; sourceVertices[i] = sourceMeshes[i].vertices; }
                var grip = hands.AddComponent<FirstPersonHandGrip>();
                grip.SetPose(42, .075f);
                var writingHands = Capture(hands);
                int raised = 0, stableLeft = 0, curled = 0;
                foreach (var original in neutralHands) {
                    Vector3[] posed = writingHands[original.Key];
                    for (int i = 0; i < original.Value.Length; i++) {
                        Vector3 before = hands.transform.InverseTransformPoint(original.Value[i]);
                        Vector3 after = hands.transform.InverseTransformPoint(posed[i]);
                        if (before.x < -.06f) { Require(Vector3.Distance(before, after) < .00001f, "GRIP_LEFT_SUPPORT_STABLE"); stableLeft++; }
                        if (before.x > .065f && before.z > -.018f && after.y - before.y > .074f) raised++;
                        if (before.x > .065f && before.z > .09f && before.x - after.x > .015f) curled++;
                    }
                }
                Require(raised > 100 && stableLeft > 100 && curled > 100, "GRIP_REAL_RIGHT_WRIST_AND_FINGERS_MOVE");
                for (int i = 0; i < filters.Length; i++) {
                    Vector3[] sourceAfter = sourceMeshes[i].vertices;
                    Require(sourceAfter.Length == sourceVertices[i].Length, "GRIP_SOURCE_COUNT");
                    for (int v = 0; v < sourceAfter.Length; v++) Require(sourceAfter[v] == sourceVertices[i][v], "GRIP_SOURCE_IMMUTABLE");
                }
                grip.SetPose(0, 0); EqualGeometry(neutralHands, Capture(hands), "GRIP_RESTORE");
                grip.SetHouseholdPose(50);
                var household = Capture(hands); int leftClosed = 0, rightClosed = 0, fingersFolded = 0;
                foreach (var original in neutralHands) {
                    Vector3[] posed = household[original.Key];
                    for (int i = 0; i < original.Value.Length; i++) {
                        Vector3 before = hands.transform.InverseTransformPoint(original.Value[i]);
                        Vector3 after = hands.transform.InverseTransformPoint(posed[i]);
                        if (before.x < -.065f && before.z > .09f && after.x - before.x > .015f) leftClosed++;
                        if (before.x > .065f && before.z > .09f && before.x - after.x > .015f) rightClosed++;
                        if(before.z < -.008f)Require(Vector3.Distance(after,before) < .00001f, "HOUSEHOLD_WRISTS_AND_SLEEVES_KEEP_SIZE_AND_POSITION");
                        if(Mathf.Abs(before.x)>.065f&&before.z>.10f&&before.y<.060f&&before.z-after.z>.035f)fingersFolded++;
                    }
                }
                Require(leftClosed > 100 && rightClosed > 100 && fingersFolded > 200, "HOUSEHOLD_BOTH_HANDS_CLOSE_AND_FOLD_FINGERS");
                grip.SetPose(0, 0); EqualGeometry(neutralHands, Capture(hands), "HOUSEHOLD_TO_WORK_RESTORES_BOTH_HANDS");
                UnityEngine.Object.DestroyImmediate(grip);
                for (int i = 0; i < filters.Length; i++) Require(filters[i].sharedMesh == sourceMeshes[i], "GRIP_REMOVE_RESTORES_SOURCE");
                EqualGeometry(neutralHands, Capture(hands), "GRIP_REMOVE_GEOMETRY");
                Debug.Log("FIRST_PERSON_HANDS_ASSET_CHECK_PASSED meshes=" + meshes + " vertices=" + vertices + " metres=" + size.ToString("F4") + " right wrist lift and finger curl; left palm stable; source immutable");
            } finally { UnityEngine.Object.DestroyImmediate(hands); }
        }

        static Vector3 SkinHand(GameObject actor, string jointName)
        {
            Transform joint = ModelLibrary.Find(actor.transform, jointName == "LeftArm" ? "PerformanceLeftForearm" : "PerformanceRightForearm");
            if (!joint) joint = ModelLibrary.Find(actor.transform, jointName); Vector3 total = Vector3.zero; int count = 0;
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                int bone = Array.IndexOf(skin.bones, joint); if (bone < 0 || !skin.sharedMesh) continue;
                BoneWeight[] weights = skin.sharedMesh.boneWeights; Color[] colours = skin.sharedMesh.colors;
                var mesh = new Mesh();
                try {
                    // Include the authored renderer scale here: with useScale=false,
                    // Unity's inherited rig scale is applied again by TransformPoint
                    // for non-unit actors (Mother, Wife, Aman). useScale=true gives
                    // the same world vertices as the renderer's bone/bind matrices.
                    skin.BakeMesh(mesh, true); Vector3[] points = mesh.vertices;
                    for (int i = 0; i < points.Length; i++) {
                        BoneWeight w = weights[i]; float influence = (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0) +
                            (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0);
                        if (influence < .65f || colours.Length != points.Length || colours[i].b < .5f) continue;
                        total += skin.transform.TransformPoint(points[i]); count++;
                    }
                } finally { UnityEngine.Object.DestroyImmediate(mesh); }
            }
            Require(count > 0, "ACTUAL_SKIN_HAND_" + actor.name + "/" + jointName); return total / count;
        }

        static Dictionary<string, Vector3[]> Capture(GameObject actor)
        {
            var result = new Dictionary<string, Vector3[]>();
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                if (!skin.enabled || !skin.sharedMesh) continue;
                var baked = new Mesh();
                try { skin.BakeMesh(baked, false); result.Add(skin.name, WorldPoints(skin.transform, baked.vertices)); }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
            foreach (var filter in actor.GetComponentsInChildren<MeshFilter>()) {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.enabled || !filter.sharedMesh) continue;
                result.Add(filter.name, WorldPoints(filter.transform, filter.sharedMesh.vertices));
            }
            return result;
        }
        static Vector3[] WorldPoints(Transform transform, Vector3[] points)
        {
            for (int i = 0; i < points.Length; i++) points[i] = transform.TransformPoint(points[i]);
            return points;
        }
        static void EqualGeometry(Dictionary<string, Vector3[]> expected, Dictionary<string, Vector3[]> actual, string label)
        {
            Require(expected.Count == actual.Count, label + "_MESH_COUNT");
            foreach (var source in expected) {
                Require(actual.TryGetValue(source.Key, out var points) && points.Length == source.Value.Length, label + "_VERTEX_COUNT_" + source.Key);
                float largest = 0;
                for (int i = 0; i < points.Length; i++) largest = Mathf.Max(largest, Vector3.Distance(points[i], source.Value[i]));
                if (largest >= .0003f) {
                    Vector3 mean = Vector3.zero, extent = Vector3.zero;
                    for (int i = 0; i < points.Length; i++) { Vector3 delta = points[i] - source.Value[i]; mean += delta; extent = Vector3.Max(extent, new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z))); }
                    mean /= points.Length;
                    Debug.LogError("PERFORMANCE_GEOMETRY_DIAGNOSTIC " + label + "/" + source.Key + " max=" + largest + " mean=" + mean.ToString("F9") + " extent=" + extent.ToString("F9") + " firstBefore=" + source.Value[0].ToString("F9") + " firstAfter=" + points[0].ToString("F9"));
                }
                Require(largest < .0003f, label + "_DISPLACEMENT_" + source.Key + "=" + largest);
            }
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("CHARACTER_PERFORMANCE_CHECK_FAILED " + message); }
    }
}
