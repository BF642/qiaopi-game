Shader "Qiaopi/CharacterSkin"
{
    Properties
    {
        _Color ("Complexion tint", Color) = (1,1,1,1)
        _Glossiness ("Soft skin smoothness", Range(0,1)) = .28
        _SoftFill ("Subtle complexion fill", Range(0,.15)) = .055
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        // Unity's Standard lighting retains world light, ambient probes, fog,
        // proper normals and shadows. The FBX COLOR channel supplies all skin
        // colour; a white material is only a neutral tint, never the complexion.
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"

        fixed4 _Color;
        half _Glossiness, _SoftFill;

        struct Input
        {
            fixed4 complexion;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.complexion = vertex.color;
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed3 complexion = saturate(input.complexion.rgb * _Color.rgb);
            output.Albedo = complexion;
            output.Metallic = 0;
            output.Smoothness = _Glossiness;
            output.Occlusion = 1;
            output.Alpha = 1;
            // A restrained colour-preserving fill keeps cheeks readable on the
            // shaded side without replacing the directional shape of the face.
            output.Emission = complexion * _SoftFill;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
