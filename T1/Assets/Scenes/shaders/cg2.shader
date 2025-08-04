Shader "Unlit/cg2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TimeSpeed ("Pulse Speed", Float) = 1
        _DistortionStrength ("Warp Intensity", Float) = 0.2
        _GlowIntensity ("Glow Intensity", Float) = 1.5
        _ColorShift ("Hue Shift", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

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
            float _DistortionStrength;
            float _GlowIntensity;
            float _ColorShift;

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

                // Plasma-style UV distortion
                float2 uv = i.uv;
                uv += sin(uv.yx * 15.0 + t * 3.0) * _DistortionStrength;
                uv += cos(uv.xy * 10.0 - t * 2.0) * (_DistortionStrength * 0.5);

                // Sample base texture
                fixed4 texColor = tex2D(_MainTex, uv);

                // Holographic color shifting using sine waves
                float r = 0.5 + 0.5 * sin(texColor.r * _ColorShift + t);
                float g = 0.5 + 0.5 * sin(texColor.g * _ColorShift + t + 2.0);
                float b = 0.5 + 0.5 * sin(texColor.b * _ColorShift + t + 4.0);
                float pulse = 0.5 + 0.5 * sin(t * 2.0 + uv.x * 5.0 + uv.y * 5.0);

                fixed4 finalColor = fixed4(r, g, b, 1.0) * pulse * _GlowIntensity;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return finalColor;
            }
            ENDCG
        }
    }
}
