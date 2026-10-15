using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // Explicit opt-in on the actor root, installed before CharacterPerformance.
    // Legacy native bones use local X flexion. Imported Meshy motion skeletons
    // retain their own axes; PoseDelta maps task gestures into those rest frames.
    [DisallowMultipleComponent]
    public sealed class NativeCharacterRig : MonoBehaviour
    {
        [Tooltip("Shoulder roll added to body poses (left +, right -); ResetPose keeps the imported A-pose.")]
        [Range(-50, 50)] public float shoulderRelaxation = 26f;

        [Tooltip("Shorter steps keep long skirts from stretching across widely separated legs.")]
        [Range(.3f, 1f)] public float strideScale = 1f;

        [Tooltip("Writing hand turn supported by the authored wrist and cuff.")]
        [Range(0, 180)] public float signWristTurn = 180f;

        // Meshy merged motion files retain their real Mixamo paths and bone axes.
        // These names were read from the downloaded Worker FBX, not inferred.
        static readonly Dictionary<string, string> ImportedJoints = new Dictionary<string, string> {
            {"BodyRoot", "mixamorig:Hips"}, {"PerformanceTorso", "mixamorig:Spine"},
            {"PerformanceHead", "mixamorig:Head"},
            {"LeftArm", "mixamorig:LeftArm"}, {"RightArm", "mixamorig:RightArm"},
            {"LeftForearm", "mixamorig:LeftForeArm"}, {"RightForearm", "mixamorig:RightForeArm"},
            {"LeftHand", "mixamorig:LeftHand"}, {"RightHand", "mixamorig:RightHand"},
            {"LeftLeg", "mixamorig:LeftUpLeg"}, {"RightLeg", "mixamorig:RightUpLeg"},
            {"LeftKnee", "mixamorig:LeftLeg"}, {"RightKnee", "mixamorig:RightLeg"},
            {"LeftFoot", "mixamorig:LeftFoot"}, {"RightFoot", "mixamorig:RightFoot"}
        };
        readonly Dictionary<Transform, Quaternion> importedFrames = new Dictionary<Transform, Quaternion>();
        public NativeMotion Motion { get; private set; }
        public bool HasImportedMotion => Motion && Motion.Ready;
        public bool UsesImportedAxes { get; private set; }

        static readonly string[] RequiredJoints = {
            "BodyRoot", "PerformanceTorso", "PerformanceHead",
            "LeftArm", "RightArm", "LeftForearm", "RightForearm",
            "LeftHand", "RightHand", "LeftLeg", "RightLeg",
            "LeftKnee", "RightKnee", "LeftFoot", "RightFoot"
        };

        public Transform Joint(string jointName)
        {
            string importedName;
            ImportedJoints.TryGetValue(jointName, out importedName);
            foreach (var candidate in GetComponentsInChildren<Transform>(true))
                if (candidate.name == jointName || (importedName != null && candidate.name == importedName))
                    return candidate;
            return null;
        }

        public bool ConfigureImportedMotion(string resourcePath, GameObject visual)
        {
            bool actualSkeleton = false;
            foreach (var candidate in visual.GetComponentsInChildren<Transform>(true))
                actualSkeleton |= candidate.name == "mixamorig:Hips";
            if (!actualSkeleton) return false;
            RestoreImportedBindPose(visual);
            UsesImportedAxes = true;
            ValidateJoints();
            foreach (string name in RequiredJoints) {
                Transform joint = Joint(name);
                importedFrames[joint] = Quaternion.Inverse(joint.rotation) * transform.rotation;
            }
            // The default 26 degrees belongs to the earlier hand-authored A pose.
            // Measure this file's actual shoulder-to-elbow rest direction instead.
            Vector3 upperArm = transform.InverseTransformDirection(Joint("LeftForearm").position - Joint("LeftArm").position);
            shoulderRelaxation = Mathf.Atan2(Mathf.Abs(upperArm.x), -upperArm.y) * Mathf.Rad2Deg;
            signWristTurn = 0;
            var animator = visual.GetComponentInChildren<Animator>(true);
            if (!animator) throw new InvalidOperationException("NATIVE_MOTION_ANIMATOR_MISSING " + resourcePath);
            Motion = gameObject.AddComponent<NativeMotion>();
            Motion.Bind(animator, NativeMotion.LoadQualifiedSet(resourcePath));
            return HasImportedMotion;
        }

        static void RestoreImportedBindPose(GameObject visual)
        {
            // The merged FBX defaults to the first running frame. Its skin still
            // retains the genuine authored rest pose in the inverse bind matrices.
            var matrices = new Dictionary<Transform, Matrix4x4>();
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                var bindings = skin.sharedMesh.bindposes; var bones = skin.bones;
                for (int i = 0; i < bindings.Length; i++)
                    if (bones[i]) matrices[bones[i]] = skin.transform.localToWorldMatrix * bindings[i].inverse;
            }
            var ordered = new List<Transform>(matrices.Keys);
            ordered.Sort((a,b) => Depth(a).CompareTo(Depth(b)));
            foreach (var bone in ordered) {
                var local = bone.parent ? bone.parent.worldToLocalMatrix * matrices[bone] : matrices[bone];
                bone.localPosition = local.GetColumn(3); bone.localRotation = local.rotation;
                bone.localScale = local.lossyScale;
            }
        }
        static int Depth(Transform joint) { int depth=0; while(joint.parent) { depth++; joint=joint.parent; } return depth; }

        public Quaternion PoseDelta(Transform joint, Vector3 degrees)
        {
            Quaternion frame;
            if (!UsesImportedAxes || !importedFrames.TryGetValue(joint, out frame)) return Quaternion.Euler(degrees);
            return frame * Quaternion.Euler(degrees) * Quaternion.Inverse(frame);
        }

        public void ValidateJoints()
        {
            foreach (string jointName in RequiredJoints)
                if (!Joint(jointName))
                    throw new InvalidOperationException("NATIVE_CHARACTER_JOINT_MISSING " + name + "/" + jointName);
        }
    }
}
