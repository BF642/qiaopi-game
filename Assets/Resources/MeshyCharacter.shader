Shader "Qiaopi/MeshyCharacter"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Original base colour", 2D) = "white" {}
        _MetalRough ("glTF roughness G / metallic B", 2D) = "white" {}
        [Normal] _BumpMap ("Original normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0,2)) = 0.45
        _RoughnessFloor ("Minimum roughness under game lights", Range(0,1)) = 0.62
        _DielectricSpecular ("Non-metal reflection", Range(0,0.08)) = 0.018
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Surface culling", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        Cull [_CullMode]
        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows
        #pragma target 3.0
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex, _MetalRough, _BumpMap;
        fixed4 _Color;
        half _BumpScale, _RoughnessFloor, _DielectricSpecular;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 colour = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 packed = tex2D(_MetalRough, IN.uv_MainTex);
            // The source maps are retained. A lower dielectric response and
            // broad roughness stop directional game lights turning skin to wax.
            o.Albedo = colour.rgb * (1.0h - packed.b);
            o.Specular = lerp(_DielectricSpecular.xxx, colour.rgb, packed.b);
            o.Smoothness = 1.0h - max(packed.g, _RoughnessFloor);
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
