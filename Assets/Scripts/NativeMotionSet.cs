using UnityEngine;

namespace Qiaopi
{
    // Written only by the source-clip qualification step. A named take alone is
    // not sufficient: Meshy exports can contain a frozen pose under Walking.
    public sealed class NativeMotionSet : ScriptableObject
    {
        public const int CurrentVersion = 1;
        public int qualificationVersion;
        public bool qualified;
        public bool directImport; // User-requested direct import, without quality qualification.
        public string role;
        public GameObject model;
        public Mesh reviewedSkin;
        public string reviewedSkinSourceName;
        public AnimationClip idle, walking, running, seated;
        [Range(0, 1)] public float fingerAnimationWeight = 1;
        public string fingerAdaptationReview;
        public bool reviewedContactAndWristAdaptation;
        public bool reviewedSeatAndGroundAdaptation;
        public float walkingReferenceMetresPerSecond, runningReferenceMetresPerSecond;
        public string[] sourceAssets, sourceDependencyHashes;

        public bool IsQualified {
            get {
                if ((!qualified && !directImport) || qualificationVersion != CurrentVersion || !model || !idle || !walking || !running) return false;
                // Direct imports deliberately bypass the old art qualification gate.
                // Unity dependency hashes vary by Editor platform/import settings;
                // moving the same sources to Windows must not restore old characters.
                if (directImport) return true;
#if UNITY_EDITOR
                // Editing/replacing the source invalidates the old qualification
                // even when its .meta GUID and take names did not change.
                if (sourceAssets == null || sourceDependencyHashes == null || sourceAssets.Length != sourceDependencyHashes.Length || sourceAssets.Length == 0) return false;
                for (int i = 0; i < sourceAssets.Length; i++)
                    if (UnityEditor.AssetDatabase.GetAssetDependencyHash(sourceAssets[i]).ToString() != sourceDependencyHashes[i]) return false;
#endif
                return true;
            }
        }

        public AnimationClip[] Clips() { return new[] { idle, walking, running, seated }; }
    }
}
