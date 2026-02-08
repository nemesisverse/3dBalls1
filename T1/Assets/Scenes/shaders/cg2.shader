Shader "Unlit/IonToonSphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.95, 0.35, 0.4, 1)    // Coral/Salmon
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.1, 0.25, 1) // Deep Purple
        _HighlightColor ("Highlight Color", Color) = (1, 0.9, 0.7, 1)
        _Step1 ("Shadow Threshold", Range(0, 1)) = 0.2
        _Step2 ("Highlight Threshold", Range(0, 1)) = 0.8
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;
            float4 _ShadowColor;
            float4 _HighlightColor;
            float _Step1;
            float _Step2;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // Get normal in world space for lighting
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Lighting Direction (Matching the top-right light in your image)
                float3 lightDir = normalize(float3(0.5, 0.5, 1.0));
                float d = dot(normalize(i.worldNormal), lightDir);
                
                // 2. Remap Dot product from [-1, 1] to [0, 1]
                float lightIntensity = d * 0.5 + 0.5;

                // 3. THE TOON LOGIC: Hard transitions using step()
                // If lightIntensity > _Step1, use BaseColor, else ShadowColor
                fixed4 color = lerp(_ShadowColor, _BaseColor, step(_Step1, lightIntensity));
                
                // If lightIntensity > _Step2, add a sharp Highlight
                color = lerp(color, _HighlightColor, step(_Step2, lightIntensity));

                // 4. Optional: Multiply by texture (keep it subtle for the toon look)
                fixed4 tex = tex2D(_MainTex, i.uv);
                
                return color * tex;
            }
            ENDCG
        }
    }
}