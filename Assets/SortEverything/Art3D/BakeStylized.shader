// Editor-only bake shader for the object art (Sort Everything ▸ Bake 3D Object Art). Never used at runtime:
// the baked PNG sprites are what ship. One stylized lighting model for every prop: a soft top-left key light with
// light wrap and a warm shadow color, a gentle fill, a warm/cool ambient hemisphere, baked ambient occlusion
// (vertex color R), controlled Blinn highlights, a soft fresnel rim, optional polished-metal reflection of a warm
// room gradient, and optional transparency for clear plastic or glass.
Shader "SortEverything/BakeStylized"
{
    Properties
    {
        _Color ("Base", Color) = (1,1,1,1)
        _ShadeColor ("Core shadow", Color) = (0.5,0.35,0.3,1)
        _Tint2 ("Gradient color (a = strength)", Color) = (0,0,0,0)
        _GradAxis ("Gradient axis (object space)", Vector) = (0,1,0,0)
        _GradRange ("Gradient range", Vector) = (-0.5,0.5,0,0)
        _Spec ("Specular", Float) = 0.25
        _Gloss ("Gloss", Float) = 0.4
        _Rim ("Rim", Float) = 0.15
        _Wrap ("Wrap", Float) = 0.35
        _Metal ("Metal", Float) = 0
        _Alpha ("Alpha", Float) = 1
        _NoiseAmt ("Noise", Float) = 0.03
        _NoiseScale ("Noise scale", Float) = 9
        _AOStrength ("AO strength", Float) = 0.55
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 0
        _ZWrite ("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _ShadeColor, _Tint2, _GradAxis, _GradRange;
                float _Spec, _Gloss, _Rim, _Wrap, _Metal, _Alpha, _NoiseAmt, _NoiseScale, _AOStrength;
                float _SrcBlend, _DstBlend, _ZWrite;
            CBUFFER_END

            float4 _SE_KeyDir, _SE_FillDir, _SE_KeyColor, _SE_FillColor, _SE_Sky, _SE_Ground;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float ao : TEXCOORD3;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                o.ao = v.color.r;
                return o;
            }

            float hash3(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float vnoise(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash3(i), hash3(i + float3(1,0,0)), f.x), lerp(hash3(i + float3(0,1,0)), hash3(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash3(i + float3(0,0,1)), hash3(i + float3(1,0,1)), f.x), lerp(hash3(i + float3(0,1,1)), hash3(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            float3 roomEnv(float3 r)
            {
                // A warm kitchen around the object: bright window up-left, cream ceiling, warm wood below.
                float3 top = float3(1.0, 0.96, 0.88), horizon = float3(0.86, 0.74, 0.62), floorC = float3(0.36, 0.25, 0.18);
                float3 c = r.y > 0 ? lerp(horizon, top, saturate(r.y * 1.4)) : lerp(horizon, floorC, saturate(-r.y * 2.0));
                float win = pow(saturate(dot(normalize(r), normalize(_SE_KeyDir.xyz))), 24.0);
                return c + win * 0.9;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 L = normalize(_SE_KeyDir.xyz);
                float3 F = normalize(_SE_FillDir.xyz);

                // Material color with an optional gradient and subtle organic tone variation.
                float3 baseC = _Color.rgb;
                float g = smoothstep(_GradRange.x, _GradRange.y, dot(i.positionOS, normalize(_GradAxis.xyz)));
                baseC = lerp(baseC, lerp(_Tint2.rgb, baseC, g), _Tint2.a);
                float n = vnoise(i.positionOS * _NoiseScale) * 0.65 + vnoise(i.positionOS * _NoiseScale * 2.7) * 0.35;
                baseC *= 1.0 + (n - 0.5) * 2.0 * _NoiseAmt;
                float3 shadeC = _ShadeColor.rgb * (baseC / max(_Color.rgb, 0.001));

                // Key light with wrap: a soft terminator and a warm core shadow instead of grey.
                float ndl = dot(N, L);
                float key = saturate((ndl + _Wrap) / (1.0 + _Wrap));
                key = smoothstep(0.0, 1.0, key);
                float3 col = lerp(shadeC, baseC, key) * lerp(float3(1,1,1), _SE_KeyColor.rgb, key);
                // Gentle fill from the right and a warm/cool ambient hemisphere.
                col += baseC * saturate(dot(N, F)) * _SE_FillColor.rgb * 0.22;
                col += baseC * lerp(_SE_Ground.rgb, _SE_Sky.rgb, N.y * 0.5 + 0.5) * 0.12;
                // Ambient occlusion (baked per vertex).
                col *= lerp(1.0 - _AOStrength, 1.0, i.ao);

                // Polished metal reflects the warm room.
                float3 R = reflect(-V, N);
                col = lerp(col, roomEnv(R) * lerp(float3(1,1,1), baseC * 1.25, 0.55) * lerp(0.7, 1.0, i.ao), _Metal);

                // Controlled highlight (never a harsh white spot) and a soft fresnel rim.
                float3 H = normalize(L + V);
                float shin = lerp(8.0, 110.0, _Gloss);
                float spec = pow(saturate(dot(N, H)), shin) * _Spec * (shin + 8.0) / 64.0;
                spec = min(spec, 0.85) * smoothstep(-0.1, 0.3, ndl) * i.ao;
                col += spec * float3(1.0, 0.97, 0.9);
                float fres = pow(1.0 - saturate(dot(N, V)), 3.0);
                col += fres * _Rim * float3(1.0, 0.93, 0.82) * (0.45 + 0.55 * saturate(ndl + 0.3));

                float a = saturate(lerp(_Alpha, 1.0, fres * 0.7 * (1.0 - _Alpha)) + spec * (1.0 - _Alpha));
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
