using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        // A modest playable household, not a reconstruction of a named historic home.
        // The existing Chen court is x=-32..-14, z=-17..3; the west gallery roof
        // covers x=-32.375..-28.125. Added rooms use that real footprint.
        // Material reference: Fujian government, 2024-05-09, 红砖厝 / stone footings:
        // https://www.fj.gov.cn/zwgk/ztzl/sxzygwzxsgzx/flsxkmh/202405/t20240509_6445715.htm
        static readonly Dictionary<string, Material> homeSurfaces = new Dictionary<string, Material>();
        Material HomeSurface(string name, string html)
        {
            if (homeSurfaces.TryGetValue(name, out var material) && material) return material;
            return homeSurfaces[name] = WorldSurfaceMaterials.Create("Chen home " + name, C(html));
        }

        void BuildHomeEnvironment()
        {
            if (!homeArt || homeArt.Find("陈家可生活的廊屋")) return;
            Transform root = TradeScenery.Group(homeArt, "陈家可生活的廊屋", Vector3.zero);
            var g = new TradeScenery.Geometry(root);
            Material timber = HomeSurface("worn timber", "80634A"), dark = HomeSurface("dark timber", "514438"),
                lime = HomeSurface("lime plaster", "C4BAA0"), brick = HomeSurface("red brick", "A1664E"),
                stone = HomeSurface("worn granite", "AAA594"), joints = HomeSurface("paving joints", "6F7367"),
                roof = HomeSurface("roof clay", "5C6157"), clay = HomeSurface("earthenware", "947452"),
                soot = HomeSurface("soot", "393B34"), rice = HomeSurface("unbleached linen", "D6C9A8"),
                indigo = HomeSurface("indigo woven linen", "526B6D"), bamboo = HomeSurface("split bamboo", "AA966C"),
                ceramic = HomeSurface("rough porcelain", "C0C5AF");

            if (!ChenCourtyardHouse.Ready) {
            // The upper wall rests exactly on the existing 1.25 m high west wall.
            // It has a real window rather than a painted black rectangle.
            g.Box(new Vector3(-31.92f, 1.93f, -9.35f), new Vector3(.18f, 1.35f, 6.0f), lime);
            g.Box(new Vector3(-31.92f, 1.93f, -1.15f), new Vector3(.18f, 1.35f, 2.30f), lime);
            g.Box(new Vector3(-31.92f, 2.45f, -4.30f), new Vector3(.18f, .30f, 4.0f), lime);
            g.Box(new Vector3(-31.92f, 1.40f, -4.30f), new Vector3(.18f, .28f, 4.0f), lime);
            for (int bar = 0; bar < 11; bar++)
                g.Box(new Vector3(-31.81f, 1.94f, -6.09f + bar * .35f), new Vector3(.06f, .84f, .036f), dark);
            foreach (float y in new[] {1.52f, 2.36f}) g.Box(new Vector3(-31.80f, y, -4.30f), new Vector3(.15f, .075f, 4.0f), timber);
            // Door-height masonry stays at the boundary; playable aisles have no new wall.
            g.Solid("西廊上墙", new Vector3(-31.92f, 1.94f, -9.35f), new Vector3(.18f, 1.35f, 6.0f));
            g.Solid("卧间西墙", new Vector3(-31.92f, 1.94f, -1.15f), new Vector3(.18f, 1.35f, 2.3f));

            HomeEnvFloor(g, new Vector3(-28.82f, 0, -7.65f), 5.9f, 3.1f, stone, joints);
            HomeEnvFloor(g, new Vector3(-23.0f, 0, -4.25f), 4.3f, 3.1f, stone, joints);
            }
            HomeEnvKitchen(g, timber, dark, lime, brick, clay, soot, ceramic, bamboo);
            HomeEnvBedroom(g, timber, dark, lime, rice, indigo, bamboo);
            if (!ChenCourtyardHouse.Ready) HomeEnvKitchenEave(g, timber, dark, roof);
            if (!ChenCourtyardHouse.Ready) HomeEnvDiningShelter(g, timber, dark, roof);
            HomeEnvStall(g, timber, dark, bamboo, rice, clay, soot);

            // Existing functional tables and their colliders stay at the exact same
            // heights. Replace only broad surfaces, and add real plank joins/aprons.
            HomeEnvTable(g, "灶边小案", new Vector3(-26.6f, 0, -7.6f), 1.2f, .7f, .74f, timber, dark);
            HomeEnvTable(g, "阿满的小书案", new Vector3(-26.6f, 0, (ChenCourtyardHouse.Ready ? -.9f : -3.7f)), 1.08f, .65f, .54f, timber, dark);
            HomeEnvTable(g, "陈家饭桌", new Vector3(-23, 0, (ChenCourtyardHouse.Ready ? -.3f : -4.65f)), 1.65f, 1.02f, .76f, timber, dark);
            HomeEnvTable(g, "村口日用摊", new Vector3(-26.4f, 0, -20.3f), 2.15f, 1f, .83f, timber, dark);
            HomeEnvStool(g, new Vector3(-24.35f, 0, (ChenCourtyardHouse.Ready ? -.25f : -4.6f)), .43f, 0, timber, dark);
            HomeEnvStool(g, new Vector3(-21.75f, 0, (ChenCourtyardHouse.Ready ? -.25f : -4.6f)), .43f, 0, timber, dark);
            HomeEnvStool(g, new Vector3(-23, 0, (ChenCourtyardHouse.Ready ? .8f : -3.55f)), .36f + HomeChildMealLift, 90, timber, dark);
            // A broad low footrest supports both feet below the child's higher seat.
            Vector3 childFootrest = new Vector3(-23, 0, (ChenCourtyardHouse.Ready ? .8f : -3.55f) - .24f);
            g.Box(childFootrest + Vector3.up * (HomeChildMealLift - .025f), new Vector3(.50f,.05f,.48f), timber);
            foreach (float side in new[] {-.19f,.19f})
                g.Box(childFootrest + new Vector3(side,(HomeChildMealLift-.05f)*.5f,0), new Vector3(.055f,HomeChildMealLift-.05f,.36f), dark);

            // The lesson has a home: a chalk rest, a bound copybook and a low stool.
            g.Box(new Vector3(-26.97f, .561f, (ChenCourtyardHouse.Ready ? -.95f : -3.75f)), new Vector3(.16f, .035f, .27f), indigo);
            g.Box(new Vector3(-26.97f, .583f, (ChenCourtyardHouse.Ready ? -.95f : -3.75f)), new Vector3(.15f, .008f, .24f), rice);
            g.Box(new Vector3(-26.23f, .554f, (ChenCourtyardHouse.Ready ? -.68f : -3.48f)), new Vector3(.025f, .025f, .14f), ceramic, Quaternion.Euler(0, 14, 0));
            HomeEnvStool(g, new Vector3(-27.48f, 0, (ChenCourtyardHouse.Ready ? -.9f : -3.7f)), .34f, 90, timber, dark);
            // A grain lid rests against the existing storage jar, not in its opening.
            g.Cylinder(new Vector3(-29.08f, .29f, -10.24f), .35f, .045f, bamboo);
            for (int strip = -4; strip <= 4; strip++)
                g.Box(new Vector3(-29.08f + strip * .061f, .315f, -10.24f), new Vector3(.024f, .008f, .49f), timber);
            g.Flush();
            HomeEnvMealDish();
            HomeEnvValidate(root);

            // Soft reflected daylight inside the existing roof. No shadow map,
            // particles, animated shader, audio or global scene-lighting changes.
            var bounce = TradeScenery.Group(root, "灶间石灰墙的暖色反光", new Vector3(-29.35f, 1.92f, -7.25f));
            var light = bounce.gameObject.AddComponent<Light>(); light.type = LightType.Point;
            light.color = C("FFE4BA"); light.intensity = .32f; light.range = 3.75f; light.shadows = LightShadows.None;
        }

        void HomeEnvMealDish()
        {
            if (!homeMeal || homeMeal.transform.Find("青菜豆腐 · 家常菜")) return;
            Vector3 centre = new Vector3(-23, .765f, (ChenCourtyardHouse.Ready ? -.3f : -4.65f));
            // The four individual rice bowls retain their exact contents. Only the
            // central serving bowl becomes a shared household dish.
            foreach (Transform child in homeMeal.transform)
            {
                if (child.name != "粗瓷饭碗" || Vector3.Distance(child.localPosition, centre) > .01f) continue;
                Transform riceContents = child.Find("饱满的晒谷");
                if (riceContents) riceContents.gameObject.SetActive(false);
            }
            Transform dish = TradeScenery.Group(homeMeal.transform, "青菜豆腐 · 家常菜", centre);
            dish.localScale = new Vector3(.72f, .63f, .72f);
            var g = new TradeScenery.Geometry(dish);
            Material green = HomeSurface("cooked greens", "607449"), leafGreen = HomeSurface("leaf greens", "83925F"),
                stem = HomeSurface("vegetable stems", "B0AD72"), tofu = HomeSurface("plain tofu", "D9CFAD");
            // A shallow bed of greens with curved leaves and visible pale stems,
            // not another rice mound. The serving bowl is 32 cm across.
            for (int i = 0; i < 15; i++)
            {
                float angle = i * 2.399963f, radius = .143f * Mathf.Sqrt((i + .5f) / 15);
                Vector3 at = new Vector3(Mathf.Cos(angle) * radius, .089f + (i % 3) * .009f, Mathf.Sin(angle) * radius);
                Quaternion rotation = Quaternion.Euler(0, i * 137.5f + 18, (i % 3 - 1) * 8);
                Material leaf = i % 3 == 0 ? leafGreen : green;
                Vector3 basePoint = at + rotation * new Vector3(0, 0, -.065f),
                    left = at + rotation * new Vector3(-.032f, .004f, -.006f),
                    tip = at + rotation * new Vector3(0, .01f, .052f),
                    right = at + rotation * new Vector3(.032f, .004f, -.006f),
                    ridge = at + rotation * new Vector3(0, .021f, -.005f);
                g.Quad(basePoint, left, tip, ridge, leaf); g.Quad(basePoint, ridge, tip, right, leaf);
                g.Quad(ridge, tip, left, basePoint, leaf); g.Quad(right, tip, ridge, basePoint, leaf);
                g.Beam(basePoint, ridge, .008f, stem);
            }
            foreach (Vector3 at in new[] {new Vector3(-.082f,.125f,-.047f), new Vector3(.046f,.126f,-.071f),
                new Vector3(-.016f,.138f,.012f), new Vector3(.078f,.128f,.056f), new Vector3(-.071f,.127f,.076f)})
                g.Box(at, new Vector3(.046f,.026f,.040f), tofu, Quaternion.Euler(0, at.x * 810, 0));
            g.Flush();
        }

        void HomeEnvFloor(TradeScenery.Geometry g, Vector3 p, float width, float depth, Material stone, Material joints)
        {
            // Original courtyard paving is about 5.8 cm above navigation y=0.
            // Thin fitted slabs sit just above it, without raising collision ground.
            g.Box(p + Vector3.up * .061f, new Vector3(width, .009f, depth), joints);
            int nx = Mathf.CeilToInt(width / .55f), nz = Mathf.CeilToInt(depth / .45f);
            for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++)
                g.Box(p + new Vector3(-width * .5f + (x + .5f) * width / nx, .068f, -depth * .5f + (z + .5f) * depth / nz),
                    new Vector3(width / nx - .011f, .008f, depth / nz - .012f), stone);
        }

        void HomeEnvKitchen(TradeScenery.Geometry g, Material timber, Material dark, Material lime,
            Material brick, Material clay, Material soot, Material ceramic, Material bamboo)
        {
            Vector3 p = new Vector3(-30.2f, .063f, -7.35f);
            // Shoulder-height family cooking area, not an industrial counter.
            g.Box(p + new Vector3(0, .37f, 0), new Vector3(2.62f, .74f, 1.06f), brick);
            g.Box(p + new Vector3(0, .77f, 0), new Vector3(2.72f, .09f, 1.12f), lime);
            g.Solid("砖砌双眼灶", p + new Vector3(0, .41f, 0), new Vector3(2.70f, .82f, 1.1f));
            foreach (float x in new[] {-.67f, .60f})
            {
                g.Box(p + new Vector3(x, .32f, -.543f), new Vector3(.49f, .40f, .025f), soot);
                g.Box(p + new Vector3(x, .55f, -.565f), new Vector3(.66f, .055f, .075f), clay);
                foreach (float side in new[] {-1f, 1f}) g.Box(p + new Vector3(x + side * .275f, .33f, -.562f), new Vector3(.05f, .42f, .064f), clay);
                for (int log = 0; log < 3; log++) g.Beam(p + new Vector3(x - .14f + log * .14f, .16f, -.60f), p + new Vector3(x - .10f + log * .11f, .16f, -.33f), .068f, dark);
            }
            // Shallow iron wok: curved rim, lowered centre and wooden cover on the
            // second fire hole. Nothing burns or produces sound during class.
            g.Lathe(p + new Vector3(-.67f, .82f, 0), new[] {new Vector2(0, -.09f), new Vector2(.21f, -.055f), new Vector2(.37f, .045f), new Vector2(.38f, .08f), new Vector2(.34f, .078f), new Vector2(.19f, -.025f), new Vector2(0, -.05f)}, soot);
            g.Lathe(p + new Vector3(.60f, .82f, 0), new[] {new Vector2(0, 0), new Vector2(.34f, 0), new Vector2(.32f, .06f), new Vector2(.15f, .15f), new Vector2(0, .17f)}, bamboo);
            g.Box(p + new Vector3(.60f, 1.015f, 0), new Vector3(.16f, .065f, .045f), dark);
            g.Beam(p + new Vector3(-.40f, .91f, .21f), p + new Vector3(.0f, .88f, .42f), .025f, timber);
            // Soot-dark chimney is physically tied into the old west gallery roof.
            g.Box(new Vector3(-31.48f, 2.17f, -6.98f), new Vector3(.29f, 2.70f, .39f), brick);
            g.Box(new Vector3(-31.48f, 3.57f, -6.98f), new Vector3(.39f, .10f, .46f), soot);
            // Fixed wall shelf, small bowls, covered seasoning crock and ladles.
            g.Box(new Vector3(-31.44f, 1.77f, -8.65f), new Vector3(.58f, .075f, 1.55f), timber);
            foreach (float z in new[] {-9.18f, -8.12f}) g.Beam(new Vector3(-31.72f, 1.37f, z), new Vector3(-31.18f, 1.73f, z), .06f, dark);
            for (int bowl = 0; bowl < 3; bowl++) HomeEnvEmptyBowl(g, new Vector3(-31.41f, 1.82f + bowl * .055f, -8.93f), .14f, ceramic);
            g.Lathe(new Vector3(-31.41f, 1.82f, -8.37f), new[] {new Vector2(.09f, 0), new Vector2(.13f, .10f), new Vector2(.105f, .24f), new Vector2(.08f, .26f)}, clay);
            g.Cylinder(new Vector3(-31.41f, 2.10f, -8.37f), .115f, .025f, dark);
            for (int i = 0; i < 5; i++) g.Beam(new Vector3(-31.45f + i * .15f, .11f + (i % 2) * .055f, -8.73f), new Vector3(-31.39f + i * .15f, .12f + (i % 2) * .055f, -9.45f), .10f, timber);
            // Water vessel is under the same roof and against the western wall.
            g.Lathe(new Vector3(-31.15f, .065f, -5.66f), new[] {new Vector2(.25f, 0), new Vector2(.38f, .14f), new Vector2(.42f, .46f), new Vector2(.35f, .77f), new Vector2(.30f, .80f), new Vector2(.29f, .73f)}, clay);
            g.Cylinder(new Vector3(-31.15f, .78f, -5.66f), .29f, .016f, soot);
            g.Solid("灶边水缸", new Vector3(-31.15f, .46f, -5.66f), new Vector3(.83f, .80f, .83f));
            HomeEnvEmptyBowl(g, new Vector3(-31.0f, .89f, -5.65f), .13f, bamboo);
        }

        void HomeEnvBedroom(TradeScenery.Geometry g, Material timber, Material dark, Material lime, Material linen, Material indigo, Material bamboo)
        {
            Vector3 bed = new Vector3(-30.54f, 0, -3.26f);
            // 1.55 × 2.04 m wooden sleeping platform, raised on four real legs.
            g.Box(bed + Vector3.up * .40f, new Vector3(1.60f, .12f, 2.12f), dark);
            g.Solid("西厢木床", bed + Vector3.up * .28f, new Vector3(1.61f, .50f, 2.13f));
            foreach (float x in new[] {-.69f, .69f}) foreach (float z in new[] {-.94f, .94f})
                g.Box(bed + new Vector3(x, .22f, z), new Vector3(.105f, .38f, .105f), timber);
            for (int strip = 0; strip < 24; strip++) g.Box(bed + new Vector3(0, .471f, -.97f + strip * .084f), new Vector3(1.51f, .025f, .075f), bamboo);
            foreach (float z in new[] {-.98f, .98f}) g.Box(bed + new Vector3(0, .486f, z), new Vector3(1.50f, .020f, .054f), linen);
            // Folded cotton quilt sits across the foot; restrained seams break the
            // old featureless slab without making a modern upholstered mattress.
            g.Box(bed + new Vector3(0, .535f, -.59f), new Vector3(1.32f, .09f, .59f), indigo);
            g.Box(bed + new Vector3(0, .579f, -.61f), new Vector3(1.30f, .009f, .075f), linen);
            for (int fold = 0; fold < 3; fold++) g.Box(bed + new Vector3(-.664f, .507f + fold * .028f, -.59f), new Vector3(.007f, .008f, .56f), linen);
            g.Box(bed + new Vector3(-.34f, .53f, .70f), new Vector3(.52f, .09f, .28f), linen);
            g.Box(bed + new Vector3(.34f, .53f, .70f), new Vector3(.52f, .09f, .28f), linen);
            // Headboard and back corner give the bed a place in a room, rather than
            // a prop outdoors. Existing gallery roof covers all of this sleeping bay.
            for (int plank = 0; plank < 8; plank++) g.Box(new Vector3(-31.29f + plank * .215f, .90f, -2.12f), new Vector3(.206f, .91f, .065f), timber);
            g.Box(new Vector3(-30.54f, 1.36f, -2.12f), new Vector3(1.72f, .075f, .10f), dark);
            g.Box(new Vector3(-30.41f, 1.40f, -.74f), new Vector3(2.90f, 2.56f, .11f), lime);
            g.Solid("卧间后屏", new Vector3(-30.41f, 1.40f, -.74f), new Vector3(2.90f, 2.56f, .11f));
            // Half-raised split-bamboo blind gives privacy without blocking the
            // approach or hiding the bed below an opaque hanging curtain.
            for (int row = 0; row < 14; row++) g.Box(new Vector3(-29.46f, 2.63f - row * .052f, -2.9f), new Vector3(.03f, .034f, 2.45f), bamboo);
            foreach (float z in new[] {-3.76f, -2.04f}) g.Beam(new Vector3(-29.43f, 2.68f, z), new Vector3(-29.43f, 1.91f, z), .009f, dark);
            // Modest storage chest at the bed foot, wholly within the west bay.
            g.Box(new Vector3(-30.49f, .31f, -4.78f), new Vector3(1.08f, .47f, .57f), timber);
            g.Box(new Vector3(-30.49f, .567f, -4.78f), new Vector3(1.12f, .055f, .60f), dark);
            g.Box(new Vector3(-30.49f, .45f, -5.077f), new Vector3(.09f, .12f, .022f), bamboo);
            g.Solid("旧衣箱", new Vector3(-30.49f, .33f, -4.78f), new Vector3(1.13f, .57f, .61f));
        }

        void HomeEnvKitchenEave(TradeScenery.Geometry g, Material timber, Material dark, Material roof)
        {
            // Lean-to overlaps the existing west roof eave at x=-28.125; it does
            // not create a detached floating roof over the original worktable.
            Vector3 backA = new Vector3(-28.34f, 3.04f, -9.54f), backB = new Vector3(-28.34f, 3.04f, -5.83f);
            Vector3 frontA = new Vector3(-25.68f, 2.77f, -9.54f), frontB = new Vector3(-25.68f, 2.77f, -5.83f);
            HomeEnvRoofPanel(g, backA, backB, frontB, frontA, roof, dark);
            for (int row = 0; row < 15; row++)
            {
                float z = -9.52f + row * .263f;
                g.Beam(new Vector3(-28.34f, 3.064f, z), new Vector3(-25.68f, 2.795f, z), .036f, roof);
                if (row % 3 == 0) g.Beam(new Vector3(-28.31f, 2.84f, z), new Vector3(-25.70f, 2.57f, z), .085f, timber);
            }
            foreach (float x in new[] {-28.20f, -25.87f}) g.Box(new Vector3(x, x < -27 ? 2.77f : 2.55f, -7.685f), new Vector3(.13f, .16f, 3.73f), dark);
            foreach (float z in new[] {-9.36f, -6.03f}) HomeEnvPost(g, new Vector3(-25.87f, 0, z), 2.62f, timber);
        }

        void HomeEnvDiningShelter(TradeScenery.Geometry g, Material timber, Material dark, Material roof)
        {
            // A small open dining shelter keeps all four sides accessible. Thin
            // rafters frame the people; no opaque wall is inserted behind them.
            foreach (float x in new[] {-25.05f, -20.95f}) foreach (float z in new[] {-5.95f, -2.45f}) HomeEnvPost(g, new Vector3(x, 0, z), 2.68f, timber);
            foreach (float x in new[] {-25.05f, -20.95f}) g.Box(new Vector3(x, 2.62f, -4.2f), new Vector3(.16f, .17f, 3.8f), dark);
            foreach (float z in new[] {-5.95f, -2.45f}) g.Box(new Vector3(-23, 2.70f, z), new Vector3(4.45f, .17f, .16f), dark);
            foreach (float side in new[] {-1f, 1f})
            {
                Vector3 a = new Vector3(-25.36f, 3.37f, -4.2f), b = new Vector3(-20.64f, 3.37f, -4.2f);
                Vector3 c = new Vector3(-20.64f, 2.90f, -4.2f + side * 2.02f), d = new Vector3(-25.36f, 2.90f, -4.2f + side * 2.02f);
                HomeEnvRoofPanel(g, a, b, c, d, roof, dark);
                for (int tile = 0; tile < 20; tile++)
                {
                    float x = -25.33f + tile * .245f;
                    g.Beam(new Vector3(x, 3.40f, -4.2f), new Vector3(x, 2.93f, -4.2f + side * 2.03f), .037f, roof);
                    if (tile % 4 == 0) g.Beam(new Vector3(x, 3.15f, -4.2f), new Vector3(x, 2.68f, -4.2f + side * 1.98f), .075f, timber);
                }
                for (int course = 1; course < 6; course++) g.Box(new Vector3(-23, 3.398f - course * .0775f, -4.2f + side * course * .336f), new Vector3(4.72f, .017f, .039f), roof);
            }
            g.Beam(new Vector3(-25.40f, 3.415f, -4.2f), new Vector3(-20.60f, 3.415f, -4.2f), .087f, roof);
        }

        void HomeEnvStall(TradeScenery.Geometry g, Material timber, Material dark, Material bamboo, Material linen, Material clay, Material soot)
        {
            // Replace the rigid slab awning with sewn, gently sagging cloth.
            Transform old = homeArt.Find("旧布凉棚"); if (old) { var r = old.GetComponent<Renderer>(); if (r) r.enabled = false; }
            for (int strip = 0; strip < 10; strip++)
            {
                float xa = -27.72f + strip * .264f, xb = xa + .264f;
                float ya = 2.58f - Mathf.Sin(strip * Mathf.PI / 10) * .10f, yb = 2.58f - Mathf.Sin((strip + 1) * Mathf.PI / 10) * .10f;
                Vector3 a = new Vector3(xa, ya, -19.36f), b = new Vector3(xb, yb, -19.36f), c = new Vector3(xb, yb - .19f, -21.20f), d = new Vector3(xa, ya - .19f, -21.20f);
                g.Quad(a, b, c, d, linen); g.Quad(d, c, b, a, linen);
                if (strip % 2 == 0) g.Beam(a + Vector3.up * .008f, d + Vector3.up * .008f, .009f, bamboo);
            }
            foreach (float x in new[] {-27.49f, -25.31f})
            {
                g.Beam(new Vector3(x, .08f, -21.07f), new Vector3(x, 2.36f, -21.07f), .065f, bamboo);
                g.Beam(new Vector3(x, 2.36f, -21.07f), new Vector3(x, 2.54f, -19.5f), .056f, bamboo);
            }
            g.Beam(new Vector3(-27.68f, 2.38f, -21.12f), new Vector3(-25.13f, 2.38f, -21.12f), .052f, dark);
            g.Box(new Vector3(-26.4f, .61f, -20.805f), new Vector3(2.00f, .25f, .028f), linen);
            // Goods have different silhouettes: oil jars, tied packets and a scoop.
            for (int jar = 0; jar < 2; jar++)
            {
                Vector3 at = new Vector3(-27.0f + jar * .25f, .84f, -20.60f);
                g.Lathe(at, new[] {new Vector2(.06f, 0), new Vector2(.095f, .10f), new Vector2(.08f, .22f), new Vector2(.035f, .26f), new Vector2(.035f, .31f)}, clay);
                g.Cylinder(at + Vector3.up * .324f, .044f, .025f, soot);
            }
            g.Box(new Vector3(-26.42f, .86f, -20.62f), new Vector3(.24f, .035f, .28f), bamboo);
            g.Beam(new Vector3(-26.43f, .885f, -20.61f), new Vector3(-26.08f, .885f, -20.61f), .025f, timber);
        }

        void HomeEnvTable(TradeScenery.Geometry g, string name, Vector3 p, float width, float depth, float height, Material timber, Material dark)
        {
            Transform old = homeArt.Find(name); if (old) { var r = old.GetComponent<Renderer>(); if (r) r.enabled = false; }
            int count = Mathf.Max(3, Mathf.RoundToInt(depth / .14f));
            for (int board = 0; board < count; board++) g.Box(p + new Vector3(0, height - .029f, -depth * .5f + (board + .5f) * depth / count), new Vector3(width, .058f, depth / count - .004f), timber);
            foreach (float z in new[] {-.5f, .5f}) g.Box(p + new Vector3(0, height - .135f, z * (depth - .13f)), new Vector3(width - .13f, .145f, .038f), dark);
            foreach (float x in new[] {-.5f, .5f}) g.Box(p + new Vector3(x * (width - .13f), height - .135f, 0), new Vector3(.038f, .145f, depth - .13f), dark);
        }

        void HomeEnvStool(TradeScenery.Geometry g, Vector3 p, float height, float yaw, Material timber, Material dark)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            for (int board = 0; board < 3; board++) g.Box(p + q * new Vector3(0, height - .027f, -.105f + board * .105f), new Vector3(.42f, .054f, .098f), timber, q);
            foreach (float x in new[] {-.155f, .155f}) foreach (float z in new[] {-.105f, .105f})
                g.Beam(p + q * new Vector3(x * 1.15f, .065f, z * 1.15f), p + q * new Vector3(x, height - .045f, z), .046f, dark);
            g.Box(p + Vector3.up * (height * .42f), new Vector3(.32f, .039f, .044f), dark, q);
            // No stool collider: actor arrival and lower-leg clearance remain valid.
        }

        void HomeEnvPost(TradeScenery.Geometry g, Vector3 p, float height, Material timber)
        {
            g.Box(p + Vector3.up * (height * .5f + .03f), new Vector3(.115f, height, .115f), timber);
            g.Solid("廊棚木柱", p + Vector3.up * (height * .5f + .03f), new Vector3(.115f, height, .115f));
        }
        void HomeEnvRoofPanel(TradeScenery.Geometry g, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material tile, Material timber)
        {
            // Both faces are intentional so the same compact helper handles east
            // and west roof planes; the underside is a separate 12 cm lower face.
            g.Quad(a, b, c, d, tile); g.Quad(d, c, b, a, tile);
            Vector3 down = Vector3.down * .12f;
            g.Quad(d + down, c + down, b + down, a + down, timber); g.Quad(a + down, b + down, c + down, d + down, timber);
            g.Quad(a, a + down, b + down, b, tile); g.Quad(b, b + down, c + down, c, tile);
            g.Quad(c, c + down, d + down, d, tile); g.Quad(d, d + down, a + down, a, tile);
        }
        void HomeEnvEmptyBowl(TradeScenery.Geometry g, Vector3 p, float radius, Material material)
        {
            g.Lathe(p, new[] {new Vector2(radius * .40f, 0), new Vector2(radius * .65f, .035f), new Vector2(radius, .115f), new Vector2(radius * .91f, .125f), new Vector2(radius * .55f, .050f), new Vector2(0, .047f)}, material);
        }
        void HomeEnvValidate(Transform root)
        {
            // Verify new solids cannot occupy any scripted interaction / family
            // standing spot. Full routes are checked by the normal chapter replay.
            Vector3[] safe = {HomeWifeStart, HomeMarket, HomeKitchen, HomeDrying, HomeStorage, HomeLesson, HomeDinner,
                new Vector3(-28.7f, 0, -8.4f), new Vector3(-26.2f, 0, (ChenCourtyardHouse.Ready ? .1f : -2.7f)),
                HomeMotherDinner, HomeSisterDinner, new Vector3(-21.75f, 0, (ChenCourtyardHouse.Ready ? -.25f : -4.6f))};
            foreach (var col in root.GetComponentsInChildren<BoxCollider>())
            {
                Bounds bound = new Bounds(col.transform.TransformPoint(col.center), Vector3.Scale(col.size, col.transform.lossyScale));
                foreach (Vector3 p in safe)
                    if (bound.SqrDistance(p + Vector3.up * .80f) < .40f * .40f)
                        Debug.LogError("HOME_ENVIRONMENT_BLOCKED " + col.name + " at " + p);
            }
            int vertices = 0, triangles = 0;
            foreach (var mesh in root.GetComponentsInChildren<MeshFilter>()) if (mesh.sharedMesh) { vertices += mesh.sharedMesh.vertexCount; triangles += mesh.sharedMesh.triangles.Length / 3; }
            Debug.Log("HOME_ENVIRONMENT_BUILT meshes=" + root.GetComponentsInChildren<MeshFilter>().Length + " vertices=" + vertices + " triangles=" + triangles + " colliders=" + root.GetComponentsInChildren<BoxCollider>().Length);
        }
    }
}
