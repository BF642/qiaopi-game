using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // Period-inspired ordinary construction, not a reconstruction of any specific property.
    // Detail meshes have no colliders; mission movement remains owned by the original shells.
    public static partial class WorldFactory
    {
        static Material Paper => Mat("unbleached letter paper", "E7DFC8");
        static Material Linen => Mat("unbleached linen", "BCA980");
        static Material Shadow => Mat("unlit recess", "302A24");
        static int pavingLayer;

        static void DetailBox(List<Vector3> v,List<int> t,Vector3 p,Vector3 size,Quaternion rotation)
        {
            Vector3 a=rotation*new Vector3(-size.x,-size.y,-size.z)*.5f+p;
            Vector3 b=rotation*new Vector3(size.x,-size.y,-size.z)*.5f+p;
            Vector3 c=rotation*new Vector3(size.x,size.y,-size.z)*.5f+p;
            Vector3 d=rotation*new Vector3(-size.x,size.y,-size.z)*.5f+p;
            Vector3 e=rotation*new Vector3(-size.x,-size.y,size.z)*.5f+p;
            Vector3 f=rotation*new Vector3(size.x,-size.y,size.z)*.5f+p;
            Vector3 g=rotation*new Vector3(size.x,size.y,size.z)*.5f+p;
            Vector3 h=rotation*new Vector3(-size.x,size.y,size.z)*.5f+p;
            Quad(v,t,a,d,c,b);Quad(v,t,e,f,g,h);Quad(v,t,a,e,h,d);Quad(v,t,b,c,g,f);Quad(v,t,d,h,g,c);Quad(v,t,a,b,f,e);
        }
        static void DetailBox(List<Vector3> v,List<int> t,Vector3 p,Vector3 size) => DetailBox(v,t,p,size,Quaternion.identity);
        static void DetailBeam(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,float width,float depth)
        {
            DetailBox(v,t,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),depth),Quaternion.FromToRotation(Vector3.up,(b-a).normalized));
        }
        static void DetailMesh(string label,List<Vector3> v,List<int> t,Vector3 p,Material material)
        {
            if(v.Count>0) MeshObject(label,FlatMesh(v,t),p,material);
        }
        static void GranitePaving(float x,float z,float width,float depth)
        {
            float top=.016f+(pavingLayer++*.0045f);
            HumanScalePaving("Old granite lane",x,z,width,depth,top,Mat("path","B1AA97"));
        }
        // Ordinary slabs are roughly a forearm long, not human-sized floor tiles.
        // Their shallow bevels, narrow joints and wear are merged by material.
        static void HumanScalePaving(string label,float x,float z,float width,float depth,float top,Material stone)
        {
            Box(label+" recessed joint bed",new Vector3(x,top-.003f,z),new Vector3(width,.004f,depth),Mat("paving joints","827D6D"));
            var faces=new[]{new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};
            var indices=new[]{new List<int>(),new List<int>(),new List<int>()};
            var materials=new[]{stone,Mat("paving lightly worn granite","B8B1A0"),Mat("paving older grey granite","969789")};
            int rows=Mathf.Max(1,Mathf.CeilToInt(depth/.44f));
            float dz=depth/rows;
            for(int row=0;row<rows;row++)
            {
                float length=.55f+(row%3)*.035f;
                float near=-depth*.5f+row*dz+.005f,far=near+dz-.010f;
                float offset=(row%2)*length*.5f;
                for(float start=-width*.5f-offset;start<width*.5f;start+=length)
                {
                    float left=Mathf.Max(-width*.5f,start)+.005f,right=Mathf.Min(width*.5f,start+length)-.005f;
                    if(right-left<.018f)continue;
                    int col=Mathf.FloorToInt((start+width*.5f+length)/length);
                    int pattern=(row*13+col*7)%23,group=pattern<3?2:pattern<8?1:0;
                    float chip=.009f+(pattern%4)*.004f;
                    chip=Mathf.Min(chip,(right-left)*.19f);
                    float y=top+(pattern%3)*.00045f;
                    var v=faces[group];var t=indices[group];int at=v.Count;
                    // A subtly crowned, individually worn stone catches raking light.
                    // The existing nine-vertex fan supplies relief without extra triangles.
                    v.Add(new Vector3((left+right)*.5f,y+.006f+(pattern%4)*.0007f,(near+far)*.5f));
                    v.Add(new Vector3(left+chip,y,near));v.Add(new Vector3(left,y,near+chip));
                    v.Add(new Vector3(left,y,far-chip));v.Add(new Vector3(left+chip,y,far));
                    v.Add(new Vector3(right-chip,y,far));v.Add(new Vector3(right,y,far-chip));
                    v.Add(new Vector3(right,y,near+chip));v.Add(new Vector3(right-chip,y,near));
                    for(int edge=0;edge<8;edge++){t.Add(at);t.Add(at+1+edge);t.Add(at+1+(edge+1)%8);}
                    if(v.Count>57000){DetailMesh(label+" staggered granite "+group,v,t,new Vector3(x,0,z),materials[group]);v.Clear();t.Clear();}
                }
            }
            for(int group=0;group<3;group++)DetailMesh(label+" staggered granite "+group,faces[group],indices[group],new Vector3(x,0,z),materials[group]);
        }
        static void PlankFace(string label,Vector3 p,float width,float height,Material material,int count=9)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<count;i++)
                DetailBox(v,t,new Vector3(-width*.5f+(i+.5f)*width/count,0,0),new Vector3(width/count-.014f,height,.045f));
            DetailMesh(label,v,t,p,material);
        }
        static void HouseAge(float x,float z,float w,float d,float h,bool main)
        {
            float rise=Mathf.Min(1.65f,(d+1.05f)*.25f),y=h+.35f;
            var g=new List<Vector3>();var gt=new List<int>();
            foreach(float side in new[]{-1f,1f})
            {
                // Solid infill closes the former empty triangular hole under each gable.
                int at=g.Count;g.Add(new Vector3(side*w*.5f,0,-d*.5f));g.Add(new Vector3(side*w*.5f,rise,0));g.Add(new Vector3(side*w*.5f,0,d*.5f));
                if(side<0){gt.Add(at);gt.Add(at+2);gt.Add(at+1);}else{gt.Add(at);gt.Add(at+1);gt.Add(at+2);}
            }
            DetailMesh("Limewashed masonry gable infill",g,gt,new Vector3(x,y,z),Cream);
            var rafters=new List<Vector3>();var rt=new List<int>();
            int count=Mathf.CeilToInt(w/.43f);
            for(int i=0;i<count;i++)
            {
                float xx=-w*.48f+i*w*.96f/(count-1);
                foreach(float side in new[]{-1f,1f})
                    DetailBeam(rafters,rt,new Vector3(xx,h+.26f,side*(d*.5f-.25f)),new Vector3(xx,h+.14f,side*(d*.5f+.53f)),.09f,.1f);
            }
            DetailMesh("Exposed dark timber rafter ends",rafters,rt,new Vector3(x,0,z),Dark);
            Box("Deep shaded front eave",new Vector3(x,h+.14f,z-d*.5f-.14f),new Vector3(w,.25f,.31f),Shadow);
            // Rough lower stone courses and patchwork remain visible on side elevations.
            SideMasonry(new Vector3(x+w*.5f+.009f,.36f,z),d,h,1);
            SideMasonry(new Vector3(x-w*.5f-.009f,.36f,z),d,h,-1);
            if(!main)
            {
                var cap=new List<Vector3>();var ct=new List<int>();
                foreach(float side in new[]{-1f,1f})
                {
                    var profile=new Vector3[9];
                    for(int i=0;i<profile.Length;i++)
                    {
                        float a=i/8f;float zz=Mathf.Lerp(-d*.5f-.10f,d*.5f+.10f,a);
                        float yy=rise*(1-Mathf.Abs(a*2-1))+.10f;
                        profile[i]=new Vector3(side*(w*.5f+.03f),yy,zz);
                    }
                    AppendTube(cap,ct,profile,.10f,.10f);
                }
                DetailMesh("Plain hard-gable coping",cap,ct,new Vector3(x,y,z),Mat("gable coping","67675C"));
            }
        }
        static void SideMasonry(Vector3 p,float width,float height,float facing)
        {
            var stones=new List<Vector3>();var st=new List<int>();var bricks=new List<Vector3>();var bt=new List<int>();
            int stoneCols=Mathf.CeilToInt(width/.56f);
            for(int j=0;j<2;j++)for(int i=0;i<stoneCols;i++)
            {
                float lo=-width*.5f+i*width/stoneCols+.013f,hi=lo+width/stoneCols-.026f;
                float bottom=j*.22f+.009f,top=bottom+.20f;
                Vector3 a=new Vector3(0,bottom,lo),b=new Vector3(0,top-.008f,lo+.012f),c=new Vector3(0,top,hi),d=new Vector3(0,bottom+.007f,hi-.012f);
                if(facing>0)Quad(stones,st,a,b,c,d);else Quad(stones,st,d,c,b,a);
            }
            int cols=Mathf.CeilToInt(width/.25f),rows=Mathf.CeilToInt(Mathf.Max(.1f,height-.46f)/.087f);
            for(int j=0;j<rows;j++)for(int i=0;i<cols;i++)
            {
                if((i*3+j*7)%11>3)continue;
                float lo=-width*.5f+i*.25f+(j%2)*.125f+.006f,hi=Mathf.Min(width*.5f-.006f,lo+.238f);
                float bottom=.46f+j*.087f,top=Mathf.Min(height,bottom+.071f);
                if(hi<=lo||top<=bottom)continue;
                Vector3 a=new Vector3(0,bottom,lo),b=new Vector3(0,top,lo),c=new Vector3(0,top,hi),d=new Vector3(0,bottom,hi);
                if(facing>0)Quad(bricks,bt,a,b,c,d);else Quad(bricks,bt,d,c,b,a);
            }
            DetailMesh("Side elevation granite rubble courses",stones,st,p,Stone);
            DetailMesh("Side elevation aged brick repairs",bricks,bt,p,Mat("brick patch","BB6949"));
        }
        static void WoodenLatticeWindow(float x,float y,float z,float w,float h,bool stoneFrame=true)
        {
            var v=new List<Vector3>();var t=new List<int>();
            Material frame=stoneFrame?Stone:Timber;
            Box("Window deep unglazed recess",new Vector3(x,y,z),new Vector3(w+.05f,h+.05f,.11f),Shadow);
            DetailBox(v,t,new Vector3(-w*.5f-.065f,0,0),new Vector3(.13f,h+.26f,.20f));
            DetailBox(v,t,new Vector3(w*.5f+.065f,0,0),new Vector3(.13f,h+.26f,.20f));
            DetailBox(v,t,new Vector3(0,h*.5f+.065f,0),new Vector3(w+.26f,.13f,.20f));
            DetailBox(v,t,new Vector3(0,-h*.5f-.065f,-.045f),new Vector3(w+.35f,.13f,.31f));
            DetailMesh("Carved stone window surround",v,t,new Vector3(x,y,z-.075f),frame);
            v=new List<Vector3>();t=new List<int>();
            for(int i=-3;i<=3;i++)DetailBox(v,t,new Vector3(i*w*.13f,0,0),new Vector3(.04f,h,.055f));
            foreach(float yy in new[]{-.3f,0,.3f})DetailBox(v,t,new Vector3(0,h*yy,0),new Vector3(w,.045f,.057f));
            DetailMesh("Fine old wooden window lattice",v,t,new Vector3(x,y,z-.135f),Dark);
        }
        static void LouverWindow(Vector3 p,float w,float h,Material wood)
        {
            Box("Unglazed shophouse window shadow",p,new Vector3(w+.16f,h+.18f,.11f),Shadow);
            var frame=new List<Vector3>();var ft=new List<int>();
            foreach(float side in new[]{-1f,1f})DetailBox(frame,ft,new Vector3(side*w*.5f,0,0),new Vector3(.09f,h+.14f,.12f));
            foreach(float yy in new[]{-h*.5f,h*.5f})DetailBox(frame,ft,new Vector3(0,yy,0),new Vector3(w+.15f,.1f,.13f));
            DetailBox(frame,ft,Vector3.zero,new Vector3(.07f,h,.13f));
            DetailMesh("Timber shutter stile and rail",frame,ft,p+new Vector3(0,0,-.085f),Dark);
            var slats=new List<Vector3>();var st=new List<int>();
            int n=Mathf.RoundToInt(h/.15f);
            for(int j=0;j<n;j++)foreach(float side in new[]{-1f,1f})
                DetailBox(slats,st,new Vector3(side*w*.255f,-h*.5f+.12f+j*(h-.2f)/(n-1),0),new Vector3(w*.43f,.11f,.06f),Quaternion.Euler(-24,0,0));
            DetailMesh("Individual wooden louvres",slats,st,p+new Vector3(0,0,-.115f),wood);
        }
        static void BambooBlind(Vector3 p,float w,float h)
        {
            var v=new List<Vector3>();var t=new List<int>();int n=Mathf.CeilToInt(h/.07f);
            for(int i=0;i<n;i++)DetailBox(v,t,new Vector3(0,-i*h/n,0),new Vector3(w,.048f,.038f));
            DetailMesh("Hanging split-bamboo shade",v,t,p,Mat("split bamboo","9F8C62"));
            foreach(float x in new[]{-w*.32f,w*.32f})Beam("Blind binding cord",p+new Vector3(x,.05f,-.034f),p+new Vector3(x,-h,-.034f),.016f,Dark);
            Beam("Rolled bamboo lower edge",p+new Vector3(-w*.5f,-h,0),p+new Vector3(w*.5f,-h,0),.09f,Timber,true);
        }
        static void ClothShade(Vector3 p,float w,float d)
        {
            var v=new List<Vector3>();var t=new List<int>();
            const int cuts=8;
            for(int i=0;i<cuts;i++)
            {
                float a=-w*.5f+i*w/cuts,b=-w*.5f+(i+1)*w/cuts;
                float sagA=Mathf.Sin((i/(float)cuts)*Mathf.PI)*-.13f,sagB=Mathf.Sin(((i+1)/(float)cuts)*Mathf.PI)*-.13f;
                Vector3 aa=new Vector3(a,.11f+sagA,d*.5f),bb=new Vector3(b,.11f+sagB,d*.5f),cc=new Vector3(b,-.13f+sagB,-d*.5f),dd=new Vector3(a,-.13f+sagA,-d*.5f);
                Quad(v,t,aa,bb,cc,dd);Quad(v,t,dd,cc,bb,aa);
            }
            DetailMesh("Plain sewn linen sunshade",v,t,p,Linen);
            Beam("Bamboo shade front spar",p+new Vector3(-w*.5f,-.13f,-d*.5f),p+new Vector3(w*.5f,-.13f,-d*.5f),.065f,Timber,true);
            foreach(float x in new[]{-w*.5f,w*.5f})Beam("Sunshade tied support",p+new Vector3(x,-.13f,-d*.5f),p+new Vector3(x,.11f,d*.5f),.045f,Dark);
        }
        static void StoneJar(Vector3 p,float radius,float height,Material material)
        {
            float[] ys={0,.07f,height*.24f,height*.78f,height*.94f,height};
            float[] rs={radius*.61f,radius*.72f,radius,radius*.92f,radius*.69f,radius*.69f};
            var v=new List<Vector3>();var t=new List<int>();
            for(int j=0;j<ys.Length-1;j++)for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                Quad(v,t,new Vector3(Mathf.Cos(a)*rs[j],ys[j],Mathf.Sin(a)*rs[j]),new Vector3(Mathf.Cos(a)*rs[j+1],ys[j+1],Mathf.Sin(a)*rs[j+1]),new Vector3(Mathf.Cos(b)*rs[j+1],ys[j+1],Mathf.Sin(b)*rs[j+1]),new Vector3(Mathf.Cos(b)*rs[j],ys[j],Mathf.Sin(b)*rs[j]));
            }
            DetailMesh("Old hand-thrown storage jar",v,t,p,material);
            Cylinder("Open earthenware jar shadow",p+Vector3.up*(height-.02f),radius*.57f,.018f,Shadow);
        }
        static void GraniteQuayFacing()
        {
            var v=new List<Vector3>();var t=new List<int>();var darker=new List<Vector3>();var dt=new List<int>();
            for(int row=0;row<3;row++)for(int column=0;column<23;column++)
            {
                float x=-15.9f+column*1.39f+(row%2)*.3f;
                if(x>15.4f)continue;
                float width=Mathf.Min(1.32f,15.9f-x);
                bool wet=row==0;var vv=wet?darker:v;var tt=wet?dt:t;
                DetailBox(vv,tt,new Vector3(x+width*.5f,-.87f+row*.31f,7.985f),new Vector3(width,.285f,.08f));
            }
            DetailMesh("Hand-cut granite harbor revetment",v,t,Vector3.zero,Stone);
            DetailMesh("Tide-darkened lower granite course",darker,dt,Vector3.zero,Mat("wet granite","777D71"));
            var cap=new List<Vector3>();var ct=new List<int>();
            for(int i=0;i<12;i++)foreach(float side in new[]{-1f,1f})
                DetailBox(cap,ct,new Vector3(side*(7.05f+i*.73f),.018f,7.68f),new Vector3(.69f,.045f,.60f));
            DetailMesh("Worn granite quay coping blocks",cap,ct,Vector3.zero,Mat("quay coping","B7B09D"));
        }
        static void SaggingRope(Vector3 a,Vector3 b)
        {
            var points=new Vector3[11];for(int i=0;i<points.Length;i++){float u=i/10f;points[i]=Vector3.Lerp(a,b,u)-Vector3.up*(Mathf.Sin(u*Mathf.PI)*.22f);}
            var v=new List<Vector3>();var t=new List<int>();AppendTube(v,t,points,.029f,.029f);
            DetailMesh("Slack hemp mooring rope",v,t,Vector3.zero,Mat("rope","AD9B72"));
        }
        static void Warehouse(float x,float z,float w,float d,float h)
        {
            // These two collider boxes exactly match the former warehouse-house footprint.
            Box("Raised granite footing",new Vector3(x,.2f,z),new Vector3(w+.18f,.4f,d+.16f),Stone,true);
            Box("Redbrick house",new Vector3(x,h*.5f+.35f,z),new Vector3(w,h,d),Mat("warehouse lime plaster","C3B595"),true);
            RoofMesh(x,h+.35f,z,w+.95f,d+1.05f,false);HouseAge(x,z,w,d,h,false);
            float front=z-d*.5f-.075f;
            Box("Warehouse loading door deep opening",new Vector3(x,1.55f,front),new Vector3(w-.7f,2.75f,.12f),Shadow);
            PlankFace("Broad aged plank warehouse doors",new Vector3(x,1.55f,front-.082f),w-.92f,2.7f,Timber,12);
            var v=new List<Vector3>();var t=new List<int>();
            foreach(float side in new[]{-1f,1f})
            {
                DetailBox(v,t,new Vector3(side*(w*.5f-.29f),1.55f,0),new Vector3(.22f,3.0f,.2f));
                DetailBeam(v,t,new Vector3(side*.10f,.45f,-.1f),new Vector3(side*(w*.5f-.65f),2.65f,-.1f),.12f,.07f);
            }
            DetailBox(v,t,new Vector3(0,3.1f,0),new Vector3(w-.28f,.23f,.24f));
            DetailMesh("Warehouse wooden jambs and diagonal braces",v,t,new Vector3(x,0,front-.14f),Dark);
            Box("Weathered warehouse signboard",new Vector3(x,h-.02f,front-.15f),new Vector3(2.0f,.40f,.10f),Dark);
            SignText("货栈",new Vector3(x,h-.02f,front-.22f),.085f);
            Brickwork(x,front+.012f,w,h,true);
        }
        static void ArchedBoatCanopy(Vector3 p,float w,float d)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;
                Vector3 aa=new Vector3(Mathf.Cos(a)*w*.5f,Mathf.Sin(a)*.66f,-d*.5f),bb=new Vector3(Mathf.Cos(b)*w*.5f,Mathf.Sin(b)*.66f,-d*.5f);
                Quad(v,t,aa,bb,bb+Vector3.forward*d,aa+Vector3.forward*d);
                Quad(v,t,aa+Vector3.forward*d,bb+Vector3.forward*d,bb,aa);
            }
            DetailMesh("Woven mat arched stern cabin canopy",v,t,p,Mat("boat matting","A99269"));
        }
    }
}
