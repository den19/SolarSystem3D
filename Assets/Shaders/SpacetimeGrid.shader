Shader "Custom/SpacetimeGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.45, 0.75, 1.0, 0.85)
        _FillColor ("Fill Color", Color) = (0.08, 0.14, 0.28, 0.18)
        _EmissionColor ("Emission", Color) = (0.2, 0.45, 0.8, 0)
        _GridWorldScale ("Grid World Scale", Range(0.02, 0.5)) = 0.1
        _LineWidth ("Line Width", Range(0.005, 0.1)) = 0.018
        _MaxLineAA ("Max Line AA", Range(0.01, 0.2)) = 0.05
        _RimBoost ("Slope Rim Boost", Range(0, 2)) = 0.65
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
            Name "SpacetimeGridForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GridColor;
                half4 _FillColor;
                half4 _EmissionColor;
                half _GridWorldScale;
                half _LineWidth;
                half _MaxLineAA;
                half _RimBoost;
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

            half GridMask(float2 worldXZ, half worldScale, half lineWidth, half maxLineAA)
            {
                float2 g = worldXZ * worldScale;
                float2 f = abs(frac(g + 0.5) - 0.5);
                float2 fw = clamp(fwidth(g) * 0.35, 1e-4, maxLineAA);
                half lx = 1.0 - smoothstep(lineWidth, lineWidth + fw.x, f.x);
                half lz = 1.0 - smoothstep(lineWidth, lineWidth + fw.y, f.y);
                return saturate(max(lx, lz));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.worldPos = pos.positionWS;
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half grid = GridMask(input.worldPos.xz, _GridWorldScale, _LineWidth, _MaxLineAA);
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);
                float3 normal = normalize(input.worldNormal);
                half slope = 1.0 - saturate(abs(dot(normal, float3(0, 1, 0))));
                half rim = pow(saturate(1.0 - abs(dot(normal, viewDir))), 2.0) * _RimBoost;

                half3 lineColor = _GridColor.rgb + _EmissionColor.rgb * (0.35 + rim);
                half3 fillColor = _FillColor.rgb + _EmissionColor.rgb * slope * 0.15;
                half3 color = lerp(fillColor, lineColor, grid);

                half alpha = lerp(_FillColor.a, _GridColor.a, grid);
                alpha = saturate(alpha + slope * 0.08);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
