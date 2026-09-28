using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    /// <summary>
    /// Continuous, texture-free sea for the two ports and the passenger ship.
    /// This component owns its generated meshes/materials and creates no colliders.
    /// Coordinates are relative to the WorldFactory root; the ship's bow faces +z.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class SeaEnvironment : MonoBehaviour
    {
        const float SeaLevel = -.66f;
        const float SeaWidth = 2600f;
        const int SeaDivisions = 160;
        static readonly int SeaTimeId = Shader.PropertyToID("_SeaTime");
        static readonly int HorizonId = Shader.PropertyToID("_HorizonColor");
        static readonly int FogRangeId = Shader.PropertyToID("_SeaFogRange");
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Material> materials = new List<Material>();
        GameObject geometry;
        Material surfaceMaterial;
        string seaKind;
        bool releasing;

        /// <summary>Returns null for a non-maritime place. Repeated calls are idempotent.</summary>
        public static SeaEnvironment Create(GameObject world, string kind)
        {
            if (!world || (kind != "harbor" && kind != "port" && kind != "ship")) return null;
            var sea = world.GetComponent<SeaEnvironment>();
            if (sea && sea.geometry && sea.seaKind == kind) return sea;
            if (!sea) sea = world.AddComponent<SeaEnvironment>();
            sea.Initialize(kind);
            return sea;
        }

        /// <summary>
        /// Match the sky's horizon without making the nearby water uniformly foggy.
        /// Sea fog defaults to 300–850 metres; setting overrideSceneFog=false follows
        /// the normal Unity fog range instead, while retaining distant horizon tint.
        /// </summary>
        public void SetHorizon(Color color, float start = 300f, float end = 850f, bool overrideSceneFog = true)
        {
            if (!surfaceMaterial) return;
            surfaceMaterial.SetColor(HorizonId, color);
            surfaceMaterial.SetVector(FogRangeId, new Vector4(Mathf.Max(0, start), Mathf.Max(start + 1, end), overrideSceneFog ? 1 : 0, 0));
        }

        void Initialize(string kind)
        {
            Release();
            seaKind = kind;
            geometry = new GameObject("Continuous sea · " + kind);
            geometry.transform.SetParent(transform, false);

            // Loading directly from Resources keeps the shader in player builds.
            Shader shader = Resources.Load<Shader>("SeaSurface");
            if (!shader) shader = Shader.Find("Qiaopi/SeaSurface");
            if (!shader)
            {
                Debug.LogError("SeaEnvironment requires Resources/SeaSurface.shader.", this);
                Release();
                return;
            }
            surfaceMaterial = Own(new Material(shader) { name = "Flowing sea · " + kind });
            surfaceMaterial.SetFloat("_SeaKind", kind == "ship" ? 2 : kind == "port" ? 1 : 0);
            surfaceMaterial.SetColor("_DeepColor", Hex(kind == "ship" ? "205F78" : "2E6977"));
            surfaceMaterial.SetColor("_WaterColor", Hex(kind == "ship" ? "3B8592" : "568D91"));
            surfaceMaterial.SetColor("_ShallowColor", Hex("86ADA2"));
            surfaceMaterial.SetColor("_FoamColor", Hex("E5E9D9"));
            surfaceMaterial.SetColor("_GlintColor", Hex("F3EBD0"));
            surfaceMaterial.SetFloat("_GlintStrength", kind == "ship" ? .62f : .48f);
            surfaceMaterial.SetFloat("_WaveStrength", kind == "ship" ? 1f : .76f);
            surfaceMaterial.SetFloat("_SeaTime", 0);
            SetHorizon(Hex("9DAFB5"));
            AddMesh("2600 metre continuous water", BuildSurface(), surfaceMaterial);
            if (kind == "ship") BuildDistantSails();
        }

        Mesh BuildSurface()
        {
            int stride = SeaDivisions + 1;
            var vertices = new Vector3[stride * stride];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[SeaDivisions * SeaDivisions * 6];
            for (int z = 0; z < stride; z++) for (int x = 0; x < stride; x++)
            {
                int index = z * stride + x;
                vertices[index] = new Vector3(((float)x / SeaDivisions - .5f) * SeaWidth, SeaLevel,
                    ((float)z / SeaDivisions - .5f) * SeaWidth);
                normals[index] = Vector3.up;
                uv[index] = new Vector2((float)x / SeaDivisions, (float)z / SeaDivisions);
            }
            int at = 0;
            for (int z = 0; z < SeaDivisions; z++) for (int x = 0; x < SeaDivisions; x++)
            {
                int a = z * stride + x, b = a + 1, c = a + stride, d = c + 1;
                triangles[at++] = a; triangles[at++] = c; triangles[at++] = b;
                triangles[at++] = b; triangles[at++] = c; triangles[at++] = d;
            }
            var mesh = new Mesh { name = "Unbroken sea grid", hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles;
            mesh.bounds = new Bounds(new Vector3(0, SeaLevel, 0), new Vector3(SeaWidth, .2f, SeaWidth));
            // Vertex displacement is bounded to +/- .026 m: the water remains below
            // the -.6 m hull bottom, and well below all y=0 walking surfaces.
            mesh.UploadMeshData(true);
            meshes.Add(mesh);
            return mesh;
        }

        void AddMesh(string label, Mesh mesh, Material material)
        {
            var go = new GameObject(label);
            go.transform.SetParent(geometry.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        void BuildDistantSails()
        {
            // Two small, batched vessel silhouettes belong only to the open-sea view.
            // No islands or shoreline geometry compete with the port backdrops.
            var hullVertices = new List<Vector3>(); var hullTriangles = new List<int>();
            var sailVertices = new List<Vector3>(); var sailTriangles = new List<int>();
            DistantBoat(hullVertices, hullTriangles, sailVertices, sailTriangles, new Vector3(-100, SeaLevel, 160), 1, 32);
            DistantBoat(hullVertices, hullTriangles, sailVertices, sailTriangles, new Vector3(145, SeaLevel, 260), 1.22f, -24);
            Material hull = SilhouetteMaterial("Distant hull and mast", Hex("52636A"));
            Material sail = SilhouetteMaterial("Distant weathered sailcloth", Hex("B4BAAC"));
            if (hull) AddMesh("Distant working boats · hulls", OwnMesh("Far hull geometry", hullVertices, hullTriangles), hull);
            if (sail) AddMesh("Distant working boats · sails", OwnMesh("Far sail geometry", sailVertices, sailTriangles), sail);
        }

        Material SilhouetteMaterial(string label, Color color)
        {
            var template = Resources.Load<Material>("WorldMaterial");
            Shader standard = template ? template.shader : Shader.Find("Standard");
            if (!standard) return null;
            var material = Own(template ? new Material(template) : new Material(standard));
            material.name = label; material.color = color;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .02f);
            return material;
        }

        static void DistantBoat(List<Vector3> hv, List<int> ht, List<Vector3> sv, List<int> st, Vector3 origin, float scale, float angle)
        {
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            // The long axis of these distant craft is x, independent of our +z ship.
            Vector3[] lower = { new Vector3(-5, .14f, 0), new Vector3(-3.5f, -.18f, -1), new Vector3(3.8f, -.18f, -1),
                new Vector3(5.2f, .12f, 0), new Vector3(3.8f, -.18f, 1), new Vector3(-3.5f, -.18f, 1) };
            Vector3[] upper = { new Vector3(-5.3f, .85f, 0), new Vector3(-3.9f, .66f, -1.35f), new Vector3(4.1f, .66f, -1.35f),
                new Vector3(5.6f, .86f, 0), new Vector3(4.1f, .66f, 1.35f), new Vector3(-3.9f, .66f, 1.35f) };
            for (int i = 0; i < lower.Length; i++)
            {
                int next = (i + 1) % lower.Length;
                Face(hv, ht, origin + rotation * (lower[i] * scale), origin + rotation * (upper[i] * scale),
                    origin + rotation * (upper[next] * scale), origin + rotation * (lower[next] * scale), false);
            }
            for (int i = 1; i < upper.Length - 1; i++)
                Triangle(hv, ht, origin + rotation * (upper[0] * scale), origin + rotation * (upper[i] * scale), origin + rotation * (upper[i + 1] * scale), false);
            foreach (float mastX in new[] { -1.5f, 2.4f })
            {
                float height = mastX < 0 ? 8.8f : 6.6f;
                Vector3 mast = new Vector3(mastX, .72f, 0);
                ThinBeam(hv, ht, origin, rotation, scale, mast, mast + Vector3.up * height, .14f);
                Vector3 a = mast + new Vector3(.17f, .7f, .05f);
                Vector3 b = mast + new Vector3(.17f, height - .3f, .05f);
                Vector3 c = mast + new Vector3(-3.4f, height - 1.6f, .5f);
                Vector3 d = mast + new Vector3(-3.9f, 1.2f, .4f);
                Face(sv, st, origin + rotation * (a * scale), origin + rotation * (b * scale), origin + rotation * (c * scale), origin + rotation * (d * scale), true);
                for (int rib = 1; rib <= 4; rib++)
                {
                    float u = rib / 5f;
                    ThinBeam(hv, ht, origin, rotation, scale, Vector3.Lerp(a, b, u), Vector3.Lerp(d, c, u), .055f);
                }
            }
        }

        static void ThinBeam(List<Vector3> v, List<int> t, Vector3 origin, Quaternion rotation, float scale, Vector3 a, Vector3 b, float width)
        {
            Vector3 side = Vector3.Cross((b - a).normalized, Vector3.forward) * width * .5f;
            Face(v, t, origin + rotation * ((a - side) * scale), origin + rotation * ((b - side) * scale),
                origin + rotation * ((b + side) * scale), origin + rotation * ((a + side) * scale), true);
        }

        static void Face(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool doubleSided)
        {
            Triangle(v, t, a, b, c, doubleSided); Triangle(v, t, a, c, d, doubleSided);
        }

        static void Triangle(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, bool doubleSided)
        {
            int first = v.Count; v.Add(a); v.Add(b); v.Add(c);
            t.Add(first); t.Add(first + 1); t.Add(first + 2);
            if (!doubleSided) return;
            // Separate back vertices preserve proper normals for Standard lighting.
            first = v.Count; v.Add(c); v.Add(b); v.Add(a);
            t.Add(first); t.Add(first + 1); t.Add(first + 2);
        }

        Mesh OwnMesh(string label, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = label, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh.UploadMeshData(true); meshes.Add(mesh); return mesh;
        }

        Material Own(Material material)
        {
            material.hideFlags = HideFlags.DontSave; materials.Add(material); return material;
        }

        static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString("#" + html, out Color color); return color;
        }

        void Update()
        {
            if (!surfaceMaterial) return;
            // Editor previews remain deterministic and do not register editor callbacks.
            surfaceMaterial.SetFloat(SeaTimeId, Application.isPlaying ? Time.time : 0);
        }

        void OnDestroy() { Release(); }

        void Release()
        {
            if (releasing) return;
            releasing = true;
            if (geometry) { geometry.SetActive(false); Dispose(geometry); geometry = null; }
            foreach (var mesh in meshes) if (mesh) Dispose(mesh);
            foreach (var material in materials) if (material) Dispose(material);
            meshes.Clear(); materials.Clear(); surfaceMaterial = null;
            releasing = false;
        }

        static void Dispose(Object value)
        {
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
    }
}
