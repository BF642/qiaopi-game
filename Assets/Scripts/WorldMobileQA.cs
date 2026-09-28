using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        string MobileReport=>Path.Combine(Application.persistentDataPath,"iphone-test-report.txt");
        void MobileCheck(bool condition,string label)
        {
            if(!condition){File.AppendAllText(MobileReport,"FAIL "+label+"\n");throw new Exception("MOBILE_QA_"+label);}
            File.AppendAllText(MobileReport,"PASS "+label+"\n");
        }
        IEnumerator RunMobileQA()
        {
            yield return new WaitForSeconds(2);
            File.WriteAllText(MobileReport,"Qiaopi iPhone test\nDevice: "+SystemInfo.deviceModel+"\nOS: "+SystemInfo.operatingSystem+"\nGPU: "+SystemInfo.graphicsDeviceName+"\nScreen: "+Screen.width+" x "+Screen.height+"\nSafe area: "+Screen.safeArea+"\n");
            MobileCheck(MobileControls,"Touch controls active");
            MobileCheck(Screen.width>Screen.height,"Landscape orientation");
            MobileCheck(!soundEnabled&&!voiceEnabled&&!musicDirector.MusicEnabled,"All sound muted");
            var initial=player.transform.position;var home=StickHome();
            HandleMobileTouch(11,home,TouchPhase.Began);
            HandleMobileTouch(11,home+new Vector2(0,-88),TouchPhase.Moved);
            float yaw=ViewYaw;
            HandleMobileTouch(22,new Vector2(990,430),TouchPhase.Began);
            HandleMobileTouch(22,new Vector2(1110,455),TouchPhase.Moved);
            yield return new WaitForSeconds(.35f);
            MobileCheck(Vector3.Distance(player.transform.position,initial)>.4f,"Joystick physically walks");
            MobileCheck(Mathf.Abs(Mathf.DeltaAngle(yaw,ViewYaw))>15,"Second finger turns camera while walking");
            HandleMobileTouch(11,home,TouchPhase.Ended);HandleMobileTouch(22,new Vector2(1110,455),TouchPhase.Ended);
            MobileCheck(mobileMove==Vector2.zero&&moveFinger<0&&lookFinger<0,"Released fingers stop movement and looking");
            help=true;HandleMobileTouch(33,home,TouchPhase.Began);MobileCheck(moveFinger<0&&mobileMove==Vector2.zero,"Modal UI blocks joystick");help=false;
            Rect viewport=MobileUiBounds();
            MobileCheck(viewport.Contains(TouchInteractRect().min)&&viewport.Contains(TouchInteractRect().max),"Interact button inside safe area");
            string[] nodes={"home","passage","passage","shore","shop","first_pay","courier"};
            for(int i=0;i<WorldRegions.All.Length;i++){
                state=StoryEngine.NewGame();state.nodeId=nodes[i];if(i==2)state.flags.Add("aboard_passage");
                dialogue=journal=map=help=puzzle=fieldNote=gallery=false;EnterNode(true);ResetView();toastUntil=0;
                yield return new WaitForSeconds(.5f);RequireFirstPerson("iPhone "+loadedWorld);
                foreach(var renderer in world.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
                    MobileCheck(!material||material.shader&&material.shader.isSupported,"Shader "+loadedWorld+" "+(material?material.shader.name:"empty"));
                int start=Time.frameCount;float time=Time.realtimeSinceStartup;yield return new WaitForSeconds(1.2f);
                File.AppendAllText(MobileReport,"FPS "+loadedWorld+" "+((Time.frameCount-start)/(Time.realtimeSinceStartup-time)).ToString("F1")+"\n");
                yield return CaptureWorld("iPhone_"+loadedWorld);
                if(i==0){map=true;mapTab=0;yield return CaptureWorld("iPhone_行路图");map=false;}
                if(i==2){map=true;mapTab=0;yield return CaptureWorld("iPhone_海船行路图");map=false;}
            }
            state=StoryEngine.NewGame();state.nodeId="home";EnterNode(true);ResetView();
            yield return WalkFirstPerson(mission.itemPosition,"iPhone luggage");
            HandleMobileTouch(44,TouchInteractRect().center,TouchPhase.Began);yield return null;
            HandleMobileTouch(44,TouchInteractRect().center,TouchPhase.Ended);MobileCheck(Complete,"Touch interaction picks up luggage");
            yield return WalkFirstPerson(mission.npcPosition,"iPhone mother");
            HandleMobileTouch(45,TouchInteractRect().center,TouchPhase.Began);yield return null;
            MobileCheck(dialogue,"Touch interaction starts conversation");yield return CaptureWorld("iPhone_与母亲交谈");
            EnsureConversation();spokenIndex=spokenLines.Count-1;Choose("ledger");MobileCheck(state.awaitingContinue,"Story choice still works");Next();MobileCheck(state.nodeId=="funding","Story continues");
            dialogue=false;yield return RunMobileGalleryReview();
            yield return RunLifeReview();
            File.AppendAllText(MobileReport,"PASS 2.0 prologue, three destinations, physical jobs, hardship, self-written letter and return to story\n");
            MobileCheck(!soundEnabled&&!voiceEnabled&&!musicDirector.MusicEnabled,"Sound remains muted after gameplay");
            File.AppendAllText(MobileReport,"MOBILE_ALL_PASSED\n");Debug.Log("MOBILE_ALL_PASSED "+MobileReport);
            state=StoryEngine.NewGame();state.nodeId="home";EnterNode(true);ResetView();
            toast="iPhone 测试完成 · 左侧行走，右侧转头";toastUntil=Time.time+4;
            // Keep the iOS process alive for inspection; the normal launch uses the real save.
        }
    }
}
