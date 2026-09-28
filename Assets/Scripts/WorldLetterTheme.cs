using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        Texture2D letterPaper;
        readonly Color letterInk=C("294440"),letterSeal=C("A34837"),letterGold=C("B49B64");

        void EnsureLetterPaper()
        {
            if(letterPaper)return;
            const int size=128;letterPaper=new Texture2D(size,size,TextureFormat.RGBA32,false);
            letterPaper.name="批纸纤维";letterPaper.wrapMode=TextureWrapMode.Repeat;letterPaper.filterMode=FilterMode.Bilinear;
            var pixels=new Color[size*size];var random=new System.Random(192);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float fibre=Mathf.Sin(x*.53f+y*.057f)*.004f+(float)random.NextDouble()*.023f;
                float grain=.976f+fibre;if(x%31==0&&y%7<3)grain-=.020f;
                pixels[y*size+x]=new Color(grain,grain,grain,1);
            }
            letterPaper.SetPixels(pixels);letterPaper.Apply(false,true);
        }
        void PaperPanel(Rect r,bool framed=true,float opacity=1f)
        {
            EnsureLetterPaper();
            Box(new Rect(r.x+3,r.y+5,r.width,r.height),new Color(.10f,.16f,.13f,.12f*opacity));
            Color old=GUI.color;GUI.color=new Color(paper.r,paper.g,paper.b,opacity);
            GUI.DrawTextureWithTexCoords(r,letterPaper,new Rect(0,0,r.width/190f,r.height/190f));GUI.color=old;
            if(framed){
                Stroke(r,new Color(line.r,line.g,line.b,.86f*opacity));
                Box(new Rect(r.x+7,r.y+7,r.width-14,1),new Color(1,.97f,.86f,.65f*opacity));
                Box(new Rect(r.x+7,r.yMax-7,r.width-14,1),new Color(.53f,.42f,.26f,.15f*opacity));
            }
        }
        void InkLine(Vector2 from,Vector2 to,Color color,float thickness=1.6f)
        {
            Matrix4x4 original=GUI.matrix;Vector2 direction=to-from;
            GUI.matrix=original*Matrix4x4.TRS(new Vector3(from.x,from.y,0),Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg),Vector3.one);
            Box(new Rect(0,-thickness*.5f,direction.magnitude,thickness),color);GUI.matrix=original;
        }
        void Stamp(Rect r,string text,int size=24)
        {
            Box(r,letterSeal);Stroke(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),new Color(.96f,.83f,.65f,.74f));
            int fitted=Mathf.Min(size,Mathf.FloorToInt((r.width-10)/Mathf.Max(1,text.Length)));
            bool wrap=serif.wordWrap;serif.wordWrap=false;
            Label(new Rect(r.x+4,r.y+3,r.width-8,r.height-6),text,fitted,paper,true,TextAnchor.MiddleCenter);
            serif.wordWrap=wrap;
            // Small breaks in the impression keep the seal from looking like a plastic tile.
            Box(new Rect(r.x+5,r.y,5,1.5f),paper);Box(new Rect(r.xMax-1.5f,r.yMax-11,1.5f,5),paper);
        }
        void LetterIcon(Rect r,string icon,Color color)
        {
            float x=r.x,y=r.y,w=r.width,h=r.height;
            if(icon=="letter"){
                Stroke(new Rect(x,y+h*.15f,w,h*.70f),color);
                InkLine(new Vector2(x,y+h*.16f),new Vector2(x+w*.5f,y+h*.56f),color);
                InkLine(new Vector2(x+w,y+h*.16f),new Vector2(x+w*.5f,y+h*.56f),color);
                InkLine(new Vector2(x,y+h*.85f),new Vector2(x+w*.31f,y+h*.52f),color);
                InkLine(new Vector2(x+w,y+h*.85f),new Vector2(x+w*.69f,y+h*.52f),color);
            }else if(icon=="album"){
                Stroke(new Rect(x+w*.09f,y,w*.82f,h),color);Box(new Rect(x+w*.27f,y,1.7f,h),color);
                Stroke(new Rect(x+w*.40f,y+h*.22f,w*.33f,h*.37f),color);
                Box(new Rect(x+w*.4f,y+h*.74f,w*.35f,1.7f),color);
            }else if(icon=="map"){
                InkLine(new Vector2(x+w*.5f,y),new Vector2(x+w,y+h*.5f),color);
                InkLine(new Vector2(x+w,y+h*.5f),new Vector2(x+w*.5f,y+h),color);
                InkLine(new Vector2(x+w*.5f,y+h),new Vector2(x,y+h*.5f),color);
                InkLine(new Vector2(x,y+h*.5f),new Vector2(x+w*.5f,y),color);
                InkLine(new Vector2(x+w*.5f,y+h*.18f),new Vector2(x+w*.5f,y+h*.82f),color,2.7f);
                InkLine(new Vector2(x+w*.23f,y+h*.5f),new Vector2(x+w*.77f,y+h*.5f),color);
            }else{
                Stroke(new Rect(x+w*.13f,y,w*.74f,h),color);
                Label(r,"?",Mathf.RoundToInt(h*.8f),color,true,TextAnchor.MiddleCenter);
            }
        }
        bool NavTab(Rect r,string text,string icon,bool selected,int size=25)
        {
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            if(selected||hover)Box(r,selected?new Color(.64f,.28f,.21f,.10f):new Color(.18f,.30f,.25f,.06f));
            Color c=selected?letterSeal:letterInk;float side=Mathf.Min(34,r.height*.49f);
            LetterIcon(new Rect(r.x+12,r.center.y-side*.5f,side,side),icon,c);
            Label(new Rect(r.x+side+24,r.y,r.width-side-29,r.height-3),text,size,c,true,TextAnchor.MiddleCenter);
            if(selected)Box(new Rect(r.x+10,r.yMax-4,r.width-20,3),letterSeal);
            return GUI.Button(r,GUIContent.none,blank);
        }
        bool LetterButton(Rect r,string title,bool primary=false,int size=22)
        {
            if(QA&&Event.current.isMouse&&title.Contains("侨批"))Debug.Log("QIAOPI_HIT "+title+" "+Event.current.type+" mouse="+Event.current.mousePosition+" rect="+r+" enabled="+GUI.enabled+" hot="+GUIUtility.hotControl);
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            if(primary){
                Box(new Rect(r.x+2,r.y+3,r.width,r.height),new Color(.16f,.13f,.10f,.19f));
                Box(r,hover?C("873C30"):letterSeal);
                Stroke(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),new Color(.98f,.87f,.67f,.42f));
            }else{
                Box(r,hover?C("E4D5B8"):C("EADFCA"));
                Box(new Rect(r.x,r.y,3,r.height),letterInk);
                Box(new Rect(r.x,r.yMax-1,r.width,1),new Color(.40f,.40f,.28f,.25f));
            }
            Label(new Rect(r.x+12,r.y+3,r.width-24,r.height-6),title,size,primary?paper:letterInk,true,TextAnchor.MiddleCenter);
            return GUI.Button(r,GUIContent.none,blank);
        }
        void LetterHeading(Rect r,bool mobile)
        {
            PaperPanel(r,false,.97f);Box(new Rect(r.x,r.y,Mathf.Min(440,r.width*.30f),3),letterSeal);
            float seal=mobile?63:42;float pad=mobile?23:14;
            Stamp(new Rect(r.x+pad,r.y+(r.height-seal)*.5f,seal,seal),"侨批",mobile?27:18);
            Label(new Rect(r.x+pad+seal+19,r.y+(mobile?12:10),mobile?590:290,mobile?49:42),"纸短情长",mobile?37:28,ink,true);
        }
    }
}
