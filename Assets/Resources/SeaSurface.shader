Shader "Qiaopi/SeaSurface"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.125, 0.373, 0.471, 1)
        _WaterColor ("Moving water", Color) = (0.231, 0.522, 0.573, 1)
        _ShallowColor ("Shallow water", Color) = (0.525, 0.678, 0.635, 1)
        _FoamColor ("Aerated foam", Color) = (0.898, 0.914, 0.851, 1)
        _GlintColor ("Sun fragments", Color) = (0.953, 0.922, 0.816, 1)
        _HorizonColor ("Sky horizon", Color) = (0.616, 0.686, 0.710, 1)
        _SeaFogRange ("Sea fog start / end / override", Vector) = (300, 850, 1, 0)
        _SeaKind ("Harbor 0 / cargo port 1 / ship 2", Float) = 0
        _SeaTime ("Sea clock", Float) = 0
        _WaveStrength ("Wave normal strength", Range(0, 2)) = 1
        _GlintStrength ("Sun fragment strength", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4 _DeepColor, _WaterColor, _ShallowColor, _FoamColor, _GlintColor, _HorizonColor;
            float4 _SeaFogRange;
            float _SeaKind, _SeaTime, _WaveStrength, _GlintStrength;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
                float2 sea : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            // A sine-free hash keeps the procedural surface inexpensive on Metal.
            float hash21(float2 p)
            {
                float3 q = frac(float3(p.x, p.y, p.x) * .1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }
            float noise2(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(cell), hash21(cell + float2(1, 0)), f.x),
                            lerp(hash21(cell + float2(0, 1)), hash21(cell + float2(1, 1)), f.x), f.y);
            }
            float boxDistance(float2 p, float2 halfSize)
            {
                float2 q = abs(p) - halfSize;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }
            float shorelineDistance(float2 p)
            {
                if (_SeaKind > 1.5) return 10000.0;
                // Port land extends indefinitely southwest; the visible water lies
                // east of x=36 and north of z=38, with a rounded corner distance.
                if (_SeaKind > .5) return length(max(p - float2(36, 38), 0.0));
                // Xiamen's main coast continues east/west behind the finite pier.
                float apron = p.y - 22.0;
                float pier = boxDistance(p - float2(0, 30), float2(6, 8));
                return max(0.0, min(apron, pier));
            }

            v2f vert(appdata v)
            {
                v2f o;
                float2 p = v.vertex.xz;
                // Long, very low swells change geometry without flooding a quay or deck.
                v.vertex.y += .016 * sin(dot(p, float2(.052, .033)) + _SeaTime * .28)
                            + .010 * sin(dot(p, float2(-.039, .075)) - _SeaTime * .23);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.sea = p;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            // The wake is part of the opaque sea, not a stack of transparent ribbons.
            // x +/-11 and z +/-30 are the hull footprint; stern turbulence travels -z.
            void shipWake(float2 p, float t, float breakup, out float churn, out float foam)
            {
                churn = 0.0; foam = 0.0;
                if (_SeaKind < 1.5) return;
                float trailing = max(-p.y - 29.0, 0.0);
                float sternGate = 1.0 - smoothstep(-33.0, -27.0, p.y);
                float age = 1.0 - smoothstep(58.0, 185.0, trailing);
                float width = 5.5 + trailing * .095;
                float wandering = sin(trailing * .067 - t * .18) * (1.0 + trailing * .012);
                float across = abs(p.x - wandering);
                float core = 1.0 - smoothstep(width * .28, width, across);
                churn = core * age * sternGate;
                float rim = 1.0 - smoothstep(.35, 2.3 + trailing * .014, abs(across - width * .82));
                float stream = .5 + .5 * sin(trailing * .47 - t * 1.8 + breakup * 4.0);
                float bubbles = smoothstep(.33, .77, breakup + stream * .16);
                foam = (core * .46 + rim * .43) * bubbles * age * sternGate;

                float sideGate = smoothstep(-36.0, -27.0, p.y) * (1.0 - smoothstep(23.0, 31.0, p.y));
                float sidePosition = 11.5 + .3 * sin(p.y * .24 + t * .5) + max(20.0 - p.y, 0.0) * .012;
                float side = 1.0 - smoothstep(.2, 1.65, abs(abs(p.x) - sidePosition));
                foam += side * sideGate * (.3 + .7 * bubbles) * .82;
                churn = max(churn, side * sideGate * .57);

                // The bow wave opens outward and aft; the hull hides its inner portion.
                float bowLine = 31.0 - abs(p.x) * .57;
                float bow = 1.0 - smoothstep(.3, 1.45, abs(p.y - bowLine));
                bow *= (1.0 - smoothstep(18.0, 30.0, abs(p.x))) * smoothstep(11.0, 19.0, p.y);
                foam += bow * (.28 + .72 * bubbles) * .83;
                churn = max(churn, bow * .5);
                foam = saturate(foam);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.sea;
                float t = _SeaTime;
                float cameraDistance = distance(_WorldSpaceCameraPos, i.world);
                float detailFade = 1.0 - smoothstep(100.0, 300.0, cameraDistance);
                float broad = noise2(p * .018 + float2(t * .002, -t * .001));
                float patches = noise2(p * .095 + float2(-t * .018, t * .011));
                float breakup = noise2(p * .58 + float2(t * .09, t * .40)) * .68
                              + noise2(p * 1.07 + float2(-t * .07, t * .54)) * .32;

                // Several nonparallel wavelengths avoid a regular tiled or checker pattern.
                float a = dot(p, float2(.48, .24)) - t * .72 + patches * 1.35;
                float b = dot(p, float2(-.29, .82)) + t * 1.02 + broad * 2.1;
                float c = dot(p, float2(.12, -.18)) + t * .41;
                float waveA = sin(a), waveB = sin(b), waveC = sin(c);
                float2 slope = float2(.48, .24) * cos(a) * .34
                             + float2(-.29, .82) * cos(b) * .20 * detailFade
                             + float2(.12, -.18) * cos(c) * .46;
                float3 normal = normalize(float3(-slope.x * _WaveStrength, 1.0, -slope.y * _WaveStrength));
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz + float3(.0001, .0001, .0001));
                float3 viewDirection = normalize(lerp(_WorldSpaceCameraPos - i.world, UNITY_MATRIX_V[2].xyz, unity_OrthoParams.w));
                float facing = saturate(dot(normal, viewDirection));
                float fresnel = pow(1.0 - facing, 3.0);

                float coast = shorelineDistance(p);
                float shallows = exp(-coast * .095);
                float colorVariation = saturate(.29 + broad * .30 + (waveA * .095 + waveC * .06) * detailFade);
                float3 color = lerp(_DeepColor.rgb, _WaterColor.rgb, colorVariation);
                color = lerp(color, _ShallowColor.rgb, shallows * (.35 + broad * .27));
                color *= .89 + .16 * saturate(dot(normal, lightDirection));
                color += _WaterColor.rgb * (.035 * waveB * detailFade);
                color = lerp(color, _HorizonColor.rgb, fresnel * .31);

                float churn, wakeFoam;
                shipWake(p, t, breakup, churn, wakeFoam);
                color = lerp(color, lerp(_WaterColor.rgb, _ShallowColor.rgb, .58), churn * .56);

                // Broken narrow crests have different timing from the broad water color.
                float wash = .5 + .5 * sin(coast * 2.7 - t * .88 + patches * 4.0);
                float shoreFoam = (1.0 - smoothstep(.15, 2.1, coast))
                                * smoothstep(.22, .78, breakup + wash * .18) * .59;
                float whitecaps = smoothstep(.87, .995, waveA * .72 + waveB * .28)
                                * smoothstep(.55, .79, patches) * .30 * detailFade;
                float foam = saturate(shoreFoam + wakeFoam + whitecaps);

                // A directional highlight plus sparse, broad sun fragments keeps the
                // sea legible from the game's high orthographic camera as well as V view.
                float3 halfDirection = normalize(lightDirection + viewDirection);
                float specular = pow(saturate(dot(normal, halfDirection)), 68.0);
                float glint = smoothstep(.78, .995, waveB) * smoothstep(.57, .83, breakup)
                            * (.22 + .78 * smoothstep(.36, .72, patches)) * detailFade;
                color += _GlintColor.rgb * (specular * .34 + glint * .30) * _GlintStrength;
                color = lerp(color, _FoamColor.rgb, foam * .88);

                float horizon = smoothstep(_SeaFogRange.x, max(_SeaFogRange.x + 1.0, _SeaFogRange.y), cameraDistance);
                float3 horizonWater = lerp(color, _HorizonColor.rgb, horizon);
                fixed4 sceneFog = fixed4(horizonWater, 1);
                UNITY_APPLY_FOG(i.fogCoord, sceneFog);
                // Ports and ships may use the longer water-specific fade even when
                // buildings use a shorter Unity fog range. Both paths compile on Metal.
                color = lerp(sceneFog.rgb, horizonWater, saturate(_SeaFogRange.z));
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
