using System;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    // Import/compile validation only: no scene, player build, audio or save code.
    public static class CharacterMaterialChecks
    {
        [MenuItem("侨批/人物样稿/检查柔和皮肤与眼睛材质")]
        public static void Run()
        {
            bool asynchronous = ShaderUtil.allowAsyncCompilation;
            try {
                ShaderUtil.allowAsyncCompilation = false;
                foreach (string name in new[] {"CharacterSkinTexture", "CharacterEye", "PeriodCloth", "MeshyCharacter"}) {
                    string path = "Assets/Resources/" + name + ".shader";
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if (!shader) throw new InvalidOperationException("Missing character shader: " + path);
                    var material = new Material(shader);
                    try {
                        if (!material.SetPass(0)) throw new InvalidOperationException("Cannot activate character shader: " + shader.name);
                        foreach (var message in ShaderUtil.GetShaderMessages(shader))
                            if (message.severity.ToString() == "Error")
                                throw new InvalidOperationException(shader.name + ": " + message.message + " at " + message.file + ":" + message.line);
                        if (!shader.isSupported) throw new InvalidOperationException("Unsupported character shader: " + shader.name);
                        Debug.Log("CHARACTER_MATERIAL_SHADER_PASSED " + shader.name + " passes=" + material.passCount);
                    } finally { UnityEngine.Object.DestroyImmediate(material); }
                }
                Debug.Log("CHARACTER_MATERIAL_SHADERS_PASSED colourSpace=" + QualitySettings.activeColorSpace);
            } finally { ShaderUtil.allowAsyncCompilation = asynchronous; }
        }
    }
}
