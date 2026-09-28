using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>Period-inspired destination variants. These are composite places, not surveyed reconstructions.</summary>
    public static class DestinationWorld
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static readonly string[] Overseas = { "port", "market", "quarters", "postoffice" };

        static DestinationWorld()
        {
            Application.quitting += ReleaseMaterials;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ReleaseMaterials;
#endif
        }

        static void ReleaseMaterials()
        {
            foreach (var material in materials.Values) if (material)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            materials.Clear();
        }

        public static string Normalize(string destination) => destination == "penang" || destination == "rangoon" ? destination : "singapore";
        public static string Name(string destination) => Normalize(destination) == "penang" ? "槟榔屿" : Normalize(destination) == "rangoon" ? "仰光" : "新加坡";
        public static string Localize(string text, string destination)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("新加坡", Name(destination));
        }

        public static RegionInfo Describe(RegionInfo source, string destination)
        {
            if (source == null) return null;
            string d = Normalize(destination);
            // Deep-copy labels; WorldRegions and its note positions remain the source of navigation truth.
            var r = new RegionInfo { id = source.id, title = Localize(source.title, d), description = source.description,
                bounds = source.bounds, spawn = source.spawn, rest = source.rest, sidePickup = source.sidePickup, sideDrop = source.sideDrop };
            r.zones = new RegionZone[source.zones == null ? 0 : source.zones.Length];
            for (int i = 0; i < r.zones.Length; i++) { var z = source.zones[i]; r.zones[i] = new RegionZone(Localize(z.name,d), z.bounds.x, z.bounds.y, z.bounds.width, z.bounds.height); }
            r.notes = new RegionNote[source.notes == null ? 0 : source.notes.Length];
            for (int i = 0; i < r.notes.Length; i++) { var n = source.notes[i]; r.notes[i] = new RegionNote(n.id, Localize(n.title,d), Localize(n.body,d), n.position.x, n.position.z); }
            if (Array.IndexOf(Overseas, source.id) < 0) return r;
            if (d == "penang")
            {
                if (r.id == "port") { r.title = "槟榔屿 · 海墘货埠"; r.description = "石驳岸、木桩桥、沿海货棚与同乡接应处"; Note(r,0,"木桥外的乡音","海墘一带，木屋立在桩脚上，货物沿木桥接驳。接应的乡亲也在打零工；认识同乡不等于有了稳妥差事。"); }
                if (r.id == "market") { r.title = "槟榔屿 · 店屋街市"; r.description = "窄面店屋、彩灰山墙、杂货柜台与晒货后院"; Note(r,0,"街铺的生意","布匹、米粮和日用杂货挤在同一条街上。账要核清，赊欠不能只凭乡音；小本生意同样有失手的时候。"); }
                if (r.id == "quarters") { r.title = "槟榔屿 · 同乡客寓"; r.description = "木廊客寓、通铺、洗衣院与共用灶间"; Note(r,0,"客寓里的饭钱","同乡能替你介绍床位，也要与你分担柴米。工钱进来以前，借宿、吃饭和看病都得留出余地。"); }
                if (r.id == "postoffice") { r.title = "槟榔屿 · 银信铺"; r.description = "店屋里的写批案、收寄柜台与封袋后院"; Note(r,1,"从槟榔屿寄回泉州","把银钱、收件乡村和想说的话一起交代清楚。跨海银信要经转递，柜台留底与家乡回批，是追问下落的凭据。"); }
            }
            else if (d == "rangoon")
            {
                if (r.id == "port") { r.title = "仰光 · 江岸货埠"; r.description = "米粮货栈、柚木装运场、接驳木栈桥与候工棚"; Note(r,0,"米袋与木料","江岸堆着米袋和长木。搬运要靠体力，也要听懂点货的规矩；不清楚的扣款，先留下工单再去问。"); }
                if (r.id == "market") { r.title = "仰光 · 米粮街"; r.description = "砖墙货铺、木百叶窗、粮行柜台与进货院"; Note(r,0,"粮行的秤与账","同一批米，要过秤、记数、核对收据。头一次独自谋生，最大的难处往往是不知道能向谁讨个公道。"); }
                if (r.id == "quarters") { r.title = "仰光 · 江岸客工院"; r.description = "深檐木廊、简陋通铺、洗衣后院与公用灶间"; Note(r,0,"异乡的雨檐","雨落在深檐外，客工围着灶火分饭。乡亲之外，也有人愿意教你当地的称呼、替你向柜台说明情况。"); }
                if (r.id == "postoffice") { r.title = "仰光 · 银信代办处"; r.description = "纸墨柜、写批台、寄款留底与转递封袋房"; Note(r,1,"长路上的银信","仰光与海峡港市、厦门之间有航运联系。本故事将转递过程压缩为可游玩的步骤；具体船期与经手局号为虚构。"); }
            }
            return r;
        }

        static void Note(RegionInfo r, int index, string title, string body)
        {
            if (index >= r.notes.Length) return;
            r.notes[index].title = title; r.notes[index].body = body;
        }

        public static void Dress(GameObject world, string region, string destination)
        {
            if (!world || Array.IndexOf(Overseas, region) < 0 || world.transform.Find("Destination scenery")) return;
            string d = Normalize(destination);
            Retint(world,d);
            var root = new GameObject("Destination scenery"); root.transform.SetParent(world.transform,false);
            var geometry = new Geometry(root.transform,d);
            if (region == "port") Port(geometry,d);
            else Town(geometry,region,d);
            geometry.Finish();
            // All destination pieces are renderer-only. Even the roofs have no physics,
            // so they cannot capture the existing ground raycasts or block delivery routes.
            Debug.Log("DESTINATION_WORLD=" + d + "/" + region + " decorative meshes=" + root.transform.childCount);
        }

        static void Retint(GameObject world,string d)
        {
            if (d == "singapore") return;
            foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material m = renderer.sharedMaterial; if (!m || renderer.GetComponent<TextMesh>()) continue;
                string n = m.name.ToLowerInvariant(); string tint = null;
                if (n.Contains("roof") || n.Contains("tile highlight")) tint = d == "penang" ? "7B5140" : "66564B";
                else if (n.Contains("limewash") || n.Contains("plaster") || n.Contains("rough lime")) tint = d == "penang" ? "C5B79A" : "B9AD94";
                else if (n.Contains("brick") && !n.Contains("joint")) tint = d == "penang" ? "A46C50" : "984F38";
                else if (n == "teal") tint = d == "penang" ? "62817A" : "4E6252";
                else if (n.Contains("port packed earth")) tint = d == "penang" ? "ADA18B" : "978269";
                if (tint == null) continue;
                string key = d + "/retint/" + n;
                if (!materials.TryGetValue(key,out var replacement) || !replacement)
                {
                    replacement = new Material(m) { name = key, hideFlags = HideFlags.DontSave }; replacement.color = ColorOf(tint); materials[key] = replacement;
                }
                renderer.sharedMaterial = replacement;
            }
        }

        static void Town(Geometry g,string region,string d)
        {
            // Facades sit against the existing northern outer block (front z=54),
            // beyond all traversable bounds. The street axis through x=0 stays visible.
            if (region == "quarters")
            {
                foreach (float x in new[] {-25f,25f}) Verandah(g,new Vector3(x,0,52.9f),18,3.15f,d);
            }
            else
            {
                foreach (float x in new[] {-25.8f,-17.2f,-8.6f,8.6f,17.2f,25.8f})
                    Facade(g,new Vector3(x,0,53.88f),7.8f,6.25f,d);
                if (region == "market")
                    foreach(float x in new[]{-30f,-21f,-12f,12f,21f,30f})
                        ShopTrim(g,new Vector3(x,0,29.88f),7.7f,d);
            }
            if (d == "rangoon")
            {
                Stupa(g,new Vector3(-70,0,129),1);
                // Work-yard pieces lie behind existing walls, not along an active route.
                TimberRack(g,new Vector3(31.8f,0,32),4.2f);
                RiceStack(g,new Vector3(-32,0,-28),3,2);
            }
            else if (d == "penang")
            {
                // Slatted eave work changes the silhouette of the inhabited courtyards.
                float z = region == "postoffice" ? 21 : region == "quarters" ? 15.1f : 24;
                EaveLace(g,new Vector3(-23,2.72f,z),13);
                if(region == "quarters") ClothesRail(g,new Vector3(-31,0,24));
            }
            else
            {
                if(region == "postoffice") ParcelTrolley(g,new Vector3(30,0,33));
                Verandah(g,new Vector3(-48,0,-25),9,4.1f,d);
            }
        }

        static void Port(Geometry g,string d)
        {
            if (d == "penang")
            {
                // The navigable stone quay ends at x=36. These water-side structures
                // are all outside that boundary, including their supporting piles.
                Jetty(g,new Vector3(39,-.15f,-13),35,3.2f);
                Jetty(g,new Vector3(42,-.15f,26),29,2.8f);
                for(int i=0;i<4;i++) StiltHouse(g,new Vector3(45+i*7.3f,.15f,-19),5.9f,7.7f,3.5f);
                for(int i=0;i<3;i++) StiltHouse(g,new Vector3(47+i*7.4f,.15f,31.5f),6.1f,7.5f,3.6f);
                g.Sign("海墘 · 槟榔屿",new Vector3(6.8f,3.55f,-24.1f),.09f);
                EaveLace(g,new Vector3(-29,2.78f,-8),9.4f);
            }
            else if (d == "rangoon")
            {
                Jetty(g,new Vector3(39,-.18f,-13),23,5);
                TimberRack(g,new Vector3(48,.15f,-13),13);
                RiceStack(g,new Vector3(13.7f,0,-5.5f),4,3);
                RiceStack(g,new Vector3(-31,0,32),3,2);
                // This composite mill is scenery on the landward side, not a named factory.
                g.Box("brick",new Vector3(-49,3.3f,6),new Vector3(13,6.6f,23));
                Roof(g,new Vector3(-49,6.6f,6),14,25,3.3f);
                g.Taper("brick",new Vector3(-53,0,17),1.2f,.66f,17,8);
                g.Taper("stone",new Vector3(-53,16.8f,17),.94f,.94f,.7f,8);
                Stupa(g,new Vector3(-63,0,127),1);
                g.Sign("仰光 · 米粮货埠",new Vector3(6.8f,3.55f,-24.1f),.08f);
            }
            else
            {
                // Larger stone fenders and loading posts distinguish the entrepot quay.
                for(int i=0;i<5;i++)
                {
                    float z=-25+i*12;
                    g.Box("stone",new Vector3(36.7f,-.23f,z),new Vector3(1.15f,1.9f,1.6f));
                    g.Taper("iron",new Vector3(37.3f,.28f,z),.20f,.31f,.65f,10);
                }
                Verandah(g,new Vector3(-46,0,-19),19,5.8f,d);
                g.Sign("新加坡 · 转口货港",new Vector3(6.8f,3.55f,-24.1f),.08f);
                ParcelTrolley(g,new Vector3(-32,0,32));
            }
        }

        static void Facade(Geometry g,Vector3 p,float w,float h,string d)
        {
            g.Box("lime",p+new Vector3(0,h*.68f,0),new Vector3(w,h*.64f,.18f));
            g.Box("stone",p+new Vector3(0,3.15f,-.12f),new Vector3(w+.18f,.18f,.33f));
            foreach(float side in new[]{-1f,1f})
            {
                float x=side*(w*.5f-.22f);
                g.Box("lime",p+new Vector3(x,h*.5f,-.13f),new Vector3(.32f,h,.35f));
                for(int i=0;i<2;i++) g.Box("stone",p+new Vector3(x,3.0f+i*3.0f,-.2f),new Vector3(.58f,.20f,.46f));
                Shutter(g,p+new Vector3(side*w*.23f,4.8f,-.2f),1.65f,1.7f);
            }
            g.Box("roof",p+new Vector3(0,h+.15f,.45f),new Vector3(w+.5f,.18f,1.35f));
            if(d=="penang")
            {
                for(int i=0;i<3;i++) g.Box(i%2==0?"lime":"accent",p+new Vector3(0,h+.3f+i*.25f,-.08f),new Vector3(w*(.74f-i*.15f),.28f,.32f));
                EaveLace(g,p+new Vector3(0,h-.1f,-.3f),w);
            }
            else if(d=="rangoon")
            {
                g.Box("timber",p+new Vector3(0,3.15f,-.35f),new Vector3(w,.15f,.8f));
                for(int i=0;i<7;i++) g.Beam("timber",p+new Vector3(-w*.45f+i*w*.15f,2.5f,0),p+new Vector3(-w*.45f+i*w*.15f,3.1f,-.9f),.10f);
            }
            else
            {
                g.Box("stone",p+new Vector3(0,h-.17f,-.13f),new Vector3(w,.18f,.28f));
                for(int i=-1;i<=1;i++) g.Box("accent",p+new Vector3(i*2.25f,3.65f,-.13f),new Vector3(1.4f,.32f,.12f));
            }
        }

        static void ShopTrim(Geometry g,Vector3 p,float w,string d)
        {
            // High strips only: no panels across an entrance or the shop's text sign.
            g.Box("accent",p+new Vector3(0,3.24f,-.04f),new Vector3(w,.16f,.18f));
            if(d=="penang") EaveLace(g,p+new Vector3(0,3.48f,-.38f),w);
            else if(d=="rangoon")
                for(int i=-2;i<=2;i++) g.Beam("timber",p+new Vector3(i*1.55f,3.1f,.1f),p+new Vector3(i*1.55f,3.62f,-.62f),.10f);
        }

        static void Verandah(Geometry g,Vector3 p,float w,float h,string d)
        {
            g.Box("timber",p+new Vector3(0,h,-.65f),new Vector3(w,.17f,2));
            int bays=Mathf.Max(3,Mathf.RoundToInt(w/3));
            for(int i=0;i<=bays;i++)
            {
                float x=-w*.5f+i*w/bays;
                g.Box(d=="singapore"?"lime":"timber",p+new Vector3(x,h*.5f,-1.4f),new Vector3(.24f,h,.24f));
                if(d=="singapore") g.Box("stone",p+new Vector3(x,h-.18f,-1.4f),new Vector3(.48f,.22f,.43f));
                else g.Beam("timber",p+new Vector3(x,h-.85f,-1.4f),p+new Vector3(x+.62f,h,-1.4f),.09f);
            }
            if(d=="penang") EaveLace(g,p+new Vector3(0,h-.14f,-1.5f),w);
        }

        static void EaveLace(Geometry g,Vector3 p,float w)
        {
            g.Box("accent",p,new Vector3(w,.15f,.12f));
            int count=Mathf.RoundToInt(w/.4f);
            for(int i=0;i<count;i++) g.Box("accent",p+new Vector3(-w*.5f+(i+.5f)*w/count,-.19f,0),new Vector3(.12f,.35f,.11f));
        }

        static void Shutter(Geometry g,Vector3 p,float w,float h)
        {
            g.Box("shadow",p,new Vector3(w+.12f,h+.16f,.07f));
            g.Box("accent",p+new Vector3(0,0,-.06f),new Vector3(w,h,.10f));
            g.Box("timber",p+new Vector3(0,0,-.13f),new Vector3(.065f,h,.05f));
            for(int i=0;i<8;i++)g.Box("timber",p+new Vector3(0,-h*.45f+i*h*.13f,-.125f),new Vector3(w,.045f,.035f));
        }

        static void Roof(Geometry g,Vector3 p,float w,float d,float rise)
        {
            g.Quad("roof",p+new Vector3(-w*.5f,0,-d*.5f),p+new Vector3(-w*.5f,rise,0),p+new Vector3(w*.5f,rise,0),p+new Vector3(w*.5f,0,-d*.5f));
            g.Quad("roof",p+new Vector3(w*.5f,0,d*.5f),p+new Vector3(w*.5f,rise,0),p+new Vector3(-w*.5f,rise,0),p+new Vector3(-w*.5f,0,d*.5f));
            g.Beam("timber",p+new Vector3(-w*.5f,rise,0),p+new Vector3(w*.5f,rise,0),.17f);
            foreach(float side in new[]{-1f,1f})
            {
                g.Beam("timber",p+new Vector3(side*w*.5f,0,-d*.5f),p+new Vector3(side*w*.5f,rise,0),.13f);
                g.Beam("timber",p+new Vector3(side*w*.5f,rise,0),p+new Vector3(side*w*.5f,0,d*.5f),.13f);
                Vector3 a=p+new Vector3(side*w*.5f,0,-d*.5f),b=p+new Vector3(side*w*.5f,rise,0),c=p+new Vector3(side*w*.5f,0,d*.5f);
                if(side>0)g.Triangle("timber",a,b,c);else g.Triangle("timber",a,c,b);
            }
        }

        static void Jetty(Geometry g,Vector3 p,float length,float width)
        {
            int count=Mathf.CeilToInt(length/.45f);
            for(int i=0;i<count;i++) g.Box("timber",p+new Vector3((i+.5f)*length/count,0,0),new Vector3(length/count-.025f,.18f,width));
            for(float x=1;x<length;x+=3.3f)foreach(float side in new[]{-1f,1f})
                g.Box("darkwood",p+new Vector3(x,-1.25f,side*width*.43f),new Vector3(.28f,2.8f,.28f));
        }

        static void StiltHouse(Geometry g,Vector3 p,float w,float d,float h)
        {
            g.Box("timber",p+new Vector3(0,h*.5f,0),new Vector3(w,h,d));
            foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
                g.Box("darkwood",p+new Vector3(sx*w*.43f,-1.3f,sz*d*.43f),new Vector3(.30f,2.9f,.30f));
            Roof(g,p+new Vector3(0,h,0),w+.7f,d+.7f,1.4f);
            Shutter(g,p+new Vector3(-w*.27f,2.15f,-d*.5f-.1f),1.05f,1.1f);
            g.Box("shadow",p+new Vector3(w*.23f,1.14f,-d*.5f-.06f),new Vector3(1.10f,2.1f,.10f));
            for(int i=0;i<12;i++) g.Box("darkwood",p+new Vector3(-w*.5f+(i+.5f)*w/12,h*.5f,-d*.5f-.04f),new Vector3(.022f,h,.026f));
        }

        static void TimberRack(Geometry g,Vector3 p,float length)
        {
            foreach(float z in new[]{-.9f,.9f}) g.Box("darkwood",p+new Vector3(0,.16f,z),new Vector3(length+.35f,.32f,.24f));
            for(int row=0;row<2;row++)for(int i=0;i<3-row;i++)
                g.CylinderBetween("timber",p+new Vector3(-length*.5f,.5f+row*.55f,-.7f+i*.7f+row*.35f),p+new Vector3(length*.5f,.5f+row*.55f,-.7f+i*.7f+row*.35f),.30f,10);
        }

        static void RiceStack(Geometry g,Vector3 p,int columns,int rows)
        {
            for(int row=0;row<rows;row++)for(int i=0;i<columns-row;i++)
            {
                Vector3 at=p+new Vector3((i-(columns-1)*.5f)*.8f+row*.4f,.3f+row*.52f,0);
                g.Box("sack",at,new Vector3(.76f,.50f,1.15f));
                g.Box("darkwood",at+new Vector3(0,.259f,0),new Vector3(.038f,.02f,1.13f));
            }
        }

        static void ClothesRail(Geometry g,Vector3 p)
        {
            foreach(float side in new[]{-1f,1f})g.Box("timber",p+new Vector3(side*1.4f,1.3f,0),new Vector3(.09f,2.6f,.09f));
            g.Beam("darkwood",p+new Vector3(-1.4f,2.4f,0),p+new Vector3(1.4f,2.4f,0),.04f);
            for(int i=0;i<3;i++)g.Box(i==1?"accent":"sack",p+new Vector3(-.9f+i*.8f,1.9f,0),new Vector3(.62f,.9f,.025f));
        }

        static void ParcelTrolley(Geometry g,Vector3 p)
        {
            g.Box("timber",p+new Vector3(0,.5f,0),new Vector3(1.35f,.13f,2.3f));
            g.CylinderBetween("darkwood",p+new Vector3(-.8f,.43f,0),p+new Vector3(.8f,.43f,0),.07f,8);
            foreach(float x in new[]{-.78f,.78f})g.CylinderBetween("darkwood",p+new Vector3(x-.07f,.43f,0),p+new Vector3(x+.07f,.43f,0),.42f,12);
            RiceStack(g,p+new Vector3(0,.58f,-.35f),2,1);
            foreach(float side in new[]{-1f,1f})g.Beam("timber",p+new Vector3(side*.57f,.55f,.9f),p+new Vector3(side*.57f,1.0f,2.4f),.085f);
        }

        static void Stupa(Geometry g,Vector3 p,float scale)
        {
            // A distant regional silhouette, deliberately not labeled as an exact landmark.
            g.Box("stone",p+Vector3.up*1.1f*scale,new Vector3(24,2.2f,24)*scale);
            float[] y={2.2f,3.1f,4.0f,5.0f,7.0f,10f,14f,18f,22f,28f,33f};
            float[] r={10,9,8.8f,7.8f,7.4f,6.5f,4.9f,2.8f,1.45f,.65f,.08f};
            for(int i=0;i<y.Length-1;i++)g.Taper("gold",p+Vector3.up*y[i]*scale,r[i]*scale,r[i+1]*scale,(y[i+1]-y[i])*scale,24);
            for(int i=0;i<4;i++)g.Taper("gold",p+Vector3.up*(28+i*1.1f)*scale,(1.5f-i*.30f)*scale,(1.5f-i*.30f)*scale,.09f*scale,20);
        }

        static Color ColorOf(string html) { ColorUtility.TryParseHtmlString("#"+html,out var color); return color; }
        static Material Mat(string d,string kind)
        {
            string key=d+"/"+kind; if(materials.TryGetValue(key,out var m)&&m)return m;
            string c=kind=="roof"?(d=="penang"?"81513E":d=="rangoon"?"685349":"545D55"):
                kind=="accent"?(d=="penang"?"597D74":d=="rangoon"?"506453":"496961"):
                kind=="lime"?"CDBF9F":kind=="stone"?"AAA695":kind=="brick"?"974F37":
                kind=="timber"?"866442":kind=="darkwood"?"503D2D":kind=="shadow"?"2C332E":kind=="sack"?"B8A77C":kind=="gold"?"BA9451":"4E5551";
            m=WorldSurfaceMaterials.Create("destination "+kind,ColorOf(c));materials[key]=m;return m;
        }

        sealed class Geometry
        {
            sealed class Part { public readonly List<Vector3> vertices=new List<Vector3>(); public readonly List<int> triangles=new List<int>(); }
            readonly Dictionary<string,Part> parts=new Dictionary<string,Part>(); readonly Transform parent; readonly string destination;
            public Geometry(Transform parent,string destination){this.parent=parent;this.destination=destination;}
            public void Quad(string material,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                if(!parts.TryGetValue(material,out var part)){part=new Part();parts.Add(material,part);}
                int n=part.vertices.Count;part.vertices.Add(a);part.vertices.Add(b);part.vertices.Add(c);part.vertices.Add(d);
                part.triangles.Add(n);part.triangles.Add(n+1);part.triangles.Add(n+2);part.triangles.Add(n);part.triangles.Add(n+2);part.triangles.Add(n+3);
            }
            public void Triangle(string material,Vector3 a,Vector3 b,Vector3 c)
            {
                if(!parts.TryGetValue(material,out var part)){part=new Part();parts.Add(material,part);}
                int n=part.vertices.Count;part.vertices.Add(a);part.vertices.Add(b);part.vertices.Add(c);
                part.triangles.Add(n);part.triangles.Add(n+1);part.triangles.Add(n+2);
            }
            public void Box(string material,Vector3 p,Vector3 size){BoxRotated(material,p,size,Quaternion.identity);}
            void BoxRotated(string m,Vector3 p,Vector3 size,Quaternion rotation)
            {
                Vector3 h=size*.5f;
                Vector3 a=p+rotation*new Vector3(-h.x,-h.y,-h.z),b=p+rotation*new Vector3(h.x,-h.y,-h.z),c=p+rotation*new Vector3(h.x,h.y,-h.z),d=p+rotation*new Vector3(-h.x,h.y,-h.z);
                Vector3 e=p+rotation*new Vector3(-h.x,-h.y,h.z),f=p+rotation*new Vector3(h.x,-h.y,h.z),g=p+rotation*new Vector3(h.x,h.y,h.z),k=p+rotation*new Vector3(-h.x,h.y,h.z);
                Quad(m,a,d,c,b);Quad(m,f,g,k,e);Quad(m,a,e,k,d);Quad(m,b,c,g,f);Quad(m,d,k,g,c);Quad(m,a,b,f,e);
            }
            public void Beam(string m,Vector3 a,Vector3 b,float width){BoxRotated(m,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),Quaternion.FromToRotation(Vector3.up,b-a));}
            public void Taper(string m,Vector3 p,float lower,float upper,float height,int sides)
            {
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                    Vector3 va=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),vb=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                    Quad(m,p+va*lower,p+va*upper+Vector3.up*height,p+vb*upper+Vector3.up*height,p+vb*lower);
                    Triangle(m,p+Vector3.up*height,p+vb*upper+Vector3.up*height,p+va*upper+Vector3.up*height);
                }
            }
            public void CylinderBetween(string m,Vector3 a,Vector3 b,float radius,int sides)
            {
                Quaternion q=Quaternion.FromToRotation(Vector3.up,b-a);float height=(b-a).magnitude;
                for(int i=0;i<sides;i++)
                {
                    float x=i*Mathf.PI*2/sides,y=(i+1)*Mathf.PI*2/sides;
                    Vector3 va=new Vector3(Mathf.Cos(x)*radius,0,Mathf.Sin(x)*radius),vb=new Vector3(Mathf.Cos(y)*radius,0,Mathf.Sin(y)*radius);
                    Quad(m,a+q*va,a+q*(va+Vector3.up*height),a+q*(vb+Vector3.up*height),a+q*vb);
                    Triangle(m,a,a+q*va,a+q*vb);Triangle(m,b,b+q*vb,b+q*va);
                }
            }
            public void Sign(string text,Vector3 p,float characterSize)
            {
                var font=Resources.Load<Font>("Fonts/Title");if(!font)return;font.RequestCharactersInTexture(text,48,FontStyle.Normal);
                var go=new GameObject("Destination sign");go.transform.SetParent(parent,false);go.transform.localPosition=p;
                var label=go.AddComponent<TextMesh>();label.text=text;label.font=font;label.fontSize=48;label.characterSize=characterSize;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=ColorOf("C6AA74");
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Box("darkwood",p+new Vector3(0,0,.08f),new Vector3(3.6f,.55f,.12f));
            }
            public void Finish()
            {
                var cleanup=parent.gameObject.AddComponent<DestinationMeshLifetime>();
                foreach(var pair in parts)
                {
                    var mesh=new Mesh{name="Destination "+destination+" "+pair.Key,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(pair.Value.vertices);mesh.SetTriangles(pair.Value.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                    var go=new GameObject("Destination detail · "+pair.Key);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=Mat(destination,pair.Key);cleanup.meshes.Add(mesh);
                }
            }
        }
    }

    [ExecuteAlways]
    public sealed class DestinationMeshLifetime : MonoBehaviour
    {
        [NonSerialized] public readonly List<Mesh> meshes = new List<Mesh>();
        void OnDestroy(){foreach(var mesh in meshes)if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
