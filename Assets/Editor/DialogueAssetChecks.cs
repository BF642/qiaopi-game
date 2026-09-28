using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Qiaopi.Editor
{
    public static class DialogueAssetChecks
    {
        [MenuItem("侨批/检查人物与语音资源")]
        public static void Run()
        {
            var ids=new[]{"wensheng","mother","aman","uncle","xusheng","guide","foreman","shopkeeper","master_he","clerk","worker"};
            var models=new HashSet<string>();
            foreach(var id in ids){var info=CharacterRoster.Get(id);if(!models.Add(info.model))throw new Exception("Duplicate identity model: "+id);
                if(!Resources.Load<GameObject>("Models/"+info.model))throw new Exception("Missing model: "+info.model);}
            // The public project supports a complete subtitle-only edition. A partial
            // voice pack must still pass every voiced-line check below.
            if(Resources.LoadAll<AudioClip>("Voice").Length==0){
                Debug.Log("QIAOPI DIALOGUE ASSET CHECKS PASSED: "+models.Count+" distinct character models; subtitle-only edition, no voice recordings installed.");
                return;
            }
            int count=0;foreach(var line in DialogueScript.AllLines()){
                var clip=Resources.Load<AudioClip>("Voice/"+line.clipKey);
                if(!clip||clip.samples<=0||clip.length<.1f)throw new Exception("Missing or empty dialogue voice: "+line.clipKey);count++;
            }
            var random=new System.Random(716);int outcomes=0;
            for(int trial=0;trial<4000;trial++){
                var state=StoryEngine.NewGame(trial+19060923);for(int step=0;step<20;step++){
                    var scene=StoryEngine.GetScene(state);if(scene.isEnding)break;
                    var enabled=scene.choices.FindAll(option=>option.enabled);var chosen=enabled[random.Next(enabled.Count)];
                    if(!Resources.Load<AudioClip>("Voice/"+VoiceKeys.ForText(chosen.title)))throw new Exception("Missing player answer voice: "+chosen.title);
                    StoryEngine.Choose(state,chosen.id);
                    if(!Resources.Load<AudioClip>("Voice/"+VoiceKeys.ForText(state.lastOutcome)))throw new Exception("Missing outcome voice: "+state.lastOutcome);
                    outcomes++;StoryEngine.Continue(state);
                }
            }
            Debug.Log("QIAOPI DIALOGUE ASSET CHECKS PASSED: "+models.Count+" distinct character models, "+count+" spoken lines, "+outcomes+" voiced choice/outcome checks.");
        }
    }
}
