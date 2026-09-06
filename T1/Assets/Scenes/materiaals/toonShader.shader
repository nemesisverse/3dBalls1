Shader "Custom/toonShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Toon Shading)]
        [Space(4)]
        _ShadeColor("Shadow Tint", Color) = (0.70, 0.52, 0.45, 1)
        _ShadeThreshold("Terminator Position", Range(-0.5, 1)) = 0.05
        _ShadeSmoothness("Terminator Softness", Range(0.001, 0.6)) = 0.22
        _AmbientStrength("Ambient / GI Strength", Range(0, 2)) = 1.0

        [Header(Glossy Highlight)]
        [Space(4)]
        _SpecularColor("Highlight Color", Color) = (1, 0.98, 0.90, 1)
        _SpecularSize("Highlight Size", Range(0, 1)) = 0.38
        _SpecularSmoothness("Highlight Softness", Range(0.001, 0.5)) = 0.12
        _SpecularStrength("Highlight Strength", Range(0, 3)) = 1.35

        [Header(Sparkle)]
        [Space(4)]
        _SparkleColor("Sparkle Color", Color) = (1, 1, 0.96, 1)
        _SparkleSize("Sparkle Size", Range(0, 1)) = 0.08
        _SparkleSmoothness("Sparkle Softness", Range(0.001, 0.3)) = 0.04

        [Header(Glassy Edge Volume)]
        [Space(4)]
        _EdgeColor("Edge Darken Tint", Color) = (0.55, 0.38, 0.34, 1)
        _EdgeStrength("Edge Darken Strength", Range(0, 1)) = 0.65
        _EdgePower("Edge Falloff", Range(0.2, 8)) = 2.6

        [Header(Top Sheen Rim)]
        [Space(4)]
        _RimColor("Rim Color", Color) = (1, 0.96, 0.88, 0.5)
        _RimPower("Rim Width", Range(0.1, 12)) = 4
        _RimThreshold("Rim Cutoff", Range(0, 1)) = 0.62
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // -------------------------------------------------------------
        //  Main lit pass (glossy toon)
        // -------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float  fogCoord    : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _ShadeColor;
                float  _ShadeThreshold;
                float  _ShadeSmoothness;
                float  _AmbientStrength;
                half4  _SpecularColor;
                float  _SpecularSize;
                float  _SpecularSmoothness;
                float  _SpecularStrength;
                half4  _SparkleColor;
                float  _SparkleSize;
                float  _SparkleSmoothness;
                half4  _EdgeColor;
                float  _EdgeStrength;
                float  _EdgePower;
                half4  _RimColor;
                float  _RimPower;
                float  _RimThreshold;
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
                OUT.fogCoord    = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4  baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // --- Main directional light (+ shadows) ---
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                float NdotL = dot(normalWS, mainLight.direction);
                float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Smooth glossy toon gradient (0 = shadow, 1 = lit)
                float toon = smoothstep(_ShadeThreshold,
                                        _ShadeThreshold + _ShadeSmoothness,
                                        NdotL) * atten;

                // Scene GI so the ball reads as part of the sky lighting
                half3 ambient = SampleSH(normalWS) * _AmbientStrength;

                half3 litRGB   = baseColor.rgb * mainLight.color;
                half3 shadeRGB = baseColor.rgb * _ShadeColor.rgb;
                half3 diffuse  = lerp(shadeRGB, litRGB, toon) + baseColor.rgb * ambient;

                // --- Extra lights (compiles out if none) ---
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < lightCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, IN.positionWS);
                    float aToon = smoothstep(_ShadeThreshold,
                                             _ShadeThreshold + _ShadeSmoothness,
                                             dot(normalWS, addLight.direction))
                                * addLight.distanceAttenuation * addLight.shadowAttenuation;
                    diffuse += baseColor.rgb * addLight.color * aToon;
                }
                #endif

                // --- Fresnel (drives both glassy edge + top sheen) ---
                float fresnel = 1.0 - saturate(dot(viewDirWS, normalWS));

                // Glassy edge darkening -> candy/glass ball volume
                float edge = pow(fresnel, _EdgePower);
                diffuse = lerp(diffuse, diffuse * _EdgeColor.rgb,
                               saturate(edge * _EdgeStrength));

                // --- Big soft glossy highlight + tiny sparkle (blob-based, no pow) ---
                float3 halfVec = normalize(mainLight.direction + viewDirWS);
                float  NdotH   = saturate(dot(normalWS, halfVec));

                float mainHi = smoothstep(1.0 - _SpecularSize - _SpecularSmoothness,
                                          1.0 - _SpecularSize + _SpecularSmoothness, NdotH);
                float sparkle = smoothstep(1.0 - _SparkleSize - _SparkleSmoothness,
                                           1.0 - _SparkleSize + _SparkleSmoothness, NdotH);

                float hiGate = saturate(toon * 0.7 + 0.3);
                half3 gloss  = (mainHi * _SpecularColor.rgb * _SpecularStrength
                              + sparkle * _SparkleColor.rgb) * hiGate;

                // --- Top sheen rim (bright, lit side only) ---
                float rim = smoothstep(_RimThreshold, _RimThreshold + 0.08,
                                       pow(fresnel, 1.0 / max(_RimPower, 0.001)));
                half3 rimLight = rim * _RimColor.rgb * _RimColor.a * toon;

                half3 color = diffuse + gloss + rimLight;
                color = MixFog(color, IN.fogCoord);
                return half4(color, baseColor.a);
            }
            ENDHLSL
        }

        // -------------------------------------------------------------
        //  Shadow casting
        // -------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.0
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _ShadeColor;
                float  _ShadeThreshold;
                float  _ShadeSmoothness;
                float  _AmbientStrength;
                half4  _SpecularColor;
                float  _SpecularSize;
                float  _SpecularSmoothness;
                float  _SpecularStrength;
                half4  _SparkleColor;
                float  _SparkleSize;
                float  _SparkleSmoothness;
                half4  _EdgeColor;
                float  _EdgeStrength;
                float  _EdgePower;
                half4  _RimColor;
                float  _RimPower;
                float  _RimThreshold;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // -------------------------------------------------------------
        //  Depth only
        // -------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _ShadeColor;
                float  _ShadeThreshold;
                float  _ShadeSmoothness;
                float  _AmbientStrength;
                half4  _SpecularColor;
                float  _SpecularSize;
                float  _SpecularSmoothness;
                float  _SpecularStrength;
                half4  _SparkleColor;
                float  _SparkleSize;
                float  _SparkleSmoothness;
                half4  _EdgeColor;
                float  _EdgeStrength;
                float  _EdgePower;
                half4  _RimColor;
                float  _RimPower;
                float  _RimThreshold;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}