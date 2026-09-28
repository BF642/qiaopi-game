using System;
using System.Collections;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        bool StoryDemo=>Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-story-demo")>=0;
        bool demoFinished;
        string demoCaption="完整剧情演示 · 泉州出发";
        IEnumerator DemoWait(float seconds)
        {
            float end=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<end)yield return null;
        }
        IEnumerator DemoRead(int index)
        {
            EnsureConversation();spokenIndex=Mathf.Clamp(index,0,spokenLines.Count-1);playedKey="";speechScroll=Vector2.zero;
            UpdateConversation();
            float duration=voiceSource.clip?voiceSource.clip.length/voiceSource.pitch:5;
            yield return DemoWait(duration+.4f);
        }
        IEnumerator RunStoryDemo()
        {
            // Keep the player's voice preference, including an already-muted classroom session.
            voiceSource.pitch=1.18f;
            string[] route="meal neighbors ledger guarantor mother share courier verify rest honest details copies desk reconcile settle stay steady".Split(' ');
            yield return DemoWait(1);
            for(int i=0;i<route.Length;i++){
                if(state.nodeId=="passage"&&!state.flags.Contains("aboard_passage")){state.flags.Add("aboard_passage");EnterNode();}
                if(LifeJourney.IsActive(state))
                {
                    // The demo accelerates physical travel just as it does the main missions.
                    foreach(string job in new[]{"courier","courier","rest"})
                    {
                        string result;
                        if(!LifeJourney.CommitAction(state,job,out result))throw new Exception("DEMO_LIVELIHOOD_UNAVAILABLE "+job);
                        demoCaption="自主谋生 "+state.journey.turns+" / 3 · "+(job=="rest"?"歇一日养身":"到信局接批送达");
                        Debug.Log("STORY_DEMO_LIVELIHOOD "+job+" "+result);
                        yield return DemoWait(2.5f);
                        if(LifeJourney.PendingHardship(state)!=null)
                        {
                            demoCaption=LifeJourney.PendingHardship(state).title+" · 留证，请同乡一起追问";
                            if(!LifeJourney.ResolveHardship(state,"evidence",out result))throw new Exception("DEMO_HARDSHIP_FAILED");
                            Debug.Log("STORY_DEMO_HARDSHIP "+result);
                            yield return DemoWait(3.5f);
                        }
                    }
                    if(!LifeJourney.Finish(state))throw new Exception("DEMO_LIVELIHOOD_INCOMPLETE");
                    EnterNode();
                }
                SceneData scene=StoryEngine.GetScene(state);
                demoCaption="完整剧情演示  "+(i+1)+" / 17  ·  "+scene.year+"  ·  "+scene.title;
                dialogue=false;progress=mission.required;carrying=false;sideCarrying=false;RefreshProps();
                controller.enabled=false;player.transform.position=mission.npcPosition+new Vector3(1.6f,.1f,-2);controller.enabled=true;
                player.transform.LookAt(new Vector3(mission.npcPosition.x,player.transform.position.y,mission.npcPosition.z));UpdateCamera(true);
                yield return DemoWait(1.2f);
                dialogue=true;yield return DemoRead(0);
                // Display the available decisions before taking the documented demo route.
                spokenIndex=spokenLines.Count-1;playedKey=CurrentLine.clipKey;voiceSource.Stop();
                var choice=scene.choices.Find(c=>c.id==route[i]);
                if(choice==null||!choice.enabled)throw new Exception("DEMO_UNAVAILABLE "+state.nodeId+"/"+route[i]);
                demoCaption="演示选择："+choice.title;
                yield return DemoWait(2);
                Choose(choice.id);EnsureConversation();
                // The choice is on screen; then read its actual consequence in full.
                yield return DemoRead(spokenLines.Count-1);
                Debug.Log("STORY_DEMO_STEP "+(i+1)+" "+state.nodeId+" -> "+state.nextNodeId+" choice="+choice.id);
                Next();
            }
            var ending=StoryEngine.GetScene(state);
            if(!ending.isEnding||state.decisions!=17)throw new Exception("DEMO_INCOMPLETE");
            demoCaption="这一程的结局："+ending.title;
            dialogue=true;EnsureConversation();
            for(int i=0;i<spokenLines.Count;i++)yield return DemoRead(i);
            voiceSource.Stop();dialogue=false;demoFinished=true;
            Debug.Log("STORY_DEMO_COMPLETE ending="+ending.endingId+" decisions="+state.decisions+" letters="+state.letters.Count);
        }
        void DrawStoryDemo()
        {
            if(!StoryDemo)return;
            Box(new Rect(454,134,824,64),C("334E43"));Label(new Rect(469,143,795,46),demoCaption,23,paper,true,TextAnchor.MiddleCenter);
            Label(new Rect(480,208,780,26),"自动演示 · 加快探索步骤 · 演示中的选择不会改动你的存档",16,ink,false,TextAnchor.MiddleCenter);
            if(!demoFinished)return;
            Box(new Rect(180,262,1240,626),paper);Stroke(new Rect(180,262,1240,626),line);
            Label(new Rect(215,283,1170,51),"同样从泉州出发，可以走向不同的人生",32,ink,true);
            string[] titles={"家门灯火","迟归的春天","灯下新字","两岸家书","递批人","更远的潮","迟到的真话","银信不断"};
            string[] descriptions={"清理旧事，回到家人身边。","带着伤病或未解的心事归乡。","让阿满的读书之路继续。","在南洋守住柜台，也守住家书。","认真核批，成为乡亲信任的递批人。","再次远行，把新的回信地址写清。","归家坦白曾经藏下的难处。","量力寄款，照实写信，把日子过下去。"};
            for(int i=0;i<8;i++){
                float x=215+(i%2)*595,y=359+(i/2)*112;
                Box(new Rect(x,y,574,98),i==4?C("DDE7D2"):C("EEE9D7"));
                Label(new Rect(x+17,y+11,538,34),titles[i]+(i==4?"  · 本次路线":""),25,i==4?red:ink,true);
                Label(new Rect(x+17,y+52,538,37),descriptions[i],19,sub);
            }
            Label(new Rect(222,835,1150,31),"本次展示17次主线选择与3轮自主谋生；其他经历由落脚港口、打拼、寄批与家庭决定展开。",18,sub);
        }
    }
}
