Shader "Qiaopi/CharacterHair"
{
    Properties
    {
        _MainTex ("Authored strands and alpha", 2D) = "white" {}
        _Color ("Strand tint (sRGB)", Color) = (0.60,0.62,0.63,1)
        _Cutoff ("Strand alpha cutoff", Range(0,1)) = 0.35
        _Glossiness ("Smoothness", Range(0,1)) = 0.31
        _SpecularLevel ("Linear specular reflectance", Range(0,0.08)) = 0.008
        _GreyAmount ("Silver hair luminance", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        Cull Off
        ZWrite On
        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows addshadow alphatest:_Cutoff
        #pragma target 3.0
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness, _SpecularLevel, _GreyAmount;
        struct Input
        {
            float2 uv_MainTex;
            float facing : VFACE;
        };

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 strands = tex2D(_MainTex, IN.uv_MainTex);
            half3 colour = strands.rgb;
            half3 tint = _Color.rgb;
            // Match Blender's linear MixRGB multiplication in both Unity modes.
            #if defined(UNITY_COLORSPACE_GAMMA)
                colour = GammaToLinearSpace(colour);
                tint = GammaToLinearSpace(tint);
            #endif
            half greyLuminance = dot(colour,half3(.2126,.7152,.0722))*.72+.28;
            colour = lerp(colour,greyLuminance.xxx,_GreyAmount)*tint;
            #if defined(UNITY_COLORSPACE_GAMMA)
                colour = LinearToGammaSpace(colour);
            #endif
            o.Albedo = colour;
            o.Specular = _SpecularLevel.xxx;
            #if defined(UNITY_COLORSPACE_GAMMA)
                o.Specular = LinearToGammaSpace(o.Specular);
            #endif
            o.Smoothness = _Glossiness;
            o.Occlusion = 1;
            o.Alpha = strands.a;
            // Hair cards are visible from inside too; flip the tangent normal
            // so the rear faces receive sensible lighting rather than black slabs.
            o.Normal = half3(0, 0, IN.facing >= 0 ? 1 : -1);
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}
