using UnityEngine;
namespace Qiaopi
{
    /// <summary>Original letter-theme score, with two-source crossfades and speech ducking.</summary>
    public sealed class QiaopiMusic : MonoBehaviour
    {
        AudioSource[] sources;
        readonly float[] gains=new float[2],fadeStart=new float[2];
        int active;
        string sceneTrack="HomeLetter",currentTrack="";
        bool reading,speaking,musicEnabled=true;
        float level=.6f,duck=1,blend;
        public bool MusicEnabled=>musicEnabled;
        public float Level=>level;
        public float DuckGain=>duck;
        public string CurrentTrack=>currentTrack;
        public float AudibleVolume=>sources==null?0:sources[0].volume+sources[1].volume;
        public bool IsPlaying=>sources!=null&&(sources[0].isPlaying||sources[1].isPlaying);
        public string TrackTitle=>currentTrack=="SeaLetter"?"《过海的银信》":currentTrack=="ReturnLetter"?"《灯下回批》":"《厝边的风》";
        void Awake()
        {
            musicEnabled=PlayerPrefs.GetInt("qiaopi-music-enabled",1)==1;
            level=Mathf.Clamp01(PlayerPrefs.GetFloat("qiaopi-music-level",.6f));
            sources=new[]{gameObject.AddComponent<AudioSource>(),gameObject.AddComponent<AudioSource>()};
            foreach(var s in sources){s.playOnAwake=false;s.loop=true;s.spatialBlend=0;s.volume=0;s.priority=180;}
        }
        public void SetScene(string place,string node)
        {
            sceneTrack=node.StartsWith("end_")||node=="reply"||node=="first_letter"||node=="records"||node=="trace_family"?"ReturnLetter":place=="quanzhou"?"HomeLetter":"SeaLetter";
        }
        public void SetReading(bool value){reading=value;}
        public void SetSpeechActive(bool value){speaking=value;}
        public void SetEnabled(bool value,bool persist=true)
        {
            musicEnabled=value;if(persist)PlayerPrefs.SetInt("qiaopi-music-enabled",value?1:0);
            if(!value&&sources!=null)foreach(var s in sources)s.volume=0;
        }
        public void SetLevel(float value,bool persist=true)
        {
            level=Mathf.Clamp01(value);if(persist)PlayerPrefs.SetFloat("qiaopi-music-level",level);
        }
        void Update()
        {
            string desired=reading?"ReturnLetter":sceneTrack;
            if(desired!=currentTrack){
                var clip=Resources.Load<AudioClip>("Music/"+desired);
                if(clip){
                    int target=-1;
                    for(int i=0;i<2;i++)if(sources[i].clip==clip&&sources[i].isPlaying)target=i;
                    if(target<0){
                        target=gains[0]<=gains[1]?0:1;
                        sources[target].Stop();sources[target].clip=clip;sources[target].volume=0;
                        gains[target]=0;sources[target].Play();
                    }
                    active=target;fadeStart[0]=gains[0];fadeStart[1]=gains[1];blend=0;currentTrack=desired;
                }
            }
            blend=Mathf.MoveTowards(blend,1,Time.unscaledDeltaTime/2.8f);
            duck=Mathf.MoveTowards(duck,speaking?.23f:1,Time.unscaledDeltaTime*(speaking?3.8f:.7f));
            float volume=musicEnabled?level*duck:0;
            float ease=Mathf.SmoothStep(0,1,blend);
            for(int i=0;i<2;i++){
                // Retarget from the current gains and retain a playing track's position.
                gains[i]=Mathf.Sqrt(Mathf.Lerp(fadeStart[i]*fadeStart[i],i==active?1:0,ease));
                sources[i].volume=volume*gains[i];
            }
            if(blend>=1&&sources[1-active].isPlaying)sources[1-active].Stop();
        }
    }
}
