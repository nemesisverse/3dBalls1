Shader "Unlit/PlasmaSphere_DOTS"
{
    Properties
    {
        [Header(Colors)]
        _ColorA ("Primary Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _ColorB ("Secondary Color", Color) = (0.0, 1.0, 0.8, 1.0)
        [HDR] _RimColor ("Rim Color", Color) = (0.0, 0.8, 1.0, 1.0)

        [Header(Plasma Pattern)]
        _NoiseScale ("Noise Scale", Float) = 2.0
        _NoiseSpeed ("Animation Speed", Vector) = (0.1, 0.2, 0.0, 0.0)
        _Distortion ("Distortion Amount", Range(0, 2)) = 1.0
        _Brightness ("Pattern Brightness", Range(0, 5)) = 1.5
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One // Additive Blending
        ZWrite Off
        Cull Back
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma exclude_renderers gles
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // --- Define Matrices for DOTS ---
            float4x4 unity_MatrixVP;
            #define UNITY_MATRIX_VP unity_MatrixVP
            #define UNITY_MATRIX_M unity_ObjectToWorld

            #define UNITY_SETUP_DOTS_SH_COEFFS
            #define UNITY_SETUP_DOTS_RENDER_BOUNDS

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // Added Normal for Rim Light
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // --- Define Properties in CBUFFER ---
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _RimColor;
                float _NoiseScale;
                float4 _NoiseSpeed;
                float _Distortion;
                float _Brightness;
                float _RimPower;
            CBUFFER_END

            // --- Define DOTS Instancing Properties ---
            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _ColorA)
                    UNITY_DOTS_INSTANCED_PROP(float4, _ColorB)
                    UNITY_DOTS_INSTANCED_PROP(float4, _RimColor)
                    UNITY_DOTS_INSTANCED_PROP(float, _NoiseScale)
                    UNITY_DOTS_INSTANCED_PROP(float4, _NoiseSpeed)
                    UNITY_DOTS_INSTANCED_PROP(float, _Distortion)
                    UNITY_DOTS_INSTANCED_PROP(float, _Brightness)
                    UNITY_DOTS_INSTANCED_PROP(float, _RimPower)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #undef unity_ObjectToWorld
                UNITY_DOTS_INSTANCING_START(BuiltinPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_ObjectToWorld)
                UNITY_DOTS_INSTANCING_END(BuiltinPropertyMetadata)

                // Macros to access properties
                #define _ColorA     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ColorA)
                #define _ColorB     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ColorB)
                #define _RimColor   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _RimColor)
                #define _NoiseScale UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _NoiseScale)
                #define _NoiseSpeed UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _NoiseSpeed)
                #define _Distortion UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Distortion)
                #define _Brightness UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Brightness)
                #define _RimPower   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimPower)
            #else
                CBUFFER_START(UnityPerDraw)
                    float4x4 unity_ObjectToWorld;
                    float4x4 unity_WorldToObject;
                    float4 unity_LODFade;
                    float4 unity_WorldTransformParams;
                CBUFFER_END
            #endif

            // --- Noise Functions ---
            float3 hash33(float3 p) {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            float noise(float3 p) {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(dot(hash33(i + float3(0,0,0)), f - float3(0,0,0)),
                                      dot(hash33(i + float3(1,0,0)), f - float3(1,0,0)), u.x),
                                 lerp(dot(hash33(i + float3(0,1,0)), f - float3(0,1,0)),
                                      dot(hash33(i + float3(1,1,0)), f - float3(1,1,0)), u.x), u.y),
                            lerp(lerp(dot(hash33(i + float3(0,0,1)), f - float3(0,0,1)),
                                      dot(hash33(i + float3(1,0,1)), f - float3(1,0,1)), u.x),
                                 lerp(dot(hash33(i + float3(0,1,1)), f - float3(0,1,1)),
                                      dot(hash33(i + float3(1,1,1)), f - float3(1,1,1)), u.x), u.y), u.z);
            }

            float fbm(float3 p) {
                float f = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 3; i++) {
                    float3 distP = p + noise(p + _Time.y * _NoiseSpeed.xyz) * _Distortion;
                    f += amp * (noise(distP) * 0.5 + 0.5);
                    p = p * 2.0;
                    amp *= 0.5;
                }
                return f;
            }

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // Manual Matrix Multiplication (Matches SimpleDots style)
                float4 worldPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.worldPos = worldPos.xyz;
                
                // Calculate Normal (Assuming uniform scale, simple rotation)
                o.normal = normalize(mul((float3x3)UNITY_MATRIX_M, v.normal));

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // --- Plasma Logic ---
                float3 noisePos = i.worldPos * _NoiseScale;
                float pattern = fbm(noisePos);
                
                float4 plasmaCol = lerp(_ColorA, _ColorB, pattern);
                plasmaColor.rgb *= _Brightness;
                plasmaColor.a *= pattern; 

                // --- Rim Light Logic ---
                // Calculate view direction manually using built-in camera position
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                
                float NdotV = saturate(dot(normalize(i.normal), viewDir));
                float rim = pow(1.0 - NdotV, _RimPower);
                float4 rimGlow = _RimColor * rim;

                float4 finalColor = plasmaColor + rimGlow;
                finalColor.a = saturate(plasmaColor.a + rimGlow.a);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}