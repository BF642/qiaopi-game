using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public static partial class WorldFactory
    {
        // An explorable remittance compound: the roofs are cut away over occupied rooms.
        // All ground collision is flush at y=0. The central x[-4,4] lane stays open.
        static void ExpandedPostOffice()
        {
            Box("Post office stone foundation", new Vector3(0, -.7f, 3), new Vector3(72, 1.1f, 70), Stone);
            Box("Post office walkable ground", new Vector3(0, -.15f, 3), new Vector3(72, .3f, 70), Mat("post courtyard earth", "9B9079"), true);
            Box("Post office foundation lower course", new Vector3(0, -1.3f, 3), new Vector3(71.6f, .18f, 69.6f), Dark);

            PostPaving("Continuous public passage", 0, 3, 8, 70);
            PostPaving("Southern stationers street", -1, -25.25f, 67, 7.5f);
            PostPaving("Parcel receiving bay floor", -23.5f, -15.5f, 17, 9);
            PostPaving("Western collecting hall floor", -19.5f, 5.5f, 25, 31);
            PostPaving("Writing hall connection to passage", -5.5f, 9, 3, 8);
            PostPaving("Southern hall connection to passage", -5.5f, -4, 3, 6);
            PostPaving("Stationer floor", 24.5f, -15.5f, 15, 11);
            PostPaving("Paper shop to despatch corridor", 23, -7.5f, 8, 5);
            PostPaving("Eastern despatch hall floor", 20, 8, 24, 26);
            PostPaving("Despatch hall connection to passage", 6, 5.5f, 4, 11);
            PostPaving("Rear cross passage", 0, 23, 64, 4);
            PostPaving("Archive court floor", -20, 30.5f, 24, 11);
            PostPaving("Archive court side doorway", -6, 29.5f, 4, 6);
            PostPaving("Packing court floor", 20, 30.5f, 24, 11);
            PostPaving("Packing court side doorway", 6, 29.5f, 4, 6);

            // Street frontage. The wide gaps line up with the parcel and paper objectives.
            PostWall("Parcel bay west wall", -32, -15.5f, .4f, 9, 1.1f);
            PostWall("Parcel bay east return", -15, -18.5f, .4f, 3, 1.1f);
            PostWall("Parcel bay front left", -29.5f, -20, 5, .4f, 1.1f);
            PostWall("Parcel bay front right", -16.5f, -20, 3, .4f, 1.1f);
            PostWall("Parcel bay back left", -29, -11, 6, .4f, 1.1f);
            PostWall("Parcel bay back right", -16.5f, -11, 3, .4f, 1.1f);
            PostDoor("收批处", -22.5f, -20, 9);
            PostTable("Parcel wrapping table", -29, -13.8f, 2.4f, 1.0f, false);
            PostBundle(new Vector3(-29.7f, .92f, -13.7f), .8f, .42f);
            PostBundle(new Vector3(-28.5f, .92f, -13.8f), .7f, .3f);
            PostMailRack(-29, -17.4f, 3.8f, 1.65f, "待寄");
            PostBundleStand(-17, -13.2f, 1.65f, 1.6f);

            PostWall("Paper shop west side", 17, -15.5f, .4f, 11, 1.12f);
            PostWall("Paper shop east side", 32, -15.5f, .4f, 11, 1.12f);
            PostWall("Paper shop frontage left", 17.5f, -21, 1, .4f, 1.12f);
            PostWall("Paper shop frontage right", 29.5f, -21, 5, .4f, 1.12f);
            PostWall("Paper shop rear left", 18, -10, 2, .4f, 1.12f);
            PostWall("Paper shop rear right", 29.5f, -10, 5, .4f, 1.12f);
            PostDoor("纸墨铺", 22.5f, -21, 9);
            PostTable("Stationer sales counter", 29.4f, -16, .85f, 5.0f, true);
            PostPaperStack(new Vector3(29.2f, .92f, -17.6f), 1.1f, .78f, 7);
            PostPaperStack(new Vector3(29.4f, .92f, -15.5f), 1.1f, .8f, 4);
            PostBrushes(new Vector3(29.3f, .92f, -13.9f));
            PostTable("Ink and blank-paper display", 19.4f, -17.4f, 1.5f, .85f, false);
            PostPaperStack(new Vector3(19.4f, .92f, -17.4f), 1.2f, .8f, 5);
            PostRearWindow(29.3f, -9.95f, 3.6f);

            // The west hall is a long, open collecting and letter-writing room.
            // Its seven-metre-wide working aisle follows x=-22 from the parcel bay.
            PostWall("Writing hall west lower wall", -32, -5, .42f, 10, 1.18f);
            PostWall("Writing hall west upper wall", -32, 13, .42f, 16, 1.18f);
            PostWall("Writing hall east middle return", -7, 3.5f, .42f, 5, 1.18f);
            PostWall("Writing hall east upper return", -7, 17.5f, .42f, 7, 1.18f);
            PostWall("Writing hall front left", -29, -10, 6, .42f, 1.18f);
            PostWall("Writing hall front middle", -14.5f, -10, 7, .42f, 1.18f);
            PostWall("Writing hall rear left", -28.5f, 21, 7, .42f, 1.18f);
            PostWall("Writing hall rear right", -12, 21, 10, .42f, 1.18f);
            PostDoor("收寄 · 代写", -22, -10, 8);
            PostDoor("账房后院", -21, 21, 8);
            PostTable("Receiving clerk long counter", -11.6f, 2.3f, .85f, 7.4f, true);
            PostPaperStack(new Vector3(-11.8f, .92f, -.4f), 1, .72f, 5);
            PostPaperStack(new Vector3(-11.8f, .92f, 3.6f), 1, .72f, 3);
            PostBrushes(new Vector3(-11.5f, .92f, 1.5f));
            PostStamp(new Vector3(-11.6f, .92f, 5.8f));
            PostWritingDesk(-28.4f, 3.4f);
            PostWritingDesk(-28.4f, 11.4f);
            PostWritingDesk(-13.4f, 16.8f);
            PostBench(-28.5f, 17.8f, 4, false);
            PostMailRack(-11.5f, -6.2f, 4.8f, 1.75f, "来批");
            PostRearWindow(-28.5f, 21.03f, 4.2f);
            PostRearWindow(-11.8f, 21.03f, 4.2f);

            // The east hall links checking, handing over, and packing. It is deliberately
            // furnished along its edges so x[18,26] remains a continuous working aisle.
            PostWall("Despatch hall west front", 8, -3, .42f, 4, 1.18f);
            PostWall("Despatch hall west rear", 8, 16, .42f, 10, 1.18f);
            PostWall("Despatch hall east wall", 32, 8, .42f, 26, 1.18f);
            PostWall("Despatch hall front left", 13, -5, 10, .42f, 1.18f);
            PostWall("Despatch hall front right", 29, -5, 6, .42f, 1.18f);
            PostWall("Despatch hall rear left", 13, 21, 10, .42f, 1.18f);
            PostWall("Despatch hall rear right", 29.5f, 21, 5, .42f, 1.18f);
            PostDoor("核对 · 交付", 22, -5, 8);
            PostTable("Checking counter", 11.5f, -.6f, .85f, 3.5f, true);
            PostPaperStack(new Vector3(11.4f, .92f, -1.7f), 1.25f, .82f, 6);
            PostStamp(new Vector3(11.5f, .92f, .6f));
            PostTable("Despatch counter", 11.7f, 16, .85f, 4.8f, true);
            PostBundle(new Vector3(11.7f, .92f, 14.4f), 1.1f, .5f);
            PostPaperStack(new Vector3(11.7f, .92f, 17.3f), 1.1f, .8f, 4);
            PostMailRack(29.2f, 1, 4.5f, 1.85f, "回批");
            PostTable("Dispatch bag tying table", 29, 17, 2.4f, 1.05f, false);
            PostBundle(new Vector3(28.3f, .92f, 17), .9f, .45f);
            PostBundle(new Vector3(29.6f, .92f, 17), .75f, .35f);
            PostRearWindow(13, 21.03f, 4.2f);
            PostRearWindow(29.7f, 21.03f, 3.2f);

            // A rear cross passage joins the archive and bagging courts without steps.
            PostWall("Archive west wall", -32, 30.5f, .42f, 11, 1.12f);
            PostWall("Archive front left", -28.5f, 25, 7, .42f, 1.12f);
            PostWall("Archive front right", -12.5f, 25, 9, .42f, 1.12f);
            PostWall("Archive east lower return", -8, 25.7f, .42f, 1.4f, 1.12f);
            PostWall("Archive east upper return", -8, 34.5f, .42f, 3, 1.12f);
            PostWall("Archive back wall", -20, 36, 24, .42f, 1.12f);
            PostDoor("账房 · 存批", -21, 25, 8);
            PostMailRack(-28.3f, 34.4f, 5.2f, 2.2f, "存批");
            PostMailRack(-12.3f, 34.4f, 5.2f, 2.2f, "账册");
            PostWritingDesk(-28.3f, 29);
            PostTable("Archive sorting table", -13.5f, 29, 2.5f, 1.0f, false);
            PostPaperStack(new Vector3(-13.9f, .92f, 29), 1.1f, .8f, 9);
            PostPaperStack(new Vector3(-12.8f, .92f, 29), .8f, .65f, 6);
            PostRearWindow(-20, 36.03f, 5.4f);

            PostWall("Packing court east wall", 32, 30.5f, .42f, 11, 1.12f);
            PostWall("Packing court front left", 13, 25, 10, .42f, 1.12f);
            PostWall("Packing court front right", 29.5f, 25, 5, .42f, 1.12f);
            PostWall("Packing court west lower return", 8, 25.7f, .42f, 1.4f, 1.12f);
            PostWall("Packing court west upper return", 8, 34.5f, .42f, 3, 1.12f);
            PostWall("Packing court back wall", 20, 36, 24, .42f, 1.12f);
            PostDoor("封袋 · 出件", 22.5f, 25, 9);
            PostTable("Mailbag sealing workbench", 12.8f, 30.4f, 3.3f, 1.05f, false);
            PostBundle(new Vector3(11.7f, .92f, 30.4f), 1, .55f);
            PostBundle(new Vector3(13.2f, .92f, 30.4f), 1.1f, .48f);
            PostStamp(new Vector3(14.2f, .92f, 30.5f));
            PostBundleStand(29, 28.6f, 3.8f, 2.7f);
            PostBundleStand(28.6f, 33.2f, 4.5f, 2.4f);
            PostRearWindow(19.5f, 36.03f, 6);

            // Small side accommodation and an open-sided tea shelter face the street.
            PostPaving("Staff sleeping room floor", -30.5f, -28, 9, 6);
            PostWall("Staff lodging south wall", -30.5f, -31, 9, .38f, 1.15f);
            PostWall("Staff lodging west wall", -35, -28, .38f, 6, 1.15f);
            PostWall("Staff lodging east wall", -26, -28, .38f, 6, 1.15f);
            PostWall("Staff lodging front left", -34, -25, 2, .38f, 1.15f);
            PostWall("Staff lodging front right", -26.5f, -25, 1, .38f, 1.15f);
            PostBed(-33.1f, -28.3f);
            PostBed(-28, -28.3f);
            PostRearWindow(-30.5f, -30.98f, 4.3f);
            PostPaving("Open tea shelter floor", -11.4f, -26, 7, 6);
            PostWall("Tea shelter rear low wall", -11.5f, -29, 7, .38f, 1.05f);
            PostTable("Tea table", -11.2f, -25.8f, 1.5f, .8f, false, .76f);
            PostBench(-11.4f, -28.1f, 3.6f, false);
            StoneJar(new Vector3(-14, 0, -26.8f), .5f, .85f, Mat("tea water jar", "766D51"));
            Cylinder("Tea pot body", new Vector3(-11.3f, .84f, -25.8f), .115f, .16f, Mat("tea pot clay", "7C4E36"));
            Cylinder("Tea pot lid", new Vector3(-11.3f, .93f, -25.8f), .12f, .02f, Dark);
            foreach (float xx in new[] { -10.85f, -11.65f })
                Cylinder("Tea cup", new Vector3(xx, .79f, -25.7f), .04f, .06f, Cream);
            PostEave("Tea shelter", -11.5f, -28.6f, 7.2f, 1.6f);

            // The perimeter establishes a single large compound, without another ring
            // of tall houses behind the quest characters.
            PostWall("West compound boundary", -35.65f, 7, .45f, 60, 1.08f);
            PostWall("East compound boundary", 35.65f, 3, .45f, 69, 1.08f);
            PostWall("Rear compound wall west", -20, 37.65f, 31, .45f, 1.08f);
            PostWall("Rear compound wall east", 20, 37.65f, 31, .45f, 1.08f);
        }

        static void PostPaving(string label, float x, float z, float width, float depth)
        {
            HumanScalePaving(label, x, z, width, depth, .008f + pavingLayer++ * .0045f, Mat("post worn paving", "AEA590"));
        }

        static void PostWall(string label, float x, float z, float width, float depth, float height)
        {
            // Enclose occupied rooms at human eye level. Courtyard boundaries remain low.
            // The original collider footprint and every broad doorway stay unchanged.
            bool room = label.StartsWith("Writing hall") || label.StartsWith("Despatch hall") ||
                label.StartsWith("Archive") || label.StartsWith("Packing court") || label.StartsWith("Staff lodging");
            if(room) height=2.65f;
            Box(label, new Vector3(x, height * .5f, z), new Vector3(width, height, depth), Brick, true);
            Box(label + " stone foot", new Vector3(x, .16f, z), new Vector3(width + .06f, .32f, depth + .06f), Stone);
            Box(label + " worn stone coping", new Vector3(x, height + .04f, z), new Vector3(width + .09f, .08f, depth + .09f), Mat("post wall coping", "8A8779"));
            var v = new List<Vector3>(); var t = new List<int>();
            bool alongX = width >= depth; float length = alongX ? width : depth;
            int courses = Mathf.Max(1, Mathf.FloorToInt((height - .34f) / .24f));
            for (int row = 0; row < courses; row++)
            {
                float yy = .37f + row * .24f;
                foreach (float side in new[] { -1f, 1f })
                {
                    DetailBox(v, t, alongX ? new Vector3(0, yy, side * (depth * .5f + .005f)) : new Vector3(side * (width * .5f + .005f), yy, 0),
                        alongX ? new Vector3(length, .016f, .012f) : new Vector3(.012f, .016f, length));
                    for (float step = -.5f * length + .35f + (row % 2) * .32f; step < length * .5f; step += .67f)
                        DetailBox(v, t, alongX ? new Vector3(step, yy + .11f, side * (depth * .5f + .006f)) : new Vector3(side * (width * .5f + .006f), yy + .11f, step),
                            alongX ? new Vector3(.017f, .2f, .013f) : new Vector3(.013f, .2f, .017f));
                }
            }
            DetailMesh(label + " aged lime joints", v, t, new Vector3(x, 0, z), Mat("post lime mortar", "B4A388"));
        }

        static void PostDoor(string label, float x, float z, float width)
        {
            // The original wide mission route stays clear. The 3 m central timber
            // portal is visual; physical jambs remain at the established outer edges.
            foreach (float side in new[] { -1f, 1f })
            {
                Box(label + " outer jamb", new Vector3(x + side * width * .5f, 1.19f, z), new Vector3(.16f, 2.38f, .18f), Dark, true);
                Box(label + " stone shoe", new Vector3(x + side * width * .5f, .14f, z), new Vector3(.26f, .28f, .26f), Stone);
            }
            var v = new List<Vector3>(); var t = new List<int>();
            DetailBox(v, t, new Vector3(0, 2.38f, 0), new Vector3(width + .2f, .14f, .2f));
            foreach (float side in new[] { -1f, 1f })
            {
                DetailBox(v, t, new Vector3(side * 1.62f, 2.21f, 0), new Vector3(.11f, .32f, .18f));
                DetailBeam(v, t, new Vector3(side * 1.6f, 2.10f, 0), new Vector3(side * 1.32f, 2.38f, 0), .055f, .08f);
            }
            DetailMesh(label + " joined lintel", v, t, new Vector3(x, 0, z), Dark);
            Box(label + " modest signboard", new Vector3(x, 2.42f, z - .15f), new Vector3(2.25f, .40f, .07f), Timber);
            SignText(label, new Vector3(x, 2.42f, z - .192f), .12f);
        }

        static void PostEave(string label, float x, float z, float width, float depth)
        {
            foreach (float side in new[] { -1f, 1f })
                Box(label + " peripheral post", new Vector3(x + side * (width * .5f - .2f), 1.4f, z), new Vector3(.18f, 2.8f, .18f), Dark, true);
            RoofMesh(x, 2.84f, z, width, depth, false);
            Box(label + " exposed eave beam", new Vector3(x, 2.78f, z - depth * .5f), new Vector3(width, .15f, .15f), Dark);
        }

        static void PostRearWindow(float x, float z, float width)
        {
            // Small peripheral window bays and eaves imply the original roof, while
            // leaving every workroom centre open to the camera and reachable on foot.
            Box("Peripheral limewashed window bay", new Vector3(x, 1.81f, z - .13f), new Vector3(width, 1.42f, .23f), Cream, true);
            LouverWindow(new Vector3(x, 1.85f, z - .30f), width - .65f, 1.08f, Teal);
            PostEave("Window bay", x, z + .05f, width + .45f, 1.5f);
        }

        static void PostTable(string label, float x, float z, float width, float depth, bool panelled, float top = .92f)
        {
            PostFurnitureCollider(label, new Vector3(x, 0, z), new Vector3(width - .08f, top, depth - .08f));
            var wood = new List<Vector3>(); var wt = new List<int>();
            var frame = new List<Vector3>(); var ft = new List<int>();
            int boards = Mathf.CeilToInt(depth / .18f); float dz = depth / boards;
            for (int i = 0; i < boards; i++)
                DetailBox(wood, wt, new Vector3(0, top - .0275f, -depth * .5f + (i + .5f) * dz), new Vector3(width, .055f, dz - .003f));
            foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
                DetailBox(frame, ft, new Vector3(sx * (width * .5f - .075f), (top - .055f) * .5f, sz * (depth * .5f - .075f)), new Vector3(.075f, top - .055f, .075f));
            foreach (float side in new[] { -1f, 1f })
            {
                DetailBox(frame, ft, new Vector3(0, top - .14f, side * (depth * .5f - .045f)), new Vector3(width - .08f, .17f, .045f));
                DetailBox(frame, ft, new Vector3(side * (width * .5f - .045f), top - .14f, 0), new Vector3(.045f, .17f, depth - .08f));
                DetailBox(frame, ft, new Vector3(side * (width * .5f - .06f), .23f, 0), new Vector3(.04f, .06f, depth - .12f));
                if (panelled)
                {
                    int panels = Mathf.CeilToInt(depth / .25f); float step = (depth - .14f) / panels;
                    for (int i = 0; i < panels; i++)
                        DetailBox(wood, wt, new Vector3(side * (width * .5f - .06f), (top + .1f) * .5f, -depth * .5f + .07f + (i + .5f) * step), new Vector3(.035f, top - .18f, step - .004f));
                    DetailBox(wood, wt, new Vector3(0, (top + .1f) * .5f, side * (depth * .5f - .06f)), new Vector3(width - .12f, top - .18f, .035f));
                }
            }
            DetailMesh(label + " individual top planks", wood, wt, new Vector3(x, 0, z), Timber);
            DetailMesh(label + " mortise frame", frame, ft, new Vector3(x, 0, z), Dark);
        }

        static void PostFurnitureCollider(string label, Vector3 p, Vector3 size)
        {
            var obj = new GameObject(label + " collision"); obj.transform.SetParent(world, false); obj.transform.localPosition = p;
            var collider = obj.AddComponent<BoxCollider>(); collider.center = Vector3.up * size.y * .5f; collider.size = size;
        }

        static void PostWritingDesk(float x, float z)
        {
            PostTable("Letter writer's desk", x, z, 1.65f, .76f, false, .78f);
            PostBench(x, z - .88f, .48f, false);
            var v = new List<Vector3>(); var t = new List<int>();
            foreach (float side in new[] { -1f, 1f })
                DetailBox(v, t, new Vector3(side * .2f, .71f, -1.04f), new Vector3(.04f, .60f, .04f));
            DetailBox(v, t, new Vector3(0, .98f, -1.04f), new Vector3(.44f, .10f, .045f));
            DetailMesh("Writer chair back", v, t, new Vector3(x, 0, z), Dark);
            PostPaperStack(new Vector3(x - .25f, .78f, z), .43f, .30f, 3);
            PostBrushes(new Vector3(x + .59f, .78f, z + .15f));
            Box("Inkstone", new Vector3(x + .31f, .8f, z - .16f), new Vector3(.19f, .04f, .12f), Shadow);
        }

        static void PostBench(float x, float z, float length, bool alongZ)
        {
            length = Mathf.Min(length, 2.4f);
            var v = new List<Vector3>(); var t = new List<int>();
            DetailBox(v, t, new Vector3(0, .425f, 0), alongZ ? new Vector3(.38f, .07f, length) : new Vector3(length, .07f, .38f));
            foreach (float side in new[] { -1f, 1f })
                DetailBox(v, t, new Vector3(alongZ ? 0 : side * (length * .5f - .07f), .20f, alongZ ? side * (length * .5f - .07f) : 0),
                    alongZ ? new Vector3(.32f, .4f, .06f) : new Vector3(.06f, .4f, .32f));
            DetailMesh("Human scale waiting seat", v, t, new Vector3(x, 0, z), Timber);
            PostFurnitureCollider("Waiting seat", new Vector3(x, 0, z), alongZ ? new Vector3(.38f, .46f, length) : new Vector3(length, .46f, .38f));
        }

        static void PostPaperStack(Vector3 p, float width, float depth, int leaves)
        {
            width = Mathf.Min(width, .43f); depth = Mathf.Min(depth, .31f);
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < leaves; i++)
                DetailBox(v, t, new Vector3(i % 2 == 0 ? -.003f : .003f, .003f + i * .008f, 0), new Vector3(width, .006f, depth));
            DetailMesh("Folded correspondence leaves", v, t, p, Paper);
            Box("Small paperweight", p + new Vector3(.04f, leaves * .008f + .012f, .045f), new Vector3(.20f, .025f, .045f), Dark);
        }

        static void PostBrushes(Vector3 p)
        {
            Cylinder("Bamboo brush holder", p + Vector3.up * .08f, .06f, .16f, Timber);
            Cylinder("Brush holder opening", p + Vector3.up * .161f, .05f, .005f, Shadow);
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < 3; i++)
                DetailBeam(v, t, new Vector3((i - 1) * .017f, .04f, 0), new Vector3((i - 1) * .032f, .29f + (i % 2) * .03f, .02f), .008f, .008f);
            DetailMesh("Fine bamboo writing brushes", v, t, p, Dark);
        }

        static void PostStamp(Vector3 p)
        {
            Box("Closed red seal paste box", p + new Vector3(.10f, .022f, 0), new Vector3(.13f, .044f, .12f), Red);
            Box("Desk seal base", p + new Vector3(-.08f, .023f, 0), new Vector3(.07f, .046f, .07f), Dark);
            Cylinder("Desk seal handle", p + new Vector3(-.08f, .087f, 0), .025f, .085f, Timber);
        }

        static void PostBundle(Vector3 p, float width, float height)
        {
            width = Mathf.Min(width, .62f); height = Mathf.Min(height, .28f);
            Box("Tied cloth mail bundle", p + Vector3.up * (height * .5f), new Vector3(width, height, width * .72f), Linen);
            var v = new List<Vector3>(); var t = new List<int>();
            DetailBox(v, t, new Vector3(0, height + .007f, 0), new Vector3(width + .01f, .014f, .016f));
            DetailBox(v, t, new Vector3(0, height + .009f, 0), new Vector3(.016f, .014f, width * .74f));
            DetailMesh("Mail bundle crossed string", v, t, p, Dark);
            Box("Destination slip", p + new Vector3(width * .2f, height + .018f, .02f), new Vector3(.13f, .004f, .08f), Paper);
        }

        static void PostBundleStand(float x, float z, float width, float depth)
        {
            width = Mathf.Min(width, 2.6f); depth = Mathf.Min(depth, 1.0f);
            PostTable("Low mailbag staging stand", x, z, width, depth, false, .55f);
            int n = Mathf.Max(1, Mathf.FloorToInt(width / .9f));
            for (int i = 0; i < n; i++)
                PostBundle(new Vector3(x - width * .5f + (i + .5f) * width / n, .55f, z), .6f, .28f);
        }

        static void PostMailRack(float x, float z, float width, float height, string label)
        {
            width = Mathf.Min(width, 3.6f); height = Mathf.Min(height, 1.85f);
            const float depth = .38f;
            PostFurnitureCollider(label + " rack", new Vector3(x, 0, z), new Vector3(width, height, depth));
            var wood = new List<Vector3>(); var wt = new List<int>();
            var papers = new List<Vector3>(); var pt = new List<int>();
            var ties = new List<Vector3>(); var tt = new List<int>();
            DetailBox(wood, wt, new Vector3(0, height * .5f, .18f), new Vector3(width, height, .035f));
            int columns = Mathf.Max(3, Mathf.FloorToInt(width / .42f));
            float rowHeight = (height - .16f) / 4;
            for (int row = 0; row <= 4; row++)
                DetailBox(wood, wt, new Vector3(0, .10f + row * rowHeight, 0), new Vector3(width, .035f, depth));
            for (int col = 0; col <= columns; col++)
                DetailBox(wood, wt, new Vector3(-width * .5f + col * width / columns, height * .5f, 0), new Vector3(.032f, height, depth));
            for (int row = 0; row < 4; row++) for (int col = 0; col < columns; col++)
            {
                float xx = -width * .5f + (col + .5f) * width / columns, yy = .12f + row * rowHeight;
                float packWidth = width / columns * .73f, packHeight = .08f + ((row + col) % 3) * .045f;
                DetailBox(papers, pt, new Vector3(xx, yy + packHeight * .5f, -.018f), new Vector3(packWidth, packHeight, .26f));
                DetailBox(ties, tt, new Vector3(xx, yy + packHeight + .003f, -.018f), new Vector3(.009f, .006f, .27f));
                DetailBox(papers, pt, new Vector3(xx, yy - .015f, -.194f), new Vector3(.085f, .024f, .004f));
            }
            DetailMesh(label + " pigeonhole joinery", wood, wt, new Vector3(x, 0, z), Timber);
            DetailMesh(label + " filed letters and shelf labels", papers, pt, new Vector3(x, 0, z), Paper);
            DetailMesh(label + " bundle threads", ties, tt, new Vector3(x, 0, z), Dark);
            Box(label + " rack label", new Vector3(x, height + .09f, z - .20f), new Vector3(.66f, .18f, .035f), Timber);
            SignText(label, new Vector3(x, height + .09f, z - .223f), .085f);
        }

        static void PostBed(float x, float z)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            DetailBox(v, t, new Vector3(0, .39f, 0), new Vector3(1.0f, .09f, 2.0f));
            foreach (float sx in new[] { -.42f, .42f }) foreach (float sz in new[] { -.89f, .89f })
                DetailBox(v, t, new Vector3(sx, .18f, sz), new Vector3(.075f, .36f, .075f));
            DetailMesh("Single staff bed joinery", v, t, new Vector3(x, 0, z), Dark);
            PostFurnitureCollider("Staff bed", new Vector3(x, 0, z), new Vector3(1, .45f, 2));
            Box("Woven sleeping mat", new Vector3(x, .45f, z), new Vector3(.96f, .035f, 1.94f), Linen);
            Box("Cloth pillow", new Vector3(x, .52f, z - .71f), new Vector3(.52f, .11f, .30f), Paper);
            Box("Folded coverlet", new Vector3(x, .52f, z + .64f), new Vector3(.90f, .11f, .50f), Teal);
        }
    }
}
