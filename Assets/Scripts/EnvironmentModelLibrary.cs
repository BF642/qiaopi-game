using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace Qiaopi
{
    [Serializable]
    public sealed class EnvironmentModelReview
    {
        public int schema = 1;
        public string assetId, geometrySha256;
        public bool approved, materialsReviewed, groundLevelReviewed, roofJoinReviewed;
        public string reviewNote;
    }

    /// <summary>The three reviewed decorative assets. Gameplay geometry is authored separately.</summary>
    public static class EnvironmentModelLibrary
    {
        public const string WritingDesk = "WritingDesk", HomeEntry = "QuanzhouHomeEntry", OfficeEntry = "QiaopiOfficeEntry";
        public static readonly Vector3 DeskPosition = new Vector3(0, 0, .35f);
        public static readonly Vector3 EntryPosition = new Vector3(0, 0, -2.07f);
        // Identical to the existing posts' inner edges. A candidate must have an
        // actual empty passage, not merely omit collision from an opaque wall.
        public const float PassageWidth = 2.17f, PassageHeight = 2.20f;
        sealed class Entry { public GameObject model; public string reason; }
        static readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>();

        public static bool Known(string id) => id == WritingDesk || id == HomeEntry || id == OfficeEntry;
        public static Vector3 ExpectedSize(string id) => id == WritingDesk ? new Vector3(1.75f, .78f, .82f) :
            id == HomeEntry ? new Vector3(4.2f, 3.4f, .65f) : new Vector3(4.8f, 3.7f, 1.4f);
        public static string ResourcePath(string id) => "EnvironmentArt/" + id + "/Model";
        public static bool IsReady(string id) => Load(id).model;
        public static string UnavailableReason(string id) => Load(id).reason;

        static Entry Load(string id)
        {
            if (!Known(id)) return new Entry { reason = "Unknown environment asset" };
            if (cache.TryGetValue(id, out var found)) return found;
            var entry = new Entry(); cache[id] = entry;
            var model = Resources.Load<GameObject>(ResourcePath(id));
            var text = Resources.Load<TextAsset>("EnvironmentArt/" + id + "/review");
            if (!model || !text) { entry.reason = "Model or review missing; authored scenery retained"; return entry; }
            try {
                var review = JsonUtility.FromJson<EnvironmentModelReview>(text.text);
                if (ValidateCandidate(id, model, review, out string reason)) entry.model = model;
                else entry.reason = reason;
            } catch (Exception error) { entry.reason = "Invalid review: " + error.Message; }
            if (!entry.model) Debug.LogWarning("ENVIRONMENT_ART_FALLBACK " + id + " " + entry.reason);
            return entry;
        }

        public static bool TrySpawn(string id, Transform parent, Vector3 position, Quaternion rotation, out GameObject instance)
        {
            instance = null; var entry = Load(id); if (!entry.model || !parent) return false;
            instance = UnityEngine.Object.Instantiate(entry.model, parent, false);
            instance.name = "Reviewed environment · " + id;
            instance.transform.localPosition = position; instance.transform.localRotation = rotation;
            instance.transform.localScale = Vector3.one;
            return true;
        }

        public static bool ValidateCandidate(string id, GameObject model, EnvironmentModelReview review, out string reason)
        {
            reason = null;
            if (!Known(id) || !model || review == null || review.schema != 1 || review.assetId != id ||
                !review.approved || !review.materialsReviewed || !review.groundLevelReviewed)
                return Fail("Missing completed art/ground review", out reason);
            if (id != WritingDesk && !review.roofJoinReviewed) return Fail("Roof join not reviewed", out reason);
            if (model.GetComponentsInChildren<Collider>(true).Length != 0 || model.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                model.GetComponentsInChildren<Animator>(true).Length != 0 || model.GetComponentsInChildren<Animation>(true).Length != 0 ||
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0 || model.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                return Fail("Decorative model must contain only static art, without gameplay components", out reason);
            if (!TryBounds(model, out Bounds bounds)) return Fail("No readable visible static geometry", out reason);
            Vector3 expected = ExpectedSize(id), tolerance = id == WritingDesk ? new Vector3(.025f,.008f,.025f) : new Vector3(.10f,.10f,.06f);
            Vector3 size = bounds.size;
            if (Mathf.Abs(size.x-expected.x)>tolerance.x || Mathf.Abs(size.y-expected.y)>tolerance.y || Mathf.Abs(size.z-expected.z)>tolerance.z ||
                Mathf.Abs(bounds.center.x)>.012f || Mathf.Abs(bounds.center.z)>.012f || Mathf.Abs(bounds.min.y)>.008f)
                return Fail("Model must be calibrated in metres, centred X/Z and resting on y=0", out reason);
            if (id == WritingDesk) {
                if (Mathf.Abs(bounds.max.y-.78f)>.008f) return Fail("Desk top would move the paper/ink surface", out reason);
            } else {
                // The existing paving is already raised slightly above y=0. New
                // thresholds must be flush, never a high plinth hidden by no collider.
                var passage = new Bounds(new Vector3(0, 1.1175f, 0), new Vector3(PassageWidth, 2.165f, size.z+.10f));
                if (IntersectsGeometry(model, passage)) return Fail("Visible geometry blocks the doorway or raises its threshold", out reason);
            }
            if (string.IsNullOrEmpty(review.geometrySha256) || GeometrySignature(model) != review.geometrySha256)
                return Fail("Reviewed geometry no longer matches this model", out reason);
            return true;
        }

        static bool Fail(string value, out string reason) { reason=value; return false; }
        public static bool TryBounds(GameObject model, out Bounds bounds)
        {
            bounds = new Bounds(); bool has=false;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true)) {
                var renderer=filter.GetComponent<MeshRenderer>(); var mesh=filter.sharedMesh;
                if (!renderer || !renderer.enabled || !mesh || !mesh.isReadable || !filter.gameObject.activeSelf) return false;
                if (renderer.sharedMaterials.Length==0) return false;
                foreach(var material in renderer.sharedMaterials) if(!material||!material.shader) return false;
                Matrix4x4 m=model.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                foreach(Vector3 v in mesh.vertices) { Vector3 p=m.MultiplyPoint3x4(v);if(!has){bounds=new Bounds(p,Vector3.zero);has=true;}else bounds.Encapsulate(p); }
            }
            return has;
        }

        public static string GeometrySignature(GameObject model)
        {
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream)) {
                foreach(var f in model.GetComponentsInChildren<MeshFilter>(true)) {
                    var mesh=f.sharedMesh;if(!mesh||!mesh.isReadable)return "";
                    Matrix4x4 m=model.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;
                    for(int i=0;i<16;i++)writer.Write(m[i]);
                    writer.Write(mesh.vertexCount);
                    foreach(Vector3 v in mesh.vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
                    writer.Write(mesh.subMeshCount);
                    for(int s=0;s<mesh.subMeshCount;s++){int[] indices=mesh.GetTriangles(s);writer.Write(indices.Length);foreach(int i in indices)writer.Write(i);}
                }
                writer.Flush();using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();
            }
        }

        public static bool IntersectsGeometry(GameObject model, Bounds box)
        {
            foreach(var f in model.GetComponentsInChildren<MeshFilter>(true)) {
                Mesh mesh=f.sharedMesh;if(!mesh||!mesh.isReadable)return true;
                Matrix4x4 m=model.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;Vector3[] v=mesh.vertices;int[] t=mesh.triangles;
                for(int i=0;i<t.Length;i+=3)
                    if(TriangleTouchesBox(m.MultiplyPoint3x4(v[t[i]]),m.MultiplyPoint3x4(v[t[i+1]]),m.MultiplyPoint3x4(v[t[i+2]]),box))return true;
            }
            return false;
        }
        static bool TriangleTouchesBox(Vector3 a,Vector3 b,Vector3 c,Bounds box)
        {
            a-=box.center;b-=box.center;c-=box.center;Vector3 ext=box.extents;
            Vector3[] axes={Vector3.right,Vector3.up,Vector3.forward};Vector3[] edges={b-a,c-b,a-c};
            foreach(var axis in axes)if(Separated(a,b,c,ext,axis))return false;
            if(Separated(a,b,c,ext,Vector3.Cross(b-a,c-a)))return false;
            foreach(var edge in edges)foreach(var axis in axes)if(Separated(a,b,c,ext,Vector3.Cross(edge,axis)))return false;
            return true;
        }
        static bool Separated(Vector3 a,Vector3 b,Vector3 c,Vector3 ext,Vector3 axis)
        {
            if(axis.sqrMagnitude<1e-16f)return false;
            float radius=ext.x*Mathf.Abs(axis.x)+ext.y*Mathf.Abs(axis.y)+ext.z*Mathf.Abs(axis.z);
            float x=Vector3.Dot(a,axis),y=Vector3.Dot(b,axis),z=Vector3.Dot(c,axis);
            return Mathf.Min(x,Mathf.Min(y,z))>radius || Mathf.Max(x,Mathf.Max(y,z))<-radius;
        }

#if UNITY_EDITOR
        public static void ClearReviewCache() { cache.Clear(); }
#endif
    }
}
