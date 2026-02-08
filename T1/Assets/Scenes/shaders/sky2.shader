Shader "Unlit/AestheticComicSky"
{
    Properties
    {
        _SkyTop ("Sky Top", Color) = (0.07, 0.05, 0.15, 1)    // Midnight Purple
        _SkyBottom ("Horizon", Color) = (1.0, 0.45, 0.5, 1)   // Sunset Pink
        _SunColor ("Sun", Color) = (1.0, 0.95, 0.8, 1)
        _SunDir ("Sun Direction", Vector) = (0, 0.1, 1, 0)
        _DotSize ("Halftone Scale", Float) = 150.0
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

            float4 _SkyTop, _SkyBottom, _SunColor, _SunDir;
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

            // Noise function for paper texture
            float paperNoise(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

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
                float3 sunDir = normalize(_SunDir.xyz);
                float sunDot = dot(dir, sunDir);

                // 1. DYNAMIC GRADIENT BANDS
                // We use a smoothstep but quantize it for that "painted" look
                float grad = saturate(dir.y * 0.5 + 0.5);
                float steppedGrad = floor(grad * 8.0) / 8.0;
                fixed4 color = lerp(_SkyBottom, _SkyTop, steppedGrad);

                // 2. THE SUN & ENERGY RINGS
                // Instead of one circle, we use multiple steps for a "bloom" effect
                float sunCore = step(0.985, sunDot);
                float sunRing1 = step(0.97, sunDot);
                float sunRing2 = step(0.94, sunDot);
                
                // Colors for rings (offsetting the hue slightly for beauty)
                color = lerp(color, _SkyBottom * 1.5, sunRing2 * 0.4);
                color = lerp(color, _SunColor * 0.8, sunRing1);
                color = lerp(color, _SunColor, sunCore);

                // 3. AESTHETIC HALFTONE CLOUDS
                // Clouds that look like ink droplets
                float2 cloudUV = screenUV * _DotSize;
                float dotPattern = sin(cloudUV.x) * cos(cloudUV.y);
                
                // We mask the dots to appear mostly in the transition zone
                float cloudMask = smoothstep(0.3, 0.5, grad) * smoothstep(0.7, 0.5, grad);
                float dots = step(dotPattern, (grad - 0.5) * 2.0);
                color = lerp(color, _SkyTop, dots * cloudMask * 0.3);

                // 4. PAPER GRAIN & CHROMATIC ABERRATION
                float grain = paperNoise(screenUV + _Time.x) * 0.05;
                color += grain;

                // Subtle edge "glitch" color split
                fixed4 colorR = lerp(_SkyBottom, _SkyTop, floor((grad + 0.005) * 8.0) / 8.0);
                fixed4 colorB = lerp(_SkyBottom, _SkyTop, floor((grad - 0.005) * 8.0) / 8.0);
                color.r = lerp(color.r, colorR.r, 0.2);
                color.b = lerp(color.b, colorB.b, 0.2);

                return color;
            }
            ENDCG
        }
    }
}