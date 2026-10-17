Shader "LD60/Background/Stars"
{
    Properties
    {
        [Header(Colors)]
        [Space(4)]
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _StarColorAlt ("Star Color Alt", Color) = (0.55, 0.75, 1, 1)
        _ColorVariance ("Color Variance", Range(0, 1)) = 0.6
        _BackgroundColor ("Background", Color) = (0.015, 0.02, 0.05, 1)

        [Header(Pixel Grid)]
        [Space(4)]
        _PixelSize ("Pixel Size (world units)", Range(0.001, 1)) = 0.05

        [Header(Layers from far to near)]
        [Space(4)]
        _LayerCell ("Cell Size in pixels (far, mid, near)", Vector) = (6, 10, 16, 0)
        _LayerFill ("Fill Chance (far, mid, near)", Vector) = (0.06, 0.04, 0.025, 0)
        _LayerBrightness ("Brightness (far, mid, near)", Vector) = (0.35, 0.65, 1, 0)
        _LayerParallax ("Parallax (far, mid, near)", Vector) = (0.08, 0.25, 0.55, 0)
        _Drift ("Drift (world units per sec, scaled by parallax)", Vector) = (0, 0, 0, 0)

        [Header(Twinkle)]
        [Space(4)]
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.5
        _TwinkleSpeed ("Twinkle Speed (cycles per sec)", Range(0, 4)) = 0.35

        [Header(Sparkle)]
        [Space(4)]
        _SparkleChance ("Sparkle Chance", Range(0, 1)) = 0.2
        _SparkleSpeed ("Sparkle Speed (flashes per sec)", Range(0, 4)) = 0.25
        _SparkleLength ("Sparkle Arm Length (pixels)", Range(0, 6)) = 2
        _SparkleSharpness ("Sparkle Sharpness", Range(1, 16)) = 6
        _SparkleBoost ("Sparkle Brightness Boost", Range(0, 2)) = 0.6

        [Header(Shape)]
        [Space(4)]
        _BigStarChance ("Big Star Chance", Range(0, 1)) = 0.12

        [Header(Emission)]
        [Space(4)]
        _Emission ("Emission (bloom multiplier, stars only)", Float) = 1

        [Header(Fade)]
        [Space(4)]
        _EdgeFade ("Edge Fade (UV)", Range(0, 0.5)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "BackgroundStars"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

            static const float SparkleArmBrightness = 0.8;

            static const float2 LayerSeedFar = float2(0, 0);
            static const float2 LayerSeedMid = float2(1024, 3072);
            static const float2 LayerSeedNear = float2(5120, 7168);

            CBUFFER_START(UnityPerMaterial)
            float4 _StarColor;
            float4 _StarColorAlt;
            float4 _BackgroundColor;
            float4 _LayerCell;
            float4 _LayerFill;
            float4 _LayerBrightness;
            float4 _LayerParallax;
            float4 _Drift;
            float _ColorVariance;
            float _PixelSize;
            float _TwinkleAmount;
            float _TwinkleSpeed;
            float _SparkleChance;
            float _SparkleSpeed;
            float _SparkleLength;
            float _SparkleSharpness;
            float _SparkleBoost;
            float _BigStarChance;
            float _Emission;
            float _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 positionWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(world);
                output.positionWS = world.xy;
                output.uv = input.uv;
                return output;
            }

            float2 LayerPixel(float2 basePixel, float parallax, float pixelSize)
            {
                float2 offset = _WorldSpaceCameraPos.xy * (1.0 - parallax) - _Drift.xy * parallax * _Time.y;
                return basePixel - round(offset / pixelSize);
            }

            float4 StarLayer(float2 pixel, float cellPixels, float fill, float brightness, float2 seed)
            {
                float cell = max(round(cellPixels), 2.0);
                float2 cellIndex = floor(pixel / cell);
                float maxArms = round(_SparkleLength);
                float4 best = 0.0;
                float bestWeight = 0.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 index = cellIndex + float2(x, y);

                        float3 roll;
                        Hash_Tchou_2_3_float(index * 2.0 + seed, roll);
                        if (roll.x > fill)
                            continue;

                        float2 center = index * cell + floor(roll.yz * cell);
                        float2 offset = abs(pixel - center);
                        float reach = max(offset.x, offset.y);
                        if (reach > maxArms + 1.5)
                            continue;

                        float3 trait;
                        Hash_Tchou_2_3_float(index * 2.0 + seed + 1.0, trait);

                        float phase = trait.z;
                        float rate = lerp(0.6, 1.4, frac(trait.z * 7.13));

                        float twinkle = 1.0 - _TwinkleAmount * 0.5 *
                                        (1.0 + sin((_Time.y * _TwinkleSpeed * rate + phase) * TWO_PI));

                        float sparkles = step(frac(trait.y * 17.0), _SparkleChance);
                        float beat = sparkles * pow(saturate(sin((_Time.y * _SparkleSpeed * rate + phase) * TWO_PI)),
                                                    _SparkleSharpness);

                        float arms = floor(beat * (maxArms + 0.999));

                        float big = step(trait.x, _BigStarChance);
                        float core = lerp(step(reach, 0.5), step(offset.x + offset.y, 1.5), big);

                        float onAxis = step(min(offset.x, offset.y), 0.5);
                        float arm = onAxis * step(reach, arms + 0.5) * SparkleArmBrightness *
                                    saturate(1.0 - (reach - 1.0) / max(arms, 1.0));

                        float shape = max(core, arm);
                        if (shape <= 0.0)
                            continue;

                        float4 tint = lerp(_StarColor, _StarColorAlt, trait.y * _ColorVariance);

                        float coverage = shape * brightness * lerp(0.55, 1.0, frac(roll.x * 13.0)) *
                                         twinkle * tint.a;

                        float flash = 1.0 + _SparkleBoost * beat;

                        float weight = coverage * flash;
                        if (weight > bestWeight)
                        {
                            bestWeight = weight;
                            best = float4(tint.rgb * flash * _Emission, coverage);
                        }
                    }
                }

                return best;
            }

            half4 CompositeOver(half4 src, half4 dst)
            {
                half alpha = src.a + dst.a * (1.0 - src.a);
                half3 rgb = (src.rgb * src.a + dst.rgb * dst.a * (1.0 - src.a)) / max(alpha, 1e-5);
                return half4(rgb, alpha);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float pixelSize = max(_PixelSize, 1e-4);

                float2 basePixel = floor(input.positionWS / pixelSize);

                float4 star = StarLayer(LayerPixel(basePixel, _LayerParallax.x, pixelSize),
                                        _LayerCell.x, _LayerFill.x, _LayerBrightness.x, LayerSeedFar);

                float4 midLayer = StarLayer(LayerPixel(basePixel, _LayerParallax.y, pixelSize),
                                       _LayerCell.y, _LayerFill.y, _LayerBrightness.y, LayerSeedMid);
                star = midLayer.w > star.w ? midLayer : star;

                float4 nearLayer = StarLayer(LayerPixel(basePixel, _LayerParallax.z, pixelSize),
                                        _LayerCell.z, _LayerFill.z, _LayerBrightness.z, LayerSeedNear);
                star = nearLayer.w > star.w ? nearLayer : star;

                half4 color = CompositeOver(half4(star.rgb, saturate(star.w)), _BackgroundColor);

                float2 toEdge = min(input.uv, 1.0 - input.uv);
                float fade = _EdgeFade > 0.0 ? smoothstep(0.0, _EdgeFade, min(toEdge.x, toEdge.y)) : 1.0;

                return half4(color.rgb, saturate(color.a * fade));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
