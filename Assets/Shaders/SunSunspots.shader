Shader "Custom/SunSunspots"
{
    Properties
    {
        _UmbraColor ("Umbra Darken Color", Color) = (0.22, 0.18, 0.14, 1)
        _PenumbraColor ("Penumbra Darken Color", Color) = (0.50, 0.42, 0.32, 1)
        _SpotStrength ("Spot Strength", Range(0.0, 1.0)) = 1.0
        _PenumbraNoise ("Penumbra Noise", Range(0.0, 1.0)) = 0.55
        _PenumbraGrainSpeed ("Penumbra Grain Speed", Range(0.0, 0.5)) = 0.04
        _BreathingAmount ("Breathing Amount", Range(0.0, 0.05)) = 0.015
        _BreathingSpeed ("Breathing Speed", Range(0.0, 1.0)) = 0.12

        _Spot0Dir ("Spot 0 Direction", Vector) = (0.7, 0.3, 0.64, 0)
        _Spot0Radii ("Spot 0 Umbra, Penumbra, Aspect", Vector) = (0.045, 0.095, 1.35, 0)
        _Spot1Dir ("Spot 1 Direction", Vector) = (-0.52, 0.58, 0.62, 0)
        _Spot1Radii ("Spot 1 Umbra, Penumbra, Aspect", Vector) = (0.032, 0.072, 1.15, 0)
        _Spot2Dir ("Spot 2 Direction", Vector) = (0.18, -0.74, 0.64, 0)
        _Spot2Radii ("Spot 2 Umbra, Penumbra, Aspect", Vector) = (0.014, 0.028, 1.0, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+5"
            "RenderType" = "Transparent"
        }
        LOD 100

        Blend DstColor SrcColor
        ZWrite Off
        Cull Back

        Pass
        {
            Name "SunSunspotsForward"
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
                float3 normalOS : TEXCOORD0;
            };

            half4 _UmbraColor;
            half4 _PenumbraColor;
            half _SpotStrength;
            half _PenumbraNoise;
            half _PenumbraGrainSpeed;
            half _BreathingAmount;
            half _BreathingSpeed;

            float4 _Spot0Dir;
            float4 _Spot0Radii;
            float4 _Spot1Dir;
            float4 _Spot1Radii;
            float4 _Spot2Dir;
            float4 _Spot2Radii;

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

            void SpotTangentBasis(float3 spotDir, out float3 tangentX, out float3 tangentY)
            {
                float3 refAxis = abs(spotDir.y) < 0.9 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
                tangentX = normalize(cross(refAxis, spotDir));
                tangentY = cross(spotDir, tangentX);
            }

            float2 SpotTangentUV(float3 normalOS, float3 tangentX, float3 tangentY)
            {
                return float2(dot(normalOS, tangentX), dot(normalOS, tangentY));
            }

            float EvaluateSpotMask(
                float3 normalOS,
                float3 spotDir,
                float umbraRadius,
                float penumbraRadius,
                float aspect,
                float grainTime,
                out float umbraMask,
                out float penumbraOnlyMask)
            {
                umbraMask = 0.0;
                penumbraOnlyMask = 0.0;

                spotDir = normalize(spotDir);

                float3 tangentX;
                float3 tangentY;
                SpotTangentBasis(spotDir, tangentX, tangentY);

                float breath = 1.0 + sin(grainTime * _BreathingSpeed * 6.28318 + dot(spotDir, float3(1.7, 2.3, 0.9))) * _BreathingAmount;
                float umbraR = umbraRadius * breath;
                float penumbraR = penumbraRadius * breath;

                float2 uv = SpotTangentUV(normalOS, tangentX, tangentY);
                uv.x /= max(aspect, 0.25);

                float dist = length(uv);
                float umbraEdge = umbraR * 0.82;
                float umbraSoft = umbraR * 0.22;
                umbraMask = 1.0 - smoothstep(umbraEdge - umbraSoft, umbraEdge + umbraSoft * 0.35, dist);

                float penumbraInner = umbraR * 0.95;
                float penumbraOuter = penumbraR;
                float penumbraFalloff = smoothstep(penumbraOuter, penumbraInner, dist);

                float2 grainUv = uv * 28.0 + tangentX.xy * 1.3 + tangentY.xy * 0.9;
                float grain = fbm3(float3(grainUv, grainTime * _PenumbraGrainSpeed * 40.0));
                float grainMod = lerp(1.0, 0.55 + grain * 0.9, _PenumbraNoise);
                penumbraFalloff *= grainMod;

                float fullPenumbra = saturate(penumbraFalloff);
                penumbraOnlyMask = saturate(fullPenumbra - umbraMask);
                return max(umbraMask, penumbraOnlyMask);
            }

            float3 CombineSpotDarkening(float3 normalOS, float grainTime)
            {
                float3 darken = float3(1.0, 1.0, 1.0);

                float umbra0, pen0, umbra1, pen1, umbra2, pen2;
                EvaluateSpotMask(normalOS, _Spot0Dir.xyz, _Spot0Radii.x, _Spot0Radii.y, _Spot0Radii.z, grainTime, umbra0, pen0);
                EvaluateSpotMask(normalOS, _Spot1Dir.xyz, _Spot1Radii.x, _Spot1Radii.y, _Spot1Radii.z, grainTime, umbra1, pen1);
                EvaluateSpotMask(normalOS, _Spot2Dir.xyz, _Spot2Radii.x, _Spot2Radii.y, _Spot2Radii.z, grainTime, umbra2, pen2);

                float umbraMask = max(umbra0, max(umbra1, umbra2));
                float penumbraMask = max(pen0, max(pen1, pen2));

                float3 umbraDark = _UmbraColor.rgb;
                float3 penumbraDark = _PenumbraColor.rgb;

                darken = lerp(darken, umbraDark, umbraMask);
                darken = lerp(darken, penumbraDark, penumbraMask);

                return darken;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.normalOS = normalize(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalOS = normalize(input.normalOS);
                float grainTime = _Time.y;

                float3 darken = CombineSpotDarkening(normalOS, grainTime);
                darken = lerp(half3(1.0, 1.0, 1.0), darken, _SpotStrength);
                return half4(darken, 1.0);
            }
            ENDHLSL
        }
    }
}
