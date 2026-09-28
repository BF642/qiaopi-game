using System;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        bool MobileControls=>Application.isMobilePlatform||Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-touch-preview")>=0;
        bool mobileTestRequested;
        bool MobileQA=>mobileTestRequested||Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-mobile-test")>=0;
        Vector2 mobileMove,moveOrigin,lookPrevious;
        int moveFinger=-1,lookFinger=-1;
        bool mobileRun,mobileInteract;
        Texture2D touchDisc;
        const float StickRadius=108f;

        void SetupMobileDefaults()
        {
            if(!Application.isMobilePlatform)return;
            Screen.sleepTimeout=SleepTimeout.NeverSleep;
            string testMarker=System.IO.Path.Combine(Application.persistentDataPath,"run-mobile-test.txt");
            mobileTestRequested=System.IO.File.Exists(testMarker);
            if(mobileTestRequested)System.IO.File.Delete(testMarker);
            Screen.orientation=ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft=Screen.autorotateToLandscapeRight=true;
            Screen.autorotateToPortrait=Screen.autorotateToPortraitUpsideDown=false;
            if(!PlayerPrefs.HasKey("qiaopi-mobile-initialized")){
                PlayerPrefs.SetInt("qiaopi-sound-enabled",0);
                PlayerPrefs.SetInt("qiaopi-voice-enabled",0);
                PlayerPrefs.SetInt("qiaopi-music-enabled",0);
                PlayerPrefs.SetInt("qiaopi-mobile-initialized",1);PlayerPrefs.Save();
            }
        }

        void GetUiMetrics(out float k,out float x,out float y)
        {
            Rect safe=MobileControls?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
            if(safe.width<1||safe.height<1)safe=new Rect(0,0,Screen.width,Screen.height);
            k=Mathf.Min(safe.width/W,safe.height/H);
            x=safe.x+(safe.width-W*k)*.5f;
            y=Screen.height-safe.yMax+(safe.height-H*k)*.5f;
        }
        Vector2 TouchUiPoint(Vector2 screenPoint)
        {
            GetUiMetrics(out float k,out float x,out float y);
            return new Vector2((screenPoint.x-x)/k,(Screen.height-screenPoint.y-y)/k);
        }
        Rect MobileUiBounds()
        {
            GetUiMetrics(out float k,out float x,out float y);Rect safe=Screen.safeArea;
            return new Rect((safe.x-x)/k,(Screen.height-safe.yMax-y)/k,safe.width/k,safe.height/k);
        }
        Vector2 StickHome(){Rect r=MobileUiBounds();return new Vector2(r.xMin+175,H-198);}
        Rect TouchInteractRect(){Rect r=MobileUiBounds();return new Rect(r.xMax-226,H-320,184,176);}
        Rect TouchRunRect(){Rect r=MobileUiBounds();return new Rect(r.xMin+65,H-413,220,72);}
        Rect TouchResetRect(){Rect r=MobileUiBounds();return new Rect(r.xMax-226,H-436,184,78);}
        bool UiBlocksTouch(Vector2 p)=>p.y<145||p.y>965||(p.x<MobileUiBounds().xMin+710&&p.y<340)||(LifeJourney.HasReachedOverseas(state)&&LifeEntryRect().Contains(p));

        void ResetMobileControls()
        {
            mobileMove=Vector2.zero;moveFinger=lookFinger=-1;mobileInteract=false;
        }
        void HandleMobileTouch(int finger,Vector2 p,TouchPhase phase)
        {
            if(Blocked){ResetMobileControls();return;}
            if(phase==TouchPhase.Ended||phase==TouchPhase.Canceled){
                if(finger==moveFinger){moveFinger=-1;mobileMove=Vector2.zero;}
                if(finger==lookFinger)lookFinger=-1;
                return;
            }
            if(phase==TouchPhase.Began){
                if(TouchInteractRect().Contains(p)){mobileInteract=true;return;}
                if(TouchRunRect().Contains(p)){mobileRun=!mobileRun;return;}
                if(TouchResetRect().Contains(p)){ResetView();return;}
                if(UiBlocksTouch(p))return;
                if(p.x<W*.44f&&p.y>420&&moveFinger<0){moveFinger=finger;moveOrigin=p;mobileMove=Vector2.zero;return;}
                if(p.x>=W*.44f&&lookFinger<0){lookFinger=finger;lookPrevious=p;return;}
            }
            if(finger==moveFinger){Vector2 delta=(p-moveOrigin)/StickRadius;mobileMove=Vector2.ClampMagnitude(new Vector2(delta.x,-delta.y),1);if(mobileMove.magnitude<.10f)mobileMove=Vector2.zero;}
            if(finger==lookFinger){Vector2 delta=Vector2.ClampMagnitude(p-lookPrevious,180);lookPrevious=p;RotateView(delta*.18f);}
        }
        void UpdateMobileInput()
        {
            if(!MobileControls||DetailQA)return;
            if(Blocked||!Application.isFocused){ResetMobileControls();return;}
            for(int i=0;i<Input.touchCount;i++){
                Touch touch=Input.GetTouch(i);HandleMobileTouch(touch.fingerId,TouchUiPoint(touch.position),touch.phase);
            }
            if(Input.touchCount==0){moveFinger=lookFinger=-1;mobileMove=Vector2.zero;}
        }
        bool ConsumeMobileInteract(){bool pressed=mobileInteract;mobileInteract=false;return pressed;}

        void TouchCircle(Vector2 center,float radius,Color color)
        {
            if(!touchDisc){
                touchDisc=new Texture2D(96,96,TextureFormat.RGBA32,false);touchDisc.name="触屏摇杆";
                var pixels=new Color[96*96];for(int y=0;y<96;y++)for(int x=0;x<96;x++){
                    float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(48,48));pixels[y*96+x]=new Color(1,1,1,Mathf.Clamp01(48-d));
                }
                touchDisc.SetPixels(pixels);touchDisc.Apply();
            }
            Color old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(center.x-radius,center.y-radius,radius*2,radius*2),touchDisc);GUI.color=old;
        }
        void DrawMobileControls()
        {
            Vector2 center=moveFinger>=0?moveOrigin:StickHome();
            TouchCircle(center,StickRadius+5,new Color(.96f,.93f,.84f,.38f));
            TouchCircle(center,StickRadius,new Color(.12f,.24f,.20f,.40f));
            Vector2 knob=center+new Vector2(mobileMove.x,-mobileMove.y)*StickRadius*.70f;
            TouchCircle(knob,39,new Color(.97f,.94f,.85f,.80f));
            Label(new Rect(center.x-95,center.y+StickRadius+12,190,40),"移动",24,paper,false,TextAnchor.MiddleCenter);
            Rect action=TouchInteractRect();Box(action,letterSeal);Stroke(new Rect(action.x+6,action.y+6,action.width-12,action.height-12),new Color(.98f,.88f,.69f,.64f));
            Label(new Rect(action.x,action.y+24,action.width,50),"互动",34,paper,true,TextAnchor.MiddleCenter);
            Label(new Rect(action.x+9,action.y+88,action.width-18,55),"交谈 · 取放",21,paper,false,TextAnchor.MiddleCenter);
            Rect run=TouchRunRect();Box(run,new Color(.12f,.24f,.20f,.68f));Label(run,mobileRun?"快走中":"快走",24,paper,false,TextAnchor.MiddleCenter);
            Rect reset=TouchResetRect();Box(reset,new Color(.12f,.24f,.20f,.68f));Label(reset,"看向目标",23,paper,false,TextAnchor.MiddleCenter);
            Label(new Rect(930,585,340,45),"在右侧空白处滑动转头",22,paper,false,TextAnchor.MiddleCenter);
        }
        string MobilePrompt()
        {
            string prompt=Prompt();return prompt.StartsWith("E  ")?"点击「互动」 · "+prompt.Substring(3):prompt.Replace("M 打开本区地图","点上方「地图」查看");
        }
    }
}
