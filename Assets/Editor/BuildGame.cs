using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;

namespace Qiaopi.Editor
{
    public static class BuildGame
    {
        [MenuItem("侨批/导出 Mac 游戏")]
        public static void Build()
        {
            try
            {
                Configure();
                CreateScene();
                StoryChecks.Run();
                InspectionChecks.Run();
                SaveChecks.Run();
                DialogueChecks.Run();
                DialogueAssetChecks.Run();
                MusicChecks.Run();
                RegionChecks.Run();
                ProportionChecks.Run();
                GalleryChecks.Run();PersonalLetterChecks.Run();DestinationChecks.Run();LifeIntegrationChecks.Run();
                string root = Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds"));
                Directory.CreateDirectory(root);
                string path = Path.Combine(root,"侨批·纸短情长.app");
                var options = new BuildPlayerOptions { scenes = new[]{"Assets/Scenes/Qiaopi.unity"}, locationPathName = path, target = BuildTarget.StandaloneOSX, options=BuildOptions.None };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result+" errors="+report.summary.totalErrors);
                VerifyBuiltMacIcon(path);
                Debug.Log("QIAOPI_BUILD_SUCCESS="+path+" bytes="+report.summary.totalSize);
                if(Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); if(Application.isBatchMode) EditorApplication.Exit(1); }
        }
        public static void Configure()
        {
            PlayerSettings.companyName="QuanzhouLetters";
            PlayerSettings.productName="侨批 · 纸短情长";
            PlayerSettings.bundleVersion="2.0.4";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"studio.quanzhou.qiaopi");
            PlayerSettings.defaultScreenWidth=1440; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.useMacAppStoreValidation=false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
            PlayerSettings.stripEngineCode=false;
            QualitySettings.vSyncCount=1;
            AudioConfiguration cfg=AudioSettings.GetConfiguration(); cfg.speakerMode=AudioSpeakerMode.Stereo; AudioSettings.Reset(cfg);
            foreach(string p in new[]{"Assets/Resources/Art/quanzhou.png","Assets/Resources/Art/harbor.png","Assets/Resources/Art/singapore.png"})
            {
                var t=AssetImporter.GetAtPath(p) as TextureImporter;
                if(t!=null) { t.textureType=TextureImporterType.Default; t.maxTextureSize=2048; t.mipmapEnabled=false; t.textureCompression=TextureImporterCompression.CompressedHQ; t.SaveAndReimport(); }
            }
            var baseMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/WorldMaterial.mat");
            if(baseMaterial==null) {baseMaterial=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(baseMaterial,"Assets/Resources/WorldMaterial.mat");}
            CreateIcon();
            AssetDatabase.SaveAssets();
        }
        const string AppIconPath = "Assets/Art/AppIcon-1.7.png";

        static void CreateIcon()
        {
            string source = Path.Combine(Application.dataPath, "Art/AppIcon-1.7.png");
            if (!File.Exists(source))
                throw new FileNotFoundException("The 1.7 app icon is required. Add the final square PNG (at least 512 pixels) at " + AppIconPath + "; no placeholder or old icon will be used.", source);

            AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(AppIconPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Cannot import the required app icon: " + AppIconPath);
            int width, height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);
            if (width != height || width < 512 || width > 4096)
                throw new InvalidOperationException("App icon must be square and between 512 and 4096 pixels; received " + width + "x" + height + " at " + AppIconPath);

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.maxTextureSize = 1024;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            // Explicit desktop settings prevent a previous compression/size override
            // from reducing the icon before Unity writes the macOS .icns resource.
            var desktop = importer.GetPlatformTextureSettings("Standalone");
            desktop.name = "Standalone";
            desktop.overridden = true;
            desktop.maxTextureSize = 1024;
            desktop.format = TextureImporterFormat.RGBA32;
            desktop.textureCompression = TextureImporterCompression.Uncompressed;
            desktop.crunchedCompression = false;
            importer.SetPlatformTextureSettings(desktop);
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null || icon.width != icon.height || icon.width < 512 || icon.width > 1024 || icon.format != TextureFormat.RGBA32)
                throw new InvalidOperationException("App icon import did not produce a 512–1024px square uncompressed RGBA32 texture: " + AppIconPath);

            // Standalone is Unity's supported icon target for macOS. Fill every
            // queried size, rather than sending the old single 256-pixel texture.
            var target = UnityEditor.Build.NamedBuildTarget.Standalone;
            int[] sizes = PlayerSettings.GetIconSizes(target, IconKind.Any);
            if (sizes == null || sizes.Length == 0)
                throw new InvalidOperationException("Unity reported no Standalone application icon slots for the macOS build.");
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIcons(target, icons, IconKind.Any);
            // Also replace the default fallback, which Finder may use when a build
            // profile has no explicit desktop icon at a particular requested size.
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            var assigned = PlayerSettings.GetIcons(target, IconKind.Any);
            if (assigned == null || assigned.Length != icons.Length)
                throw new InvalidOperationException("macOS app icon slot assignment was not retained.");
            for (int i = 0; i < assigned.Length; i++)
                if (assigned[i] != icon)
                    throw new InvalidOperationException("macOS icon slot " + i + " did not retain " + AppIconPath);
            Debug.Log("QIAOPI_ICON_READY=" + AppIconPath + " source=" + width + "x" + height + " slots=" + string.Join(",", sizes));
        }

        static void VerifyBuiltMacIcon(string appPath)
        {
            // Validate the actual resource referenced by the finished application,
            // not merely the texture visible in the Unity project inspector.
            string contents = Path.Combine(appPath, "Contents");
            var plist = new System.Xml.XmlDocument { XmlResolver = null };
            plist.Load(Path.Combine(contents, "Info.plist"));
            var iconNode = plist.SelectSingleNode("/plist/dict/key[.='CFBundleIconFile']/following-sibling::string[1]");
            if (iconNode == null || string.IsNullOrWhiteSpace(iconNode.InnerText))
                throw new InvalidOperationException("Built macOS Info.plist does not reference an application icon.");
            string iconName = iconNode.InnerText;
            if (!iconName.EndsWith(".icns", StringComparison.OrdinalIgnoreCase)) iconName += ".icns";
            string resource = Path.Combine(contents, "Resources", iconName);
            if (!File.Exists(resource)) throw new FileNotFoundException("Built macOS app icon resource is missing.", resource);
            byte[] bytes = File.ReadAllBytes(resource);
            if (bytes.Length < 16 || System.Text.Encoding.ASCII.GetString(bytes, 0, 4) != "icns" || IconBigEndianInt(bytes, 4) != bytes.Length)
                throw new InvalidOperationException("Built macOS app icon is not a complete ICNS resource: " + resource);
            bool highResolution = false;
            for (int offset = 8; offset < bytes.Length;)
            {
                if (offset + 8 > bytes.Length) throw new InvalidOperationException("Truncated ICNS entry: " + resource);
                int length = IconBigEndianInt(bytes, offset + 4);
                if (length < 8 || length > bytes.Length - offset) throw new InvalidOperationException("Invalid ICNS entry length: " + resource);
                string kind = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
                // ic09=512px, ic10=1024px, ic14=256pt at 2x (512px).
                if (length > 8 && (kind == "ic09" || kind == "ic10" || kind == "ic14")) highResolution = true;
                offset += length;
            }
            if (!highResolution) throw new InvalidOperationException("Built macOS icon lacks a 512/1024px representation: " + resource);
            Debug.Log("QIAOPI_MAC_ICON_VERIFIED=" + resource + " bytes=" + bytes.Length);
        }

        static int IconBigEndianInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        public static void CreateScene()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            // Runtime regions use linear atmospheric haze. Keep this variant in
            // Standard materials when Unity strips shaders for the player build.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=105;RenderSettings.fogEndDistance=310;
            RenderSettings.fogColor=new Color(.773f,.816f,.745f);
            var cameraGO=new GameObject("Camera"); cameraGO.tag="MainCamera"; var camera=cameraGO.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.95f,.936f,.893f); camera.orthographic=true; camera.orthographicSize=5; camera.transform.position=new Vector3(0,0,-10);
            cameraGO.AddComponent<AudioListener>();
            new GameObject("泉州侨批 · 可探索的旅程").AddComponent<WorldGame>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Qiaopi.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Qiaopi.unity",true)};
            AssetDatabase.SaveAssets();
        }
    }
}
