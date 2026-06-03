Shader "Custom/SunSurfaceFlicker"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1.0, 0.72, 0.35, 0.12)
        _FlickerIntensity ("Flicker Intensity", Range(0.0, 0.5)) = 0.12
        _SlowSpeed ("Slow Drift Speed", Range(0.0, 2.0)) = 0.35
        _FastSpeed ("Fast Flicker Speed", Range(0.0, 12.0)) = 4.5
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.2
        _RimIntensity ("Rim Intensity", Range(0.0, 1.0)) = 0.22
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
        }
        LOD 100

        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "SunSurfaceFlickerForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            half4 _BaseColor;
            half _FlickerIntensity;
            half _SlowSpeed;
            half _FastSpeed;
            half _RimPower;
            half _RimIntensity;

            float hash33(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float valueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash33(i + float3(0.0, 0.0, 0.0));
                float n100 = hash33(i + float3(1.0, 0.0, 0.0));
                float n010 = hash33(i + float3(0.0, 1.0, 0.0));
                float n110 = hash33(i + float3(1.0, 1.0, 0.0));
                float n001 = hash33(i + float3(0.0, 0.0, 1.0));
                float n101 = hash33(i + float3(1.0, 0.0, 1.0));
                float n011 = hash33(i + float3(0.0, 1.0, 1.0));
                float n111 = hash33(i + float3(1.0, 1.0, 1.0));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            float fbm3(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * valueNoise3(p);
                    p *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);

                float slow = fbm3(normal * 2.4 + _Time.y * _SlowSpeed);
                float fast = valueNoise3(normal * 9.0 + _Time.y * _FastSpeed);
                float flicker = 1.0 + ((slow * 0.65 + fast * 0.35) - 0.5) * _FlickerIntensity * 2.0;

                float rim = pow(1.0 - saturate(dot(normal, viewDir)), _RimPower);
                float rimGlow = rim * _RimIntensity;

                half3 color = _BaseColor.rgb * flicker * (1.0 + rimGlow * 1.8);
                half alpha = _BaseColor.a * flicker + rimGlow * 0.35;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
