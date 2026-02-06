Shader "Unlit/EtherealOrb"
{
    Properties
    {
        [Header(Glow Colors)]
        [HDR] _RimColor ("Rim Color (Edge)", Color) = (2, 2, 2, 1) // High intensity white
        _InnerColor ("Inner Color (Center)", Color) = (0.1, 0.1, 0.1, 0.2) // Dark center

        [Header(Rim Settings)]
        _RimPower ("Rim Sharpness", Range(0.5, 8.0)) = 3.0
        _RimScale ("Rim Intensity", Range(0.0, 5.0)) = 1.5

        [Header(Animation)]
        _PulseSpeed ("Breathing Speed", Range(0.0, 5.0)) = 1.5
        _PulseAmount ("Breathing Depth", Range(0.0, 0.5)) = 0.05
    }
    SubShader
    {
        // "Queue"="Transparent" allows us to see through the black parts
        // "Blend SrcAlpha One" adds the light to the background (Glowing effect)
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One 
        ZWrite Off // Disable ZWrite for proper hologram transparency
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            // Variables
            half4 _RimColor;
            half4 _InnerColor;
            half _RimPower;
            half _RimScale;
            half _PulseSpeed;
            half _PulseAmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // 1. Calculate World Space Normals and View Direction
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Calculate Fresnel (Rim Light)
                // Dot product of View Direction and Normal returns 1.0 at center, 0.0 at edge.
                // We invert it (1.0 - dot) so the Edge is 1.0.
                half NdotV = saturate(dot(i.normal, i.viewDir));
                half fresnel = 1.0 - NdotV;

                // 2. Sharpen the rim
                // Using 'pow' makes the transition exponential. 
                // Higher _RimPower = thinner, sharper ring.
                half rimTerm = pow(fresnel, _RimPower);

                // 3. Animation (Breathing)
                // We oscillate the intensity slightly over time
                half pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                rimTerm *= pulse * _RimScale;

                // 4. Color Composition
                // Start with the Inner Color
                half4 finalColor = _InnerColor;
                
                // Add the Rim Color on top
                // We multiply by opacity to ensure the edge is solid light
                finalColor += _RimColor * rimTerm;

                // 5. Opacity Logic
                // The edge should be opaque, the center transparent
                // We use the rim value to drive the alpha channel
                finalColor.a = saturate(_InnerColor.a + rimTerm);

                return finalColor;
            }
            ENDCG
        }
    }
}