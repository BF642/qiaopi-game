using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    public partial class WorldGame
    {
        // EnterNode may run again for a new task within the same world. Keep exactly
        // one set of furniture, and let the existing world root own its lifetime.
        void SetupNanyangArt()
        {
            if (!world || world.transform.Find("Nanyang daily places")) return;
            if (loadedWorld != "quarters" && loadedWorld != "market" &&
                loadedWorld != "port" && loadedWorld != "postoffice") return;
            var group = new GameObject("Nanyang daily places");
            group.transform.SetParent(world.transform, false);
            NanyangPlaceScenery.Build(group.transform, loadedWorld);
            WalkPath.Invalidate();
        }
    }

    /// <summary>Human-scale furnishings in existing rooms, not new building shells.</summary>
    static class NanyangPlaceScenery
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static Material Wood => Surface("nanyang old timber", "7E6044");
        static Material Dark => Surface("nanyang dark timber", "493B2C");
        static Material Paper => Surface("nanyang unbleached paper", "E8DCBC");
        static Material Ink => Surface("nanyang faded ink", "45473B");
        static Material Red => Surface("nanyang faded red seal", "9C513B");
        static Material Linen => Surface("nanyang woven linen", "AE9D76");
        static Material Teal => Surface("nanyang worn cotton teal", "63796A");
        static Material Iron => Surface("nanyang seasoned iron", "48443B");
        static Material Clay => Surface("nanyang glazed clay bowl", "C4C5AC");
        static Material Rope => Surface("nanyang hemp rope", "A79065");

        public static void Build(Transform root, string place)
        {
            var g = new Geometry(root);
            if (place == "quarters") Quarters(g);
            if (place == "market") Market(g);
            if (place == "port") Port(g);
            if (place == "postoffice") PostOffice(g);
            g.Flush();
        }

        static float Floor(float x, float z) => WorldFactory.CharacterGroundPosition(new Vector3(x, 0, z)).y;

        static void Quarters(Geometry g)
        {
            // Cooking court: the entrance at (22,-18.5) and its central aisle stay open.
            // The diner stands south of this counter; the cook has a separate north side.
            float floor = Floor(24.1f, -12.2f), top = floor + .85f;
            Table(g, new Vector3(24.1f, floor, -12.2f), 3.0f, .82f, .85f, "Serving counter");
            Shade(g, new Vector3(24.1f, floor, -11.65f), 4.4f, 2.7f);
            SoupPot(g, new Vector3(23.3f, top, -12.15f));
            Bowl(g, new Vector3(24.20f, top, -12.33f), true);
            Bowl(g, new Vector3(24.65f, top, -12.29f), true);
            for (int i = 0; i < 3; i++) Bowl(g, new Vector3(25.18f, top + i * .040f, -12.11f), false);
            g.Beam(new Vector3(24.05f, top + .04f, -12.50f), new Vector3(24.29f, top + .04f, -12.40f), .008f, Wood);
            g.Beam(new Vector3(24.07f, top + .04f, -12.52f), new Vector3(24.31f, top + .04f, -12.42f), .008f, Wood);
            g.Box(new Vector3(24.15f, floor + .60f, -12.62f), new Vector3(1.22f, .26f, .04f), Dark);
            g.Sign("热饭 · 汤水", new Vector3(24.15f, floor + .60f, -12.649f), .052f, Paper.color);
            g.Box(new Vector3(24.1f, floor + 2.31f, -13.04f), new Vector3(1.55f, .28f, .065f), Wood);
            g.Sign("灶间饭摊", new Vector3(24.1f, floor + 2.31f, -13.078f), .069f, Paper.color);
            Bench(g, new Vector3(29.5f, Floor(29.5f, -12.1f), -12.1f), 2.6f, 90);
            // A small rice basket sits under the solid counter footprint, not in the aisle.
            Basket(g, new Vector3(24.95f, floor + .04f, -12.18f), .26f, .32f);

            // The original one-metre bed is retained. Add a low head rail, a cotton
            // fold and a bedside letter pouch; the FirstDay sleeping point stays clear.
            foreach (float x in new[] { -29.48f, -28.62f })
                g.Box(new Vector3(x, .63f, 5.10f), new Vector3(.055f, .56f, .055f), Wood);
            g.Box(new Vector3(-29.05f, .86f, 5.10f), new Vector3(.91f, .075f, .055f), Wood);
            g.Box(new Vector3(-29.05f, .69f, 5.10f), new Vector3(.85f, .11f, .035f), Wood);
            for (int fold = 0; fold < 4; fold++)
                g.Box(new Vector3(-29.05f, .548f + fold * .007f, 3.33f + fold * .075f), new Vector3(.80f, .013f, .11f), Teal);
            float bedside = Floor(-28.35f, 5.95f);
            Table(g, new Vector3(-28.35f, bedside, 5.95f), .72f, .50f, .58f, "Bedside letter shelf");
            Vector3 pouch = new Vector3(-28.25f, bedside + .98f, 5.95f);
            Quaternion side = Quaternion.Euler(0, -90, 0);
            g.Box(pouch + side * new Vector3(0, 0, .07f), new Vector3(.54f, .65f, .036f), Wood, side);
            g.Box(pouch, new Vector3(.47f, .44f, .045f), Linen, side);
            g.Box(pouch + side * new Vector3(0, -.075f, -.036f), new Vector3(.48f, .30f, .034f), Teal, side);
            g.Box(pouch + side * new Vector3(-.045f, .095f, -.029f), new Vector3(.30f, .31f, .008f), Paper, side * Quaternion.Euler(0, 0, -5));
            g.Box(pouch + side * new Vector3(-.045f, .105f, -.037f), new Vector3(.067f, .058f, .006f), Red, side);
            g.Sign("家信", pouch + side * new Vector3(0, -.075f, -.058f), .049f, Paper.color, side);
            foreach (float z in new[] { 5.72f, 6.18f })
                g.Beam(new Vector3(-28.32f, bedside + .59f, z), new Vector3(-28.32f, bedside + 1.30f, z), .032f, Wood);
            BoundLetters(g, new Vector3(-28.35f, bedside + .60f, 5.95f), .30f, .035f);
        }

        static void Market(Geometry g)
        {
            float floor = Floor(-14.6f, 16.55f), top = floor + .87f;
            Table(g, new Vector3(-14.6f, floor, 16.55f), 3.8f, .86f, .87f, "Chen account table");
            Ledger(g, new Vector3(-14.67f, top, 16.41f), "进 出");
            Abacus(g, new Vector3(-15.58f, top + .028f, 16.48f), .62f, .34f);
            BoundLetters(g, new Vector3(-13.46f, top + .01f, 16.51f), .37f, .052f);
            InkAndBrush(g, new Vector3(-14.02f, top, 16.60f));
            CounterLabel(g, new Vector3(-13.03f, top, 16.83f), "核账", 0);

            // Shelves face west into the court, with goods held behind the task's stand point.
            Vector3 shelf = new Vector3(-11.85f, Floor(-11.85f, 19.25f), 19.25f);
            Quaternion faceWest = Quaternion.Euler(0, 90, 0);
            Shelf(g, shelf, 2.8f, 1.80f, .55f, faceWest, true);
            Vector3 manifest = new Vector3(-12.19f, shelf.y + 1.14f, 18.7f);
            g.Box(manifest, new Vector3(.57f, .69f, .025f), Dark, faceWest);
            g.Box(manifest + faceWest * new Vector3(0, 0, -.019f), new Vector3(.50f, .60f, .009f), Paper, faceWest);
            g.Sign("交货清单", manifest + faceWest * new Vector3(0, .18f, -.026f), .044f, Ink.color, faceWest);
            g.Sign("米  布  茶", manifest + faceWest * new Vector3(0, -.035f, -.026f), .037f, Ink.color, faceWest);
            g.Sign("核数 · 留单", manifest + faceWest * new Vector3(0, -.21f, -.026f), .030f, Red.color, faceWest);
            g.Box(manifest + faceWest * new Vector3(0, .325f, -.040f), new Vector3(.17f, .036f, .025f), Wood, faceWest);
        }

        static void Port(Geometry g)
        {
            // This shelter already exists. No extra roof or wall is introduced.
            float floor = Floor(-27.4f, 28.65f), top = floor + .89f;
            Table(g, new Vector3(-27.4f, floor, 28.65f), 3.8f, .86f, .89f, "Tally and wage table");
            Ledger(g, new Vector3(-27.45f, top, 28.49f), "工 账");
            Abacus(g, new Vector3(-28.36f, top + .028f, 28.52f), .65f, .36f);
            BoundLetters(g, new Vector3(-26.13f, top + .01f, 28.59f), .38f, .056f);
            InkAndBrush(g, new Vector3(-26.72f, top, 28.69f));
            CounterLabel(g, new Vector3(-28.95f, top, 28.90f), "工账", 0);

            // A few staged sacks on a low pallet make counting an actual piece of work.
            Vector3 pallet = new Vector3(-30.5f, Floor(-30.5f, 30.65f), 30.65f);
            g.Solid("Counted cargo pallet", pallet + Vector3.up * .32f, new Vector3(1.8f, .64f, 1.4f));
            foreach (float x in new[] { -.64f, .64f })
                g.Box(pallet + new Vector3(x, .085f, 0), new Vector3(.12f, .17f, 1.4f), Dark);
            for (int board = 0; board < 7; board++)
                g.Box(pallet + new Vector3(-.78f + board * .26f, .195f, 0), new Vector3(.25f, .055f, 1.40f), Wood);
            foreach (Vector3 offset in new[] { new Vector3(-.45f, .22f, -.31f), new Vector3(.38f, .22f, -.31f), new Vector3(0, .22f, .34f) })
            {
                Vector3 p = pallet + offset;
                g.Lathe(p, new[] { new Vector2(0,0), new Vector2(.24f,.015f), new Vector2(.34f,.16f), new Vector2(.29f,.40f), new Vector2(.095f,.47f), new Vector2(.07f,.53f), new Vector2(0,.53f) }, Linen);
                g.Cylinder(p + Vector3.up * .472f, .105f, .027f, Rope);
            }
            g.Box(pallet + new Vector3(.57f, .64f, -.50f), new Vector3(.40f, .31f, .025f), Dark);
            g.Box(pallet + new Vector3(.57f, .64f, -.518f), new Vector3(.34f, .25f, .008f), Paper);
            g.Sign("点数", pallet + new Vector3(.57f, .70f, -.526f), .041f, Ink.color);
            g.Sign("一  二  三", pallet + new Vector3(.57f, .595f, -.526f), .025f, Red.color);
            g.Beam(pallet + new Vector3(.57f, .20f, -.50f), pallet + new Vector3(.57f, .78f, -.50f), .03f, Wood);
        }

        static void PostOffice(Geometry g)
        {
            // Decorate the original wrapping, checking and archive surfaces in place.
            // They already have collision, so this group adds none in the compound.
            CounterLabel(g, new Vector3(-29, .92f, -13.31f), "收寄", 0);
            Tray(g, new Vector3(-28.03f, .93f, -14.04f), .33f, .33f, Teal);
            BoundLetters(g, new Vector3(-28.03f, .96f, -14.04f), .24f, .035f);
            g.Box(new Vector3(-29.72f, 1.01f, -14.01f), new Vector3(.23f, .045f, .075f), Red);

            CounterLabel(g, new Vector3(11.93f, .92f, -.60f), "待核", -90);
            for (int row = 0; row < 3; row++)
            {
                Vector3 p = new Vector3(11.49f, .928f, -.94f + row * .39f);
                Tray(g, p, .40f, .32f, row == 1 ? Red : Wood);
                BoundLetters(g, p + Vector3.up * .031f, .25f, .025f);
            }
            CounterLabel(g, new Vector3(-13.5f, .92f, 29.45f), "留底", 0);
            Ledger(g, new Vector3(-14.47f, .927f, 28.96f), "留 底", .58f);
            g.Box(new Vector3(-13.9f, 1.005f, 29), new Vector3(.014f, .007f, .32f), Red);
            g.Box(new Vector3(-12.8f, .982f, 29), new Vector3(.014f, .007f, .32f), Rope);
            // The visitor reaches the left end of the existing reply rack. Put a
            // readable end label there, instead of making them walk into the shelf.
            Quaternion west = Quaternion.Euler(0, 90, 0);
            g.Box(new Vector3(27.377f, 1.21f, 1), new Vector3(.33f, .64f, .025f), Dark, west);
            g.Sign("回\n批", new Vector3(27.356f, 1.21f, 1), .085f, Paper.color, west);
        }

        static void Table(Geometry g, Vector3 p, float width, float depth, float height, string name)
        {
            // A single waist-high collider represents the real table, including legs.
            g.Solid(name, p + Vector3.up * (height * .5f), new Vector3(width, height, depth));
            int boards = Mathf.CeilToInt(depth / .17f);
            for (int i = 0; i < boards; i++)
                g.Box(p + new Vector3(0, height - .026f, -depth * .5f + (i + .5f) * depth / boards), new Vector3(width, .052f, depth / boards - .004f), Wood);
            foreach (float x in new[] { -.5f, .5f }) foreach (float z in new[] { -.5f, .5f })
                g.Box(p + new Vector3(x * (width - .15f), (height - .052f) * .5f, z * (depth - .15f)), new Vector3(.073f, height - .052f, .073f), Dark);
            foreach (float z in new[] { -1f, 1f })
                g.Box(p + new Vector3(0, height - .135f, z * (depth * .5f - .045f)), new Vector3(width - .13f, .16f, .043f), Dark);
            foreach (float x in new[] { -1f, 1f })
                g.Box(p + new Vector3(x * (width * .5f - .075f), .25f, 0), new Vector3(.055f, .060f, depth - .10f), Wood);
            g.Box(p + new Vector3(0, .25f, 0), new Vector3(width - .10f, .065f, .052f), Wood);
        }

        static void Bench(Geometry g, Vector3 p, float width, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            g.Solid("Meal bench", p + Vector3.up * .25f, new Vector3(width, .50f, .40f), q);
            g.Box(p + Vector3.up * .475f, new Vector3(width, .06f, .40f), Wood, q);
            foreach (float side in new[] { -.37f, .37f })
            {
                g.Box(p + q * new Vector3(width * side, .235f, 0), new Vector3(.13f, .47f, .32f), Dark, q);
                g.Box(p + q * new Vector3(width * side, .045f, 0), new Vector3(.22f, .08f, .49f), Dark, q);
            }
            g.Box(p + Vector3.up * .22f, new Vector3(width * .78f, .065f, .06f), Wood, q);
        }

        static void Shade(Geometry g, Vector3 p, float width, float depth)
        {
            foreach (float x in new[] { -width * .5f, width * .5f }) foreach (float z in new[] { -depth * .5f, depth * .5f })
            {
                g.Cylinder(p + new Vector3(x, 1.25f, z), .049f, 2.50f, Wood);
                g.Solid("Rice stall slender canopy post", p + new Vector3(x, 1.25f, z), new Vector3(.10f, 2.5f, .10f));
            }
            foreach (float z in new[] { -depth * .5f, depth * .5f })
                g.Beam(p + new Vector3(-width * .5f, 2.50f, z), p + new Vector3(width * .5f, 2.50f, z), .058f, Wood);
            for (int strip = 0; strip < 12; strip++)
            {
                float left = -width * .5f - .15f + strip * (width + .3f) / 12;
                float right = left + (width + .3f) / 12;
                for (int along = 0; along < 8; along++)
                {
                    float za = -depth * .5f - .14f + along * (depth + .28f) / 8;
                    float zb = za + (depth + .28f) / 8;
                    float ya = 2.49f - .09f * Mathf.Sin((along / 8f) * Mathf.PI);
                    float yb = 2.49f - .09f * Mathf.Sin(((along + 1) / 8f) * Mathf.PI);
                    Vector3 a = p + new Vector3(left, ya, za), b = p + new Vector3(left, yb, zb);
                    Vector3 c = p + new Vector3(right, yb, zb), d = p + new Vector3(right, ya, za);
                    g.Quad(a, b, c, d, strip % 4 == 0 ? Teal : Linen);
                    g.Quad(d - Vector3.up * .015f, c - Vector3.up * .015f, b - Vector3.up * .015f, a - Vector3.up * .015f, Linen);
                }
            }
        }

        static void SoupPot(Geometry g, Vector3 p)
        {
            g.Lathe(p, new[] { new Vector2(0,0), new Vector2(.22f,0), new Vector2(.28f,.08f), new Vector2(.29f,.26f), new Vector2(.30f,.29f), new Vector2(.26f,.29f), new Vector2(.255f,.255f), new Vector2(.21f,.045f), new Vector2(0,.045f) }, Iron);
            g.Cylinder(p + Vector3.up * .252f, .258f, .008f, Surface("nanyang soup broth", "93824A"));
            foreach (float side in new[] { -1f, 1f })
            {
                g.Beam(p + new Vector3(side * .28f, .20f, -.075f), p + new Vector3(side * .39f, .20f, -.075f), .030f, Iron);
                g.Beam(p + new Vector3(side * .39f, .20f, -.075f), p + new Vector3(side * .39f, .20f, .075f), .030f, Iron);
                g.Beam(p + new Vector3(side * .39f, .20f, .075f), p + new Vector3(side * .28f, .20f, .075f), .030f, Iron);
            }
            for (int i = 0; i < 6; i++)
                g.Box(p + new Vector3(Mathf.Sin(i * 2.4f) * .15f, .259f, Mathf.Cos(i * 2.4f) * .15f), new Vector3(.043f, .008f, .019f), Teal, Quaternion.Euler(0, i * 31, 0));
            g.Beam(p + new Vector3(.07f, .19f, -.02f), p + new Vector3(.22f, .53f, .21f), .022f, Wood);
        }

        static void Bowl(Geometry g, Vector3 p, bool filled)
        {
            g.Lathe(p, new[] { new Vector2(0,0), new Vector2(.055f,0), new Vector2(.065f,.024f), new Vector2(.12f,.092f), new Vector2(.122f,.11f), new Vector2(.111f,.11f), new Vector2(.055f,.030f), new Vector2(0,.026f) }, Clay);
            g.Lathe(p, new[] { new Vector2(.111f,.101f), new Vector2(.122f,.101f), new Vector2(.122f,.110f), new Vector2(.111f,.110f) }, Teal);
            if (!filled) return;
            g.Cylinder(p + Vector3.up * .106f, .105f, .005f, Surface("nanyang soup broth", "93824A"));
            for (int i = 0; i < 5; i++)
                g.Box(p + new Vector3(Mathf.Sin(i * 2.1f) * .063f, .111f, Mathf.Cos(i * 2.1f) * .063f), new Vector3(.035f, .006f, .012f), Teal, Quaternion.Euler(0, i * 27, 0));
        }

        static void Basket(Geometry g, Vector3 p, float radius, float height)
        {
            g.Lathe(p, new[] { new Vector2(0,0), new Vector2(radius*.74f,0), new Vector2(radius,height*.95f), new Vector2(radius*.94f,height), new Vector2(radius*.88f,height*.90f), new Vector2(radius*.66f,.035f), new Vector2(0,.035f) }, Linen);
            for (int ring = 0; ring < 5; ring++)
            {
                float y = .04f + ring * (height - .06f) / 4, r = radius * Mathf.Lerp(.77f, 1, y / height);
                g.Lathe(p, new[] { new Vector2(r,y), new Vector2(r+.007f,y), new Vector2(r+.007f,y+.011f), new Vector2(r,y+.011f) }, Rope);
            }
        }

        static void Shelf(Geometry g, Vector3 p, float width, float height, float depth, Quaternion q, bool goods)
        {
            g.Solid("Chen stock shelf", p + Vector3.up * (height * .5f), new Vector3(width, height, depth), q);
            foreach (float x in new[] { -width * .5f, width * .5f })
                g.Box(p + q * new Vector3(x, height * .5f, 0), new Vector3(.065f, height, depth), Dark, q);
            g.Box(p + q * new Vector3(0, height * .5f, depth * .5f - .016f), new Vector3(width, height, .032f), Wood, q);
            for (int row = 0; row < 4; row++)
            {
                float y = .12f + row * .52f;
                g.Box(p + q * new Vector3(0, y, 0), new Vector3(width, .046f, depth), Wood, q);
                for (int col = 0; col < 5; col++)
                {
                    Vector3 at = p + q * new Vector3(-width * .39f + col * width * .195f, y + .026f, 0);
                    if (goods && row % 2 == 0)
                    {
                        g.Box(at + Vector3.up * .09f, new Vector3(.38f, .18f, .30f), (col + row) % 2 == 0 ? Linen : Teal, q);
                        g.Box(at + Vector3.up * .186f, new Vector3(.014f, .008f, .31f), Rope, q);
                    }
                    else
                    {
                        g.Lathe(at, new[] { new Vector2(0,0), new Vector2(.095f,0), new Vector2(.13f,.17f), new Vector2(.073f,.25f), new Vector2(.073f,.29f), new Vector2(0,.29f) }, Clay);
                        g.Cylinder(at + Vector3.up * .30f, .08f, .025f, Dark);
                    }
                }
            }
        }

        static void CounterLabel(Geometry g, Vector3 p, string text, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            // A small paper-faced wooden token rests on the desk; the shop's
            // wall sign and the navigation label already identify the room.
            g.Box(p + Vector3.up * .07f, new Vector3(.40f, .14f, .09f), Wood, q);
            g.Box(p + q * new Vector3(0, .072f, -.047f), new Vector3(.33f, .101f, .004f), Paper, q);
            g.Sign(text, p + q * new Vector3(0, .072f, -.051f), .016f, Ink.color, q);
            g.Box(p + q * new Vector3(.137f, .039f, -.051f), new Vector3(.018f, .018f, .003f), Red, q);
        }

        static void Ledger(Geometry g, Vector3 p, string title, float scale = 1)
        {
            g.Box(p + Vector3.up * .014f, new Vector3(.72f, .026f, .45f) * scale, Teal);
            g.Box(p + Vector3.up * .029f, new Vector3(.68f, .012f, .415f) * scale, Paper);
            g.Box(p + Vector3.up * .036f, new Vector3(.010f, .002f, .412f) * scale, Rope);
            for (int col = 0; col < 8; col++)
            {
                float x = (-.285f + col * .081f) * scale;
                g.Box(p + new Vector3(x, .037f, 0), new Vector3(.002f, .002f, .35f) * scale, Red);
                for (int row = 0; row < 5; row++)
                    g.Box(p + new Vector3(x + .014f * scale, .039f, (-.145f + row * .058f) * scale), new Vector3((.018f + (row + col) % 3 * .005f) * scale, .002f, .003f), Ink);
            }
            g.Sign(title, p + new Vector3(0, .043f, .162f * scale), .026f * scale, Red.color, Quaternion.Euler(90, 0, 0));
        }

        static void BoundLetters(Geometry g, Vector3 p, float width, float height)
        {
            g.Box(p + Vector3.up * (height * .5f), new Vector3(width, height, width * 1.25f), Paper);
            g.Box(p + Vector3.up * (height + .004f), new Vector3(.012f, .007f, width * 1.27f), Rope);
            g.Box(p + new Vector3(width * .22f, height + .004f, -.06f), new Vector3(.046f, .006f, .054f), Red);
        }

        static void InkAndBrush(Geometry g, Vector3 p)
        {
            g.Box(p + Vector3.up * .027f, new Vector3(.21f, .054f, .14f), Iron);
            g.Box(p + Vector3.up * .056f, new Vector3(.15f, .004f, .10f), Ink);
            g.Beam(p + new Vector3(-.17f, .030f, -.10f), p + new Vector3(-.14f, .030f, .21f), .009f, Wood);
            g.Beam(p + new Vector3(-.17f, .030f, -.10f), p + new Vector3(-.175f, .030f, -.15f), .008f, Ink);
        }

        static void Abacus(Geometry g, Vector3 p, float width, float depth)
        {
            foreach (float z in new[] { -depth * .5f, depth * .5f, depth * .17f })
                g.Box(p + new Vector3(0, .025f, z), new Vector3(width, .052f, .029f), Dark);
            foreach (float x in new[] { -width * .5f, width * .5f })
                g.Box(p + new Vector3(x, .025f, 0), new Vector3(.030f, .052f, depth), Dark);
            for (int rod = 0; rod < 9; rod++)
            {
                float x = -width * .42f + rod * width * .105f;
                g.Beam(p + new Vector3(x, .023f, -depth * .45f), p + new Vector3(x, .023f, depth * .45f), .007f, Wood);
                for (int bead = 0; bead < 7; bead++)
                {
                    float z = bead < 5 ? -depth * .38f + bead * depth * .087f : depth * .27f + (bead - 5) * depth * .105f;
                    g.Box(p + new Vector3(x, .025f, z), new Vector3(width * .075f, .038f, depth * .070f), Wood);
                }
            }
        }

        static void Tray(Geometry g, Vector3 p, float width, float depth, Material material)
        {
            g.Box(p + Vector3.up * .012f, new Vector3(width, .022f, depth), material);
            foreach (float x in new[] { -width * .5f, width * .5f })
                g.Box(p + new Vector3(x, .04f, 0), new Vector3(.012f, .06f, depth), material);
            g.Box(p + new Vector3(0, .04f, depth * .5f), new Vector3(width, .06f, .012f), material);
        }

        static Material Surface(string name, string hex)
        {
            if (materials.TryGetValue(name, out var material) && material) return material;
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return materials[name] = WorldSurfaceMaterials.Create(name, color);
        }

        // Merge all small pieces by material; labels alone need separate renderers.
        // No temporary primitive GameObjects or unmanaged per-detail meshes are created.
        sealed class Geometry
        {
            readonly Transform root;
            readonly Dictionary<Material, Part> groups = new Dictionary<Material, Part>();
            sealed class Part { public readonly List<Vector3> vertices = new List<Vector3>(); public readonly List<int> indices = new List<int>(); }
            public Geometry(Transform root) { this.root = root; }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material material)
            {
                if (!groups.TryGetValue(material, out var part)) groups[material] = part = new Part();
                int at = part.vertices.Count;
                part.vertices.Add(a); part.vertices.Add(b); part.vertices.Add(c); part.vertices.Add(d);
                part.indices.Add(at); part.indices.Add(at + 1); part.indices.Add(at + 2);
                part.indices.Add(at); part.indices.Add(at + 2); part.indices.Add(at + 3);
            }
            public void Box(Vector3 p, Vector3 size, Material material, Quaternion? rotation = null)
            {
                Quaternion q = rotation ?? Quaternion.identity; Vector3 half = size * .5f;
                Vector3 a=p+q*new Vector3(-half.x,-half.y,-half.z), b=p+q*new Vector3(half.x,-half.y,-half.z);
                Vector3 c=p+q*new Vector3(half.x,half.y,-half.z), d=p+q*new Vector3(-half.x,half.y,-half.z);
                Vector3 e=p+q*new Vector3(-half.x,-half.y,half.z), f=p+q*new Vector3(half.x,-half.y,half.z);
                Vector3 h=p+q*new Vector3(-half.x,half.y,half.z), j=p+q*new Vector3(half.x,half.y,half.z);
                Quad(a,d,c,b,material); Quad(e,f,j,h,material); Quad(a,e,h,d,material);
                Quad(b,c,j,f,material); Quad(d,h,j,c,material); Quad(a,b,f,e,material);
            }
            public void Solid(string name, Vector3 p, Vector3 size, Quaternion? rotation = null)
            {
                var go = new GameObject(name + " collision"); go.transform.SetParent(root, false);
                go.transform.localPosition = p; go.transform.localRotation = rotation ?? Quaternion.identity;
                go.AddComponent<BoxCollider>().size = size;
            }
            public void Beam(Vector3 a, Vector3 b, float width, Material material)
            {
                Box((a+b)*.5f, new Vector3(width, Vector3.Distance(a,b), width), material, Quaternion.FromToRotation(Vector3.up, (b-a).normalized));
            }
            public void Cylinder(Vector3 p, float radius, float height, Material material)
            {
                Lathe(p, new[] { new Vector2(0,-height*.5f), new Vector2(radius,-height*.5f), new Vector2(radius,height*.5f), new Vector2(0,height*.5f) }, material);
            }
            public void Lathe(Vector3 p, Vector2[] profile, Material material)
            {
                const int segments = 24;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                    Vector3 da = new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)), db = new Vector3(Mathf.Sin(b),0,Mathf.Cos(b));
                    for (int j = 0; j < profile.Length - 1; j++)
                    {
                        Vector2 lo = profile[j], hi = profile[j+1];
                        Quad(p+da*lo.x+Vector3.up*lo.y, p+db*lo.x+Vector3.up*lo.y,
                            p+db*hi.x+Vector3.up*hi.y, p+da*hi.x+Vector3.up*hi.y, material);
                    }
                }
            }
            public void Sign(string text, Vector3 p, float size, Color color, Quaternion? rotation = null)
            {
                var font = Resources.Load<Font>("Fonts/Title"); if (!font) return;
                font.RequestCharactersInTexture(text, 48, FontStyle.Normal);
                var go = new GameObject("Place inscription · " + text); go.transform.SetParent(root, false);
                go.transform.localPosition = p; go.transform.localRotation = rotation ?? Quaternion.identity;
                var label = go.AddComponent<TextMesh>(); label.font = font; label.fontSize = 48; label.characterSize = size;
                label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.text = text; label.color = color;
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = font.material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
            public void Flush()
            {
                var lifetime = root.gameObject.AddComponent<DestinationMeshLifetime>();
                foreach (var group in groups)
                {
                    var mesh = new Mesh { name = "Nanyang daily objects · " + group.Key.name, indexFormat = IndexFormat.UInt32 };
                    mesh.SetVertices(group.Value.vertices); mesh.SetTriangles(group.Value.indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    lifetime.meshes.Add(mesh);
                    var go = new GameObject(mesh.name); go.transform.SetParent(root, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
                }
            }
        }
    }
}
