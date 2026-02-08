Shader "Unlit/ComicPrintSphere"
{
    Properties
    {
        _BaseColor ("Paper Color", Color) = (0.9, 0.3, 0.3, 1)
        _ShadowColor ("Ink Color", Color) = (0.1, 0.1, 0.3, 1)
        _DotSize ("Ben-Day Dot Scale", Float) = 60.0
        _Offset ("Print Misalignment", Range(0, 0.05)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _BaseColor, _ShadowColor;
            float _DotSize, _Offset;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.pos);
                float3 lightDir = normalize(float3(0.5, 0.5, 1.0));
                
                // 1. Lighting with a "Toon" Ramp
                float d = dot(normal, lightDir) * 0.5 + 0.5;
                float shadowMask = step(d, 0.45);

                // 2. Ben-Day Dots (Halftone) - Offset per color channel for "Print Error"
                float2 uv = i.screenPos.xy / i.screenPos.w;
                
                // Red Channel Dots
                float dotR = sin((uv.x + _Offset) * _DotSize) * sin((uv.y + _Offset) * _DotSize);
                // Blue Channel Dots
                float dotB = sin((uv.x - _Offset) * _DotSize) * sin((uv.y - _Offset) * _DotSize);
                
                float halftones = step(d, dotR * 0.5 + 0.2);

                // 3. Ink Bleed Silhouette (Hand-drawn edge)
                float rim = 1.0 - saturate(dot(normal, float3(0,0,1)));
                float inkEdge = step(0.7, rim);

                // 4. Combine Colors
                fixed4 color = _BaseColor;
                
                // Apply shadow halftone
                color = lerp(color, _ShadowColor, halftones);
                
                // Offset Glitch (Chromatic Aberration on highlights)
                float spec = step(0.8, d);
                color.r += step(0.78, d) * 0.2;
                color.b += step(0.82, d) * 0.2;
                color = lerp(color, fixed4(1,1,1,1), spec);

                // Ink Outline
                color = lerp(color, fixed4(0,0,0,1), inkEdge * 0.5);

                return color;
            }
            ENDCG
        }
    }
}