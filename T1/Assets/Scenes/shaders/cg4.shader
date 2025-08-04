Shader "Unlit/cg4"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TimeSpeed ("Pulse Speed", Float) = 1
        _GlowIntensity ("Glow Intensity", Float) = 1.5
        _ColorShift ("Hue Shift", Float) = 2
        _GlitchIntensity ("Glitch Flicker Strength", Float) = 0.2
        _GridIntensity ("Grid Overlay Intensity", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _TimeSpeed;
            float _GlowIntensity;
            float _ColorShift;
            float _GlitchIntensity;
            float _GridIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 screenUV : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenUV = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _TimeSpeed;

                // Sample base texture (no UV distortion)
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // Holographic color shifting
                float r = 0.5 + 0.5 * sin(texColor.r * _ColorShift + t);
                float g = 0.5 + 0.5 * sin(texColor.g * _ColorShift + t + 2.0);
                float b = 0.5 + 0.5 * sin(texColor.b * _ColorShift + t + 4.0);
                float pulse = 0.5 + 0.5 * sin(t * 2.0 + i.uv.x * 5.0 + i.uv.y * 5.0);

                fixed4 finalColor = fixed4(r, g, b, 1.0) * pulse * _GlowIntensity;

                // Glitch flicker effect
                float glitchNoise = frac(sin(dot(i.uv, float2(12.9898, 78.233))) * 43758.5453);
                float glitch = step(0.97, frac(glitchNoise + sin(t * 20.0))) * _GlitchIntensity;

                // Grid overlay
                float2 grid = abs(frac(i.uv * 10.0) - 0.5);
                float gridLine = smoothstep(0.48, 0.5, max(grid.x, grid.y));
                float gridOverlay = (1.0 - gridLine) * _GridIntensity;

                finalColor.rgb += glitch + gridOverlay;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return finalColor;
            }
            ENDCG
        }
    }
}
