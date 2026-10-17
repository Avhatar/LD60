Shader "Hidden/LD60/WideScreenBars"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _WideScreenBarsParams;

        #define BarsSize       _WideScreenBarsParams.x
        #define BarsSmoothness _WideScreenBarsParams.y

        half4 FragBars(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float distanceFromCenter = abs(0.5 - input.texcoord.y) * 1.41421356;
            float mask = smoothstep(BarsSize - BarsSmoothness, BarsSize, distanceFromCenter);

            return half4(0.0, 0.0, 0.0, mask);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, Zero One

        Pass
        {
            Name "Wide Screen Bars"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBars
            ENDHLSL
        }
    }
}
