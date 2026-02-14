Shader "Custom/SolidOffWhiteGlow"
{
    Properties
    {
        [HDR] _GlowColor("Glow Color", Color) = (1, 0.98, 0.9, 1)
        _GlowIntensity("Intensity", Range(0, 10)) = 2.5
        _CoreSharpness("Core Sharpness", Range(0.1, 5)) = 2.0
        _CenterWhite("Center Whiteness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Additive blending for that light-emissive look
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _GlowColor;
            float _GlowIntensity;
            float _CoreSharpness;
            float _CenterWhite;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calculate distance from center (assuming a quad or sprite)
                float2 centerUV = input.uv - 0.5;
                float dist = length(centerUV) * 2.0; // 0 at center, 1 at edges
                
                // Create a smooth falloff
                // Saturate keeps the values between 0 and 1
                float falloff = saturate(1.0 - dist);
                
                // Sharpen the core using power
                float mask = pow(falloff, _CoreSharpness);
                
                // COLOR LOGIC: 
                // To keep it "Off-White" but "Solid," we mix the tint with pure white 
                // based on how close to the center we are.
                float3 baseColor = _GlowColor.rgb;
                float3 finalRGB = lerp(baseColor, float3(1, 1, 1), mask * _CenterWhite);
                
                // Apply Intensity
                finalRGB *= mask * _GlowIntensity;

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}