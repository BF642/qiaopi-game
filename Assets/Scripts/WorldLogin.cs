using System;
using System.Collections;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool loginVisible, loginLoading;
        Texture2D loginPoster;
        GUIStyle loginButtonStyle, loginHintStyle;
        string loginError;
        void ShowLogin()
        {
            loginVisible=true;
            loginPoster=Resources.Load<Texture2D>("Art/LoginPoster");
            Cursor.lockState=CursorLockMode.None;
            Cursor.visible=true;
            // Do not create the world, advance tasks, or touch saved audio preferences before entry.
            Debug.Log("QIAOPI_LOGIN_READY");
        }
        Rect LoginButtonRect()
        {
            float w=Screen.width,h=w*941f/1672f;
            if(h>Screen.height){h=Screen.height;w=h*1672f/941f;}
            float k=w/1672f;
            return new Rect((Screen.width-w)*.5f+111*k,(Screen.height-h)*.5f+704*k,320*k,66*k);
        }
        bool loginPointerPressed;
        void UpdateLoginInput()
        {
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))RequestLogin();
            var pointer=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            if(Input.GetMouseButtonDown(0)){loginPointerPressed=LoginButtonRect().Contains(pointer);}
            if(Input.GetMouseButtonUp(0)){
                if(loginPointerPressed&&LoginButtonRect().Contains(pointer))RequestLogin();
                loginPointerPressed=false;
            }
        }
        void RequestLogin()
        {
            if(loginLoading)return;
            loginLoading=true;StartCoroutine(LoginAndEnter());
        }
        void DrawLogin()
        {
            GUI.matrix=Matrix4x4.identity; GUI.color=Color.white;
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,ScaleMode.StretchToFill,false,0,C("282A26"),0,0);
            float width=Screen.width, height=width*941f/1672f;
            if(height>Screen.height){height=Screen.height;width=height*1672f/941f;}
            Rect art=new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height);
            if(loginPoster)GUI.DrawTexture(art,loginPoster,ScaleMode.StretchToFill);
            if(loginButtonStyle==null){
                loginButtonStyle=new GUIStyle(GUI.skin.button){font=titleFont??font,fontSize=28,alignment=TextAnchor.MiddleCenter};
                loginButtonStyle.normal.textColor=C("FFF2D4");loginButtonStyle.hover.textColor=Color.white;loginButtonStyle.active.textColor=Color.white;
                loginButtonStyle.normal.background=Texture2D.whiteTexture;loginButtonStyle.hover.background=Texture2D.whiteTexture;loginButtonStyle.active.background=Texture2D.whiteTexture;
                loginHintStyle=new GUIStyle(GUI.skin.label){font=font,alignment=TextAnchor.MiddleCenter,wordWrap=true};
                loginHintStyle.normal.textColor=C("53362A");
            }
            float k=width/1672f;
            Rect button=new Rect(art.x+111*k,art.y+704*k,320*k,66*k);
            loginButtonStyle.fontSize=Mathf.Max(18,Mathf.RoundToInt(30*k));
            GUI.backgroundColor=loginLoading?C("726759"):C("8F3929");GUI.enabled=!loginLoading;
            bool enter=GUI.Button(button,loginLoading?"正在启程…":"登  录",loginButtonStyle);
            GUI.enabled=true;GUI.backgroundColor=Color.white;
            loginHintStyle.fontSize=Mathf.Max(12,Mathf.RoundToInt(16*k));
            GUI.Label(new Rect(art.x+46*k,art.y+780*k,450*k,50*k),loginError??"点击登录或按回车，开启旅程",loginHintStyle);
            if(enter)RequestLogin();
        }
        IEnumerator LoginAndEnter()
        {
            // Let the pressed button/loading state paint before loading the saved world.
            yield return null;yield return new WaitForEndOfFrame();
            try{BeginJourney();loginVisible=false;loginLoading=false;Debug.Log("QIAOPI_LOGIN_ENTERED node="+state.nodeId);}
            catch(Exception e){loginError="旅程载入失败，请重新打开游戏。";Debug.LogException(e);}
        }
    }
}
