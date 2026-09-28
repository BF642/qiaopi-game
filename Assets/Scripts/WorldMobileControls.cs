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
        bool UiBlocksTouch(Vector2 p)=>new Rect(MobileUiBounds().xMax-820,22,800,112).Contains(p)||(LifeJourney.HasReachedOverseas(state)&&LifeEntryRect().Contains(p));

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
            TouchCircle(center,StickRadius+3,new Color(.96f,.93f,.84f,.21f));
            TouchCircle(center,StickRadius,new Color(.10f,.20f,.17f,.16f));
            Vector2 knob=center+new Vector2(mobileMove.x,-mobileMove.y)*StickRadius*.70f;
            TouchCircle(knob,34,new Color(.97f,.94f,.85f,moveFinger>=0?.72f:.40f));
            string prompt=Prompt();bool available=!string.IsNullOrEmpty(prompt);
            Rect action=TouchInteractRect();float radius=Mathf.Min(action.width,action.height)*.46f;
            TouchCircle(action.center,radius+2,new Color(hudGold.r,hudGold.g,hudGold.b,available?.62f:.22f));
            TouchCircle(action.center,radius,available?new Color(.57f,.23f,.17f,.55f):new Color(.09f,.17f,.14f,.22f));
            HudText(action,ContextActionName(prompt),available?33:28,available?hudText:hudMuted,true,TextAnchor.MiddleCenter);
            Rect run=TouchRunRect();HudSurface(run,mobileRun?.5f:.2f);HudText(run,mobileRun?"快走中":"快走",23,mobileRun?hudGold:hudMuted,false,TextAnchor.MiddleCenter);
            Rect reset=TouchResetRect();HudSurface(reset,.2f);HudText(reset,"朝向目标",22,hudMuted,false,TextAnchor.MiddleCenter);
        }
        string MobilePrompt()
        {
            string prompt=Prompt();return prompt.StartsWith("E  ")?prompt.Substring(3):prompt;
        }
    }
}
