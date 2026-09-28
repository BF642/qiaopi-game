using System;
using System.Collections;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        IEnumerator RunLandscapeQA()
        {
            yield return new WaitForSeconds(2);
            bool quick=Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-landscape-review")>=0;
            string[] nodes={"home","passage","passage","shore","shop","first_pay","courier"};
            for(int i=0;i<WorldRegions.All.Length;i++)
            {
                state=StoryEngine.NewGame();state.nodeId=nodes[i];if(i==2)state.flags.Add("aboard_passage");
                dialogue=journal=map=help=puzzle=fieldNote=false;EnterNode(true);
                yield return new WaitForSeconds(.6f);
                foreach(var r in world.GetComponentsInChildren<Renderer>())
                    foreach(var m in r.sharedMaterials)
                        if(m&&(!m.shader||!m.shader.isSupported))throw new Exception("LANDSCAPE_SHADER_UNSUPPORTED "+r.name);
                if(!RenderSettings.skybox||!RenderSettings.skybox.shader.isSupported)throw new Exception("LANDSCAPE_SKY_UNSUPPORTED");
                bool coast=loadedWorld=="harbor"||loadedWorld=="port"||loadedWorld=="ship";
                if(coast&&!world.GetComponentInChildren<SeaEnvironment>())throw new Exception("LANDSCAPE_SEA_MISSING");
                firstPersonFov=72f;UpdateCamera(true);yield return CaptureWorld("环境新版_"+loadedWorld+"_入口");
                firstPersonFov=82f;UpdateCamera(true);yield return CaptureWorld("环境新版_"+loadedWorld+"_全景");
                firstPersonFov=72f;
                if(quick){controller.enabled=false;player.transform.position=mission.npcPosition+new Vector3(0,.1f,-1.5f);controller.enabled=true;}
                else yield return WalkCheck(mission.npcPosition,"Landscape "+loadedWorld);
                FaceCurrentTask();firstPersonFov=72f;UpdateCamera(true);yield return CaptureWorld("环境新版_"+loadedWorld+"_远眺");
                if(cam.orthographic)throw new Exception("LANDSCAPE_PERSPECTIVE_NOT_ACTIVE");
                firstPersonFov=48f;UpdateCamera(true);yield return CaptureWorld("环境新版_"+loadedWorld+"_近景");
                RequireFirstPerson("landscape close view");
                if(loadedWorld=="ship"||loadedWorld=="postoffice"){
                    int frames=Time.frameCount;float begin=Time.realtimeSinceStartup;yield return new WaitForSeconds(2);
                    Debug.Log("LANDSCAPE_OBSERVED_FPS "+loadedWorld+"="+((Time.frameCount-frames)/(Time.realtimeSinceStartup-begin)));
                }
                Debug.Log("LANDSCAPE_RUNTIME_OK "+loadedWorld+" renderers="+world.GetComponentsInChildren<Renderer>().Length);
            }
            Debug.Log(quick?"LANDSCAPE_REVIEW_ALL_PASSED: seven region captures, supported shaders and first-person eye-height and field-of-view checks.":"LANDSCAPE_ALL_PASSED: seven populated surroundings, supported shaders, physical navigation, first-person eye-height and field-of-view checks.");
            NewGame();
        }
    }
}
