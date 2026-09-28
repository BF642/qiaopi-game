using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public static partial class WorldFactory
    {
        // Three independent plans, in metres. All traversable floor tops are y = 0.
        // Mission circles have radius 2.3; no collidable furnishing enters them.
        // Detail meshes batch repeated boards, masonry, rails and rope coils.
        static void ExpandedHarbor()
        {
            Box("厦门出洋石岸 · walkable", new Vector3(0, -.30f, -5), new Vector3(72, .60f, 54), Mat("expanded quay sand", "A99C83"), true);
            Box("石驳岸基座", new Vector3(0, -.95f, -5), new Vector3(72, .70f, 54), Mat("expanded quay foundation", "7D8074"));
            GranitePaving(0, -5, 12, 54);
            GranitePaving(0, -21, 59, 5);
            GranitePaving(0, 15, 62, 6);
            GranitePaving(-23, -3, 5, 39);
            GranitePaving(23, -3, 5, 39);

            // Departure street has narrow domestic fronts, unlike the cargo port.
            EHDepartureHouse(-17, -26, 7, 7, 3.05f, "客寓");
            EHDepartureHouse(17, -26, 7, 7, 3.2f, "船具");
            EHDepartureHouse(-15, -10, 7, 9, 3.15f, "行李寄存");
            EHDepartureHouse(15, -10, 7, 9, 3.3f, "出洋问讯");
            EHDepartureHouse(-29, -7, 6.8f, 11, 3.15f, "茶寮");
            EHDepartureHouse(29, -8, 6.8f, 11, 3.1f, "客商歇脚");
            EHTurnedWarehouse(-29, 11, 10, 11, 4.5f, 0);
            EHTurnedWarehouse(29, 5, 10, 10, 4.6f, 0);

            // The inspection yard opens south and east; its posts are well off the
            // main street and the worker route (-24,-22) -> (23,14).
            EHOpenShelter("行李验票院", new Vector3(-14, 0, 9), 10, 11, 2.85f, true);
            EHWorkTable("验票院旧木案", new Vector3(-17, 0, 12), 2.8f, .72f, .92f);
            for (int i = 0; i < 4; i++)
            {
                Box("检验簿册", new Vector3(-18.0f + i * .65f, .945f, 12), new Vector3(.34f, .05f, .25f), Paper);
                EHParcel(new Vector3(-18.0f + (i % 2) * 1.35f, 0, 5 + (i / 2) * 1.1f), .95f);
            }
            Box("验票木牌", new Vector3(-14, 2.48f, 3.4f), new Vector3(2.8f, .42f, .10f), Dark);
            SignText("验票 · 行李", new Vector3(-14, 2.48f, 3.33f), .085f);
            EHOpenShelter("候船歇脚棚", new Vector3(14, 0, 10), 9, 8, 2.7f, false);
            EHWaitingBench(new Vector3(11.0f, 0, 10)); EHWaitingBench(new Vector3(16.8f, 0, 12));
            EHParcel(new Vector3(17.1f, 0, 7), 1.1f);

            // Tea and helper anchors (-6,-22) and (-24,-22) stay empty.
            EHOpenShelter("街边茶棚", new Vector3(-10, 0, -29), 5, 3.6f, 2.65f, false);
            EHWaitingBench(new Vector3(-10, 0, -29));
            Pot(new Vector3(-12.2f, 0, -29), .48f);
            CargoCart(new Vector3(27, 0, -27));
            CargoCart(new Vector3(-31, 0, -27));
            Tree(new Vector3(-33, 0, -17), .92f);
            Tree(new Vector3(33, 0, 16), .72f);
            EHParcel(new Vector3(-28, 0, -18), 1.2f);
            EHParcel(new Vector3(29, 0, 16), 1.15f);
            Barrel(new Vector3(32, 0, 16));

            // A broad stone apron gives way to one 12-metre timber pier.
            EHQuayFace(22, -36, -6, false);
            EHQuayFace(22, 6, 36, false);
            Box("出洋栈桥承梁", new Vector3(0, -.44f, 30), new Vector3(12, .55f, 16), Dark);
            Box("出洋栈桥 · walkable", new Vector3(0, -.09f, 30), new Vector3(12, .18f, 16), Timber, true);
            EHDeckBoards("栈桥旧木板与接缝", new Vector3(0, .007f, 30), 12, 16, .52f);
            foreach (float x in new[] { -5.7f, 5.7f })
            {
                for (int i = 0; i < 5; i++)
                {
                    float z = 22.5f + i * 3.6f;
                    Cylinder("栈桥系缆木桩", new Vector3(x, .45f, z), .20f, 2.15f, Dark, true);
                    EHCoil(new Vector3(x, .91f, z), .31f, 3);
                    if (i < 4) SaggingRope(new Vector3(x, 1.1f, z), new Vector3(x, 1.1f, z + 3.6f));
                }
                // Thin safety curbs run only at the pier edges, never across boarding.
                Box("栈桥侧缘护木", new Vector3(x, .11f, 30), new Vector3(.20f, .22f, 16), Dark, true);
            }
            Box("出洋方向高悬横梁", new Vector3(0, 4.1f, 19), new Vector3(13.4f, .25f, .30f), Dark);
            foreach (float x in new[] { -6.6f, 6.6f }) Cylinder("出洋口高木柱", new Vector3(x, 2, 19), .15f, 4, Dark, true);
            Box("出洋悬牌", new Vector3(0, 3.6f, 18.80f), new Vector3(3.5f, .64f, .15f), Dark);
            SignText("厦门 · 出洋口", new Vector3(0, 3.6f, 18.69f), .11f);
            Lantern(new Vector3(-6.55f, 3.1f, 19)); Lantern(new Vector3(6.55f, 3.1f, 19));
            // Boarding interaction is at (0,30), on level timber with a clear apron.
            // The boarding apron uses the same flush, fully jointed deck as the pier.
            Ship(new Vector3(0, -.60f, 47));
            EHRemoteJunk(new Vector3(-27, -.55f, 55), .78f, -18);
            EHRemoteJunk(new Vector3(31, -.55f, 61), .68f, 27);
            CoastalHorizon();
        }

        static void ExpandedPort()
        {
            // Singapore has a complete flat cargo yard. Water is strictly beyond
            // x=37 or z=39, not interleaved with any delivery target.
            Box("新加坡货港 · continuous walkable yard", new Vector3(0, -.30f, 3), new Vector3(72, .60f, 70), Mat("port packed earth", "9F927B"), true);
            Box("货港石基", new Vector3(0, -.89f, 3), new Vector3(72, .60f, 70), Stone);
            GranitePaving(0, 3, 8, 70);
            GranitePaving(0, -21, 65, 5);
            GranitePaving(0, 7, 65, 5);
            GranitePaving(0, 24, 65, 6);
            GranitePaving(-20, 3, 5, 63);
            GranitePaving(21, 3, 5, 63);

            // Long warehouse frontage faces west, parallel to the side waterfront.
            EHTurnedWarehouse(31, -12, 20, 8, 5.2f, 90);
            EHTurnedWarehouse(31, 25, 21, 8, 5.4f, 90);
            EHTurnedWarehouse(-28, 12, 12, 8, 4.8f, -90);
            EHTurnedWarehouse(24, -27, 20, 6, 4.7f, 0);
            EHOpenShelter("苦力休息长棚", new Vector3(-29, 0, -4), 10, 8, 2.8f, false);
            EHWaitingBench(new Vector3(-29, 0, -5.5f)); EHWaitingBench(new Vector3(-29, 0, -1.5f));
            Pot(new Vector3(-33, 0, -2), .65f);
            EHOpenShelter("码头工班茶棚", new Vector3(-10, 0, -29), 5, 3.6f, 2.65f, false);
            EHWaitingBench(new Vector3(-10, 0, -29));
            EHOpenShelter("北场货物点收棚", new Vector3(-28, 0, 29), 12, 11, 3.3f, true);
            Box("工班告示木板", new Vector3(-26, 2.4f, 23.35f), new Vector3(4.2f, 1.3f, .12f), Dark);
            SignText("点货 · 候工", new Vector3(-26, 2.4f, 23.26f), .13f);

            // Cargo islands leave a road grid; pickup circles (-23,-18), (-18,7)
            // and the two overlapping delivery circles (21,24)/(24,23) are empty.
            EHCargoIsland(new Vector3(-30, 0, -18), 3, 2, 1.2f);
            EHCargoIsland(new Vector3(-15, 0, -17), 2, 2, 1.25f);
            EHCargoIsland(new Vector3(14, 0, -13), 3, 2, 1.18f);
            EHCargoIsland(new Vector3(14, 0, 0), 3, 2, 1.14f);
            EHCargoIsland(new Vector3(13, 0, 13), 2, 2, 1.2f);
            EHCargoIsland(new Vector3(13, 0, 31), 3, 2, 1.2f);
            EHCargoIsland(new Vector3(-28, 0, 35), 3, 1, 1.1f);
            EHCrane(new Vector3(32.5f, 0, 5.5f), 1);
            EHCrane(new Vector3(32.5f, 0, 36), 1);
            CargoCart(new Vector3(-13, 0, -28));
            CargoCart(new Vector3(14, 0, 19));
            EHCoil(new Vector3(34, .06f, 11), 1.0f, 5);
            Barrel(new Vector3(25.5f, 0, 31)); Barrel(new Vector3(24, 0, 33));
            Barrel(new Vector3(-33, 0, 20));
            Palm(new Vector3(-34, 0, -29)); Palm(new Vector3(-34, 0, 21));

            EHQuayFace(36.0f, -32, 38, true);
            EHQuayFace(38.0f, -36, 36, false);
            for (int i = 0; i < 8; i++)
            {
                float z = -28 + i * 8.7f;
                Cylinder("侧岸系缆铁木桩", new Vector3(35.6f, .43f, z), .22f, .86f, Dark, true);
            }
            // Steamer-facing shipping lane, not the departure pier's central axis.
            EHRemoteJunk(new Vector3(53, -.65f, 9), 1.10f, 0);
            EHRemoteJunk(new Vector3(58, -.65f, 41), .82f, 18);
            EHOpenShelter("码头门岗", new Vector3(10, 0, -29), 5, 3.6f, 2.75f, true);
            Box("货运路旧木路牌", new Vector3(6.8f, 2.9f, -24), new Vector3(3.4f, .65f, .12f), Dark);
            SignText("货港 · 沿岸货栈", new Vector3(6.8f, 2.9f, -24.08f), .095f);
            // Northward travel NPC (0,29) has an uninterrupted 8m-wide approach.
        }

        static void ExpandedShip()
        {
            EHShipHull();
            Box("远洋船整块可行甲板", new Vector3(0, -.16f, 0), new Vector3(22, .32f, 60), Timber, true);
            EHDeckBoards("远洋船旧木甲板", new Vector3(0, .009f, 0), 22, 60, .57f);
            // Port and starboard bulkheads leave the entire x[-3,3] axis clear.
            EHDeckCabin(new Vector3(-7.4f, 0, 20), 6.4f, 12, false);
            EHDeckCabin(new Vector3(7.4f, 0, -23), 6.4f, 10, true);
            EHDeckHatch(new Vector3(-7.6f, 0, -2), 4.8f, 6);
            EHDeckHatch(new Vector3(7.6f, 0, 5), 4.8f, 5);
            EHCargoIsland(new Vector3(-8.2f, 0, -23.5f), 1, 2, 1.05f);
            EHCargoIsland(new Vector3(-8.1f, 0, -6.8f), 1, 1, 1.18f);
            EHCargoIsland(new Vector3(8.1f, 0, 22), 1, 2, 1.13f);
            Barrel(new Vector3(-9.1f, 0, 8)); Barrel(new Vector3(9.0f, 0, 16.6f));
            EHCoil(new Vector3(-8.8f, .06f, 4.5f), .74f, 4);
            EHCoil(new Vector3(8.8f, .06f, -14.3f), .71f, 4);

            // Mast feet sit in the side working zones; all sail spars cross above
            // head height. No central pole obstructs the 6m circulation spine.
            EHDeckMast(new Vector3(-5.2f, 0, 3), 12.5f, 7.0f, 8.0f);
            EHDeckMast(new Vector3(5.2f, 0, -13), 10.8f, 6.5f, 6.8f);
            EHShipRails();
            EHShipWheel(new Vector3(-6.6f, 0, -28));
            Cylinder("船首起锚绞盘", new Vector3(7.6f, .62f, 27), .62f, 1.24f, Dark, true);
            Beam("起锚绞盘横杆", new Vector3(6.2f, 1.24f, 27), new Vector3(9, 1.24f, 27), .12f, Timber, true);
            EHCoil(new Vector3(9, .06f, 26.5f), .64f, 3);
            // A tea ledge is beyond the 2.3m clear circle at (-6,-19).
            EHWorkTable("船上茶水窄案", new Vector3(-9.8f, 0, -18.5f), .95f, .65f, .84f);
            Pot(new Vector3(-9.8f, .84f, -18.5f), .17f);
            Lantern(new Vector3(-9.8f, 3.1f, -17));
            Lantern(new Vector3(9.8f, 3.2f, 18));
            // NPC (0,18), tea (-6,-19), work endpoints (-7,-12)/(7,12)
            // and spawn (0,-25) are all clear, with actual deck underfoot.
        }

        static void EHDepartureHouse(float x, float z, float width, float depth, float height, string sign)
        {
            House(x, z, width, depth, height, Brick, false);
            float front = z - depth * .5f - .27f;
            Box("出洋街店额", new Vector3(x, height - .3f, front), new Vector3(2.5f, .42f, .10f), Dark);
            SignText(sign, new Vector3(x, height - .3f, front - .07f), .085f);
        }

        static void EHTurnedWarehouse(float x, float z, float width, float depth, float height, float yaw)
        {
            Transform parent = world;
            var group = new GameObject("独立沿岸货栈");
            group.transform.SetParent(parent, false); group.transform.localPosition = new Vector3(x, 0, z);
            world = group.transform;
            Warehouse(0, 0, width, depth, height);
            world = parent; group.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        static void EHOpenShelter(string label, Vector3 p, float width, float depth, float height, bool tiled)
        {
            foreach (float x in new[] { -width * .46f, width * .46f })
                foreach (float z in new[] { -depth * .44f, depth * .44f })
                    Cylinder(label + " · timber post", p + new Vector3(x, height * .5f, z), .13f, height, Dark, true);
            Box(label + " · eave beam", p + Vector3.up * height, new Vector3(width, .21f, depth), Dark);
            if (tiled) RoofMesh(p.x, p.y + height + .11f, p.z, width + .8f, depth + .9f, false);
            else ClothShade(p + Vector3.up * (height + .14f), width + .4f, depth + .4f);
        }

        static void EHWater(string label, Vector2 center, Vector2 size)
        {
            int nx = Mathf.Clamp(Mathf.CeilToInt(size.x / 4), 12, 58);
            int nz = Mathf.Clamp(Mathf.CeilToInt(size.y / 4), 12, 58);
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++)
                vertices.Add(new Vector3(center.x - size.x * .5f + x * size.x / nx, -.66f, center.y - size.y * .5f + z * size.y / nz));
            for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++)
            {
                int at = z * (nx + 1) + x;
                triangles.Add(at); triangles.Add(at + nx + 1); triangles.Add(at + 1);
                triangles.Add(at + 1); triangles.Add(at + nx + 1); triangles.Add(at + nx + 2);
            }
            var mesh = FlatMesh(vertices, triangles); mesh.MarkDynamic();
            var water = MeshObject(label, mesh, Vector3.zero, Mat("expanded sea ink teal", "5A7A77"));
            water.AddComponent<WaterMotion>().Initialize(mesh);
        }

        // Board width is 210–240 mm, with staggered butt joints and small paired
        // treenails. Top faces rather than tiny box objects keep large decks cheap.
        static void EHDeckBoards(string label, Vector3 p, float width, float depth, float spacing)
        {
            var light = new List<Vector3>(); var lt = new List<int>();
            var dark = new List<Vector3>(); var dt = new List<int>();
            var pegs = new List<Vector3>(); var pt = new List<int>();
            var edges = new List<Vector3>(); var et = new List<int>();
            float boardWidth = Mathf.Clamp(spacing * .42f, .21f, .24f);
            int columns = Mathf.CeilToInt(width / boardWidth);
            float dx = width / columns, halfDepth = depth * .5f;
            const float length = 2.35f, joint = .008f;
            Quad(edges, et, new Vector3(-width * .5f, -.002f, -halfDepth), new Vector3(-width * .5f, -.002f, halfDepth),
                new Vector3(width * .5f, -.002f, halfDepth), new Vector3(width * .5f, -.002f, -halfDepth));
            for (int col = 0; col < columns; col++)
            {
                float left = -width * .5f + col * dx + joint * .5f, right = left + dx - joint;
                float offset = (col % 4) * length * .25f;
                int row = 0;
                for (float start = -halfDepth - offset; start < halfDepth; start += length, row++)
                {
                    float near = Mathf.Max(start, -halfDepth) + joint * .5f;
                    float far = Mathf.Min(start + length, halfDepth) - joint * .5f;
                    if (far <= near) continue;
                    bool repaired = (col * 7 + row * 3) % 13 < 3;
                    Quad(repaired ? dark : light, repaired ? dt : lt,
                        new Vector3(left, 0, near), new Vector3(left, 0, far), new Vector3(right, 0, far), new Vector3(right, 0, near));
                    if (far - near < .2f) continue;
                    foreach (float xx in new[] { left + .045f, right - .045f })
                        foreach (float zz in new[] { near + .065f, far - .065f })
                            Quad(pegs, pt, new Vector3(xx - .005f, .001f, zz - .005f), new Vector3(xx - .005f, .001f, zz + .005f),
                                new Vector3(xx + .005f, .001f, zz + .005f), new Vector3(xx + .005f, .001f, zz - .005f));
                }
            }
            foreach (float side in new[] { -1f, 1f })
            {
                DetailBox(edges, et, new Vector3(side * (width * .5f - .045f), -.004f, 0), new Vector3(.09f, .018f, depth));
                DetailBox(edges, et, new Vector3(0, -.004f, side * (halfDepth - .045f)), new Vector3(width, .018f, .09f));
            }
            DetailMesh(label + " · narrow sunworn planks", light, lt, p, Mat("expanded old deck wood", "987A53"));
            DetailMesh(label + " · staggered repairs", dark, dt, p, Mat("expanded repaired deck wood", "847053"));
            DetailMesh(label + " · paired treenails", pegs, pt, p, Mat("deck treenails", "514736"));
            DetailMesh(label + " · caulk and edge binding", edges, et, p, Mat("deck caulk wood", "564D3E"));
        }

        static void EHWorkTable(string label, Vector3 p, float width, float depth, float height)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
                DetailBox(v, t, new Vector3(sx * (width * .5f - .09f), (height - .055f) * .5f, sz * (depth * .5f - .08f)), new Vector3(.08f, height - .055f, .08f));
            foreach (float side in new[] { -1f, 1f })
            {
                DetailBox(v, t, new Vector3(0, height - .16f, side * (depth * .5f - .06f)), new Vector3(width - .08f, .16f, .045f));
                DetailBox(v, t, new Vector3(side * (width * .5f - .07f), .28f, 0), new Vector3(.055f, .065f, depth - .12f));
            }
            DetailBox(v, t, new Vector3(0, height - .035f, 0), new Vector3(width, .065f, depth));
            DetailMesh(label + " · joined frame", v, t, p, Timber);
            EHDeckBoards(label + " · tabletop boards", p + Vector3.up * height, width, depth, .52f);
            var obstacle = new GameObject(label + " collision"); obstacle.transform.SetParent(world, false); obstacle.transform.localPosition = p;
            var collider = obstacle.AddComponent<BoxCollider>(); collider.center = Vector3.up * (height * .5f); collider.size = new Vector3(width, height, depth);
        }

        static void EHWaitingBench(Vector3 p)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            DetailBox(v, t, new Vector3(0, .42f, 0), new Vector3(1.8f, .07f, .37f));
            foreach (float side in new[] { -1f, 1f })
                DetailBox(v, t, new Vector3(side * .68f, .20f, 0), new Vector3(.09f, .40f, .32f));
            DetailBox(v, t, new Vector3(0, .18f, 0), new Vector3(1.4f, .065f, .055f));
            DetailMesh("候船人尺度长凳", v, t, p, Timber);
            var obstacle = new GameObject("Waiting bench collision"); obstacle.transform.SetParent(world, false); obstacle.transform.localPosition = p;
            var collider = obstacle.AddComponent<BoxCollider>(); collider.center = Vector3.up * .23f; collider.size = new Vector3(1.8f, .46f, .37f);
        }

        static void EHQuayFace(float line, float start, float end, bool alongZ)
        {
            var dry = new List<Vector3>(); var dt = new List<int>(); var wet = new List<Vector3>(); var wt = new List<int>();
            int count = Mathf.CeilToInt((end - start) / .9f); float step = (end - start) / count;
            for (int row = 0; row < 4; row++) for (int i = 0; i < count; i++)
            {
                float a = start + (i + .5f) * step, y = -1.10f + row * .31f;
                Vector3 pos = alongZ ? new Vector3(line, y, a) : new Vector3(a, y, line);
                Vector3 scale = alongZ ? new Vector3(.14f, .28f, step - .045f) : new Vector3(step - .045f, .28f, .14f);
                DetailBox(row == 0 ? wet : dry, row == 0 ? wt : dt, pos, scale);
            }
            DetailMesh("整列旧石驳岸", dry, dt, Vector3.zero, Stone);
            DetailMesh("潮线湿石", wet, wt, Vector3.zero, Mat("expanded wet quay stone", "626F68"));
        }

        static void EHCoil(Vector3 p, float radius, int turns)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < turns; i++)
            {
                var points = new Vector3[25];
                float r = radius * (1 - i * .10f);
                for (int j = 0; j < points.Length; j++)
                {
                    float angle = j * Mathf.PI * 2 / 24;
                    points[j] = p + new Vector3(Mathf.Cos(angle) * r, i * .037f, Mathf.Sin(angle) * r);
                }
                AppendTube(v, t, points, .025f, .025f);
            }
            DetailMesh("盘绕的旧麻缆", v, t, Vector3.zero, Mat("rope", "AD9B72"));
        }

        static void EHParcel(Vector3 p, float size)
        {
            var parcel = Blob("粗麻布包", p + Vector3.up * size * .43f, new Vector3(size, size * .86f, size * .79f), Linen);
            Box("布包平放木托", p + Vector3.up * .055f, new Vector3(size * 1.1f, .11f, size * .95f), Timber);
            Beam("布包麻绳", p + new Vector3(-size * .43f, size * .49f, 0), p + new Vector3(size * .43f, size * .49f, 0), .042f, Dark);
        }

        static void EHCargoIsland(Vector3 p, int columns, int rows, float size)
        {
            // Multiple box faces share two renderers, with one footprint collider.
            var wood = new List<Vector3>(); var wt = new List<int>(); var bands = new List<Vector3>(); var bt = new List<int>();
            float stride = size + .18f;
            for (int z = 0; z < rows; z++) for (int x = 0; x < columns; x++)
            {
                float xx = (x - (columns - 1) * .5f) * stride, zz = (z - (rows - 1) * .5f) * stride;
                int height = (x + z) % 3 == 0 ? 2 : 1;
                for (int k = 0; k < height; k++)
                {
                    Vector3 c = new Vector3(xx, size * (.5f + k), zz);
                    DetailBox(wood, wt, c, new Vector3(size, size, size));
                    foreach (float side in new[] { -.33f, .33f }) DetailBox(bands, bt, c + new Vector3(side * size, 0, 0), new Vector3(.055f, size + .022f, size + .025f));
                    DetailBeam(bands, bt, c + new Vector3(-size * .42f, -size * .40f, -size * .51f), c + new Vector3(size * .42f, size * .40f, -size * .51f), .09f, .045f);
                }
            }
            DetailMesh("批量旧木货箱", wood, wt, p, Timber); DetailMesh("批量货箱束带与斜撑", bands, bt, p, Dark);
            var collider = new GameObject("Cargo island collision"); collider.transform.SetParent(world, false); collider.transform.localPosition = p;
            var box = collider.AddComponent<BoxCollider>(); box.center = Vector3.up * size;
            box.size = new Vector3(columns * stride - .18f, size * 2, rows * stride - .18f);
        }

        static void EHCrane(Vector3 p, float direction)
        {
            foreach (float x in new[] { -1.3f, 1.3f })
            {
                Cylinder("木起重架承柱", p + new Vector3(x, 3.4f, 0), .18f, 6.8f, Dark, true);
                Beam("起重架斜脚", p + new Vector3(x, .12f, -1.6f), p + new Vector3(x, 4.5f, 0), .18f, Timber);
            }
            Beam("起重架顶梁", p + new Vector3(-1.8f, 6.6f, 0), p + new Vector3(1.8f, 6.6f, 0), .26f, Dark);
            Vector3 end = p + new Vector3(direction * 5.3f, 7.4f, 0);
            Beam("木起重吊臂", p + new Vector3(0, 5.8f, 0), end, .30f, Timber);
            Beam("吊臂拉索", p + new Vector3(-1.1f, 6.7f, 0), end, .035f, Dark);
            Beam("垂下麻吊绳", end, end - Vector3.up * 5, .036f, Dark);
            Cylinder("手摇绞盘", p + new Vector3(0, .6f, -.8f), .42f, 1.2f, Timber, true);
            EHCoil(p + new Vector3(.7f, .04f, -1.9f), .57f, 4);
        }

        static void EHRemoteJunk(Vector3 p, float scale, float yaw)
        {
            Transform parent = world;
            var group = new GameObject("远景帆船组"); group.transform.SetParent(parent, false); group.transform.localPosition = p;
            world = group.transform; Ship(Vector3.zero); world = parent;
            group.transform.localScale = Vector3.one * scale; group.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        static void EHShipHull()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3[] rim = { new Vector3(-11.4f, .16f, -30), new Vector3(0, .26f, -33), new Vector3(11.4f, .16f, -30), new Vector3(11.4f, .20f, 29), new Vector3(0, .65f, 35), new Vector3(-11.4f, .20f, 29) };
            Vector3[] keel = { new Vector3(-8.7f, -2.7f, -28), new Vector3(0, -2.6f, -31), new Vector3(8.7f, -2.7f, -28), new Vector3(8.5f, -2.8f, 28), new Vector3(0, -1.5f, 33), new Vector3(-8.5f, -2.8f, 28) };
            for (int i = 0; i < rim.Length; i++) { int next = (i + 1) % rim.Length; Quad(v, t, rim[i], keel[i], keel[next], rim[next]); }
            DetailMesh("独立远洋木船船壳", v, t, Vector3.zero, Mat("expanded ship hull", "554433"));
            v = new List<Vector3>(); t = new List<int>();
            Quad(v, t, new Vector3(-11, -.02f, 29), new Vector3(-7, -.02f, 32), new Vector3(0, -.02f, 34), new Vector3(11, -.02f, 29));
            Quad(v, t, new Vector3(-11, -.02f, -30), new Vector3(11, -.02f, -30), new Vector3(6, -.02f, -32), new Vector3(0, -.02f, -32.8f));
            DetailMesh("船首尾外伸甲板", v, t, Vector3.zero, Timber);
        }

        static void EHShipRails()
        {
            var rail = new List<Vector3>(); var rt = new List<int>();
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * 10.86f;
                for (int i = 0; i <= 24; i++)
                    DetailBox(rail, rt, new Vector3(x, .59f, -29.3f + i * 2.44f), new Vector3(.14f, 1.18f, .14f));
                DetailBox(rail, rt, new Vector3(x, 1.16f, 0), new Vector3(.21f, .18f, 60));
                DetailBox(rail, rt, new Vector3(x, .52f, 0), new Vector3(.10f, .10f, 60));
                Box("船舷实体护缘", new Vector3(x, .11f, 0), new Vector3(.20f, .22f, 60), Dark, true);
            }
            foreach (float z in new[] { -31.2f, 31.2f })
                DetailBox(rail, rt, new Vector3(0, 1.08f, z), new Vector3(17.5f, .16f, .18f));
            DetailMesh("批量船舷木栏", rail, rt, Vector3.zero, Dark);
        }

        static void EHDeckCabin(Vector3 p, float width, float depth, bool facesNorth)
        {
            float back = facesNorth ? -depth * .5f : depth * .5f;
            float entrance = -back;
            Box("敞顶舱室后壁", p + new Vector3(0, 1.18f, back), new Vector3(width, 2.36f, .18f), Dark, true);
            foreach (float side in new[] { -1f, 1f })
                Box("敞顶舱室侧壁", p + new Vector3(side * width * .5f, 1.18f, 0), new Vector3(.18f, 2.36f, depth), Timber, true);
            Box("舱室门槛", p + new Vector3(0, .045f, entrance), new Vector3(width, .09f, .18f), Timber);
            var frame = new List<Vector3>(); var ft = new List<int>();
            var mats = new List<Vector3>(); var mt = new List<int>();
            var bedding = new List<Vector3>(); var bt = new List<int>();
            int berths = Mathf.FloorToInt((depth - 2) / 2.15f);
            foreach (float side in new[] { -1f, 1f })
            {
                // Sleeping places fit inside the former continuous bunk footprint.
                for (int i = 0; i < berths; i++)
                {
                    Vector3 c = new Vector3(side * width * .33f, 0, (i - (berths - 1) * .5f) * 2.15f);
                    DetailBox(frame, ft, c + Vector3.up * .39f, new Vector3(.87f, .10f, 1.96f));
                    foreach (float sx in new[] { -.36f, .36f }) foreach (float sz in new[] { -.86f, .86f })
                        DetailBox(frame, ft, c + new Vector3(sx, .18f, sz), new Vector3(.08f, .36f, .08f));
                    DetailBox(mats, mt, c + Vector3.up * .455f, new Vector3(.81f, .03f, 1.90f));
                    DetailBox(bedding, bt, c + new Vector3(0, .52f, -.68f), new Vector3(.51f, .10f, .29f));
                    DetailBox(bedding, bt, c + new Vector3(0, .52f, .65f), new Vector3(.75f, .10f, .47f));
                }
                // Same horizontal collision extent, reduced to knee height.
                var obstacle = new GameObject("舱内卧铺 collision"); obstacle.transform.SetParent(world, false); obstacle.transform.localPosition = p;
                var collider = obstacle.AddComponent<BoxCollider>(); collider.center = new Vector3(side * width * .33f, .23f, 0);
                collider.size = new Vector3(.87f, .46f, berths * 2.15f - .19f);
                for (float z = -depth * .5f + .4f; z < depth * .5f; z += .38f)
                    DetailBox(frame, ft, new Vector3(side * (width * .5f - .095f), 1.18f, z), new Vector3(.026f, 2.32f, .025f));
            }
            DetailMesh("舱内分铺与竖向板缝", frame, ft, p, Dark);
            DetailMesh("单人铺草席", mats, mt, p, Linen);
            DetailMesh("铺上枕头叠被", bedding, bt, p, Mat("cabin faded bedding", "A49B7C"));
            ClothShade(p + new Vector3(0, 2.54f, back * .62f), width + .2f, depth * .31f);
            Box("舱内家书木匣", p + new Vector3(0, .21f, back * .72f), new Vector3(.68f, .42f, .44f), Timber);
            Box("舱内叠放旧信", p + new Vector3(0, .433f, back * .72f), new Vector3(.24f, .025f, .17f), Paper);
        }

        static void EHDeckHatch(Vector3 p, float width, float depth)
        {
            Box("货舱舱口盖板", p + Vector3.up * .055f, new Vector3(width, .11f, depth), Dark);
            EHDeckBoards("可拆卸货舱板", p + Vector3.up * .12f, width - .22f, depth - .22f, .41f);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("舱口侧框", p + new Vector3(side * width * .5f, .14f, 0), new Vector3(.16f, .28f, depth + .16f), Timber, true);
                Box("舱口端框", p + new Vector3(0, .14f, side * depth * .5f), new Vector3(width, .28f, .16f), Timber, true);
            }
        }

        static void EHDeckMast(Vector3 p, float height, float sailWidth, float sailHeight)
        {
            Cylinder("甲板主桅", p + Vector3.up * height * .5f, .24f, height, Dark, true);
            Box("桅脚楔木", p + Vector3.up * .22f, new Vector3(1.1f, .44f, 1.1f), Dark, true);
            Sail(p + new Vector3(.25f, height * .66f, .18f), sailWidth, sailHeight);
            foreach (float side in new[] { -1f, 1f })
            {
                Beam("舷侧桅杆支索", p + Vector3.up * (height - .35f), new Vector3(side * 10.6f, 1.1f, p.z + 4.5f), .032f, Dark);
                Beam("桅杆纵支索", p + Vector3.up * (height - .7f), new Vector3(side * 9.8f, 1.05f, p.z - 8), .028f, Dark);
            }
        }

        static void EHShipWheel(Vector3 p)
        {
            Box("船尾舵架", p + new Vector3(0, .48f, 0), new Vector3(.62f, .96f, .48f), Dark, true);
            var v = new List<Vector3>(); var t = new List<int>(); var rim = new Vector3[33];
            for (int i = 0; i < rim.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / 32;
                rim[i] = p + new Vector3(Mathf.Cos(angle) * .45f, 1.16f + Mathf.Sin(angle) * .45f, -.29f);
            }
            AppendTube(v, t, rim, .042f, .042f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 center = p + new Vector3(0, 1.16f, -.29f);
                DetailBeam(v, t, center, center + new Vector3(Mathf.Cos(angle) * .56f, Mathf.Sin(angle) * .56f, 0), .038f, .038f);
            }
            DetailMesh("船尾八辐木舵轮", v, t, Vector3.zero, Timber);
        }
    }
}
