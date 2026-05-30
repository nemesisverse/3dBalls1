Shader "Custom/Indicator"
{
    Properties
    {
        [Header(Base)]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Glow)]
        _GlowColor("Glow Color", Color) = (1, 1, 1, 1)
        _GlowIntensity("Intensity", Range(0, 5)) = 2.0
        _RimPower("Rim Power", Range(0.5, 8)) = 3.0
        _GlowSize("Shell Size", Range(0, 0.15)) = 0.04

        [Header(Pulse)]
        _PulseSpeed("Speed", Range(0, 10)) = 2.0
        _PulseMin("Min Brightness", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        // ── Pass 1 ─ Opaque base + fresnel rim highlight ───────────────────────
        Pass
        {
            Name "BASE"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _GlowColor;
                float  _GlowIntensity;
                float  _RimPower;
                float  _GlowSize;
                float  _PulseSpeed;
                float  _PulseMin;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Fresnel rim — bright at silhouette edges
                float3 N   = normalize(IN.normalWS);
                float3 V   = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  rim = pow(1.0 - saturate(dot(N, V)), _RimPower);

                // Pulse — sine wave between _PulseMin and 1.0
                float pulse = lerp(_PulseMin, 1.0, sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);

                half3 finalRGB = texColor.rgb + _GlowColor.rgb * _GlowIntensity * rim * pulse;
                return half4(finalRGB, texColor.a);
            }
            ENDHLSL
        }

        // ── Pass 2 ─ Additive glow shell (extends beyond silhouette) ──────────
        Pass
        {
            Name "GLOW_SHELL"
            Blend  One One      // Additive — adds light on top of everything behind
            ZWrite Off
            Cull   Front        // Back-faces of expanded shell form the halo

            HLSLPROGRAM
            #pragma vertex   vertGlow
            #pragma fragment fragGlow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            // Identical CBUFFER layout across all passes — required for SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _GlowColor;
                float  _GlowIntensity;
                float  _RimPower;
                float  _GlowSize;
                float  _PulseSpeed;
                float  _PulseMin;
            CBUFFER_END

            Varyings vertGlow(Attributes IN)
            {
                Varyings OUT;
                // Expand mesh outward along normals to build glow shell
                float3 expandedOS = IN.positionOS.xyz + IN.normalOS * _GlowSize;
                OUT.positionHCS   = TransformObjectToHClip(expandedOS);
                return OUT;
            }

            half4 fragGlow(Varyings IN) : SV_Target
            {
                float pulse = lerp(_PulseMin, 1.0, sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);
                return half4(_GlowColor.rgb * _GlowIntensity * pulse, 1.0);
            }
            ENDHLSL
        }
    }
}