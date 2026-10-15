using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>Inhabitable 2.7 extensions; all metres and all joins share the old ground plane.</summary>
    public static partial class WorldFactory
    {
        static void AddDistrictExpansion(string kind)
        {
            if(kind!="postoffice"&&kind!="quarters"&&kind!="quanzhou"&&kind!="port")return;
            Transform parent=world;
            var district=new GameObject("Connected working district · "+kind);
            district.transform.SetParent(parent,false);world=district.transform;
            try {
                if(kind=="port")DistrictPort();
                else {
                    Material earth=kind=="quanzhou"?Mat("expanded village earth","9E8A69"):
                        kind=="quarters"?Mat("quarters packed earth","A49479"):Mat("post courtyard earth","9B9079");
                    // Floors meet the existing edge; each extension has a real collider.
                    DistrictGround("North court ground",10,47,90,18,earth);
                    DistrictGround("East lane ground",46,3,20,70,earth);
                    DistrictLane("North connecting lane",0,39.8f,51.6f,39.8f,4.1f);
                    DistrictLane("North west lane",-32,39.8f,0,39.8f,4.1f);
                    DistrictLane("East inhabited lane",52,-27,52,51,4.1f);
                    DistrictLane("South street continuation",31,-25,52,-25,4.2f);
                    DistrictLane("Mid block cross lane",35,25,52,25,4.0f);
                    DistrictLane("East north passage",36,25,36,39.8f,3.2f);
                    DistrictBoundary(kind);
                    if(kind=="quanzhou")DistrictVillage();
                    else {
                        string route=kind=="quarters"?"trace_family":"trace_desk";
                        for(int i=0;i<3;i++)DistrictRoom(route,i);
                        DistrictLane("Rear room threshold",0,39.8f,0,51,3.5f);
                        DistrictLane("North room porch",7,39.8f,7,50,3.6f);
                        DistrictLane("East room front court",36,kind=="quarters"?3:5,52,kind=="quarters"?3:5,4.1f);
                        DistrictLane("North room approach",7,39.8f,51,39.8f,4.1f);
                        DistrictLane("Eastern shaded walk",38,20,52,20,3.3f);
                        DistrictGarden(new Vector3(-22,0,46),kind=="quarters");
                        DistrictLaundry(new Vector3(45,0,-10),kind=="quarters");
                        DistrictSign(new Vector3(0,0,39.8f),kind=="quarters"?"读信间 · 同乡后街":"留底账房 · 复核东巷",0);
                        DistrictSign(new Vector3(48,0,-24),kind=="quarters"?"同乡后街":"复核东巷",90);
                        DistrictSign(new Vector3(35,0,39.6f),kind=="quarters"?"读信间 ←   同乡院 →":"留底账房 ←   转交簿 →",0);
                    }
                }
                DistrictDailyWorkStations(kind);
            }
            finally { world=parent; }
        }

        static void DistrictGround(string label,float x,float z,float width,float depth,Material material)
        {
            Box(label+" substantial footing",new Vector3(x,-.42f,z),new Vector3(width,.72f,depth),Stone);
            Box(label+" walkable",new Vector3(x,-.10f,z),new Vector3(width,.20f,depth),material,true);
        }
        static void DistrictLane(string label,float ax,float az,float bx,float bz,float width)
        {
            float x=(ax+bx)*.5f,z=(az+bz)*.5f;
            bool vertical=Mathf.Abs(bz-az)>Mathf.Abs(bx-ax);
            float w=vertical?width:Mathf.Abs(bx-ax)+width,d=vertical?Mathf.Abs(bz-az)+width:width;
            HumanScalePaving(label,x,z,w,d,.025f,Mat("district worn hand cut paving","AAA38F"));
            // Thin runoff courses stay below a footstep, never form raised barriers.
            var stones=new HBSBatch();
            float length=vertical?d:w;
            for(float at=-length*.5f+.3f;at<length*.5f;at+=1.35f) {
                foreach(float side in new[]{-1f,1f}) {
                    Vector3 p=new Vector3(x+(vertical?side*width*.5f:at),.022f,z+(vertical?at:side*width*.5f));
                    stones.Box(p,vertical?new Vector3(.18f,.04f,1.23f):new Vector3(1.23f,.04f,.18f),Mat("district rain channel stone","818875"));
                }
            }
            stones.Flush(label+" flush drainage edge");
        }
        static void DistrictBoundary(string kind)
        {
            Material wall=kind=="quanzhou"?Brick:Mat("district repaired limewash","B8AB8E");
            DistrictWall("East property wall",new Vector3(55.55f,.59f,12),new Vector3(.30f,1.18f,86),wall);
            DistrictWall("North property wall west",new Vector3(-18.8f,.59f,55.5f),new Vector3(31.8f,1.18f,.30f),wall);
            DistrictWall("North property wall east",new Vector3(30.9f,.59f,55.5f),new Vector3(49.0f,1.18f,.30f),wall);
            DistrictWall("North court west wall",new Vector3(-35.5f,.56f,47),new Vector3(.30f,1.12f,16.6f),wall);
            // Low south edge binds the ground visually, beyond the saved playable rectangle.
            DistrictWall("East lane south wall",new Vector3(45.5f,.55f,-31.55f),new Vector3(20.2f,1.1f,.30f),wall);
        }
        static void DistrictWall(string label,Vector3 p,Vector3 size,Material material)
        {
            Box(label,p,size,material,true);
            bool alongX=size.x>size.z;
            Box(label+" granite footing",new Vector3(p.x,.17f,p.z),new Vector3(size.x+.04f,.34f,size.z+.04f),Stone);
            Box(label+" weathered coping",p+Vector3.up*(size.y*.5f+.035f),new Vector3(size.x+.055f,.07f,size.z+.055f),Mat("district wall coping","85887B"));
            // A few plaster repairs communicate age without hundreds of brick objects.
            if(size.y>2.0f) {
                var joints=new HBSBatch();
                for(int i=0;i<3;i++) {
                    Vector3 patch=p+new Vector3(alongX?(-.3f+i*.3f)*size.x:(size.x*.5f+.006f),-.50f+i*.13f,alongX?(-size.z*.5f-.006f):(-.3f+i*.3f)*size.z);
                    joints.Box(patch,alongX?new Vector3(.64f+i*.10f,.17f,.014f):new Vector3(.014f,.17f,.64f+i*.10f),Mat("district worn lime repair","C3B79B"));
                }
                joints.Flush(label+" lime repairs");
            }
        }
        static void DistrictRoom(string route,int index)
        {
            Vector3 point=InquiryPlaces.Evidence(route,index);
            Transform parent=world;
            var room=new GameObject(InquiryPlaces.Title(route,index)+" · enterable workroom");
            room.transform.SetParent(parent,false);room.transform.localPosition=point;world=room.transform;
            try {
                bool domestic=route=="trace_family",harbor=route=="trace_harbor";
                var plaster=domestic?Mat("district residential lime plaster","C0B197"):
                    harbor?Mat("district cargo weathered brick","A77D63"):Mat("district archive lime plaster","BBAF97");
                int floorIndex=pavingSurfaces.Count;
                HumanScalePaving("Interior hand laid stone floor",0,2.5f,10.6f,10,.033f,Mat("district interior granite","ADA28A"));
                // Paving registration is normally world-aligned; these floors are
                // inside a translated workroom transform.
                for(int surfaceIndex=floorIndex;surfaceIndex<pavingSurfaces.Count;surfaceIndex++) {
                    var surface=pavingSurfaces[surfaceIndex];surface.center=world.TransformPoint(surface.center);pavingSurfaces[surfaceIndex]=surface;
                }
                DistrictWall("Workroom rear masonry",new Vector3(0,1.43f,7.5f),new Vector3(10.6f,2.86f,.28f),plaster);
                foreach(float side in new[]{-1f,1f}) {
                    // Two actual side windows, with masonry sill and lintel rather than
                    // black rectangles laid on an unbroken wall.
                    DistrictWall("Workroom side front",new Vector3(side*5.3f,1.43f,-.4f),new Vector3(.28f,2.86f,4.2f),plaster);
                    DistrictWall("Workroom side back",new Vector3(side*5.3f,1.43f,5.5f),new Vector3(.28f,2.86f,4.0f),plaster);
                    DistrictWall("Side window lower course",new Vector3(side*5.3f,.45f,2.6f),new Vector3(.28f,.9f,1.8f),plaster);
                    DistrictWall("Side window lintel",new Vector3(side*5.3f,2.68f,2.6f),new Vector3(.28f,.36f,1.8f),plaster);
                    for(int bar=0;bar<5;bar++)Box("Timber window lattice",new Vector3(side*5.3f,1.72f,1.88f+bar*.36f),new Vector3(.065f,1.58f,.065f),Dark);
                    DistrictWall("Front doorway return",new Vector3(side*3.62f,1.43f,-2.5f),new Vector3(3.36f,2.86f,.28f),plaster);
                    Box("Open door jamb",new Vector3(side*1.91f,1.39f,-2.56f),new Vector3(.13f,2.78f,.19f),Dark,true);
                    // Open leaves are fixed flat against front returns; they cannot
                    // clip the approaching player or swing shut over a saved position.
                    PlankFace("Open worn timber door",new Vector3(side*2.64f,1.28f,-2.69f),1.22f,2.48f,Timber,7);
                    Box("Porch post",new Vector3(side*4.9f,1.49f,-3.65f),new Vector3(.18f,2.98f,.18f),Dark,true);
                    Box("Porch post stone shoe",new Vector3(side*4.9f,.17f,-3.65f),new Vector3(.32f,.34f,.32f),Stone);
                }
                Box("Door lintel",new Vector3(0,2.78f,-2.55f),new Vector3(3.95f,.18f,.22f),Dark);
                DistrictRoofInfill(plaster);
                DepthGallery("Tiled room roof and rafters",new Vector3(0,0,2.2f),11.4f,11.7f,3.32f,0);
                DepthGallery("Sheltered entrance porch",new Vector3(0,0,-3.28f),11.35f,2.36f,3.10f,0);
                Box("Workroom old timber sign",new Vector3(0,2.99f,-3.98f),new Vector3(2.7f,.38f,.10f),Dark);
                SignText(InquiryPlaces.Title(route,index),new Vector3(0,2.99f,-4.044f),.14f);
                // The investigation stand-point is (0,0,0); the document goes on this
                // real .78m desk. No paper here competes with the live evidence object.
                PostTable("Evidence reading desk",0,1.25f,1.65f,.76f,false,.78f);
                PostBrushes(new Vector3(.63f,.78f,1.39f));
                Box("Desk inkstone",new Vector3(-.62f,.795f,1.38f),new Vector3(.15f,.03f,.095f),Shadow);
                PostTable(domestic?"Household side chest":"Side sorting bench",-3.8f,5.8f,1.75f,.74f,true,.86f);
                PostPaperStack(new Vector3(-3.8f,.86f,5.8f),.43f,.31f,5);
                PostBench(3.8f,4.9f,1.9f,true);
                if(domestic) {
                    PostBed(-3.5f,3.35f);
                    Clothesline(new Vector3(1.9f,0,6.7f),new Vector3(4.8f,0,6.7f));
                    DepthDomesticDetails(new Vector3(4.4f,0,.1f),false);
                }
                else {
                    PostMailRack(1.5f,6.4f,4.5f,1.9f,index==0?"旧底":index==1?"转交":"留存");
                    PostBundleStand(-3.9f,.9f,1.1f,1.1f);
                    DistrictAbacus(new Vector3(-3.8f,.86f,5.65f));
                    if(harbor) {
                        EHCoil(new Vector3(3.85f,.08f,1.6f),.30f,4);
                        Box("Cargo tally board",new Vector3(3.8f,1.70f,7.32f),new Vector3(1.48f,.91f,.045f),Dark);
                        SignText(index==0?"点收留凭":index==1?"经手记名":"船期留底",new Vector3(3.8f,1.70f,7.285f),.10f);
                    }
                }
                // Interior light comes through the open door and real window holes.
                // No per-room realtime lights or post-processing cost is added.
            }
            finally { world=parent; }
        }
        static void DistrictRoofInfill(Material masonry)
        {
            // Fill the gables all the way to the roof underside; looking up from
            // inside must not reveal open sky between the walls and the tiles.
            const float bottom=2.86f,front=-2.5f,rear=7.5f,ridge=2.2f,peak=4.81f;
            float frontTop=3.16f+1.65f*(1-Mathf.Abs(front-ridge)/5.85f);
            float rearTop=3.16f+1.65f*(1-Mathf.Abs(rear-ridge)/5.85f);
            var infill=new HBSBatch();
            infill.Box(new Vector3(0,(bottom+frontTop)*.5f,front),new Vector3(10.6f,frontTop-bottom,.28f),masonry);
            infill.Box(new Vector3(0,(bottom+rearTop)*.5f,rear),new Vector3(10.6f,rearTop-bottom,.28f),masonry);
            foreach(float side in new[]{-1f,1f})foreach(float face in new[]{-.14f,.14f}) {
                float x=side*5.3f+face;
                Vector3 a=new Vector3(x,bottom,front),b=new Vector3(x,frontTop,front),c=new Vector3(x,peak,ridge),d=new Vector3(x,bottom,ridge);
                infill.Face(a,b,c,d,masonry);infill.Face(d,c,b,a,masonry);
                a=new Vector3(x,bottom,ridge);b=new Vector3(x,peak,ridge);c=new Vector3(x,rearTop,rear);d=new Vector3(x,bottom,rear);
                infill.Face(a,b,c,d,masonry);infill.Face(d,c,b,a,masonry);
            }
            infill.Flush("Workroom closed masonry gables");
        }
        static void DistrictAbacus(Vector3 p)
        {
            var b=new HBSBatch();
            foreach(float side in new[]{-1f,1f})b.Box(p+new Vector3(0,.025f,side*.14f),new Vector3(.43f,.04f,.025f),Dark);
            foreach(float side in new[]{-1f,1f})b.Box(p+new Vector3(side*.22f,.025f,0),new Vector3(.025f,.04f,.30f),Dark);
            b.Box(p+new Vector3(0,.025f,.055f),new Vector3(.43f,.026f,.016f),Dark);
            for(int i=0;i<7;i++) {
                float x=-.18f+i*.06f;b.Beam(p+new Vector3(x,.024f,-.135f),p+new Vector3(x,.024f,.135f),.008f,Timber);
                for(int n=0;n<4;n++)b.Box(p+new Vector3(x,.03f,-.095f+n*.026f),new Vector3(.038f,.032f,.018f),Mat("abacus dark wood beads","493D2E"));
                b.Box(p+new Vector3(x,.03f,.098f),new Vector3(.038f,.032f,.020f),Dark);
            }
            b.Flush("Hand sized bamboo abacus");
        }
        static void DistrictGarden(Vector3 p,bool domestic)
        {
            HumanScalePaving("Working court beaten stone",p.x,p.z,20,12,.028f,Mat("district courtyard dusty stone","A69B83"));
            DistrictWell(p+new Vector3(-5,0,1));
            PostBench(p.x+3,p.z-1,2.2f,false);
            PostTable("Courtyard shaded tea table",p.x+3,p.z+1,1.6f,.8f,false,.74f);
            Cylinder("Covered clay teapot",p+new Vector3(3,.82f,1),.105f,.16f,Mat("tea pot clay","7C4E36"));
            DistrictPergola(p+new Vector3(3,0,1),7,7);
            if(domestic)Clothesline(p+new Vector3(-7,0,4.7f),p+new Vector3(1,0,4.7f));
            else {
                PostBundleStand(p.x+7,p.z+3.8f,2,1.2f);
                PostPaperStack(p+new Vector3(3,.75f,1),.4f,.3f,4);
            }
            DistrictLane("Court entrance",p.x,39.8f,p.x,p.z-2,3.5f);
        }
        static void DistrictLaundry(Vector3 p,bool domestic)
        {
            DistrictPergola(p,8,8);
            HumanScalePaving("East work court floor",p.x,p.z,10,13,.03f,Mat("district interior granite","ADA28A"));
            if(domestic) {
                Clothesline(p+new Vector3(-3,0,1.5f),p+new Vector3(3,0,1.5f));
                StoneJar(p+new Vector3(-3.3f,0,-2.6f),.54f,.86f,Mat("household water jar","756F57"));
                ESWashArea(p.x+2.7f,p.z-2.7f);
            }
            else {
                PostTable("Empty bag drying bench",p.x,p.z+2.8f,3.1f,.85f,false,.86f);
                for(int i=0;i<3;i++)PostBundle(new Vector3(p.x-.95f+i*.95f,.87f,p.z+2.8f),.65f,.24f);
                EHParcel(p+new Vector3(-3,0,-2.3f),.8f);
            }
            DistrictLane("Working court connection",p.x+2,p.z-4,52,p.z-4,3.3f);
        }
        static void DistrictPergola(Vector3 p,float width,float depth)
        {
            foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-depth*.5f,depth*.5f}) {
                Box("Timber shelter upright",p+new Vector3(x,1.46f,z),new Vector3(.17f,2.92f,.17f),Dark,true);
                Box("Timber shelter stone shoe",p+new Vector3(x,.13f,z),new Vector3(.3f,.26f,.3f),Stone);
            }
            DepthGallery("Practical clay tiled shelter",p,width+1,depth+1,3.07f,0);
        }
        static void DistrictWell(Vector3 p)
        {
            var b=new HBSBatch();
            const int segments=16;
            for(int i=0;i<segments;i++) {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments;
                Vector3 o1=new Vector3(Mathf.Cos(a)*.74f,.64f,Mathf.Sin(a)*.74f),o2=new Vector3(Mathf.Cos(c)*.74f,.64f,Mathf.Sin(c)*.74f);
                Vector3 i1=new Vector3(Mathf.Cos(a)*.52f,.64f,Mathf.Sin(a)*.52f),i2=new Vector3(Mathf.Cos(c)*.52f,.64f,Mathf.Sin(c)*.52f);
                b.Face(p+o1,p+o2,p+i2,p+i1,Stone);b.Face(p+o1-Vector3.up*.64f,p+o2-Vector3.up*.64f,p+o2,p+o1,Stone);
                b.Face(p+i1,p+i2,p+i2-Vector3.up*.6f,p+i1-Vector3.up*.6f,Shadow);
            }
            b.Flush("Stone well ring with open interior");
            Cylinder("Well safety collision",p+Vector3.up*.32f,.74f,.64f,Stone,true).GetComponent<Renderer>().enabled=false;
            Cylinder("Dark still well water",p+Vector3.up*.05f,.50f,.02f,Mat("well still water","59685B"));
            foreach(float side in new[]{-1f,1f})Box("Well rope frame",p+new Vector3(side*.97f,1.10f,0),new Vector3(.12f,2.2f,.12f),Dark,true);
            Beam("Well lifting beam",p+new Vector3(-1.08f,2.13f,0),p+new Vector3(1.08f,2.13f,0),.13f,Dark);
            Beam("Well hemp rope",p+new Vector3(.13f,2.13f,0),p+new Vector3(.13f,.35f,0),.023f,Timber);
            StoneJar(p+new Vector3(1.37f,0,-.35f),.24f,.38f,Mat("old bucket staves","82684A"));
        }
        static void DistrictSign(Vector3 p,string label,float yaw)
        {
            Transform parent=world;var sign=new GameObject(label+" direction board");sign.transform.SetParent(parent,false);sign.transform.localPosition=p;sign.transform.localRotation=Quaternion.Euler(0,yaw,0);world=sign.transform;
            try {
                Box("Wayfinding post",new Vector3(-1.85f,1.14f,0),new Vector3(.12f,2.28f,.12f),Dark,true);
                Box("Faded timber direction board",new Vector3(0,2.12f,0),new Vector3(3.85f,.46f,.10f),Dark);
                SignText(label,new Vector3(0,2.12f,-.066f),.10f);
            }finally {world=parent;}
        }
        static void DistrictVillage()
        {
            DistrictGarden(new Vector3(-22,0,46),true);
            DistrictSign(new Vector3(0,0,39.8f),"井埕 ←   晒谷东巷 →",0);
            DistrictLane("Village east cross lane",31,3,52,3,4.0f);
            DistrictLane("Village drying yard approach",37,3,37,29,4.0f);
            DistrictLane("Village drying yard north lane",37,29,52,29,4.0f);
            HumanScalePaving("East threshing court",44,17,15,20,.035f,Mat("sun warmed threshing stone","B4A384"));
            DistrictPergola(new Vector3(44,0,46),13,9);
            PostTable("Farm implement repair bench",40.3f,48.2f,2.5f,.78f,false,.81f);
            PostBench(48.6f,48,2.0f,true);
            DepthDomesticDetails(new Vector3(49,0,43.2f),false);
            for(int i=0;i<3;i++) {
                Vector3 a=new Vector3(43.5f+i*.4f,.10f,49.3f),c=new Vector3(43.5f+i*.4f,1.8f,50.1f);
                Beam("Stored bamboo farm tool",a,c,.045f,Timber);
                Box("Hand forged hoe blade",c+new Vector3(0,-.08f,-.13f),new Vector3(.16f,.035f,.28f),Mat("old agricultural iron","5D6258"));
            }
            DistrictSign(new Vector3(48,0,39),"晒谷埕 · 农具棚",0);
            // Raised planted strips are tangible small plots, leaving a full loop
            // around the work yard rather than filling the new ground with props.
            DistrictPlot(new Vector3(43,0,-12),12,14);
            DistrictPlot(new Vector3(15,0,48),14,7);
            var straw=new HBSBatch();
            for(int i=0;i<4;i++) {
                Vector3 p=new Vector3(42+(i%2)*3.7f,.055f,13+(i/2)*5.0f);
                straw.Box(p,new Vector3(2.4f,.07f,3.1f),Mat("sun drying woven mat","A89361"));
                for(int j=0;j<9;j++)straw.Box(p+new Vector3(-1.04f+j*.26f,.045f,0),new Vector3(.10f,.06f,2.8f),Mat("sun drying loose grain","B2A26A"));
            }
            straw.Flush("Grain laid out on four drying mats");
            PostBench(38.6f,33,2.2f,false);
        }
        static void DistrictPlot(Vector3 p,float width,float depth)
        {
            Box("Raised planted earth",p+Vector3.up*.13f,new Vector3(width,.26f,depth),Mat("cultivated village soil","827458"),true);
            var plants=new HBSBatch();
            for(float x=-width*.5f+.7f;x<width*.5f;x+=1.1f)for(float z=-depth*.5f+.7f;z<depth*.5f;z+=1.0f) {
                Vector3 a=p+new Vector3(x,.27f,z);
                plants.Face(a+new Vector3(-.22f,0,0),a+new Vector3(0,.34f,.06f),a+new Vector3(.22f,0,0),a+new Vector3(0,.18f,-.04f),Mat("vegetable young leaves","697B4C"));
                plants.Face(a+new Vector3(0,0,-.22f),a+new Vector3(.04f,.31f,0),a+new Vector3(0,0,.22f),a+new Vector3(-.04f,.17f,0),Mat("vegetable young leaves","697B4C"));
            }
            plants.Flush("Small household vegetable plot");
        }
        static void DistrictDailyWorkStations(string kind)
        {
            if(kind=="quanzhou") {
                StoneJar(new Vector3(35.7f,0,3),.32f,.48f,Mat("woven rice carrier","A08A61"));
                StoneJar(new Vector3(50.7f,0,29),.32f,.48f,Mat("woven rice carrier","A08A61"));
                return; // The second worker draws from the real well and serves its tea table.
            }
            if(kind=="port") {
                PostTable("South cargo handover stand",-39.4f,-20,.7f,.7f,false,.68f);
                EHParcel(new Vector3(-39.4f,.69f,-20),.48f);
                PostTable("North cargo receiving stand",-51.4f,5,.7f,.7f,false,.68f);
                EHParcel(new Vector3(-51.4f,.69f,5),.48f);
                StoneJar(new Vector3(-64.6f,0,-18),.34f,.76f,Mat("work crew water jar","766D51"));
                PostTable("South lane work crew tea table",-53,-38.4f,1.0f,.65f,false,.73f);
                Cylinder("Work crew covered tea pot",new Vector3(-53,.81f,-38.4f),.11f,.16f,Mat("tea pot clay","7C4E36"));
                return;
            }
            StoneJar(new Vector3(50.7f,0,-20),.34f,.76f,Mat("district shared water jar","766D51"));
            PostTable("Lane tea handover table",39.4f,25,.70f,1.10f,false,.73f);
            Cylinder("Lane teapot",new Vector3(39.4f,.81f,25),.11f,.16f,Mat("tea pot clay","7C4E36"));
            if(kind=="quarters") {
                PostTable("Rear lane firewood stand",-1.4f,39,.65f,.70f,false,.42f);
                var wood=new HBSBatch();
                for(int i=0;i<6;i++)wood.Beam(new Vector3(-1.6f+(i%3)*.19f,.46f+(i/3)*.10f,38.72f),new Vector3(-1.6f+(i%3)*.19f,.46f+(i/3)*.10f,39.27f),.09f,Timber);
                wood.Flush("Dry firewood for back lane kitchen");
                // The stove faces west so the carrier stands at (7,49), not in it.
                Box("Rear lane shared brick stove",new Vector3(8.4f,.39f,49),new Vector3(.90f,.78f,1.25f),Brick,true);
                Box("Rear lane stove firemouth",new Vector3(7.935f,.29f,49),new Vector3(.025f,.31f,.43f),Shadow);
                Cylinder("Rear lane iron cooking pot",new Vector3(8.4f,.87f,49),.34f,.18f,Mat("seasoned cooking iron","3C3932"));
                Cylinder("Rear lane pot lid",new Vector3(8.4f,.97f,49),.31f,.03f,Dark);
                DepthGallery("Back lane stove weather cover",new Vector3(8.5f,0,49),2.0f,2.0f,2.93f,0);
            }
            else {
                PostBundleStand(-1.4f,39,.70f,.70f);
                PostTable("North lane incoming dispatch stand",8.4f,49,.7f,.8f,false,.80f);
                PostBundle(new Vector3(8.4f,.81f,49),.55f,.28f);
            }
        }
        static void DistrictPort()
        {
            Material earth=Mat("port packed earth","9F927B");
            DistrictGround("Western landward cargo court",-53, -10.5f,34,67,earth);
            DistrictGround("Southern cargo street",0,-38,72,12,earth);
            DistrictLane("Landward lane",-38,-40,-38,20,4.0f);
            DistrictLane("Western warehouse lane",-67,-40,-67,20,3.1f);
            DistrictLane("Southern cargo street paving",-67,-40,32,-40,4.1f);
            DistrictLane("Cargo western cross lane",-67,-20,-31,-20,4.1f);
            DistrictLane("Cargo northern cross lane",-67,5,-31,5,4.1f);
            DistrictLane("Port room approach",-50,-20,-50,10,3.1f);
            DistrictLane("Port room south approach",-67,-14,-52,-14,3.1f);
            for(int i=0;i<3;i++)DistrictRoom("trace_harbor",i);
            DistrictWall("Landward cargo wall",new Vector3(-69.55f,.67f,-10.5f),new Vector3(.32f,1.34f,66.4f),Mat("cargo district worn limewash","AEA489"));
            DistrictWall("South cargo court boundary",new Vector3(-18,.64f,-43.55f),new Vector3(102,1.28f,.32f),Mat("cargo district worn limewash","AEA489"));
            // The tidal bank remains at the old level. No new floor covers the
            // east shipping water or the northern shipping lane.
            EHQuayFace(23.1f,-69,-36,false);
            DistrictWall("Western shore low parapet",new Vector3(-53,.46f,23.3f),new Vector3(33,.92f,.27f),Stone);
            DistrictSign(new Vector3(-35,0,-20),"货栈经手间 ←",0);
            DistrictSign(new Vector3(-38,0,4),"点收间 ↑   船期簿 ↓",0);
            DistrictSign(new Vector3(-13,0,-35),"船期簿房 ←   货港 →",0);
            DistrictPergola(new Vector3(-59,0,-25),10,7);
            PostTable("Rope and sail repair bench",-60,-24,3.5f,.78f,false,.81f);
            EHCoil(new Vector3(-60,.82f,-24),.25f,3);
            PostBench(-55.6f,-27,1.9f,true);
            EHParcel(new Vector3(-64,0,17),1.2f);
            EHParcel(new Vector3(-61.5f,0,17),.9f);
        }
    }
}
