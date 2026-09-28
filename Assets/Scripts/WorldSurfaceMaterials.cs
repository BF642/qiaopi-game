using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>
    /// World-metre surface detail for the existing hand-built meshes, including meshes
    /// without UVs/tangents. Textures are generated once per family and shared by all
    /// palette entries. Create returns a new material for WorldFactory's palette cache.
    /// </summary>
    public static class WorldSurfaceMaterials
    {
        enum Surface { Plain, Stone, Brick, Lime, Wood, Earth, Clay, Fibre }
        const int Resolution = 256;
        static readonly Dictionary<Surface, Texture2D> textures = new Dictionary<Surface, Texture2D>();
        static readonly List<Material> ownedMaterials = new List<Material>();

        public static Material Create(string name, Color color)
        {
            string key = (name ?? "surface").ToLowerInvariant();
            Surface type = Identify(key);
            Shader shader = type == Surface.Plain ? null : Resources.Load<Shader>("HistoricSurface");
            Material material;
            if (shader)
            {
                material = new Material(shader);
                material.SetTexture("_SurfaceTex", GetTexture(type));
                Vector2 metres = TileMetres(type);
                material.SetVector("_TileMetres", new Vector4(metres.x, metres.y, 0, 0));
                material.SetFloat("_DetailStrength", type == Surface.Fibre ? .26f : type == Surface.Wood ? .58f : type == Surface.Earth ? .55f : .80f);
                material.SetFloat("_MacroStrength", type == Surface.Earth ? .11f : type == Surface.Stone ? .10f : .075f);
                material.SetFloat("_Relief", type == Surface.Brick ? .006f : type == Surface.Earth ? .0022f :
                    type == Surface.Stone ? .0023f : type == Surface.Wood ? .0012f : type == Surface.Fibre ? .0004f : .0011f);
                material.SetFloat("_Weathering", type == Surface.Lime ? .24f : type == Surface.Stone || type == Surface.Brick ? .20f : type == Surface.Wood ? .12f : .04f);
                material.SetFloat("_CavityStrength", type == Surface.Brick ? .45f : .25f);
                material.SetFloat("_BrickBond", type == Surface.Brick ? .52f : 0);
                material.SetColor("_JointColor", new Color(.66f, .63f, .54f, 1));
                material.SetFloat("_Glossiness", type == Surface.Wood ? .18f : type == Surface.Clay ? .14f : .10f);
                material.SetFloat("_Metallic", 0);
            }
            else
            {
                // Foliage, water, metals, food and coloured signs retain their existing
                // simple material. Architectural detail is not imposed on every prop.
                var template = Resources.Load<Material>("WorldMaterial");
                material = template ? new Material(template) : new Material(Shader.Find("Standard"));
                material.SetFloat("_Glossiness", key.Contains("brass") ? .30f : .045f);
                material.SetFloat("_Metallic", key.Contains("brass") ? .45f : key.Contains("iron") ? .25f : 0);
                if (type != Surface.Plain) Debug.LogWarning("HistoricSurface shader unavailable; using plain material for " + name);
            }

            if (type == Surface.Earth)
            {
                // Broad old ground colours were golden under the afternoon sun. Keep
                // their hue relationship, but make dust read as a restrained grey-brown.
                float grey = color.r * .30f + color.g * .59f + color.b * .11f;
                Color dusty = Color.Lerp(color, new Color(grey, grey, grey, color.a), .30f);
                color = new Color(dusty.r * .87f, dusty.g * .88f, dusty.b * .89f, color.a);
            }
            material.name = name ?? "Historic surface";
            material.color = color;
            material.hideFlags = HideFlags.DontSave;
            ownedMaterials.Add(material);
            Application.quitting -= ReleaseSharedResources;
            Application.quitting += ReleaseSharedResources;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= ReleaseSharedResources;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ReleaseSharedResources;
#endif
            return material;
        }

        static Surface Identify(string n)
        {
            if (Has(n, "sandstone", "granite") && !Has(n, "earth", "soil")) return Surface.Stone;
            if (Has(n, "earth", "soil", "mud", "courtyard sand", "quay sand", "field bank", "drying floor", "dirt")) return Surface.Earth;
            if (Has(n, "brick patch", "terracotta", "clay", "earthenware", "roof", "tile highlight", "water jar")) return Surface.Clay;
            if (n.Contains("brick")) return Surface.Brick;
            if (Has(n, "plaster", "lime", "mortar", "joints", "shophouse", "longhouse")) return Surface.Lime;
            if (Has(n, "timber", "wood", "plank", "gangplank", "ship hull", "barrel", "counter panel", "bamboo") || n == "teal") return Surface.Wood;
            if (Has(n, "stone", "paving", "pavement", "foundation", "coping", "path", "narrow lane")) return Surface.Stone;
            if (Has(n, "linen", "canvas", "paper", "parcel wrap", "basket", "woven", "matting", "straw", "rope", "lantern")) return Surface.Fibre;
            return Surface.Plain;
        }

        static bool Has(string name, params string[] fragments)
        {
            foreach (string part in fragments) if (name.Contains(part)) return true;
            return false;
        }

        static Vector2 TileMetres(Surface type)
        {
            switch (type)
            {
                case Surface.Brick: return new Vector2(1.68f, 1.44f); // six 28 cm bricks, sixteen 9 cm courses
                case Surface.Wood: return new Vector2(1.20f, 3.20f); // long fibres, no extra plank seams
                case Surface.Earth: return new Vector2(1.65f, 1.65f);
                case Surface.Fibre: return new Vector2(.42f, .42f);
                case Surface.Clay: return new Vector2(.85f, .85f);
                default: return new Vector2(1.60f, 1.60f);
            }
        }

        static Texture2D GetTexture(Surface type)
        {
            if (textures.TryGetValue(type, out Texture2D existing) && existing) return existing;
            var pixels = new Color32[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++) for (int x = 0; x < Resolution; x++)
            {
                float u = (x + .5f) / Resolution, v = (y + .5f) / Resolution;
                float coarse = Noise(u, v, 4, 4, 17);
                float medium = Noise(u, v, 19, 19, 47);
                float fine = Noise(u, v, 83, 83, 101);
                float value = .5f, roughness = .82f, cavity = .98f, height = .5f;
                switch (type)
                {
                    case Surface.Stone:
                        value += (coarse - .5f) * .25f + (medium - .5f) * .31f + (fine - .5f) * .16f;
                        height += (medium - .5f) * .35f + (fine - .5f) * .20f;
                        cavity = 1 - Mathf.Max(0, .27f - fine) * .36f;
                        roughness = .76f + fine * .18f;
                        break;
                    case Surface.Brick:
                        int row = Mathf.FloorToInt(v * 16), col = Mathf.FloorToInt(u * 6 + (row % 2) * .5f);
                        float bx = Mathf.Repeat(u * 6 + (row % 2) * .5f, 1), by = Mathf.Repeat(v * 16, 1);
                        float edgeDistance = Mathf.Min(Mathf.Min(bx, 1 - bx) * .28f, Mathf.Min(by, 1 - by) * .09f);
                        float face = Smooth(.0025f, .007f, edgeDistance);
                        float brickTone = Hash(Mod(col, 6), row, 73) - .5f;
                        value += brickTone * .25f + (medium - .5f) * .13f + (fine - .5f) * .10f;
                        value -= (1 - face) * .055f;
                        height = .17f + face * (.49f + (fine - .5f) * .07f);
                        cavity = .70f + face * .28f;
                        roughness = .83f + (1 - face) * .12f;
                        break;
                    case Surface.Lime:
                        value += (coarse - .5f) * .20f + (medium - .5f) * .17f + (fine - .5f) * .10f;
                        height += (medium - .5f) * .18f + (fine - .5f) * .27f;
                        cavity = 1 - Mathf.Max(0, .25f - fine) * .30f;
                        roughness = .87f;
                        break;
                    case Surface.Wood:
                        float grain = Noise(u, v, 53, 4, 211);
                        float fibres = Noise(u, v, 109, 13, 97);
                        float dx = Mathf.Repeat(u - .32f + .5f, 1) - .5f;
                        float dy = Mathf.Repeat(v - .57f + .5f, 1) - .5f;
                        float knot = Mathf.Exp(-(dx * dx * 590 + dy * dy * 240));
                        value += (grain - .5f) * .32f + (fibres - .5f) * .15f + (coarse - .5f) * .11f - knot * .15f;
                        height += (grain - .5f) * .26f + (fibres - .5f) * .09f;
                        cavity = 1 - Mathf.Max(0, .32f - fibres) * .22f - knot * .07f;
                        roughness = .71f + fibres * .18f;
                        break;
                    case Surface.Earth:
                        float grit = Noise(u, v, 117, 117, 321);
                        value += (coarse - .5f) * .18f + (medium - .5f) * .24f + (grit - .5f) * .13f;
                        height += (medium - .5f) * .28f + (grit - .5f) * .28f;
                        cavity = .91f + grit * .08f;
                        roughness = .96f;
                        break;
                    case Surface.Clay:
                        value += (coarse - .5f) * .16f + (medium - .5f) * .15f + (fine - .5f) * .12f;
                        height += (medium - .5f) * .16f + (fine - .5f) * .22f;
                        roughness = .80f;
                        break;
                    case Surface.Fibre:
                        float weave = Mathf.Sin(u * Mathf.PI * 128) * Mathf.Sin(v * Mathf.PI * 128);
                        value += weave * .065f + (medium - .5f) * .14f;
                        height += weave * .13f + (fine - .5f) * .05f;
                        roughness = .94f;
                        break;
                }
                // Linear data: R=color variation, G=roughness, B=cavity, A=height.
                pixels[y * Resolution + x] = new Color(Mathf.Clamp01(value), Mathf.Clamp01(roughness), Mathf.Clamp01(cavity), Mathf.Clamp01(height));
            }
            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, true, true)
            {
                name = "Historic " + type + " · shared packed detail",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            textures[type] = texture;
            return texture;
        }

        // Periodic value noise produces a seamless tile, including its normal gradients.
        static float Noise(float u, float v, int fx, int fy, int seed)
        {
            float x = u * fx, y = v * fy;
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float tx = x - ix, ty = y - iy;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            return Mathf.Lerp(Mathf.Lerp(Hash(Mod(ix, fx), Mod(iy, fy), seed), Hash(Mod(ix + 1, fx), Mod(iy, fy), seed), tx),
                Mathf.Lerp(Hash(Mod(ix, fx), Mod(iy + 1, fy), seed), Hash(Mod(ix + 1, fx), Mod(iy + 1, fy), seed), tx), ty);
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFFu) / 16777215f;
            }
        }
        static int Mod(int n, int divisor) { int value = n % divisor; return value < 0 ? value + divisor : value; }
        static float Smooth(float low, float high, float value) { float t = Mathf.InverseLerp(low, high, value); return t * t * (3 - 2 * t); }

        /// <summary>Call only once all worlds using these materials have been released.</summary>
        public static void ReleaseSharedResources()
        {
            foreach (Material material in ownedMaterials) if (material) Dispose(material);
            foreach (Texture2D texture in textures.Values) if (texture) Dispose(texture);
            ownedMaterials.Clear(); textures.Clear();
        }
        static void Dispose(Object value) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
