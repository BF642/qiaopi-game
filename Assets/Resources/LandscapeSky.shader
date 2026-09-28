Shader "Qiaopi/LandscapeSky"
{
    Properties
    {
        _Zenith ("Upper sky", Color) = (0.39,0.59,0.69,1)
        _Horizon ("Horizon haze", Color) = (0.76,0.82,0.80,1)
        _Cloud ("Cloud light", Color) = (0.95,0.94,0.86,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Zenith, _Horizon, _Cloud;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION; float3 direction:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p) { float2 i=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y); }
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction);
                float elevation=saturate(d.y);
                float3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(elevation,.55));
                float2 uv=d.xz/max(.14,d.y+.15)*1.65+float2(_Time.y*.002,0);
                float n=noise(uv)*.57+noise(uv*2.1)*.28+noise(uv*4.3)*.15;
                float cloud=smoothstep(.51,.77,n)*smoothstep(.035,.20,d.y)*(1-smoothstep(.70,1,d.y));
                color=lerp(color,_Cloud.rgb,cloud*.72);
                float sun=dot(d,normalize(float3(-.48,.48,-.72)));
                color+=float3(1,.82,.51)*(pow(saturate(sun),28)*.075+smoothstep(.99925,.9998,sun)*.55);
                return fixed4(color,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
