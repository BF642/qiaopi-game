using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // Sample the actual imported Generic transform curves and blend their poses.
    // A disabled Animator cannot overwrite the task overlay later in the frame.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class NativeMotion : MonoBehaviour
    {
        readonly Dictionary<Transform, Quaternion> sourceRotations = new Dictionary<Transform, Quaternion>();
        readonly Dictionary<Transform, Vector3> sourcePositions = new Dictionary<Transform, Vector3>();
        readonly Dictionary<Transform, Vector3> sourceScales = new Dictionary<Transform, Vector3>();
        readonly Dictionary<Transform, Quaternion> animatedRotations = new Dictionary<Transform, Quaternion>();
        readonly Dictionary<Transform, Vector3> animatedPositions = new Dictionary<Transform, Vector3>();
        readonly AnimationClip[] clips = new AnimationClip[4];
        readonly float[] weights = new float[4];
        Transform[] joints;
        readonly HashSet<Transform> fingers = new HashSet<Transform>();
        Transform hips;
        Vector3 seatedAnchorOffset;
        float fingerWeight = 1;
        Animator animator;
        NativeMotionContacts contacts;
        bool relaxReviewedWrists;
        public float SeatSurfaceHeight { get; set; } = .43f;
        double motionTime, gaitPhase;
        float walkingReference, runningReference, playbackRate=1;
        public double GaitPhase => gaitPhase;
        public float PlaybackRate => playbackRate;
        public float RunningWeight => weights[2];
        public float WalkingReferenceSpeed => walkingReference*Mathf.Abs(transform.lossyScale.y);
        public bool Ready => animator && clips[0] && clips[1] && clips[2];
        public bool HasAuthoredIdle => Ready;
        public bool HasAuthoredSeat => Ready && clips[3];
        public string SeatedClip => clips[3] ? clips[3].name : null;
        public string WalkingClip => clips[1] ? clips[1].name : null;
        public string RunningClip => clips[2] ? clips[2].name : null;
        public string IdleClip => clips[0] ? clips[0].name : null;
        public float FingerAnimationWeight => fingerWeight;
        public Vector3 SeatedAnchorOffset => seatedAnchorOffset;

        public static NativeMotionSet LoadQualifiedSet(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            int slash = resourcePath.LastIndexOf('/');
            string folder = slash >= 0 ? resourcePath.Substring(0, slash) : resourcePath;
            var set = Resources.Load<NativeMotionSet>(folder + "/NativeMotionSet");
            return set && set.IsQualified && set.model == Resources.Load<GameObject>(resourcePath) ? set : null;
        }
        public static bool HasQualifiedSet(string resourcePath) { return LoadQualifiedSet(resourcePath); }

        public static AnimationClip[] LoadImportedClips(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return new AnimationClip[0];
            int slash = resourcePath.LastIndexOf('/');
            return Resources.LoadAll<AnimationClip>(slash >= 0 ? resourcePath.Substring(0, slash) : resourcePath);
        }
        // Idle_4 is the actual take name inspected in the Worker Current export.
        public static bool IsIdleTake(string take)
        {
            return take == "Idle" || take == "Standing Idle" || take == "Idle_4";
        }
        public static bool HasCompleteSet(AnimationClip[] imported, string walkTake = "Walking")
        {
            return (Find(imported, "Idle") || Find(imported, "Standing Idle") || Find(imported, "Idle_4")) && Find(imported, walkTake) && Find(imported, "Running");
        }

        public void Bind(Animator target, NativeMotionSet qualified)
        {
            if (!qualified || !qualified.IsQualified) throw new InvalidOperationException("NATIVE_MOTION_UNQUALIFIED " + name);
            clips[0] = qualified.idle; clips[1] = qualified.walking;
            clips[2] = qualified.running; clips[3] = qualified.seated;
            if (!clips[0] || !clips[1] || !clips[2]) throw new InvalidOperationException("NATIVE_MOTION_IDLE_WALK_RUN_REQUIRED " + name);
            animator = target; animator.enabled = false; animator.applyRootMotion = false;
            walkingReference=qualified.walkingReferenceMetresPerSecond; runningReference=qualified.runningReferenceMetresPerSecond;
            fingerWeight = Mathf.Clamp01(qualified.fingerAnimationWeight);
            QualitySettings.skinWeights = SkinWeights.Unlimited;
            var descendants = new List<Transform>(animator.GetComponentsInChildren<Transform>(true));
            // The model container belongs to placement/height normalization. No
            // verified source curves target it, so animation must not reset it.
            descendants.Remove(animator.transform); joints = descendants.ToArray();
            foreach (var joint in joints) {
                sourceRotations[joint] = joint.localRotation; sourcePositions[joint] = joint.localPosition;
                sourceScales[joint] = joint.localScale;
                animatedRotations[joint] = joint.localRotation; animatedPositions[joint] = joint.localPosition;
                if (joint.name == "mixamorig:Hips") hips = joint;
                if ((joint.name.StartsWith("mixamorig:LeftHand", StringComparison.Ordinal) && joint.name != "mixamorig:LeftHand") ||
                    (joint.name.StartsWith("mixamorig:RightHand", StringComparison.Ordinal) && joint.name != "mixamorig:RightHand")) fingers.Add(joint);
            }
            relaxReviewedWrists=qualified.reviewedContactAndWristAdaptation;
            if (relaxReviewedWrists || qualified.reviewedSeatAndGroundAdaptation) {
                contacts=new NativeMotionContacts(transform,animator.transform,clips[3],qualified.role=="Worker",
                    ModelLibrary.CharacterHeight(qualified.role)/ModelLibrary.CharacterHeight("Worker"));
                RestoreSource();
            }
            if (clips[3] && hips) {
                Vector3 restHip = animator.transform.InverseTransformPoint(hips.position);
                clips[3].SampleAnimation(animator.gameObject, 0);
                Vector3 authoredHip = animator.transform.InverseTransformPoint(hips.position);
                // The chair take was authored away from the character origin.
                // Align its seat to the game's actual chair, retaining height,
                // posture and small movement throughout the source animation.
                seatedAnchorOffset = new Vector3(restHip.x - authoredHip.x, 0, restHip.z - authoredHip.z);
                RestoreSource();
            }
        }

        static AnimationClip Find(AnimationClip[] imported, string take)
        {
            foreach (var clip in imported) {
                if (!clip) continue;
                string last = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                if (string.Equals(last, take, StringComparison.OrdinalIgnoreCase)) return clip;
            }
            return null;
        }

        void RestoreSource()
        {
            foreach (var joint in joints) if (joint) {
                joint.localRotation = sourceRotations[joint]; joint.localPosition = sourcePositions[joint];
                joint.localScale = sourceScales[joint];
            }
        }

        public void Evaluate(float deltaTime, float speed, float seatBlend = 0, float travelledMetresPerSecond = -1)
        {
            if (!Ready || !float.IsFinite(deltaTime) || deltaTime<0 || !float.IsFinite(speed)) return;
            deltaTime=Mathf.Min(deltaTime,.1f);
            float travel = Mathf.Clamp01(speed * 3f);
            // Speed 1 is exactly Walking: a broken or constant Walking take must
            // never appear to move just because a little Running leaked into it.
            float runBlend = Mathf.Clamp01((speed - 1f) / .5f);
            float chair = HasAuthoredSeat ? Mathf.Clamp01(seatBlend) : 0;
            weights[0] = (1 - travel) * (1-chair); weights[1] = travel * (1-runBlend) * (1-chair);
            weights[2] = travel * runBlend * (1-chair); weights[3] = chair;
            motionTime += Mathf.Max(0, deltaTime);
            float reference=Mathf.Lerp(walkingReference,runningReference,runBlend)*Mathf.Abs(transform.lossyScale.y);
            float requestedRate=travelledMetresPerSecond>=0 && reference>.01f
                ? travelledMetresPerSecond/reference : Mathf.Lerp(Mathf.Clamp01(speed),1,runBlend);
            playbackRate=Mathf.MoveTowards(playbackRate,Mathf.Clamp(requestedRate,0,2),deltaTime*5);
            gaitPhase += deltaTime*playbackRate / Mathf.Lerp(clips[1].length, clips[2].length, runBlend);
            float total = 0;
            for (int i = 0; i < clips.Length; i++) {
                if (weights[i] <= 0) continue;
                RestoreSource();
                float time = i == 0 || i == 3 ? (float)(motionTime % clips[i].length)
                    : (float)(gaitPhase % 1) * clips[i].length;
                clips[i].SampleAnimation(animator.gameObject, time);
                if (i == 3 && hips) hips.position += animator.transform.TransformVector(seatedAnchorOffset);
                if (contacts != null) {
                    if(i==0) contacts.GroundIdle();
                    if(i==3) contacts.Seat(SeatSurfaceHeight);
                    if(relaxReviewedWrists) contacts.RelaxWrists(i==3);
                }
                float blend = weights[i] / (total + weights[i]); total += weights[i];
                foreach (var joint in joints) if (joint) {
                    animatedRotations[joint] = Quaternion.Slerp(animatedRotations[joint], joint.localRotation, blend);
                    animatedPositions[joint] = Vector3.Lerp(animatedPositions[joint], joint.localPosition, blend);
                }
            }
            foreach (var joint in joints) if (joint) {
                // Meshy skin can collapse when the stock motion closes fingers
                // aggressively. Only reviewed roles soften those relative-rest
                // rotations; all source keys, weights and body motion remain.
                if (fingerWeight < 1 && fingers.Contains(joint))
                    animatedRotations[joint] = Quaternion.Slerp(sourceRotations[joint], animatedRotations[joint], fingerWeight);
                joint.localRotation = animatedRotations[joint]; joint.localPosition = animatedPositions[joint];
            }
            SynchronizeReviewedHandSupports();
        }

        public void SynchronizeReviewedHandSupports() { if(contacts!=null) contacts.SynchronizeHandSupports(); }

        public Quaternion Rotation(Transform joint)
        {
            Quaternion rotation; return animatedRotations.TryGetValue(joint, out rotation) ? rotation : joint.localRotation;
        }
        public Vector3 Position(Transform joint)
        {
            Vector3 position; return animatedPositions.TryGetValue(joint, out position) ? position : joint.localPosition;
        }
        public void ResetPose()
        {
            if (!Ready) return;
            motionTime = gaitPhase = 0; playbackRate=1; RestoreSource();
            foreach (var joint in joints) if (joint) {
                animatedRotations[joint] = joint.localRotation; animatedPositions[joint] = joint.localPosition;
            }
        }
    }
}
