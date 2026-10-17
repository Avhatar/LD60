Shader "LD60/Space/Body"
{
    Properties
    {
        [MainTexture] _BaseMap ("Surface", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)

        [Header(Sphere)]
        [Space(4)]
        _DiscRadius ("Disc Radius (UV)", Range(0.05, 0.5)) = 0.335
        _DiscCenter ("Disc Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Spin ("Spin (turns)", Float) = 0
        _Tilt ("Axis Tilt (deg)", Range(-180, 180)) = 0
        _DifferentialSpin ("Differential Spin", Range(-1, 1)) = 0
        _Flatten ("Flatten Projection", Range(0, 1)) = 0.5
        _OuterPassthrough ("Keep Art Outside Disc", Range(0, 1)) = 1

        [Header(Shading)]
        [Space(4)]
        _LimbDarken ("Limb Darken", Range(0, 1)) = 0
        _LimbColor ("Limb Color", Color) = (0, 0, 0, 1)
        _FarSideDim ("Far Side Dim", Range(0, 1)) = 0.12
        _FarSideDrift ("Far Side Drift", Range(0, 0.25)) = 0.04

        [Header(Star Shadow)]
        [Space(4)]
        _SunDir ("Star Direction XY (0 = unlit)", Vector) = (0, 0, 0, 0)
        _ShadowColor ("Shadow Color (alpha = opacity)", Color) = (0, 0, 0, 0.8)
        _TerminatorBow ("Terminator Bow", Range(-1, 1)) = 0.35
        _TerminatorOffset ("Terminator Offset", Range(-1, 1)) = 0
        _TerminatorSoftness ("Terminator Softness", Range(0, 1)) = 0.06

        [Header(Dots)]
        [Space(4)]
        [Toggle(_DOTS_ON)] _Dots ("Dot Stylize", Float) = 0
        _DotPitch ("Dot Pitch (world units)", Range(0.001, 0.25)) = 0.012
        _DotRadius ("Dot Radius", Range(0.05, 1.5)) = 0.85
        _DotSoftness ("Edge Softness", Range(0, 1)) = 0.1
        _BackgroundColor ("Background", Color) = (0, 0, 0, 0)

        [Header(Emission)]
        [Space(4)]
        _Emission ("Emission (bloom multiplier)", Float) = 1

        [Header(Output)]
        [Space(4)]
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.01
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
            Name "SpaceBody"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #pragma shader_feature_local_fragment _DOTS_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _DiscCenter;
            float4 _LimbColor;
            float4 _BackgroundColor;
            float4 _SunDir;
            float4 _ShadowColor;
            float _TerminatorBow;
            float _TerminatorOffset;
            float _TerminatorSoftness;
            float _DiscRadius;
            float _Spin;
            float _Tilt;
            float _DifferentialSpin;
            float _Flatten;
            float _OuterPassthrough;
            float _LimbDarken;
            float _FarSideDim;
            float _FarSideDrift;
            float _Dots;
            float _DotPitch;
            float _DotRadius;
            float _DotSoftness;
            float _Emission;
            float _Cutoff;
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
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float dotMask = 1.0;

                #ifdef _DOTS_ON
                    float2 bodySize = float2(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21));
                    float2 cells = max(bodySize / max(_DotPitch, 1e-4), 1.0);

                    float2 grid = (uv - _DiscCenter.xy) * cells;
                    float2 cellCenter = round(grid);

                    float dotRadius = _DotRadius * 0.5;
                    float softness = max(_DotSoftness * 0.5, length(fwidth(grid)) * 0.5);
                    dotMask = 1.0 - smoothstep(dotRadius - softness, dotRadius + softness, length(grid - cellCenter));
                    uv = cellCenter / cells + _DiscCenter.xy;
                #endif

                float2 p = (uv - _DiscCenter.xy) / max(_DiscRadius, 1e-4);

                float tiltSin, tiltCos;
                sincos(radians(_Tilt), tiltSin, tiltCos);
                float2 axis = float2(tiltCos, tiltSin);
                float2 q = float2(p.x * axis.x + p.y * axis.y, -p.x * axis.y + p.y * axis.x);

                float r2 = dot(q, q);
                float z = sqrt(saturate(1.0 - r2));

                float spin = _Spin * TWO_PI * (1.0 - _DifferentialSpin * q.y * q.y);
                float sinSpin, cosSpin;
                sincos(spin, sinSpin, cosSpin);

                float2 source = float2(q.x * cosSpin + z * sinSpin, q.y);
                float rotatedZ = z * cosSpin - q.x * sinSpin;
                float farSide = saturate(-rotatedZ);

                float cosLatitude = sqrt(saturate(1.0 - q.y * q.y));
                float folded = atan2(source.x, abs(rotatedZ)) * (2.0 / PI);
                source.x = lerp(source.x, folded * cosLatitude, _Flatten);

                source.y += _FarSideDrift * farSide;
                source /= max(1.0, length(source));

                float2 sphereUV = _DiscCenter.xy + float2(source.x * axis.x - source.y * axis.y,
                                                          source.x * axis.y + source.y * axis.x) * _DiscRadius * 0.995;

                half4 sphere = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, sphereUV, 0);
                sphere.rgb = lerp(sphere.rgb, _LimbColor.rgb, (1.0 - z) * _LimbDarken);
                sphere.rgb *= 1.0 - _FarSideDim * farSide;

                float2 fromCenter = uv - _DiscCenter.xy;
                half4 outer = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, uv, 0);
                outer.a *= _OuterPassthrough * step(dot(fromCenter, fromCenter), 0.25);

                half4 color = r2 <= 1.0 ? sphere : outer;
                color *= _BaseColor;

                float sunLength = length(_SunDir.xy);
                float lit = dot(p, _SunDir.xy / max(sunLength, 1e-4)) + _TerminatorBow * z - _TerminatorOffset;
                float edge = max(_TerminatorSoftness, 1e-4);
                float night = (1.0 - smoothstep(-edge, edge, lit)) * step(1e-4, sunLength);
                float3 litRgb = sqrt(max(color.rgb, 0.0));
                float3 shadeRgb = sqrt(max(_ShadowColor.rgb, 0.0));
                float3 shaded = lerp(litRgb, shadeRgb, night * _ShadowColor.a);
                color.rgb = shaded * shaded;

                color.rgb *= _Emission;

                color = lerp(_BackgroundColor, color, dotMask);

                clip(color.a - _Cutoff);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
