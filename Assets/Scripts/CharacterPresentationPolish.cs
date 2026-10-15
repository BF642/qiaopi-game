using UnityEngine;

namespace Qiaopi
{
    // A presentation layer on the shipped, individually authored cast. Never
    // replaces their face geometry, source textures or shared FBX materials.
    public static class CharacterPresentationPolish
    {
        public static void Apply(GameObject actor, string model)
        {
            if (!actor || ModelLibrary.CharacterHeight(model) <= 0) return;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) {
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++) {
                    var material = materials[index];
                    if (!material || !material.shader) continue;
                    block.Clear(); renderer.GetPropertyBlock(block, index);
                    switch (material.shader.name) {
                        case "Qiaopi/CharacterSkinTexture":
                            // A little reflected light reads through the porch
                            // shade. Keep the actual complexion and cast shadow.
                            block.SetFloat("_Wrap", .31f);
                            block.SetFloat("_DiffuseResponse", .89f);
                            block.SetFloat("_SoftFill", .038f);
                            block.SetFloat("_SpecularStrength", .007f);
                            block.SetFloat("_SpecularPower", 22f);
                            break;
                        case "Qiaopi/CharacterEye":
                            // Retain a moving corneal catchlight, with less of
                            // the glass-marble flash at conversational distance.
                            block.SetFloat("_SpecularStrength", .065f);
                            block.SetFloat("_SpecularPower", 192f);
                            break;
                        case "Qiaopi/CharacterHair":
                            block.SetFloat("_Glossiness", .24f);
                            block.SetFloat("_SpecularLevel", .0045f);
                            break;
                        case "Qiaopi/CharacterClothing":
                            // Less embossed cotton: keep the atlas's folds and
                            // all authored dye/hand regions, soften their edges.
                            block.SetFloat("_BumpScale", model == "Mother" || model == "Wife" || model == "Aman" ? .36f : .43f);
                            block.SetFloat("_RoughnessBase", .94f);
                            block.SetFloat("_SpecularLevel", .013f);
                            break;
                        default: continue;
                    }
                    renderer.SetPropertyBlock(block, index);
                }
            }
        }

        // Stable between saves/builds, unlike an instance ID or random seed.
        // Only used for tiny resting asymmetries, never a different face/body.
        public static float RestingPhase(string model)
        {
            uint value = 17;
            foreach (char letter in model ?? "") value = unchecked(value * 31 + letter);
            return (value % 6283) * .001f;
        }
    }
}
