using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // Anatomical lid deltas transferred by the unchanged skin UVs from the CC0
    // MakeHuman expression targets. Each cast member has their own scale/shape;
    // source FBXs, eye spheres and head silhouette remain untouched. Mouth
    // corners receive only a small separately authored neutral relaxation.
    public static class CharacterFaceMotion
    {
        public const string ShapeName = "Qiaopi natural eyelid closure";
        public const string MouthShapeName = "Qiaopi relaxed mouth corners";
        [Serializable] sealed class MotionFile { public MotionModel[] models; }
        [Serializable] sealed class MotionModel { public string model; public MotionPoint[] points, mouth; }
        [Serializable] sealed class MotionPoint { public float u, v, x, y, z; }
        static readonly Dictionary<string, Dictionary<long, Vector3>> cache = new Dictionary<string, Dictionary<long, Vector3>>();
        static MotionFile source;
        static bool loaded;

        static Dictionary<long, Vector3> Points(string model, bool mouth = false)
        {
            if (string.IsNullOrEmpty(model)) return null;
            string key = model + (mouth ? ":mouth" : ":lids");
            if (cache.TryGetValue(key, out var found)) return found;
            if (!loaded) {
                loaded = true;
                var asset = Resources.Load<TextAsset>("CharacterFaceMotion");
                if (asset) source = JsonUtility.FromJson<MotionFile>(asset.text);
            }
            if (source == null || source.models == null) return null;
            foreach (var item in source.models) if (item.model == model) {
                found = new Dictionary<long, Vector3>();
                var entries = mouth ? item.mouth : item.points; if (entries == null) return null;
                foreach (var p in entries) found[Key(Mathf.RoundToInt(p.u * 100000), Mathf.RoundToInt(p.v * 100000))] = new Vector3(p.x, p.y, p.z);
                cache.Add(key, found); return found;
            }
            return null;
        }
        static long Key(int u, int v) { return ((long)u << 32) ^ (uint)v; }
        static bool Find(Dictionary<long, Vector3> points, Vector2 uv, out Vector3 delta)
        {
            int x = Mathf.RoundToInt(uv.x * 100000), y = Mathf.RoundToInt(uv.y * 100000);
            if (points.TryGetValue(Key(x, y), out delta)) return true;
            // FBX/Unity retain the same UVs but may round on opposite sides of
            // a quantization boundary. Never use a distant nearest face point.
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                if (points.TryGetValue(Key(x + dx, y + dy), out delta)) return true;
            delta = Vector3.zero; return false;
        }

        // Call only on an instance-owned copy made by CharacterPerformance.
        public static int Bind(Mesh mesh, MeshRenderer original, Transform actor, string model)
        {
            int result = BindFrame(mesh, original, actor, model, false);
            BindFrame(mesh, original, actor, model, true);
            return result;
        }
        static int BindFrame(Mesh mesh, MeshRenderer original, Transform actor, string model, bool mouth)
        {
            if (!mesh || !mesh.isReadable || !original || !actor) return -1;
            var points = Points(model, mouth); if (points == null) return -1;
            var uv = mesh.uv; var vertices = mesh.vertices;
            if (uv.Length != vertices.Length) return -1;
            var skin = new bool[vertices.Length]; var lashes = new bool[vertices.Length];
            var materials = original.sharedMaterials;
            for (int slot = 0; slot < Mathf.Min(mesh.subMeshCount, materials.Length); slot++) {
                var material = materials[slot]; if (!material) continue;
                bool face = material.name.StartsWith("QP2_SkinTexture_", StringComparison.Ordinal);
                bool lash = material.name.StartsWith("QP2_Face_FineLash", StringComparison.Ordinal) || material.name.StartsWith("QP2_Face_LidRoot", StringComparison.Ordinal);
                if (!face && !lash) continue;
                foreach (int index in mesh.GetIndices(slot)) { skin[index] |= face; lashes[index] |= lash; }
            }
            var delta = new Vector3[vertices.Length]; var affected = new List<int>();
            Matrix4x4 toMesh = original.transform.worldToLocalMatrix * actor.localToWorldMatrix;
            for (int index = 0; index < vertices.Length; index++) if (skin[index] && Find(points, uv[index], out var local)) {
                delta[index] = toMesh.MultiplyVector(local); affected.Add(index);
            }
            if (affected.Count < 500) {
                Debug.LogWarning("FACE_MOTION_MAPPING_SKIPPED " + model + " matched=" + affected.Count); return -1;
            }
            // Lash roots are separate tiny fibre meshes without anatomical UVs.
            // Carry them with the adjacent lid; leave eyebrow hairs in place.
            Matrix4x4 toActor = actor.worldToLocalMatrix * original.transform.localToWorldMatrix;
            var localVertices = new Vector3[vertices.Length];
            foreach (int index in affected) localVertices[index] = toActor.MultiplyPoint3x4(vertices[index]);
            for (int index = 0; index < vertices.Length; index++) if (!mouth && lashes[index]) {
                Vector3 point = toActor.MultiplyPoint3x4(vertices[index]); float nearest = .0035f * .0035f; int match = -1;
                foreach (int candidate in affected) {
                    float distance = (point - localVertices[candidate]).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; match = candidate; }
                }
                if (match >= 0) delta[index] = delta[match];
            }
            string shape = mouth ? MouthShapeName : ShapeName;
            mesh.AddBlendShapeFrame(shape, 100, delta, null, null);
            return mesh.GetBlendShapeIndex(shape);
        }

        public static float Weight(string model, float seconds)
        {
            float identity = CharacterPresentationPolish.RestingPhase(model);
            float period = 3.8f + .7f * Mathf.Sin(identity * 1.7f);
            float at = Mathf.Repeat(seconds + 1.4f + identity * .53f, period);
            float blink = at < .065f ? Mathf.SmoothStep(0, 1, at / .065f)
                : at < .085f ? 1 : at < .205f ? 1 - Mathf.SmoothStep(0, 1, (at - .085f) / .12f) : 0;
            float relaxed = model == "Wife" || model == "Aman" ? .035f : .02f;
            return Mathf.Lerp(relaxed, 1, blink) * 100;
        }
    }
}
