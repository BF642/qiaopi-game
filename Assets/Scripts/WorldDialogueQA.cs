using System;
using System.Collections;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        IEnumerator RunDialogueQA()
        {
            yield return new WaitForSeconds(1);
            var nodes=new[]{"home","funding","passage","shore","dock","shop","courier","first_letter","farewell","reply"};
            foreach(var node in nodes){
                state=StoryEngine.NewGame();state.nodeId=node;dialogue=false;EnterNode();dialogue=true;
                yield return new WaitForSeconds(.7f);UpdateConversation();
                string expected=CharacterRoster.Get(CharacterRoster.ForNode(node)).model;
                if(npc.name!=mission.npcName||!npc.transform.Find("BlenderModel"))throw new Exception("DIALOGUE_QA_NPC "+node);
                if(!voiceSource.clip||!voiceSource.isPlaying)throw new Exception("DIALOGUE_QA_NO_SOUND "+node);
                if(!portraitActor||portraitId!=CurrentLine.speakerId)throw new Exception("DIALOGUE_QA_WRONG_PORTRAIT "+node);
                if(portraitTexture.width!=520)throw new Exception("DIALOGUE_QA_PORTRAIT_TEXTURE");
                Debug.Log("DIALOGUE_QA_OK "+node+" npc="+expected+" speaker="+portraitId+" clip="+voiceSource.clip.name+" duration="+voiceSource.clip.length);
                if(node=="home"||node=="shop"||node=="courier")yield return CaptureWorld("人物对话_"+node);
                if(node=="farewell"){
                    while(!LastDialogueLine){NextDialogueLine();yield return new WaitForSeconds(.4f);if(portraitId!=CurrentLine.speakerId||!voiceSource.isPlaying)throw new Exception("DIALOGUE_QA_SPEAKER_SWITCH");}
                    yield return CaptureWorld("人物对话_farewell");
                }
                dialogue=false;yield return null;if(voiceSource.isPlaying)throw new Exception("DIALOGUE_QA_CLOSE_NOT_STOPPED");
            }
            state=StoryEngine.NewGame();state.nodeId="shop";dialogue=false;EnterNode();dialogue=true;yield return new WaitForSeconds(.5f);
            spokenIndex=spokenLines.Count-1;var scene=StoryEngine.GetScene(state);var choice=scene.choices.Find(c=>c.enabled);Choose(choice.id);yield return new WaitForSeconds(.5f);
            if(portraitId!="wensheng"||!voiceSource.isPlaying||voiceSource.clip.name!=VoiceKeys.ForText(choice.title))throw new Exception("DIALOGUE_QA_CHOICE_SPEAKER");
            NextDialogueLine();yield return new WaitForSeconds(.5f);
            if(portraitId!="narrator"||!voiceSource.isPlaying||voiceSource.clip.name!=VoiceKeys.ForText(state.lastOutcome))throw new Exception("DIALOGUE_QA_OUTCOME_SPEAKER");
            voiceEnabled=false;voiceSource.Stop();yield return null;if(voiceSource.isPlaying)throw new Exception("DIALOGUE_QA_MUTE");
            voiceEnabled=true;PlayCurrentVoice();yield return null;if(!voiceSource.isPlaying)throw new Exception("DIALOGUE_QA_REPLAY");
            Debug.Log("DIALOGUE_QA_ALL_PASSED: identity, live portraits, spoken lines, speaker changes, player answer, narrated consequence, stop, mute, replay.");
            state=StoryEngine.NewGame();state.nodeId="shop";dialogue=false;EnterNode();dialogue=true;
        }
    }
}
