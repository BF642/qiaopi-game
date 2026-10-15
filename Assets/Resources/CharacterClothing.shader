Shader "Qiaopi/CharacterClothing"
{
    Properties
    {
        _MainTex ("Original garment colour atlas", 2D) = "white" {}
        [Normal] _BumpMap ("Original garment normal atlas", 2D) = "bump" {}
        _SpecMap ("Original garment specular atlas (linear)", 2D) = "black" {}
        _CreamColor ("Cotton dye (sRGB)", Color) = (0.8235294,0.8039216,0.7372549,1)
        _BrownColor ("Twill dye (sRGB)", Color) = (0.4588235,0.3764706,0.2901961,1)
        _SkinTint ("Hand tint (sRGB)", Color) = (0.9063318,0.8703449,0.8266571,1)
        _CreamStrength ("Cotton luminance scale", Float) = 2.3
        _CreamOffset ("Cotton luminance offset", Float) = 0.20
        _BrownStrength ("Twill luminance scale", Float) = 2.2
        _BrownOffset ("Twill luminance offset", Float) = 0.16
        _BumpScale ("Normal strength", Range(0,1)) = 0.55
        _RoughnessBase ("Base roughness", Range(0,1)) = 0.91
        _RoughnessScale ("Specular atlas roughness scale", Range(0,1)) = 0.17
        _SpecularLevel ("Linear specular reflectance", Range(0,0.08)) = 0.0192
        _UseRegionMask ("Use authored garment colour regions", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows addshadow
        #pragma target 3.0
        #include "UnityCG.cginc"

        sampler2D _MainTex, _BumpMap, _SpecMap;
        fixed4 _CreamColor, _BrownColor, _SkinTint;
        half _CreamStrength, _CreamOffset, _BrownStrength, _BrownOffset;
        half _BumpScale, _RoughnessBase, _RoughnessScale, _SpecularLevel;
        half _UseRegionMask;
        struct Input
        {
            float2 uv_MainTex;
            fixed4 garmentRegion : COLOR;
        };

        float3 RGBToHSV(float3 c)
        {
            float4 k = float4(0, -1.0/3.0, 2.0/3.0, -1);
            float4 p = lerp(float4(c.bg,k.wz),float4(c.gb,k.xy),step(c.b,c.g));
            float4 q = lerp(float4(p.xyw,c.r),float4(c.r,p.yzx),step(p.x,c.r));
            float d = q.x-min(q.w,q.y);
            return float3(abs(q.z+(q.w-q.y)/(6*d+1e-10)), d/(q.x+1e-10), q.x);
        }

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            half3 source = tex2D(_MainTex,IN.uv_MainTex).rgb;
            half3 cream = _CreamColor.rgb, brown = _BrownColor.rgb, skinTint = _SkinTint.rgb;
            // The source Blender nodes run before display transforms. Performing
            // their HSV/luminance tests on sRGB pixels misclassifies the blue shirt.
            #if defined(UNITY_COLORSPACE_GAMMA)
                source = GammaToLinearSpace(source);
                cream = GammaToLinearSpace(cream);
                brown = GammaToLinearSpace(brown);
                skinTint = GammaToLinearSpace(skinTint);
            #endif
            half luminance = dot(source,half3(0.2126,0.7152,0.0722));
            float3 hsv = RGBToHSV(source);
            // Match authored_clothing.py. Reflected blue in the white undershirt
            // crosses the old hard threshold and otherwise creates torn edges.
            half blue = smoothstep(0.46,0.50,hsv.x)
                      * (1-smoothstep(0.71,0.75,hsv.x))
                      * smoothstep(0.06,0.26,hsv.y);
            blue = lerp(blue,saturate(IN.garmentRegion.r),_UseRegionMask);
            cream *= luminance*_CreamStrength+_CreamOffset;
            brown *= luminance*_BrownStrength+_BrownOffset;
            // Complexion is the common exported COLOR0 layer: G identifies
            // trouser faces; B identifies hands. Skin uses its texture, not COLOR0.
            half3 colour = lerp(source,cream,blue);
            colour = lerp(colour,brown,saturate(IN.garmentRegion.g));
            colour = lerp(colour,source*skinTint,saturate(IN.garmentRegion.b));
            #if defined(UNITY_COLORSPACE_GAMMA)
                colour = LinearToGammaSpace(colour);
            #endif
            o.Albedo = colour;
            // Blender's tangent Normal Map node scales XY then mixes Z toward
            // one before normalising; Unity's usual bump-strength helper only
            // scales XY and reconstructs Z, which makes these folds look harder.
            half3 tangentNormal = UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));
            tangentNormal.xy *= _BumpScale;
            tangentNormal.z = lerp(1,tangentNormal.z,saturate(_BumpScale));
            o.Normal = normalize(tangentNormal);
            half specularLuminance = dot(tex2D(_SpecMap,IN.uv_MainTex).rgb,half3(0.2126,0.7152,0.0722));
            o.Smoothness = saturate(1-_RoughnessBase+_RoughnessScale*specularLuminance);
            o.Specular = _SpecularLevel.xxx;
            #if defined(UNITY_COLORSPACE_GAMMA)
                o.Specular = LinearToGammaSpace(o.Specular);
            #endif
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
