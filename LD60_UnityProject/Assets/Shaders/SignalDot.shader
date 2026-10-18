Shader "LD60/Signals/SignalDot"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (0.6, 0.95, 1, 0.95)
        _AltColor ("Alt Color", Color) = (0.45, 1, 0.55, 0.95)

        [Header(Shape)]
        [Space(4)]
        _Radius ("Radius (quad fraction)", Range(0.05, 0.5)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.35

        [Header(Brightness)]
        [Space(4)]
        _Emission ("Emission (bloom multiplier)", Float) = 1
        _AltEmission ("Alt Emission", Float) = 1
        _Fade ("Fade", Range(0, 1)) = 1

        [Header(Output)]
        [Space(4)]
        _Cutoff ("Alpha Cutoff", Range(0, 0.1)) = 0.004
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
        }

        Pass
        {
            Name "SignalDot"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _AltColor;
            float _Radius;
            float _EdgeSoftness;
            float _Emission;
            float _AltEmission;
            float _Fade;
            float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float distanceToCenter = length(input.uv - 0.5);
                float radius = max(_Radius, 1e-4);
                float softness = max(_EdgeSoftness * radius, fwidth(distanceToCenter));
                float circle = 1.0 - smoothstep(radius - softness, radius, distanceToCenter);

                float altBlend = saturate(input.color.r);
                float4 tint = lerp(_BaseColor, _AltColor, altBlend);
                float emission = lerp(_Emission, _AltEmission, altBlend);

                float alpha = tint.a * circle * input.color.a * saturate(_Fade);
                clip(alpha - _Cutoff);

                return half4(tint.rgb * emission, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
