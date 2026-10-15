Shader "Qiaopi/CharacterSkinTexture"
{
    Properties
    {
        _MainTex ("Authored complexion", 2D) = "white" {}
        _Color ("Complexion tint (sRGB)", Color) = (1,1,1,1)
        _Wrap ("Soft diffuse wrap", Range(0,0.5)) = 0.28
        _DiffuseResponse ("Direct diffuse response", Range(0.5,1)) = 0.86
        _SpecularStrength ("Restrained skin highlight", Range(0,0.06)) = 0.012
        _SpecularPower ("Skin highlight exponent", Range(8,64)) = 24
        _SoftFill ("Subtle warm reflected fill", Range(0,0.06)) = 0.022
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf QiaopiSoft fullforwardshadows addshadow exclude_path:deferred
        #pragma target 3.0
        #include "CharacterSoftLighting.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        half _SoftFill;
        struct Input { float2 uv_MainTex; };

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            half3 complexion = QiaopiToLinear(tex2D(_MainTex,IN.uv_MainTex).rgb);
            complexion *= QiaopiToLinear(_Color.rgb);
            o.Albedo = QiaopiToProjectSpace(complexion);
            o.Specular = 0;
            o.Smoothness = 0.18;
            o.Occlusion = 1;
            o.Alpha = 1;
            // Two percent of the surface colour leaves the eye sockets readable
            // while retaining cast shadows, cheek shape and the actual texture.
            o.Emission = o.Albedo*half3(1,0.88,0.78)*_SoftFill;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
