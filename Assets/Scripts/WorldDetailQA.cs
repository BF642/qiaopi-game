using System;
using System.Collections;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        IEnumerator RunDetailQA()
        {
            yield return new WaitForSeconds(2);
            bool review=Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-fast-review")>=0;
            string[] nodes={"home","passage","passage","shore","shop","first_pay","courier"};
            for(int i=0;i<WorldRegions.All.Length;i++) {
                state=StoryEngine.NewGame();state.nodeId=nodes[i];if(i==2)state.flags.Add("aboard_passage");
                dialogue=journal=map=help=puzzle=fieldNote=false;firstPersonFov=72f;EnterNode(true);
                yield return new WaitForSeconds(.4f);
                var renderers=world.GetComponentsInChildren<Renderer>();int detailed=0;
                foreach(var r in renderers)foreach(var mat in r.sharedMaterials) {
                    if(mat&&(!mat.shader||!mat.shader.isSupported))throw new Exception("DETAIL_SHADER_UNSUPPORTED "+r.name);
                    if(r.enabled&&mat&&mat.shader.name.Contains("HistoricSurface"))detailed++;
                }
                if(detailed<3)throw new Exception("DETAIL_MATERIALS_MISSING "+loadedWorld);
                if(review){controller.enabled=false;player.transform.position=mission.npcPosition+new Vector3(0,.1f,-1.5f);controller.enabled=true;}
                else yield return WalkCheck(mission.npcPosition,"Human scale "+loadedWorld);
                if(Mathf.Abs(controller.height-1.72f)>.01f)throw new Exception("DETAIL_CONTROLLER_SCALE");
                firstPersonFov=48f;FaceCurrentTask();UpdateCamera(true);yield return CaptureWorld("实尺细节_"+loadedWorld);
                if(loadedWorld=="quanzhou"||loadedWorld=="market"||loadedWorld=="postoffice") {
                    firstPersonFov=72f;UpdateCamera(true);yield return CaptureWorld("实尺环境_"+loadedWorld);
                }
                int frames=Time.frameCount;float begin=Time.realtimeSinceStartup;yield return new WaitForSeconds(1);
                Debug.Log("DETAIL_RUNTIME_OK "+loadedWorld+" surfaces="+detailed+" fps="+((Time.frameCount-frames)/(Time.realtimeSinceStartup-begin)));
                if(i==0){
                    dialogue=true;EnsureConversation();UpdateConversation();yield return CaptureWorld("实尺人物对话");dialogue=false;
                    if(review){controller.enabled=false;player.transform.position=new Vector3(-12,.1f,5.8f);controller.enabled=true;}
                    else yield return WalkCheck(new Vector3(-12,0,5.8f),"Door and person comparison");
                    firstPersonFov=48f;FaceCurrentTask();UpdateCamera(true);yield return CaptureWorld("实尺门窗与人物");
                }
            }
            Debug.Log(review?"DETAIL_REVIEW_PASSED: seven close material views, surface layering, human collider scale and portrait framing.":"DETAIL_ALL_PASSED: seven physically walked regions, close material captures, human collider scale and portrait framing.");
            Application.Quit();
        }
    }
}
