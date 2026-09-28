using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Qiaopi
{
    public static class VoiceKeys
    {
        public static string ForText(string text)
        {
            using(var sha=SHA256.Create()) {
                var hash=sha.ComputeHash(Encoding.UTF8.GetBytes(text??""));var b=new StringBuilder("text_");
                for(int i=0;i<10;i++)b.Append(hash[i].ToString("x2"));return b.ToString();
            }
        }
    }
    public partial class WorldGame
    {
        List<DialogueLine> spokenLines=new List<DialogueLine>();
        int spokenIndex;
        string conversationKey="",playedKey="",portraitId="";
        AudioSource voiceSource;
        bool voiceEnabled=true,voiceAvailable,showSituation;
        string voiceClipKey="";
        AudioClip currentVoiceClip;
        GameObject portraitStage,portraitActor;
        Camera portraitCamera;
        RenderTexture portraitTexture;
        Vector2 speechScroll;
        const int PortraitLayer=30;
        bool LastDialogueLine=>spokenLines.Count==0||spokenIndex>=spokenLines.Count-1;
        bool ConversationVisible=>dialogue&&!journal&&!map&&!help&&!puzzle&&!resetAsk&&!fieldNote&&!gallery&&!letterEditor&&!lifePanel;
        DialogueLine CurrentLine=>spokenLines.Count==0?null:spokenLines[Mathf.Clamp(spokenIndex,0,spokenLines.Count-1)];
        AudioClip CurrentVoiceClip
        {
            get {
                string key=CurrentLine==null?"":CurrentLine.clipKey;
                if(voiceClipKey!=key){voiceClipKey=key;currentVoiceClip=string.IsNullOrEmpty(key)?null:Resources.Load<AudioClip>("Voice/"+key);}
                return currentVoiceClip;
            }
        }
        string VoiceStatus=>!CurrentVoiceClip?"本句暂无配音":!voiceEnabled?"语音已关闭":voiceSource&&voiceSource.isPlaying?"正在说话  ▂ ▅ ▃ ▆":"本句已读完";
        string VoiceToggleTitle=>!voiceAvailable?"语音：暂无配音":voiceEnabled?"语音：开启":"语音：关闭";
        void SetupConversation()
        {
            voiceAvailable=Resources.LoadAll<AudioClip>("Voice").Length>0;
            voiceEnabled=voiceAvailable&&PlayerPrefs.GetInt("qiaopi-voice-enabled",1)==1;
            voiceSource=gameObject.AddComponent<AudioSource>();voiceSource.volume=.92f;voiceSource.spatialBlend=0;voiceSource.playOnAwake=false;
            portraitStage=new GameObject("对话人物摄影棚");portraitStage.transform.position=new Vector3(800,0,800);
            var cameraObject=new GameObject("半身特写镜头");cameraObject.transform.SetParent(portraitStage.transform,false);
            portraitCamera=cameraObject.AddComponent<Camera>();portraitCamera.enabled=false;portraitCamera.cullingMask=1<<PortraitLayer;
            portraitCamera.orthographic=true;portraitCamera.orthographicSize=.82f;portraitCamera.nearClipPlane=.1f;portraitCamera.farClipPlane=12;
            portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=C("D4CBB6");
            portraitTexture=new RenderTexture(520,640,24,RenderTextureFormat.ARGB32);portraitTexture.name="当前说话人实时特写";portraitTexture.Create();portraitCamera.targetTexture=portraitTexture;
            var lightObject=new GameObject("人物柔光");lightObject.transform.SetParent(portraitStage.transform,false);lightObject.transform.localRotation=Quaternion.Euler(20,160,0);
            var portraitLight=lightObject.AddComponent<Light>();portraitLight.type=LightType.Directional;portraitLight.cullingMask=1<<PortraitLayer;portraitLight.intensity=1.1f;portraitLight.color=C("FFF2DA");portraitLight.shadows=LightShadows.None;
        }
        void ResetConversation()
        {
            conversationKey="";playedKey="";spokenIndex=0;showSituation=false;spokenLines.Clear();if(voiceSource)voiceSource.Stop();
        }
        void EnsureConversation()
        {
            string key=state.nodeId+":"+state.decisions+":"+state.awaitingContinue;
            if(key==conversationKey)return;
            conversationKey=key;playedKey="";spokenIndex=0;speechScroll=Vector2.zero;showSituation=false;
            if(state.awaitingContinue){
                spokenLines=new List<DialogueLine>();
                if(state.history.Count>0){string answer=state.history[state.history.Count-1].choice;spokenLines.Add(new DialogueLine{speakerId="wensheng",text=answer,clipKey=VoiceKeys.ForText(answer)});}
                spokenLines.Add(new DialogueLine{speakerId="narrator",text=state.lastOutcome,clipKey=VoiceKeys.ForText(state.lastOutcome)});
            }else spokenLines=DialogueScript.Get(state);
            if(voiceSource)voiceSource.Stop();
        }
        void UpdateConversation()
        {
            if(!ConversationVisible){if(voiceSource&&voiceSource.isPlaying)voiceSource.Stop();playedKey="";return;}
            EnsureConversation();var line=CurrentLine;if(line==null)return;
            ShowPortrait(line.speakerId);
            if(playedKey!=line.clipKey){playedKey=line.clipKey;PlayCurrentVoice();}
        }
        void PlayCurrentVoice()
        {
            voiceSource.Stop();var line=CurrentLine;if(line==null)return;
            voiceSource.clip=CurrentVoiceClip;
            if(voiceEnabled&&voiceSource.clip)voiceSource.Play();
        }
        void NextDialogueLine()
        {
            if(LastDialogueLine)return;spokenIndex++;playedKey="";speechScroll=Vector2.zero;showSituation=false;voiceSource.Stop();
        }
        void ShowPortrait(string id)
        {
            if(portraitId==id&&portraitActor)return;
            portraitId=id;if(portraitActor){portraitActor.SetActive(false);Destroy(portraitActor);}
            var character=CharacterRoster.Get(id);
            portraitActor=ModelLibrary.Spawn(character.model,portraitStage.transform,Vector3.zero);
            if(!portraitActor)return;
            foreach(var t in portraitActor.GetComponentsInChildren<Transform>())t.gameObject.layer=PortraitLayer;
            foreach(var r in portraitActor.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            portraitActor.AddComponent<CharacterIdle>();
            float height=ModelLibrary.CharacterHeight(character.model);
            float head=height*.74f;
            portraitCamera.transform.localPosition=new Vector3(.08f,head,3.2f);portraitCamera.transform.LookAt(portraitStage.transform.position+new Vector3(0,head,0));
            portraitCamera.orthographicSize=height*.39f;
        }
        void LateUpdate()
        {
            if(!ConversationVisible||!portraitActor||!portraitCamera)return;
            float talking=voiceEnabled&&voiceSource.isPlaying?1:0;
            portraitActor.transform.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*.7f)*1.8f+Mathf.Sin(Time.time*2.4f)*talking,0);
            portraitCamera.Render();
        }
        void Dialogue(SceneData scene)
        {
            EnsureConversation();var current=CurrentLine;if(current==null)return;
            if(MobileControls){MobileDialogue(scene);return;}
            var who=CharacterRoster.Get(current.speakerId);
            Rect panel=new Rect(34,486,1532,480);PaperPanel(panel);Box(new Rect(panel.x,panel.y,panel.width,3),letterSeal);
            Label(new Rect(60,502,920,30),scene.title+"  ·  "+scene.year+"  /  "+scene.location,16,sub);
            if(SmallButton(new Rect(1450,500,84,36),"收起")){dialogue=false;voiceSource.Stop();}
            Rect portrait=new Rect(60,549,240,296);Box(portrait,C("D4CBB6"));
            if(portraitTexture)GUI.DrawTexture(portrait,portraitTexture,ScaleMode.ScaleToFit,false);Stroke(portrait,C("B4A88B"));
            Label(new Rect(64,857,232,41),who.name,28,ink,true,TextAnchor.MiddleCenter);
            Label(new Rect(61,902,238,39),who.role,15,sub,false,TextAnchor.UpperCenter);
            Label(new Rect(326,551,655,42),current.speakerId=="narrator"?"这一刻，你记得……":who.name+"说：",26,red,true);
            string body=showSituation?scene.body:current.text;
            float h=Height(body,24,633,true);
            speechScroll=GUI.BeginScrollView(new Rect(326,610,653,204),speechScroll,new Rect(0,0,630,Mathf.Max(202,h+8)),false,false);
            Label(new Rect(0,0,630,h+8),body,24,ink,true);GUI.EndScrollView();
            Label(new Rect(329,826,590,27),(spokenIndex+1)+" / "+spokenLines.Count+"  ·  "+VoiceStatus,16,sub);
            bool voiceControlsEnabled=GUI.enabled;GUI.enabled=voiceControlsEnabled&&CurrentVoiceClip;
            if(SmallButton(new Rect(327,862,131,36),"重听本句"))PlayCurrentVoice();
            GUI.enabled=voiceControlsEnabled&&voiceAvailable;
            if(SmallButton(new Rect(472,862,145,36),VoiceToggleTitle)){
                voiceEnabled=!voiceEnabled;PlayerPrefs.SetInt("qiaopi-voice-enabled",voiceEnabled?1:0);if(voiceEnabled)PlayCurrentVoice();else voiceSource.Stop();
            }
            GUI.enabled=voiceControlsEnabled;
            if(SmallButton(new Rect(641,862,138,36),showSituation?"回到台词":"查看情境")){showSituation=!showSituation;speechScroll=Vector2.zero;}
            if(!LastDialogueLine){if(Button(new Rect(793,856,188,53),"下一句  ↵",true))NextDialogueLine();}
            else Label(new Rect(795,863,180,33),scene.isEnding?"故事终章":state.awaitingContinue?"继续旅程 →":"选择你的回答 →",17,red);
            Label(new Rect(327,921,651,26),state.awaitingContinue?state.lastChanges:"Enter / 空格：下一句     数字 1—3：选择回答",15,sub);
            if(!LastDialogueLine){
                Label(new Rect(1020,558,474,57),state.awaitingContinue?"你的回答已经记下。":"读完这段话，再作决定。",23,sub,true);
                Label(new Rect(1021,638,469,131),state.awaitingContinue?"接下来，看看这次选择\n留下了怎样的结果。":"不同的人，有各自的牵挂。\n按「下一句」继续交谈。",21,sub,true);
                if(Button(new Rect(1020,826,480,68),state.awaitingContinue?"查看这次选择的结果":"直接到最后一句",false)){spokenIndex=spokenLines.Count-1;playedKey="";speechScroll=Vector2.zero;voiceSource.Stop();}
                return;
            }
            if(state.awaitingContinue){Label(new Rect(1020,568,476,115),"这一笔，已记下。\n接下来，亲自去走下一程。",23,sub,true);if(Button(new Rect(1010,758,490,77),"继续旅程  →",true))Next();return;}
            if(scene.isEnding){Label(new Rect(1010,578,487,100),state.decisions+" 次关键抉择\n一段属于你的南洋人生",25,sub,true);if(Button(new Rect(1010,739,490,65),"读最后一封信",true)){journal=true;dialogue=false;journalScroll=Vector2.zero;}if(Button(new Rect(1010,826,490,65),"重新从泉州启程  R",false))resetAsk=true;return;}
            float y=553;for(int i=0;i<scene.choices.Count;i++){
                var c=scene.choices[i];Rect r=new Rect(1009,y,493,104);bool hover=r.Contains(Event.current.mousePosition)&&c.enabled;
                Box(r,hover?C("E1D8BF"):C("EEE6D2"));Stroke(r,hover?C("8C9375"):line);
                bool enabled=GUI.enabled;GUI.enabled=enabled&&c.enabled;
                Label(new Rect(1023,y+12,34,36),(i+1).ToString(),24,c.enabled?red:sub,true);
                Label(new Rect(1066,y+9,418,39),c.title,20,c.enabled?ink:sub,true);
                Label(new Rect(1066,y+50,418,47),c.enabled?c.hint:c.unavailableReason,15,sub);
                if(GUI.Button(r,GUIContent.none,blank)){Choose(c.id);voiceSource.Stop();}GUI.enabled=enabled;y+=119;
            }
        }
        void OnDestroy()
        {
            CancelCameraPointer();
            if(letterPaper)Destroy(letterPaper);if(touchDisc)Destroy(touchDisc);
            if(landscapeSky){if(RenderSettings.skybox==landscapeSky)RenderSettings.skybox=null;Destroy(landscapeSky);}
            if(portraitTexture){portraitTexture.Release();Destroy(portraitTexture);}
            if(portraitStage)Destroy(portraitStage);
        }
    }
}
