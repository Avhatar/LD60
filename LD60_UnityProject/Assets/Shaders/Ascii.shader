Shader "LD60/Stylize/Ascii"
{
    Properties
    {
        [MainTexture] _MainTex ("Source", 2D) = "white" {}
        _CellSize ("Cell Size (px)", Range(2, 64)) = 8
        _Tone ("Tone Divisor", Range(0.01, 2)) = 1
        _Fade ("Fade", Range(0, 1)) = 1
        _BackgroundColor ("Background", Color) = (0, 0, 0, 1)
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
            Name "Ascii"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _BackgroundColor;
            float4 _Resolution;
            float _CellSize;
            float _Tone;
            float _Fade;
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

            float GlyphMask(float glyphCode, float2 glyphUV)
            {
                float2 cell = floor(glyphUV * float2(4.0, -4.0) + 2.5);
                if (clamp(cell.x, 0.0, 4.0) != cell.x || clamp(cell.y, 0.0, 4.0) != cell.y)
                    return 0.0;

                int bitIndex = (int)(cell.x + 5.0 * cell.y);
                int bits = (int)glyphCode;

                return ((bits >> bitIndex) & 1) == 1 ? 1.0 : 0.0;
            }

            float SelectGlyphCode(float tone)
            {
                float glyphCode = 65536.0;
                if (tone > 0.2) glyphCode = 65600.0;
                if (tone > 0.3) glyphCode = 332772.0;
                if (tone > 0.4) glyphCode = 15255086.0;
                if (tone > 0.5) glyphCode = 23385164.0;
                if (tone > 0.6) glyphCode = 15252014.0;
                if (tone > 0.7) glyphCode = 13199452.0;
                if (tone > 0.8) glyphCode = 11512810.0;

                return glyphCode;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 resolution = ResolveResolution();
                float cellSize = max(2.0, _CellSize);
                float2 pixel = input.uv * resolution;
                float2 cellOrigin = floor(pixel / cellSize) * cellSize;

                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, cellOrigin / resolution);

                #ifdef UNITY_COLORSPACE_GAMMA
                    float3 toneColor = texel.rgb;
                #else
                    float3 toneColor = LinearToSRGB(texel.rgb);
                #endif

                float tone = (toneColor.r + toneColor.b) / max(_Tone, 0.01);
                float glyphCode = SelectGlyphCode(tone);

                float2 glyphUV = fmod(pixel / (cellSize * 0.5), 2.0) - 1.0;
                float mask = GlyphMask(glyphCode, glyphUV);

                float4 glyph = lerp(_BackgroundColor, texel, mask);

                return lerp(texel, glyph, _Fade);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
