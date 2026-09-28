using UnityEditor;
using UnityEngine;
namespace Qiaopi.Editor
{
    public sealed class MusicImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Resources/Music/"))return;
            var importer=(AudioImporter)assetImporter;importer.forceToMono=false;importer.loadInBackground=false;
            var sample=importer.defaultSampleSettings;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.85f;
            sample.loadType=AudioClipLoadType.Streaming;sample.preloadAudioData=true;
            sample.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=sample;
        }
    }
    public static class MusicChecks
    {
        public static void Run()
        {
            foreach(string name in new[]{"HomeLetter","SeaLetter","ReturnLetter"}){
                var clip=Resources.Load<AudioClip>("Music/"+name);
                if(!clip||clip.length<60||clip.channels!=2||clip.frequency!=44100)throw new System.Exception("Invalid music asset: "+name);
            }
            Debug.Log("QIAOPI MUSIC CHECKS PASSED: 3 stereo 44100Hz loops, each longer than 60 seconds.");
        }
    }
}
