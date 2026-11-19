Shader "CNC/URP/EmissiveFlow"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.08, 0.14, 0.22, 1)
        [HDR] _FlowColor("Flow Emission Color", Color) = (0.0, 1.8, 2.4, 1)
        _FlowMask("Flow Mask", 2D) = "white" {}
        _FlowDir("Flow Direction (XY)", Vector) = (1, 0, 0, 0)
        _FlowSpeed("Flow Speed", Float) = 0.8
        _PulseSpeed("Pulse Speed", Float) = 2.0
        _Metallic("Metallic", Range(0, 1)) = 0.2
        _Smoothness("Smoothness", Range(0, 1)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_FlowMask); SAMPLER(sampler_FlowMask);
            float _CNC_SimTime;

            CBUFFER_START(UnityPerMaterial)
                float4 _FlowMask_ST;
                half4  _BaseColor;
                half4  _FlowColor;
                float4 _FlowDir;
                float  _FlowSpeed;
                float  _PulseSpeed;
                half   _Metallic;
                half   _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _FlowMask);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float t = (_CNC_SimTime > 0.0001) ? _CNC_SimTime : _Time.y;
                float2 uv = IN.uv - _FlowDir.xy * (_FlowSpeed * t);
                half mask = SAMPLE_TEXTURE2D(_FlowMask, sampler_FlowMask, uv).a;
                half pulse = 0.78h + 0.22h * sin(t * _PulseSpeed);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = _BaseColor.rgb;
                surfaceData.alpha = 1.0h;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = _FlowColor.rgb * mask * pulse;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
