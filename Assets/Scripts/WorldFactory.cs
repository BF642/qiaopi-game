using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>Hand-built, fully three dimensional places along the remittance route.</summary>
    public static partial class WorldFactory
    {
        static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        static Transform world;
        static Mesh leafMesh;
        static System.Random random;
        static Material Brick => Mat("brick", "9D4C36");
        static Material Cream => Mat("plaster", "D4C4A5");
        static Material Stone => Mat("stone", "ACAA98");
        static Material Dark => Mat("deep timber", "55402E");
        static Material Timber => Mat("timber", "826040");
        static Material Teal => Mat("teal", "4E665A");
        static Material Roof => Mat("roof tiles", "414541");
        static Material Red => Mat("lantern", "A3402D");
        static Material Gold => Mat("brass", "B29155");

        public static GameObject Build(string kind)
        {
            var root = new GameObject("Place · " + kind);
            world = root.transform;
            pavingLayer = 0;
            random = new System.Random(kind == "harbor" ? 27 : kind == "singapore" ? 68 : 12);
            switch(kind){
                case "quanzhou":ExpandedQuanzhou();break;
                case "harbor":ExpandedHarbor();break;
                case "ship":ExpandedShip();break;
                case "port":ExpandedPort();break;
                case "market":ExpandedMarket();break;
                case "quarters":ExpandedQuarters();break;
                default:ExpandedPostOffice();break;
            }
            if(kind=="harbor"||kind=="port")HarborSurroundings(kind);
            else if(kind!="ship")SettlementSurroundings(kind);
            AddSpatialDepth(kind);
            if(Application.isPlaying)BatchScenery(root);
            if(kind=="harbor"||kind=="port"||kind=="ship")SeaEnvironment.Create(root,kind);
            if (Application.isPlaying) WorldAtmosphere.Create(root, kind);
            return root;
        }

        static Mesh sceneryCube;
        static void BatchScenery(GameObject root)
        {
            var groups=new Dictionary<string,List<MeshFilter>>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
                var r=f.GetComponent<MeshRenderer>();
                if(!r||!r.enabled||!f.sharedMesh||!f.sharedMesh.isReadable||r.sharedMaterials.Length!=1)continue;
                bool animated=false;
                for(Transform t=f.transform;t&&t!=root.transform;t=t.parent)
                    if(t.name.Contains("sway pivot")){animated=true;break;}
                if(animated||r.bounds.size.magnitude>45)continue;
                Vector3 p=r.bounds.center;
                string key=r.sharedMaterial.GetEntityId()+"/"+Mathf.FloorToInt(p.x/24)+"/"+Mathf.FloorToInt(p.z/24);
                if(!groups.TryGetValue(key,out var list)){list=new List<MeshFilter>();groups[key]=list;}
                list.Add(f);
            }
            int reduced=0;
            foreach(var group in groups.Values){
                if(group.Count<3)continue;
                var parts=new CombineInstance[group.Count];
                for(int i=0;i<group.Count;i++)parts[i]=new CombineInstance{mesh=group[i].sharedMesh,transform=root.transform.worldToLocalMatrix*group[i].transform.localToWorldMatrix};
                var mesh=new Mesh{name="Combined static scenery",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                mesh.CombineMeshes(parts,true,true);mesh.RecalculateBounds();
                var go=new GameObject("Combined scenery · "+group.Count+" pieces");go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=group[0].GetComponent<MeshRenderer>().sharedMaterial;
                foreach(var f in group)f.GetComponent<MeshRenderer>().enabled=false;
                reduced+=group.Count-1;
            }
            Debug.Log("SCENERY_BATCHED "+root.name+" draw objects reduced="+reduced);
        }

        static Material Mat(string name, string html)
        {
            if (palette.TryGetValue(name, out var material) && material) return material;
            ColorUtility.TryParseHtmlString("#" + html, out Color c);
            material = WorldSurfaceMaterials.Create(name,c);
            palette[name] = material;
            return material;
        }
        static GameObject Shape(string name, PrimitiveType primitive, Vector3 p, Vector3 size, Material mat, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(primitive);
            if(primitive==PrimitiveType.Cube){
                if(!sceneryCube){var cv=new List<Vector3>();var ct=new List<int>();DetailBox(cv,ct,Vector3.zero,Vector3.one);sceneryCube=FlatMesh(cv,ct);sceneryCube.name="Readable scenery cube";}
                go.GetComponent<MeshFilter>().sharedMesh=sceneryCube;
            }
            go.name = name; go.transform.SetParent(world, false); go.transform.localPosition = p; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var collider = go.GetComponent<Collider>();
            if (!solid && collider) { collider.enabled = false; if(Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider); }
            return go;
        }
        static GameObject Box(string n, Vector3 p, Vector3 s, Material m, bool solid = false) => Shape(n, PrimitiveType.Cube, p, s, m, solid);
        static GameObject Cylinder(string n, Vector3 p, float r, float h, Material m, bool solid = false) => Shape(n, PrimitiveType.Cylinder, p, new Vector3(r * 2, h / 2, r * 2), m, solid);
        static GameObject MeshObject(string n, Mesh mesh, Vector3 p, Material m)
        {
            var go = new GameObject(n); go.transform.SetParent(world, false); go.transform.localPosition = p;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }
        static void Beam(string n, Vector3 a, Vector3 b, float width, Material m, bool round = false)
        {
            var go = round ? Cylinder(n, (a + b) * .5f, width * .5f, Vector3.Distance(a, b), m) : Box(n, (a + b) * .5f, new Vector3(width, Vector3.Distance(a,b), width), m);
            go.transform.up = b - a;
        }
        static Mesh FlatMesh(List<Vector3> vertices, List<int> triangles)
        {
            var m = new Mesh(); m.SetVertices(vertices); m.SetTriangles(triangles, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.Add(i); t.Add(i+1); t.Add(i+2); t.Add(i); t.Add(i+2); t.Add(i+3);
        }
        static float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);

        static void Base(Material surface)
        {
            Box("Cut stone diorama foundation", new Vector3(0,-.67f,2), new Vector3(32,1.1f,36), Mat("foundation", "8C8778"));
            Box("Walkable ground", new Vector3(0,-.12f,2), new Vector3(31.8f,.24f,35.8f), surface, true);
            Box("Foundation lower reveal", new Vector3(0,-1.16f,2), new Vector3(31.4f,.12f,35.4f), Dark);
        }
        static void Path(float x, float z, float width, float depth)
        {
            GranitePaving(x,z,width,depth);
        }
        static void Quanzhou()
        {
            Base(Mat("courtyard sand", "9E8A69"));
            Path(0,1.5f,6.2f,31); Path(0,4,25,3.1f); Path(0,-5,24,2.8f); Path(0,12,23,2.8f);
            House(-12.1f,5.5f,4.5f,7.4f,4.0f,Brick,false);
            House(12.1f,6.8f,4.5f,7.0f,4.4f,Mat("rose brick", "A4583F"),false);
            House(0,18.4f,12.2f,5.0f,4.5f,Brick,true);
            House(14.0f,-8.8f,3.5f,4.1f,2.7f,Cream,false);
            Box("Low courtyard wall left",new Vector3(-14.6f,.55f,-2),new Vector3(.45f,1.1f,7),Brick,true);
            Box("Low courtyard wall right",new Vector3(14.6f,.55f,-.7f),new Vector3(.45f,1.1f,5.4f),Brick,true);
            Tree(new Vector3(-13.8f,0,-7.7f),.83f);
            Tree(new Vector3(13.2f,0,15.8f),.65f);
            Planter(-6.6f,15.0f,2.3f,1.5f); Planter(6.6f,15.0f,2.3f,1.5f);
            Clothesline(new Vector3(-13.6f,0,11.5f),new Vector3(-7.2f,0,11.5f));
            Pot(new Vector3(-9.5f,0,-1.6f),.60f); Pot(new Vector3(-10.4f,0,-2),.43f);
            Pot(new Vector3(9.7f,0,1.6f),.55f); Pot(new Vector3(8.9f,0,2),.34f);
            Bench(new Vector3(-8.3f,0,-10.5f));
            CourtyardGate(new Vector3(-12.0f,0,15.0f));
            Lantern(new Vector3(-4.6f,3.6f,15.6f)); Lantern(new Vector3(4.6f,3.6f,15.6f));
            for(int i=0;i<8;i++) Shrub(new Vector3(i<4?-14.4f:14.4f,.4f,-12.6f+(i%4)*1.7f),.65f);
            Box("Family courtyard stone table",new Vector3(-11.7f,.87f,-3.5f),new Vector3(1.8f,.17f,1.0f),Stone);
            Cylinder("Table pedestal",new Vector3(-11.7f,.4f,-3.5f),.25f,.8f,Stone);
            Cylinder("Tea bowl",new Vector3(-11.45f,1.02f,-3.5f),.12f,.12f,Cream);
        }
        static void House(float x,float z,float w,float d,float h,Material wall,bool swallow)
        {
            Box("Raised granite footing",new Vector3(x,.2f,z),new Vector3(w+.18f,.4f,d+.16f),Stone,true);
            Box(swallow?"Village public hall masonry shell":"Ordinary family dwelling masonry shell",new Vector3(x,h*.5f+.35f,z),new Vector3(w,h,d),wall,true);
            Box("Lime mortar eave bedding",new Vector3(x,h+.31f,z),new Vector3(w+.15f,.16f,d+.15f),Mat("old lime mortar","BEB29A"));
            RoofMesh(x,h+.35f,z,w+.95f,d+1.05f,swallow);
            HouseAge(x,z,w,d,h,swallow);
            float front=z-d*.5f-.025f;
            Box("Door stone surround",new Vector3(x,1.38f,front),new Vector3(1.48f,2.40f,.14f),Stone);
            Box("Recessed timber double door",new Vector3(x,1.35f,front-.09f),new Vector3(1.20f,2.18f,.07f),Shadow);
            PlankFace("Aged hand-planed double door",new Vector3(x,1.35f,front-.14f),1.16f,2.14f,Dark,8);
            Box("Door central seam",new Vector3(x,1.35f,front-.14f),new Vector3(.027f,2.12f,.04f),Timber);
            CylinderHandle(new Vector3(x-.20f,1.22f,front-.2f)); CylinderHandle(new Vector3(x+.20f,1.22f,front-.2f));
            Box("Doorstep",new Vector3(x,.11f,front-.4f),new Vector3(2.1f,.22f,.65f),Stone);
            foreach(float side in new[]{-1f,1f})
            {
                float wx=x+side*w*.31f;
                Window(wx,1.95f,front-.035f,Mathf.Min(1.40f,w*.23f),1.22f);
                Box("Corner stone binding",new Vector3(x+side*(w*.5f-.10f),h*.5f+.32f,front-.02f),new Vector3(.19f,h,.10f),Mat("corner granite","ADA892"));
            }
            Brickwork(x,front-.085f,w,h);
            if(swallow)
            {
                Box("Door lintel plaque",new Vector3(x,3.28f,front-.15f),new Vector3(2.55f,.48f,.12f),Dark);
                SignText("榕溪公所",new Vector3(x,3.28f,front-.225f),.095f);
                foreach(float side in new[]{-1f,1f}) Box("Main hall stone bay divider",new Vector3(x+side*w*.20f,2.05f,front-.015f),new Vector3(.14f,3.3f,.09f),Stone);
            }
        }
        static void CylinderHandle(Vector3 p)
        {
            var go=Cylinder("Brass door handle",p,.065f,.04f,Gold);go.transform.rotation=Quaternion.Euler(90,0,0);
        }
        static void Window(float x,float y,float z,float w,float h)
        {
            WoodenLatticeWindow(x,y,z,w,h,true);
        }
        static void RoofMesh(float x,float y,float z,float w,float d,bool swallow)
        {
            float rise=Mathf.Min(1.65f,d*.25f);
            SolidRoofConstruction(x,y,z,w,d,rise,swallow);
            var v=new List<Vector3>();var t=new List<int>();
            var detail=new List<Vector3>();var dt=new List<int>();
            int strips=Mathf.Max(8,Mathf.RoundToInt(w/.24f));
            for(int i=0;i<strips;i++)
            {
                float a=-w*.5f+w*i/strips,b=-w*.5f+w*(i+1)/strips;
                float liftA=swallow?Mathf.Pow(Mathf.Abs(a)/(w*.5f),8)*.43f:0;
                float liftB=swallow?Mathf.Pow(Mathf.Abs(b)/(w*.5f),8)*.43f:0;
                Quad(v,t,new Vector3(a,liftA,-d*.5f),new Vector3(a,rise+liftA,0),new Vector3(b,rise+liftB,0),new Vector3(b,liftB,-d*.5f));
                Quad(v,t,new Vector3(b,liftB,d*.5f),new Vector3(b,rise+liftB,0),new Vector3(a,rise+liftA,0),new Vector3(a,liftA,d*.5f));
                // Ceramic seams, segmented eave ends, and overlap lines share one mesh.
                foreach(float side in new[]{-1f,1f})
                {
                    Vector3 eave=new Vector3(a,liftA+.035f,side*d*.5f);
                    Vector3 ridge=new Vector3(a,rise+liftA+.025f,0);
                    AppendTube(detail,dt,new[]{eave,ridge},.045f,.036f);
                    AppendTube(detail,dt,new[]{eave+new Vector3(.08f,-.015f,side*.055f),eave+new Vector3(.08f,-.015f,side*.23f)},.088f,.073f);
                }
            }
            int tileRows=Mathf.Max(4,Mathf.CeilToInt(d*.5f/.32f));
            for(int row=1;row<tileRows;row++)foreach(float side in new[]{-1f,1f})
            {
                float ratio=(float)row/tileRows;
                AppendTube(detail,dt,new[]{new Vector3(-w*.46f,rise*ratio+.027f,side*d*.5f*(1-ratio)),new Vector3(w*.46f,rise*ratio+.027f,side*d*.5f*(1-ratio))},.018f,.018f);
            }
            MeshObject("Sloping tiled roof",FlatMesh(v,t),new Vector3(x,y,z),Roof);
            MeshObject("Batched ceramic tile ridges and eaves",FlatMesh(detail,dt),new Vector3(x,y,z),Mat("tile highlight","595B51"));
            var ridgeV=new List<Vector3>();var ridgeT=new List<int>();
            AppendTube(ridgeV,ridgeT,new[]{new Vector3(-w*.42f,rise+.13f,0),new Vector3(w*.42f,rise+.13f,0)},.16f,.16f);
            if(swallow)
            {
                foreach(float side in new[]{-1f,1f})
                {
                    // Each broad curved stem divides into two tapering tips in depth.
                    var stem=new Vector3[9];
                    for(int j=0;j<stem.Length;j++){float u=j/8f;stem[j]=new Vector3(side*(w*.36f+.94f*u),rise+.12f+.36f*u*u,0);}
                    AppendTube(ridgeV,ridgeT,stem,.22f,.14f);
                    foreach(float fork in new[]{-1f,1f})
                    {
                        var curve=new Vector3[11];
                        for(int j=0;j<curve.Length;j++)
                        {
                            float u=j/10f;
                            curve[j]=stem[8]+new Vector3(side*(.16f*u+.43f*u*u),.18f*u+.86f*u*u,fork*(.09f*u+.53f*u*u));
                        }
                        AppendTube(ridgeV,ridgeT,curve,.145f,.015f);
                    }
                }
            }
            MeshObject(swallow?"Curved forked swallowtail ridge":"Ceramic ridge cap",FlatMesh(ridgeV,ridgeT),new Vector3(x,y,z),Roof);
            if(swallow)
            {
                var trim=new List<Vector3>();var tt=new List<int>();
                foreach(float side in new[]{-1f,1f})foreach(float fork in new[]{-1f,1f})
                {
                    var curve=new Vector3[13];
                    for(int j=0;j<curve.Length;j++)
                    {
                        float u=j/12f;
                        if(u<.45f){float a=u/.45f;curve[j]=new Vector3(side*(w*.36f+.94f*a),rise+.075f+.36f*a*a,-.155f);}
                        else {float a=(u-.45f)/.55f;curve[j]=new Vector3(side*(w*.36f+.94f+.16f*a+.43f*a*a),rise+.435f+.18f*a+.86f*a*a,fork*(.09f*a+.53f*a*a)-.08f);}
                    }
                    AppendTube(trim,tt,curve,.032f,.010f);
                }
                MeshObject("Lime plaster swallowtail inlay",FlatMesh(trim,tt),new Vector3(x,y,z),Cream);
            }
        }
        static void AppendTube(List<Vector3> v,List<int> t,Vector3[] points,float r0,float r1)
        {
            const int sides=6;
            for(int i=0;i<points.Length-1;i++)
            {
                Vector3 tangent=(points[i+1]-points[i]).normalized;
                Vector3 axis=Vector3.Cross(tangent,Mathf.Abs(Vector3.Dot(tangent,Vector3.forward))>.95f?Vector3.up:Vector3.forward).normalized;
                Vector3 other=Vector3.Cross(tangent,axis).normalized;
                float ra=Mathf.Lerp(r0,r1,(float)i/(points.Length-1));
                float rb=Mathf.Lerp(r0,r1,(float)(i+1)/(points.Length-1));
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;
                    Vector3 aa=axis*Mathf.Cos(a)+other*Mathf.Sin(a),bb=axis*Mathf.Cos(b)+other*Mathf.Sin(b);
                    Quad(v,t,points[i]+aa*ra,points[i]+bb*ra,points[i+1]+bb*rb,points[i+1]+aa*rb);
                }
            }
        }
        static void Brickwork(float x,float front,float w,float h,bool warehouse=false)
        {
            var bricks=new List<Vector3>();var bt=new List<int>();
            var stones=new List<Vector3>();var st=new List<int>();
            int rows=Mathf.FloorToInt((h-.30f)/.115f),cols=Mathf.FloorToInt((w-.20f)/.29f);
            for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
            {
                float bx=-w*.5f+.12f+col*.29f+(row%2)*.145f,by=.40f+row*.115f;
                if(bx>w*.5f-.28f)continue;
                if(warehouse&&Mathf.Abs(bx)<w*.43f&&by<3.04f)continue;
                if(!warehouse&&Mathf.Abs(bx)<.86f&&by<2.64f)continue;
                if(!warehouse&&by>1.22f&&by<2.74f&&Mathf.Abs(Mathf.Abs(bx)-w*.31f)<.87f)continue;
                bool granite=row<2 || (row*5+col*3)%19<4;
                if(!granite&&(row+col)%6==0)continue;
                var vv=granite?stones:bricks;var tr=granite?st:bt;
                float bw=.269f,cut=granite?.012f:.004f;
                Quad(vv,tr,new Vector3(bx+cut,by,0),new Vector3(bx,by+.09f-cut,0),new Vector3(bx+bw-cut,by+.095f,0),new Vector3(bx+bw,by+.005f,0));
            }
            DetailMesh("Irregular hand-fired redbrick face",bricks,bt,new Vector3(x,0,front),Mat("brick patch","BB6949"));
            DetailMesh("Granite rubble interwoven with red brick",stones,st,new Vector3(x,0,front-.007f),Stone);
        }
        static void CourtyardGate(Vector3 p)
        {
            foreach(float side in new[]{-1f,1f})
            {
                Box("Redbrick courtyard gate pier",p+new Vector3(side*1.65f,1.28f,0),new Vector3(.55f,2.56f,.65f),Brick,true);
                Box("Granite gate pier footing",p+new Vector3(side*1.65f,.25f,0),new Vector3(.69f,.5f,.79f),Stone);
                Box("Courtyard wall return",p+new Vector3(side*2.36f,.72f,0),new Vector3(.9f,1.44f,.36f),Brick,true);
                Box("Courtyard wall coping",p+new Vector3(side*2.36f,1.46f,0),new Vector3(1.02f,.16f,.54f),Roof);
            }
            Box("Courtyard gate stone lintel",p+Vector3.up*2.58f,new Vector3(3.95f,.25f,.7f),Cream);
            RoofMesh(p.x,p.y+2.75f,p.z,4.5f,1.45f,false);
            // A 2.75 metre unobstructed opening leads into the side courtyard.
        }
        static void SignText(string label,Vector3 p,float characterSize)
        {
            var font=Resources.Load<Font>("Fonts/Title");if(!font)return;
            font.RequestCharactersInTexture(label,48,FontStyle.Normal);
            var go=new GameObject("Carved sign · "+label);go.transform.SetParent(world,false);go.transform.localPosition=p;
            var text=go.AddComponent<TextMesh>();text.font=font;text.text=label;text.fontSize=48;text.characterSize=characterSize;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Gold.color;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Bench(Vector3 p)
        {
            Box("Wooden courtyard bench",p+Vector3.up*.405f,new Vector3(1.65f,.09f,.38f),Timber);
            foreach(float x in new[]{-.58f,.58f}) Box("Bench foot",p+new Vector3(x,.20f,0),new Vector3(.11f,.40f,.33f),Dark);
        }
        static void Pot(Vector3 p,float radius)
        {
            Cylinder("Terracotta storage jar",p+Vector3.up*(radius*.7f),radius*.77f,radius*1.4f,Mat("terracotta","99573D"));
            Cylinder("Jar shoulder",p+Vector3.up*(radius*1.37f),radius*.61f,radius*.17f,Mat("terracotta rim","AD6A47"));
            Cylinder("Dark open jar mouth",p+Vector3.up*(radius*1.465f),radius*.44f,.02f,Dark);
        }
        static void Lantern(Vector3 p)
        {
            Transform parent=world;
            var pivot=new GameObject("Lantern sway pivot");pivot.transform.SetParent(world,false);pivot.transform.localPosition=p+Vector3.up*.9f;
            world=pivot.transform;p=new Vector3(0,-.9f,0);
            Beam("Lantern suspension",p+Vector3.up*.9f,p+Vector3.up*.42f,.035f,Dark);
            Shape("Red paper lantern",PrimitiveType.Sphere,p,new Vector3(.66f,.9f,.66f),Red);
            Cylinder("Lantern cap",p+Vector3.up*.4f,.22f,.08f,Gold);
            Cylinder("Lantern tassel",p-Vector3.up*.59f,.035f,.3f,Gold);
            world=parent;
        }
        static void Planter(float x,float z,float w,float d)
        {
            // Domestic jars and loose plants, rather than contemporary raised landscaping.
            StoneJar(new Vector3(x-w*.22f,0,z),.53f,.79f,Mat("old clay jar","8E5740"));
            Shrub(new Vector3(x-w*.22f,1.0f,z),.78f);
            StoneJar(new Vector3(x+w*.30f,0,z+.20f),.36f,.54f,Mat("weathered stone basin","999583"));
            Shrub(new Vector3(x+w*.30f,.72f,z+.20f),.48f);
        }
        static void Shrub(Vector3 p,float s) { Blob("Leafy shrub",p,new Vector3(s*1.6f,s,s*1.3f),Mat("leaf green","648954")); }
        static Mesh Icosahedron()
        {
            if(leafMesh) return leafMesh;
            float g=(1+Mathf.Sqrt(5))*.5f;
            Vector3[] p={new Vector3(-1,g,0),new Vector3(1,g,0),new Vector3(-1,-g,0),new Vector3(1,-g,0),new Vector3(0,-1,g),new Vector3(0,1,g),new Vector3(0,-1,-g),new Vector3(0,1,-g),new Vector3(g,0,-1),new Vector3(g,0,1),new Vector3(-g,0,-1),new Vector3(-g,0,1)};
            int[] f={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            var v=new List<Vector3>();var t=new List<int>();for(int i=0;i<f.Length;i++){v.Add(p[f[i]].normalized*.5f);t.Add(i);}leafMesh=FlatMesh(v,t);return leafMesh;
        }
        static GameObject Blob(string name,Vector3 p,Vector3 scale,Material m)
        {
            var go=MeshObject(name,Icosahedron(),p,m);go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(Rand(0,35),Rand(0,360),Rand(0,25));return go;
        }
        static void Tree(Vector3 p,float s)
        {
            Cylinder("Old banyan trunk",p+Vector3.up*2.25f*s,.48f*s,4.5f*s,Timber,true);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*.4f;Vector3 branch=p+new Vector3(Mathf.Cos(a)*2.0f,4.8f,Mathf.Sin(a)*1.5f)*s;
                Beam("Banyan spreading branch",p+Vector3.up*2.9f*s,branch,.35f*s,Timber,true);
                Blob("Faceted banyan canopy",branch+Vector3.up*.75f*s,new Vector3(4.1f,3.0f,3.8f)*s,i%2==0?Mat("canopy shade","497253"):Mat("canopy sun","779154"));
                if(i%2==0) Beam("Hanging banyan root",branch,branch-Vector3.up*2.3f*s,.04f*s,Timber);
            }
            Blob("Canopy crown",p+Vector3.up*6.4f*s,new Vector3(4.5f,3,4)*s,Mat("canopy crown","88A15D"));
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Beam("Banyan root buttress",p+Vector3.up*.75f*s,p+new Vector3(Mathf.Cos(a)*1.05f,.04f,Mathf.Sin(a)*1.05f)*s,.23f*s,Timber);}
        }
        static void Clothesline(Vector3 a,Vector3 b)
        {
            Cylinder("Bamboo laundry pole",a+Vector3.up*1.5f,.055f,3,Timber);
            Cylinder("Bamboo laundry pole",b+Vector3.up*1.5f,.055f,3,Timber);
            Beam("Laundry cord",a+Vector3.up*2.8f,b+Vector3.up*2.8f,.025f,Dark);
            for(int i=0;i<4;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,.18f+i*.21f)+Vector3.up*2.08f;
                var cloth=Box("Hanging household linen",p,new Vector3(.86f,1.35f,.055f),i%2==0?Cream:Teal);
                var pivot=new GameObject("Laundry sway pivot");pivot.transform.SetParent(world,false);pivot.transform.localPosition=p+Vector3.up*.675f;
                cloth.transform.SetParent(pivot.transform,true);pivot.transform.localRotation=Quaternion.Euler(0,0,i%2==0?-5:4);
            }
        }
        static void Crate(Vector3 p,float s=1)
        {
            Box("Wooden shipping crate",p+Vector3.up*s*.5f,new Vector3(s,s,s),Timber);
            Box("Crate front brace",p+new Vector3(0,s*.5f,-s*.505f),new Vector3(s*1.02f,.11f*s,.055f),Dark).transform.rotation=Quaternion.Euler(0,0,40);
            foreach(float x in new[]{-.38f,.38f}) Box("Crate iron strap",p+new Vector3(x*s,s*.505f,0),new Vector3(.055f,s*1.02f,s*1.025f),Dark);
        }
        static void Harbor()
        {
            Water();
            Box("Harbor cut-stone base",new Vector3(0,-.75f,-4),new Vector3(32,1.35f,24),Mat("harbor stone","868779"));
            Box("Walkable quay",new Vector3(0,-.12f,-4),new Vector3(32,.24f,24),Mat("quay sandstone","B1AA97"),true);
            Path(0,-2,6,25); Path(0,4,28,2.5f);
            GraniteQuayFacing();
            Box("Dock foundation",new Vector3(0,-.32f,12.5f),new Vector3(13,.62f,9.2f),Dark);
            Box("Walkable timber pier",new Vector3(0,-.08f,12.5f),new Vector3(13,.16f,9.2f),Timber,true);
            for(int i=0;i<18;i++) Box("Pier plank seam",new Vector3(0,.007f,8.2f+i*.5f),new Vector3(12.9f,.014f,.033f),Dark);
            foreach(float x in new[]{-6.4f,6.4f}) for(int i=0;i<4;i++)
            {
                float z=8.5f+i*2.6f;Cylinder("Timber mooring piling",new Vector3(x,.38f,z),.18f,2.15f,Dark);
                Cylinder("Rope winding",new Vector3(x,.93f,z),.22f,.17f,Mat("rope","AD9B72"));
                if(i<3) SaggingRope(new Vector3(x,1.05f,z),new Vector3(x,1.05f,z+2.6f));
            }
            Warehouse(-12.2f,1.8f,4.7f,8.2f,3.6f);
            Warehouse(12.2f,2.2f,4.7f,8.2f,3.6f);
            for(int i=0;i<6;i++) Crate(new Vector3(i<3?-12.5f:11.5f,0,-6.5f-(i%3)*1.45f),1.1f);
            Crate(new Vector3(-12.5f,1.1f,-6.5f),.83f); Crate(new Vector3(12.7f,0,7.1f),1.2f);
            Barrel(new Vector3(-8.7f,0,7.1f)); Barrel(new Vector3(-9.8f,0,7.2f));
            CargoCart(new Vector3(9.6f,0,-9.8f));
            Ship(new Vector3(0,0,24));
            var plank=Box("Boarding gangplank",new Vector3(0,.6f,17.6f),new Vector3(2.2f,.18f,3.6f),Mat("gangplank","95734D"));plank.transform.rotation=Quaternion.Euler(-20,0,0);
            for(int i=0;i<4;i++)
            {
                Vector3 p=new Vector3(-28+i*18,-.1f,43+Rand(-4,4));
                Blob("Distant island",p,new Vector3(18,Rand(4,7),12),Mat("distant land","709A87"));
            }
            Lantern(new Vector3(-10.0f,3,-2.6f)); Lantern(new Vector3(10.0f,3,-2.2f));
        }
        static void Water()
        {
            const int nx=28,nz=36;var v=new List<Vector3>();var t=new List<int>();
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)v.Add(new Vector3(-48+x*96f/nx,-.6f,-20+z*90f/nz));
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int i=z*(nx+1)+x;t.Add(i);t.Add(i+nx+1);t.Add(i+1);t.Add(i+1);t.Add(i+nx+1);t.Add(i+nx+2);}
            var mesh=FlatMesh(v,t);mesh.MarkDynamic();
            var water=MeshObject("Muted harbor water",mesh,Vector3.zero,Mat("water","5E8582"));
            water.AddComponent<WaterMotion>().Initialize(mesh);
            for(int i=0;i<20;i++)
            {
                float x=(i%2==0?-1:1)*Rand(17,35),z=Rand(-10,39);
                Box("Sun glint on water",new Vector3(x,-.53f,z),new Vector3(Rand(1.0f,3.2f),.015f,.10f),Mat("water glint","90ADA3"));
            }
        }
        static void Barrel(Vector3 p)
        {
            Cylinder("Cargo barrel",p+Vector3.up*.64f,.48f,1.28f,Timber);
            foreach(float y in new[]{.2f,1.0f}) Cylinder("Barrel iron hoop",p+Vector3.up*y,.495f,.10f,Dark);
            Cylinder("Barrel lid",p+Vector3.up*1.29f,.46f,.025f,Mat("barrel lid","9C7A52"));
        }
        static void CargoCart(Vector3 p)
        {
            Box("Handcart bed",p+Vector3.up*.65f,new Vector3(1.6f,.15f,2.2f),Timber);
            foreach(float x in new[]{-.9f,.9f})
            {
                var wheel=Cylinder("Cart wheel",p+new Vector3(x,.48f,.2f),.48f,.15f,Dark);wheel.transform.rotation=Quaternion.Euler(0,0,90);
                Beam("Cart handle",p+new Vector3(x*.6f,.7f,-.8f),p+new Vector3(x*.6f,1.15f,-2.5f),.1f,Timber);
            }
            Crate(p+new Vector3(0,.76f,.35f),1.1f);
        }
        static void Ship(Vector3 p)
        {
            Transform parent=world;var boat=new GameObject("Harbor boat sway pivot");boat.transform.SetParent(world,false);boat.transform.localPosition=p;
            world=boat.transform;p=Vector3.zero;
            var v=new List<Vector3>();var t=new List<int>();
            Vector3[] rim={new Vector3(-3.8f,1.35f,-5.8f),new Vector3(3.8f,1.35f,-5.8f),new Vector3(4.3f,1.65f,3.7f),new Vector3(0,2.05f,7.4f),new Vector3(-4.3f,1.65f,3.7f)};
            Vector3[] bottom={new Vector3(-2.8f,-.4f,-5.1f),new Vector3(2.8f,-.4f,-5.1f),new Vector3(3.0f,-.4f,3.1f),new Vector3(0,-.2f,6.4f),new Vector3(-3.0f,-.4f,3.1f)};
            for(int i=0;i<5;i++){int j=(i+1)%5;Quad(v,t,rim[i],bottom[i],bottom[j],rim[j]);}
            MeshObject("Wooden junk boat hull",FlatMesh(v,t),p,Mat("ship hull","604633"));
            Box("Ship main deck",p+new Vector3(0,1.3f,-.8f),new Vector3(7.5f,.18f,10),Timber);
            foreach(float x in new[]{-3.65f,3.65f})
            {
                Beam("Ship gunwale",p+new Vector3(x,1.85f,-5.6f),p+new Vector3(x,2.15f,4.3f),.21f,Red);
                for(int i=0;i<5;i++) Beam("Ship railing upright",p+new Vector3(x,1.4f,-4.9f+i*2),p+new Vector3(x,2.0f,-4.9f+i*2),.10f,Dark);
            }
            Box("Stern cabin",p+new Vector3(0,2.4f,-3.6f),new Vector3(4.5f,2.1f,3.2f),Dark);
            PlankFace("Stern cabin rough timber face",p+new Vector3(0,2.4f,-5.22f),4.45f,2.05f,Timber,16);
            ArchedBoatCanopy(p+new Vector3(0,3.48f,-3.6f),5,3.7f);
            WoodenLatticeWindow(p.x,p.y+2.5f,p.z-5.27f,2.3f,1,false);
            Cylinder("Main ship mast",p+new Vector3(0,6.35f,1),.16f,10.9f,Dark);
            Sail(p+new Vector3(.18f,6.6f,1),6.2f,6.7f);
            Cylinder("Forward mast",p+new Vector3(0,4.65f,4.6f),.11f,7.0f,Dark);
            Sail(p+new Vector3(.12f,5.8f,4.6f),3.8f,4.7f);
            Beam("Mast rigging left",p+new Vector3(0,11.7f,1),p+new Vector3(-3.6f,1.7f,3),.025f,Dark);
            Beam("Mast rigging right",p+new Vector3(0,11.7f,1),p+new Vector3(3.6f,1.7f,3),.025f,Dark);
            Crate(p+new Vector3(-2.4f,1.45f,1.3f),1.0f);
            world=parent;
        }
        static void Sail(Vector3 p,float w,float h)
        {
            var v=new List<Vector3>();var t=new List<int>();
            Quad(v,t,new Vector3(-w*.44f,-h*.5f,0),new Vector3(w*.53f,-h*.43f,0),new Vector3(w*.34f,h*.5f,.25f),new Vector3(-w*.38f,h*.43f,.25f));
            Quad(v,t,v[3],v[2],v[1],v[0]);
            MeshObject("Cream battened junk sail",FlatMesh(v,t),p,Mat("sail canvas","D2C5A5"));
            for(int i=0;i<6;i++)
            {
                float yy=-h*.43f+i*h*.17f;float width=Mathf.Lerp(w*.97f,w*.72f,i/5f);
                Beam("Sail bamboo batten",p+new Vector3(-width*.46f,yy,-.025f),p+new Vector3(width*.54f,yy,.07f),.05f,Timber);
            }
        }
        static void Singapore()
        {
            Base(Mat("singapore pavement","A59A83"));
            Path(0,1.5f,6.4f,31);Path(0,4,27,3);Path(0,-5,27,3);Path(0,12,26,3);
            Shop(-12.2f,5.1f,4.5f,7.8f,Mat("aged limewashed shophouse","C8BA9C"));
            Shop(12.2f,5.1f,4.5f,7.8f,Mat("pale weathered shophouse","D4C4A5"));
            RemittanceOffice();
            Stall(new Vector3(-11.9f,0,-5.8f),3.8f,Linen);
            Stall(new Vector3(11.9f,0,-5.8f),3.8f,Linen);
            Crate(new Vector3(-13,0,-9),1.0f);Crate(new Vector3(-11.8f,0,-9.5f),.8f);
            Pot(new Vector3(10.1f,0,-9.2f),.56f);Pot(new Vector3(11.2f,0,-9.4f),.4f);
            Palm(new Vector3(-13.8f,0,-11.8f)); Palm(new Vector3(13.8f,0,-11.8f));
            Planter(-6.8f,15.6f,2.2f,1.7f);Planter(6.8f,15.6f,2.2f,1.7f);
            Beam("Cross-street festival string",new Vector3(-10,6,10.8f),new Vector3(10,6,10.8f),.035f,Dark);
            for(int i=0;i<5;i++)Lantern(new Vector3(-8+i*4,5.2f,10.8f));
            Bench(new Vector3(8.7f,0,-11.9f));
            Box("Street drainage border left",new Vector3(-14.8f,.012f,2),new Vector3(.18f,.025f,31),Dark);
            Box("Street drainage border right",new Vector3(14.8f,.012f,2),new Vector3(.18f,.025f,31),Dark);
        }
        static void Shop(float x,float z,float w,float d,Material wall)
        {
            // Retain the exact solid shell, while the upper overhang makes an open five-foot way.
            Box("Two-storey shophouse",new Vector3(x,3.2f,z),new Vector3(w,6.4f,d),wall,true);
            float oldFront=z-d*.5f-.08f,front=oldFront-1.45f;
            Box("Upper storey over covered walk",new Vector3(x,4.82f,oldFront-.72f),new Vector3(w,3.14f,1.5f),wall);
            Box("Arcade worn stone floor",new Vector3(x,.025f,oldFront-.67f),new Vector3(w,.05f,1.68f),Stone);
            RoofMesh(x,6.4f,z-.72f,w+.55f,d+1.95f,false);
            HouseAge(x,z,w,d,6.05f,false);
            Box("Plain lime plaster cornice",new Vector3(x,6.17f,front-.035f),new Vector3(w+.17f,.19f,.27f),Cream);
            Box("Dark timber arcade ceiling beam",new Vector3(x,3.20f,front+.05f),new Vector3(w,.23f,.35f),Dark);
            foreach(float dx in new[]{-w*.245f,w*.245f}) LouverWindow(new Vector3(x+dx,4.85f,front-.045f),1.1f,1.88f,x<0?Teal:Dark);
            foreach(float dx in new[]{-w*.45f,w*.45f})
            {
                Box("Five-foot-way square plaster pier",new Vector3(x+dx,1.56f,front+.06f),new Vector3(.30f,3.12f,.38f),Cream);
                Box("Arcade granite pier foot",new Vector3(x+dx,.21f,front+.06f),new Vector3(.38f,.42f,.46f),Stone);
            }
            Box("Recessed timber shopfront darkness",new Vector3(x,1.46f,oldFront-.035f),new Vector3(w-.5f,2.84f,.09f),Shadow);
            PlankFace("Removable shophouse timber shopboards",new Vector3(x+w*.29f,1.40f,oldFront-.11f),w*.28f,2.75f,Dark,5);
            PlankFace("Left stack of removable shopboards",new Vector3(x-w*.32f,1.4f,oldFront-.13f),w*.18f,2.75f,Timber,4);
            Box("Old shop counter within covered walk",new Vector3(x,.48f,oldFront-.47f),new Vector3(w*.36f,.90f,.60f),Timber);
            Box("Small horizontal wooden shop sign",new Vector3(x,2.69f,front-.05f),new Vector3(w*.70f,.45f,.10f),Dark);
            SignText(x<0?"杂货":"米粮",new Vector3(x,2.69f,front-.112f),.092f);
            BambooBlind(new Vector3(x+w*.23f,3.08f,front-.07f),w*.40f,.64f);
            Brickwork(x,oldFront-.01f,w,3.12f,true);
        }
        static void RemittanceOffice()
        {
            // Open front: the counter, shelves, and writing desk are visible in true depth.
            Box("Remittance office back wall",new Vector3(0,2.7f,20.5f),new Vector3(11,5.4f,.35f),Mat("office old plaster","C5B69B"),true);
            foreach(float x in new[]{-5.4f,5.4f})Box("Office side wall",new Vector3(x,2.7f,18.45f),new Vector3(.3f,5.4f,4.4f),Cream,true);
            Box("Office stone floor",new Vector3(0,-.07f,18),new Vector3(11,.14f,6),Mat("office worn stone floor","B1AA97"),true);
            Box("Office upper facade",new Vector3(0,4.4f,16.2f),new Vector3(11,2.0f,.35f),Mat("office faded limewash","D4C4A5"));
            Box("Office dark eave beam",new Vector3(0,5.4f,18.4f),new Vector3(11.25f,.18f,4.7f),Dark);
            foreach(float xx in new[]{-3.5f,0,3.5f})LouverWindow(new Vector3(xx,4.42f,15.97f),1.43f,1.4f,Dark);
            foreach(float xx in new[]{-5.3f,-1.75f,1.75f,5.3f}) Box("Narrow shophouse bay plaster divider",new Vector3(xx,4.4f,15.97f),new Vector3(.17f,1.88f,.17f),Cream);
            RoofMesh(0,5.5f,18.4f,11.7f,5.4f,false);
            foreach(float x in new[]{-5.2f,-3.8f,3.8f,5.2f})Cylinder("Office arcade column",new Vector3(x,1.72f,15.95f),.16f,3.45f,Cream,true);
            Box("Remittance wooden counter",new Vector3(0,.75f,16.9f),new Vector3(6.8f,1.5f,.8f),Timber,true);
            Box("Counter polished dark top",new Vector3(0,1.55f,16.9f),new Vector3(7.05f,.13f,1),Dark);
            for(int i=0;i<4;i++)
            {
                Box("Counter inset panels",new Vector3(-2.5f+i*1.65f,.78f,16.485f),new Vector3(1.25f,.95f,.04f),Mat("counter panel","977346"));
                Box("Sorting shelf upright",new Vector3(-3+i*2,2.2f,20.17f),new Vector3(.10f,3.1f,.45f),Dark);
            }
            for(int i=0;i<3;i++)Box("Postal sorting shelf",new Vector3(0,1.05f+i*.9f,20.1f),new Vector3(6.2f,.12f,.7f),Dark);
            for(int i=0;i<9;i++)Box("Bound letter bundles",new Vector3(-2.55f+(i%3)*2.15f,1.32f+(i/3)*.9f,19.91f),new Vector3(.8f,.33f,.45f),i%2==0?Paper:Mat("old parcel wrap","AD8F6B"));
            for(int i=0;i<3;i++)Box("Letters on the counter",new Vector3(-1.8f+i*1.5f,1.64f,16.85f),new Vector3(.65f,.045f,.45f),Paper);
            Box("Office hanging sign",new Vector3(0,3.21f,15.73f),new Vector3(3.8f,.62f,.14f),Dark);
            SignText("信局",new Vector3(0,3.21f,15.65f),.145f);
            BambooBlind(new Vector3(-4.65f,3.37f,15.7f),1.2f,.57f);
            BambooBlind(new Vector3(4.65f,3.37f,15.7f),1.2f,.72f);
            Lantern(new Vector3(-4.6f,2.9f,15.55f));Lantern(new Vector3(4.6f,2.9f,15.55f));
        }
        static void Awning(Vector3 p,float w,float d,Material color)
        {
            ClothShade(p,w,d);
        }
        static void Stall(Vector3 p,float w,Material color)
        {
            foreach(float dx in new[]{-w*.46f,w*.46f})Cylinder("Market stall bamboo post",p+new Vector3(dx,1.5f,0),.065f,3,Timber);
            Awning(p+new Vector3(0,2.8f,0),w,2.4f,color);
            Box("Market table",p+Vector3.up*.9f,new Vector3(w-.25f,.18f,1.5f),Timber);
            foreach(float dx in new[]{-w*.35f,w*.35f})Box("Stall trestle",p+new Vector3(dx,.45f,0),new Vector3(.15f,.9f,1.2f),Dark);
            for(int i=0;i<4;i++)
            {
                Vector3 q=p+new Vector3(-w*.32f+i*w*.21f,1.02f,0);
                Box("Display basket",q,new Vector3(.6f,.15f,.72f),Mat("basket","AA9063"));
                Blob("Market fruit",q+Vector3.up*.22f,new Vector3(.48f,.4f,.5f),i%2==0?Mat("fruit gold","BE9443"):Mat("fruit green","79844C"));
            }
        }
        static void Palm(Vector3 p)
        {
            var trunk=Cylinder("Palm trunk",p+new Vector3(.225f,2.9f,0),.15f,5.82f,Timber,true);trunk.transform.up=new Vector3(.45f,5.8f,0);
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;Vector3 tip=new Vector3(Mathf.Cos(a)*2.7f,5.0f,Mathf.Sin(a)*2.7f);
                Vector3 start=new Vector3(.45f,5.8f,0),mid=(start+tip)*.5f+Vector3.up*.75f;
                var v=new List<Vector3>();var t=new List<int>();Vector3 width=new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a))*.52f;
                Quad(v,t,start,mid-width,tip,mid+width);Quad(v,t,mid+width,tip,mid-width,start);
                MeshObject("Faceted palm frond",FlatMesh(v,t),p,Mat("palm leaf","598568"));
            }
        }
    }
    public sealed class WaterMotion : MonoBehaviour
    {
        Mesh mesh; Vector3[] original; Vector3[] vertices;
        public void Initialize(Mesh m) { mesh=m; original=m.vertices; vertices=(Vector3[])original.Clone(); }
        void Update()
        {
            if(!mesh) return;float time=Time.time*.55f;
            for(int i=0;i<vertices.Length;i++) {Vector3 p=original[i];p.y+=Mathf.Sin(p.x*.29f+time)*.075f+Mathf.Sin(p.z*.35f-time*.7f)*.045f;vertices[i]=p;}
            mesh.vertices=vertices;mesh.RecalculateNormals();
        }
    }
}
