using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public static partial class WorldFactory
    {
        // Backdrop only: no water, no colliders, no task objects. The land cap is
        // below the playable ground and extends beyond the far-view fog distance.
        static void HarborSurroundings(string kind)
        {
            if (kind != "harbor" && kind != "port") return;
            bool xiamen = kind == "harbor";
            var batch = new HBSBatch();
            Vector2[] coast = xiamen ? new[]
            {
                new Vector2(-690,-1260), new Vector2(675,-1240),
                new Vector2(710,-780), new Vector2(684,-360), new Vector2(650,-24),
                new Vector2(310,9), new Vector2(165,4), new Vector2(118,10),
                new Vector2(81,16), new Vector2(56,17), new Vector2(36,22),
                new Vector2(-36,22), new Vector2(-57,17), new Vector2(-86,12),
                new Vector2(-124,18), new Vector2(-178,8), new Vector2(-320,15),
                new Vector2(-662,-15), new Vector2(-714,-420)
            } : new[]
            {
                new Vector2(-1250,-1250), new Vector2(-19,-1268),
                new Vector2(17,-790), new Vector2(20,-365), new Vector2(30,-181),
                new Vector2(22,-108), new Vector2(28,-58), new Vector2(36,-32),
                new Vector2(36,38), new Vector2(-36,38), new Vector2(-53,31),
                new Vector2(-95,25), new Vector2(-162,29), new Vector2(-324,15),
                new Vector2(-803,16), new Vector2(-1262,-26), new Vector2(-1270,-650)
            };
            HBSLand(batch, coast, xiamen);
            if (xiamen) HBSXiamen(batch); else HBSSingaporePort(batch);
            batch.Flush(xiamen ? "厦门连续陆岸与港外街巷" : "南洋连续陆岸与货栈街");
        }

        // Geometry is grouped by material rather than one GameObject per stone,
        // tile or tree. A bucket is split before Unity's 16-bit index limit.
        sealed class HBSBatch
        {
            sealed class Part
            {
                public Material material;
                public readonly List<Vector3> vertices = new List<Vector3>();
                public readonly List<int> triangles = new List<int>();
            }
            readonly List<Part> parts = new List<Part>();
            readonly Dictionary<Material, Part> active = new Dictionary<Material, Part>();
            Part Get(Material material)
            {
                if (!active.TryGetValue(material, out var part) || part.vertices.Count > 52000)
                {
                    part = new Part { material = material };
                    active[material] = part; parts.Add(part);
                }
                return part;
            }
            public void Box(Vector3 position, Vector3 size, Material material)
            {
                var part = Get(material); DetailBox(part.vertices, part.triangles, position, size);
            }
            public void Beam(Vector3 a, Vector3 b, float width, Material material)
            {
                var part = Get(material); DetailBeam(part.vertices, part.triangles, a, b, width, width);
            }
            public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material material)
            {
                var part = Get(material); Quad(part.vertices, part.triangles, a, b, c, d);
            }
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Material material)
            {
                var part = Get(material); int i = part.vertices.Count;
                part.vertices.Add(a); part.vertices.Add(b); part.vertices.Add(c);
                part.triangles.Add(i); part.triangles.Add(i + 1); part.triangles.Add(i + 2);
            }
            public void Flush(string label)
            {
                for (int i = 0; i < parts.Count; i++)
                    DetailMesh(label + " · " + parts[i].material.name, parts[i].vertices, parts[i].triangles, Vector3.zero, parts[i].material);
            }
        }

        static void HBSLand(HBSBatch b, Vector2[] outline, bool xiamen)
        {
            Material land = Mat(xiamen ? "Xiamen hinterland soil" : "Straits hinterland soil", xiamen ? "A2967D" : "9B957E");
            Material shore = Mat("continuous weathered shore", "8C8874");
            Material wet = Mat("continuous tidal shingle", "707B70");
            // Ear clipping handles the inward-curving banks without filling the bay.
            var polygon = new List<Vector2>(outline);
            float area = 0;
            for (int i = 0; i < polygon.Count; i++)
            { Vector2 a = polygon[i], c = polygon[(i + 1) % polygon.Count]; area += a.x * c.y - c.x * a.y; }
            if (area < 0) polygon.Reverse();
            var ring = new List<int>(); for (int i = 0; i < polygon.Count; i++) ring.Add(i);
            int guard = polygon.Count * polygon.Count;
            while (ring.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < ring.Count; i++)
                {
                    int ia = ring[(i + ring.Count - 1) % ring.Count], ib = ring[i], ic = ring[(i + 1) % ring.Count];
                    Vector2 a = polygon[ia], c = polygon[ic], m = polygon[ib];
                    if (HBSCross(m - a, c - m) <= .0001f) continue;
                    bool contains = false;
                    for (int j = 0; j < ring.Count; j++)
                    {
                        int candidate = ring[j]; if (candidate == ia || candidate == ib || candidate == ic) continue;
                        Vector2 p = polygon[candidate];
                        if (HBSCross(m - a, p - a) >= 0 && HBSCross(c - m, p - m) >= 0 && HBSCross(a - c, p - c) >= 0)
                        { contains = true; break; }
                    }
                    if (contains) continue;
                    b.Triangle(HBSP(a, -.04f), HBSP(c, -.04f), HBSP(m, -.04f), land);
                    ring.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) break;
            }
            if (ring.Count == 3)
                b.Triangle(HBSP(polygon[ring[0]], -.04f), HBSP(polygon[ring[2]], -.04f), HBSP(polygon[ring[1]], -.04f), land);

            // A sloped shore meets the sea instead of exposing the old box sides.
            // Main quay edges use a narrow, submerged skirt; outer coast gets a
            // shallow stony transition. No cap crosses z=22 / x=36,z=38.
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i], c = polygon[(i + 1) % polygon.Count];
                Vector2 outward = new Vector2(c.y - a.y, a.x - c.x).normalized;
                bool playableQuay = xiamen ? Mathf.Abs(a.y - 22) < .01f && Mathf.Abs(c.y - 22) < .01f :
                    (Mathf.Abs(a.x - 36) < .01f && Mathf.Abs(c.x - 36) < .01f) || (Mathf.Abs(a.y - 38) < .01f && Mathf.Abs(c.y - 38) < .01f);
                float width = playableQuay ? .18f : 2.4f;
                Vector3 topA = HBSP(a, -.045f), topC = HBSP(c, -.045f);
                Vector3 midA = HBSP(a + outward * width, -.56f), midC = HBSP(c + outward * width, -.56f);
                Vector3 lowA = HBSP(a + outward * (width + 3.0f), -1.48f), lowC = HBSP(c + outward * (width + 3.0f), -1.48f);
                b.Face(topA, topC, midC, midA, shore);
                b.Face(midA, midC, lowC, lowA, wet);
            }
        }

        static float HBSCross(Vector2 a, Vector2 c) => a.x * c.y - a.y * c.x;
        static Vector3 HBSP(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        static void HBSXiamen(HBSBatch b)
        {
            Material lane = Mat("Xiamen old departure lane", "B3AA94");
            HBSRoad(b, new[] { new Vector2(0,-33), new Vector2(0,-66), new Vector2(-8,-105), new Vector2(-24,-163) }, 11, lane);
            HBSRoad(b, new[] { new Vector2(-133,-45), new Vector2(-76,-38), new Vector2(-40,-38), new Vector2(0,-38), new Vector2(63,-40), new Vector2(127,-54) }, 8, lane);
            HBSRoad(b, new[] { new Vector2(-43,-37), new Vector2(-44,-4), new Vector2(-50,12) }, 5, lane);
            HBSRoad(b, new[] { new Vector2(43,-38), new Vector2(44,-9), new Vector2(50,10) }, 5, lane);
            HBSRoad(b, new[] { new Vector2(-82,-83), new Vector2(-79,-39), new Vector2(-77,2) }, 4.5f, lane);
            HBSRoad(b, new[] { new Vector2(81,-88), new Vector2(78,-39), new Vector2(75,1) }, 4.5f, lane);

            // Low street fronts begin beyond the playable boundary. The southern
            // central avenue remains open, including the far-view camera approach.
            HBSHouse(b, new Vector3(-27,0,-48), 8, 9, 3.7f, false, 0);
            HBSHouse(b, new Vector3(28,0,-49), 8, 10, 3.8f, false, 0);
            HBSHouse(b, new Vector3(-46,0,-52), 9, 11, 4.3f, false, 0);
            HBSHouse(b, new Vector3(47,0,-53), 8, 11, 4.1f, false, 0);
            HBSHouse(b, new Vector3(-52,0,-17), 8, 10, 4.2f, false, 90);
            HBSHouse(b, new Vector3(53,0,-20), 9, 11, 4.5f, false, -90);
            HBSHouse(b, new Vector3(-53,0,3), 9, 10, 4.0f, false, 90);
            HBSHouse(b, new Vector3(55,0,-1), 9, 12, 4.4f, false, -90);
            HBSHouse(b, new Vector3(-66,0,-56), 8, 11, 4.8f, false, 0);
            HBSHouse(b, new Vector3(68,0,-57), 9, 10, 4.6f, false, 0);
            HBSHouse(b, new Vector3(-32,0,-79), 8, 11, 4.5f, false, 0);
            HBSHouse(b, new Vector3(28,0,-81), 9, 12, 4.6f, false, 0);
            HBSHouse(b, new Vector3(-92,0,-11), 9, 10, 5.0f, false, 0);
            HBSHouse(b, new Vector3(91,0,-12), 9, 12, 5.2f, false, 0);
            HBSHouse(b, new Vector3(-112,0,-48), 8, 12, 4.8f, false, 25);
            HBSHouse(b, new Vector3(114,0,-61), 10, 12, 5.4f, false, -12);
            HBSHouse(b, new Vector3(-57,0,-103), 9, 11, 5.1f, false, 0);
            HBSHouse(b, new Vector3(58,0,-111), 10, 12, 5.2f, false, 0);

            // Shore villages are grounded in broad rising terrain, not floating
            // mountain blobs. They frame the open northern shipping view.
            HBSMound(b, new Vector3(-143,-.08f,-44), new Vector3(44,24,49), Mat("Xiamen inland olive ridge", "7B846D"), 12);
            HBSMound(b, new Vector3(147,-.08f,-61), new Vector3(48,31,57), Mat("Xiamen inland distant ridge", "7D8D7C"), 12);
            HBSMound(b, new Vector3(-203,-.1f,-108), new Vector3(68,47,65), Mat("Xiamen far hillside", "84998B"), 12);
            HBSMound(b, new Vector3(210,-.1f,-133), new Vector3(73,51,70), Mat("Xiamen far hillside", "84998B"), 12);
            Vector3[] trees = { new Vector3(-41,0,-58),new Vector3(41,0,-67),new Vector3(-64,0,-27),new Vector3(65,0,-30),new Vector3(-98,0,-34),new Vector3(103,0,-36),new Vector3(-79,0,-76),new Vector3(87,0,-94),new Vector3(-31,0,-117),new Vector3(28,0,-118) };
            for (int i = 0; i < trees.Length; i++) HBSTree(b, trees[i], 5.0f + (i % 3) * .8f, false);
            HBSFishingShore(b, new Vector3(-67,0,11), false);
            HBSFishingShore(b, new Vector3(70,0,12), false);
            for (int i = 0; i < 22; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                HBSMound(b, new Vector3(side * (42 + (i / 2) * 6.2f), -.89f, 16 + Mathf.Sin(i * 1.7f) * 1.2f), new Vector3(1.7f + (i % 3) * .5f, 1.0f + (i % 4) * .13f, 1.2f), Mat("shore granite outcrop", "91988A"), 7);
            }
            HBSBoat(b, new Vector3(-55,-.66f,66), 1.05f, 22, true);
            HBSBoat(b, new Vector3(55,-.66f,91), 1.3f, -16, true);
            HBSBoat(b, new Vector3(-15,-.66f,129), .95f, -12, true);
            HBSChannelMark(b, new Vector3(-17,-.66f,53), false);
            HBSChannelMark(b, new Vector3(17,-.66f,53), true);
            HBSChannelMark(b, new Vector3(-30,-.66f,106), false);
            HBSChannelMark(b, new Vector3(30,-.66f,106), true);
        }

        static void HBSSingaporePort(HBSBatch b)
        {
            Material lane = Mat("Straits warehouse road dust", "A9A28A");
            HBSRoad(b, new[] { new Vector2(0,-33),new Vector2(-3,-72),new Vector2(-12,-140),new Vector2(-27,-194) }, 11, lane);
            HBSRoad(b, new[] { new Vector2(-37,-21),new Vector2(-78,-21),new Vector2(-137,-31),new Vector2(-195,-38) }, 9, lane);
            HBSRoad(b, new[] { new Vector2(-37,20),new Vector2(-77,16),new Vector2(-139,7) }, 7, lane);
            HBSRoad(b, new[] { new Vector2(-65,16),new Vector2(-64,-50),new Vector2(-72,-115) }, 7, lane);
            HBSRoad(b, new[] { new Vector2(-25,-43),new Vector2(-56,-44),new Vector2(-101,-51) }, 6, lane);

            // The port expands inland to the west and south. No building or land
            // cap enters the north/east navigation water.
            HBSHouse(b, new Vector3(-51,0,-7), 12, 15, 5.6f, true, 90);
            HBSHouse(b, new Vector3(-53,0,-37), 14, 16, 5.9f, true, 90);
            HBSHouse(b, new Vector3(-28,0,-49), 15, 13, 4.8f, true, 0);
            HBSHouse(b, new Vector3(27,0,-49), 12, 12, 4.6f, true, 0);
            HBSHouse(b, new Vector3(-81,0,-8), 15, 13, 6.1f, true, 0);
            HBSHouse(b, new Vector3(-93,0,-42), 18, 15, 6.3f, true, 0);
            HBSHouse(b, new Vector3(-49,0,-74), 14, 16, 5.6f, true, 0);
            HBSHouse(b, new Vector3(6,0,-87), 13, 15, 5.7f, true, 0);
            HBSHouse(b, new Vector3(-25,0,-117), 15, 14, 6.0f, true, 0);
            HBSHouse(b, new Vector3(-110,0,-78), 16, 15, 6.2f, true, 0);
            HBSHouse(b, new Vector3(-118,0,-8), 17, 15, 6.5f, true, 0);
            HBSHouse(b, new Vector3(-77,0,-103), 12, 13, 6.0f, true, 0);
            // A lower inhabited lane beyond the warehouses distinguishes the
            // warm working settlement from Xiamen's stone departure street.
            for (int i = 0; i < 5; i++)
                HBSHouse(b, new Vector3(-117 + i * 14,0,-137 - (i % 2) * 3), 8, 11, 4.3f + (i % 2) * .5f, false, 0);

            Vector3[] palms = { new Vector3(-42,0,-52),new Vector3(-42,0,24),new Vector3(-74,0,21),new Vector3(-103,0,17),new Vector3(-119,0,-24),new Vector3(-82,0,-57),new Vector3(-69,0,-86),new Vector3(18,0,-68),new Vector3(10,0,-112),new Vector3(-37,0,-98),new Vector3(-96,0,-125),new Vector3(-132,0,-106),new Vector3(-19,0,-153) };
            for (int i = 0; i < palms.Length; i++) HBSTree(b, palms[i], 6.5f + (i % 4) * .7f, true);
            for (int i = 0; i < 8; i++)
            {
                Vector3 center = new Vector3(-77 + (i % 4) * 8.6f,0,-32 - (i / 4) * 33);
                HBSCargo(b, center, 2 + (i % 2), 1.1f);
            }
            HBSFishingShore(b, new Vector3(-55,0,27), true);
            HBSFishingShore(b, new Vector3(23,0,-60), true);
            for (int i = 0; i < 12; i++)
                HBSMound(b, new Vector3(-43 - i * 6.7f,-.90f,28 - i * .25f), new Vector3(2.3f,.95f + (i % 3) * .18f,1.45f), Mat("Straits tidal stones", "879180"), 7);
            HBSMound(b, new Vector3(-156,-.08f,-106), new Vector3(38,15,42), Mat("Straits inland green", "758B67"), 12);
            HBSMound(b, new Vector3(-210,-.08f,-167), new Vector3(61,27,69), Mat("Straits distant tree line", "789684"), 12);
            HBSBoat(b, new Vector3(75,-.66f,-15), 1.35f, 5, true);
            HBSBoat(b, new Vector3(98,-.66f,43), 1.2f, -25, true);
            HBSBoat(b, new Vector3(34,-.66f,112), 1.45f, 63, true);
            HBSBoat(b, new Vector3(-56,-.66f,76), .82f, 72, false);
            HBSChannelMark(b, new Vector3(52,-.66f,51), true);
            HBSChannelMark(b, new Vector3(78,-.66f,76), false);
            HBSChannelMark(b, new Vector3(11,-.66f,70), true);
        }

        static void HBSRoad(HBSBatch b, Vector2[] points, float width, Material material)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 direction = (points[i + 1] - points[i]).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x) * width * .5f;
                b.Face(HBSP(points[i] - side,-.025f),HBSP(points[i] + side,-.025f),HBSP(points[i + 1] + side,-.025f),HBSP(points[i + 1] - side,-.025f),material);
                // Sparse worn stone strips make the lane legible without modern markings.
                for (int j = 0; j < 7; j++)
                {
                    Vector2 p = Vector2.Lerp(points[i],points[i + 1],(j + .5f) / 7);
                    b.Box(HBSP(p,-.018f),new Vector3(width * .13f,.012f,.72f),Mat("old lane granite fragments","B4AF9B"));
                }
            }
        }

        static void HBSHouse(HBSBatch b, Vector3 p, float width, float depth, float height, bool warehouse, float yaw)
        {
            Quaternion turn = Quaternion.Euler(0,yaw,0);
            Material wall = warehouse ? Mat("surrounding limewashed godown","B3AA8C") : Mat("surrounding old red brick","9A6048");
            Material roof = Mat("surrounding grey clay roofs","585A4D");
            HBSRotatedBox(b,p + Vector3.up * (.20f - .04f),new Vector3(width + .26f,.4f,depth + .25f),turn,Stone);
            HBSRotatedBox(b,p + Vector3.up * (height * .5f + .16f),new Vector3(width,height,depth),turn,wall);
            float eave = height + .19f, rise = Mathf.Min(depth * .25f,2.3f);
            Vector3 a=p+turn*new Vector3(-width*.55f,eave,-depth*.55f),c=p+turn*new Vector3(width*.55f,eave,-depth*.55f);
            Vector3 d=p+turn*new Vector3(-width*.55f,eave,depth*.55f),e=p+turn*new Vector3(width*.55f,eave,depth*.55f);
            Vector3 peakA=p+turn*new Vector3(-width*.55f,eave+rise,0),peakC=p+turn*new Vector3(width*.55f,eave+rise,0);
            b.Face(a,peakA,peakC,c,roof); b.Face(e,peakC,peakA,d,roof);
            b.Triangle(p+turn*new Vector3(-width*.5f,eave,-depth*.5f),p+turn*new Vector3(-width*.5f,eave,depth*.5f),p+turn*new Vector3(-width*.5f,eave+rise,0),wall);
            b.Triangle(p+turn*new Vector3(width*.5f,eave,depth*.5f),p+turn*new Vector3(width*.5f,eave,-depth*.5f),p+turn*new Vector3(width*.5f,eave+rise,0),wall);
            b.Beam(peakA+Vector3.up*.10f,peakC+Vector3.up*.10f,.20f,roof);
            for(int i=0;i<9;i++)
            {
                float u=(i+.5f)/9;
                b.Beam(Vector3.Lerp(a,c,u)+Vector3.up*.018f,Vector3.Lerp(peakA,peakC,u)+Vector3.up*.018f,.045f,Mat("surrounding roof seams","6D6E5E"));
                b.Beam(Vector3.Lerp(d,e,u)+Vector3.up*.018f,Vector3.Lerp(peakA,peakC,u)+Vector3.up*.018f,.045f,Mat("surrounding roof seams","6D6E5E"));
            }
            float front = -depth*.5f-.03f;
            float doorWidth = warehouse ? width*.51f : 1.75f;
            HBSRotatedBox(b,p+turn*new Vector3(0,1.61f,front),new Vector3(doorWidth,2.85f,.10f),turn,Shadow);
            for(int i=0;i<(warehouse?7:3);i++)
            {
                int count=warehouse?7:3;
                HBSRotatedBox(b,p+turn*new Vector3(-doorWidth*.5f+(i+.5f)*doorWidth/count,1.60f,front-.065f),new Vector3(doorWidth/count-.038f,2.78f,.045f),turn,Timber);
            }
            foreach(float side in new[]{-1f,1f})
            {
                float x=side*width*.34f;
                HBSRotatedBox(b,p+turn*new Vector3(x,2.65f,front),new Vector3(width*.18f,1.48f,.10f),turn,Shadow);
                for(int i=0;i<5;i++) HBSRotatedBox(b,p+turn*new Vector3(x,2.05f+i*.29f,front-.065f),new Vector3(width*.19f,.11f,.08f),turn,Dark);
            }
            if(warehouse)
            {
                float awning=front-1.05f;
                HBSRotatedBox(b,p+turn*new Vector3(0,3.54f,awning),new Vector3(width*.84f,.16f,2.25f),turn,Mat("godown loading canopy","787664"));
                foreach(float x in new[]{-width*.38f,width*.38f}) b.Beam(p+turn*new Vector3(x,0,awning-.8f),p+turn*new Vector3(x,3.53f,awning-.8f),.17f,Dark);
                HBSCargo(b,p+turn*new Vector3(width*.28f,0,front-1.2f),2,1.15f);
            }
        }

        static void HBSRotatedBox(HBSBatch b,Vector3 p,Vector3 size,Quaternion rotation,Material material)
        {
            // Batched rotated cuboid; never creates a Primitive/Collider.
            Vector3 a=rotation*new Vector3(-size.x,-size.y,-size.z)*.5f+p,c=rotation*new Vector3(size.x,-size.y,-size.z)*.5f+p;
            Vector3 d=rotation*new Vector3(size.x,size.y,-size.z)*.5f+p,e=rotation*new Vector3(-size.x,size.y,-size.z)*.5f+p;
            Vector3 f=rotation*new Vector3(-size.x,-size.y,size.z)*.5f+p,g=rotation*new Vector3(size.x,-size.y,size.z)*.5f+p;
            Vector3 h=rotation*new Vector3(size.x,size.y,size.z)*.5f+p,j=rotation*new Vector3(-size.x,size.y,size.z)*.5f+p;
            b.Face(a,e,d,c,material);b.Face(f,g,h,j,material);b.Face(a,f,j,e,material);
            b.Face(c,d,h,g,material);b.Face(e,j,h,d,material);b.Face(a,c,g,f,material);
        }

        static void HBSMound(HBSBatch b,Vector3 p,Vector3 size,Material material,int sides)
        {
            float[] radius={1,.91f,.56f,.08f};float[] elevation={0,.30f,.77f,1};
            for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
                float irregularA=1+.085f*Mathf.Sin(i*2.7f),irregularC=1+.085f*Mathf.Sin((i+1)*2.7f);
                Vector3 lowA=p+new Vector3(Mathf.Cos(a)*size.x*radius[ring]*irregularA,elevation[ring]*size.y,Mathf.Sin(a)*size.z*radius[ring]);
                Vector3 lowC=p+new Vector3(Mathf.Cos(c)*size.x*radius[ring]*irregularC,elevation[ring]*size.y,Mathf.Sin(c)*size.z*radius[ring]);
                Vector3 highA=p+new Vector3(Mathf.Cos(a)*size.x*radius[ring+1]*irregularA,elevation[ring+1]*size.y,Mathf.Sin(a)*size.z*radius[ring+1]);
                Vector3 highC=p+new Vector3(Mathf.Cos(c)*size.x*radius[ring+1]*irregularC,elevation[ring+1]*size.y,Mathf.Sin(c)*size.z*radius[ring+1]);
                b.Face(lowA,highA,highC,lowC,material);
                if(ring==2)b.Triangle(highA,p+Vector3.up*size.y,highC,material);
            }
        }

        static void HBSTree(HBSBatch b,Vector3 p,float height,bool palm)
        {
            Vector3 crown=p+new Vector3(palm ? .45f : 0,height,0);
            b.Beam(p,crown,palm ? .29f : .63f,Mat("surrounding worn tree bark","77644C"));
            if(palm)
            {
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4;Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    Vector3 middle=crown+direction*1.65f+Vector3.up*.57f,tip=crown+direction*3.7f-Vector3.up*1.0f;
                    Vector3 side=new Vector3(-direction.z,0,direction.x)*.62f;
                    b.Face(crown,middle-side,tip,middle+side,Mat("surrounding palm fronds","607D58"));
                    b.Face(middle+side,tip,middle-side,crown,Mat("surrounding palm fronds","607D58"));
                    b.Beam(crown,tip,.044f,Mat("surrounding palm ribs","7B8661"));
                }
            }
            else
            {
                for(int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI*.5f;Vector3 end=crown+new Vector3(Mathf.Cos(a)*1.65f,-.70f,Mathf.Sin(a)*1.5f);
                    b.Beam(p+Vector3.up*height*.6f,end,.23f,Timber);
                    HBSMound(b,end-Vector3.up*.5f,new Vector3(2.7f,2.0f,2.5f),Mat("surrounding banyan leaves","667D55"),8);
                }
                HBSMound(b,crown-Vector3.up*.3f,new Vector3(2.5f,2.1f,2.6f),Mat("surrounding banyan crown","788A62"),8);
            }
        }

        static void HBSCargo(HBSBatch b,Vector3 p,int count,float size)
        {
            for(int i=0;i<count;i++)
            {
                Vector3 c=p+new Vector3((i-(count-1)*.5f)*(size+.10f),size*.5f,0);
                b.Box(c,new Vector3(size,size,size),Timber);
                foreach(float x in new[]{-.32f,.32f})b.Box(c+Vector3.right*x*size,new Vector3(.055f,size+.025f,size+.02f),Dark);
                b.Beam(c+new Vector3(-size*.4f,-size*.4f,-size*.51f),c+new Vector3(size*.4f,size*.4f,-size*.51f),.09f,Dark);
            }
        }

        static void HBSFishingShore(HBSBatch b,Vector3 p,bool tropical)
        {
            // Low racks, hand-laid nets and pulled-up skiffs make the shore inhabited.
            foreach(float x in new[]{-2.6f,2.6f})b.Beam(p+new Vector3(x,0,-1),p+new Vector3(x,2.8f,-1),.12f,Dark);
            b.Beam(p+new Vector3(-2.6f,2.65f,-1),p+new Vector3(2.6f,2.65f,-1),.10f,Timber);
            for(int i=0;i<11;i++)
                b.Beam(p+new Vector3(-2.45f+i*.49f,2.48f,-1),p+new Vector3(-2.45f+i*.49f,.7f,-1.24f),.018f,Mat("old fishing net hemp","887D60"));
            for(int i=0;i<5;i++)b.Beam(p+new Vector3(-2.5f,.75f+i*.35f,-1.18f),p+new Vector3(2.5f,.75f+i*.35f,-1.18f),.018f,Mat("old fishing net hemp","887D60"));
            HBSBoat(b,p+new Vector3(tropical?-5:5,.06f,-2.5f),.50f,76,false);
            HBSCargo(b,p+new Vector3(-4.2f,0,-3.8f),2,.72f);
        }

        static void HBSBoat(HBSBatch b,Vector3 p,float scale,float yaw,bool sail)
        {
            Quaternion q=Quaternion.Euler(0,yaw,0);
            Vector3[] rim={new Vector3(-1.55f,.56f,-4),new Vector3(1.55f,.56f,-4),new Vector3(1.8f,.75f,2.6f),new Vector3(0,1.04f,4.7f),new Vector3(-1.8f,.75f,2.6f)};
            for(int i=0;i<rim.Length;i++)
            {
                int j=(i+1)%rim.Length;Vector3 a=p+q*(rim[i]*scale),c=p+q*(rim[j]*scale);
                Vector3 bottomA=p+q*(new Vector3(rim[i].x*.68f,-.68f,rim[i].z*.88f)*scale),bottomC=p+q*(new Vector3(rim[j].x*.68f,-.68f,rim[j].z*.88f)*scale);
                b.Face(a,bottomA,bottomC,c,Mat("small harbor craft hull","5A4938"));
                b.Beam(a,c,.10f*scale,Timber);
            }
            HBSRotatedBox(b,p+Vector3.up*.40f*scale,new Vector3(2.8f,.13f,6.5f)*scale,q,Timber);
            for(int i=0;i<3;i++)HBSRotatedBox(b,p+q*(new Vector3(0,.67f,-2.4f+i*1.9f)*scale),new Vector3(2.8f,.14f,.28f)*scale,q,Dark);
            if(!sail)return;
            Vector3 mast=p+q*(new Vector3(-.20f,.52f,.0f)*scale);
            b.Beam(mast,mast+Vector3.up*7.8f*scale,.13f*scale,Dark);
            Vector3 aS=p+q*(new Vector3(-.15f,2.5f,.12f)*scale),bS=p+q*(new Vector3(3.8f,3.0f,.12f)*scale),cS=p+q*(new Vector3(2.7f,8.0f,.12f)*scale),dS=p+q*(new Vector3(-.15f,7.7f,.12f)*scale);
            b.Face(aS,bS,cS,dS,Mat("distant sail unbleached canvas","C7BEA1"));b.Face(dS,cS,bS,aS,Mat("distant sail unbleached canvas","C7BEA1"));
            for(int i=0;i<6;i++)b.Beam(Vector3.Lerp(aS,dS,i/5f),Vector3.Lerp(bS,cS,i/5f),.038f*scale,Timber);
            b.Beam(mast+Vector3.up*7.5f*scale,p+q*(new Vector3(-1.45f,.6f,-3.6f)*scale),.018f*scale,Dark);
        }

        static void HBSChannelMark(HBSBatch b,Vector3 p,bool red)
        {
            b.Beam(p-Vector3.up*1.3f,p+Vector3.up*3.4f,.18f,Dark);
            b.Box(p+Vector3.up*2.5f,new Vector3(.55f,.52f,.24f),red?Red:Mat("faded channel white","C6C0A8"));
            b.Face(p+new Vector3(0,3.45f,0),p+new Vector3(0,2.82f,0),p+new Vector3(1.02f,2.98f,.05f),p+new Vector3(.94f,3.42f,.05f),red?Red:Linen);
        }
    }
}
