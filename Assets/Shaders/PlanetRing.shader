Shader "Custom/PlanetRing"
{
    Properties
    {
        _BaseMap ("Ring Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _EmissionColor ("Emission", Color) = (0, 0, 0, 0)
        _RimBoost ("View Rim Boost", Range(0, 3)) = 1.2
        _SunInfluence ("Sun Influence", Range(0, 1)) = 0.65
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
            Name "PlanetRingForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half _RimBoost;
                half _SunInfluence;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.worldPos = pos.positionWS;
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = tex.a * _BaseColor.a;
                half3 albedo = tex.rgb * _BaseColor.rgb;

                float3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);
                float3 normal = normalize(input.worldNormal);
                float rim = pow(1.0 - saturate(abs(dot(normal, viewDir))), 2.5);
                half rimBoost = 1.0 + rim * _RimBoost;

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float sunSide = saturate(dot(normal, lightDir));
                half sunMask = lerp(1.0h, sunSide, _SunInfluence);

                half3 color = albedo * rimBoost * sunMask + _EmissionColor.rgb;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
