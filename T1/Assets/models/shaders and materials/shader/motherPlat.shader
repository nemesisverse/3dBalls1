Shader "Custom/motherPlat"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.3, 0.5, 1.0, 1.0)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _FresnelColor("Fresnel Color", Color) = (1.0, 0.45, 0.0, 1.0)
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 3.0
        _EmissionStrength("Emission Strength", Range(0.0, 5.0)) = 1.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _AlphaPower("Alpha Rim Power", Range(0.1, 10.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _FresnelColor;
                float  _FresnelPower;
                float  _EmissionStrength;
                float  _Metallic;
                float  _Smoothness;
                float  _AlphaPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Albedo
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float NdotV = saturate(dot(N, V));

                // Fresnel rim color
                float fresnel    = pow(1.0 - NdotV, _FresnelPower);
                half3 fresnelRGB = _FresnelColor.rgb * fresnel;

                // Emission: constant rim glow — NO breathing pulse
                half3 emission = fresnelRGB * _EmissionStrength;

                // Alpha: 0 at center (NdotV=1), 1 at rim (NdotV=0)
                float alpha = pow(1.0 - NdotV, _AlphaPower);

                // Lighting
                Light  mainLight = GetMainLight(IN.shadowCoord);
                float3 L = mainLight.direction;
                float3 H = normalize(V + L);
                float  NdotL = saturate(dot(N, L));
                float  NdotH = saturate(dot(N, H));
                half3  radiance = mainLight.color * mainLight.shadowAttenuation;

                half3 diffuse  = albedo.rgb * radiance * NdotL;
                half3 specular = pow(NdotH, exp2(_Smoothness * 10.0 + 1.0)) * radiance * _Metallic;
                half3 ambient  = SampleSH(N) * albedo.rgb;

                // Scale lit color by alpha so the body fades with transparency
                half3 finalColor = (ambient + diffuse + specular) * alpha + emission;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _FresnelColor;
                float  _FresnelPower;
                float  _EmissionStrength;
                float  _Metallic;
                float  _Smoothness;
                float  _AlphaPower;
            CBUFFER_END

            struct ShadowAttribs  { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct ShadowVaryings { float4 positionHCS : SV_POSITION; };

            ShadowVaryings shadowVert(ShadowAttribs IN)
            {
                ShadowVaryings OUT;
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS   = TransformObjectToWorldNormal(IN.normalOS);
                float3 lightDir = normalize(_MainLightPosition.xyz);
                posWS = ApplyShadowBias(posWS, normWS, lightDir);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                #if UNITY_REVERSED_Z
                    OUT.positionHCS.z = min(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionHCS.z = max(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return OUT;
            }

            half4 shadowFrag(ShadowVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   depthVert
            #pragma fragment depthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _FresnelColor;
                float  _FresnelPower;
                float  _EmissionStrength;
                float  _Metallic;
                float  _Smoothness;
                float  _AlphaPower;
            CBUFFER_END

            struct DepthAttribs  { float4 positionOS : POSITION; };
            struct DepthVaryings { float4 positionHCS : SV_POSITION; };

            DepthVaryings depthVert(DepthAttribs IN)
            {
                DepthVaryings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 depthFrag(DepthVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}