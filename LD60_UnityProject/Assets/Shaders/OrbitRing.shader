Shader "LD60/Space/OrbitRing"
{
    Properties
    {
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)

        [Header(Dots)]
        [Space(4)]
        _DotPitch ("Dot Pitch (world units along orbit)", Range(0.01, 2)) = 0.15
        _DotSize ("Dot Size (world units)", Range(0.005, 0.5)) = 0.05

        [Header(Pixelize)]
        [Space(4)]
        [Toggle(_PIXELIZE_ON)] _Pixelize ("Snap To Pixel Grid", Float) = 1
        _PixelSize ("Pixel Size (world units)", Range(0.001, 0.5)) = 0.05

        [Header(Ring)]
        [Space(4)]
        _OrbitRadius ("Orbit Radius (0 = from geometry)", Float) = 0

        [Header(Output)]
        [Space(4)]
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.004
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
            Name "OrbitRing"
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
            float4 _BaseColor;
            float _DotPitch;
            float _DotSize;
            float _Pixelize;
            float _PixelSize;
            float _OrbitRadius;
            float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 positionWS : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(world);
                output.positionWS = world.xy;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 center = float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13);
                float2 radial = input.positionWS - center;
                float2 angular = radial;
                float alongSoftness;

                #ifdef _PIXELIZE_ON
                    float pixelSize = max(_PixelSize, 1e-4);
                    angular = (floor(input.positionWS / pixelSize) + 0.5) * pixelSize - center;
                    alongSoftness = 1e-4;
                #else
                    alongSoftness = max(fwidth(input.positionWS.x), fwidth(input.positionWS.y)) * 0.5 + 1e-6;
                #endif

                float radius = length(radial);
                float ringRadius = _OrbitRadius > 0.0 ? _OrbitRadius : radius;
                float circumference = TWO_PI * ringRadius;
                float count = max(round(circumference / max(_DotPitch, 1e-3)), 1.0);
                float spacing = circumference / count;

                float phase = atan2(angular.y, angular.x) * count / TWO_PI;
                float along = abs(frac(phase + 0.5) - 0.5) * spacing;
                float across = abs(radius - ringRadius);

                float acrossSoftness = max(fwidth(radius) * 0.5, 1e-6);
                float dotRadius = _DotSize * 0.5;

                float mask = (1.0 - smoothstep(dotRadius - alongSoftness, dotRadius + alongSoftness, along)) *
                             (1.0 - smoothstep(dotRadius - acrossSoftness, dotRadius + acrossSoftness, across));

                half4 color = _BaseColor;
                color.a *= mask;

                clip(color.a - _Cutoff);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
