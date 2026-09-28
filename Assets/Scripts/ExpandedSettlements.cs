using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>Three distinct full-size settlements. Collidable details respect authored routes.</summary>
    public static partial class WorldFactory
    {
        static Vector2[] esPoints;
        static readonly List<Rect> esRoutes = new List<Rect>();
        static string esPlace;
        static int esPaving;

        static void ExpandedQuanzhou()
        {
            ESBegin("quanzhou",Mat("expanded village earth","9E8A69"),new[]{
                new Vector2(0,-27),new Vector2(-23,-5),new Vector2(-26,-13),new Vector2(21,18),
                new Vector2(0,30),new Vector2(-6,-22),new Vector2(-24,-22),new Vector2(23,14)});
            ESRoute(0,-22,-24,-22);ESRoute(-23,-22,-23,-5);ESRoute(-23,-13,-26,-13);
            ESRoute(0,8,21,8);ESRoute(21,8,21,18);ESRoute(21,14,23,14);
            ESPaths();
            ESOpenCourt("陈家院落",-23,-7,18,20,9,false,0,Brick);
            ESShelterBack(-23,2,17,2.4f,2.85f,false);
            ESHouse(-27,11,13,7,3.6f,Brick);
            ESHouse(-12,11,10,8,3.35f,Mat("village patched plaster","C3B18F"));
            ESHouse(-28,-28,12,5,3.0f,Mat("village work shed plaster","B4A68B"));
            ESHouse(29,-18,11,9,3.1f,Brick);
            ESOpenCourt("族亲堂屋前院",21,21,21,20,11,false,0,Cream);
            ESShelterBack(21,29.4f,20,3.1f,3.6f,true);
            ESBench(-28.8f,-2.0f);ESJar(-30,-11,.56f);ESJar(-29.6f,-9.6f,.39f);
            ESWorkTable(-28.5f,-1,1.65f);ESWorkTable(15.1f,26,2.1f);
            ESBench(25.8f,26.4f);ESJar(29,23.5f,.55f);
            Lantern(new Vector3(15,3.0f,11.0f));Lantern(new Vector3(27,3.0f,11.0f));
            Box("榕溪公所旧木匾",new Vector3(21,3.14f,28),new Vector3(3.8f,.60f,.14f),Dark);
            SignText("榕溪公所",new Vector3(21,3.14f,27.915f),.115f);
            Clothesline(new Vector3(-30,0,-1.0f),new Vector3(-18,0,-1.0f));
            ESDryingYard(17,-5,22,15);
            ESField(-25,28,19,17);ESField(25,35,18,6);
            ESWalkStrip(-11,27,3.5f,21,Mat("village field bank","B1A280"));
            ESWalkStrip(-23,18.4f,25,3.2f,Mat("village field bank","B1A280"));
            ESTree(-34,-17,.82f);ESTree(33,7,.9f);ESTree(-34,20,.72f);ESTree(11,35,.78f);
            ESVillageGate(0,30);
            ESBoundaryGarden(-34,29,5);ESBoundaryGarden(34,-28,4);
        }

        static void ExpandedMarket()
        {
            ESBegin("market",Mat("market old granite earth","A99E88"),new[]{
                new Vector2(0,-27),new Vector2(-20,19),new Vector2(-22,-4),new Vector2(21,7),
                new Vector2(0,28),new Vector2(22,-18),new Vector2(-6,-22),new Vector2(-24,-22),new Vector2(23,14)});
            ESRoute(0,-22,-24,-22);ESRoute(0,-18,22,-18);
            ESRoute(0,-4,-22,-4);ESRoute(0,8,-20,8);ESRoute(-20,8,-20,19);
            ESRoute(0,7,21,7);ESRoute(21,7,23,7);ESRoute(23,7,23,14);
            ESPaths();
            // Continuous narrow-fronted shop rows form a northern commercial street.
            foreach(float x in new[]{-30f,-21f,-12f,12f,21f,30f})
                ESShop(x,34,7.8f,8,x<0?Mat("market shop limewash","C8BA9C"):Cream);
            ESWalkStrip(0,27,69,3.4f,Stone);
            ESOpenCourt("掌柜可入账房",-20,18,18,14,9,false,0,Cream);
            ESShelterBack(-20,24,17.4f,2.5f,3.0f,false);
            ESCounter(-20,23.1f,7.4f);ESPaperBundles(-25.3f,22.9f);
            BambooBlind(new Vector3(-26.8f,2.85f,12),2.1f,.65f);
            Box("杂货铺木匾",new Vector3(-20,2.7f,24.1f),new Vector3(3.2f,.50f,.12f),Dark);
            SignText("杂货账房",new Vector3(-20,2.7f,24.02f),.095f);
            ESOpenCourt("货物后院",25,9.5f,18,19,9,true,7,Mat("market yard plaster","BBA98B"));
            ESShelterBack(25,18.3f,17.4f,2.4f,2.65f,false);
            ESCrateStack(30.7f,16,3);ESCrateStack(31,2.9f,2);ESJar(30.5f,11.0f,.63f);
            ESMarketBooth(-31,-13,6.5f,4.8f,"米");ESMarketBooth(-31,-3,6.5f,4.8f,"布");
            ESMarketBooth(-31,5,6.5f,4.8f,"茶");ESMarketBooth(-12,-12,8.0f,4.8f,"米");
            ESMarketBooth(-12,1.0f,8,4.8f,"杂");ESMarketBooth(31,-9,7,5.2f,"油");
            ESMarketBooth(13,-10.4f,7,5.0f,"茶");
            ESHouse(-29,-29,12,4.5f,3.0f,Brick);
            ESHouse(29,-28,12,5,3.2f,Mat("market store rough limewash","C2B18F"));
            ESCrateStack(32.8f,-24.0f,2);ESJar(31.5f,-14.2f,.7f);
            ESBench(26.7f,-20.5f);ESWorkTable(28.0f,-20.8f,1.5f);
            Lantern(new Vector3(-8.6f,3.9f,23));Lantern(new Vector3(8.6f,3.9f,23));
            ESTree(-35,17,.72f);ESTree(35,22,.75f);
            ESBoundaryGarden(35,35,3);
        }

        static void ExpandedQuarters()
        {
            ESBegin("quarters",Mat("quarters packed earth","A49479"),new[]{
                new Vector2(0,-27),new Vector2(-22,8),new Vector2(22,14),new Vector2(0,27),
                new Vector2(-6,-22),new Vector2(-24,-22),new Vector2(23,14)});
            ESRoute(0,-22,-24,-22);ESRoute(0,-4,-22,-4);ESRoute(-22,-4,-22,8);
            ESRoute(0,14,22,14);ESRoute(22,14,23,14);
            ESRoute(0,-2,22,-2);ESRoute(22,-2,22,14);
            ESPaths();
            // Cutaway communal rooms are genuinely empty inside, with separate sleeping bays.
            ESOpenCourt("乡亲通铺院",-23,8,20,16,9,false,0,Mat("dormitory old limewash","BDAE91"));
            ESShelterBack(-23,15.1f,19.4f,2.4f,2.75f,false);
            ESBedRow(-29.7f,4.1f,2);ESBedRow(-16.4f,4.1f,2);
            ESBench(-28.5f,13);ESJar(-31.0f,9.0f,.43f);
            ESOpenCourt("晾衣与井水后院",23,14,20,22,9,true,14,Cream);
            ESShelterBack(23,24.1f,19.4f,2.4f,2.7f,false);
            Clothesline(new Vector3(16.4f,0,21),new Vector3(30.4f,0,21));
            Clothesline(new Vector3(16.4f,0,18.7f),new Vector3(30.4f,0,18.7f));
            ESWashArea(30,8.4f);ESBench(29.2f,18.4f);
            ESOpenCourt("公用灶间",22,-12,21,13,9,false,0,Mat("kitchen smoke stained plaster","AFA185"));
            ESShelterBack(22,-6.2f,20.4f,2.5f,2.55f,false);
            ESStove(15,-8);ESStove(19,-8);ESWorkTable(27.3f,-9.0f,2.1f);
            ESJar(30,-15,.66f);ESJar(28.5f,-15.5f,.48f);ESBench(16,-15.1f);
            ESHouse(-28,-12,11,10,3.2f,Brick);ESHouse(-11,-13,8,13,3.0f,Mat("quarters lime patch","C1B092"));
            ESHouse(-27,25,12,10,3.3f,Mat("quarters west longhouse","BDA88B"));
            ESHouse(-12,29,8,12,3.4f,Brick);ESHouse(29,33,12,6,3.1f,Cream);
            ESWalkStrip(-19.2f,28,3.3f,17,Mat("quarters rear narrow lane","AAA08A"));
            ESWalkStrip(-23,18.2f,24,3.1f,Stone);
            ESWorkTable(-32.3f,-19.7f,1.7f);ESCrateStack(-31.5f,-25.9f,2);
            ESJar(-9.0f,22,.43f);ESJar(-8.8f,24,.55f);
            Lantern(new Vector3(-31.5f,2.7f,.5f));Lantern(new Vector3(31.3f,2.7f,3.5f));
            ESTree(34.7f,-25,.68f);ESTree(-35,34,.72f);ESTree(10.0f,34.5f,.63f);
            ESBoundaryGarden(-35,-28,4);
        }

        static void ESBegin(string kind,Material ground,Vector2[] points)
        {
            esPlace=kind;esPoints=points;esRoutes.Clear();esPaving=0;
            esRoutes.Add(new Rect(-4,-32,8,70));
            Box(kind+" substantial landscape foundation",new Vector3(0,-.72f,3),new Vector3(78,1.15f,76),Mat("expanded foundation","85806F"));
            Box(kind+" full walkable ground",new Vector3(0,-.13f,3),new Vector3(77.6f,.26f,75.6f),ground,true);
            Box(kind+" lower stone reveal",new Vector3(0,-1.33f,3),new Vector3(77.2f,.12f,75.2f),Dark);
        }
        static void ESRoute(float ax,float az,float bx,float bz,float width=3.6f)
        {
            esRoutes.Add(new Rect(Mathf.Min(ax,bx)-width*.5f,Mathf.Min(az,bz)-width*.5f,Mathf.Abs(bx-ax)+width,Mathf.Abs(bz-az)+width));
        }
        static void ESPaths()
        {
            ESWalkStrip(0,3,7.7f,70,Mat("expanded main path","AAA28C"));
            for(int i=1;i<esRoutes.Count;i++)
            {
                Rect route=esRoutes[i];ESWalkStrip(route.center.x,route.center.y,route.width-.15f,route.height-.15f,Mat("expanded side path","B4AA92"));
            }
        }
        static void ESWalkStrip(float x,float z,float width,float depth,Material color)
        {
            float y=.016f+esPaving++*.0045f;
            HumanScalePaving("Settlement paving",x,z,width,depth,y,color);
        }
        static bool ESAllowed(float x,float z,float width,float depth,bool routes=true)
        {
            Rect r=new Rect(x-width*.5f,z-depth*.5f,width,depth);
            foreach(Vector2 p in esPoints)
            {
                float dx=Mathf.Max(r.xMin-p.x,0,p.x-r.xMax),dz=Mathf.Max(r.yMin-p.y,0,p.y-r.yMax);
                if(dx*dx+dz*dz<2.3f*2.3f)return false;
            }
            if(routes)foreach(Rect path in esRoutes)if(r.Overlaps(path))return false;
            return true;
        }
        static void ESGuard(float x,float z,float w,float d,string name)
        {
            if(!ESAllowed(x,z,w,d))throw new InvalidOperationException(esPlace+" geometry would obstruct a reserved mission point or 3.6m corridor: "+name+" "+new Vector2(x,z));
        }
        static GameObject ESSolid(string label,Vector3 p,Vector3 size,Material material)
        {
            ESGuard(p.x,p.z,size.x,size.z,label);return Box(label,p,size,material,true);
        }
        static void ESHouse(float x,float z,float w,float d,float h,Material wall)
        {
            ESGuard(x,z,w+.18f,d+.16f,"ordinary cottage");House(x,z,w,d,h,wall,false);
        }
        static void ESShop(float x,float z,float w,float d,Material wall)
        {
            ESGuard(x,z,w,d,"continuous shophouse");Shop(x,z,w,d,wall);
        }
        static void ESOpenCourt(string label,float x,float z,float w,float d,float entry,bool westDoor,float westDoorZ,Material wall)
        {
            ESWalkStrip(x,z,w-.38f,d-.38f,Mat("expanded courtyard stone","B0A38A"));
            float left=x-w*.5f,right=x+w*.5f,front=z-d*.5f,back=z+d*.5f;
            ESCourtWall(label+" back wall",x,back,w,.38f,1.55f,wall);
            ESCourtWall(label+" east wall",right,z,.38f,d,1.25f,wall);
            if(westDoor)
            {
                float gap=7.3f;
                float lower=westDoorZ-gap*.5f,upper=westDoorZ+gap*.5f;
                if(lower>front)ESCourtWall(label+" west wall south",left,(front+lower)*.5f,.38f,lower-front,1.25f,wall);
                if(upper<back)ESCourtWall(label+" west wall north",left,(upper+back)*.5f,.38f,back-upper,1.25f,wall);
            }
            else ESCourtWall(label+" west wall",left,z,.38f,d,1.25f,wall);
            float segment=(w-entry)*.5f;
            ESCourtWall(label+" entrance left return",left+segment*.5f,front,segment,.38f,1.04f,wall);
            ESCourtWall(label+" entrance right return",right-segment*.5f,front,segment,.38f,1.04f,wall);
            float gateX=x,gateWidth=3.9f;
            foreach(Rect route in esRoutes)
                if(route.height>route.width&&front>=route.yMin&&front<=route.yMax&&Mathf.Abs(route.center.x-x)<entry*.5f-2)
                    gateX=route.center.x;
            bool compactGate=ESAllowed(gateX-gateWidth*.5f-.1f,front,.20f,.24f)&&ESAllowed(gateX+gateWidth*.5f+.1f,front,.20f,.24f);
            if(!compactGate){gateX=x;gateWidth=entry+.28f;}
            foreach(float side in new[]{-1f,1f})
            {
                float xx=gateX+side*(gateWidth*.5f+.10f);
                ESSolid(label+" timber gate jamb",new Vector3(xx,1.125f,front),new Vector3(.20f,2.25f,.24f),Dark);
            }
            if(compactGate)Box(label+" adult-scale entrance lintel",new Vector3(gateX,2.28f,front),new Vector3(gateWidth+.45f,.14f,.28f),Timber);
            ESCourtDrain(label,x,z,w,d,entry);
            // Narrow rear and side eave fragments show room shape without covering its interior.
            var beams=new List<Vector3>();var bt=new List<int>();
            DetailBox(beams,bt,new Vector3(x,2.43f,back),new Vector3(w,.15f,.19f));
            foreach(float side in new[]{-1f,1f})DetailBox(beams,bt,new Vector3(x+side*w*.5f,2.4f,z+d*.28f),new Vector3(.16f,.15f,d*.43f));
            DetailMesh(label+" open roof framing",beams,bt,Vector3.zero,Timber);
        }
        static void ESCourtWall(string label,float x,float z,float w,float d,float h,Material material)
        {
            if(w<.05f||d<.05f)return;
            ESSolid(label,new Vector3(x,h*.5f,z),new Vector3(w,h,d),material);
            Box(label+" worn stone coping",new Vector3(x,h+.025f,z),new Vector3(w+.04f,.07f,d+.04f),Stone);
            // Small granite foundation stones sit within the original wall footprint.
            var v=new List<Vector3>();var t=new List<int>();bool alongX=w>d;
            float length=alongX?w:d;int count=Mathf.Max(1,Mathf.CeilToInt(length/.48f));
            for(int i=0;i<count;i++)
            {
                float at=-length*.5f+(i+.5f)*length/count;
                DetailBox(v,t,new Vector3(x+(alongX?at:0),.16f,z+(alongX?0:at)),
                    alongX?new Vector3(length/count-.014f,.29f,d+.026f):new Vector3(w+.026f,.29f,length/count-.014f));
            }
            DetailMesh(label+" individual stone wall footing",v,t,Vector3.zero,Mat("courtyard wall foot granite","999988"));
        }
        static void ESCourtDrain(string label,float x,float z,float w,float d,float entry)
        {
            float top=.018f+esPaving*.0045f;
            var joint=new List<Vector3>();var jt=new List<int>();var stone=new List<Vector3>();var st=new List<int>();
            foreach(float side in new[]{-1f,1f})
            {
                float xx=x+side*(w*.5f-.42f);
                DetailBox(joint,jt,new Vector3(xx,top,z),new Vector3(.21f,.018f,d-.8f));
                for(float at=-d*.5f+.6f;at<d*.5f-.4f;at+=.56f)
                    DetailBox(stone,st,new Vector3(xx,top+.014f,z+at),new Vector3(.28f,.032f,.45f));
            }
            float back=z+d*.5f-.41f;
            DetailBox(joint,jt,new Vector3(x,top,back),new Vector3(w-.72f,.018f,.21f));
            for(float at=-w*.5f+.65f;at<w*.5f-.4f;at+=.56f)
                DetailBox(stone,st,new Vector3(x+at,top+.014f,back),new Vector3(.45f,.032f,.28f));
            // Flush threshold: a visual stone sill, without a step or extra collider.
            for(float at=-entry*.5f+.27f;at<entry*.5f;at+=.54f)
                DetailBox(stone,st,new Vector3(x+at,top+.004f,z-d*.5f),new Vector3(.52f,.024f,.32f));
            DetailMesh(label+" recessed rainwater channel",joint,jt,Vector3.zero,Shadow);
            DetailMesh(label+" drain covers and worn threshold",stone,st,Vector3.zero,Stone);
        }
        static void ESShelterBack(float x,float z,float w,float d,float height,bool swallow)
        {
            RoofMesh(x,height,z,w,d,swallow);
            int bays=Mathf.Max(1,Mathf.CeilToInt((w-.72f)/3.5f));
            for(int i=0;i<=bays;i++)
            {
                float xx=x-w*.5f+.36f+i*(w-.72f)/bays;
                if(ESAllowed(xx,z,.20f,.20f))ESSolid("Rear gallery support",new Vector3(xx,height*.5f,z),new Vector3(.20f,height,.20f),Dark);
            }
        }
        static void ESJar(float x,float z,float r)
        {
            if(!ESAllowed(x,z,r*2,r*2))return;StoneJar(new Vector3(x,0,z),r,r*1.45f,Mat("settlement earthenware","956047"));
        }
        static void ESBench(float x,float z)
        {
            if(!ESAllowed(x,z,2.2f,.75f))return;
            ESSolid("Adult-height timber bench seat",new Vector3(x,.407f,z),new Vector3(1.7f,.085f,.39f),Timber);
            var v=new List<Vector3>();var t=new List<int>();
            foreach(float side in new[]{-1f,1f})DetailBox(v,t,new Vector3(x+side*.59f,.19f,z),new Vector3(.10f,.38f,.32f));
            DetailBox(v,t,new Vector3(x,.22f,z),new Vector3(1.3f,.065f,.07f));
            DetailMesh("Bench mortised legs and stretcher",v,t,Vector3.zero,Dark);
        }
        static void ESWorkTable(float x,float z,float width)
        {
            if(!ESAllowed(x,z,width,1.1f))return;
            ESSolid("Usable settlement worktable",new Vector3(x,.735f,z),new Vector3(width,.09f,.86f),Timber);
            var v=new List<Vector3>();var t=new List<int>();
            foreach(float xx in new[]{-width*.36f,width*.36f})foreach(float zz in new[]{-.31f,.31f})DetailBox(v,t,new Vector3(xx,.345f,zz),new Vector3(.075f,.69f,.075f));
            DetailBox(v,t,new Vector3(0,.31f,0),new Vector3(width*.72f,.06f,.07f));
            DetailMesh("Worktable trestle legs",v,t,new Vector3(x,0,z),Dark);
            Cylinder("Small tea bowl",new Vector3(x,.823f,z),.075f,.075f,Cream);
        }
        static void ESCounter(float x,float z,float width)
        {
            ESSolid("Waist-height open shop counter",new Vector3(x,.445f,z),new Vector3(width,.89f,.72f),Dark);
            Box("Worn counter top",new Vector3(x,.928f,z),new Vector3(width+.08f,.076f,.80f),Timber);
            PlankFace("Counter planks",new Vector3(x,.465f,z-.367f),width-.06f,.84f,Timber,Mathf.RoundToInt(width/.19f));
        }
        static void ESPaperBundles(float x,float z)
        {
            ESWorkTable(x,z,2.1f);
            for(int i=0;i<3;i++)Box("Bound ledger and wrapped paper",new Vector3(x-.6f+i*.55f,.822f,z),new Vector3(.34f,.084f,.44f),i%2==0?Paper:Linen);
        }
        static void ESCrateStack(float x,float z,int count)
        {
            if(!ESAllowed(x,z,2.6f,2.0f))return;
            for(int i=0;i<count;i++)
            {
                Vector3 p=new Vector3(x+(i%2)*1.05f-.5f,(i/2)*.94f,z);
                Crate(p,.94f);
                ESSolid("Solid shipping crate",p+Vector3.up*.46f,new Vector3(.92f,.92f,.92f),Timber);
            }
        }
        static void ESMarketBooth(float x,float z,float w,float d,string goods)
        {
            ESGuard(x,z,w,d,"roofed market stall");
            int bays=w>=7.7f?3:2;float pitch=w/bays,shadeDepth=Mathf.Min(3.1f,d-.55f);
            var shade=new List<Vector3>();var st=new List<int>();var wood=new List<Vector3>();var wt=new List<int>();
            var baskets=new List<Vector3>();var bt=new List<int>();var wares=new List<Vector3>();var gt=new List<int>();
            var secondary=new List<Vector3>();var gt2=new List<int>();
            for(int bay=0;bay<bays;bay++)
            {
                float bx=x-w*.5f+(bay+.5f)*pitch,bw=pitch-.18f,front=z+.12f-shadeDepth*.5f,back=z+.12f+shadeDepth*.5f;
                float counterZ=z+d*.22f;
                ESCounter(bx,counterZ,bw-.28f);
                // Each vendor has a 2.3m high awning, rather than one empty 8m canopy.
                for(int strip=0;strip<4;strip++)
                {
                    float a=-bw*.5f+strip*bw/4,b=a+bw/4;
                    float sa=Mathf.Sin(strip*Mathf.PI/4)*-.07f,sb=Mathf.Sin((strip+1)*Mathf.PI/4)*-.07f;
                    Vector3 aa=new Vector3(bx+a,2.23f+sa,front),bb=new Vector3(bx+a,2.39f+sa,back);
                    Vector3 cc=new Vector3(bx+b,2.39f+sb,back),dd=new Vector3(bx+b,2.23f+sb,front);
                    Quad(shade,st,aa,bb,cc,dd);Quad(shade,st,dd,cc,bb,aa);
                }
                foreach(float side in new[]{-1f,1f})
                {
                    float px=bx+side*(bw*.5f-.07f);
                    DetailBox(wood,wt,new Vector3(px,1.11f,front+.09f),new Vector3(.085f,2.22f,.085f));
                    DetailBox(wood,wt,new Vector3(px,1.19f,back-.09f),new Vector3(.085f,2.38f,.085f));
                    DetailBeam(wood,wt,new Vector3(px,2.23f,front),new Vector3(px,2.39f,back),.065f,.065f);
                }
                DetailBox(wood,wt,new Vector3(bx,2.22f,front),new Vector3(bw,.065f,.065f));
                int trays=Mathf.Max(2,Mathf.FloorToInt((bw-.35f)/.66f));
                for(int tray=0;tray<trays;tray++)
                {
                    float tx=bx+(tray-(trays-1)*.5f)*.64f;
                    DetailBox(baskets,bt,new Vector3(tx,.995f,counterZ),new Vector3(.52f,.045f,.47f));
                    foreach(float side in new[]{-1f,1f})
                    {
                        DetailBox(baskets,bt,new Vector3(tx+side*.255f,1.04f,counterZ),new Vector3(.022f,.075f,.47f));
                        DetailBox(baskets,bt,new Vector3(tx,1.04f,counterZ+side*.225f),new Vector3(.52f,.075f,.024f));
                    }
                    ESStallWares(wares,gt,secondary,gt2,new Vector3(tx,1.025f,counterZ),goods,tray+bay);
                }
                // Empty trays tucked below the counter give the stall a working back.
                DetailBox(baskets,bt,new Vector3(bx,.15f,counterZ-.16f),new Vector3(.48f,.26f,.39f));
            }
            DetailMesh("Separate vendor linen awnings",shade,st,Vector3.zero,Linen);
            DetailMesh("Human-scale stall bamboo poles and spars",wood,wt,Vector3.zero,Timber);
            DetailMesh("Hand-sized woven display trays",baskets,bt,Vector3.zero,Mat("market woven trays","AC9062"));
            Material waresMaterial=goods=="布"?Mat("folded cloth indigo","566775"):goods=="茶"?Mat("wrapped tea packets","A18B65"):
                goods=="油"?Mat("small market oil jars","97664B"):Mat("market grain and produce","B19B64");
            DetailMesh("Individual "+goods+" wares",wares,gt,Vector3.zero,waresMaterial);
            DetailMesh("Small cloth ties and packet details",secondary,gt2,Vector3.zero,Linen);
            Box("Plain cloth trade pennant",new Vector3(x-w*.43f,1.82f,z-shadeDepth*.49f),new Vector3(.41f,.72f,.025f),Linen);
            SignText(goods,new Vector3(x-w*.43f,1.86f,z-shadeDepth*.49f-.025f),.10f);
        }
        static void ESStallWares(List<Vector3> v,List<int> t,List<Vector3> alt,List<int> at,Vector3 p,string goods,int pattern)
        {
            if(goods=="布")
            {
                for(int layer=0;layer<3;layer++)
                    DetailBox(layer%2==0?v:alt,layer%2==0?t:at,p+new Vector3((layer%2)*.018f,.03f+layer*.065f,0),new Vector3(.44f,.06f,.34f));
                DetailBox(alt,at,p+new Vector3(0,.17f,0),new Vector3(.025f,.15f,.36f));
            }
            else if(goods=="油")
            {
                for(int jar=0;jar<2;jar++)
                {
                    Vector3 origin=p+new Vector3((jar-.5f)*.23f,0,0);
                    for(int side=0;side<8;side++)
                    {
                        float a=side*Mathf.PI/4,b=(side+1)*Mathf.PI/4;
                        Vector3 aa=new Vector3(Mathf.Cos(a)*.105f,0,Mathf.Sin(a)*.105f),bb=new Vector3(Mathf.Cos(b)*.105f,0,Mathf.Sin(b)*.105f);
                        Vector3 cc=new Vector3(Mathf.Cos(b)*.067f,.23f,Mathf.Sin(b)*.067f),dd=new Vector3(Mathf.Cos(a)*.067f,.23f,Mathf.Sin(a)*.067f);
                        Quad(v,t,origin+aa,origin+dd,origin+cc,origin+bb);
                    }
                    DetailBox(alt,at,origin+Vector3.up*.25f,new Vector3(.11f,.045f,.11f));
                }
            }
            else if(goods=="茶")
            {
                for(int row=0;row<2;row++)for(int col=0;col<3;col++)
                {
                    Vector3 item=p+new Vector3((col-1)*.14f,.075f,(row-.5f)*.18f);
                    DetailBox(v,t,item,new Vector3(.12f,.15f,.15f));
                    DetailBox(alt,at,item+new Vector3(0,.006f,-.076f),new Vector3(.025f,.14f,.008f));
                }
            }
            else
            {
                // Small rice mounds and vegetables, not head-sized undifferentiated blobs.
                for(int row=0;row<3;row++)for(int col=0;col<4;col++)
                {
                    float h=.045f+((row+col+pattern)%3)*.018f;
                    Vector3 item=p+new Vector3((col-1.5f)*.105f,h*.5f,(row-1)*.115f);
                    DetailBox(v,t,item,new Vector3(.09f,h,.097f),Quaternion.Euler(0,(row+col)*17,0));
                }
                DetailBox(alt,at,p+new Vector3(.16f,.10f,.13f),new Vector3(.035f,.028f,.19f));
            }
        }
        static void ESTree(float x,float z,float scale)
        {
            if(!ESAllowed(x,z,scale,scale))return;Tree(new Vector3(x,0,z),scale);
        }
        static void ESDryingYard(float x,float z,float w,float d)
        {
            Box("Village threshing and drying yard",new Vector3(x,.012f,z),new Vector3(w,.022f,d),Mat("sun baked drying floor","B49A6D"));
            var v=new List<Vector3>();var t=new List<int>();
            for(int row=0;row<3;row++)for(int col=0;col<4;col++)
            {
                float xx=x-w*.32f+col*w*.21f,zz=z-d*.28f+row*d*.28f;
                if(!ESAllowed(xx,zz,2.8f,1.9f))continue;
                DetailBox(v,t,new Vector3(xx,.062f,zz),new Vector3(2.8f,.045f,1.9f));
            }
            DetailMesh("Laid-out grain drying mats",v,t,Vector3.zero,Mat("sun drying straw","AA8B4E"));
            ESWorkTable(x+w*.39f,z+d*.38f,2.0f);
            ESJar(x-w*.43f,z-d*.34f,.65f);
        }
        static void ESField(float x,float z,float w,float d)
        {
            Box("Village planted plot",new Vector3(x,.013f,z),new Vector3(w,.026f,d),Mat("cultivated earth","736E45"));
            var v=new List<Vector3>();var t=new List<int>();
            for(int row=0;row<Mathf.FloorToInt(d/.9f);row++)for(int col=0;col<Mathf.FloorToInt(w/.65f);col++)
            {
                float xx=x-w*.47f+col*.65f,zz=z-d*.46f+row*.9f;
                if(!ESAllowed(xx,zz,.28f,.28f,false))continue;
                float h=.22f+(row+col)%3*.075f;
                DetailBox(v,t,new Vector3(xx,h*.5f,zz),new Vector3(.09f,h,.24f),Quaternion.Euler(0,(col%3)*22,7));
            }
            DetailMesh("Low cultivated field rows",v,t,Vector3.zero,Mat("field green","7A824B"));
        }
        static void ESVillageGate(float x,float z)
        {
            foreach(float side in new[]{-1f,1f})ESSolid("Village lane timber gateway",new Vector3(x+side*5.55f,1.75f,z),new Vector3(.34f,3.5f,.42f),Dark);
            Box("Village gateway crosspiece",new Vector3(x,3.44f,z),new Vector3(11.5f,.25f,.4f),Timber);
            Box("Village wooden nameboard",new Vector3(x,3.35f,z-.24f),new Vector3(2.4f,.58f,.1f),Dark);
            SignText("榕溪村",new Vector3(x,3.35f,z-.31f),.12f);
        }
        static void ESBoundaryGarden(float x,float z,int count)
        {
            for(int i=0;i<count;i++)if(ESAllowed(x,z+i*1.7f,1.4f,1.4f))Shrub(new Vector3(x,.4f,z+i*1.7f),.9f);
        }
        static void ESBedRow(float x,float z,int count)
        {
            var frame=new List<Vector3>();var ft=new List<int>();var mats=new List<Vector3>();var mt=new List<int>();
            var cloth=new List<Vector3>();var ct=new List<int>();
            for(int i=0;i<count;i++)
            {
                ESGuard(x,z+i*6.1f,2.7f,3.4f,"communal sleeping area");
                foreach(float side in new[]{-1f,1f})
                {
                    Vector3 p=new Vector3(x+side*.65f,.39f,z+i*6.1f);
                    ESSolid("Individual 1m by 2m sleeping cot",p,new Vector3(.98f,.11f,2.03f),Dark);
                    DetailBox(mats,mt,p+Vector3.up*.075f,new Vector3(.90f,.042f,1.95f));
                    DetailBox(cloth,ct,p+new Vector3(0,.15f,.73f),new Vector3(.55f,.11f,.31f));
                    DetailBox(cloth,ct,p+new Vector3(0,.12f,-.61f),new Vector3(.84f,.07f,.60f));
                    foreach(float a in new[]{-1f,1f})foreach(float b in new[]{-1f,1f})
                        DetailBox(frame,ft,p+new Vector3(a*.39f,-.205f,b*.86f),new Vector3(.085f,.37f,.085f));
                    for(int strip=0;strip<5;strip++)DetailBox(frame,ft,p+new Vector3(0,.099f,-.8f+strip*.4f),new Vector3(.90f,.008f,.013f));
                }
            }
            DetailMesh("Cot legs and woven mat bindings",frame,ft,Vector3.zero,Timber);
            DetailMesh("Individual adult sleeping mats",mats,mt,Vector3.zero,Mat("worn sleeping straw","A39369"));
            DetailMesh("Small pillows and folded cotton blankets",cloth,ct,Vector3.zero,Linen);
        }
        static void ESWashArea(float x,float z)
        {
            if(!ESAllowed(x,z,2.5f,2.1f))return;
            ESSolid("Shared masonry wash basin",new Vector3(x,.36f,z),new Vector3(1.9f,.72f,.88f),Stone);
            Box("Washbasin recessed dark water",new Vector3(x,.726f,z),new Vector3(1.64f,.018f,.63f),Mat("basin water","647A6B"));
            ESJar(x-2,z+.8f,.47f);
            Box("Wooden washboard",new Vector3(x,.79f,z-.08f),new Vector3(.33f,.055f,.58f),Timber).transform.localRotation=Quaternion.Euler(-14,0,0);
        }
        static void ESStove(float x,float z)
        {
            if(!ESAllowed(x,z,2.4f,1.7f))return;
            ESSolid("Shared brick cooking stove",new Vector3(x,.39f,z),new Vector3(1.35f,.78f,.93f),Brick);
            Box("Firemouth soot",new Vector3(x,.28f,z-.48f),new Vector3(.42f,.31f,.03f),Shadow);
            Cylinder("Iron cooking pot",new Vector3(x,.85f,z),.34f,.18f,Mat("seasoned cooking iron","3C3932"));
            Cylinder("Wooden pot lid",new Vector3(x,.956f,z),.32f,.035f,Dark);
        }
    }
}
