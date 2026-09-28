using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public static partial class WorldFactory
    {
        // These are scenery extensions, not extra navigation space. ESBegin and the post
        // office both finish their walkable slab at y=0. The landscape hides its sides.
        internal static void SettlementSurroundings(string kind)
        {
            if (kind != "quanzhou" && kind != "market" && kind != "quarters" && kind != "postoffice") return;
            Transform parent = world;
            var surroundings = new GameObject("Settlement surroundings · " + kind);
            surroundings.transform.SetParent(parent, false);
            world = surroundings.transform;
            try
            {
                Material ground = kind == "quanzhou" ? Mat("expanded village earth", "9E8A69") :
                    kind == "market" ? Mat("market old granite earth", "A99E88") :
                    kind == "quarters" ? Mat("quarters packed earth", "A49479") : Mat("post courtyard earth", "9B9079");
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                Quad(vertices, triangles, new Vector3(-600, -.025f, -597), new Vector3(-600, -.025f, 603),
                    new Vector3(600, -.025f, 603), new Vector3(600, -.025f, -597));
                MeshObject("Continuous 1200m landscape · top -0.025m", FlatMesh(vertices, triangles), Vector3.zero, ground);

                var scene = new SSGeometry(kind);
                if (kind == "quanzhou") SSVillageLandscape(scene);
                else SSTownLandscape(scene, kind);
                scene.Finish();
            }
            finally
            {
                // No primitives with physics are used below. This defensive cleanup also
                // keeps future background embellishments from changing pathfinding.
                foreach (Collider collider in surroundings.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                    if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
                }
                world = parent;
            }
        }

        static Material SSPath => Mat("surroundings trodden earth", "B0A080");
        static Material SSRoad => Mat("surroundings worn street", "A69D89");
        static Material SSSoil => Mat("surroundings cultivated soil", "87765A");
        static Material SSDitch => Mat("surroundings irrigation water", "59685B");
        static Material SSLeaf => Mat("surroundings foliage", "657149");
        static Material SSLeafDark => Mat("surroundings deep foliage", "4A5F41");
        static Material SSCrop => Mat("surroundings young crops", "7A8150");
        static Material SSStraw => Mat("surroundings dry straw", "A59360");
        static Material SSRough => Mat("surroundings rough limewash", "B6A789");
        static Material SSBrickPale => Mat("surroundings old brick patches", "AA694E");

        static void SSVillageLandscape(SSGeometry s)
        {
            // The village opens into cultivated land. Roads have actual destinations:
            // farm clusters, a larger western hamlet, and the northern field road.
            SSRibbon(s, SSPath, 7.5f, new Vector2(0, 41.5f), new Vector2(0, 58), new Vector2(8, 83), new Vector2(15, 116), new Vector2(8, 182));
            SSRibbon(s, SSPath, 7, new Vector2(0, -35.5f), new Vector2(0, -57), new Vector2(-7, -91), new Vector2(-13, -180));
            SSRibbon(s, SSPath, 6, new Vector2(-39.5f, 4), new Vector2(-73, 4), new Vector2(-110, -2), new Vector2(-180, 7));
            SSRibbon(s, SSPath, 5.5f, new Vector2(39.5f, 15), new Vector2(70, 15), new Vector2(100, 24), new Vector2(180, 31));

            foreach (float side in new[] { -1f, 1f })
            {
                SSField(s, side * 60, -18, 30, 24, side < 0 ? 0 : 1);
                SSField(s, side * 60, 34, 28, 18, 1);
                SSField(s, side * 99, -30, 34, 29, 2);
                SSField(s, side * 107, 53, 39, 33, 0);
                SSField(s, side * 64, 86, 31, 27, 2);
                SSField(s, side * 105, 101, 39, 36, 1);
                SSField(s, side * 61, -76, 31, 32, 0);
                SSField(s, side * 104, -88, 39, 39, 2);
                SSField(s, side * 29, -61, 37, 30, 1);
                SSField(s, side * 33, -108, 35, 36, 0);
            }
            SSField(s, -26, 64, 31, 32, 0);
            SSField(s, 32, 65, 25, 30, 1);
            SSField(s, -24, 109, 35, 36, 2);
            SSField(s, 39, 112, 28, 32, 0);

            // Modest single-storey houses are grouped around lanes and earth courts.
            SSFarmstead(s, -63, 16, 0, false);
            SSFarmstead(s, -84, 14, 0, true);
            SSFarmstead(s, 63, 51, 0, false);
            SSFarmstead(s, 90, -57, 180, true);
            SSFarmstead(s, -58, -47, 90, false);
            SSFarmstead(s, -53, 118, 0, true);
            SSFarmstead(s, 72, 119, 0, false);
            SSVillageBridge(s, -73, 4);
            SSVillageBridge(s, 71, 15);

            foreach (Vector2 p in new[] { new Vector2(-45, -27), new Vector2(-47, 30), new Vector2(46, -20),
                new Vector2(47, 39), new Vector2(-31, 45), new Vector2(29, -41), new Vector2(-91, 8),
                new Vector2(84, 33), new Vector2(-122, -60), new Vector2(125, 83), new Vector2(-11, 143) })
                SSTree(s, p.x, p.y, s.Range(4.4f, 7.1f), false);

            // Low hedgerows soften plot boundaries without becoming a wall around the map.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 11; i++)
                {
                    float z = -119 + i * 24 + s.Range(-5, 5);
                    SSTree(s, side * s.Range(137, 159), z, s.Range(4, 7), false);
                    SSGrove(s, side * s.Range(120, 138), z + 9, 4);
                }
            for (int i = 0; i < 16; i++)
                SSGrove(s, -126 + i * 17, i % 2 == 0 ? -143 : 156, 3);
            SSOuterTerrain(s, true);
        }

        static void SSField(SSGeometry s, float x, float z, float w, float d, int crop)
        {
            Material bed = crop == 0 ? Mat("surroundings moist field bed", "6E7355") : SSSoil;
            float elevation = .28f + (crop == 1 ? .56f : crop == 2 ? .30f : 0) + (Mathf.Abs(x) > 85 ? .32f : 0);
            // Solid battered earth banks give adjacent fields distinct levels. Roads and
            // farmsteads stay on their existing plane; every vertex remains outside the game area.
            SSBatteredField(s,x,z,w,d,elevation,bed);
            s.Box(SSPath, new Vector3(x - w * .5f, elevation + .055f, z), new Vector3(.55f, .15f, d + .7f));
            s.Box(SSPath, new Vector3(x + w * .5f, elevation + .055f, z), new Vector3(.55f, .15f, d + .7f));
            s.Box(SSPath, new Vector3(x, elevation + .055f, z - d * .5f), new Vector3(w, .15f, .55f));
            s.Box(SSPath, new Vector3(x, elevation + .055f, z + d * .5f), new Vector3(w, .15f, .55f));
            s.Rect(SSDitch, x - w * .5f - .8f, z, .75f, d, -.008f);
            int rows = Mathf.Max(3, Mathf.FloorToInt(w / 2.7f));
            for (int r = 0; r < rows; r++)
            {
                float xx = x - w * .5f + 1.4f + r * (w - 2.8f) / (rows - 1);
                s.Box(crop == 2 ? SSStraw : SSPath, new Vector3(xx, elevation + .03f, z), new Vector3(crop == 2 ? .55f : .25f, .09f, d - 1.5f));
                for (float zz = z - d * .5f + 1.2f; zz < z + d * .5f - 1; zz += crop == 2 ? 2.7f : 2.1f)
                {
                    float h = crop == 0 ? s.Range(.30f, .54f) : crop == 1 ? s.Range(.22f, .43f) : s.Range(.38f, .62f);
                    Material leaf = crop == 2 ? SSStraw : (r % 3 == 0 ? SSLeaf : SSCrop);
                    SSGrass(s, new Vector3(xx + s.Range(-.18f, .18f), elevation, zz), h, leaf);
                }
            }
        }

        static void SSFarmstead(SSGeometry s, float x, float z, float yaw, bool stone)
        {
            Quaternion turn = Quaternion.Euler(0, yaw, 0);
            Vector3 origin = new Vector3(x, 0, z);
            SSBuilding(s, origin, turn, 7.3f, 8.5f, 3.1f, stone ? SSRough : Brick, false, false);
            Vector3 wing = origin + turn * new Vector3(8, 0, 1.2f);
            SSBuilding(s, wing, turn, 5.2f, 6.1f, 2.5f, SSRough, false, false);
            SSLocalBox(s, SSPath, origin, turn, new Vector3(2, -.005f, -9.1f), new Vector3(17, .018f, 9));
            SSLocalBox(s, Stone, origin, turn, new Vector3(-5.7f, .37f, -7), new Vector3(.38f, .78f, 6.4f));
            SSLocalBox(s, Brick, origin, turn, new Vector3(11.1f, .37f, -7), new Vector3(.38f, .78f, 6.4f));
            SSLocalBox(s, Dark, origin, turn, new Vector3(8.5f, .62f, -6.7f), new Vector3(1.5f, 1.24f, 1.1f));
            Vector3 tree = origin + turn * new Vector3(-7.5f, 0, -2.5f);
            SSTree(s, tree.x, tree.z, 5.5f, false);
        }

        static void SSVillageBridge(SSGeometry s, float x, float z)
        {
            s.Rect(SSDitch, x, z, .75f, 30, -.01f);
            for (int i = 0; i < 6; i++) s.Box(Stone, new Vector3(x, .025f, z - 1.4f + i * .56f), new Vector3(2.8f, .12f, .48f));
        }

        static void SSTownLandscape(SSGeometry s, string kind)
        {
            bool quarters = kind == "quarters";
            bool office = kind == "postoffice";
            // A street network continues through the surrounding blocks, rather than
            // ending at a decorative ring of buildings. The four inner strips begin
            // outside even ESBegin's larger physical floor.
            SSStreet(s, -45, 3, 9, 82, quarters);
            SSStreet(s, 45, 3, 9, 82, quarters);
            SSStreet(s, 0, 46.5f, 100, 9, quarters);
            SSStreet(s, 0, -40.5f, 100, 9, quarters);
            SSStreet(s, 0, 119, 8, 136, quarters);
            SSStreet(s, 0, -113, 8, 136, quarters);
            foreach (float side in new[] { -1f, 1f })
            {
                SSStreet(s, side * 89, 3, 7, 282, quarters);
                SSStreet(s, side * 126, 3, 6, 282, quarters);
                SSStreet(s, side * 117, 44.5f, 142, 7, quarters);
                SSStreet(s, side * 117, -37.5f, 142, 7, quarters);
                SSStreet(s, side * 119, 6, 146, 5, quarters);
            }
            foreach (float z in new[] { 82f, 119f, -76f, -112f }) SSStreet(s, 0, z, 282, 6, quarters);

            if (quarters) SSResidentialBlocks(s);
            else SSCommercialBlocks(s, office);

            // Streets have open rear courts, drainage, shade trees and practical work
            // yards. They remain visible between the roof rows in a distant view.
            foreach (float side in new[] { -1f, 1f })
            {
                SSCourt(s, side * 75, 25, 13, 22, quarters);
                SSCourt(s, side * 112.5f, -16, 13, 23, quarters);
                SSCourt(s, side * 112.5f, 60, 13, 24, quarters);
                SSCourt(s, side * 70, -69, 15, 5, quarters);
                for (int i = 0; i < 5; i++)
                {
                    float z = -96 + i * 48;
                    SSTree(s, side * (i % 2 == 0 ? 134 : 94), z, quarters ? 6.2f : 7.8f, i % 2 == 0);
                    SSGrove(s, side * 135, z + 10, 3);
                }
                SSTree(s, side * 48, 51, 6.3f, true);
                SSTree(s, side * 50, -47, 5.2f, false);
            }
            for (int i = 0; i < 8; i++)
            {
                SSTree(s, -116 + i * 32, 136 + i % 2 * 9, 5.5f + i % 3, i % 3 == 0);
                SSGrove(s, -114 + i * 32, -133 - i % 2 * 9, 4);
            }
            SSOuterTerrain(s, false);
        }

        static void SSCommercialBlocks(SSGeometry s, bool office)
        {
            // Three northward street rows, including rear courts and narrow alley gaps.
            for (int row = 0; row < 3; row++)
                for (int i = -11; i <= 11; i++)
                {
                    if (i == 0 || i == -5 || i == 5 || i == -10 || i == 10) continue;
                    float x = i * 8.6f, z = 62 + row * 37;
                    Material wall = (i + row + 24) % 4 == 0 ? Brick : (i % 3 == 0 ? SSRough : Cream);
                    SSBuilding(s, new Vector3(x, 0, z), Quaternion.identity, 8.05f, 16, 6.1f + (i + 12) % 3 * .28f, wall, true, office);
                }
            // Streets along the eastern and western edges contain two further rows.
            foreach (float side in new[] { -1f, 1f })
                for (int row = 0; row < 2; row++)
                    for (int i = 0; i < 7; i++)
                    {
                        if (i == 3) continue; // cross street, not a walled-off block
                        float x = side * (59 + row * 38), z = -28 + i * 10.5f;
                        SSBuilding(s, new Vector3(x, 0, z), Quaternion.Euler(0, side > 0 ? 90 : -90, 0), 9.8f, 16,
                            5.7f + (i % 3) * .35f, i % 4 == 0 ? SSBrickPale : SSRough, true, office);
                    }
            // Lower southern shop backs preserve views into the playable streets.
            for (int row = 0; row < 2; row++)
                for (int i = -11; i <= 11; i++)
                {
                    if (i == 0 || Mathf.Abs(i) == 5 || Mathf.Abs(i) == 10) continue;
                    SSBuilding(s, new Vector3(i * 8.6f, 0, -58 - row * 36), Quaternion.identity, 7.9f, 15,
                        row == 0 ? 3.0f : 4.0f, i % 3 == 0 ? Brick : SSRough, false, office);
                }
        }

        static void SSResidentialBlocks(SSGeometry s)
        {
            // Boarding houses use long, low roof masses and washing courts. There is
            // much less commercial frontage than around the market or letter office.
            foreach (float side in new[] { -1f, 1f })
                for (int row = 0; row < 2; row++)
                    for (int i = 0; i < 4; i++)
                    {
                        float x = side * (58 + row * 39), z = -25 + i * 20;
                        SSBuilding(s, new Vector3(x, 0, z), Quaternion.Euler(0, side > 0 ? 90 : -90, 0), 17, 13,
                            2.9f + (i % 2) * .35f, i % 3 == 0 ? Brick : SSRough, false, false);
                    }
            for (int row = 0; row < 3; row++)
                for (int i = -4; i <= 4; i++)
                {
                    if (i == 0) continue;
                    float x = i * 25, z = 61 + row * 37;
                    SSBuilding(s, new Vector3(x, 0, z), Quaternion.identity, 21, 14, row == 2 ? 4.8f : 3.1f,
                        (i + row + 8) % 3 == 0 ? SSBrickPale : SSRough, false, false);
                    if (Mathf.Abs(i) <= 3) SSCourt(s, x, z + 12, 19, 8, true);
                }
            for (int row = 0; row < 2; row++)
                for (int i = -4; i <= 4; i++)
                {
                    if (i == 0) continue;
                    SSBuilding(s, new Vector3(i * 25, 0, -59 - row * 36), Quaternion.identity, 21, 13, 2.75f,
                        i % 2 == 0 ? Brick : SSRough, false, false);
                    SSCourt(s, i * 25, -48 - row * 36, 19, 6, true);
                }
        }

        static void SSStreet(SSGeometry s, float x, float z, float w, float d, bool earth)
        {
            s.Rect(earth ? SSPath : SSRoad, x, z, w, d, -.014f);
            bool alongZ = d > w;
            float length = alongZ ? d : w;
            for (float at = -length * .5f + 2.1f; at < length * .5f - 1; at += 3.4f)
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 p = alongZ ? new Vector3(x + side * (w * .5f - .3f), .006f, z + at) : new Vector3(x + at, .006f, z + side * (d * .5f - .3f));
                    s.Box(Stone, p, alongZ ? new Vector3(.42f, .045f, 3.1f) : new Vector3(3.1f, .045f, .42f));
                }
            if (!earth)
                s.Rect(SSSoil, alongZ ? x - w * .5f + .8f : x, alongZ ? z : z - d * .5f + .8f,
                    alongZ ? .22f : w - 1.3f, alongZ ? d - 1.3f : .22f, -.006f);
        }

        static void SSBuilding(SSGeometry s, Vector3 p, Quaternion r, float w, float d, float h, Material plaster, bool shop, bool office)
        {
            // Narrow fronts, opaque wooden shutters, a shaded five-foot-way and tiled
            // roofs. No glass, bright stripe awnings, or modern flat-roofed colour blocks.
            SSLocalBox(s, Stone, p, r, new Vector3(0, .15f, 0), new Vector3(w, .34f, d));
            if (shop)
            {
                SSLocalBox(s, plaster, p, r, new Vector3(0, 1.53f, 1.2f), new Vector3(w, 2.82f, d - 2.4f));
                SSLocalBox(s, plaster, p, r, new Vector3(0, (h + 2.9f) * .5f, 0), new Vector3(w, h - 2.9f, d));
                SSLocalBox(s, Shadow, p, r, new Vector3(0, 1.35f, -d * .5f + 2.37f), new Vector3(w - .6f, 2.45f, .12f));
                foreach (float side in new[] { -1f, 1f })
                {
                    SSLocalBox(s, Stone, p, r, new Vector3(side * (w * .5f - .25f), .32f, -d * .5f + .35f), new Vector3(.64f, .64f, .64f));
                    SSLocalBox(s, plaster, p, r, new Vector3(side * (w * .5f - .25f), 1.73f, -d * .5f + .35f), new Vector3(.36f, 2.6f, .36f));
                    SSShutter(s, p, r, side * w * .235f, 4.45f, -d * .5f - .045f, 1.8f, 2.25f, office ? Teal : Dark);
                }
                SSLocalBox(s, Stone, p, r, new Vector3(0, -.005f, -d * .5f + .9f), new Vector3(w, .03f, 2.6f));
                SSLocalBox(s, Stone, p, r, new Vector3(0, 2.91f, -d * .5f - .03f), new Vector3(w + .13f, .17f, .18f));
                SSLocalBox(s, Dark, p, r, new Vector3(0, 2.31f, -d * .5f + 2.23f), new Vector3(w * .62f, .42f, .14f));
                for (int i = 0; i < 5; i++) SSLocalBox(s, Timber, p, r, new Vector3(-w * .3f + i * w * .15f, 1.1f, -d * .5f + 2.27f), new Vector3(w * .14f, 2.0f, .08f));
            }
            else
            {
                SSLocalBox(s, plaster, p, r, new Vector3(0, h * .5f + .16f, 0), new Vector3(w, h, d));
                int doors = Mathf.Max(1, Mathf.RoundToInt(w / 7));
                for (int i = 0; i < doors; i++)
                {
                    float x = (i - (doors - 1) * .5f) * w / doors;
                    SSLocalBox(s, Shadow, p, r, new Vector3(x, 1.3f, -d * .5f - .045f), new Vector3(1.55f, 2.45f, .11f));
                    for (int plank = 0; plank < 5; plank++) SSLocalBox(s, Dark, p, r, new Vector3(x - .58f + plank * .29f, 1.26f, -d * .5f - .11f), new Vector3(.265f, 2.33f, .07f));
                    SSShutter(s, p, r, x + w / doors * .30f, 1.95f, -d * .5f - .045f, 1.2f, 1.25f, Timber);
                }
                h += .16f;
            }
            SSLocalBox(s, Shadow, p, r, new Vector3(0, h - .02f, -d * .5f - .2f), new Vector3(w + .25f, .22f, .58f));
            SSRoof(s, p, r, w + .7f, d + .9f, h + .09f);
            // Discontinuous masonry patches make the long elevations less uniform.
            for (int i = 0; i < 4; i++)
                SSLocalBox(s, i % 2 == 0 ? Stone : SSBrickPale, p, r, new Vector3(-w * .36f + i * w * .24f, .57f + i % 2 * .24f, -d * .5f - .035f), new Vector3(w * .14f, .35f, .08f));
        }

        static void SSShutter(SSGeometry s, Vector3 p, Quaternion r, float x, float y, float z, float w, float h, Material timber)
        {
            SSLocalBox(s, Shadow, p, r, new Vector3(x, y, z), new Vector3(w + .16f, h + .16f, .1f));
            foreach (float side in new[] { -1f, 1f }) SSLocalBox(s, timber, p, r, new Vector3(x + side * w * .48f, y, z - .065f), new Vector3(.1f, h, .08f));
            for (int i = 0; i < 7; i++) SSLocalBox(s, timber, p, r, new Vector3(x, y - h * .45f + i * h * .15f, z - .075f), new Vector3(w, h * .12f, .1f));
            SSLocalBox(s, Dark, p, r, new Vector3(x, y, z - .13f), new Vector3(.09f, h, .06f));
        }

        static void SSRoof(SSGeometry s, Vector3 p, Quaternion r, float w, float d, float y)
        {
            float rise = Mathf.Min(1.55f, d * .17f);
            Vector3 a = p + r * new Vector3(-w * .5f, y, -d * .5f), b = p + r * new Vector3(-w * .5f, y + rise, 0);
            Vector3 c = p + r * new Vector3(w * .5f, y + rise, 0), e = p + r * new Vector3(w * .5f, y, -d * .5f);
            Vector3 f = p + r * new Vector3(-w * .5f, y, d * .5f), g = p + r * new Vector3(w * .5f, y, d * .5f);
            s.Face(Roof, a, b, c, e); s.Face(Roof, g, c, b, f);
            s.Triangle(SSRough, a, f, b); s.Triangle(SSRough, e, c, g);
            SSLocalBox(s, Roof, p, r, new Vector3(0, y + rise + .065f, 0), new Vector3(w + .1f, .17f, .22f));
            for (float x = -w * .5f + .22f; x < w * .5f; x += .75f)
                foreach (float side in new[] { -1f, 1f })
                    s.Beam(Mat("surroundings raised tile joints", "50524A"), p + r * new Vector3(x, y + .025f, side * d * .5f), p + r * new Vector3(x, y + rise + .025f, 0), .045f);
        }

        static void SSCourt(SSGeometry s, float x, float z, float w, float d, bool laundry)
        {
            s.Rect(SSPath, x, z, w, d, -.006f);
            s.Box(SSRough, new Vector3(x - w * .5f, .5f, z), new Vector3(.3f, 1.0f, d));
            s.Box(SSRough, new Vector3(x + w * .5f, .5f, z), new Vector3(.3f, 1.0f, d));
            s.Box(Brick, new Vector3(x, .42f, z + d * .5f), new Vector3(w, .84f, .3f));
            s.Box(Dark, new Vector3(x + w * .33f, .54f, z + d * .26f), new Vector3(1.9f, 1.08f, 1.2f));
            if (laundry)
            {
                Vector3 a = new Vector3(x - w * .32f, 2.2f, z), b = new Vector3(x + w * .32f, 2.2f, z);
                s.Beam(Dark, a - Vector3.up * 2.2f, a, .1f); s.Beam(Dark, b - Vector3.up * 2.2f, b, .1f);
                s.Beam(Timber, a, b, .034f);
                for (int i = 0; i < 5; i++)
                    s.Box(i % 2 == 0 ? Linen : Teal, new Vector3(Mathf.Lerp(a.x, b.x, (i + 1) / 6f), 1.67f, z), new Vector3(.7f, .95f, .024f), Quaternion.Euler(0, i * 9 - 18, 0));
                s.Box(Stone, new Vector3(x - w * .32f, .34f, z + d * .3f), new Vector3(1.4f, .7f, .9f));
            }
            else
            {
                for (int i = 0; i < 3; i++) s.Box(Timber, new Vector3(x - w * .30f + i * 1.3f, .42f, z + d * .3f), new Vector3(1, .84f, 1.05f));
            }
        }

        static void SSRibbon(SSGeometry s, Material material, float width, params Vector2[] route)
        {
            for (int i = 1; i < route.Length; i++)
            {
                Vector2 a = route[i - 1], b = route[i], direction = (b - a).normalized;
                Vector2 offset = new Vector2(-direction.y, direction.x) * width * .5f;
                s.Face(material, new Vector3(a.x + offset.x, -.012f, a.y + offset.y), new Vector3(b.x + offset.x, -.012f, b.y + offset.y),
                    new Vector3(b.x - offset.x, -.012f, b.y - offset.y), new Vector3(a.x - offset.x, -.012f, a.y - offset.y));
            }
        }

        static void SSLocalBox(SSGeometry s, Material material, Vector3 p, Quaternion r, Vector3 local, Vector3 size)
        {
            s.Box(material, p + r * local, size, r);
        }

        static void SSGrass(SSGeometry s, Vector3 p, float h, Material material)
        {
            s.Triangle(material, p + new Vector3(-.28f, 0, 0), p + new Vector3(.04f, h, .025f), p + new Vector3(.28f, 0, 0));
            s.Triangle(material, p + new Vector3(0, 0, -.27f), p + new Vector3(.025f, h * .82f, 0), p + new Vector3(0, 0, .27f));
            s.Triangle(material, p + new Vector3(.28f, 0, 0), p + new Vector3(.04f, h, .025f), p + new Vector3(-.28f, 0, 0));
            s.Triangle(material, p + new Vector3(0, 0, .27f), p + new Vector3(.025f, h * .82f, 0), p + new Vector3(0, 0, -.27f));
        }

        static void SSTree(SSGeometry s, float x, float z, float h, bool palm)
        {
            Vector3 p = new Vector3(x, 0, z);
            s.Beam(Dark, p, p + new Vector3(.18f, h * .74f, .08f), palm ? .30f : .51f);
            if (palm)
            {
                Vector3 crown = p + new Vector3(.18f, h * .76f, .08f);
                for (int i = 0; i < 9; i++)
                {
                    float angle = (i * 40 + x) * Mathf.Deg2Rad;
                    Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 across = new Vector3(-radial.z, 0, radial.x);
                    for (int j = 0; j < 4; j++)
                    {
                        float a = j / 4f, b = (j + 1) / 4f;
                        Vector3 u = crown + radial * (a * h * .51f) + Vector3.up * (Mathf.Sin(a * Mathf.PI) * .72f - a * .68f);
                        Vector3 v = crown + radial * (b * h * .51f) + Vector3.up * (Mathf.Sin(b * Mathf.PI) * .72f - b * .68f);
                        float wa = Mathf.Sin(a * Mathf.PI) * .39f + .04f, wb = Mathf.Sin(b * Mathf.PI) * .39f + .01f;
                        s.Face(i % 2 == 0 ? SSLeaf : SSLeafDark, u - across * wa, v - across * wb, v + across * wb, u + across * wa);
                        s.Face(i % 2 == 0 ? SSLeaf : SSLeafDark, u + across * wa, v + across * wb, v - across * wb, u - across * wa);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * .5f + x;
                    Vector3 branch = p + new Vector3(Mathf.Cos(angle) * h * .20f, h * .67f, Mathf.Sin(angle) * h * .19f);
                    s.Beam(Dark, p + Vector3.up * h * .40f, branch, .20f);
                    SSCrown(s, branch + Vector3.up * h * .11f, new Vector3(h * .32f, h * .26f, h * .29f), i % 2 == 0 ? SSLeaf : SSLeafDark);
                }
                SSCrown(s, p + Vector3.up * h * .9f, new Vector3(h * .29f, h * .24f, h * .27f), SSLeaf);
            }
        }

        static void SSCrown(SSGeometry s, Vector3 p, Vector3 size, Material material)
        {
            const int sides = 7;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 pa = p + new Vector3(Mathf.Cos(a) * size.x, -.15f * size.y, Mathf.Sin(a) * size.z);
                Vector3 pb = p + new Vector3(Mathf.Cos(b) * size.x, -.15f * size.y, Mathf.Sin(b) * size.z);
                Vector3 qa = p + new Vector3(Mathf.Cos(a) * size.x * .64f, size.y * .62f, Mathf.Sin(a) * size.z * .64f);
                Vector3 qb = p + new Vector3(Mathf.Cos(b) * size.x * .64f, size.y * .62f, Mathf.Sin(b) * size.z * .64f);
                s.Face(material, pa, qa, qb, pb);
                s.Triangle(material, qa, p + Vector3.up * size.y, qb);
                s.Triangle(material, pb, p - Vector3.up * size.y * .63f, pa);
            }
        }

        static void SSGrove(SSGeometry s, float x, float z, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 p = new Vector3(x + s.Range(-3, 3), .6f, z + s.Range(-3, 3));
                SSCrown(s, p, new Vector3(s.Range(1.2f, 2.8f), s.Range(.65f, 1.4f), s.Range(1.1f, 2.5f)), i % 2 == 0 ? SSLeaf : SSLeafDark);
            }
        }

        static void SSOuterTerrain(SSGeometry s, bool village)
        {
            // Three-dimensional distant terrain, followed by low-contrast field patches.
            // Near farmland stays low enough to see across; distant ridges are higher.
            if(village) {
                SSHillMass(s,-116,170,92,50,18,0);
                SSHillMass(s,112,179,108,64,24,1);
                SSHillMass(s,-197,53,64,118,31,2);
                SSHillMass(s,193,29,65,107,21,3);
                SSHillMass(s,25,-215,130,70,25,4);
            }
            else {
                SSHillMass(s,-212,157,83,74,22,2);
                SSHillMass(s,205,185,102,81,27,3);
            }
            Material a = Mat(village ? "distant uncultivated earth" : "distant city earth", village ? "998B6C" : "A1967E");
            Material b = Mat(village ? "distant grassy soil" : "distant weathered ground", village ? "8F906B" : "978D77");
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2 / 24, radius = (ring == 0 ? 176 : 263) + s.Range(-12, 12);
                    Vector3 center = new Vector3(Mathf.Cos(angle) * radius, -.020f + ring * .001f, Mathf.Sin(angle) * radius + 3);
                    float rx = s.Range(20, 37), rz = s.Range(17, 31);
                    for (int j = 0; j < 7; j++)
                    {
                        float u = j * Mathf.PI * 2 / 7, v = (j + 1) * Mathf.PI * 2 / 7;
                        s.Triangle(i % 2 == 0 ? a : b, center,
                            center + new Vector3(Mathf.Cos(v) * rx, 0, Mathf.Sin(v) * rz),
                            center + new Vector3(Mathf.Cos(u) * rx, 0, Mathf.Sin(u) * rz));
                    }
                }
        }

        // Every decoration is guarded against the *larger* main slab, not merely the
        // navigation rectangle. Batches have no colliders and stay below 60k vertices.
        sealed class SSGeometry
        {
            sealed class Part
            {
                public readonly List<Vector3> vertices = new List<Vector3>();
                public readonly List<int> triangles = new List<int>();
            }
            readonly Dictionary<Material, List<Part>> meshes = new Dictionary<Material, List<Part>>();
            readonly System.Random rng;
            readonly string kind;
            public SSGeometry(string place) { kind = place; rng = new System.Random(place == "quanzhou" ? 1501 : place == "market" ? 1502 : place == "quarters" ? 1503 : 1504); }
            public float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            static bool Outside(float left, float right, float near, float far) => right <= -39.5f || left >= 39.5f || far <= -35.5f || near >= 41.5f;
            Part Get(Material material, int count)
            {
                if (!meshes.TryGetValue(material, out var parts)) { parts = new List<Part> { new Part() }; meshes.Add(material, parts); }
                Part part = parts[parts.Count - 1];
                if (part.vertices.Count + count > 58000) { part = new Part(); parts.Add(part); }
                return part;
            }
            public void Box(Material material, Vector3 p, Vector3 size) => Box(material, p, size, Quaternion.identity);
            public void Box(Material material, Vector3 p, Vector3 size, Quaternion rotation)
            {
                Vector3 x = rotation * Vector3.right * size.x * .5f, y = rotation * Vector3.up * size.y * .5f, z = rotation * Vector3.forward * size.z * .5f;
                float ex = Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), ez = Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z);
                if (!Outside(p.x - ex, p.x + ex, p.z - ez, p.z + ez)) return;
                Part part = Get(material, 24); DetailBox(part.vertices, part.triangles, p, size, rotation);
            }
            public void Beam(Material material, Vector3 a, Vector3 b, float width)
            {
                Box(material, (a + b) * .5f, new Vector3(width, Vector3.Distance(a, b), width), Quaternion.FromToRotation(Vector3.up, (b - a).normalized));
            }
            public void Rect(Material material, float x, float z, float w, float d, float y)
            {
                Face(material, new Vector3(x - w * .5f, y, z - d * .5f), new Vector3(x - w * .5f, y, z + d * .5f),
                    new Vector3(x + w * .5f, y, z + d * .5f), new Vector3(x + w * .5f, y, z - d * .5f));
            }
            public void Face(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                if (!Outside(Mathf.Min(a.x, b.x, c.x, d.x), Mathf.Max(a.x, b.x, c.x, d.x), Mathf.Min(a.z, b.z, c.z, d.z), Mathf.Max(a.z, b.z, c.z, d.z))) return;
                Part part = Get(material, 4); Quad(part.vertices, part.triangles, a, b, c, d);
            }
            public void Triangle(Material material, Vector3 a, Vector3 b, Vector3 c)
            {
                if (!Outside(Mathf.Min(a.x, b.x, c.x), Mathf.Max(a.x, b.x, c.x), Mathf.Min(a.z, b.z, c.z), Mathf.Max(a.z, b.z, c.z))) return;
                Part part = Get(material, 3); int at = part.vertices.Count;
                part.vertices.Add(a); part.vertices.Add(b); part.vertices.Add(c);
                part.triangles.Add(at); part.triangles.Add(at + 1); part.triangles.Add(at + 2);
            }
            public void Finish()
            {
                foreach (var material in meshes)
                    for (int i = 0; i < material.Value.Count; i++)
                    {
                        Part part = material.Value[i];
                        Mesh mesh = FlatMesh(part.vertices, part.triangles);
                        mesh.name = kind + " surroundings · " + material.Key.name + " · " + i;
                        MeshObject(mesh.name, mesh, Vector3.zero, material.Key);
                    }
            }
        }
    }
}
