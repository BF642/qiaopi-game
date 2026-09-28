Shader "Qiaopi/HistoricSurface"
{
    Properties
    {
        _Color ("Original palette colour", Color) = (1,1,1,1)
        _SurfaceTex ("Packed surface detail", 2D) = "gray" {}
        _TileMetres ("World metres per texture tile", Vector) = (1.6,1.6,0,0)
        _DetailStrength ("Local colour variation", Range(0,1)) = .8
        _MacroStrength ("Broad age variation", Range(0,.4)) = .1
        _Relief ("Physical relief in metres", Range(0,.015)) = .002
        _Weathering ("Lower wall weathering", Range(0,.5)) = .2
        _CavityStrength ("Fine cavity shading", Range(0,1)) = .25
        _BrickBond ("Lime joint colour", Range(0,1)) = 0
        _JointColor ("Mortar colour", Color) = (.66,.63,.54,1)
        _Glossiness ("Worn surface smoothness", Range(0,1)) = .1
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        // The generated built-in Standard passes retain Unity fog, forward shadows,
        // shadow casting and light probes. No unlit approximation or extra passes.
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"

        sampler2D _SurfaceTex;
        fixed4 _Color, _JointColor;
        float4 _TileMetres;
        half _DetailStrength, _MacroStrength, _Relief, _Weathering, _CavityStrength, _BrickBond;
        half _Glossiness, _Metallic;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v)
        {
            // Most WorldFactory meshes have no tangents. A local orthogonal frame
            // lets the Surface Shader convert our world-space normal correctly.
            float3 n = normalize(v.normal);
            float3 reference = abs(n.y) > .95 ? float3(0,0,1) : float3(0,1,0);
            v.tangent = float4(normalize(cross(reference, n)), 1);
        }

        float hash21(float2 p)
        {
            float3 q = frac(float3(p.x,p.y,p.x) * .1031);
            q += dot(q, q.yzx + 33.33);
            return frac((q.x + q.y) * q.z);
        }
        float ageNoise(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),
                        lerp(hash21(i+float2(0,1)),hash21(i+float2(1,1)),f.x),f.y);
        }

        float3 reliefNormal(float3 p, float3 n, float height)
        {
            // A surface-gradient bump uses the packed height directly: no normal
            // texture, tangent UVs, additional texture samples or object scale needed.
            float3 dx = ddx(p), dy = ddy(p);
            float3 r1 = cross(dy,n), r2 = cross(n,dx);
            float determinant = dot(dx,r1);
            float3 gradient = sign(determinant) * (ddx(height)*r1 + ddy(height)*r2);
            return normalize((abs(determinant)+1e-7)*n - gradient);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 normal = normalize(WorldNormalVector(IN,float3(0,0,1)));
            float3 weights = abs(normal);
            weights *= weights; weights *= weights;
            weights /= max(dot(weights,float3(1,1,1)),.0001);
            float2 metres = max(_TileMetres.xy,float2(.01,.01));
            // Y-up world projection: walls keep vertical courses; horizontal floors
            // use x/z. Textures remain at their physical size on stretched meshes.
            float4 xSample = tex2D(_SurfaceTex,IN.worldPos.zy/metres);
            float4 ySample = tex2D(_SurfaceTex,IN.worldPos.xz/metres);
            float4 zSample = tex2D(_SurfaceTex,IN.worldPos.xy/metres);
            float4 detail = xSample*weights.x + ySample*weights.y + zSample*weights.z;

            float macro = ageNoise(IN.worldPos.xz*.21 + IN.worldPos.y*float2(.093,.137));
            float brightness = 1.0 + (detail.r-.5)*_DetailStrength + (macro-.5)*_MacroStrength;
            float3 colour = _Color.rgb * brightness;
            float joints = (1.0-smoothstep(.28,.48,detail.a))*_BrickBond;
            colour = lerp(colour,_JointColor.rgb*brightness,joints);

            // Ground-contact staining is restrained and applies to upright surfaces,
            // leaving floors, paper and the tops of furniture free of a dark blanket.
            float vertical = 1.0 - saturate(abs(normal.y));
            float low = 1.0-smoothstep(.04,.32+macro*.47,IN.worldPos.y);
            float weather = vertical*low*_Weathering*(.55+macro*.45);
            colour *= 1.0-weather;
            o.Albedo = max(colour,0.0);
            o.Occlusion = lerp(1.0,detail.b,_CavityStrength);
            o.Metallic = _Metallic;
            o.Smoothness = saturate(_Glossiness + (1.0-detail.g)*.15);
            o.Alpha = _Color.a;

            float3 worldBump = reliefNormal(IN.worldPos,normal,detail.a*_Relief);
            float3 tangent = normalize(WorldNormalVector(IN,float3(1,0,0)));
            float3 bitangent = normalize(WorldNormalVector(IN,float3(0,1,0)));
            o.Normal = normalize(float3(dot(worldBump,tangent),dot(worldBump,bitangent),dot(worldBump,normal)));
        }
        ENDCG
    }
    Fallback "Diffuse"
}
