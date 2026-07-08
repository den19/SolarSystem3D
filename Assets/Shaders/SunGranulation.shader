Shader "Custom/SunGranulation"
{
    Properties
    {
        [HDR] _CellColor ("Cell Bright Color", Color) = (1.3, 0.9, 0.32, 1.0)
        _LaneColor ("Lane Dark Color", Color) = (0.09, 0.03, 0.008, 1.0)
        _GranuleScale ("Granule Scale", Range(1.0, 48.0)) = 2.0
        _LaneWidth ("Lane Width", Range(0.02, 0.5)) = 0.2
        _DriftSpeed ("Drift Speed", Range(0.0, 0.25)) = 0.045
        _PulseAmount ("Pulse Amount", Range(0.0, 0.5)) = 0.28
        _PulseSpeed ("Pulse Speed", Range(0.2, 4.0)) = 1.1
        _DetailBlend ("Detail Blend", Range(0.0, 1.0)) = 1.0
        _OverlayAlpha ("Overlay Alpha", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+12"
            "RenderType" = "Transparent"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "SunGranulationForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            half4 _CellColor;
            half4 _LaneColor;
            half _GranuleScale;
            half _LaneWidth;
            half _DriftSpeed;
            half _PulseAmount;
            half _PulseSpeed;
            half _DetailBlend;
            half _OverlayAlpha;

            float hash13(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float3 hash33(float3 p)
            {
                p = float3(
                    dot(p, float3(127.1, 311.7, 74.7)),
                    dot(p, float3(269.5, 183.3, 246.1)),
                    dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453);
            }

            // Returns (F1, F2) squared-then-sqrt nearest feature distances.
            float2 worleyF1F2(float3 p)
            {
                float3 cell = floor(p);
                float3 fracP = frac(p);

                float f1 = 8.0;
                float f2 = 8.0;

                [unroll]
                for (int z = -1; z <= 1; z++)
                {
                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        [unroll]
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 neighbor = float3(x, y, z);
                            float3 point = hash33(cell + neighbor);
                            float3 diff = neighbor + point - fracP;
                            float dist = dot(diff, diff);

                            if (dist < f1)
                            {
                                f2 = f1;
                                f1 = dist;
                            }
                            else if (dist < f2)
                            {
                                f2 = dist;
                            }
                        }
                    }
                }

                return float2(sqrt(f1), sqrt(f2));
            }

            float valueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash13(i + float3(0.0, 0.0, 0.0));
                float n100 = hash13(i + float3(1.0, 0.0, 0.0));
                float n010 = hash13(i + float3(0.0, 1.0, 0.0));
                float n110 = hash13(i + float3(1.0, 1.0, 0.0));
                float n001 = hash13(i + float3(0.0, 0.0, 1.0));
                float n101 = hash13(i + float3(1.0, 0.0, 1.0));
                float n011 = hash13(i + float3(0.0, 1.0, 1.0));
                float n111 = hash13(i + float3(1.0, 1.0, 1.0));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_DetailBlend <= 0.001)
                    return half4(0.0, 0.0, 0.0, 0.0);

                float3 normal = normalize(input.worldNormal);
                float time = _Time.y;

                float3 drift = float3(
                    sin(time * _DriftSpeed * 1.7),
                    cos(time * _DriftSpeed * 1.3),
                    sin(time * _DriftSpeed * 0.9 + 1.4)) * (time * _DriftSpeed * 2.5);

                float granuleScale = _GranuleScale;

                float3 samplePos = normal * granuleScale + drift;
                float2 ff = worleyF1F2(samplePos);
                float f1 = ff.x;
                float edge = ff.y - ff.x;

                // Dark intergranular lanes where two cells meet (small edge distance).
                float laneMask = 1.0 - smoothstep(_LaneWidth * 0.2, _LaneWidth, edge);
                float cellMask = 1.0 - laneMask;

                // Pebble/dome shading: bright at the cell center (small F1), darker toward rim.
                float dome = 1.0 - saturate(f1 * 1.35);
                dome = pow(dome, 0.7);

                float cellHash = hash13(floor(samplePos) + drift * 0.15);
                float pulse = 1.0 + sin(time * _PulseSpeed + cellHash * 6.28318) * _PulseAmount;
                // Per-cell brightness variation so granules aren't uniform.
                float cellBright = 0.8 + hash13(floor(samplePos + 31.7)) * 0.4;
                float micro = valueNoise3(samplePos * 3.2 + time * 0.35) * 0.1;

                // Normalized granule body brightness in [0,1]: dark lanes, bright domes.
                float body = cellMask * dome * pulse * cellBright;
                body = saturate(body + micro);

                // Mild contrast around the dark lane color keeps peaks close to the
                // bloom threshold (~1.0) so granules stay crisp instead of blooming out.
                float contrast = lerp(1.0, 1.25, _DetailBlend);
                body = saturate((body - 0.5) * contrast + 0.5);

                half3 color = lerp(_LaneColor.rgb, _CellColor.rgb, body);

                // Ramp opacity quickly so the granulation reads clearly once it starts
                // fading in, instead of staying near-transparent through most of the zoom.
                float alphaRamp = smoothstep(0.0, 0.45, _DetailBlend);
                half alpha = saturate(_OverlayAlpha * alphaRamp * (0.9 + cellMask * 0.1));
                return half4(max(color, 0.0), alpha);
            }
            ENDHLSL
        }
    }
}
