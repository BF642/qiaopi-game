using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        bool TouchButton(Rect r,string title,bool primary=false,int size=32)
        {
            return LetterButton(r,title,primary,size);
        }
        void MobileHUD(SceneData scene)
        {
            Rect safe=MobileUiBounds();float x=safe.xMin+30,right=safe.xMax-30;
            LetterHeading(new Rect(x,22,safe.width-60,112),true);
            Label(new Rect(x+106,84,850,37),"盘缠 "+state.money+"   身体 "+state.health+"   亲情 "+state.family+"   信用 "+state.trust,24,sub);
            if(NavTab(new Rect(right-790,30,195,96),"侨批匣","letter",journal,29)){journal=!journal;map=help=gallery=false;journalScroll=Vector2.zero;}
            if(NavTab(new Rect(right-585,30,180,96),"图集","album",gallery,29)){if(gallery)gallery=false;else OpenGallery();}
            if(NavTab(new Rect(right-390,30,180,96),"地图","map",map,29)){map=!map;journal=help=gallery=false;journalScroll=Vector2.zero;}
            if(NavTab(new Rect(right-195,30,185,96),"帮助","help",help,29)){help=!help;gallery=false;}
            if(Blocked)return;
            PaperPanel(new Rect(x,157,660,167),false,.94f);Box(new Rect(x,157,3,167),letterSeal);
            Label(new Rect(x+22,172,616,36),scene.year+" · "+scene.title,28,red,true);
            Label(new Rect(x+22,222,616,88),Complete?"事情办妥，去找"+mission.npcName+"。":mission.objective,30,ink,true);
            Label(new Rect(right-640,157,640,55),region.title,28,paper,true,TextAnchor.MiddleRight);
        }
        void MobileDialogue(SceneData scene)
        {
            var current=CurrentLine;var who=CharacterRoster.Get(current.speakerId);Rect safe=MobileUiBounds();
            Rect panel=new Rect(safe.xMin+30,362,safe.width-60,600);PaperPanel(panel);Box(new Rect(panel.x,panel.y,panel.width,3),letterSeal);
            float left=panel.x+24,choices=panel.xMax-630,bodyX=left+296,bodyW=choices-bodyX-32;
            Label(new Rect(left,379,panel.width-210,52),scene.title+" · "+scene.location,28,sub);
            if(TouchButton(new Rect(panel.xMax-169,376,145,76),"收起",false,28)){dialogue=false;voiceSource.Stop();}
            Rect portrait=new Rect(left,449,264,329);Box(portrait,C("D4CBB6"));
            if(portraitTexture)GUI.DrawTexture(portrait,portraitTexture,ScaleMode.ScaleToFit,false);
            Label(new Rect(left,791,264,53),who.name,38,ink,true,TextAnchor.MiddleCenter);
            Label(new Rect(left,854,264,70),who.role,25,sub,false,TextAnchor.UpperCenter);
            Label(new Rect(bodyX,449,bodyW,53),current.speakerId=="narrator"?"这一刻，你记得……":who.name+"说：",36,red,true);
            string body=showSituation?scene.body:current.text;float h=Height(body,36,bodyW-30,true);
            speechScroll=GUI.BeginScrollView(new Rect(bodyX,518,bodyW,257),speechScroll,new Rect(0,0,bodyW-30,Mathf.Max(252,h+8)),false,false);
            Label(new Rect(0,0,bodyW-30,h+8),body,36,ink,true);GUI.EndScrollView();
            Label(new Rect(bodyX,788,bodyW,42),(spokenIndex+1)+" / "+spokenLines.Count+"  ·  "+VoiceStatus,25,sub);
            if(TouchButton(new Rect(bodyX,848,bodyW,88),showSituation?"回到台词":"查看情境",false,28)){showSituation=!showSituation;speechScroll=Vector2.zero;}
            if(!LastDialogueLine){
                Label(new Rect(choices,474,587,167),state.awaitingContinue?"你的回答已记下。\n看看这次选择留下的结果。":"读完这段话，再作决定。\n每个人都有自己的牵挂。",32,sub,true);
                if(TouchButton(new Rect(choices,674,599,122),"下一句",true,36))NextDialogueLine();
                if(TouchButton(new Rect(choices,824,599,112),"直接到最后一句",false,30)){spokenIndex=spokenLines.Count-1;playedKey="";speechScroll=Vector2.zero;voiceSource.Stop();}
                return;
            }
            if(state.awaitingContinue){
                Label(new Rect(choices,474,587,219),"这一笔，已记下。\n"+state.lastChanges,32,sub,true);
                if(TouchButton(new Rect(choices,789,599,147),"继续旅程",true,36))Next();return;
            }
            if(scene.isEnding){
                Label(new Rect(choices,474,587,154),"一段属于你的南洋人生",36,sub,true);
                if(TouchButton(new Rect(choices,647,599,126),"读最后一封信",true)){journal=true;dialogue=false;journalScroll=Vector2.zero;}
                if(TouchButton(new Rect(choices,810,599,126),"重新从泉州启程"))resetAsk=true;return;
            }
            for(int i=0;i<scene.choices.Count;i++){
                var choice=scene.choices[i];float y=466+i*157;Rect button=new Rect(choices,y,599,142);
                Box(button,C("EADFCA"));Box(new Rect(button.x,button.y,3,button.height),letterSeal);bool enabled=GUI.enabled;GUI.enabled=enabled&&choice.enabled;
                Label(new Rect(choices+20,y+12,559,43),choice.title,31,choice.enabled?ink:sub,true);
                Label(new Rect(choices+20,y+64,559,66),choice.enabled?choice.hint:choice.unavailableReason,25,sub);
                if(GUI.Button(button,GUIContent.none,blank)){Choose(choice.id);voiceSource.Stop();}GUI.enabled=enabled;
            }
        }
        void MobileHelp()
        {
            Rect safe=MobileUiBounds();Rect r=new Rect(safe.xMin+60,158,safe.width-120,794);PaperPanel(r);Box(new Rect(r.x,r.y,r.width,3),letterSeal);
            float x=r.x+45;Label(new Rect(x,188,r.width-90,65),"亲自走一程，再写一封批。",42,ink,true);
            Label(new Rect(x,283,r.width-90,277),"左侧摇杆：行走　　右侧空白处滑动：转头、抬头、低头\n快走：切换步行速度　　看向目标：面向当前目的地\n互动：走近金色标记后，交谈、拾取行囊或交付侨批\n对话中点「下一句」继续，读完后直接点你的回答。\n顶部可打开地图、侨批匣和图集；完成互动后自动保存。",34,ink,true);
            float width=(r.width-130)/3;
            if(TouchButton(new Rect(x,612,width,105),musicDirector.MusicEnabled?"音乐：开启":"音乐：关闭",false,30))musicDirector.SetEnabled(!musicDirector.MusicEnabled);
            if(TouchButton(new Rect(x+width+20,612,width,105),soundEnabled?"环境声：开启":"环境声：关闭",false,30)){soundEnabled=!soundEnabled;WorldAtmosphere.SetSoundEnabled(soundEnabled);PlayerPrefs.SetInt("qiaopi-sound-enabled",soundEnabled?1:0);}
            bool voiceControlsEnabled=GUI.enabled;GUI.enabled=voiceControlsEnabled&&voiceAvailable;
            if(TouchButton(new Rect(x+(width+20)*2,612,width,105),VoiceToggleTitle,false,30)){voiceEnabled=!voiceEnabled;PlayerPrefs.SetInt("qiaopi-voice-enabled",voiceEnabled?1:0);if(!voiceEnabled)voiceSource.Stop();}
            GUI.enabled=voiceControlsEnabled;
            Label(new Rect(x,741,r.width-90,52),"1906 年，从泉州晋江启程。人物与情节虚构。",28,sub);
            if(TouchButton(new Rect(x,817,(r.width-120)/2,104),"继续探索",true,34))help=false;
            if(TouchButton(new Rect(r.center.x+15,817,(r.width-120)/2,104),"重新启程",false,34)){resetAsk=true;help=false;}
        }
    }
}
