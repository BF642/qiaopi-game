Shader "Qiaopi/PeriodCloth"
{
    Properties
    {
        _Color ("Dyed cotton", Color) = (.29,.39,.38,1)
        _WeaveScale ("Threads per metre", Float) = 420
        _WeaveAmount ("Subtle cloth weave", Range(0,.08)) = .035
        _Wrap ("Cloth diffuse wrap", Range(0,.5)) = .18
        _DiffuseResponse ("Diffuse response", Range(.5,1)) = .88
        _SpecularStrength ("Matte fibre reflection", Range(0,.05)) = .004
        _SpecularPower ("Broad fibre highlight", Range(8,64)) = 12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf QiaopiSoft fullforwardshadows addshadow vertex:vert exclude_path:deferred
        #pragma target 3.0
        #include "CharacterSoftLighting.cginc"
        fixed4 _Color;
        half _WeaveScale, _WeaveAmount;
        struct Input { float3 clothPosition; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            // FBX bodies may be authored in centimetres. Keep the weave's
            // frequency in game metres and attached to the moving garment.
            float metresPerUnit = length(mul((float3x3)unity_ObjectToWorld,float3(1,0,0)));
            o.clothPosition = v.vertex.xyz * metresPerUnit;
        }
        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Geometric folds stay in the garment. Fine woven fibres fade
            // below a pixel rather than turning into stripes at a distance.
            float3 threads = IN.clothPosition * _WeaveScale;
            float footprint = max(length(ddx(threads)),length(ddy(threads)));
            half visible = 1-smoothstep(.35,1.1,footprint);
            half fibre = (sin(threads.x*6.28318)*sin(threads.y*6.28318)
                + sin(threads.z*6.28318)*.35)*.5;
            o.Albedo = _Color.rgb * (1 + fibre*_WeaveAmount*visible);
            o.Specular = 0;
            o.Smoothness = .06;
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
