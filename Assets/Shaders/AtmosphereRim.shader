Shader "Custom/AtmosphereRim"
{
    Properties
    {
        _AtmosphereColor ("Atmosphere Color", Color) = (0.1, 0.6, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _SunInfluence ("Sun Influence", Range(0.0, 1.0)) = 0.8
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "AtmospherePass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;
            };

            half4 _AtmosphereColor;
            half _RimPower;
            half _SunInfluence;

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
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);
                float3 normal = normalize(input.worldNormal);

                // Fresnel rim calculation
                float rim = 1.0 - saturate(dot(normal, viewDir));
                float rimPower = pow(rim, _RimPower);

                // Get main light in URP
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);

                // Sun influence (brighter on sun side, soft falloff to shadow side)
                float lightIntensity = saturate(dot(normal, lightDir));
                float sunMask = lerp(1.0, lightIntensity, _SunInfluence);

                half4 finalColor = _AtmosphereColor * rimPower * sunMask;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
