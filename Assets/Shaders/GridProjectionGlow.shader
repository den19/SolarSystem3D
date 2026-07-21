Shader "Custom/GridProjectionGlow"
{
    Properties
    {
        _Color ("Color", Color) = (0.24, 0.86, 0.48, 0.55)
        _Softness ("Edge Softness", Range(0.1, 4)) = 1.6
        _CoreBoost ("Core Boost", Range(0.5, 3)) = 1.35
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "GridProjectionGlowForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Softness;
                half _CoreBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float dist = length(centered);
                half falloff = saturate(1.0 - dist);
                falloff = pow(falloff, _Softness);
                half alpha = saturate(falloff * _Color.a * _CoreBoost);
                half3 color = _Color.rgb * (0.55 + falloff * 0.45);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
