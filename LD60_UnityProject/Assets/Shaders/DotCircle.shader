Shader "LD60/Stylize/DotCircle"
{
    Properties
    {
        [MainTexture] _MainTex ("Source", 2D) = "white" {}
        _DotSize ("Dot Size (px)", Range(2, 128)) = 7
        _DotRadius ("Dot Radius", Range(0.05, 1.5)) = 0.75
        _DotSoftness ("Edge Softness", Range(0, 1)) = 0
        _BackgroundColor ("Background", Color) = (0, 0, 0, 0)
        _Resolution ("Resolution Override (0 = auto)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "DotCircle"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _BackgroundColor;
            float4 _Resolution;
            float _DotSize;
            float _DotRadius;
            float _DotSoftness;
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
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float2 ResolveResolution()
            {
                if (_Resolution.x >= 1.0 && _Resolution.y >= 1.0)
                    return _Resolution.xy;
                if (_MainTex_TexelSize.z >= 1.0 && _MainTex_TexelSize.w >= 1.0)
                    return _MainTex_TexelSize.zw;

                return _ScreenParams.xy;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 resolution = ResolveResolution();
                float cellSize = max(2.0, floor(_DotSize));
                float radius = cellSize * 0.5 * _DotRadius;

                float2 pixel = input.uv * resolution;
                float2 cellOrigin = floor(pixel / cellSize) * cellSize;
                float2 cellCenter = cellOrigin + cellSize * 0.5;

                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, cellOrigin / resolution);
                float distanceToCenter = length(cellCenter - pixel);
                float softness = max(cellSize * 0.5 * _DotSoftness, 1e-4);
                float dotMask = 1.0 - smoothstep(radius - softness, radius + softness, distanceToCenter);

                return lerp(_BackgroundColor, texel, dotMask);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
