using System;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class SeatedWritingPostureChecks
    {
        public static void Run()
        {
            try { CheckAll(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void CheckWifeAndBuild()
        {
            try {
                CheckAll();
                var actor = ModelLibrary.Spawn("Wife", null, Vector3.zero);
                try { NativeMotionChecks.Check(actor); }
                finally { UnityEngine.Object.DestroyImmediate(actor); }
                BuildMeshyCharacterStudy.Build();
            } catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void CheckAll()
        {
                foreach (string role in new[] { "Worker", "Aman", "Wife" }) {
                    GameObject actor = ModelLibrary.Spawn(role, null, Vector3.zero);
                    try {
                        var rig = actor.GetComponent<NativeCharacterRig>();
                        if (!rig || !rig.HasImportedMotion) throw new Exception("WRITING_REQUIRES_NATIVE " + role);
                        var performance = CharacterPerformance.For(actor);
                        performance.Automatic = false;
                        performance.ResetPose();
                        performance.SetSeated(true, role == "Aman" ? .36f : .43f);
                        for (int i = 0; i < 60; i++) performance.Step(1f / 30);
                        performance.Play(CharacterGesture.Sign, 2.6f);
                        float minimumForward = float.PositiveInfinity;
                        for (int i = 0; i < 60; i++) {
                            performance.Step(1f / 30);
                            if (i < 20) continue;
                            Vector3 relative = actor.transform.InverseTransformVector(
                                rig.Joint("PerformanceHead").position - rig.Joint("BodyRoot").position);
                            minimumForward = Mathf.Min(minimumForward, relative.z);
                            float stature = ModelLibrary.CharacterHeight(role) / 1.74f;
                            if (relative.z < -.025f * stature || relative.y < .2f * stature)
                                throw new Exception("WRITER_LEANS_BACK " + role + " headFromHip=" + relative.ToString("F4"));
                            if (Vector3.Distance(performance.LeftHandPoint, performance.RightHandPoint) > .40f)
                                throw new Exception("WRITING_HANDS_SEPARATED " + role);
                        }
                        Debug.Log("SEATED_WRITING_POSTURE_PASS role=" + role + " minimumHeadForward=" + minimumForward);
                    } finally { UnityEngine.Object.DestroyImmediate(actor); }
                }

        }
    }
}
