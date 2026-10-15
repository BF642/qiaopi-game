Shader "Qiaopi/CharacterEye"
{
    Properties
    {
        _MainTex ("Authored iris and sclera", 2D) = "white" {}
        _Color ("Sclera tint (sRGB)", Color) = (0.9063,0.9261,0.9163,1)
        _IrisDarkening ("Dark brown iris luminance", Range(0.15,0.7)) = 0.45
        _IrisNeutralising ("Suppress red iris hue", Range(0,1)) = 0.68
        _Wrap ("Gentle eye diffuse wrap", Range(0,0.4)) = 0.12
        _DiffuseResponse ("Eye diffuse response", Range(0.5,1)) = 0.82
        _SpecularStrength ("Corneal reflection energy", Range(0,0.25)) = 0.10
        _SpecularPower ("Corneal highlight exponent", Range(64,512)) = 256
        _IrisRadius ("Authored iris UV radius", Float) = 0.1155
        _CorneaCurvature ("Convex corneal slope", Range(0.3,1)) = 0.70
        _CorneaForward ("Imported eye forward", Vector) = (0,0,1,0)
        _CorneaRight ("Imported eye UV right", Vector) = (1,0,0,0)
        _CorneaUp ("Imported eye UV up", Vector) = (0,1,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf QiaopiEye fullforwardshadows addshadow exclude_path:deferred exclude_path:prepass
        #pragma target 3.0
        #include "CharacterSoftLighting.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        half _IrisDarkening, _IrisNeutralising;
        float _IrisRadius, _CorneaCurvature;
        float4 _CorneaForward, _CorneaRight, _CorneaUp;
        struct Input { float2 uv_MainTex; };

        struct SurfaceOutputQiaopiEye
        {
            fixed3 Albedo;
            float3 Normal;
            fixed3 Specular;
            half3 Emission;
            half Smoothness;
            half Occlusion;
            fixed Alpha;
            float3 CorneaNormal;
            half CorneaMask;
        };

        inline half4 LightingQiaopiEye(SurfaceOutputQiaopiEye s, half3 viewDir, UnityGI gi)
        {
            // The iris proxy is concave after its transparent outer shell was
            // removed. Keep that geometry for diffuse detail, but reflect the
            // actual lights on a separate, convex, clear corneal interface.
            half nl = dot(normalize(s.Normal),gi.light.dir);
            half diffuse = saturate((nl+_Wrap)/(1+_Wrap));
            half3 colour = s.Albedo*(gi.indirect.diffuse+gi.light.color*(diffuse*_DiffuseResponse));
            float3 normal = normalize(s.CorneaNormal);
            float3 halfVector = normalize(gi.light.dir+viewDir+float3(0,0,0.000001));
            float nh = saturate(dot(normal,halfVector));
            float nlight = saturate(dot(normal,gi.light.dir));
            float nview = saturate(dot(normal,viewDir));
            // Energy-normalised Blinn lobe: narrowing the highlight must not
            // also erase it. Unlike a painted white dot, its peak moves with
            // the light and the camera, and respects real shadow attenuation.
            float normalization = (_SpecularPower+8)/(8*UNITY_PI);
            float highlight = pow(nh,_SpecularPower)*normalization*_SpecularStrength;
            highlight *= nlight*smoothstep(0,0.15,nview)*s.CorneaMask;
            colour += gi.light.color*highlight;
            return half4(colour,s.Alpha);
        }

        inline void LightingQiaopiEye_GI(SurfaceOutputQiaopiEye s, UnityGIInput data, inout UnityGI gi)
        {
            gi = UnityGlobalIllumination(data,s.Occlusion,s.Normal);
        }

        void surf(Input IN, inout SurfaceOutputQiaopiEye o)
        {
            half3 source = QiaopiToLinear(tex2D(_MainTex,IN.uv_MainTex).rgb);
            half brightest = max(source.r,max(source.g,source.b));
            half darkest = min(source.r,min(source.g,source.b));
            half saturation = (brightest-darkest)/max(brightest,0.0001);
            half luminance = dot(source,half3(0.2126,0.7152,0.0722));
            // The source atlas has intensely orange-red irises. Affect only the
            // coloured dark pixels; preserve the pupil, pale sclera and its veins.
            half irisMask = smoothstep(0.30,0.65,saturation)*(1-smoothstep(0.32,0.50,luminance));
            half3 brown = luminance*half3(1.45,1.05,0.70);
            half3 iris = lerp(source,brown,_IrisNeutralising)*_IrisDarkening;
            half3 sclera = source*QiaopiToLinear(_Color.rgb);
            o.Albedo = QiaopiToProjectSpace(lerp(sclera,iris,irisMask));
            o.Specular = 0;
            o.Smoothness = 0.25;
            o.Occlusion = 1;
            o.Alpha = 1;
            o.Emission = 0;
            float2 fromFirst = IN.uv_MainTex-float2(0.292067,0.290036);
            float2 fromSecond = IN.uv_MainTex-float2(0.707838,0.702067);
            float2 radial = dot(fromFirst,fromFirst)<dot(fromSecond,fromSecond)?fromFirst:fromSecond;
            radial /= max(_IrisRadius,0.001);
            float radius = length(radial);
            o.CorneaMask = 1-smoothstep(0.94,1.15,radius);
            float3 forward = UnityObjectToWorldNormal(_CorneaForward.xyz);
            float3 right = UnityObjectToWorldDir(_CorneaRight.xyz);
            float3 up = UnityObjectToWorldDir(_CorneaUp.xyz);
            o.CorneaNormal = normalize(forward+_CorneaCurvature*(radial.x*right+radial.y*up));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
