using System;
using System.Collections;
using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        IEnumerator CheckMusic()
        {
            voiceEnabled=true;
            musicDirector.SetEnabled(true,false);musicDirector.SetLevel(.6f,false);
            yield return new WaitForSeconds(3.1f);
            if(!musicDirector.IsPlaying||musicDirector.CurrentTrack!="HomeLetter")throw new Exception("MUSIC_QA_HOME");
            AudioSource homeTrack=null;foreach(var source in GetComponents<AudioSource>())if(source.clip&&source.clip.name=="HomeLetter")homeTrack=source;
            journal=true;yield return new WaitForSeconds(.3f);
            float position=homeTrack.time,volume=musicDirector.AudibleVolume;
            journal=false;yield return null;yield return null;
            if(homeTrack.time<position||Mathf.Abs(musicDirector.AudibleVolume-volume)>.12f)throw new Exception("MUSIC_QA_FAST_RETARGET");
            yield return new WaitForSeconds(3.1f);
            dialogue=true;yield return new WaitForSeconds(1.0f);
            if(!voiceSource.isPlaying||musicDirector.DuckGain>.3f)throw new Exception("MUSIC_QA_SPEECH_DUCK");
            yield return CaptureWorld("新版泉州_人物与配色");
            dialogue=false;yield return new WaitForSeconds(1.7f);
            if(musicDirector.DuckGain<.95f)throw new Exception("MUSIC_QA_RECOVER");
            musicDirector.SetEnabled(false,false);yield return null;
            if(musicDirector.AudibleVolume>.001f)throw new Exception("MUSIC_QA_MUTE");
            musicDirector.SetEnabled(true,false);musicDirector.SetLevel(.3f,false);yield return null;
            if(musicDirector.Level!=.3f)throw new Exception("MUSIC_QA_VOLUME");
            musicDirector.SetLevel(.6f,false);
            state=StoryEngine.NewGame();state.nodeId="passage";EnterNode();yield return new WaitForSeconds(3.1f);
            if(musicDirector.CurrentTrack!="SeaLetter"||!musicDirector.IsPlaying)throw new Exception("MUSIC_QA_SEA");
            yield return CaptureWorld("新版厦门_旧码头");
            state=StoryEngine.NewGame();state.nodeId="shop";EnterNode();yield return new WaitForSeconds(1);
            yield return CaptureWorld("新版南洋_旧街屋");
            journal=true;yield return new WaitForSeconds(3.1f);
            if(musicDirector.CurrentTrack!="ReturnLetter")throw new Exception("MUSIC_QA_LETTERS");
            journal=false;NewGame();yield return new WaitForSeconds(3.1f);
            if(musicDirector.CurrentTrack!="HomeLetter")throw new Exception("MUSIC_QA_SCENE_RETURN");
            yield return CaptureWorld("新版泉州_红砖古厝");
            help=true;yield return CaptureWorld("新版游戏_声音设置");help=false;
            Debug.Log("MUSIC_QA_ALL_PASSED: playback, fast letter-box retarget without track restart or gain jump, speech duck and recovery, mute, level, 3 scene themes, letter-box theme.");
            yield return RunWorldQA();
        }
    }
}
