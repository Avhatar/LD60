Shader "LD60/Background/Grid"
{
    Properties
    {
        [Header(Grid)]
        [Space(4)]
        _CellSize ("Cell Size (world units)", Float) = 1
        _LineThickness ("Line Thickness (world units)", Float) = 0.03
        _LineColor ("Line Color", Color) = (0.35, 0.85, 1, 0.22)

        [Header(Major Lines)]
        [Space(4)]
        _MajorEvery ("Major Every N Cells", Float) = 5
        _MajorThickness ("Major Line Thickness (world units)", Float) = 0.06
        _MajorColor ("Major Line Color", Color) = (0.6, 1, 1, 0.5)

        [Header(Background)]
        [Space(4)]
        _BackgroundColor ("Background", Color) = (0, 0, 0, 0)

        [Header(Motion)]
        [Space(4)]
        _Parallax ("Parallax, 0 pins to the screen and 1 to the world", Range(0, 1)) = 1
        _Scroll ("Scroll (world units per sec)", Vector) = (0, 0, 0, 0)
        _Pulse ("Pulse (amount, cycles per sec)", Vector) = (0.12, 0.3, 0, 0)
        _Scanline ("Scanline (amount, bands per world unit, speed)", Vector) = (0.15, 1.5, 0.4, 0)

        [Header(Pixelize)]
        [Space(4)]
        [Toggle(_PIXELIZE_ON)] _Pixelize ("Snap To Pixel Grid", Float) = 1
        _PixelSize ("Pixel Size (world units)", Range(0.001, 0.5)) = 0.05

        [Header(Fade)]
        [Space(4)]
        _EdgeFade ("Edge Fade (UV)", Range(0, 0.5)) = 0
        _RadialFade ("Radial Fade (inner, outer world radius)", Vector) = (0, 0, 0, 0)
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
            Name "BackgroundGrid"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #pragma shader_feature_local_fragment _PIXELIZE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _LineColor;
            float4 _MajorColor;
            float4 _BackgroundColor;
            float4 _Scroll;
            float4 _Pulse;
            float4 _Scanline;
            float4 _RadialFade;
            float _CellSize;
            float _LineThickness;
            float _MajorEvery;
            float _MajorThickness;
            float _Parallax;
            float _Pixelize;
            float _PixelSize;
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

            float GridLines(float2 coord, float cell, float halfWidth, float2 filterRadius)
            {
                float2 toBoundary = abs(frac(coord / cell - 0.5) - 0.5) * cell;
                float2 overlap = min(toBoundary + filterRadius, halfWidth) -
                                 max(toBoundary - filterRadius, -halfWidth);
                float2 coverage = saturate(overlap / (2.0 * filterRadius));
                return saturate(max(coverage.x, coverage.y));
            }

            half4 CompositeOver(half4 src, half4 dst)
            {
                half alpha = src.a + dst.a * (1.0 - src.a);
                half3 rgb = (src.rgb * src.a + dst.rgb * dst.a * (1.0 - src.a)) / max(alpha, 1e-5);
                return half4(rgb, alpha);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 offset = _WorldSpaceCameraPos.xy * (1.0 - _Parallax) - _Scroll.xy * _Time.y;

                float minor;
                float major;
                float scanCoord;

                #ifdef _PIXELIZE_ON
                    float pixelSize = max(_PixelSize, 1e-4);
                    float2 coord = floor(input.positionWS / pixelSize) - round(offset / pixelSize);

                    float cell = max(round(_CellSize / pixelSize), 2.0);
                    float majorCell = cell * max(round(_MajorEvery), 1.0);

                    float minorWidth = max(floor(_LineThickness / pixelSize * 0.5) * 2.0 + 1.0, 1.0);
                    float majorWidth = max(floor(_MajorThickness / pixelSize * 0.5) * 2.0 + 1.0, 1.0);
                    float2 filterRadius = 0.5;

                    minor = GridLines(coord, cell, minorWidth * 0.5, filterRadius) *
                            saturate(_LineThickness / pixelSize);
                    major = GridLines(coord, majorCell, majorWidth * 0.5, filterRadius) *
                            saturate(_MajorThickness / pixelSize);
                    scanCoord = coord.y * pixelSize;
                #else
                    float2 coord = input.positionWS - offset;
                    float2 filterRadius = fwidth(coord) * 0.5 + 1e-6;

                    float cell = max(_CellSize, 1e-4);
                    float majorCell = cell * max(_MajorEvery, 1.0);

                    minor = GridLines(coord, cell, _LineThickness * 0.5, filterRadius);
                    major = GridLines(coord, majorCell, _MajorThickness * 0.5, filterRadius);
                    scanCoord = coord.y;
                #endif

                minor = saturate(minor - major);

                float pulse = 1.0 + _Pulse.x * sin(_Time.y * _Pulse.y * TWO_PI);
                float scan = 1.0 - _Scanline.x * 0.5 *
                             (1.0 + sin((scanCoord * _Scanline.y + _Time.y * _Scanline.z) * TWO_PI));
                float modulation = saturate(pulse * scan);

                half4 color = _BackgroundColor;
                color = CompositeOver(half4(_LineColor.rgb, minor * _LineColor.a * modulation), color);
                color = CompositeOver(half4(_MajorColor.rgb, major * _MajorColor.a * modulation), color);

                float2 toEdge = min(input.uv, 1.0 - input.uv);
                float fade = _EdgeFade > 0.0 ? smoothstep(0.0, _EdgeFade, min(toEdge.x, toEdge.y)) : 1.0;

                if (_RadialFade.y > _RadialFade.x)
                {
                    float2 origin = float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13);
                    fade *= 1.0 - smoothstep(_RadialFade.x, _RadialFade.y, length(input.positionWS - origin));
                }

                return half4(color.rgb, saturate(color.a * fade));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
