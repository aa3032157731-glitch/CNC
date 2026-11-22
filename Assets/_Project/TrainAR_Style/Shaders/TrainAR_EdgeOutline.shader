Shader "Hidden/TrainAR/EdgeOutline"
{
    Properties
    {
        _OutlineColor("Ink Outline Color", Color) = (0.14, 0.16, 0.21, 1.0)
        _OutlineThickness("Outline Thickness (px)", Range(0.5, 4.0)) = 1.55
        _DepthThreshold("Depth Threshold", Range(0.005, 0.5)) = 0.045
        _NormalThreshold("Normal Crease Threshold", Range(0.05, 1.5)) = 0.42
        _DepthNormalThreshold("Grazing Angle Start", Range(0.0, 1.0)) = 0.45
        _DepthNormalScale("Grazing Angle Scale", Range(0.0, 12.0)) = 6.0
        _EdgeSoftness("Edge Anti-Alias Softness", Range(0.01, 0.4)) = 0.12
        _MaxDistance("Max Outline Distance", Float) = 22.0
        _OutlineOpacity("Outline Opacity", Range(0.0, 1.0)) = 0.92
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "TrainAR_EdgeOutlinePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _OutlineColor;
            float _OutlineThickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float _DepthNormalThreshold;
            float _DepthNormalScale;
            float _EdgeSoftness;
            float _MaxDistance;
            half  _OutlineOpacity;

            float SampleLinearEyeDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (_OutlineOpacity <= 0.001h)
                    return sceneColor;

                // Resolution-consistent pixel offset (scaled relative to 1080p baseline so thickness looks identical at any resolution)
                float resScale = max(_ScreenParams.y / 1080.0, 0.5);
                float halfScaleFloor = floor(_OutlineThickness * resScale * 0.5);
                float halfScaleCeil  = ceil(_OutlineThickness * resScale * 0.5);
                float2 texel = _ScreenParams.zw - 1.0; // (1/width, 1/height)

                float2 uvBL = uv - texel * halfScaleFloor;
                float2 uvTR = uv + texel * halfScaleCeil;
                float2 uvBR = uv + float2(texel.x * halfScaleCeil, -texel.y * halfScaleFloor);
                float2 uvTL = uv + float2(-texel.x * halfScaleFloor, texel.y * halfScaleCeil);

                // Roberts-Cross Depth Samples (linear eye depth)
                float dBL = SampleLinearEyeDepth(uvBL);
                float dTR = SampleLinearEyeDepth(uvTR);
                float dBR = SampleLinearEyeDepth(uvBR);
                float dTL = SampleLinearEyeDepth(uvTL);
                float dCenter = SampleLinearEyeDepth(uv);
                float dMin = min(min(dBL, dTR), min(dBR, dTL));

                // Distance fade (keeps distant studio floor boundary spotless white)
                float distFade = 1.0 - smoothstep(_MaxDistance * 0.72, _MaxDistance, dMin);
                if (distFade <= 0.001)
                    return sceneColor;

                // Roberts-Cross World-Space Normal Samples
                float3 nBL = SampleSceneNormals(uvBL);
                float3 nTR = SampleSceneNormals(uvTR);
                float3 nBR = SampleSceneNormals(uvBR);
                float3 nTL = SampleSceneNormals(uvTL);
                float3 nCenter = SampleSceneNormals(uv);

                // Reconstruct view direction for grazing angle compensation (prevents false lines on flat studio floor)
                float3 posWS = ComputeWorldSpacePosition(uv, SampleSceneDepth(uv), UNITY_MATRIX_I_VP);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - posWS);
                float NdotV = saturate(dot(normalize(nCenter + 1e-5), viewDirWS));
                float grazing = saturate((1.0 - NdotV - _DepthNormalThreshold) / max(1.0 - _DepthNormalThreshold, 0.001));
                float depthThreshCompensated = _DepthThreshold * (1.0 + grazing * _DepthNormalScale);

                // Relative depth Roberts-Cross (scale-invariant across close-up and overview cameras!)
                float invDepth = 1.0 / max(dMin, 0.15);
                float depthFiniteDiff0 = (dTR - dBL) * invDepth;
                float depthFiniteDiff1 = (dTL - dBR) * invDepth;
                float edgeDepthRaw = sqrt(depthFiniteDiff0 * depthFiniteDiff0 + depthFiniteDiff1 * depthFiniteDiff1);
                float edgeDepth = smoothstep(depthThreshCompensated, depthThreshCompensated + _EdgeSoftness * 0.5, edgeDepthRaw);

                // World-Space Normal Roberts-Cross (detects crisp mechanical bevels, cutaway edges, and part seams!)
                float3 normalDiff0 = nTR - nBL;
                float3 normalDiff1 = nTL - nBR;
                float edgeNormalRaw = sqrt(dot(normalDiff0, normalDiff0) + dot(normalDiff1, normalDiff1));

                // Suppress false normal noise right at far sky pixels (where normal length is ~0)
                float validNormal = step(0.1, length(nBL) + length(nTR) + length(nBR) + length(nTL));
                float edgeNormal = smoothstep(_NormalThreshold, _NormalThreshold + _EdgeSoftness, edgeNormalRaw) * validNormal;

                // Combine outer silhouette (stronger) + interior crease lines (slightly refined)
                float edge = max(edgeDepth, edgeNormal * 0.88) * distFade * _OutlineOpacity;

                // Subtle ink tinting: blend toward dark cool charcoal ink without crushing highlights elsewhere
                half3 finalRGB = lerp(sceneColor.rgb, _OutlineColor.rgb, saturate(edge));
                return half4(finalRGB, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
