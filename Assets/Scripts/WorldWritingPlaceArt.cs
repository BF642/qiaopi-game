using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    /// <summary>A real, reachable seat. These positions are world metres, not UI coordinates.</summary>
    public sealed class WritingPlace
    {
        public string title;
        public Vector3 approach, seatEye, lookAt;
    }

    public partial class WorldGame
    {
        WritingPlace writingPlace;

        void BuildWritingPlace()
        {
            writingPlace = null;
            if (!world || loadedWorld == "ship") return;
            string title;
            Vector3 centre;
            float yaw = 0;
            bool existingRoof = false, existingDesk = false;
            switch (loadedWorld)
            {
                case "quanzhou": title = "陈家东厢"; centre = new Vector3(-17.25f, 0, -8); yaw = 90; break;
                case "harbor": title = "候船借写间"; centre = new Vector3(14, 0, 9); existingRoof = true; break;
                case "port": title = "工班写批间"; centre = new Vector3(-11, 0, -6.5f); yaw = -90; break;
                case "market": title = "陈记账房侧间"; centre = new Vector3(-25.6f, 0, 16.75f); break;
                case "quarters": title = "住处写批间"; centre = new Vector3(28.1f, 0, 13.4f); yaw = 90; break;
                default: title = "信局写批长廊"; centre = new Vector3(-28.4f, 0, 11.05f); existingRoof = true; existingDesk = true; break;
            }
            var room = new GameObject(title + " · sheltered writing place");
            room.transform.SetParent(world.transform, false);
            room.transform.localPosition = centre;
            room.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            writingPlace = new WritingPlace
            {
                title = title,
                approach = room.transform.TransformPoint(new Vector3(.55f, 0, -1.3f)),
                seatEye = room.transform.TransformPoint(new Vector3(0, 1.20f, -.63f)),
                lookAt = room.transform.TransformPoint(new Vector3(-.17f, .90f, .58f))
            };
            WritingPlaceScenery.Build(room.transform, title, loadedWorld, existingRoof, existingDesk);
        }
    }

    /// <summary>Small rooms in existing yards, keeping all main paths and mission anchors clear.</summary>
    static class WritingPlaceScenery
    {
        static Mesh cube, cylinder;
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        public static void Build(Transform root, string title, string place, bool existingRoof, bool existingDesk)
        {
            var art = new Geometry(root);
            Material wood = Surface("writing aged timber", "796044"), dark = Surface("writing dark timber", "493C2D");
            Material plaster = Surface("writing lime plaster", place == "quarters" ? "B7B09A" : place == "port" ? "BAAE91" : "D3C7AC");
            Material baseWall = Surface(place == "quanzhou" ? "writing red brick" : "writing granite foundation", place == "quanzhou" ? "9E6047" : "999987");
            Material floor = Surface("writing worn granite paving", "A49D89"), paper = Surface("writing handmade paper", "EBDFC1");
            Material ink = Surface("writing ink", "2D3530"), red = Surface("writing seal", "A4563E");
            Material linen = Surface("writing woven linen", "AF9D78"), teal = Surface("writing book teal", "62796A");
            Material brass = Surface("writing lamp brass", "A48A56");

            // Flush paving does not alter the game's y=0 walking surface.
            art.Box("Stone floor", new Vector3(0, .014f, 0), new Vector3(4.2f, .028f, 4.2f), dark);
            for (int x = 0; x < 7; x++) for (int z = 0; z < 7; z++)
                art.Box("Hand-cut floor slabs", new Vector3(-1.8f + x * .6f, .034f, -1.8f + z * .6f), new Vector3(.586f, .025f, .586f), floor);

            // A 2.2m clear doorway opens toward the courtyard. Unlike a facade cube,
            // the room has separate walls and a traversable, fully empty interior.
            art.Box("Back wall", new Vector3(0, 1.30f, 2.07f), new Vector3(4.28f, 2.6f, .18f), plaster, true);
            art.Box("East wall", new Vector3(2.07f, 1.30f, 0), new Vector3(.18f, 2.6f, 4.15f), plaster, true);
            art.Box("West wall below window", new Vector3(-2.07f, .48f, 0), new Vector3(.18f, .96f, 4.15f), plaster, true);
            art.Box("West window head", new Vector3(-2.07f, 2.37f, 0), new Vector3(.18f, .46f, 4.15f), plaster, true);
            art.Box("West wall front return", new Vector3(-2.07f, 1.55f, -1.36f), new Vector3(.18f, 1.18f, 1.41f), plaster, true);
            art.Box("West wall rear return", new Vector3(-2.07f, 1.55f, 1.69f), new Vector3(.18f, 1.18f, .76f), plaster, true);
            foreach (float side in new[] { -1f, 1f })
            {
                art.Box("Doorway masonry return", new Vector3(side * 1.64f, 1.3f, -2.07f), new Vector3(.86f, 2.6f, .18f), plaster, true);
                art.Box("Door jamb", new Vector3(side * 1.14f, 1.15f, -2.08f), new Vector3(.11f, 2.3f, .23f), dark, true);
                art.Box("Open timber door", new Vector3(side * 1.28f, 1.02f, -1.78f), new Vector3(.065f, 2.03f, .52f), wood);
                art.Box("Wall granite footing", new Vector3(side * 2.07f, .19f, 0), new Vector3(.20f, .38f, 4.2f), baseWall);
                art.Box("Rear corner post", new Vector3(side * 1.93f, 1.29f, 1.94f), new Vector3(.12f, 2.58f, .12f), dark);
            }
            art.Box("Rear wall footing", new Vector3(0, .19f, 1.975f), new Vector3(4.15f, .38f, .035f), baseWall);
            art.Box("Entrance lintel", new Vector3(0, 2.33f, -2.08f), new Vector3(2.46f, .16f, .24f), dark);
            art.Box("Door sign", new Vector3(0, 2.52f, -2.185f), new Vector3(1.63f, .31f, .055f), wood);
            art.Sign(title, new Vector3(0, 2.52f, -2.22f), .065f, new Color(.88f, .80f, .61f));

            // A real open lattice window admits daylight from the side, not an opaque
            // black rectangle. Slender mortised bars read correctly from the seat.
            foreach (float y in new[] { .99f, 2.10f })
                art.Box("Window sill and head", new Vector3(-2.06f, y, .31f), new Vector3(.27f, .065f, 1.91f), dark);
            for (int bar = 0; bar < 8; bar++)
                art.Box("Window lattice upright", new Vector3(-2.08f, 1.55f, -.52f + bar * .24f), new Vector3(.052f, 1.10f, .032f), wood);
            foreach (float y in new[] { 1.26f, 1.77f })
                art.Box("Window cross lattice", new Vector3(-2.08f, y, .32f), new Vector3(.051f, .032f, 1.76f), wood);

            if (!existingRoof)
            {
                Material tile = Surface("writing roof tiles", "55574B");
                foreach (float side in new[] { -1f, 1f })
                {
                    art.Box("Weathered tiled roof", new Vector3(0, 2.97f, side * 1.14f), new Vector3(4.68f, .14f, 2.45f), tile, false, Quaternion.Euler(side * 15, 0, 0));
                    art.Box("Eave timber", new Vector3(0, 2.67f, side * 2.15f), new Vector3(4.55f, .16f, .17f), dark);
                    for (int row = 0; row < 18; row++)
                        art.Beam("Tile cover ridges", new Vector3(-2.18f + row * .256f, 3.335f, 0), new Vector3(-2.18f + row * .256f, 2.73f, side * 2.30f), .052f, tile);
                }
                art.Beam("Roof ridge cap", new Vector3(-2.34f, 3.36f, 0), new Vector3(2.34f, 3.36f, 0), .13f, tile);
            }
            art.Box("Ceiling crossbeam", new Vector3(0, 2.61f, 1.45f), new Vector3(4.16f, .15f, .13f), dark);
            art.Box("Front ceiling crossbeam", new Vector3(0, 2.61f, -1.55f), new Vector3(4.16f, .15f, .13f), dark);

            if (!existingDesk)
            {
                for (int plank = 0; plank < 5; plank++)
                    art.Box("Writing desk top board", new Vector3(0, .749f, .002f + plank * .175f), new Vector3(1.75f, .062f, .170f), wood);
                foreach (float x in new[] { -.73f, .73f }) foreach (float z in new[] { .04f, .66f })
                    art.Box("Desk leg", new Vector3(x, .357f, z), new Vector3(.078f, .714f, .078f), dark);
                foreach (float z in new[] { -.023f, .72f })
                    art.Box("Desk apron", new Vector3(0, .65f, z), new Vector3(1.6f, .14f, .055f), dark);
                art.Box("Desk solid top", new Vector3(0, .747f, .35f), new Vector3(1.70f, .067f, .80f), wood, true, null, false);
                art.Box("Desk cross stretcher", new Vector3(0, .26f, .35f), new Vector3(1.48f, .055f, .055f), wood);
                art.Box("Woven chair seat", new Vector3(0, .437f, -.63f), new Vector3(.49f, .061f, .43f), linen);
                foreach (float x in new[] { -.20f, .20f }) foreach (float z in new[] { -.80f, -.46f })
                    art.Box("Chair foot", new Vector3(x, .21f, z), new Vector3(.045f, .42f, .045f), dark);
                foreach (float x in new[] { -.21f, .21f })
                    art.Box("Chair back upright", new Vector3(x, .74f, -.83f), new Vector3(.045f, .67f, .045f), wood);
                art.Box("Chair curved back rail", new Vector3(0, 1.05f, -.83f), new Vector3(.48f, .085f, .055f), wood);
            }

            // The enlarged, legible sheet sits on the desk itself. UI stays on the
            // right; paper, red ruling, brush and lamp occupy the visible left half.
            art.Box("Open qiaopi sheet", new Vector3(-.29f, .787f, .26f), new Vector3(.48f, .008f, .51f), paper);
            foreach (float x in new[] { -.515f, -.075f })
                art.Box("Qiaopi red border", new Vector3(x, .792f, .26f), new Vector3(.004f, .002f, .48f), red);
            foreach (float z in new[] { .025f, .495f })
                art.Box("Qiaopi red border", new Vector3(-.295f, .792f, z), new Vector3(.445f, .002f, .004f), red);
            for (int column = 0; column < 7; column++)
                art.Box("Qiaopi vertical ruling", new Vector3(-.46f + column * .055f, .792f, .26f), new Vector3(.0016f, .0015f, .45f), red);
            art.Box("Folded addressed envelope", new Vector3(.52f, .799f, .40f), new Vector3(.21f, .025f, .34f), paper, false, Quaternion.Euler(0, -8, 0));
            art.Box("Envelope red seal", new Vector3(.52f, .814f, .45f), new Vector3(.052f, .006f, .060f), red);
            art.Box("Inkstone body", new Vector3(.26f, .804f, .19f), new Vector3(.21f, .045f, .15f), ink);
            art.Box("Inkstone ink well", new Vector3(.24f, .829f, .19f), new Vector3(.12f, .003f, .095f), dark);
            art.Beam("Bamboo writing brush", new Vector3(-.63f, .806f, .16f), new Vector3(-.57f, .806f, .47f), .010f, wood);
            art.Beam("Ink-dark brush tip", new Vector3(-.63f, .806f, .16f), new Vector3(-.641f, .806f, .105f), .010f, ink);
            art.Box("Brass paperweight", new Vector3(-.29f, .803f, .52f), new Vector3(.34f, .022f, .033f), brass);
            art.Cylinder("Brush pot", new Vector3(.69f, .858f, .64f), .075f, .155f, dark);
            for (int pen = 0; pen < 3; pen++)
                art.Beam("Upright spare brush", new Vector3(.67f + pen * .018f, .86f, .64f), new Vector3(.66f + pen * .026f, 1.16f - pen * .025f, .66f), .009f, wood);

            // Small shelves and tied remittance bundles give this room a distinct
            // purpose, rather than another blank wall around a generic table.
            art.Box("Letter shelf backing", new Vector3(-1.36f, .96f, 1.62f), new Vector3(1.04f, 1.82f, .07f), dark);
            foreach (float x in new[] { -1.89f, -.83f })
                art.Box("Letter shelf upright", new Vector3(x, .97f, 1.43f), new Vector3(.055f, 1.88f, .44f), wood);
            for (int row = 0; row < 4; row++)
            {
                float y = .23f + row * .46f;
                art.Box("Letter shelf", new Vector3(-1.36f, y, 1.42f), new Vector3(1.06f, .048f, .48f), wood);
                for (int book = 0; book < 4; book++)
                {
                    float x = -1.71f + book * .225f;
                    art.Box("Bound qiaopi bundle", new Vector3(x, y + .078f, 1.40f), new Vector3(.19f, .11f, .32f), (book + row) % 3 == 0 ? teal : paper);
                    art.Box("Bundle cotton tie", new Vector3(x, y + .137f, 1.40f), new Vector3(.010f, .007f, .33f), linen);
                }
            }
            art.Box("Framed address card", new Vector3(.47f, 1.50f, 1.96f), new Vector3(.88f, .76f, .034f), dark);
            art.Box("Address card paper", new Vector3(.47f, 1.50f, 1.938f), new Vector3(.78f, .66f, .010f), paper);
            art.Sign("泉州家书", new Vector3(.47f, 1.63f, 1.928f), .056f, new Color(.29f, .33f, .28f));
            art.Sign("银信相伴", new Vector3(.47f, 1.37f, 1.928f), .043f, new Color(.42f, .39f, .32f));
            art.Cylinder("Oil lamp base", new Vector3(-.76f, .804f, .66f), .065f, .045f, brass);
            art.Cylinder("Oil lamp stem", new Vector3(-.76f, .886f, .66f), .023f, .13f, brass);
            art.Cylinder("Oil cup", new Vector3(-.76f, .96f, .66f), .064f, .032f, brass);
            art.Cylinder("Lamp wick", new Vector3(-.76f, .991f, .66f), .009f, .035f, ink);
            var lamp = new GameObject("Warm oil-lamp light"); lamp.transform.SetParent(root, false); lamp.transform.localPosition = new Vector3(-.76f, 1.05f, .66f);
            var light = lamp.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .83f, .61f); light.range = 3.6f; light.intensity = .75f; light.shadows = LightShadows.None;
            art.Flush();
        }

        static Material Surface(string name, string hex)
        {
            if (materials.TryGetValue(name, out var material) && material) return material;
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return materials[name] = WorldSurfaceMaterials.Create(name, color);
        }

        sealed class Geometry
        {
            readonly Transform root;
            readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
            public Geometry(Transform root) { this.root = root; }
            public void Box(string name, Vector3 position, Vector3 size, Material material, bool solid = false, Quaternion? rotation = null, bool visible = true)
            {
                if (!cube) cube = CreateCube();
                Quaternion q = rotation ?? Quaternion.identity;
                if (visible) Add(cube, position, q, size, material);
                if (solid)
                {
                    var go = new GameObject(name + " collider"); go.transform.SetParent(root, false); go.transform.localPosition = position; go.transform.localRotation = q;
                    go.AddComponent<BoxCollider>().size = size;
                }
            }
            public void Cylinder(string name, Vector3 position, float radius, float height, Material material)
            {
                if (!cylinder) cylinder = CreateCylinder();
                Add(cylinder, position, Quaternion.identity, new Vector3(radius * 2, height, radius * 2), material);
            }
            public void Beam(string name, Vector3 a, Vector3 b, float width, Material material)
            {
                if (!cylinder) cylinder = CreateCylinder();
                Add(cylinder, (a + b) * .5f, Quaternion.FromToRotation(Vector3.up, b - a), new Vector3(width, Vector3.Distance(a, b), width), material);
            }
            void Add(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
            {
                if (!groups.TryGetValue(material, out var group)) groups[material] = group = new List<CombineInstance>();
                group.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, scale) });
            }
            public void Sign(string text, Vector3 position, float size, Color color)
            {
                var font = Resources.Load<Font>("Fonts/Title"); if (!font) return;
                font.RequestCharactersInTexture(text, 48, FontStyle.Normal);
                var sign = new GameObject("Writing room sign · " + text); sign.transform.SetParent(root, false); sign.transform.localPosition = position;
                var label = sign.AddComponent<TextMesh>(); label.font = font; label.fontSize = 48; label.characterSize = size; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.text = text; label.color = color;
                var renderer = sign.GetComponent<MeshRenderer>(); renderer.sharedMaterial = font.material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            public void Flush()
            {
                // Generated shared meshes need explicit ownership when this room is replaced.
                var cleanup = root.gameObject.AddComponent<DestinationMeshLifetime>();
                foreach (var pair in groups)
                {
                    var mesh = new Mesh { name = "Writing room · " + pair.Key.name, indexFormat = IndexFormat.UInt32 };
                    cleanup.meshes.Add(mesh);
                    mesh.CombineMeshes(pair.Value.ToArray(), true, true); mesh.RecalculateBounds();
                    var go = new GameObject(mesh.name); go.transform.SetParent(root, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = pair.Key;
                }
            }
        }

        static Mesh CreateCube()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            Face(vertices, triangles, new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(.5f,-.5f,-.5f));
            Face(vertices, triangles, new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f), new Vector3(-.5f,-.5f,.5f));
            Face(vertices, triangles, new Vector3(-.5f,-.5f,.5f), new Vector3(-.5f,.5f,.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(-.5f,-.5f,-.5f));
            Face(vertices, triangles, new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(.5f,.5f,.5f), new Vector3(.5f,-.5f,.5f));
            Face(vertices, triangles, new Vector3(-.5f,.5f,-.5f), new Vector3(-.5f,.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(.5f,.5f,-.5f));
            Face(vertices, triangles, new Vector3(-.5f,-.5f,.5f), new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,-.5f,.5f));
            var mesh = new Mesh { name = "Writing room metric cube" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static void Face(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int at = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.Add(at); triangles.Add(at + 1); triangles.Add(at + 2); triangles.Add(at); triangles.Add(at + 2); triangles.Add(at + 3);
        }
        static Mesh CreateCylinder()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8, b = (i + 1) * Mathf.PI / 8;
                Vector3 p = new Vector3(Mathf.Sin(a) * .5f, 0, Mathf.Cos(a) * .5f), q = new Vector3(Mathf.Sin(b) * .5f, 0, Mathf.Cos(b) * .5f);
                Face(vertices, triangles, p + Vector3.down * .5f, q + Vector3.down * .5f, q + Vector3.up * .5f, p + Vector3.up * .5f);
                Face(vertices, triangles, Vector3.up * .5f, p + Vector3.up * .5f, q + Vector3.up * .5f, Vector3.up * .5f);
                Face(vertices, triangles, Vector3.down * .5f, q + Vector3.down * .5f, p + Vector3.down * .5f, Vector3.down * .5f);
            }
            var mesh = new Mesh { name = "Writing room round joinery" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
