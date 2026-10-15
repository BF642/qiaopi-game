using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Qiaopi.Editor
{
    // Inspect each source take independently. No locomotion mixer, task driver,
    // retargeting or baked Blender action participates in this qualification.
    public static class NativeMotionSourceChecks
    {
        [Serializable] public sealed class CurveEvidence
        {
            public string path, property;
            public int keys;
            public float firstTime, lastTime, minimum, maximum, span;
        }
        [Serializable] public sealed class ClipEvidence
        {
            public string name, asset, take;
            public float duration, leftKneeRange, rightKneeRange, maximumBoneRotationRange;
            public float minimumKneeFlexion = 180, maximumKneeFlexion, minimumHipFlexion = 180, maximumHipFlexion;
            public int curveCount, varyingCurves;
            public bool qualifies;
            [NonSerialized] public AnimationClip sourceClip;
            public List<CurveEvidence> curves = new List<CurveEvidence>();
        }
        [Serializable] public sealed class SourceEvidence
        {
            public string role, prefab, selectedProductionResource;
            public bool completeNames, qualifiedCompleteSet, productionAdmitted;
            public List<ClipEvidence> clips = new List<ClipEvidence>();
        }

        public static void VerifyStaging()
        {
            try {
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-refresh-native-textures") >= 0)
                    ModelArtImport.RefreshNativeTexturePlatforms();
                foreach (string role in new[] { "Wensheng", "Wife", "Mother" }) {
                    var actor = ModelLibrary.Spawn(role, null, Vector3.zero);
                    try {
                        var rig = actor.GetComponent<NativeCharacterRig>();
                        if (rig && rig.HasImportedMotion) NativeMotionChecks.Check(actor);
                        else NativeCharacterChecks.Check(actor);
                    } finally { Object.DestroyImmediate(actor); }
                }
                var source = Inspect(RoleArgument());
                if (!source.qualifiedCompleteSet && source.productionAdmitted)
                    throw new InvalidOperationException("UNQUALIFIED_MOTION_ADMITTED " + source.role);
                Debug.Log("NATIVE_MOTION_STAGING_PASSED oldThree=true sourceComplete=" + source.qualifiedCompleteSet +
                    " productionAdmitted=" + source.productionAdmitted);
                EditorApplication.Exit(0);
            } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void ReportSource()
        {
            try {
                var report = Inspect(RoleArgument());
                EditorApplication.Exit(report.qualifiedCompleteSet ? 0 : 2);
            } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void QualifyRole()
        {
            try {
                var report = Inspect(RoleArgument());
                string folder = Path.GetDirectoryName(report.prefab).Replace('\\', '/');
                string asset = folder + "/NativeMotionSet.asset";
                var set = AssetDatabase.LoadAssetAtPath<NativeMotionSet>(asset);
                if (!set) { set = ScriptableObject.CreateInstance<NativeMotionSet>(); AssetDatabase.CreateAsset(set, asset); }
                set.role = report.role; set.model = AssetDatabase.LoadAssetAtPath<GameObject>(report.prefab);
                // This is a documented per-role adaptation, not a claim that
                // the official animation is unchanged. Only apply on request
                // after the corresponding close-up comparison was inspected.
                var arguments = Environment.GetCommandLineArgs();
                int fingerArgument = Array.IndexOf(arguments, "-native-reviewed-finger-weight");
                if (fingerArgument >= 0 && fingerArgument + 1 < arguments.Length) {
                    set.fingerAnimationWeight = Mathf.Clamp01(float.Parse(arguments[fingerArgument + 1], System.Globalization.CultureInfo.InvariantCulture));
                    set.fingerAdaptationReview = "Source rest / official Idle_4 / relative-rest finger constraint close-ups reviewed; body curves, mesh and original weights preserved.";
                }
                if (Array.IndexOf(arguments,"-native-reviewed-contacts")>=0) set.reviewedContactAndWristAdaptation=true;
                if (Array.IndexOf(arguments,"-native-reviewed-seat-contacts")>=0) set.reviewedSeatAndGroundAdaptation=true;
                int strideArgument=Array.IndexOf(arguments,"-native-measured-stride");
                if(strideArgument>=0 && strideArgument+2<arguments.Length) {
                    set.walkingReferenceMetresPerSecond=float.Parse(arguments[strideArgument+1],System.Globalization.CultureInfo.InvariantCulture);
                    set.runningReferenceMetresPerSecond=float.Parse(arguments[strideArgument+2],System.Globalization.CultureInfo.InvariantCulture);
                }
                set.qualificationVersion = NativeMotionSet.CurrentVersion; set.qualified = report.qualifiedCompleteSet;
                set.idle = set.walking = set.running = set.seated = null;
                var dependencies = new HashSet<string> { report.prefab };
                if(set.reviewedSkin) dependencies.Add(AssetDatabase.GetAssetPath(set.reviewedSkin));
                if(set.reviewedSkin && AssetDatabase.LoadAssetAtPath<Material>(folder+"/ReviewedMaterial.mat"))
                    dependencies.Add(folder+"/ReviewedMaterial.mat");
                foreach (var row in report.clips) if (row.qualifies) {
                    bool used = true;
                    if (NativeMotion.IsIdleTake(row.take) && !set.idle) set.idle = row.sourceClip;
                    else if (row.take == WalkTakeArgument() && !set.walking) set.walking = row.sourceClip;
                    else if (row.take == "Running" && !set.running) set.running = row.sourceClip;
                    else if (row.take == SeatTakeArgument() && !set.seated) set.seated = row.sourceClip;
                    else used = false;
                    if (used) dependencies.Add(row.asset);
                }
                var paths = new List<string>(dependencies); paths.Sort(StringComparer.Ordinal);
                set.sourceAssets = paths.ToArray(); set.sourceDependencyHashes = new string[paths.Count];
                for (int i = 0; i < paths.Count; i++) set.sourceDependencyHashes[i] = AssetDatabase.GetAssetDependencyHash(paths[i]).ToString();
                EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
                Debug.Log("NATIVE_MOTION_QUALIFICATION role=" + report.role + " admitted=" + set.IsQualified +
                    " seat=" + (set.seated ? set.seated.name : "unavailable"));
                EditorApplication.Exit(set.IsQualified ? 0 : 2);
            } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static SourceEvidence Inspect(string role)
        {
            if (ModelLibrary.CharacterHeight(role) <= 0) throw new ArgumentException("Unknown role " + role);
            string resource = "Models/" + role + "MeshyMotion/" + role + "MeshyMotion";
            var prefab = Resources.Load<GameObject>(resource);
            if (!prefab) throw new InvalidOperationException("SOURCE_PREFAB_MISSING " + resource);
            var clips = NativeMotion.LoadImportedClips(resource);
            // Prefer individually verified extracted takes to any duplicate
            // embedded takes retained in the untouched model FBX.
            Array.Sort(clips, (a, b) => {
                bool aa = AssetDatabase.GetAssetPath(a).EndsWith(".anim", StringComparison.Ordinal);
                bool bb = AssetDatabase.GetAssetPath(b).EndsWith(".anim", StringComparison.Ordinal);
                return aa == bb ? string.CompareOrdinal(a.name, b.name) : aa ? -1 : 1;
            });
            var report = new SourceEvidence { role = role, prefab = AssetDatabase.GetAssetPath(prefab),
                selectedProductionResource = ModelLibrary.MeshyResource(role), completeNames = NativeMotion.HasCompleteSet(clips, WalkTakeArgument()) };
            report.productionAdmitted = report.selectedProductionResource == resource;
            var model = Object.Instantiate(prefab);
            try {
                var animator = model.GetComponentInChildren<Animator>(true);
                if (!animator) throw new InvalidOperationException("SOURCE_ANIMATOR_MISSING");
                animator.enabled = false;
                var joints = animator.GetComponentsInChildren<Transform>(true);
                var byName = new Dictionary<string, Transform>();
                foreach (var joint in joints) byName[joint.name] = joint;
                var positions = new Vector3[joints.Length]; var scales = new Vector3[joints.Length];
                var rotations = new Quaternion[joints.Length]; var initial = new Quaternion[joints.Length];
                for (int j = 0; j < joints.Length; j++) {
                    positions[j] = joints[j].localPosition; scales[j] = joints[j].localScale; rotations[j] = joints[j].localRotation;
                }
                foreach (var clip in clips) {
                    if (clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                    var row = new ClipEvidence { name = clip.name, asset = AssetDatabase.GetAssetPath(clip),
                        duration = clip.length, take = clip.name.Substring(clip.name.LastIndexOf('|') + 1), sourceClip = clip };
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var values = curve.keys;
                        if (values.Length == 0) continue;
                        var c = new CurveEvidence { path = binding.path, property = binding.propertyName, keys = values.Length,
                            firstTime = values[0].time, lastTime = values[values.Length - 1].time,
                            minimum = float.PositiveInfinity, maximum = float.NegativeInfinity };
                        foreach (var key in values) { c.minimum = Mathf.Min(c.minimum, key.value); c.maximum = Mathf.Max(c.maximum, key.value); }
                        c.span = c.maximum - c.minimum;
                        if (c.span > .00001f) row.varyingCurves++;
                        row.curves.Add(c);
                    }
                    row.curveCount = row.curves.Count;
                    for (int sample = 0; sample <= 32; sample++) {
                        for (int j = 0; j < joints.Length; j++) {
                            joints[j].localPosition = positions[j]; joints[j].localRotation = rotations[j]; joints[j].localScale = scales[j];
                        }
                        clip.SampleAnimation(animator.gameObject, clip.length * sample / 32f);
                        for (int j = 0; j < joints.Length; j++) {
                            if (sample == 0) initial[j] = joints[j].localRotation;
                            float angle = Quaternion.Angle(initial[j], joints[j].localRotation);
                            if (!float.IsFinite(angle)) throw new InvalidOperationException("NONFINITE_SOURCE_CLIP " + clip.name);
                            row.maximumBoneRotationRange = Mathf.Max(row.maximumBoneRotationRange, angle);
                            if (joints[j].name == "mixamorig:LeftLeg") row.leftKneeRange = Mathf.Max(row.leftKneeRange, angle);
                            if (joints[j].name == "mixamorig:RightLeg") row.rightKneeRange = Mathf.Max(row.rightKneeRange, angle);
                        }
                        foreach (string side in new[] { "Left", "Right" }) {
                            var thigh = byName["mixamorig:" + side + "UpLeg"];
                            var knee = byName["mixamorig:" + side + "Leg"];
                            var ankle = byName["mixamorig:" + side + "Foot"];
                            var pelvis = byName["mixamorig:Hips"]; var spine = byName["mixamorig:Spine"];
                            float kneeFlex = 180 - Vector3.Angle(thigh.position - knee.position, ankle.position - knee.position);
                            float hipFlex = 180 - Vector3.Angle(spine.position - pelvis.position, knee.position - thigh.position);
                            row.minimumKneeFlexion = Mathf.Min(row.minimumKneeFlexion, kneeFlex);
                            row.maximumKneeFlexion = Mathf.Max(row.maximumKneeFlexion, kneeFlex);
                            row.minimumHipFlexion = Mathf.Min(row.minimumHipFlexion, hipFlex);
                            row.maximumHipFlexion = Mathf.Max(row.maximumHipFlexion, hipFlex);
                        }
                    }
                    float kneeRange = Mathf.Max(row.leftKneeRange, row.rightKneeRange);
                    row.qualifies = row.duration > .1f && row.varyingCurves > 0 && row.maximumBoneRotationRange > .1f;
                    if (row.take == WalkTakeArgument()) row.qualifies &= kneeRange > 8;
                    if (row.take == "Running") row.qualifies &= kneeRange > 12;
                    // An intentionally quiet standing idle is valid. Breathing
                    // and eye motion are optional, but a frozen running pose is not.
                    if (NativeMotion.IsIdleTake(row.take))
                        row.qualifies = row.duration > .1f && row.maximumKneeFlexion < 35 && row.maximumHipFlexion < 35;
                    if (row.take == SeatTakeArgument())
                        row.qualifies = row.duration > .1f && row.minimumKneeFlexion > 45 && row.maximumKneeFlexion < 140 &&
                            row.minimumHipFlexion > 40 && row.maximumHipFlexion < 135;
                    report.clips.Add(row);
                    Debug.Log("NATIVE_SOURCE_CLIP role=" + role + " take=" + row.take + " curves=" + row.curveCount +
                        " varying=" + row.varyingCurves + " knee=" + kneeRange + " qualifies=" + row.qualifies);
                }
            } finally { Object.DestroyImmediate(model); }
            bool idle = false, walk = false, run = false;
            foreach (var clip in report.clips) if (clip.qualifies) {
                idle |= NativeMotion.IsIdleTake(clip.take);
                walk |= clip.take == WalkTakeArgument(); run |= clip.take == "Running";
            }
            report.qualifiedCompleteSet = idle && walk && run;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/characters-v282/native-pipeline", role));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "unity-source-clip-evidence.json"), JsonUtility.ToJson(report, true));
            return report;
        }

        static string RoleArgument()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-native-role") return args[i + 1];
            return "Worker";
        }
        // The male name was observed in the downloaded Worker file. For another
        // actual export, pass its inspected take name instead of guessing it.
        static string WalkTakeArgument()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-native-walk-take") return args[i + 1];
            return "Walking";
        }

        static string SeatTakeArgument()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-native-seat-take") return args[i + 1];
            return "Chair_Sit_Idle_M";
        }
    }
}
