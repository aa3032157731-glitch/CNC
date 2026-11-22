Shader "TrainAR/URP/StudioFloor"
{
    Properties
    {
        _FloorColor("Studio Backdrop Color", Color) = (0.985, 0.988, 0.992, 1.0)
        _ShadowColor("Cast Shadow Color", Color) = (0.78, 0.80, 0.84, 1.0)
        _ShadowStrength("Shadow Opacity", Range(0, 1)) = 0.72
        _FadeStart("Radial Fade Start", Float) = 6.5
        _FadeEnd("Radial Fade End", Float) = 14.0
        _GridAlpha("Subtle Technical Grid Alpha", Range(0, 0.15)) = 0.0
        _GridScale("Grid Spacing", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _FloorColor;
                half4 _ShadowColor;
                half  _ShadowStrength;
                float _FadeStart;
                float _FadeEnd;
                half  _GridAlpha;
                float _GridScale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float dist = length(IN.positionWS.xz);
                float radialMask = 1.0 - smoothstep(_FadeStart, _FadeEnd, dist);

                half shadowMask = (1.0h - mainLight.shadowAttenuation) * _ShadowStrength * radialMask;
                half3 col = lerp(_FloorColor.rgb, _ShadowColor.rgb, shadowMask);

                if (_GridAlpha > 0.001h)
                {
                    float2 g = abs(frac(IN.positionWS.xz / max(_GridScale, 0.05) - 0.5) - 0.5) / fwidth(IN.positionWS.xz / max(_GridScale, 0.05));
                    float lineMask = 1.0 - min(min(g.x, g.y), 1.0);
                    col = lerp(col, _ShadowColor.rgb, lineMask * _GridAlpha * radialMask);
                }

                return half4(col, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = half3(TransformObjectToWorldNormal(IN.normalOS));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOct = saturate(octNormalWS * 0.5 + 0.5);
                    return half4(PackFloat2To888(remappedOct), 0.0h);
                #else
                    return half4(NormalizeNormalPerPixel(normalWS), 0.0h);
                #endif
            }
            ENDHLSL
        }
    }
}
