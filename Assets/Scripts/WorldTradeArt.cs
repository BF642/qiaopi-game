using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    public partial class WorldGame
    {
        GameObject tradeLedgerArt;
        Transform tradeArtRoot, tradeScaleBeam, tradeRicePan, tradeWeightPan, tradeRiceFill;
        readonly GameObject[] tradePickupArt = new GameObject[3], tradePlacedArt = new GameObject[3];
        readonly GameObject[] tradeChangeCoins = new GameObject[30];
        TextMesh tradeScaleReadout, tradeCashReadout, tradeDebtReadout;
        GameObject tradeOutgoingArt, tradeReceiptArt;
        int tradeArtWeight = -1, tradeArtChange = -1, tradeArtDebt = -1;

        void SetupTradeArt()
        {
            tradeLedgerArt = null;
            tradeArtRoot = tradeScaleBeam = tradeRicePan = tradeWeightPan = tradeRiceFill = null;
            tradeScaleReadout = tradeCashReadout = tradeDebtReadout = null;
            tradeOutgoingArt = tradeReceiptArt = null;
            for (int i = 0; i < 3; i++) { tradePickupArt[i] = null; tradePlacedArt[i] = null; }
            for (int i = 0; i < tradeChangeCoins.Length; i++) tradeChangeCoins[i] = null;
            tradeArtWeight = tradeArtChange = tradeArtDebt = -1;
            if (!actors || !TradeWork.IsActive(state)) return;
            tradeArtRoot = new GameObject("职业操作 · 实物工作台").transform;
            tradeArtRoot.SetParent(actors.transform, false);
            if (state.tradeShift.route == "dock") BuildTradeDock();
            else if (state.tradeShift.route == "shop") BuildTradeShop();
            else BuildTradeCourier();
            UpdateTradeArt(); Physics.SyncTransforms(); WalkPath.Invalidate();
        }

        void UpdateTradeArt()
        {
            if (!tradeArtRoot || !TradeWork.IsActive(state)) return;
            var shift = state.tradeShift;
            if (shift.route == "dock") {
                for (int i = 0; i < TradeWork.DockCount(state); i++) {
                    bool placed = (shift.deliveredMask & (1 << i)) != 0;
                    if (tradePickupArt[i]) tradePickupArt[i].SetActive(!placed && shift.held != i);
                    if (tradePlacedArt[i]) tradePlacedArt[i].SetActive(placed);
                }
            }
            else if (shift.route == "shop") {
                int target = TradeWork.ShopWeightTarget(state);
                float angle = Mathf.Clamp((shift.weightUnits - target) * 4, -14, 14);
                if (tradeScaleBeam) tradeScaleBeam.localRotation = Quaternion.Euler(0, 0, angle);
                Quaternion tilt = Quaternion.Euler(0, 0, angle);
                if (tradeRicePan) tradeRicePan.localPosition = tilt * new Vector3(-.49f, 0, 0) + Vector3.down * .28f;
                if (tradeWeightPan) tradeWeightPan.localPosition = tilt * new Vector3(.49f, 0, 0) + Vector3.down * .28f;
                if (tradeRiceFill) { tradeRiceFill.gameObject.SetActive(shift.weightUnits > 0); tradeRiceFill.localScale = new Vector3(1, .18f + shift.weightUnits * .09f, 1); }
                for (int i = 0; i < tradeChangeCoins.Length; i++) if (tradeChangeCoins[i]) tradeChangeCoins[i].SetActive(i < shift.change);
                if (tradeScaleReadout && tradeArtWeight != shift.weightUnits) {
                    tradeScaleReadout.text = "已称 " + (shift.weightUnits * .5f).ToString("0.#") + " 斤";
                    tradeArtWeight = shift.weightUnits;
                }
                if (tradeCashReadout && tradeArtChange != shift.change) { tradeCashReadout.text = "找零 " + shift.change; tradeArtChange = shift.change; }
                if (tradeDebtReadout && tradeArtDebt != shift.debt) { tradeDebtReadout.text = "赊欠 " + shift.debt; tradeArtDebt = shift.debt; }
            }
            else {
                if (tradeOutgoingArt) tradeOutgoingArt.SetActive(shift.stage == "read");
                if (tradeReceiptArt) tradeReceiptArt.SetActive(shift.stage == "receipt");
            }
        }

        void BuildTradeDock()
        {
            var g = new TradeScenery.Geometry(tradeArtRoot);
            for (int zone = 0; zone < 3; zone++) {
                Vector3 p = TradePlaces.DockDrop(zone) + Vector3.right * 1.45f;
                p = WorldFactory.CharacterGroundPosition(p);
                TradeScenery.Pallet(g, p, zone == 1);
                // All labels face the worker to the west. A paper store gets a real
                // rain roof, while the cloth cart has wheels and push handles.
                Quaternion west = Quaternion.Euler(0, 90, 0);
                Vector3 sign = p + new Vector3(-.79f, 1.10f, 0);
                g.Box(sign, new Vector3(1.34f, .49f, .055f), TradeScenery.Dark, west);
                g.Sign(TradePlaces.DockZone(state,zone), sign + new Vector3(-.031f, 0, 0), .073f, TradeScenery.Paper.color, west);
                g.Beam(p + new Vector3(-.74f, .2f, 0), p + new Vector3(-.74f, 1.38f, 0), .048f, TradeScenery.Wood);
                if (zone == 2) TradeScenery.RainRoof(g, p);
            }
            for (int i = 0; i < TradeWork.DockCount(state); i++) {
                int zone = TradeWork.DockDestination(state, i);
                Vector3 pick = WorldFactory.CharacterGroundPosition(TradePlaces.DockPickup(i) + Vector3.left * 1.35f);
                TradeScenery.PickupSkid(g, pick);
                tradePickupArt[i] = TradeScenery.Parcel(tradeArtRoot, pick + Vector3.up * .17f, zone, true);
                Vector3 drop = WorldFactory.CharacterGroundPosition(TradePlaces.DockDrop(zone) + Vector3.right * 1.45f);
                tradePlacedArt[i] = TradeScenery.Parcel(tradeArtRoot, drop + Vector3.up * (zone == 1 ? .70f : .23f), zone, false);
            }
            if (WorkEvents.ChoiceId(state) == "pair" || WorkEvents.ChoiceId(state) == "teach") {
                Vector3 shared = WorldFactory.CharacterGroundPosition(new Vector3(-14.45f, 0, 12));
                TradeScenery.PickupSkid(g, shared);
                var parcel = TradeScenery.Parcel(tradeArtRoot, shared + Vector3.up * .17f, 1, false);
                parcel.name = "工友接手的布匹";
            }
            g.Flush();
        }

        void BuildTradeShop()
        {
            Vector3 p = WorldFactory.CharacterGroundPosition(new Vector3(-14.6f, 0, 13.5f));
            var g = new TradeScenery.Geometry(tradeArtRoot);
            TradeScenery.Table(g, p, 3.1f, .85f, .89f);
            Quaternion north = Quaternion.Euler(0, 180, 0);
            g.Box(p + new Vector3(0, .66f, .442f), new Vector3(1.35f, .26f, .035f), TradeScenery.Dark);
            g.Sign("陈记 · 称货找零", p + new Vector3(0, .66f, .465f), .054f, TradeScenery.Paper.color, north);
            // A raised beam balance and its two independently hanging pans.
            Vector3 pivot = p + new Vector3(.30f, 1.56f, 0);
            g.Box(p + new Vector3(.30f, .93f, 0), new Vector3(.40f, .085f, .36f), TradeScenery.Dark);
            g.Box(p + new Vector3(.30f, 1.24f, 0), new Vector3(.055f, .60f, .055f), TradeScenery.Brass);
            tradeScaleBeam = TradeScenery.Group(tradeArtRoot, "可摆动秤梁", pivot);
            var beam = new TradeScenery.Geometry(tradeScaleBeam);
            beam.Box(Vector3.zero, new Vector3(1.16f, .035f, .035f), TradeScenery.Brass);
            for (int i = -5; i <= 5; i++) beam.Box(new Vector3(i * .10f, .024f, .002f), new Vector3(.009f, .02f, .043f), TradeScenery.Dark);
            beam.Flush();
            Transform support = TradeScenery.Group(tradeArtRoot, "秤盘悬挂中心", pivot);
            tradeRicePan = TradeScenery.ScalePan(support, "米盘", false);
            tradeWeightPan = TradeScenery.ScalePan(support, "砝码盘", true);
            tradeRiceFill = TradeScenery.Group(tradeRicePan, "随称量增加的米", new Vector3(0, .043f, 0));
            var rice = new TradeScenery.Geometry(tradeRiceFill);
            rice.Lathe(Vector3.zero, new[] { new Vector2(0,0), new Vector2(.15f,0), new Vector2(.18f,.026f), new Vector2(.13f,.08f), new Vector2(0,.105f) }, TradeScenery.Rice);
            for (int i = 0; i < 17; i++) {
                float a = i * 2.39f, radius = .034f + (i % 4) * .032f;
                rice.Box(new Vector3(Mathf.Cos(a) * radius, .097f - radius * .34f, Mathf.Sin(a) * radius), new Vector3(.018f,.012f,.009f), i % 3 == 0 ? TradeScenery.Paper : TradeScenery.Rice, Quaternion.Euler(0, i * 43, 0));
            }
            rice.Flush();
            Vector3 cash = p + new Vector3(-.96f, .905f, .02f);
            TradeScenery.Tray(g, cash, .73f, .51f);
            for (int i = 0; i < tradeChangeCoins.Length; i++) {
                Vector3 pos = cash + new Vector3(-.255f + (i % 6) * .102f, .045f + (i / 18) * .018f, -.13f + ((i / 6) % 3) * .13f);
                var coin = TradeScenery.Group(tradeArtRoot, "找零实物钱 " + (i + 1), pos);
                var cg = new TradeScenery.Geometry(coin);
                cg.Cylinder(Vector3.zero, .041f, .014f, TradeScenery.Brass);
                cg.Box(new Vector3(0,.0075f,0), new Vector3(.013f,.002f,.013f), TradeScenery.Dark);
                cg.Flush(); tradeChangeCoins[i] = coin.gameObject;
            }
            // Numerals are written on small counter slips, separate from the money.
            tradeScaleReadout = g.Sign("已称 0 斤", p + new Vector3(.33f, 1.04f, .408f), .053f, TradeScenery.Ink.color, north);
            tradeCashReadout = g.Sign("找零 0", p + new Vector3(-.98f, 1.04f, .408f), .051f, TradeScenery.Ink.color, north);
            foreach (float x in new[] { .33f, -.98f }) g.Box(p + new Vector3(x, 1.04f, .396f), new Vector3(.75f,.21f,.013f), TradeScenery.Paper);
            // A ledger sits separately on the east end; credit never appears as coins.
            Vector3 book = p + new Vector3(1.20f, .945f, .02f);
            tradeLedgerArt = TradeScenery.Group(tradeArtRoot, "可拿起的赊账簿", book).gameObject;
            var ledger = new TradeScenery.Geometry(tradeLedgerArt.transform);
            ledger.Box(Vector3.zero, new Vector3(.38f,.035f,.58f), TradeScenery.Dark);
            ledger.Box(Vector3.up * .025f, new Vector3(.34f,.012f,.54f), TradeScenery.Paper);
            for (int row = 0; row < 5; row++) ledger.Box(new Vector3(0,.034f,-.19f + row * .08f), new Vector3(.26f,.004f,.004f), TradeScenery.Ink);
            ledger.Flush();
            tradeDebtReadout = g.Sign("赊欠 0", p + new Vector3(1.18f,1.08f,.39f), .041f, TradeScenery.Ink.color, north);
            g.Box(p + new Vector3(1.18f,1.08f,.38f), new Vector3(.49f,.19f,.013f), TradeScenery.Paper);
            g.Box(p + new Vector3(.28f,.17f,0), new Vector3(.8f,.3f,.48f), TradeScenery.Linen);
            g.Flush();
        }

        void BuildTradeCourier()
        {
            var g = new TradeScenery.Geometry(tradeArtRoot);
            // Door plates are attached to the original rooms; no road-centre desks,
            // new walls, or colliders interfere with the large public passage.
            TradeScenery.DoorPlate(g, new Vector3(-34.0f, 1.74f, -24.79f), 180, "南街三号", "阿福住处");
            TradeScenery.DoorPlate(g, new Vector3(28.05f, 1.74f, 24.77f), 0, "北巷七号", "街坊收批处");
            g.Sign("问路 · 茶棚", new Vector3(-12.5f, 1.13f, -25.36f), .049f, TradeScenery.Ink.color, Quaternion.Euler(0,180,0));
            Vector3 outgoing = new Vector3(-28.03f,1.055f,-14.04f);
            tradeOutgoingArt = TradeScenery.Letter(tradeArtRoot, outgoing, "待交批封", false);
            int recipient = TradeWork.CourierRecipient(state);
            for (int i = 0; i < 2; i++) {
                // The two doorways face opposite directions. Put each signature
                // stand to the visitor's left, clear of the person and doorway.
                Vector3 table = WorldFactory.CharacterGroundPosition(TradePlaces.CourierRecipient(i) +
                    new Vector3(i == 0 ? .75f : -.75f, 0, i == 0 ? .16f : -.16f));
                const float top = .76f;
                TradeScenery.SignatureTable(g, table, top);
                if (i == recipient) {
                    // Letter thickness is .026 m; its underside rests on the top.
                    Vector3 received = table + Vector3.up * (top + .013f);
                    tradeReceiptArt = TradeScenery.Letter(tradeArtRoot, received, "台上收讫回执", true);
                }
            }
            g.Flush();
        }
    }

    /// <summary>Native, metre-sized 3D work props. Small details are merged by
    /// material and every owned mesh is released when the task actors are rebuilt.</summary>
    static class TradeScenery
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material Wood => Surface("trade worn wood", "836143");
        public static Material Dark => Surface("trade dark timber", "4E3C2C");
        public static Material Paper => Surface("trade batch paper", "E6D9B9");
        public static Material Ink => Surface("trade charcoal ink", "41433C");
        public static Material Brass => Surface("trade old brass", "A68B52");
        public static Material Linen => Surface("trade rice sack linen", "B3A17C");
        public static Material Teal => Surface("trade indigo cloth", "587579");
        public static Material Red => Surface("trade vermilion seal", "985342");
        public static Material Rice => Surface("trade rice grains", "D7C8A2");
        static Material Surface(string name, string hex) {
            if (materials.TryGetValue(name, out var material) && material) return material;
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return materials[name] = WorldSurfaceMaterials.Create(name, color);
        }
        public static Transform Group(Transform root, string name, Vector3 p) {
            var group = new GameObject(name).transform; group.SetParent(root, false); group.localPosition = p; return group;
        }
        public static void Table(Geometry g, Vector3 p, float width, float depth, float height) {
            g.Solid("称货窄柜", p + Vector3.up * height * .5f, new Vector3(width,height,depth));
            for (int i = 0; i < 5; i++) g.Box(p + new Vector3(0,height-.028f,-depth*.5f + (i+.5f)*depth/5), new Vector3(width,.056f,depth/5-.005f), Wood);
            foreach (float x in new[] {-.5f,.5f}) foreach (float z in new[] {-.5f,.5f}) g.Box(p + new Vector3(x*(width-.2f),height*.45f,z*(depth-.16f)), new Vector3(.095f,height*.9f,.095f), Dark);
            g.Box(p + new Vector3(0,.24f,0), new Vector3(width-.12f,.07f,.07f), Dark);
        }
        public static void PickupSkid(Geometry g, Vector3 p) {
            for (int i = 0; i < 5; i++) g.Box(p + new Vector3(-.42f+i*.21f,.14f,0), new Vector3(.19f,.065f,.94f), Wood);
            foreach (float z in new[] {-.33f,.33f}) g.Box(p + new Vector3(0,.058f,z), new Vector3(.96f,.12f,.08f), Dark);
            // Low cargo only: a single collider never occupies the standing point.
            g.Solid("待运货托",p + Vector3.up*.26f,new Vector3(1.05f,.52f,1.00f));
        }
        public static void Pallet(Geometry g, Vector3 p, bool cart) {
            float top = cart ? .68f : .21f;
            g.Solid(cart ? "布匹平板车" : "分类木货栈", p + Vector3.up*(top*.5f), new Vector3(1.45f,top,1.70f));
            for (int i = 0; i < 7; i++) g.Box(p + new Vector3(-.63f+i*.21f,top-.026f,0), new Vector3(.2f,.052f,1.7f), Wood);
            foreach (float z in new[] {-.61f,.61f}) g.Box(p + new Vector3(0,top-.13f,z), new Vector3(1.5f,.18f,.1f), Dark);
            if (!cart) return;
            foreach (float x in new[] {-.72f,.72f}) {
                Quaternion wheel = Quaternion.Euler(0,0,90);
                g.Lathe(p + new Vector3(x,.35f,.15f), new[] { new Vector2(.25f,-.055f),new Vector2(.35f,-.055f),new Vector2(.35f,.055f),new Vector2(.25f,.055f) }, Dark, wheel);
                for (int i = 0; i < 6; i++) {
                    float a = i*Mathf.PI/3;
                    g.Beam(p+new Vector3(x,.35f,.15f),p+new Vector3(x,.35f+Mathf.Cos(a)*.29f,.15f+Mathf.Sin(a)*.29f),.03f,Wood);
                }
                g.Beam(p+new Vector3(x*.8f,.56f,-.6f),p+new Vector3(x*.8f,.76f,-1.18f),.055f,Wood);
            }
        }
        public static void RainRoof(Geometry g, Vector3 p) {
            foreach (float x in new[] {-.8f,.8f}) foreach (float z in new[] {-.93f,.93f}) {
                g.Beam(p+new Vector3(x,0,z),p+new Vector3(x,2.15f,z),.065f,Wood);
                g.Solid("纸货雨棚细柱",p+new Vector3(x,1.06f,z),new Vector3(.07f,2.12f,.07f));
            }
            for (int i = 0; i < 10; i++) {
                float z=-1.04f+i*.23f;
                g.Box(p+new Vector3(0,2.17f,z),new Vector3(2.04f,.047f,.225f),i%4==0?Teal:Linen,Quaternion.Euler(0,0,-5));
            }
            g.Box(p+new Vector3(.62f,.58f,.8f),new Vector3(.32f,.27f,.035f),Paper);
            g.Sign("怕 潮",p+new Vector3(.62f,.58f,.777f),.050f,Red.color);
        }
        public static GameObject Parcel(Transform root, Vector3 p, int kind, bool tag) {
            Transform piece=Group(root,"货包 · "+kind,p);var g=new Geometry(piece);
            if(kind==0) {
                g.Lathe(Vector3.zero,new[]{new Vector2(0,0),new Vector2(.30f,.02f),new Vector2(.38f,.20f),new Vector2(.31f,.62f),new Vector2(.12f,.72f),new Vector2(.08f,.78f),new Vector2(0,.80f)},Linen);
                g.Cylinder(new Vector3(0,.72f,0),.13f,.037f,Dark);
                for(int i=0;i<5;i++)g.Beam(new Vector3(-.035f+i*.015f,.74f,.03f),new Vector3(-.14f+i*.06f,.85f,.07f),.018f,Linen);
            } else {
                g.Box(new Vector3(0,.32f,0),new Vector3(.86f,.64f,.72f),kind==1?Teal:Paper);
                foreach(float x in new[]{-.25f,.25f}) {
                    g.Box(new Vector3(x,.325f,0),new Vector3(.032f,.66f,.745f),kind==1?Linen:Dark);
                    g.Box(new Vector3(x,.65f,0),new Vector3(.044f,.026f,.77f),Linen);
                }
                if(kind==1)for(int i=0;i<5;i++)g.Box(new Vector3(0,.09f+i*.108f,-.37f),new Vector3(.78f,.018f,.018f),Linen);
                else g.Box(new Vector3(0,.33f,-.374f),new Vector3(.19f,.18f,.012f),Red);
            }
            if(tag) {
                Quaternion east=Quaternion.Euler(0,-90,0);
                g.Box(new Vector3(.415f,.47f,0),new Vector3(.62f,.31f,.016f),Paper,east);
                g.Sign(kind==0?"米 · 粮栈":kind==1?"布 · 布车":"纸 · 怕潮",new Vector3(.426f,.47f,0),.052f,kind==2?Red.color:Ink.color,east);
            }
            g.Flush();return piece.gameObject;
        }
        public static Transform ScalePan(Transform root,string name,bool weight) {
            Transform pan=Group(root,name,Vector3.zero);var g=new Geometry(pan);
            g.Lathe(Vector3.zero,new[]{new Vector2(0,0),new Vector2(.16f,.016f),new Vector2(.22f,.068f),new Vector2(.21f,.077f),new Vector2(.155f,.036f),new Vector2(0,.026f)},Brass);
            foreach(float a in new[]{0f,120f,240f})g.Beam(new Vector3(Mathf.Cos(a*Mathf.Deg2Rad)*.2f,.067f,Mathf.Sin(a*Mathf.Deg2Rad)*.2f),new Vector3(0,.28f,0),.009f,Dark);
            if(weight) {g.Cylinder(new Vector3(0,.071f,0),.084f,.09f,Dark);g.Cylinder(new Vector3(0,.13f,0),.052f,.035f,Brass);}
            g.Flush();return pan;
        }
        public static void Tray(Geometry g,Vector3 p,float width,float depth) {
            g.Box(p,new Vector3(width,.042f,depth),Dark);
            foreach(float x in new[]{-1f,1f})g.Box(p+new Vector3(x*width*.5f,.052f,0),new Vector3(.035f,.09f,depth+.035f),Wood);
            foreach(float z in new[]{-1f,1f})g.Box(p+new Vector3(0,.052f,z*depth*.5f),new Vector3(width,.09f,.035f),Wood);
        }
        public static void SignatureTable(Geometry g, Vector3 p, float height) {
            // Small door-side writing stand, entirely decorative: interaction and
            // the existing walkable route do not acquire a new blocking collider.
            const float width = .72f, depth = .55f;
            for (int board = 0; board < 5; board++)
                g.Box(p + new Vector3(-width * .5f + (board + .5f) * width / 5, height - .025f, 0),
                    new Vector3(width / 5 - .003f, .05f, depth), Wood);
            foreach (float x in new[] { -.28f, .28f }) foreach (float z in new[] { -.20f, .20f })
                g.Box(p + new Vector3(x, (height - .05f) * .5f, z), new Vector3(.045f, height - .05f, .045f), Dark);
            g.Box(p + new Vector3(0, .24f, 0), new Vector3(.60f, .045f, .045f), Dark);
            // A brush and inkstone establish its purpose without another sign.
            g.Box(p + new Vector3(.255f, height + .012f, -.13f), new Vector3(.12f, .024f, .12f), Ink);
            g.Beam(p + new Vector3(-.25f, height + .012f, -.19f), p + new Vector3(-.25f, height + .012f, .14f), .012f, Wood);
        }
        public static void DoorPlate(Geometry g,Vector3 p,float yaw,string address,string detail) {
            Quaternion q=Quaternion.Euler(0,yaw,0);
            g.Box(p,new Vector3(1.43f,.65f,.07f),Dark,q);
            g.Box(p+q*new Vector3(0,0,-.043f),new Vector3(1.31f,.53f,.02f),Paper,q);
            g.Sign(address,p+q*new Vector3(0,.115f,-.058f),.083f,Ink.color,q);
            g.Sign(detail,p+q*new Vector3(0,-.13f,-.058f),.049f,Red.color,q);
        }
        public static GameObject Letter(Transform root,Vector3 p,string name,bool receipt) {
            Transform letter=Group(root,name,p);var g=new Geometry(letter);
            g.Box(Vector3.zero,new Vector3(.30f,.026f,.41f),Paper);
            g.Box(new Vector3(.09f,.018f,-.11f),new Vector3(.06f,.008f,.058f),Red);
            for(int i=0;i<3;i++)g.Box(new Vector3(-.085f+i*.07f,.018f,.03f),new Vector3(.006f,.006f,.19f),Ink);
            if(!receipt)g.Box(new Vector3(0,.026f,0),new Vector3(.019f,.012f,.43f),Linen);
            g.Flush();return letter.gameObject;
        }
        public sealed class Geometry
        {
            readonly Transform root;
            readonly Dictionary<Material,Part> parts=new Dictionary<Material,Part>();
            sealed class Part {public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<int> triangles=new List<int>();}
            public Geometry(Transform root){this.root=root;}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material material) {
                if(!parts.TryGetValue(material,out var part))parts[material]=part=new Part();
                int at=part.vertices.Count;part.vertices.Add(a);part.vertices.Add(b);part.vertices.Add(c);part.vertices.Add(d);
                part.triangles.Add(at);part.triangles.Add(at+1);part.triangles.Add(at+2);part.triangles.Add(at);part.triangles.Add(at+2);part.triangles.Add(at+3);
            }
            public void Box(Vector3 p,Vector3 size,Material material,Quaternion? rotation=null) {
                Quaternion q=rotation??Quaternion.identity;Vector3 h=size*.5f;
                Vector3 a=p+q*new Vector3(-h.x,-h.y,-h.z),b=p+q*new Vector3(h.x,-h.y,-h.z),c=p+q*new Vector3(h.x,h.y,-h.z),d=p+q*new Vector3(-h.x,h.y,-h.z);
                Vector3 e=p+q*new Vector3(-h.x,-h.y,h.z),f=p+q*new Vector3(h.x,-h.y,h.z),j=p+q*new Vector3(h.x,h.y,h.z),k=p+q*new Vector3(-h.x,h.y,h.z);
                Quad(a,d,c,b,material);Quad(e,f,j,k,material);Quad(a,e,k,d,material);Quad(b,c,j,f,material);Quad(d,k,j,c,material);Quad(a,b,f,e,material);
            }
            public void Solid(string name,Vector3 p,Vector3 size) {var go=Group(root,name+" collision",p);go.gameObject.AddComponent<BoxCollider>().size=size;}
            public void Beam(Vector3 a,Vector3 b,float width,Material material) {Box((a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),material,Quaternion.FromToRotation(Vector3.up,(b-a).normalized));}
            public void Cylinder(Vector3 p,float radius,float height,Material material) {Lathe(p,new[]{new Vector2(0,-height*.5f),new Vector2(radius,-height*.5f),new Vector2(radius,height*.5f),new Vector2(0,height*.5f)},material);}
            public void Lathe(Vector3 p,Vector2[] profile,Material material,Quaternion? rotation=null) {
                Quaternion q=rotation??Quaternion.identity;const int n=24;
                for(int i=0;i<n;i++) {float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;Vector3 da=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),db=new Vector3(Mathf.Sin(b),0,Mathf.Cos(b));
                    for(int j=0;j<profile.Length-1;j++) {Vector2 lo=profile[j],hi=profile[j+1];Quad(p+q*(da*lo.x+Vector3.up*lo.y),p+q*(db*lo.x+Vector3.up*lo.y),p+q*(db*hi.x+Vector3.up*hi.y),p+q*(da*hi.x+Vector3.up*hi.y),material);}}
            }
            public TextMesh Sign(string text,Vector3 p,float size,Color color,Quaternion? rotation=null) {
                var font=Resources.Load<Font>("Fonts/Title");if(!font)return null;
                font.RequestCharactersInTexture(text+"0123456789.斤赊欠找零已称",48,FontStyle.Normal);
                var go=Group(root,"实物题签 · "+text,p);go.localRotation=rotation??Quaternion.identity;
                var label=go.gameObject.AddComponent<TextMesh>();label.font=font;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.text=text;label.color=color;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return label;
            }
            public void Flush() {
                var lifetime=root.gameObject.GetComponent<DestinationMeshLifetime>()??root.gameObject.AddComponent<DestinationMeshLifetime>();
                foreach(var entry in parts) {
                    var mesh=new Mesh{name="工作实物 · "+entry.Key.name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(entry.Value.vertices);mesh.SetTriangles(entry.Value.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();lifetime.meshes.Add(mesh);
                    var go=Group(root,mesh.name,Vector3.zero);go.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=entry.Key;
                }
                parts.Clear();
            }
        }
    }
}
