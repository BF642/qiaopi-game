using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public enum CharacterGesture { None, Lift, SetDown, Offer, Sign, Point, Nod, Scoop, Weigh }

    // Only actors explicitly given CharacterIdle/For use this driver. The player
    // and AmbientWalker retain their own four-joint locomotion driver.
    // Editor preview/verification actors also need Awake and OnDestroy: without
    // ExecuteAlways, a component explicitly initialized outside Play mode never
    // receives destruction cleanup from Unity.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class CharacterPerformance : MonoBehaviour
    {
        public bool Busy => gesture != CharacterGesture.None;
        public CharacterGesture Gesture => gesture;
        public float Progress => Busy ? Mathf.Clamp01(elapsed / duration) : 1f;
        public bool IsCarrying => carrying;
        public bool HasUpperRig => torso && head;
        public bool HasElbowRig => forearms[0] && forearms[1];
        public bool HasSeatedRig => bodyRoot && knees[0] && knees[1];
        public bool IsSeated => seated || seatBlend > .01f;
        public bool UsesNativeRig => nativeRig;
        public bool HasFaceMotion {
            get { if (nativeRig) return false; foreach (var face in convertedHeads) if (face.renderer && face.renderer.sharedMesh.GetBlendShapeIndex(CharacterFaceMotion.ShapeName) >= 0) return true; return false; }
        }
        public Transform LeftKneeJoint => knees[0];
        public Transform RightKneeJoint => knees[1];
        public Transform LeftForearmJoint => forearms[0];
        public Transform RightForearmJoint => forearms[1];
        public Transform TorsoJoint => torso;
        public Transform HeadJoint => head;
        public Vector3 LeftHandPoint => HandJoint(0) ? HandJoint(0).TransformPoint(handOffsets[0]) : transform.position + transform.up;
        public Vector3 RightHandPoint => HandJoint(1) ? HandJoint(1).TransformPoint(handOffsets[1]) : transform.position + transform.up;
        public bool Automatic = true;
        public bool TurnBodyToLook = true;

        [SerializeField] List<Mesh> ownedMeshes = new List<Mesh>();
        [SerializeField] List<OriginalSkin> originalSkins = new List<OriginalSkin>();
        [SerializeField] List<ConvertedHead> convertedHeads = new List<ConvertedHead>();
        [SerializeField] Transform[] limbs = new Transform[4];
        [SerializeField] Transform[] forearms = new Transform[2];
        [SerializeField] Quaternion[] forearmRest = new Quaternion[2];
        [SerializeField] Transform[] originalParents = new Transform[2];
        [SerializeField] Quaternion[] rest = new Quaternion[4];
        [SerializeField] Vector3[] handOffsets = new Vector3[2];
        [SerializeField] bool nativeRig;
        NativeCharacterRig nativeDefinition;
        NativeMotion nativeMotion;
        Vector3 nativePreviousPosition;
        bool nativePositionSampled, nativeHasTravelled;
        float nativeLeftTaskBlend, nativeRightTaskBlend, nativeTorsoTaskBlend;
        [SerializeField] Transform[] nativeHands = new Transform[2];
        [SerializeField] Quaternion[] nativeHandRest = new Quaternion[2];
        [SerializeField] Vector3[] nativeHandRollAxes = new Vector3[2];
        [SerializeField] Transform[] nativeFeet = new Transform[2];
        [SerializeField] Quaternion[] nativeFootRest = new Quaternion[2];
        [SerializeField] float nativeShoulderRelaxation;
        [SerializeField] float nativeStrideScale = 1f;
        [SerializeField] float nativeSignWristTurn = 180f;
        [SerializeField] float nativeKneeForwardOffset;
        [SerializeField] Transform torso, head, bodyRoot;
        [SerializeField] Transform[] knees = new Transform[2];
        [SerializeField] Quaternion[] kneeRest = new Quaternion[2];
        [SerializeField] Vector3 bodyRestPosition;
        [SerializeField] float hipLocalHeight, kneeLocalHeight, groundLocalHeight;
        [SerializeField] List<SeatSkin> seatSkins = new List<SeatSkin>();
        [SerializeField] Quaternion torsoRest, headRest;
        [SerializeField] float authoredHeight, signInward = 20;
        [SerializeField] float restingPhase;
        [SerializeField] string faceModel;
        [SerializeField] bool initialized;
        [SerializeField] string runtimeOwnerId;
        bool carrying, looking, seated, seatedWeights;
        float seatBlend, seatDrop, seatAngle;
        Vector3 lookPoint;
        CharacterGesture gesture;
        float elapsed, duration = 1f, targetSpeed, motion, phase, clock;
        Vector3 currentTorso, currentHead, leftPose, rightPose;
        Vector2 elbowPose;
        float nativeRightWristPose;
        readonly Vector3[] importedHandForward=new Vector3[2], importedHandBack=new Vector3[2];

        [Serializable] sealed class SeatSkin { public Mesh mesh; public BoneWeight[] standing, sitting; }
        [Serializable] sealed class OriginalSkin { public SkinnedMeshRenderer renderer; public Mesh mesh; public Transform[] bones; public Bounds bounds; }
        [Serializable] sealed class ConvertedHead { public MeshRenderer original; public SkinnedMeshRenderer renderer; public bool enabled; }

        public static CharacterPerformance For(GameObject actor)
        {
            if (!actor) return null;
            var result = actor.GetComponent<CharacterPerformance>();
            if (!result) result = actor.AddComponent<CharacterPerformance>();
            result.Initialize();
            return result;
        }

        void Awake() { Initialize(); }
        void LateUpdate() { if (Automatic && Application.isPlaying) Step(Time.unscaledDeltaTime); }
        void OnDisable() { ResetPose(); }

        public void SetLocomotion(float speed01) { targetSpeed = Mathf.Clamp(speed01, 0, nativeRig ? 1.5f : 1f); }
        public void SetCarrying(bool value) { carrying = value; }
        public void LookAt(Vector3 worldPoint) { lookPoint = worldPoint; looking = true; }
        public void ClearLook() { looking = false; }
        // The seat is a surface height in metres above the actor's ground. The
        // thigh tilts while the counter-rotated calf retains its neutral height.
        public void SetSeated(bool value, float surfaceHeight = .43f)
        {
            Initialize();
            seated = value && HasSeatedRig;
            if (!seated) return;
            if (nativeMotion) nativeMotion.SeatSurfaceHeight=Mathf.Clamp(surfaceHeight,.25f,.65f);
            float scale = Mathf.Abs(transform.lossyScale.y);
            float hip = (hipLocalHeight - groundLocalHeight) * scale;
            float knee = (kneeLocalHeight - groundLocalHeight) * scale;
            float thigh = Mathf.Max(.12f, hip - knee);
            float desiredHip = surfaceHeight + authoredHeight * scale * .035f;
            if (nativeRig) {
                // An authored thigh need not be perfectly vertical in its rest
                // pose. Account for its depth so the seated shin/foot stays level.
                float forward = nativeKneeForwardOffset * scale;
                float length = Mathf.Sqrt(thigh * thigh + forward * forward);
                seatAngle = (Mathf.Acos(Mathf.Clamp((desiredHip - knee) / length, -.15f, .55f)) - Mathf.Atan2(forward, thigh)) * Mathf.Rad2Deg;
            } else seatAngle = Mathf.Acos(Mathf.Clamp((desiredHip - knee) / thigh, -.15f, .55f)) * Mathf.Rad2Deg;
            seatDrop = desiredHip - hip;
            SetSeatWeights(true);
            targetSpeed = 0;
        }
        void SetSeatWeights(bool value)
        {
            if (seatedWeights == value) return;
            foreach (var skin in seatSkins) if (skin.mesh) skin.mesh.boneWeights = value ? skin.sitting : skin.standing;
            seatedWeights = value;
        }
        public void Play(CharacterGesture value, float seconds = 1f)
        {
            Initialize(); gesture = value; elapsed = 0; duration = Mathf.Max(.2f, seconds);
        }

        void Initialize()
        {
            if (initialized) { EnsureMeshOwnership(); return; }
            var authoredRig = GetComponent<NativeCharacterRig>();
            // Validate before recording or changing any pose. An incomplete new
            // rig must not silently fall back to the legacy mesh/UV conversion.
            if (authoredRig) authoredRig.ValidateJoints();
            initialized = true; runtimeOwnerId = gameObject.GetEntityId().ToString();
            nativeRig = authoredRig;
            string[] names = { "LeftArm", "RightArm", "LeftLeg", "RightLeg" };
            for (int i = 0; i < 4; i++) limbs[i] = ModelLibrary.Find(transform, names[i]);
            phase = transform.position.x * .73f + transform.position.z * .19f;
            var identity = GetComponent<CharacterModelIdentity>();
            faceModel = identity ? identity.modelId : name;
            restingPhase = CharacterPresentationPolish.RestingPhase(faceModel);
            if (nativeRig) BindNativeRig(authoredRig);
            else {
                BuildUpperRig();
                FindHandAnchors();
                BuildElbowRig();
                FindHandAnchors();
            }
            for (int i = 0; i < 4; i++) if (limbs[i]) rest[i] = limbs[i].localRotation;
            if (torso) torsoRest = torso.localRotation;
            if (head) headRest = head.localRotation;
            FitSignatureWidth();
            if (!nativeRig) BuildKneeRig();
        }

        void BindNativeRig(NativeCharacterRig rig)
        {
            // This character already owns a complete deform rig. Keep its mesh,
            // weights, UVs, hierarchy and static face exactly as imported.
            bodyRoot = rig.Joint("BodyRoot");
            nativeDefinition = rig; nativeMotion = rig.Motion;
            torso = rig.Joint("PerformanceTorso");
            head = rig.Joint("PerformanceHead");
            nativeShoulderRelaxation = rig.shoulderRelaxation;
            nativeStrideScale = Mathf.Clamp(rig.strideScale, .3f, 1f);
            nativeSignWristTurn = Mathf.Clamp(rig.signWristTurn, 0, 180f);
            bodyRestPosition = bodyRoot.localPosition;
            for (int side = 0; side < 2; side++) {
                string prefix = side == 0 ? "Left" : "Right";
                forearms[side] = rig.Joint(prefix + "Forearm");
                nativeHands[side] = rig.Joint(prefix + "Hand");
                nativeHandRest[side] = nativeHands[side].localRotation;
                nativeHandRollAxes[side] = nativeHands[side].InverseTransformDirection(nativeHands[side].position - forearms[side].position).normalized;
                nativeFeet[side] = rig.Joint(prefix + "Foot");
                knees[side] = rig.Joint(prefix + "Knee");
                forearmRest[side] = forearms[side].localRotation;
                nativeFootRest[side] = nativeFeet[side].localRotation;
                kneeRest[side] = knees[side].localRotation;
                handOffsets[side] = Vector3.zero;
            }
            if(rig.HasImportedMotion) for(int side=0;side<2;side++) {
                string prefix="mixamorig:"+(side==0 ? "Left":"Right");
                var index=rig.Joint(prefix+"HandIndex1"); var pinky=rig.Joint(prefix+"HandPinky1");
                if(index && pinky) {
                    Transform hand=nativeHands[side];
                    importedHandForward[side]=hand.InverseTransformDirection((index.position+pinky.position)*.5f-hand.position).normalized;
                    importedHandBack[side]=hand.InverseTransformDirection(Vector3.Cross(index.position-hand.position,pinky.position-hand.position).normalized*(side==0 ? 1:-1));
                }
            }
            Bounds bounds = ModelLibrary.LocalBounds(transform);
            authoredHeight = bounds.size.y;
            groundLocalHeight = bounds.min.y;
            Vector3 hips = transform.InverseTransformPoint((limbs[2].position + limbs[3].position) * .5f);
            Vector3 calves = transform.InverseTransformPoint((knees[0].position + knees[1].position) * .5f);
            hipLocalHeight = hips.y; kneeLocalHeight = calves.y;
            nativeKneeForwardOffset = calves.z - hips.z;
        }

        // Runtime torso, neck and elbow extensions to the shipped five-bone rig. All source
        // meshes/materials stay immutable; the face is rigid above the neck.
        void BuildUpperRig()
        {
            Transform body = ModelLibrary.Find(transform, "BodyRoot");
            if (!body || !limbs[0] || !limbs[1]) return;
            var sourceSkins = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool readable = false;
            foreach (var skin in sourceSkins)
                readable |= skin.sharedMesh && skin.sharedMesh.isReadable && Array.IndexOf(skin.bones, body) >= 0;
            if (!readable) return;
            Bounds bounds = ModelLibrary.LocalBounds(transform);
            float height = bounds.size.y; authoredHeight = height;
            if (height < .1f) return;
            Vector3 shoulder = transform.InverseTransformPoint((limbs[0].position + limbs[1].position) * .5f);
            Vector3 waist = new Vector3(shoulder.x, bounds.min.y + height * .53f, shoulder.z);
            Vector3 neck = new Vector3(shoulder.x, shoulder.y + height * .052f, shoulder.z);
            torso = NewJoint("PerformanceTorso", body, transform.TransformPoint(waist));
            head = NewJoint("PerformanceHead", torso, transform.TransformPoint(neck));
            for (int i = 0; i < 2; i++) { originalParents[i] = limbs[i].parent; limbs[i].SetParent(torso, true); }

            foreach (var skin in sourceSkins) {
                Mesh source = skin.sharedMesh;
                if (!source || !source.isReadable) continue;
                Transform[] bones = skin.bones;
                int bodyIndex = Array.IndexOf(bones, body);
                if (bodyIndex < 0 || source.boneWeights.Length != source.vertexCount) continue;
                var mesh = Instantiate(source); mesh.name = source.name + " · performance"; ownedMeshes.Add(mesh);
                var newBones = new Transform[bones.Length + 1]; Array.Copy(bones, newBones, bones.Length); newBones[bones.Length] = torso;
                Matrix4x4[] bindings = source.bindposes;
                var worldSkin = new Matrix4x4[bones.Length];
                for (int b = 0; b < bones.Length; b++) worldSkin[b] = bones[b].localToWorldMatrix * bindings[b];
                var newBindings = new Matrix4x4[bindings.Length + 1]; Array.Copy(bindings, newBindings, bindings.Length);
                // Preserve the original BodyRoot's authored FBX coordinate mapping.
                // A renderer transform alone omits its import-space bind rotation.
                newBindings[bindings.Length] = torso.worldToLocalMatrix * body.localToWorldMatrix * bindings[bodyIndex];
                var weights = source.boneWeights; var vertices = source.vertices;
                for (int i = 0; i < weights.Length; i++) {
                    float y = transform.InverseTransformPoint(SkinPoint(vertices[i], weights[i], worldSkin)).y;
                    float upper = Smooth01(Mathf.InverseLerp(waist.y - height * .025f, waist.y + height * .12f, y));
                    weights[i] = SplitWeight(weights[i], bodyIndex, bones.Length, upper);
                }
                mesh.bindposes = newBindings; mesh.boneWeights = weights;
                originalSkins.Add(new OriginalSkin { renderer = skin, mesh = source, bones = bones, bounds = skin.localBounds });
                skin.sharedMesh = mesh; skin.bones = newBones;
                Bounds expanded = skin.localBounds; expanded.Expand(expanded.size * .5f); skin.localBounds = expanded;
            }

            // The authored head mesh may also contain an apron/braid accessory.
            // Weight by height, so lower accessories follow the torso and only
            // the upper neck/head follows the gaze joint. Never scale the face.
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true)) {
                var renderer = filter.GetComponent<MeshRenderer>();
                Mesh source = filter.sharedMesh;
                if (!renderer || !source || !source.isReadable || !filter.name.StartsWith("Head_and_tailored_body_Mesh", StringComparison.Ordinal)) continue;
                Mesh mesh = Instantiate(source); mesh.name = source.name + " · neck binding"; ownedMeshes.Add(mesh);
                CharacterFaceMotion.Bind(mesh, renderer, transform, faceModel);
                var vertices = source.vertices; var weights = new BoneWeight[vertices.Length];
                for (int i = 0; i < vertices.Length; i++) {
                    float y = transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i])).y;
                    float upper = Smooth01(Mathf.InverseLerp(shoulder.y + height * .025f, shoulder.y + height * .085f, y));
                    weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1f - upper, boneIndex1 = 1, weight1 = upper };
                }
                mesh.bindposes = new[] { torso.worldToLocalMatrix * filter.transform.localToWorldMatrix, head.worldToLocalMatrix * filter.transform.localToWorldMatrix };
                mesh.boneWeights = weights;
                var headObject = new GameObject(filter.name);
                headObject.layer = filter.gameObject.layer; headObject.transform.SetParent(filter.transform, false);
                // Keep the rest matrix, then follow the head frame. Skinning
                // already handles torso/neck blends; the renderer's object basis
                // also needs to follow the authored corneal reflection shader.
                headObject.transform.SetParent(head, true);
                // BakeMesh excludes local scale but still carries an inherited
                // rig scale. Render generated skins in a unit world-scale frame:
                // their bind matrices already contain the authored metric size.
                // This keeps static-to-skinned head bounds and vertices identical.
                Vector3 inheritedScale = headObject.transform.lossyScale;
                Vector3 localScale = headObject.transform.localScale;
                headObject.transform.localScale = new Vector3(localScale.x / inheritedScale.x, localScale.y / inheritedScale.y, localScale.z / inheritedScale.z);
                var skin = headObject.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = mesh; skin.sharedMaterials = renderer.sharedMaterials; skin.bones = new[] { torso, head }; skin.rootBone = torso;
                // The spawned cast uses per-instance polish, while ambient
                // clothing may have a separate dye. Preserve both when the
                // original static face/accessories become a gaze-bound skin.
                var appearance = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(appearance);
                skin.SetPropertyBlock(appearance);
                for (int materialIndex = 0; materialIndex < skin.sharedMaterials.Length; materialIndex++) {
                    appearance.Clear();
                    renderer.GetPropertyBlock(appearance, materialIndex);
                    skin.SetPropertyBlock(appearance, materialIndex);
                }
                skin.shadowCastingMode = renderer.shadowCastingMode; skin.receiveShadows = renderer.receiveShadows;
                skin.lightProbeUsage = renderer.lightProbeUsage; skin.reflectionProbeUsage = renderer.reflectionProbeUsage;
                skin.updateWhenOffscreen = true; skin.quality = SkinQuality.Bone4;
                Bounds expanded = mesh.bounds; expanded.Expand(expanded.size * .35f); skin.localBounds = expanded;
                convertedHeads.Add(new ConvertedHead { original = renderer, renderer = skin, enabled = renderer.enabled });
                skin.enabled = renderer.enabled; renderer.enabled = false;
            }
        }

        Transform NewJoint(string name, Transform parent, Vector3 position)
        {
            var joint = new GameObject(name).transform;
            joint.gameObject.layer = parent.gameObject.layer;
            joint.SetParent(parent, false); joint.SetPositionAndRotation(position, transform.rotation);
            return joint;
        }

        void FindHandAnchors()
        {
            // The hands are part of the continuous clothing/body mesh, not separate
            // wrist objects. Cache their bind-pose centre in each arm's space.
            // Blue vertex colour marks exposed skin on the authored body.
            var skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int side = 0; side < 2; side++) {
                Transform palmBone = HandJoint(side);
                if (!palmBone) continue;
                Vector3 total = Vector3.zero; int count = 0;
                foreach (var skin in skins) {
                    Mesh mesh = skin.sharedMesh; if (!mesh || !mesh.isReadable) continue;
                    int arm = Array.IndexOf(skin.bones, palmBone); if (arm < 0) continue;
                    Vector3[] vertices = mesh.vertices; BoneWeight[] weights = mesh.boneWeights; Color[] colours = mesh.colors;
                    Matrix4x4[] bindings = mesh.bindposes; Transform[] bones = skin.bones;
                    var worldSkin = new Matrix4x4[bones.Length];
                    for (int b = 0; b < bones.Length; b++) worldSkin[b] = bones[b].localToWorldMatrix * bindings[b];
                    if (colours.Length != vertices.Length || weights.Length != vertices.Length) continue;
                    for (int i = 0; i < vertices.Length; i++) {
                        BoneWeight w = weights[i];
                        float influence = (w.boneIndex0 == arm ? w.weight0 : 0) + (w.boneIndex1 == arm ? w.weight1 : 0) +
                            (w.boneIndex2 == arm ? w.weight2 : 0) + (w.boneIndex3 == arm ? w.weight3 : 0);
                        if (influence < .65f || colours[i].b < .5f) continue;
                        // Raw imported vertices still use the FBX author's axes;
                        // the bind poses carry the Unity-space conversion too.
                        Vector3 point = SkinPoint(vertices[i], w, worldSkin);
                        total += palmBone.InverseTransformPoint(point); count++;
                    }
                }
                handOffsets[side] = count > 0 ? total / count : palmBone.InverseTransformPoint(limbs[side].position - transform.up * .48f);
            }
        }

        Transform HandJoint(int side) { return nativeRig ? nativeHands[side] : forearms[side] ? forearms[side] : limbs[side]; }

        void BuildElbowRig()
        {
            if (!torso) return;
            var palms = new Vector3[2];
            var shoulders = new Vector3[2];
            for (int side = 0; side < 2; side++) {
                shoulders[side] = limbs[side].position;
                palms[side] = limbs[side].TransformPoint(handOffsets[side]);
                // Fit every actor (including the child) to their own authored arm.
                // The elbow is just short of the midpoint between shoulder/palm.
                forearms[side] = NewJoint(side == 0 ? "PerformanceLeftForearm" : "PerformanceRightForearm",
                    limbs[side], Vector3.Lerp(shoulders[side], palms[side], .49f));
                forearmRest[side] = forearms[side].localRotation;
                handOffsets[side] = forearms[side].InverseTransformPoint(palms[side]);
            }
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                Mesh mesh = skin.sharedMesh;
                // BuildUpperRig already made these instance-owned copies. Never
                // change a resource mesh or create nested restore records.
                if (!mesh || !mesh.isReadable || !ownedMeshes.Contains(mesh)) continue;
                Transform[] bones = skin.bones; int left = Array.IndexOf(bones, limbs[0]), right = Array.IndexOf(bones, limbs[1]);
                if (left < 0 || right < 0) continue;
                Matrix4x4[] bindings = mesh.bindposes;
                var matrices = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++) matrices[i] = bones[i].localToWorldMatrix * bindings[i];
                var newBones = new Transform[bones.Length + 2]; Array.Copy(bones, newBones, bones.Length);
                var newBindings = new Matrix4x4[bindings.Length + 2]; Array.Copy(bindings, newBindings, bindings.Length);
                for (int side = 0; side < 2; side++) {
                    int arm = side == 0 ? left : right;
                    newBones[bones.Length + side] = forearms[side];
                    newBindings[bindings.Length + side] = forearms[side].worldToLocalMatrix * matrices[arm];
                }
                Vector3[] vertices = mesh.vertices; BoneWeight[] weights = mesh.boneWeights;
                for (int vertex = 0; vertex < vertices.Length; vertex++) {
                    Vector3 point = SkinPoint(vertices[vertex], weights[vertex], matrices);
                    for (int side = 0; side < 2; side++) {
                        Vector3 armLine = palms[side] - shoulders[side];
                        float along = Vector3.Dot(point - shoulders[side], armLine) / Mathf.Max(.001f, armLine.sqrMagnitude);
                        float lowerArm = Smooth01(Mathf.InverseLerp(.37f, .65f, along));
                        weights[vertex] = SplitWeight(weights[vertex], side == 0 ? left : right, bones.Length + side, lowerArm);
                    }
                }
                mesh.bindposes = newBindings; mesh.boneWeights = weights; skin.bones = newBones;
            }
        }

        void FitSignatureWidth()
        {
            if (!HasElbowRig) return;
            // Shoulder widths differ substantially between the foreman, young
            // adults and the child. Fit the same receipt-sized hand gap to each
            // body rather than making everybody share one upper-arm angle.
            float low = 0, high = 42, targetGap = authoredHeight * .14f;
            forearms[0].localRotation = forearmRest[0] * PoseDelta(forearms[0], new Vector3(-84, 0, 0));
            forearms[1].localRotation = forearmRest[1] * PoseDelta(forearms[1], new Vector3(-85, 0, 0));
            for (int i = 0; i < 14; i++) {
                float inward = (low + high) * .5f;
                float angle = inward + nativeShoulderRelaxation;
                limbs[0].localRotation = rest[0] * PoseDelta(limbs[0], new Vector3(-22, 0, angle));
                limbs[1].localRotation = rest[1] * PoseDelta(limbs[1], new Vector3(-40, 0, -angle));
                float gap = transform.InverseTransformPoint(RightHandPoint).x - transform.InverseTransformPoint(LeftHandPoint).x;
                if (gap > targetGap) low = inward; else high = inward;
            }
            signInward = (low + high) * .5f;
            for (int i = 0; i < 2; i++) { limbs[i].localRotation = rest[i]; forearms[i].localRotation = forearmRest[i]; }
        }

        Quaternion PoseDelta(Transform joint, Vector3 angles)
        {
            return nativeDefinition ? nativeDefinition.PoseDelta(joint, angles) : Quaternion.Euler(angles);
        }

        void ApplyPose(Transform joint, Quaternion pose, float authoredBlend = 1)
        {
            if (!joint) return;
            joint.localRotation = nativeMotion && nativeMotion.Ready
                ? Quaternion.Slerp(nativeMotion.Rotation(joint), pose, authoredBlend) : pose;
        }

        void BuildKneeRig()
        {
            bodyRoot = ModelLibrary.Find(transform, "BodyRoot");
            if (!bodyRoot || !limbs[2] || !limbs[3] || authoredHeight < .1f) return;
            bodyRestPosition = bodyRoot.localPosition;
            // Use the shipped hip pivots, and a knee just below half the leg.
            // These values are measured before any pose is applied.
            Bounds bounds = ModelLibrary.LocalBounds(transform);
            groundLocalHeight = bounds.min.y;
            hipLocalHeight = transform.InverseTransformPoint((limbs[2].position + limbs[3].position) * .5f).y;
            kneeLocalHeight = groundLocalHeight + (hipLocalHeight - groundLocalHeight) * .49f;
            for (int side = 0; side < 2; side++) {
                Vector3 at = transform.InverseTransformPoint(limbs[side + 2].position); at.y = kneeLocalHeight;
                knees[side] = NewJoint(side == 0 ? "PerformanceLeftKnee" : "PerformanceRightKnee", limbs[side + 2], transform.TransformPoint(at));
                kneeRest[side] = knees[side].localRotation;
            }
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                Mesh mesh = skin.sharedMesh;
                if (!mesh || !mesh.isReadable || !ownedMeshes.Contains(mesh)) continue;
                Transform[] bones = skin.bones; Matrix4x4[] bindings = mesh.bindposes;
                var matrices = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++) matrices[i] = bones[i].localToWorldMatrix * bindings[i];
                int originalBody = Array.IndexOf(bones, bodyRoot), originalTorso = Array.IndexOf(bones, torso);
                int[] legs = { Array.IndexOf(bones, limbs[2]), Array.IndexOf(bones, limbs[3]) };
                // Static apron/braid skins did not previously need leg bones.
                var newBones = new List<Transform>(bones); var newBindings = new List<Matrix4x4>(bindings);
                int mapping = originalBody >= 0 ? originalBody : originalTorso;
                if (mapping < 0 && legs[0] < 0) continue;
                for (int side = 0; side < 2; side++) if (legs[side] < 0) {
                    legs[side] = newBones.Count; newBones.Add(limbs[side + 2]);
                    newBindings.Add(limbs[side + 2].worldToLocalMatrix * matrices[mapping]);
                }
                int firstKnee = newBones.Count;
                for (int side = 0; side < 2; side++) {
                    newBones.Add(knees[side]);
                    Matrix4x4 map = legs[side] < matrices.Length ? matrices[legs[side]] : matrices[mapping];
                    newBindings.Add(knees[side].worldToLocalMatrix * map);
                }
                var vertices = mesh.vertices; var standing = mesh.boneWeights; var sitting = (BoneWeight[])standing.Clone();
                for (int i = 0; i < vertices.Length; i++) {
                    Vector3 point = transform.InverseTransformPoint(SkinPoint(vertices[i], standing[i], matrices));
                    float calf = 1f - Smooth01(Mathf.InverseLerp(kneeLocalHeight - authoredHeight * .045f, kneeLocalHeight + authoredHeight * .045f, point.y));
                    // Transfer existing trouser/foot weights to actual calves.
                    for (int side = 0; side < 2; side++) if (legs[side] < matrices.Length)
                        standing[i] = SplitWeight(standing[i], legs[side], firstKnee + side, calf);
                    sitting[i] = standing[i];
                    // Long skirts/aprons were mostly body-bound. Only while seated,
                    // make their lower part drape over thighs and shins; standing
                    // locomotion retains its authored weights exactly.
                    float lower = 1f - Smooth01(Mathf.InverseLerp(hipLocalHeight - authoredHeight * .06f, hipLocalHeight + authoredHeight * .035f, point.y));
                    float leftX = transform.InverseTransformPoint(limbs[2].position).x;
                    float rightX = transform.InverseTransformPoint(limbs[3].position).x;
                    int sideAt = Mathf.Abs(point.x - leftX) < Mathf.Abs(point.x - rightX) ? 0 : 1;
                    foreach (int from in new[] { originalBody, originalTorso }) if (from >= 0) {
                        sitting[i] = SplitWeight(sitting[i], from, legs[sideAt], lower * (1f - calf));
                        sitting[i] = SplitWeight(sitting[i], from, firstKnee + sideAt, lower * calf / Mathf.Max(.001f, 1f - lower * (1f - calf)));
                    }
                }
                mesh.bindposes = newBindings.ToArray(); mesh.boneWeights = standing; skin.bones = newBones.ToArray();
                seatSkins.Add(new SeatSkin { mesh = mesh, standing = standing, sitting = sitting });
            }
        }

        void EnsureMeshOwnership()
        {
            string instance = gameObject.GetEntityId().ToString();
            if (runtimeOwnerId == instance) return;
            // Review cameras clone live portraits. Serialized transform references
            // are remapped by Instantiate, but mesh assets remain shared: duplicate
            // only our transient meshes so destroying the source portrait cannot
            // invalidate the review copy or its neutral restoration metadata.
            runtimeOwnerId = instance;
            var copies = new Dictionary<Mesh, Mesh>();
            for (int i = 0; i < ownedMeshes.Count; i++) if (ownedMeshes[i]) {
                Mesh original = ownedMeshes[i], copy = Instantiate(original);
                copy.name = original.name; copies.Add(original, copy); ownedMeshes[i] = copy;
            }
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.sharedMesh && copies.TryGetValue(skin.sharedMesh, out var copy)) skin.sharedMesh = copy;
            foreach (var seat in seatSkins) if (seat.mesh && copies.TryGetValue(seat.mesh, out var owned)) seat.mesh = owned;
            ResetPose();
        }

        static Vector3 SkinPoint(Vector3 vertex, BoneWeight w, Matrix4x4[] matrices)
        {
            return matrices[w.boneIndex0].MultiplyPoint3x4(vertex) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(vertex) * w.weight1 +
                matrices[w.boneIndex2].MultiplyPoint3x4(vertex) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(vertex) * w.weight3;
        }

        static float Smooth01(float value) { value = Mathf.Clamp01(value); return value * value * (3f - 2f * value); }

        static BoneWeight SplitWeight(BoneWeight weight, int original, int added, float amount)
        {
            float moved = 0;
            if (weight.boneIndex0 == original) { float part = weight.weight0 * amount; weight.weight0 -= part; moved += part; }
            if (weight.boneIndex1 == original) { float part = weight.weight1 * amount; weight.weight1 -= part; moved += part; }
            if (weight.boneIndex2 == original) { float part = weight.weight2 * amount; weight.weight2 -= part; moved += part; }
            if (weight.boneIndex3 == original) { float part = weight.weight3 * amount; weight.weight3 -= part; moved += part; }
            if (moved <= 0) return weight;
            int least = 0; float minimum = weight.weight0;
            if (weight.weight1 < minimum) { least = 1; minimum = weight.weight1; }
            if (weight.weight2 < minimum) { least = 2; minimum = weight.weight2; }
            if (weight.weight3 < minimum) { least = 3; minimum = weight.weight3; }
            // Keep the strongest four when a source ever has four influences.
            if (moved > minimum) {
                if (least == 0) { weight.boneIndex0 = added; weight.weight0 = moved; }
                else if (least == 1) { weight.boneIndex1 = added; weight.weight1 = moved; }
                else if (least == 2) { weight.boneIndex2 = added; weight.weight2 = moved; }
                else { weight.boneIndex3 = added; weight.weight3 = moved; }
            }
            float total = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
            if (total > .00001f) { weight.weight0 /= total; weight.weight1 /= total; weight.weight2 /= total; weight.weight3 /= total; }
            return weight;
        }

        // Explicit step also permits deterministic editor checks without time,
        // audio, gameplay or save state. It never changes an actor's position.
        public void Step(float deltaTime)
        {
            if (!initialized) Initialize();
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0) return;
            // Gesture time follows the same wall clock as the work presentation.
            // Capping it to a small frame step made arms stay down on slow frames
            // while the UI had already reached signing or finished the action.
            float frameTime = deltaTime, dt = Mathf.Min(frameTime, .1f); clock += frameTime;
            if (!nativeRig) SetFaceExpression(CharacterFaceMotion.Weight(faceModel, clock), 100);
            motion = Mathf.MoveTowards(motion, seated ? 0 : targetSpeed, dt * 4);
            bool motionClips = nativeMotion && nativeMotion.Ready;
            bool authoredSeat = motionClips && nativeMotion.HasAuthoredSeat;
            if (motionClips) {
                float actualSpeed=NativeTravelSpeed(frameTime);
                nativeMotion.Evaluate(frameTime, motion * (1f - seatBlend), seatBlend, actualSpeed);
            }
            bool bothArms = carrying || (seated && !authoredSeat) || gesture == CharacterGesture.Lift || gesture == CharacterGesture.SetDown ||
                gesture == CharacterGesture.Sign || gesture == CharacterGesture.Scoop || gesture == CharacterGesture.Weigh;
            bool rightArm = bothArms || gesture == CharacterGesture.Offer || gesture == CharacterGesture.Point;
            // Dialogue Nod only adds a head gesture; it never pulls both arms
            // away from the source idle animation for every spoken sentence.
            nativeLeftTaskBlend = Mathf.MoveTowards(nativeLeftTaskBlend, bothArms ? 1 : 0, frameTime * 4);
            nativeRightTaskBlend = Mathf.MoveTowards(nativeRightTaskBlend, rightArm ? 1 : 0, frameTime * 4);
            nativeTorsoTaskBlend = Mathf.MoveTowards(nativeTorsoTaskBlend, bothArms ? 1 : 0, frameTime * 4);
            seatBlend = Mathf.MoveTowards(seatBlend, seated ? 1 : 0, frameTime * 1.65f);
            if (!seated && seatBlend <= 0) SetSeatWeights(false);
            phase += motion * dt * 8f;
            if (TurnBodyToLook && looking && motion < .1f && seatBlend < .01f) {
                Vector3 toward = lookPoint - transform.position; toward.y = 0;
                if (toward.sqrMagnitude > .02f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toward), dt * 105f);
            }
            float stride = Mathf.Clamp01(motion);
            float legStride = stride * nativeStrideScale;
            float swing = Mathf.Sin(phase) * (nativeRig ? 25f * legStride : 23f * motion);
            float breath = Mathf.Sin(clock * 1.45f + phase * .07f) * .65f * (1f - stride);
            Vector3 left = new Vector3(carrying ? -22 : -swing * .65f + breath + 3, 0, carrying ? -3 : 0);
            Vector3 right = new Vector3(carrying ? -22 : swing * .65f - breath + 3, 0, carrying ? 3 : 0);
            // Relaxed elbows at rest, and forearms under the parcel when carrying.
            // The old shoulder-only lift kept the arms rigid like planks.
            Vector2 elbows = carrying ? new Vector2(-76, -76) : new Vector2(-9 - motion * 7, -9 - motion * 7);
            Vector3 bodyPose = new Vector3(breath * .45f + (carrying ? 3 : 0), 0, 0);
            Vector3 headPose = new Vector3(-breath * .3f, 0, 0);
            // Relax into a slightly different shoulder/hand balance per actor.
            // This has no pelvis/root translation: feet remain planted. Fade
            // completely out for work, carrying, walking and the seated pose.
            float resting = !Busy && !carrying ? (1f - stride) * (1f - seatBlend) : 0;
            if (resting > 0) {
                float side = Mathf.Sin(restingPhase) >= 0 ? 1 : -1;
                float settle = Mathf.Sin(clock * .48f + restingPhase);
                float attention = Mathf.Sin(clock * .29f + restingPhase * 1.7f);
                left.x += (1.4f + side * .8f) * resting;
                right.x += (.2f - side * .7f) * resting;
                left.z -= .7f * resting; right.z += .7f * resting;
                elbows.x -= (1.5f + side) * resting;
                elbows.y -= (1.5f - side) * resting;
                bodyPose.z += (side * .65f + settle * .28f) * resting;
                bodyPose.y += attention * .45f * resting;
                headPose.z -= (side * .35f + settle * .12f) * resting;
                if (!looking) headPose.y += attention * 1.35f * resting;
            }
            if (seatBlend > 0) {
                left = Vector3.Lerp(left, new Vector3(-13, 0, -3), seatBlend);
                right = Vector3.Lerp(right, new Vector3(-16, 0, 3), seatBlend);
                elbows = Vector2.Lerp(elbows, new Vector2(-74, -70), seatBlend);
                bodyPose.x += 3 * seatBlend;
            }
            if (looking && head) {
                Vector3 view = transform.InverseTransformDirection(lookPoint - head.position);
                float yaw = Mathf.Atan2(view.x, view.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(view.y, new Vector2(view.x, view.z).magnitude) * Mathf.Rad2Deg;
                headPose.x += Mathf.Clamp(pitch, -7, 9); headPose.y = Mathf.Clamp(yaw, seatBlend > .5f ? -28 : -12, seatBlend > .5f ? 28 : 12);
            }
            float rightWrist = 0, importedTaskHold=0;
            CharacterGesture taskGesture=gesture;
            if (Busy) {
                elapsed = Mathf.Min(duration, elapsed + frameTime); float p = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(p * Mathf.PI);
                float hold = Smooth01(Mathf.Clamp01(p / .25f)) * (1f - Smooth01(Mathf.Clamp01((p - .78f) / .22f)));
                if(motionClips) importedTaskHold=hold;
                switch (gesture) {
                    case CharacterGesture.Lift:
                    case CharacterGesture.SetDown:
                        bodyPose.x += 25f * envelope;
                        left = Vector3.Lerp(left, new Vector3(-28, 0, -3), envelope);
                        right = Vector3.Lerp(right, new Vector3(-28, 0, 3), envelope);
                        elbows = Vector2.Lerp(elbows, new Vector2(-34, -34), envelope);
                        headPose.x -= 7f * envelope; break;
                    case CharacterGesture.Offer:
                        if(!motionClips) {right = Vector3.Lerp(right, new Vector3(-39, -5, 6), hold); elbows.y = Mathf.Lerp(elbows.y, -47, hold);}
                        bodyPose.x += 4 * hold; headPose.x += 2 * hold; break;
                    case CharacterGesture.Sign:
                        // Keep upper arms near the ribs, bend at real elbows,
                        // support the paper with the left palm and write above it.
                        right = Vector3.Lerp(right, new Vector3(-40, 0, -signInward), hold);
                        left = Vector3.Lerp(left, new Vector3(-22, 0, signInward), hold);
                        elbows = Vector2.Lerp(elbows, new Vector2(-84, -85 + Mathf.Sin(p * 31) * 1.2f), hold);
                        // Pronation at the authored wrist turns the writing palm
                        // toward the paper; the supporting left palm stays open.
                        if (nativeRig) rightWrist = nativeSignWristTurn * hold;
                        bodyPose.x += 3 * hold; headPose.x += 8 * hold; break;
                    case CharacterGesture.Point:
                        right = Vector3.Lerp(right, new Vector3(-46, -9, 15), hold); elbows.y = Mathf.Lerp(elbows.y, -24, hold); headPose.y -= 5 * hold; break;
                    case CharacterGesture.Nod:
                        headPose.x += 4.5f * Mathf.Sin(p * Mathf.PI * 2) * envelope; break;
                    case CharacterGesture.Scoop:
                        right = Vector3.Lerp(right, new Vector3(-22 - Mathf.Sin(p * Mathf.PI * 2) * 8, 4, 5 + Mathf.Sin(p * Mathf.PI * 2) * 3), hold);
                        left = Vector3.Lerp(left, new Vector3(-18, 0, -3), hold); elbows = Vector2.Lerp(elbows, new Vector2(-55, -60), hold); bodyPose.x += 7 * envelope; break;
                    case CharacterGesture.Weigh:
                        right = Vector3.Lerp(right, new Vector3(-25, 2 + Mathf.Sin(p * Mathf.PI * 2) * 7, 5), hold);
                        left = Vector3.Lerp(left, new Vector3(-19, 0, -3), hold); elbows = Vector2.Lerp(elbows, new Vector2(-60, -70), hold); headPose.x += 5 * hold; break;
                }
                if (elapsed >= duration) gesture = CharacterGesture.None;
            }
            if(motionClips && (carrying || (taskGesture!=CharacterGesture.None && taskGesture!=CharacterGesture.Nod))) {
                // Imported arms use spatial work targets below; the older rig's
                // shoulder/elbow angle sums are not a retargeted animation.
                left=new Vector3(3,0,0); right=new Vector3(3,0,0); elbows=new Vector2(-9,-9);
            }
            // Native shoulders are authored in an A-pose. Apply the same inward
            // roll to every body gesture; source rotations stay intact for reset.
            left.z += nativeShoulderRelaxation;
            right.z -= nativeShoulderRelaxation;
            float blend = 1f - Mathf.Exp(-12f * frameTime);
            elbowPose = Vector2.Lerp(elbowPose, elbows, blend);
            leftPose = Vector3.Lerp(leftPose, left, blend); rightPose = Vector3.Lerp(rightPose, right, blend);
            currentTorso = Vector3.Lerp(currentTorso, bodyPose, blend); currentHead = Vector3.Lerp(currentHead, headPose, blend);
            if (nativeRig) {
                nativeRightWristPose = Mathf.Lerp(nativeRightWristPose, rightWrist, blend);
                if (nativeHands[0]) ApplyPose(nativeHands[0], nativeHandRest[0], nativeLeftTaskBlend);
                if (nativeHands[1]) ApplyPose(nativeHands[1], nativeHandRest[1] * Quaternion.AngleAxis(nativeRightWristPose, nativeHandRollAxes[1]), nativeRightTaskBlend);
            }
            if (limbs[0]) ApplyPose(limbs[0], rest[0] * PoseDelta(limbs[0], leftPose), nativeLeftTaskBlend);
            if (limbs[1]) ApplyPose(limbs[1], rest[1] * PoseDelta(limbs[1], rightPose), nativeRightTaskBlend);
            if (forearms[0]) ApplyPose(forearms[0], forearmRest[0] * PoseDelta(forearms[0], new Vector3(elbowPose.x, 0, 0)), nativeLeftTaskBlend);
            if (forearms[1]) ApplyPose(forearms[1], forearmRest[1] * PoseDelta(forearms[1], new Vector3(elbowPose.y, 0, 0)), nativeRightTaskBlend);
            if (limbs[2]) ApplyPose(limbs[2], rest[2] * PoseDelta(limbs[2], new Vector3(motionClips ? -seatAngle : Mathf.Lerp(swing, -seatAngle, seatBlend), 0, 0)), authoredSeat ? 0 : seatBlend);
            if (limbs[3]) ApplyPose(limbs[3], rest[3] * PoseDelta(limbs[3], new Vector3(motionClips ? -seatAngle : Mathf.Lerp(-swing, -seatAngle, seatBlend), 0, 0)), authoredSeat ? 0 : seatBlend);
            for (int side = 0; side < 2; side++) {
                float walkKnee = 0;
                if (nativeRig) {
                    // Flex during the forward swing, with a smooth zero-slope
                    // transition into stance. Running raises cadence, not height.
                    float legPhase = phase + side * Mathf.PI;
                    float lift = Mathf.Max(0, -Mathf.Cos(legPhase));
                    walkKnee = lift * lift * 32f * legStride;
                    float ankle = (-Mathf.Sin(legPhase) * 13.75f * legStride - walkKnee * .55f) * (1f - seatBlend);
                    if (nativeFeet[side]) ApplyPose(nativeFeet[side], nativeFootRest[side] * PoseDelta(nativeFeet[side], new Vector3(motionClips ? 0 : ankle, 0, 0)), authoredSeat ? 0 : seatBlend);
                }
                if (knees[side]) ApplyPose(knees[side], kneeRest[side] * PoseDelta(knees[side], new Vector3(motionClips ? seatAngle : Mathf.Lerp(walkKnee, seatAngle, seatBlend), 0, 0)), authoredSeat ? 0 : seatBlend);
            }
            if (bodyRoot) {
                Vector3 target = bodyRestPosition + bodyRoot.parent.InverseTransformVector(Vector3.up * seatDrop);
                bodyRoot.localPosition = authoredSeat ? nativeMotion.Position(bodyRoot) : motionClips ? Vector3.Lerp(nativeMotion.Position(bodyRoot), target, seatBlend)
                    : bodyRestPosition + bodyRoot.parent.InverseTransformVector(Vector3.up * seatDrop * seatBlend);
            }
            // A seated clip counter-rotates its spine against the pelvis. Keep
            // that compensation when adding a work gesture; replacing it with
            // the bind pose made a seated writer lean backwards over the stool.
            if (torso) ApplyPose(torso, (motionClips ? nativeMotion.Rotation(torso) : torsoRest) *
                PoseDelta(torso, currentTorso), nativeTorsoTaskBlend);
            if (head) head.localRotation = (motionClips ? nativeMotion.Rotation(head) : headRest) * PoseDelta(head, currentHead);
            if(motionClips) {
                ApplyImportedWork(taskGesture,importedTaskHold);
                nativeMotion.SynchronizeReviewedHandSupports();
            }
        }

        void ApplyImportedWork(CharacterGesture task,float hold)
        {
            float right=hold*nativeRightTaskBlend,left=hold*nativeLeftTaskBlend;
            if(carrying) {left=nativeLeftTaskBlend;right=nativeRightTaskBlend;}
            if(task==CharacterGesture.Offer) ApplyImportedHand(1,new Vector3(.18f,1.06f,.30f),true,right);
            else if(task==CharacterGesture.Sign) {
                float stroke=Mathf.Sin(elapsed*15)*.012f;
                ApplyImportedHand(0,new Vector3(-.14f,.83f,.34f),false,left);
                ApplyImportedHand(1,new Vector3(.10f+stroke,.91f,.32f),false,right);
            } else if(task==CharacterGesture.Point) ApplyImportedHand(1,new Vector3(.19f,1.18f,.43f),false,right);
            else if(task==CharacterGesture.Scoop || task==CharacterGesture.Weigh) {
                ApplyImportedHand(0,new Vector3(-.17f,1.00f,.31f),true,left);
                ApplyImportedHand(1,new Vector3(.17f,1.02f+Mathf.Sin(elapsed*5)*.03f,.32f),true,right);
            } else if(task==CharacterGesture.Lift || task==CharacterGesture.SetDown || carrying) {
                float y=carrying ? .98f : Mathf.Lerp(.76f,.98f,Mathf.Sin(Progress*Mathf.PI));
                ApplyImportedHand(0,new Vector3(-.19f,y,.32f),true,left);
                ApplyImportedHand(1,new Vector3(.19f,y,.32f),true,right);
            }
        }

        void ApplyImportedHand(int side,Vector3 modelTarget,bool palmUp,float blend)
        {
            Transform shoulder=limbs[side], elbow=forearms[side], hand=nativeHands[side];
            if(!shoulder || !elbow || !hand || blend<=.00001f) return;
            float scale=authoredHeight*Mathf.Abs(transform.lossyScale.y)/1.74f;
            Vector3 desired=transform.position+transform.right*(modelTarget.x*scale)+transform.up*(modelTarget.y*scale)+transform.forward*(modelTarget.z*scale);
            Vector3 target=Vector3.Lerp(hand.position,desired,blend);
            Vector3 a=shoulder.position,b=elbow.position,c=hand.position;
            
            float upper=(b-a).magnitude,lower=(c-b).magnitude;
            Vector3 line=target-a; float distance=Mathf.Clamp(line.magnitude,Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
            Vector3 direction=line.normalized;
            // Keep the elbow down beside the ribs while the forearm presents
            // the letter. The target is body-height relative, not camera-height.
            Vector3 hint=transform.position+transform.right*((side==0 ? -.36f:.36f)*scale)+transform.up*(Mathf.Min(modelTarget.y+.10f,1.16f)*scale);
            Vector3 bend=Vector3.ProjectOnPlane(Vector3.Lerp(b-a,hint-a,blend),direction).normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 desiredElbow=a+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            shoulder.rotation=Quaternion.FromToRotation(b-a,desiredElbow-a)*shoulder.rotation;
            elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;
            if(importedHandForward[side].sqrMagnitude>.1f && importedHandBack[side].sqrMagnitude>.1f) {
                Vector3 fingerDirection=transform.forward+(palmUp ? Vector3.zero:-transform.up*.18f);
                Quaternion desiredHand=Quaternion.LookRotation(fingerDirection,palmUp ? -transform.up:transform.up)*
                    Quaternion.Inverse(Quaternion.LookRotation(importedHandForward[side],importedHandBack[side]));
                Vector3 roll=(hand.position-elbow.position).normalized;
                Vector3 currentBack=Vector3.ProjectOnPlane(hand.TransformDirection(importedHandBack[side]),roll);
                Vector3 targetBack=Vector3.ProjectOnPlane(desiredHand*importedHandBack[side],roll);
                if(currentBack.sqrMagnitude>.0001f && targetBack.sqrMagnitude>.0001f)
                    elbow.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(currentBack,targetBack,roll)*.75f*blend,roll)*elbow.rotation;
                hand.rotation=Quaternion.Slerp(hand.rotation,desiredHand,blend);
            }
        }

        float NativeTravelSpeed(float dt)
        {
            Vector3 now=transform.position;
            Vector3 displacement=Vector3.ProjectOnPlane(now-nativePreviousPosition,Vector3.up);
            bool sample=nativePositionSampled;
            nativePositionSampled=true; nativePreviousPosition=now;
            // Actor root only: animation hip bob and body lean are not travel.
            // Initial placement, long pauses and teleports must not accelerate a cycle.
            if(!sample || dt<=.00001f || dt>.25f || displacement.magnitude>1f) {
                if(dt>.25f || displacement.magnitude>1f) nativeHasTravelled=false;
                return -1;
            }
            float speed=displacement.magnitude/dt;
            if(!float.IsFinite(speed) || speed>8f) {nativeHasTravelled=false;return -1;}
            if(speed>.001f) nativeHasTravelled=true;
            // A stationary inspection actor still needs to demonstrate its clip.
            // Once an actor actually moves, stopping its root also stops its feet.
            return nativeHasTravelled ? speed : -1;
        }

        public void ResetPose()
        {
            if (!initialized) return;
            nativePreviousPosition=transform.position; nativePositionSampled=true; nativeHasTravelled=false;
            if (nativeMotion) nativeMotion.ResetPose(); nativeLeftTaskBlend = nativeRightTaskBlend = nativeTorsoTaskBlend = 0;
            gesture = CharacterGesture.None; elapsed = 0; targetSpeed = motion = 0; carrying = looking = seated = false; seatBlend = 0;
            seatedWeights = true; SetSeatWeights(false);
            if (bodyRoot) bodyRoot.localPosition = bodyRestPosition;
            for (int side = 0; side < 2; side++) if (knees[side]) knees[side].localRotation = kneeRest[side];
            if (nativeRig) for (int side = 0; side < 2; side++) if (nativeFeet[side]) nativeFeet[side].localRotation = nativeFootRest[side];
            nativeRightWristPose = 0;
            if (nativeRig) for (int side = 0; side < 2; side++) if (nativeHands[side]) nativeHands[side].localRotation = nativeHandRest[side];
            leftPose = rightPose = currentTorso = currentHead = Vector3.zero; elbowPose = Vector2.zero;
            for (int i = 0; i < 2; i++) if (forearms[i]) forearms[i].localRotation = forearmRest[i];
            for (int i = 0; i < 4; i++) if (limbs[i]) limbs[i].localRotation = rest[i];
            if (torso) torso.localRotation = torsoRest;
            if (head) head.localRotation = headRest;
            SetFaceExpression(0, 0);
        }

        // Separate face weights let QA compare the actual neutral, relaxed and
        // blink poses without replacing a character or moving an eye sphere.
        public void SetFaceExpression(float eyelids, float mouthCorners)
        {
            // The Meshy figure has no authored blinking or speech shapes. Never
            // apply the previous Wensheng's UV-derived facial deltas to it.
            if (nativeRig) return;
            foreach (var face in convertedHeads) {
                var skin = face.renderer; if (!skin || !skin.sharedMesh) continue;
                int lids = skin.sharedMesh.GetBlendShapeIndex(CharacterFaceMotion.ShapeName);
                int mouth = skin.sharedMesh.GetBlendShapeIndex(CharacterFaceMotion.MouthShapeName);
                if (lids >= 0) skin.SetBlendShapeWeight(lids, Mathf.Clamp(eyelids, 0, 100));
                if (mouth >= 0) skin.SetBlendShapeWeight(mouth, Mathf.Clamp(mouthCorners, 0, 100));
            }
        }

        void OnDestroy()
        {
            ResetPose();
            // Native joints and skins belong to the imported character, not this
            // driver. Removing/reloading the driver only restores the rest pose.
            if (nativeRig) return;
            // Restore original ownership even if only this component is removed.
            foreach (var old in originalSkins) if (old.renderer) { old.renderer.sharedMesh = old.mesh; old.renderer.bones = old.bones; old.renderer.localBounds = old.bounds; }
            for (int i = 0; i < 2; i++) if (limbs[i] && originalParents[i]) limbs[i].SetParent(originalParents[i], true);
            foreach (var item in convertedHeads) {
                if (item.original) item.original.enabled = item.enabled;
                if (item.renderer) Release(item.renderer.gameObject);
            }
            foreach (var forearm in forearms) if (forearm) Release(forearm.gameObject);
            foreach (var knee in knees) if (knee) Release(knee.gameObject);
            foreach (var mesh in ownedMeshes) if (mesh) Release(mesh);
            if (torso) Release(torso.gameObject);
        }

        static void Release(UnityEngine.Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
