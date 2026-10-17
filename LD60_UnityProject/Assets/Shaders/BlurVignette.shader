Shader "Hidden/LD60/BlurVignette"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_BlurVignetteBlurTexture);

        float4 _BlurVignetteTexelSize;
        float4 _BlurVignetteShape;
        float4 _BlurVignetteParams;

        #define SourceTexel     _BlurVignetteTexelSize.xy
        #define ShapeCenter     _BlurVignetteShape.xy
        #define ShapeRadius     _BlurVignetteShape.z
        #define ShapeSmoothness _BlurVignetteShape.w
        #define ShapeRoundness  _BlurVignetteParams.x
        #define BlurIntensity   _BlurVignetteParams.y
        #define SampleSpread    _BlurVignetteParams.z

        half4 SampleBlitSource(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
        }

        half4 FragDownsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = input.texcoord;
            float2 offset = SourceTexel * SampleSpread;

            half4 sum = SampleBlitSource(uv) * 4.0;
            sum += SampleBlitSource(uv + float2(-offset.x, -offset.y));
            sum += SampleBlitSource(uv + float2( offset.x,  offset.y));
            sum += SampleBlitSource(uv + float2( offset.x, -offset.y));
            sum += SampleBlitSource(uv + float2(-offset.x,  offset.y));

            return sum * 0.125;
        }

        half4 FragUpsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = input.texcoord;
            float2 offset = SourceTexel * SampleSpread;

            half4 sum = SampleBlitSource(uv + float2(-offset.x * 2.0, 0.0));
            sum += SampleBlitSource(uv + float2(-offset.x,  offset.y)) * 2.0;
            sum += SampleBlitSource(uv + float2( 0.0, offset.y * 2.0));
            sum += SampleBlitSource(uv + float2( offset.x,  offset.y)) * 2.0;
            sum += SampleBlitSource(uv + float2( offset.x * 2.0, 0.0));
            sum += SampleBlitSource(uv + float2( offset.x, -offset.y)) * 2.0;
            sum += SampleBlitSource(uv + float2( 0.0, -offset.y * 2.0));
            sum += SampleBlitSource(uv + float2(-offset.x, -offset.y)) * 2.0;

            return sum * 0.0833333333;
        }

        half4 FragComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uvSource = input.texcoord;
            float2 uvScreen = (uvSource - _BlitScaleBias.zw) / max(_BlitScaleBias.xy, 0.00001);

            float2 delta = uvScreen - ShapeCenter;
            delta.x *= lerp(1.0, _ScreenParams.x / _ScreenParams.y, ShapeRoundness);

            float gradient = smoothstep(ShapeRadius, ShapeRadius + ShapeSmoothness, length(delta));

            half4 sharp = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvSource);
            half3 blurred = SAMPLE_TEXTURE2D_X(_BlurVignetteBlurTexture, sampler_LinearClamp, uvScreen).rgb;

            return half4(lerp(sharp.rgb, blurred, saturate(gradient * BlurIntensity)), sharp.a);
        }

        half4 FragCopy(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            return SampleBlitSource(input.texcoord);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Blur Vignette Downsample"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragDownsample
            ENDHLSL
        }

        Pass
        {
            Name "Blur Vignette Upsample"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragUpsample
            ENDHLSL
        }

        Pass
        {
            Name "Blur Vignette Composite"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragComposite
            ENDHLSL
        }

        Pass
        {
            Name "Blur Vignette Copy"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCopy
            ENDHLSL
        }
    }
}
