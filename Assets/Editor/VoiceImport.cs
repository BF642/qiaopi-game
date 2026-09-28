using System;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    /// <summary>Consistent local speech import, with no streaming or network dependency.</summary>
    public sealed class VoiceImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (assetPath.StartsWith("Assets/Resources/Voice/", StringComparison.OrdinalIgnoreCase)) Configure((AudioImporter)assetImporter);
        }
        static void Configure(AudioImporter importer)
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .65f;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = 22050;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }
        [MenuItem("侨批/配音/刷新离线配音导入设置")]
        public static void ApplyAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Voice" }))
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as AudioImporter;
                if (!importer) continue;
                Configure(importer); importer.SaveAndReimport(); count++;
            }
            Debug.Log("QIAOPI_VOICE_IMPORTED=" + count + " Vorbis quality=.65 mono 22050 preload DecompressOnLoad");
        }
    }
}
