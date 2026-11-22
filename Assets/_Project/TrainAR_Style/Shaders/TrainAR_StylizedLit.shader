Shader "TrainAR/URP/StylizedLit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.93, 0.92, 0.89, 1.0)
        _ShadowColor("Shadow Tint", Color) = (0.72, 0.76, 0.83, 1.0)
        _CreviceTint("Crevice / Grime Tint", Color) = (0.58, 0.54, 0.50, 1.0)
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        _BrushMap("Painterly Brush / Grime Map", 2D) = "gray" {}
        _BrushScale("Brush World Scale", Float) = 0.85
        _BrushStrength("Brush Texture Strength", Range(0, 0.45)) = 0.12
        _GrimeInCrevice("Crevice Grime Amount", Range(0, 0.8)) = 0.28

        [Header(Stylized Cel Shading)]
        _ShadowThreshold("Shadow Threshold", Range(-0.5, 0.6)) = 0.08
        _HighlightThreshold("Highlight Threshold", Range(0.2, 0.95)) = 0.62
        _RampSmoothness("Cel Ramp Softness", Range(0.01, 0.35)) = 0.14
        _ShadowReceiveStrength("Cast Shadow Strength", Range(0, 1)) = 0.55
        _AmbientLift("Studio Ambient Lift", Range(0, 0.5)) = 0.16

        [Header(Specular and Rim)]
        _SpecColor("Specular Color", Color) = (1.0, 0.99, 0.96, 1.0)
        _SpecSize("Specular Patch Size", Range(0.01, 0.35)) = 0.08
        _SpecIntensity("Specular Intensity", Range(0, 1)) = 0.16
        _RimColor("Rim Highlight Color", Color) = (0.98, 0.99, 1.0, 1.0)
        _RimPower("Rim Power", Range(1.0, 8.0)) = 3.4
        _RimIntensity("Rim Intensity", Range(0, 0.8)) = 0.22

        [Header(Cutaway and Animation)]
        _UVScrollSpeed("UV Scroll Speed (XY)", Vector) = (0, 0, 0, 0)
        [Toggle] _UseTriplanarBase("Use Triplanar Base UV", Float) = 0
        _TriplanarScale("Triplanar Scale", Float) = 1.0

        [Header(Teaching Focus Highlight)]
        _HighlightAmount("Focus Highlight Amount", Range(0, 1)) = 0.0
        _HighlightColor("Focus Highlight Color", Color) = (0.20, 0.72, 0.98, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        // ------------------------------------------------------------------
        // 1. Forward Lit Stylized TrainAR Pass
        // ------------------------------------------------------------------
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float3 normalOS   : TEXCOORD4;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BrushMap);
            SAMPLER(sampler_BrushMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half4  _ShadowColor;
                half4  _CreviceTint;
                float4 _BaseMap_ST;
                float4 _BrushMap_ST;
                float  _BrushScale;
                half   _BrushStrength;
                half   _GrimeInCrevice;
                half   _ShadowThreshold;
                half   _HighlightThreshold;
                half   _RampSmoothness;
                half   _ShadowReceiveStrength;
                half   _AmbientLift;
                half4  _SpecColor;
                half   _SpecSize;
                half   _SpecIntensity;
                half4  _RimColor;
                half   _RimPower;
                half   _RimIntensity;
                float4 _UVScrollSpeed;
                float  _UseTriplanarBase;
                float  _TriplanarScale;
                half   _HighlightAmount;
                half4  _HighlightColor;
            CBUFFER_END

            half4 SampleTriplanar(TEXTURE2D_PARAM(tex, samp), float3 pos, float3 nrm, float scale)
            {
                float3 w = pow(abs(nrm) + 1e-4, 4.0);
                w /= (w.x + w.y + w.z);
                half4 cx = SAMPLE_TEXTURE2D(tex, samp, pos.yz * scale);
                half4 cy = SAMPLE_TEXTURE2D(tex, samp, pos.xz * scale);
                half4 cz = SAMPLE_TEXTURE2D(tex, samp, pos.xy * scale);
                return cx * w.x + cy * w.y + cz * w.z;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = nrmInputs.normalWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalOS = IN.normalOS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap) + _UVScrollSpeed.xy * _Time.y;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 normalOS = normalize(IN.normalOS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                // Base Albedo (UV or Object-space Triplanar for crisp cutaway hatching)
                half4 texSample = (_UseTriplanarBase > 0.5)
                    ? SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), IN.positionOS, normalOS, _TriplanarScale)
                    : SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                half3 albedo = texSample.rgb * _BaseColor.rgb;

                // Subtle hand-painted watercolor / brush grain in object space (no swimming during rotation)
                half brushVal = SampleTriplanar(TEXTURE2D_ARGS(_BrushMap, sampler_BrushMap), IN.positionOS, normalOS, _BrushScale).r;
                half brushSigned = (brushVal - 0.5h) * 2.0h;

                // Baked vertex AO (channel R; default meshes without colors have white = 1.0)
                half vertAO = saturate(IN.color.r);

                // Main directional light + soft shadow
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lightDir = normalize(mainLight.direction);
                half NdotL = dot(normalWS, lightDir);

                // Soft stepped TrainAR cel ramp
                half diffuseBand = smoothstep(_ShadowThreshold - _RampSmoothness, _ShadowThreshold + _RampSmoothness, NdotL);
                half castShadow = lerp(1.0h, mainLight.shadowAttenuation, _ShadowReceiveStrength);
                half litFactor = diffuseBand * castShadow;

                // Soft sunny upper highlight band
                half topHighlight = smoothstep(_HighlightThreshold - _RampSmoothness, _HighlightThreshold + _RampSmoothness, NdotL) * castShadow;

                // Apply painterly crevice grime concentrated in AO pockets and shadow transitions
                half creviceMask = saturate((1.0h - vertAO) * 1.35h + (1.0h - litFactor) * 0.25h);
                half grimeAmt = saturate(creviceMask * _GrimeInCrevice * (0.65h + 0.55h * (1.0h - brushVal)));
                half3 grainedAlbedo = albedo * (1.0h + brushSigned * _BrushStrength);
                grainedAlbedo = lerp(grainedAlbedo, grainedAlbedo * _CreviceTint.rgb, grimeAmt);

                // Blend between clean cool shadow tint and bright lit tone
                half3 shadowLit = grainedAlbedo * _ShadowColor.rgb;
                half3 brightLit = grainedAlbedo * lerp(1.0h, 1.06h, topHighlight);

                // Hemisphere sky/ground studio bounce (keeps undersides crisp and luminous like TrainAR)
                half skyFactor = normalWS.y * 0.5h + 0.5h;
                half3 studioBounce = lerp(half3(0.88, 0.90, 0.94), half3(1.03, 1.02, 1.00), skyFactor);

                half3 color = lerp(shadowLit, brightLit, litFactor) * lerp(half3(1, 1, 1), studioBounce, 0.35h);
                color += grainedAlbedo * _AmbientLift * vertAO;

                // Additional fill lights (soft cel contribution)
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < pixelLightCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, IN.positionWS);
                    half addNdotL = smoothstep(-0.1h, 0.45h, dot(normalWS, addLight.direction));
                    color += grainedAlbedo * addLight.color * (addNdotL * addLight.distanceAttenuation * 0.22h);
                }
                #endif

                // Stylized soft specular highlight
                float3 halfVec = normalize(lightDir + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfVec));
                half specBand = smoothstep(1.0h - _SpecSize - 0.035h, 1.0h - _SpecSize + 0.035h, NdotH);
                color += _SpecColor.rgb * (specBand * _SpecIntensity * litFactor * vertAO);

                // Clean silhouette separation rim light
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half rimMask = saturate(NdotL * 0.45h + 0.55h) * vertAO;
                color += _RimColor.rgb * (fresnel * _RimIntensity * rimMask);

                // Teaching focus highlight pulse (TrainAR subtle interaction feedback)
                if (_HighlightAmount > 0.001h)
                {
                    half pulse = 0.72h + 0.28h * sin(_Time.y * 4.2h);
                    half focusRim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 1.9h);
                    half hMix = saturate((0.18h + 0.62h * focusRim) * _HighlightAmount * pulse);
                    color = lerp(color, _HighlightColor.rgb * 1.15h, hMix);
                }

                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // 2. Shadow Caster Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // 3. DepthOnly Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // 4. DepthNormals Pass (Required for crisp URP Ink Outlines & SSAO)
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = half3(TransformObjectToWorldNormal(input.normalOS));
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOct = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packed = PackFloat2To888(remappedOct);
                    return half4(packed, 0.0h);
                #else
                    return half4(NormalizeNormalPerPixel(normalWS), 0.0h);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
