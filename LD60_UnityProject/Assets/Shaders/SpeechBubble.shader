Shader "LD60/UI/SpeechBubble"
{
    Properties
    {
        [PerRendererData] [MainTexture] _MainTex ("Sprite", 2D) = "white" {}

        [Header(Pixelize)]
        [Space(4)]
        _Pixelize ("Pixelize", Float) = 131.75
        _PixelizeOffset ("Pixelize Offset", Vector) = (0, 0, 0, 0)

        [Header(Noise)]
        [Space(4)]
        _NoiseScale ("Noise Scale", Float) = 55.27
        _NoiseMulty ("Noise Opacity", Range(0, 1)) = 0.3

        [Header(Gradient)]
        [Space(4)]
        [ToggleUI] _Gradient ("Gradient Enabled", Float) = 1
        [ToggleUI] _GradientUV ("Gradient From UV Instead Of Canvas Position", Float) = 1
        _GradientTiling ("Gradient Tiling", Vector) = (1, -0.78, 0, 0)
        _GradientOffset ("Gradient Offset", Vector) = (0.9, 0.45, 0, 0)
        _GradientRotation ("Gradient Rotation (degrees)", Float) = 248.51

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] [ToggleUI] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            Name "SpeechBubble"

            Cull Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            ColorMask [_ColorMask]
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.5

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

            static const float3 GradientDarkStopLinear = 0.05660379;
            static const float3 GradientLightStopLinear = 1.0;
            static const float GradientLightStopPosition = 0.8941177;
            static const float GradientPosterizeSteps = 18.0;
            static const float NoiseOctaveAmplitudes[3] = { 0.125, 0.25, 0.5 };
            static const float NoiseOctaveFrequencies[3] = { 1.0, 0.5, 0.25 };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _ClipRect;
            float2 _PixelizeOffset;
            float2 _GradientTiling;
            float2 _GradientOffset;
            float _GradientRotation;
            float _Pixelize;
            float _NoiseScale;
            float _NoiseMulty;
            float _Gradient;
            float _GradientUV;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 positionCanvas : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionCanvas = input.positionOS.xy;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 inCell = frac(uv);
                inCell = inCell * inCell * (3.0 - 2.0 * inCell);

                float cornerBL, cornerBR, cornerTL, cornerTR;
                Hash_Tchou_2_1_float(cell + float2(0.0, 0.0), cornerBL);
                Hash_Tchou_2_1_float(cell + float2(1.0, 0.0), cornerBR);
                Hash_Tchou_2_1_float(cell + float2(0.0, 1.0), cornerTL);
                Hash_Tchou_2_1_float(cell + float2(1.0, 1.0), cornerTR);

                float bottom = lerp(cornerBL, cornerBR, inCell.x);
                float top = lerp(cornerTL, cornerTR, inCell.x);
                return lerp(bottom, top, inCell.y);
            }

            float FractalValueNoise(float2 uv, float scale)
            {
                float noise = 0.0;

                [unroll]
                for (int octave = 0; octave < 3; octave++)
                    noise += ValueNoise(uv * (scale * NoiseOctaveFrequencies[octave])) * NoiseOctaveAmplitudes[octave];

                return noise;
            }

            float4 BlendOverlay(float4 base, float4 blend, float opacity)
            {
                float4 screened = 1.0 - 2.0 * (1.0 - base) * (1.0 - blend);
                float4 multiplied = 2.0 * base * blend;
                float4 baseIsDark = step(base, 0.5);
                return lerp(base, multiplied * baseIsDark + (1.0 - baseIsDark) * screened, opacity);
            }

            float4 SamplePosterizedGradient(float position)
            {
                float3 ramp = lerp(GradientDarkStopLinear, GradientLightStopLinear,
                                   saturate(position / GradientLightStopPosition));

                #ifdef UNITY_COLORSPACE_GAMMA
                ramp = LinearToSRGB(ramp);
                #endif

                return float4(floor(ramp * GradientPosterizeSteps) / GradientPosterizeSteps, 1.0);
            }

            float GradientPositionAlongSweep(float2 sweep, float rotationDegrees)
            {
                float sine, cosine;
                sincos(radians(rotationDegrees), sine, cosine);
                return (0.5 + 0.5 * cosine) * sweep.x + (0.5 - 0.5 * cosine) * sweep.y;
            }

            float RectMask2DCoverage(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 pixelizedUV = floor(input.uv * _Pixelize) / _Pixelize;
                float2 sampleUV = pixelizedUV + _PixelizeOffset;

                float4 tint = 1.0;
                if (_Gradient > 0.5)
                {
                    float2 gradientSource = _GradientUV > 0.5 ? pixelizedUV : input.positionCanvas;
                    float2 sweep = gradientSource * _GradientTiling + _GradientOffset;
                    tint = SamplePosterizedGradient(GradientPositionAlongSweep(sweep, _GradientRotation));
                }

                float4 color = tint * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                color = BlendOverlay(color, FractalValueNoise(sampleUV, _NoiseScale).xxxx, _NoiseMulty);
                color *= input.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= RectMask2DCoverage(input.positionCanvas, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
