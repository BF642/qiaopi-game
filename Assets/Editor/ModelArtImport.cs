using UnityEngine;
using UnityEditor;
namespace Qiaopi.Editor
{
    public class ModelArtImport : AssetPostprocessor
    {
        const int NativeTextureLimit = 8192;
        public static void RefreshNativeTexturePlatforms()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Models" })) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (MeshyCharacterForAsset(path) == null) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var texture = (TextureImporter)AssetImporter.GetAtPath(path);
                foreach (string platform in new[] { "Standalone", "iPhone", "Android" }) {
                    var settings = texture.GetPlatformTextureSettings(platform);
                    if (!settings.overridden || settings.maxTextureSize != NativeTextureLimit)
                        throw new System.InvalidOperationException("NATIVE_TEXTURE_PLATFORM_NOT_APPLIED " + path + " " + platform);
                    if (platform != "Standalone" && settings.format != TextureImporterFormat.ASTC_4x4)
                        throw new System.InvalidOperationException("NATIVE_TEXTURE_MOBILE_COMPRESSION_NOT_APPLIED " + path);
                }
                count++;
            }
            Debug.Log("NATIVE_TEXTURE_PLATFORMS_APPLIED count=" + count + " standaloneMax=8192 mobileMax=8192 mobileFormat=ASTC4x4");
        }

        void OnPreprocessTexture()
        {
            if (MeshyCharacterForAsset(assetPath) != null) {
                var texture = (TextureImporter)assetImporter;
                bool normalMap = assetPath.EndsWith("_normal.png", System.StringComparison.OrdinalIgnoreCase) || assetPath.EndsWith("_normal.jpg", System.StringComparison.OrdinalIgnoreCase);
                bool packedData = assetPath.EndsWith("_metalrough.png", System.StringComparison.OrdinalIgnoreCase) || assetPath.EndsWith("_metalrough.jpg", System.StringComparison.OrdinalIgnoreCase);
                texture.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                texture.sRGBTexture = !normalMap && !packedData;
                texture.alphaIsTransparency = false; texture.mipmapEnabled = true;
                texture.wrapMode = TextureWrapMode.Repeat; texture.maxTextureSize = NativeTextureLimit;
                texture.textureCompression = TextureImporterCompression.CompressedHQ;
                // This is an upper limit, not an upscale: original 2K files stay 2K.
                texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                    name = "Standalone", overridden = true, maxTextureSize = NativeTextureLimit,
                    format = normalMap ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7,
                    textureCompression = TextureImporterCompression.CompressedHQ,
                    compressionQuality = 100
                });
                foreach (string platform in new[] { "iPhone", "Android" })
                    texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                        name = platform, overridden = true, maxTextureSize = NativeTextureLimit,
                        format = TextureImporterFormat.ASTC_4x4,
                        textureCompression = TextureImporterCompression.CompressedHQ,
                        compressionQuality = 100
                    });
                return;
            }
            if(!assetPath.StartsWith("Assets/Resources/CharacterArt/"))return;
            var imp=(TextureImporter)assetImporter;
            bool normal=assetPath.EndsWith("_normal.png",System.StringComparison.OrdinalIgnoreCase);
            bool data=normal||assetPath.EndsWith("_specular.png",System.StringComparison.OrdinalIgnoreCase);
            bool hair=System.IO.Path.GetFileName(assetPath).StartsWith("QP2_HairAsset_");
            imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            imp.sRGBTexture=!data;
            imp.alphaIsTransparency=hair;imp.mipmapEnabled=true;
            // Preserve thin authored hair strands in the distant mip levels.
            imp.mipMapsPreserveCoverage=hair;
            if(hair)imp.alphaTestReferenceValue=.35f;
            imp.wrapMode=TextureWrapMode.Clamp;imp.maxTextureSize=2048;
            imp.textureCompression=TextureImporterCompression.CompressedHQ;
        }
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/Resources/Models/"))return;
            var imp=(ModelImporter)assetImporter;
            bool nativeMotion = assetPath.Contains("MeshyMotion/");
            imp.importCameras=false;imp.importLights=false;imp.importAnimation=nativeMotion;
            imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            imp.meshCompression=ModelImporterMeshCompression.Off;
            imp.isReadable=true;imp.addCollider=false;
            if (assetPath == "Assets/Resources/Models/WifeMeshyMotion/WifeMeshyMotion.fbx") {
                imp.importNormals = ModelImporterNormals.Import;
                imp.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
                imp.normalSmoothingAngle = 60;
                imp.importTangents = ModelImporterTangents.CalculateMikk;
            }
            string modelName=System.IO.Path.GetFileNameWithoutExtension(assetPath);
            string figureName=modelName.EndsWith("Study")?modelName.Substring(0,modelName.Length-5):modelName;
            if(ModelLibrary.CharacterHeight(figureName)>0 || MeshyCharacterForAsset(assetPath) != null) {
                // Four named limb transforms are rotated directly by gameplay.
                // Keep them exposed, including bones with tiny or zero weights,
                // and retain smooth seam blending in the continuous body mesh.
                imp.animationType=ModelImporterAnimationType.Generic;
                imp.optimizeGameObjects=false;imp.optimizeBones=false;
                imp.skinWeights=ModelImporterSkinWeights.Standard;
                if (nativeMotion) {
                    imp.skinWeights=ModelImporterSkinWeights.Custom;
                    imp.maxBonesPerVertex=32;imp.minBoneWeight=0;
                    imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                    imp.animationCompression=ModelImporterAnimationCompression.Off;
                    var clips=imp.defaultClipAnimations;
                    foreach(var clip in clips) {
                        clip.loopTime=true;clip.loopPose=false;
                        clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
                        clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;
                    }
                    imp.clipAnimations=clips;
                }
            }
        }
        void OnPostprocessModel(GameObject model)
        {
            if(!assetPath.StartsWith("Assets/Resources/Models/"))return;
            string modelName=System.IO.Path.GetFileNameWithoutExtension(assetPath);
            string figureName=modelName.EndsWith("Study")?modelName.Substring(0,modelName.Length-5):modelName;
            if(ModelLibrary.CharacterHeight(figureName)<=0 && MeshyCharacterForAsset(assetPath) == null)return;
            // Lower graphics presets otherwise discard seam weights at runtime.
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skin.quality=assetPath.Contains("MeshyMotion/") ? SkinQuality.Auto : SkinQuality.Bone4;
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true)) {
                var filter=renderer.GetComponent<MeshFilter>();
                if(!filter||!filter.sharedMesh)continue;
                var materials=renderer.sharedMaterials;
                for(int submesh=0;submesh<materials.Length&&submesh<filter.sharedMesh.subMeshCount;submesh++) {
                    var material=materials[submesh];
                    if(material&&material.name.StartsWith("QP2_EyeTexture"))FitCorneaFrame(filter.sharedMesh,submesh,material);
                }
            }
        }
        void OnPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            if (!assetPath.Contains("MeshyMotion/")) return;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true; settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
        void OnPostprocessMaterial(Material material)
        {
            if(!assetPath.StartsWith("Assets/Resources/Models/"))return;
            string nativeCharacter = MeshyCharacterForAsset(assetPath);
            if (nativeCharacter != null) {
                material.shader = RequiredShader("MeshyCharacter", "Qiaopi/MeshyCharacter");
                string art = System.IO.Path.GetDirectoryName(assetPath).Replace('\\','/') + "/" + nativeCharacter;
                material.SetTexture("_MainTex", (AssetDatabase.LoadAssetAtPath<Texture2D>(art + "_basecolor.png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>(art + "_basecolor.jpg")));
                material.SetTexture("_MetalRough", (AssetDatabase.LoadAssetAtPath<Texture2D>(art + "_metalrough.png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>(art + "_metalrough.jpg")));
                material.SetTexture("_BumpMap", (AssetDatabase.LoadAssetAtPath<Texture2D>(art + "_normal.png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>(art + "_normal.jpg")));
                material.color = Color.white;
                material.SetFloat("_BumpScale", .45f);
                material.SetFloat("_RoughnessFloor", .62f);
                material.SetFloat("_DielectricSpecular", .018f);
                material.SetFloat("_CullMode", assetPath.Contains("WifeMeshyMotion/") ? 2 : 0);
                material.renderQueue = -1;
                return;
            }
            Color c=material.HasProperty("_Color")?material.color:Color.white;
            material.shader=Shader.Find("Standard");material.color=c;
            material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.10f);
            // Keep the earlier scenery/prop material treatment untouched. The new
            // cast has explicit material families and a painted vertex complexion.
            if(!material.name.StartsWith("QP2_"))return;
            if(material.name.StartsWith("QP2_SkinTexture_")) {
                material.shader=RequiredShader("CharacterSkinTexture","Qiaopi/CharacterSkinTexture");
                string texturePath="Assets/Resources/CharacterArt/"+material.name+".png";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if(!texture)throw new System.InvalidOperationException("Textured QP2 skin requires "+texturePath);
                material.mainTexture=texture;material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
                // Blender's supplied tint is linear. Unity material Color
                // properties are authored in sRGB; the engine converts them
                // when passing to a Linear project. Convert once here for both
                // project modes, rather than baking a second darkening into skin.
                Color linearTint=material.name.EndsWith("_female")?new Color(.90f,.82f,.76f,1):new Color(.76f,.69f,.60f,1);
                material.color=linearTint.gamma;
                material.SetFloat("_Wrap",.28f);material.SetFloat("_DiffuseResponse",.86f);
                material.SetFloat("_SpecularStrength",.012f);material.SetFloat("_SpecularPower",24f);
                material.SetFloat("_SoftFill",.022f);
                material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=-1;
            } else if(material.name.StartsWith("QP2_Skin")) {
                var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/CharacterSkin.shader")??Shader.Find("Qiaopi/CharacterSkin");
                if(!shader)throw new System.InvalidOperationException("QP2 skin requires Assets/Resources/CharacterSkin.shader; plain white Standard material would discard the authored complexion.");
                material.shader=shader;material.color=Color.white;
                material.SetFloat("_Glossiness",.28f);material.SetFloat("_SoftFill",.055f);
            } else if(material.name.StartsWith("QP2_EyeTexture")) {
                material.shader=RequiredShader("CharacterEye","Qiaopi/CharacterEye");
                var eye=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/CharacterArt/brown_eye.png");
                if(!eye)throw new System.InvalidOperationException("QP2 eyes require Assets/Resources/CharacterArt/brown_eye.png.");
                material.mainTexture=eye;material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
                material.color=new Color(.80f,.84f,.82f,1).gamma;
                material.SetFloat("_IrisDarkening",.45f);material.SetFloat("_IrisNeutralising",.68f);
                material.SetFloat("_Wrap",.12f);material.SetFloat("_DiffuseResponse",.82f);
                material.SetFloat("_SpecularStrength",.10f);material.SetFloat("_SpecularPower",256f);
                material.SetFloat("_IrisRadius",.1155f);material.SetFloat("_CorneaCurvature",.70f);
                material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=-1;
            } else if(material.name.StartsWith("QP2_HairAsset_")) {
                material.shader=RequiredShader("CharacterHair","Qiaopi/CharacterHair");
                bool grey=material.name.EndsWith("_grey");
                string hairTexture=grey?material.name.Substring(0,material.name.Length-5):material.name;
                material.mainTexture=RequiredTexture(hairTexture);
                material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
                Color linearTint=grey?new Color(.42f,.44f,.43f,1):(hairTexture.EndsWith("_short01")?new Color(.32f,.34f,.36f,1):new Color(.055f,.068f,.078f,1));
                material.color=linearTint.gamma;
                material.SetFloat("_Cutoff",.35f);material.SetFloat("_Glossiness",.31f);
                material.SetFloat("_GreyAmount",grey?1:0);
                // Principled IOR 1.5, Specular IOR Level .10 -> normal-incidence .008.
                material.SetFloat("_SpecularLevel",.008f);
                material.renderQueue=(int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            } else if(material.name.StartsWith("QP2_AuthoredWomen_")) {
                material.shader=RequiredShader("CharacterClothing","Qiaopi/CharacterClothing");
                string textureName=material.name.EndsWith("_Aman")?"QP2_AuthoredWomen_cf011":"QP2_AuthoredWomen_f018";
                material.mainTexture=RequiredTexture(textureName+"_color");
                material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
                material.SetTexture("_BumpMap",RequiredTexture(textureName+"_normal"));
                material.SetTexture("_SpecMap",RequiredTexture(textureName+"_specular"));
                CharacterDyes(material.name,out Color shirt,out Color skirt);
                material.SetColor("_CreamColor",shirt);material.SetColor("_BrownColor",skirt);
                material.SetColor("_SkinTint",new Color(.90f,.82f,.76f,1).gamma);
                material.SetFloat("_UseRegionMask",1);
                material.SetFloat("_CreamStrength",2.3f);material.SetFloat("_CreamOffset",.20f);
                material.SetFloat("_BrownStrength",2.2f);material.SetFloat("_BrownOffset",.16f);
                material.SetFloat("_BumpScale",.42f);
                material.SetFloat("_RoughnessBase",.92f);material.SetFloat("_RoughnessScale",.12f);
                material.SetFloat("_SpecularLevel",.016f);material.renderQueue=-1;
            } else if(material.name.StartsWith("QP2_AuthoredCottonAndTwill")) {
                material.shader=RequiredShader("CharacterClothing","Qiaopi/CharacterClothing");
                const string textureName="QP2_AuthoredCottonAndTwill";
                material.mainTexture=RequiredTexture(textureName+"_color");
                material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
                material.SetTexture("_BumpMap",RequiredTexture(textureName+"_normal"));
                material.SetTexture("_SpecMap",RequiredTexture(textureName+"_specular"));
                // These colours are the source sRGB hex colours. The shader does
                // all atlas recolouring in linear space, including Gamma projects.
                CharacterDyes(material.name,out Color shirt,out Color trousers);
                material.SetColor("_CreamColor",shirt);
                material.SetColor("_BrownColor",trousers);
                material.SetFloat("_UseRegionMask",0);
                material.SetColor("_SkinTint",new Color(.80f,.73f,.65f,1).gamma);
                material.SetFloat("_CreamStrength",2.3f);material.SetFloat("_CreamOffset",.20f);
                material.SetFloat("_BrownStrength",2.2f);material.SetFloat("_BrownOffset",.16f);
                material.SetFloat("_BumpScale",.55f);
                material.SetFloat("_RoughnessBase",.91f);material.SetFloat("_RoughnessScale",.17f);
                material.SetFloat("_SpecularLevel",.0192f);
                material.renderQueue=-1;
            } else if(material.name.StartsWith("QP2_PeriodCloth_")) {
                material.shader=RequiredShader("PeriodCloth","Qiaopi/PeriodCloth");
                material.color=c.gamma;
                material.SetFloat("_WeaveScale",420f);material.SetFloat("_WeaveAmount",.035f);
                material.SetFloat("_SpecularStrength",.004f);material.SetFloat("_Wrap",.18f);
                material.renderQueue=-1;
            } else if(material.name.StartsWith("QP2_Face_Brow_v280_")) {
                material.color=c.gamma;material.SetFloat("_Glossiness",.08f);
            } else if(material.name.StartsWith("QP2_Hair")||material.name.StartsWith("QP2_Silver")) {
                material.SetFloat("_Glossiness",.25f);
            } else if(material.name.StartsWith("QP2_Cloth_")) {
                material.SetFloat("_Glossiness",.12f);
            } else if(material.name.StartsWith("QP2_Brass")) {
                material.SetFloat("_Metallic",.35f);material.SetFloat("_Glossiness",.28f);
            } else if(material.name.StartsWith("QP2_Sole")) {
                material.SetFloat("_Glossiness",.08f);
            }
        }
        static string MeshyCharacterForAsset(string path)
        {
            foreach (string character in new[] { "Wensheng", "Wife", "Mother", "Aman", "Uncle", "Xusheng", "Guide", "Foreman", "Shopkeeper", "MasterHe", "Clerk", "Worker" })
                if (path.StartsWith("Assets/Resources/Models/" + character + "Meshy/", System.StringComparison.Ordinal) ||
                    path.StartsWith("Assets/Resources/Models/" + character + "MeshyMotion/", System.StringComparison.Ordinal))
                    return character;
            return null;
        }
        static void CharacterDyes(string materialName,out Color shirt,out Color trousers)
        {
            string upper="D2CDBC",lower="75604A";
            if(materialName.EndsWith("_Worker")){upper="8D8266";lower="4C5955";}
            else if(materialName.EndsWith("_Uncle")){upper="686B5C";lower="434B44";}
            else if(materialName.EndsWith("_Xusheng")){upper="687C75";lower="384E52";}
            else if(materialName.EndsWith("_Guide")){upper="A08C65";lower="455254";}
            else if(materialName.EndsWith("_Foreman")){upper="736B55";lower="495147";}
            else if(materialName.EndsWith("_Shopkeeper")){upper="494E54";lower="333F44";}
            else if(materialName.EndsWith("_MasterHe")){upper="4A6971";lower="36484E";}
            else if(materialName.EndsWith("_Clerk")){upper="9AB3A6";lower="3A5353";}
            else if(materialName.EndsWith("_Mother")){upper="816358";lower="534C47";}
            else if(materialName.EndsWith("_Wife")){upper="648A81";lower="394E55";}
            else if(materialName.EndsWith("_Aman")){upper="987E91";lower="595C6D";}
            ColorUtility.TryParseHtmlString("#"+upper,out shirt);
            ColorUtility.TryParseHtmlString("#"+lower,out trousers);
        }
        static void FitCorneaFrame(Mesh mesh,int submesh,Material material)
        {
            var vertices=mesh.vertices;var uv=mesh.uv;var normals=mesh.normals;
            if(uv.Length!=vertices.Length||normals.Length!=vertices.Length)
                throw new System.InvalidOperationException("QP2 eyes require UVs and authored normals to fit the corneal reflection frame.");
            var triangles=mesh.GetTriangles(submesh);
            Vector3 right=Vector3.zero,up=Vector3.zero,normal=Vector3.zero;
            int sampled=0;
            for(int i=0;i+2<triangles.Length;i+=3) {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                Vector2 center=(uv[a]+uv[b]+uv[c])/3;
                float radius=Mathf.Min((center-new Vector2(.292067f,.290036f)).magnitude,
                    (center-new Vector2(.707838f,.702067f)).magnitude)/.1155f;
                if(radius>.88f)continue;
                Vector2 du=uv[b]-uv[a],dv=uv[c]-uv[a];
                float determinant=du.x*dv.y-du.y*dv.x;
                if(Mathf.Abs(determinant)<.00000001f)continue;
                Vector3 edgeU=vertices[b]-vertices[a],edgeV=vertices[c]-vertices[a];
                float weight=Mathf.Abs(determinant);
                right+=(edgeU*dv.y-edgeV*du.y)*(weight/determinant);
                up+=(edgeV*du.x-edgeU*dv.x)*(weight/determinant);
                normal+=(normals[a]+normals[b]+normals[c])*weight;
                sampled++;
            }
            if(sampled<4||right.sqrMagnitude<1e-12f||up.sqrMagnitude<1e-12f)
                throw new System.InvalidOperationException("QP2 eye atlas did not provide a valid corneal frame: "+mesh.name);
            Vector3 forward=Vector3.Cross(right,up).normalized;
            if(Vector3.Dot(forward,normal)<0)forward=-forward;
            right=Vector3.ProjectOnPlane(right,forward).normalized;
            Vector3 vertical=Vector3.Cross(forward,right).normalized;
            if(Vector3.Dot(vertical,up)<0)vertical=-vertical;
            material.SetVector("_CorneaForward",forward);
            material.SetVector("_CorneaRight",right);
            material.SetVector("_CorneaUp",vertical);
            Debug.Log("QP2_CORNEA_FRAME "+mesh.name+" triangles="+sampled+" forward="+forward+" right="+right+" up="+vertical);
        }
        static Shader RequiredShader(string fileName,string shaderName)
        {
            string path="Assets/Resources/"+fileName+".shader";
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(path)??Shader.Find(shaderName);
            if(!shader)throw new System.InvalidOperationException("QP2 character material requires "+path);
            return shader;
        }
        static Texture2D RequiredTexture(string fileName)
        {
            string path="Assets/Resources/CharacterArt/"+fileName+".png";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture)throw new System.InvalidOperationException("QP2 character material requires "+path);
            return texture;
        }
    }
}
