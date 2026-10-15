Shader "Qiaopi/OilFlame"
{
    Properties { _Color ("Flame color", Color) = (1, 0.82, 0.25, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.position=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float core=saturate(1-abs(i.uv.y-.38)*3);
                return fixed4(lerp(_Color.rgb,fixed3(1,.97,.76),core),1);
            }
            ENDCG
        }
    }
}
