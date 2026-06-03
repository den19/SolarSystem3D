Shader "Custom/SaturnGreatWhiteSpot"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (0.97, 0.94, 0.88, 0.72)
        _EdgeColor ("Edge Color", Color) = (0.82, 0.78, 0.72, 0.28)
        _SpotStrength ("Spot Strength", Range(0.0, 1.0)) = 1.0
        _Dissipation ("Dissipation", Range(0.0, 1.0)) = 0.0
        _Turbulence ("Turbulence", Range(0.0, 1.0)) = 0.55
        _GrainSpeed ("Grain Speed", Range(0.0, 0.5)) = 0.035
        _BreathingAmount ("Breathing Amount", Range(0.0, 0.08)) = 0.022
        _BreathingSpeed ("Breathing Speed", Range(0.0, 1.0)) = 0.09

        _StormCenterLongitude ("Storm Center Longitude", Float) = 0.0
        _StormLongitudeHalfExtent ("Storm Longitude Half Extent", Range(0.0, 3.14159)) = 0.15
        _StormLatitudeHalfExtent ("Storm Latitude Half Extent", Range(0.0, 0.5)) = 0.10
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

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "SaturnGreatWhiteSpotForward"
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

            half4 _CoreColor;
            half4 _EdgeColor;
            half _SpotStrength;
            half _Dissipation;
            half _Turbulence;
            half _GrainSpeed;
            half _BreathingAmount;
            half _BreathingSpeed;

            float _StormCenterLongitude;
            float _StormLongitudeHalfExtent;
            float _StormLatitudeHalfExtent;

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

            float WrapLongitudeDelta(float lon, float centerLon)
            {
                float delta = lon - centerLon;
                return atan2(sin(delta), cos(delta));
            }

            float EvaluateBandMask(float dist, float halfExtent, float innerFraction, float softness)
            {
                if (halfExtent <= 0.0001)
                    return 0.0;

                float innerEdge = halfExtent * innerFraction;
                float outerEdge = halfExtent;
                float soft = max(halfExtent * softness, 0.001);
                return 1.0 - smoothstep(outerEdge - soft, outerEdge + soft * 0.35, dist);
            }

            void EvaluateStormMasks(
                float3 normalOS,
                float grainTime,
                out float coreMask,
                out float penumbraMask)
            {
                coreMask = 0.0;
                penumbraMask = 0.0;

                float breath = 1.0 + sin(
                    grainTime * _BreathingSpeed * 6.28318 + _StormCenterLongitude * 2.17)
                    * _BreathingAmount;

                float lonHalf = _StormLongitudeHalfExtent * breath;
                float latHalf = _StormLatitudeHalfExtent * breath;

                float lon = atan2(normalOS.z, normalOS.x);
                float lat = asin(clamp(normalOS.y, -1.0, 1.0));

                float lonDist = abs(WrapLongitudeDelta(lon, _StormCenterLongitude));
                float latDist = abs(lat);

                float lonPenumbra = EvaluateBandMask(lonDist, lonHalf, 0.55, 0.28);
                float latPenumbra = EvaluateBandMask(latDist, latHalf, 0.5, 0.32);
                float penumbra = lonPenumbra * latPenumbra;

                float coreLonHalf = lonHalf * 0.48;
                float coreLatHalf = latHalf * 0.58;
                float lonCore = EvaluateBandMask(lonDist, coreLonHalf, 0.62, 0.22);
                float latCore = EvaluateBandMask(latDist, coreLatHalf, 0.58, 0.24);
                float core = lonCore * latCore;

                float3 grainCoord = float3(
                    lon * 9.5 + lat * 14.0,
                    lat * 22.0 + lonDist * 6.0,
                    grainTime * _GrainSpeed * 35.0);
                float grain = fbm3(grainCoord);
                float grainMod = lerp(1.0, 0.52 + grain * 0.92, _Turbulence);
                penumbra *= grainMod;

                float3 breakupCoord = float3(
                    lon * 16.0 + _StormCenterLongitude * 3.1,
                    lat * 28.0,
                    grainTime * _GrainSpeed * 52.0 + _StormCenterLongitude);
                float breakup = fbm3(breakupCoord);
                float breakupMod = lerp(1.0, saturate(0.25 + breakup * 1.35), _Dissipation);
                penumbra *= breakupMod;
                core *= lerp(1.0, saturate(0.45 + breakup * 0.85), _Dissipation * 0.85);

                coreMask = saturate(core);
                penumbraMask = saturate(penumbra - coreMask);
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

                float coreMask;
                float penumbraMask;
                EvaluateStormMasks(normalOS, grainTime, coreMask, penumbraMask);

                float totalMask = saturate(coreMask + penumbraMask);
                totalMask *= _SpotStrength;

                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, coreMask);
                half alpha = lerp(_EdgeColor.a, _CoreColor.a, coreMask);
                alpha *= totalMask;

                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
