Shader "Unlit/ToonComicSky"
{
    Properties
    {
        _SkyColor ("Top Color", Color) = (0.1, 0.05, 0.2, 1) // Deep Purple
        _HorizonColor ("Horizon Color", Color) = (0.95, 0.35, 0.4, 1) // Coral/Pink
        _SunColor ("Sun Color", Color) = (1, 0.9, 0.4, 1)
        _SunDir ("Sun Direction", Vector) = (0.5, 0.2, 1, 0)
        _DotSize ("Halftone Size", Float) = 100.0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _SkyColor, _HorizonColor, _SunColor, _SunDir;
            float _DotSize;

            struct appdata {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.viewDir = v.texcoord;
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.viewDir);
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // 1. STYLIZED GRADIENT (The "Ion Temple" Step)
                // Instead of a smooth transition, we "step" the horizon
                float vertical = dir.y * 0.5 + 0.5;
                float steps = floor(vertical * 6.0) / 6.0;
                fixed4 color = lerp(_HorizonColor, _SkyColor, steps);

                // 2. THE SUN (Hard Circle)
                float3 sunDir = normalize(_SunDir.xyz);
                float sunPresence = dot(dir, sunDir);
                float sunDisc = step(0.98, sunPresence);
                float sunGlow = step(0.92, sunPresence);
                
                color = lerp(color, _SunColor, sunGlow * 0.5); // Outer Glow
                color = lerp(color, _SunColor, sunDisc);       // Hard Core

                // 3. SPIDER-VERSE HALFTONE CLOUDS
                // We place dots specifically near the horizon line
                float dotPattern = sin(screenUV.x * _DotSize) * sin(screenUV.y * _DotSize);
                float cloudMask = step(0.4, vertical) * step(vertical, 0.6); // Banded area
                
                // If in the cloud band, inject the halftone pattern
                float stylizedClouds = step(0.1, dotPattern * (1.0 - vertical));
                color = lerp(color, _HorizonColor * 1.2, stylizedClouds * cloudMask * 0.4);

                // 4. HORIZONTAL "TECH" LINES
                float lines = step(0.98, frac(vertical * 20.0));
                color = lerp(color, _SkyColor, lines * 0.2);

                return color;
            }
            ENDCG
        }
    }
}