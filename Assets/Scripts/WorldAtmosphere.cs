using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    /// <summary>Quiet, geometry-based life in the world. Owns no colliders or gameplay state.</summary>
    public sealed class WorldAtmosphere : MonoBehaviour
    {
        struct Hanging
        {
            public Transform transform;
            public Quaternion rest;
            public float phase, amount;
        }
        static readonly List<WorldAtmosphere> active = new List<WorldAtmosphere>();
        static bool soundEnabled = true;
        readonly List<Hanging> hanging = new List<Hanging>();
        readonly List<Material> ownedMaterials = new List<Material>();
        Mesh floatingMesh, birdMesh, rippleMesh;
        Vector3[] floatingVertices, birdVertices, rippleVertices;
        Vector3[] seeds;
        float[] phases;
        Transform boat;
        Vector3 boatRest;
        AudioSource ambient;
        AudioClip loop;
        string place;
        float nextGeometryFrame;
        const int FloatingVerticesPerParticle = 24;
        const int BirdVerticesPerBird = 30;
        const int BirdCount = 3;

        /// <summary>Called automatically by WorldFactory.Build. Safe to call again.</summary>
        public static WorldAtmosphere Create(GameObject world, string kind)
        {
            if (!world) return null;
            var existing = world.GetComponent<WorldAtmosphere>();
            if (existing) return existing;
            var atmosphere = world.AddComponent<WorldAtmosphere>();
            atmosphere.Initialize(kind);
            return atmosphere;
        }
        public static void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (!active[i]) { active.RemoveAt(i); continue; }
                if (active[i].ambient) active[i].ambient.mute = !enabled;
            }
        }
        void Initialize(string kind)
        {
            place = kind;
            active.Add(this);
            foreach (var child in GetComponentsInChildren<Transform>())
            {
                if (child.name == "Lantern sway pivot" || child.name == "Laundry sway pivot")
                    hanging.Add(new Hanging { transform = child, rest = child.localRotation, phase = hanging.Count * 1.77f, amount = child.name.StartsWith("Lantern") ? 3.0f : 5.2f });
                if (child.name == "Harbor boat sway pivot") { boat = child; boatRest = child.localPosition; }
            }
            CreateFloatingGeometry();
            CreateBirds();
            // Continuous water and shoreline/wake motion are owned by SeaEnvironment.
            CreateSound();
            UpdateGeometry(0);
        }
        Material Material(string label, Color color)
        {
            var template = Resources.Load<Material>("WorldMaterial");
            var m = template ? new Material(template) : new Material(Shader.Find("Standard"));
            m.name = label; m.color = color; m.SetFloat("_Glossiness", .05f);
            ownedMaterials.Add(m); return m;
        }
        Mesh DynamicGeometry(string label, int count, Material material)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var mesh = new Mesh { name = label };
            mesh.MarkDynamic(); mesh.vertices = new Vector3[count];
            var indices = new int[count]; for (int i = 0; i < count; i++) indices[i] = i;
            mesh.triangles = indices;
            mesh.bounds = new Bounds(new Vector3(0, 5, 5), new Vector3(90, 35, 100));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return mesh;
        }
        void CreateFloatingGeometry()
        {
            int count = place == "quanzhou" ? 26 : (place == "harbor" || place == "port" || place == "ship") ? 16 : 18;
            seeds = new Vector3[count]; phases = new float[count];
            var random = new System.Random((place == "harbor" || place == "port" || place == "ship") ? 531 : 227);
            for (int i = 0; i < count; i++)
            {
                seeds[i] = new Vector3((float)random.NextDouble() * 27 - 13.5f, (float)random.NextDouble() * 5 + .5f, (float)random.NextDouble() * 30 - 11);
                phases[i] = (float)random.NextDouble() * Mathf.PI * 2;
            }
            floatingVertices = new Vector3[count * FloatingVerticesPerParticle];
            floatingMesh = DynamicGeometry("Drifting leaves and sunlit motes", floatingVertices.Length,
                Material("Muted airborne leaves", (place == "harbor" || place == "port" || place == "ship") ? new Color(.69f, .78f, .71f) : new Color(.69f, .64f, .35f)));
        }
        void CreateBirds()
        {
            birdVertices = new Vector3[BirdCount * BirdVerticesPerBird];
            birdMesh = DynamicGeometry((place == "harbor" || place == "port" || place == "ship") ? "Three circling harbor birds" : "Three gliding swifts", birdVertices.Length,
                Material("Distant bird silhouette", (place == "harbor" || place == "port" || place == "ship") ? new Color(.84f, .85f, .76f) : new Color(.28f, .31f, .28f)));
        }
        void CreateRipples()
        {
            const int rings = 5, segments = 32;
            rippleVertices = new Vector3[rings * segments * 6];
            rippleMesh = DynamicGeometry("Quiet expanding harbor ripples", rippleVertices.Length,
                Material("Sea ripple highlights", new Color(.40f, .67f, .64f)));
        }
        void Update()
        {
            float time = Time.time;
            for (int i = 0; i < hanging.Count; i++)
            {
                var item = hanging[i];
                if (item.transform) item.transform.localRotation = item.rest * Quaternion.Euler(Mathf.Sin(time * .77f + item.phase) * item.amount,
                    0, Mathf.Sin(time * .51f + item.phase) * item.amount * .55f);
            }
            if (boat)
            {
                boat.localPosition = boatRest + Vector3.up * (Mathf.Sin(time * .61f) * .085f);
                boat.localRotation = Quaternion.Euler(Mathf.Sin(time * .42f) * .48f, 0, Mathf.Sin(time * .57f + .8f) * .65f);
            }
            // Tiny distant animation is intentionally updated at 25 Hz, without allocations.
            if (time >= nextGeometryFrame) { nextGeometryFrame = time + .04f; UpdateGeometry(time); }
        }
        void UpdateGeometry(float time)
        {
            if (!floatingMesh) return;
            for (int i = 0; i < seeds.Length; i++)
            {
                Vector3 p = seeds[i]; float phase = phases[i];
                p.x = Mathf.Repeat(p.x + time * (.12f + (i % 3) * .025f) + 15, 30) - 15;
                p.y += Mathf.Sin(time * .45f + phase) * .31f;
                p.z += Mathf.Sin(time * .19f + phase) * 1.1f;
                float size = i % 4 == 0 ? .12f : .045f;
                Quaternion q = Quaternion.Euler(time * (14 + i % 7), phase * Mathf.Rad2Deg + time * 11, phase * 40);
                Vector3 a = p + q * new Vector3(-size, 0, 0), b = p + q * new Vector3(size, 0, 0);
                Vector3 c = p + q * new Vector3(0, 0, -size * .47f), d = p + q * new Vector3(0, 0, size * .47f);
                Vector3 top = p + q * new Vector3(0, size * .2f, 0), bottom = p - q * new Vector3(0, size * .2f, 0);
                int j = i * FloatingVerticesPerParticle;
                Triangle(floatingVertices, ref j, top, a, c); Triangle(floatingVertices, ref j, top, c, b);
                Triangle(floatingVertices, ref j, top, b, d); Triangle(floatingVertices, ref j, top, d, a);
                Triangle(floatingVertices, ref j, bottom, c, a); Triangle(floatingVertices, ref j, bottom, b, c);
                Triangle(floatingVertices, ref j, bottom, d, b); Triangle(floatingVertices, ref j, bottom, a, d);
            }
            floatingMesh.vertices = floatingVertices; floatingMesh.RecalculateNormals();
            for (int i = 0; i < BirdCount; i++)
            {
                float angle = time * (.062f + i * .009f) + i * 2.094f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * (13 + i), 8.4f + i * .65f + Mathf.Sin(time * .24f + i) * .4f,
                    ((place == "harbor" || place == "port" || place == "ship") ? 20 : 11) + Mathf.Sin(angle) * 10);
                Quaternion q = Quaternion.LookRotation(new Vector3(-Mathf.Sin(angle) * (13 + i), 0, Mathf.Cos(angle) * 10));
                float flap = Mathf.Sin(time * 2.8f + i * 1.7f) * .19f;
                Vector3 nose = p + q * new Vector3(0, 0, .3f), tail = p + q * new Vector3(0, 0, -.26f);
                Vector3 left = p + q * new Vector3(-.095f, 0, 0), right = p + q * new Vector3(.095f, 0, 0);
                Vector3 top = p + Vector3.up * .095f;
                Vector3 wingL = p + q * new Vector3(-.73f, flap, -.07f), wingR = p + q * new Vector3(.73f, flap, -.07f);
                Vector3 back = p + q * new Vector3(0, 0, -.16f);
                int j = i * BirdVerticesPerBird;
                Triangle(birdVertices, ref j, nose, left, top); Triangle(birdVertices, ref j, nose, top, right);
                Triangle(birdVertices, ref j, tail, top, left); Triangle(birdVertices, ref j, tail, right, top);
                Triangle(birdVertices, ref j, nose, right, left); Triangle(birdVertices, ref j, tail, left, right);
                Triangle(birdVertices, ref j, left, wingL, back); Triangle(birdVertices, ref j, back, wingL, left);
                Triangle(birdVertices, ref j, right, back, wingR); Triangle(birdVertices, ref j, wingR, back, right);
            }
            birdMesh.vertices = birdVertices; birdMesh.RecalculateNormals();
            if (rippleMesh)
            {
                int j = 0;
                for (int ring = 0; ring < 5; ring++)
                {
                    float progress = Mathf.Repeat(time * .10f + ring * .213f, 1);
                    float radius = 1.3f + progress * 3.4f;
                    float width = Mathf.Sin(progress * Mathf.PI) * .075f + .006f;
                    Vector3 center = new Vector3(ring % 2 == 0 ? -11 - ring * 1.4f : 11 + ring, -.47f, 9 + ring * 4);
                    for (int segment = 0; segment < 32; segment++)
                    {
                        float a = segment * Mathf.PI / 16, b = (segment + 1) * Mathf.PI / 16;
                        Vector3 da = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a) * .65f), db = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b) * .65f);
                        Triangle(rippleVertices, ref j, center + da * radius, center + db * radius, center + db * (radius + width));
                        Triangle(rippleVertices, ref j, center + da * radius, center + db * (radius + width), center + da * (radius + width));
                    }
                }
                rippleMesh.vertices = rippleVertices; rippleMesh.RecalculateNormals();
            }
        }
        static void Triangle(Vector3[] vertices, ref int offset, Vector3 a, Vector3 b, Vector3 c)
        {
            vertices[offset++] = a; vertices[offset++] = b; vertices[offset++] = c;
        }
        void CreateSound()
        {
            const int frequency = 22050, seconds = 14;
            var samples = new float[frequency * seconds];
            var random = new System.Random((place == "harbor" || place == "port" || place == "ship") ? 809 : 415);
            float low = 0, softer = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / frequency;
                float noise = (float)random.NextDouble() * 2 - 1;
                low += (noise - low) * .035f; softer += (low - softer) * .014f;
                float wind = low * .11f + softer * .20f;
                float wave = (place == "harbor" || place == "port" || place == "ship") ? (low * .24f + noise * .017f) * (.58f + .42f * Mathf.Sin(time * Mathf.PI * 2 / 7)) : noise * .0018f;
                float bird = 0;
                if (place != "harbor" && place != "port" && place != "ship")
                {
                    // A few quiet, short rising chirps, separated by several seconds.
                    float chirp = Mathf.Repeat(time + 1.8f, 6.8f);
                    if (chirp < .24f) bird = Mathf.Sin(time * Mathf.PI * 2 * 1740 + chirp * chirp * 7200) * Mathf.Sin(chirp / .24f * Mathf.PI) * .012f;
                }
                float seam = Mathf.Clamp01(Mathf.Min(time, seconds - time) / .5f);
                seam = seam * seam * (3 - 2 * seam);
                samples[i] = (wind + wave + bird) * seam;
            }
            loop = AudioClip.Create("Soft " + place + " air and distant life", samples.Length, 1, frequency, false);
            loop.SetData(samples, 0);
            ambient = gameObject.AddComponent<AudioSource>();
            ambient.playOnAwake = false; ambient.loop = true; ambient.clip = loop; ambient.spatialBlend = 0;
            ambient.volume = (place == "harbor" || place == "port" || place == "ship") ? .28f : .22f; ambient.mute = !soundEnabled;
            if (Application.isPlaying) ambient.Play();
        }
        void OnDestroy()
        {
            active.Remove(this);
            if (ambient) ambient.Stop();
            if (loop) Destroy(loop);
            if (floatingMesh) Destroy(floatingMesh);
            if (birdMesh) Destroy(birdMesh);
            if (rippleMesh) Destroy(rippleMesh);
            foreach (var material in ownedMaterials) if (material) Destroy(material);
        }
    }
}
