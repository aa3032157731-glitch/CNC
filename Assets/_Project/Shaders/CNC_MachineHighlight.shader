Shader "CNC/URP/MachineHighlight"
{
    Properties
    {
        [HDR] _RimColor("Rim Color", Color) = (0.0, 2.0, 2.8, 1.0)
        _RimPower("Rim Power", Range(0.5, 6.0)) = 2.2
        _FillAlpha("Inner Fill Alpha", Range(0, 0.5)) = 0.14
        _ScanSpeed("Scanline Speed", Float) = 1.6
        _ScanDensity("Scanline Density", Float) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "HighlightOverlay"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half  _RimPower;
                half  _FillAlpha;
                float _ScanSpeed;
                float _ScanDensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 n = normalize(IN.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                half fresnel = pow(saturate(1.0h - abs(dot(n, v))), _RimPower);
                half scan = 0.75h + 0.25h * sin(IN.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed * 6.28318);
                half alpha = saturate(_FillAlpha + fresnel * 0.85h) * scan;
                return half4(_RimColor.rgb, alpha * _RimColor.a);
            }
            ENDHLSL
        }
    }
}
