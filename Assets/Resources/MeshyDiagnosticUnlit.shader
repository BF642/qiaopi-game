Shader "Qiaopi/MeshyDiagnosticUnlit" {
Properties { _MainTex("Source colour",2D)="white"{} }
SubShader { Tags {"RenderType"="Opaque"} Cull Off Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct input { float4 vertex:POSITION;float2 uv:TEXCOORD0;};
struct output {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
sampler2D _MainTex;
output vert(input v){output o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
fixed4 frag(output i):SV_Target{return tex2D(_MainTex,i.uv);}
ENDCG
} } }
