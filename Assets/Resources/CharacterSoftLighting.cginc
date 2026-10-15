#ifndef QIAOPI_CHARACTER_SOFT_LIGHTING_INCLUDED
#define QIAOPI_CHARACTER_SOFT_LIGHTING_INCLUDED
#include "UnityPBSLighting.cginc"

half _Wrap, _DiffuseResponse, _SpecularStrength, _SpecularPower;

// Keep direct lights, realtime shadows, light probes and baked diffuse light.
// Small broad highlights replace the reflective Fresnel/probe lobe that made
// the Gamma preview look waxy. This is a lit material, not a painted billboard.
inline half4 LightingQiaopiSoft(SurfaceOutputStandardSpecular s, half3 viewDir, UnityGI gi)
{
    half3 normal = normalize(s.Normal);
    half nl = dot(normal,gi.light.dir);
    half diffuse = saturate((nl+_Wrap)/(1+_Wrap));
    half3 halfVector = normalize(gi.light.dir+viewDir);
    half highlight = pow(saturate(dot(normal,halfVector)),_SpecularPower);
    highlight *= _SpecularStrength*saturate(nl);
    half3 colour = s.Albedo*(gi.indirect.diffuse+gi.light.color*(diffuse*_DiffuseResponse));
    colour += gi.light.color*highlight;
    return half4(colour,s.Alpha);
}

inline void LightingQiaopiSoft_GI(SurfaceOutputStandardSpecular s, UnityGIInput data, inout UnityGI gi)
{
    // Deliberately request diffuse GI only, not mirror-like environment probes.
    gi = UnityGlobalIllumination(data,s.Occlusion,s.Normal);
}

inline half3 QiaopiToLinear(half3 colour)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
        return GammaToLinearSpace(colour);
    #else
        return colour;
    #endif
}

inline half3 QiaopiToProjectSpace(half3 colour)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
        return LinearToGammaSpace(colour);
    #else
        return colour;
    #endif
}
#endif
