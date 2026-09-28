using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>Metric, static detail for eye-level play. No changes to mission ground or paths.</summary>
    public static partial class WorldFactory
    {
        static void AddSpatialDepth(string kind)
        {
            Transform parent=world;
            var root=new GameObject("Eye-level architecture and landscape depth · "+kind);
            root.transform.SetParent(parent,false);world=root.transform;
            try {
                switch(kind) {
                    case "quanzhou":
                        DepthGallery("陈家西厢廊屋",new Vector3(-30.25f,0,-7.2f),17.5f,4.25f,2.88f,90);
                        DepthGallery("族亲堂屋侧廊",new Vector3(29.2f,0,20.2f),17.3f,3.2f,3.0f,90);
                        DepthRoadEdge(-4.05f,-30,33,true);DepthRoadEdge(4.05f,-30,33,true);
                        DepthDomesticDetails(new Vector3(-29.6f,0,-4.9f),false);
                        DepthDomesticDetails(new Vector3(28.5f,0,25.5f),false);
                        DepthEarthBank(new Vector3(-38.8f,0,-21),3.0f,14.0f,.62f);
                        DepthEarthBank(new Vector3(37.9f,0,27),2.9f,12.0f,.78f);
                        break;
                    case "quarters":
                        DepthGallery("通铺西厢实顶",new Vector3(-30.1f,0,7.4f),14.7f,5.3f,2.94f,90);
                        DepthGallery("通铺东厢实顶",new Vector3(-15.2f,0,7.4f),14.7f,5.0f,2.94f,90);
                        DepthGallery("灶间遮雨檐",new Vector3(22,0,-8.1f),19.8f,4.6f,2.90f,0);
                        DepthRoadEdge(-4.05f,-30,35,true);DepthRoadEdge(4.05f,-30,35,true);
                        DepthDomesticDetails(new Vector3(29.5f,0,22.2f),true);
                        break;
                    case "market":
                        DepthGallery("陈记柜台实顶",new Vector3(-20,0,22.4f),17.8f,4.9f,3.10f,0);
                        DepthGallery("货院北廊",new Vector3(25,0,16.9f),17.4f,4.0f,3.08f,0);
                        DepthRoadEdge(-4.05f,-30,35,true);DepthRoadEdge(4.05f,-30,35,true);
                        DepthDomesticDetails(new Vector3(30,0,13.2f),true);
                        break;
                    case "postoffice":
                        // Connected shaded galleries around open working courts. Real roofs
                        // create depth overhead, while the middle lanes remain sunlit.
                        DepthGallery("写批长廊实顶",new Vector3(-28.1f,0,5.4f),29.8f,7.5f,3.15f,90);
                        DepthGallery("收寄柜台实顶",new Vector3(-10.2f,0,5.4f),29.8f,6.4f,3.15f,90);
                        DepthGallery("核批柜台实顶",new Vector3(11.0f,0,8),24.4f,5.8f,3.15f,90);
                        DepthGallery("封袋长廊实顶",new Vector3(29.1f,0,8),24.4f,5.6f,3.15f,90);
                        DepthGallery("账房实顶",new Vector3(-20,0,30.5f),23.8f,10.4f,3.28f,0);
                        DepthGallery("封袋后院实顶",new Vector3(20,0,30.5f),23.8f,10.4f,3.28f,0);
                        DepthGallery("纸铺廊檐",new Vector3(24.4f,0,-12.2f),14.6f,4.3f,2.94f,0);
                        DepthDomesticDetails(new Vector3(30.6f,0,34.1f),true);
                        break;
                    case "harbor":
                        DepthQuaySteps(new Vector3(-9.8f,0,22.0f),false);
                        DepthQuaySteps(new Vector3(17.8f,0,22.0f),false);
                        DepthMooringFender(new Vector3(-5.95f,-.2f,25));
                        DepthMooringFender(new Vector3(5.95f,-.2f,33));
                        break;
                    case "port":
                        DepthQuaySteps(new Vector3(36,0,-4),true);
                        DepthQuaySteps(new Vector3(-7,0,38),false);
                        DepthMooringFender(new Vector3(36,-.15f,17));
                        break;
                    case "ship":
                        DepthShipJoinery();break;
                }
            }
            finally { world=parent; }
        }

        static void SolidRoofConstruction(float x,float y,float z,float w,float d,float rise,bool swallow)
        {
            const float thickness=.16f;
            var underside=new List<Vector3>();var ut=new List<int>();
            var edge=new List<Vector3>();var et=new List<int>();
            int cuts=Mathf.Clamp(Mathf.CeilToInt(w/.8f),4,32);
            for(int i=0;i<cuts;i++) {
                float a=-w*.5f+w*i/cuts,b=-w*.5f+w*(i+1)/cuts;
                float liftA=swallow?Mathf.Pow(Mathf.Abs(a)/(w*.5f),8)*.43f:0;
                float liftB=swallow?Mathf.Pow(Mathf.Abs(b)/(w*.5f),8)*.43f:0;
                foreach(float side in new[]{-1f,1f}) {
                    Vector3 ae=new Vector3(a,liftA,side*d*.5f),ar=new Vector3(a,rise+liftA,0);
                    Vector3 be=new Vector3(b,liftB,side*d*.5f),br=new Vector3(b,rise+liftB,0);
                    Vector3 down=Vector3.down*thickness;
                    if(side<0) {
                        Quad(underside,ut,be+down,br+down,ar+down,ae+down);
                        Quad(edge,et,ae+down,ae,be,be+down);
                    } else {
                        Quad(underside,ut,ae+down,ar+down,br+down,be+down);
                        Quad(edge,et,be+down,be,ae,ae+down);
                    }
                    if(i==0)Quad(edge,et,ae,ae+down,ar+down,ar);
                    if(i==cuts-1)Quad(edge,et,be+down,be,br,br+down);
                }
            }
            DetailMesh("Solid roof underside · 160 mm clay and timber",underside,ut,new Vector3(x,y,z),Mat("roof underside timber","66523D"));
            DetailMesh("Layered fired-clay roof fascia",edge,et,new Vector3(x,y,z),Mat("roof fascia clay","5C5A4D"));
            // Rafters are visible from below the porticos, rather than a paper-thin plane.
            var timber=new List<Vector3>();var tt=new List<int>();
            int ribs=Mathf.Clamp(Mathf.CeilToInt(w/.78f),4,32);
            for(int i=0;i<=ribs;i++) {
                float xx=-w*.46f+i*w*.92f/ribs;
                float lift=swallow?Mathf.Pow(Mathf.Abs(xx)/(w*.5f),8)*.43f:0;
                foreach(float side in new[]{-1f,1f})
                    DetailBeam(timber,tt,new Vector3(xx,lift-.24f,side*d*.49f),new Vector3(xx,rise+lift-.24f,0),.09f,.13f);
            }
            DetailBox(timber,tt,new Vector3(0,rise-.28f,0),new Vector3(w,.19f,.22f));
            DetailMesh("Structural roof rafters and ridge beam",timber,tt,new Vector3(x,y,z),Dark);
        }

        static void DepthGallery(string label,Vector3 p,float width,float depth,float height,float yaw)
        {
            Transform parent=world;
            var group=new GameObject(label);group.transform.SetParent(parent,false);
            group.transform.localPosition=p;group.transform.localRotation=Quaternion.Euler(0,yaw,0);world=group.transform;
            try {
                RoofMesh(0,height,0,width,depth,false);
                var v=new List<Vector3>();var t=new List<int>();
                foreach(float side in new[]{-1f,1f})
                    DetailBox(v,t,new Vector3(0,height-.29f,side*(depth*.5f-.25f)),new Vector3(width,.21f,.20f));
                // Tie beams span above walking headroom. There are no extra pillars or
                // invisible collision changes in the authored task corridors.
                for(float at=-width*.5f+.55f;at<width*.5f;at+=3.2f)
                    DetailBox(v,t,new Vector3(at,height-.39f,0),new Vector3(.17f,.19f,depth-.45f));
                DetailMesh(label+" mortise-and-tenon tie beams",v,t,Vector3.zero,Dark);
            }
            finally { world=parent; }
        }

        static void DepthRoadEdge(float x,float near,float far,bool settlement)
        {
            var blocks=new List<Vector3>();var bt=new List<int>();
            var channel=new List<Vector3>();var ct=new List<int>();
            for(float z=near;z<far;z+=.62f) {
                if(settlement) {
                    if(!ESAllowed(x,z,.48f,.65f,false))continue;
                    Rect edge=new Rect(x-.24f,z-.325f,.48f,.65f);bool crossing=false;
                    for(int i=1;i<esRoutes.Count;i++)if(edge.Overlaps(esRoutes[i])) { crossing=true;break; }
                    if(crossing)continue;
                }
                DetailBox(blocks,bt,new Vector3(x,.060f,z),new Vector3(.22f,.12f,.58f));
                // A dark recessed gutter with visible side thickness, wholly off road.
                float xx=x+Mathf.Sign(x)*.21f;
                DetailBox(channel,ct,new Vector3(xx,.013f,z),new Vector3(.21f,.023f,.61f));
                if(Mathf.FloorToInt(z*4)%11==0)
                    DetailBox(blocks,bt,new Vector3(xx,.052f,z),new Vector3(.27f,.08f,.21f));
            }
            DetailMesh("Raised hand-cut lane kerb and crossing stones",blocks,bt,Vector3.zero,Mat("worn kerb granite","96978A"));
            DetailMesh("Narrow roadside rain channels",channel,ct,Vector3.zero,Mat("damp gutter stone","666E5C"));
        }

        static void DepthDomesticDetails(Vector3 p,bool work)
        {
            // Small things with a recognizable purpose: woven carriers, bamboo poles,
            // folded cloth and a shallow basin. All stay beside existing furniture.
            StoneJar(p,.24f,.31f,Mat("woven household basket","9E8861"));
            Cylinder("Woven basket bottom in shadow",p+Vector3.up*.24f,.15f,.01f,Dark);
            var v=new List<Vector3>();var t=new List<int>();
            AppendTube(v,t,new[]{new Vector3(-.19f,.24f,0),new Vector3(-.17f,.51f,0),new Vector3(0,.60f,0),new Vector3(.17f,.51f,0),new Vector3(.19f,.24f,0)},.019f,.019f);
            for(int i=0;i<4;i++)
                DetailBox(v,t,new Vector3(.51f+i*.07f,.07f,.02f),new Vector3(.044f,.045f,1.25f),Quaternion.Euler(0,-12+i*3,0));
            DetailMesh("Bound bamboo carrier and working poles",v,t,p,Timber);
            Box("Folded handwoven cloth",p+new Vector3(-.51f,.05f,-.01f),new Vector3(.38f,.08f,.32f),work?Linen:Paper);
            Cylinder("Shallow wash bowl rim",p+new Vector3(-.53f,.068f,.65f),.22f,.12f,Mat("household earthenware basin","805D43"));
            Cylinder("Recessed wash bowl interior",p+new Vector3(-.53f,.129f,.65f),.18f,.008f,Shadow);
        }

        static void DepthEarthBank(Vector3 p,float w,float d,float h)
        {
            var v=new List<Vector3>();var t=new List<int>();
            Vector3 a=new Vector3(-w*.5f,-.015f,-d*.5f),b=new Vector3(-w*.5f,-.015f,d*.5f);
            Vector3 c=new Vector3(w*.5f,-.015f,d*.5f),e=new Vector3(w*.5f,-.015f,-d*.5f);
            Vector3 aa=new Vector3(-w*.25f,h,-d*.42f),bb=new Vector3(-w*.25f,h,d*.42f);
            Vector3 cc=new Vector3(w*.25f,h,d*.42f),ee=new Vector3(w*.25f,h,-d*.42f);
            Quad(v,t,a,b,bb,aa);Quad(v,t,e,ee,cc,c);Quad(v,t,a,aa,ee,e);Quad(v,t,b,c,cc,bb);Quad(v,t,aa,bb,cc,ee);
            DetailMesh("Raised garden earth bank",v,t,p,Mat("garden bank soil","89816A"));
        }

        static void DepthQuaySteps(Vector3 p,bool east)
        {
            Transform parent=world;var group=new GameObject("Granite landing steps down to tide");
            group.transform.SetParent(parent,false);group.transform.localPosition=p;
            group.transform.localRotation=Quaternion.Euler(0,east?90:0,0);world=group.transform;
            try {
                for(int i=0;i<5;i++)
                    Box("Salt-worn landing step",new Vector3(0,-.14f-i*.19f,.17f+i*.30f),new Vector3(2.4f,.32f,.37f),i>2?Mat("tidal damp granite","69786B"):Stone);
                var v=new List<Vector3>();var t=new List<int>();
                foreach(float side in new[]{-1f,1f})for(int i=0;i<5;i++)
                    DetailBox(v,t,new Vector3(side*1.31f,.03f-i*.19f,.17f+i*.30f),new Vector3(.22f,.40f,.35f));
                DetailMesh("Cut stone landing cheek walls",v,t,Vector3.zero,Stone);
            }
            finally { world=parent; }
        }

        static void DepthMooringFender(Vector3 p)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<3;i++) {
                Vector3 top=p+new Vector3(0,.36f,i*.16f),bottom=top+new Vector3(0,-1.05f,0);
                AppendTube(v,t,new[]{top,bottom},.060f,.065f);
            }
            DetailMesh("Hemp and timber mooring fender",v,t,Vector3.zero,Mat("old hemp rope","96835F"));
        }

        static void DepthShipJoinery()
        {
            var v=new List<Vector3>();var t=new List<int>();
            foreach(float side in new[]{-1f,1f}) {
                for(int i=0;i<11;i++) {
                    float z=-25+i*5f;
                    DetailBox(v,t,new Vector3(side*10.43f,.72f,z),new Vector3(.16f,1.44f,.21f));
                    DetailBeam(v,t,new Vector3(side*10.43f,.96f,z),new Vector3(side*9.98f,.08f,z),.12f,.18f);
                }
            }
            DetailMesh("Ship gunwale knees and visible timber ribs",v,t,Vector3.zero,Dark);
        }

        static void SSBatteredField(SSGeometry s,float x,float z,float w,float d,float height,Material bed)
        {
            float inset=.55f;
            Vector3 a=new Vector3(x-w*.5f-inset,-.02f,z-d*.5f-inset),b=new Vector3(x-w*.5f-inset,-.02f,z+d*.5f+inset);
            Vector3 c=new Vector3(x+w*.5f+inset,-.02f,z+d*.5f+inset),e=new Vector3(x+w*.5f+inset,-.02f,z-d*.5f-inset);
            Vector3 aa=new Vector3(x-w*.5f,height,z-d*.5f),bb=new Vector3(x-w*.5f,height,z+d*.5f);
            Vector3 cc=new Vector3(x+w*.5f,height,z+d*.5f),ee=new Vector3(x+w*.5f,height,z-d*.5f);
            s.Face(SSSoil,a,b,bb,aa);s.Face(SSSoil,e,ee,cc,c);s.Face(SSSoil,a,aa,ee,e);s.Face(SSSoil,b,c,cc,bb);
            s.Face(bed,aa,bb,cc,ee);
            // Open stone drains show the drop between cultivated plots and the road.
            for(float zz=z-d*.5f+.4f;zz<z+d*.5f-.3f;zz+=.85f)
                s.Box(Stone,new Vector3(x-w*.5f-.28f,height*.38f,zz),new Vector3(.21f,height*.76f,.76f));
        }

        static void SSHillMass(SSGeometry s,float x,float z,float rx,float rz,float height,int seed)
        {
            // One shared-vertex mesh per hill gives continuous lighting across rings.
            // Rounded crowns and muted foliage replace the former bright faceted cone.
            const int sides=28,rings=6;
            var vertices=new List<Vector3>(1+sides*rings);
            var triangles=new List<int>(sides*(2*rings-1)*3);
            vertices.Add(HillVertex(x,z,rx,rz,height,0,0,seed));
            for(int ring=1;ring<=rings;ring++)
                for(int i=0;i<sides;i++)
                    vertices.Add(HillVertex(x,z,rx,rz,height,ring/(float)rings,i*Mathf.PI*2/sides,seed));
            for(int i=0;i<sides;i++) {
                triangles.Add(0);triangles.Add(1+(i+1)%sides);triangles.Add(1+i);
            }
            for(int ring=0;ring<rings-1;ring++)for(int i=0;i<sides;i++) {
                int a=1+ring*sides+i,b=1+ring*sides+(i+1)%sides;
                int c=b+sides,d=a+sides;
                triangles.Add(a);triangles.Add(b);triangles.Add(c);
                triangles.Add(a);triangles.Add(c);triangles.Add(d);
            }
            Mesh mesh=FlatMesh(vertices,triangles);
            mesh.name="Smooth wooded ridge · "+seed;
            MeshObject(mesh.name,mesh,Vector3.zero,Mat(seed%2==0?"muted wooded ridge":"muted distant ridge",
                seed%2==0?"526A59":"5B7066"));
        }
        static Vector3 HillVertex(float x,float z,float rx,float rz,float height,float radial,float angle,int seed)
        {
            float irregular=1+.055f*Mathf.Sin(angle*3+seed)+.025f*Mathf.Cos(angle*5-seed);
            float profile=Mathf.Pow(Mathf.Max(0,1-radial*radial),1.75f);
            float contour=1+.045f*Mathf.Sin(angle*2+seed)*radial*(1-radial);
            float yy=height*.72f*profile*contour-.025f;
            return new Vector3(x+Mathf.Cos(angle)*rx*radial*irregular,yy,z+Mathf.Sin(angle)*rz*radial*irregular);
        }
    }
}
