Shader "Unlit/OrbitingFlakes"
{
    Properties
    {
        [Header(Global Atmosphere)]
        _ColorTop ("Top Color", Color) = (0.0, 0.02, 0.08, 1)
        _ColorBot ("Bottom Color", Color) = (0.05, 0.1, 0.2, 1)
        
        [Header(Orbiting Flakes)]
        _StarDensity ("Flake Density", Range(50, 500)) = 150.0
        _StarSize ("Flake Size", Range(0.001, 0.03)) = 0.01
        _RotationSpeed ("Orbit Speed", Range(-1.0, 1.0)) = 0.05 // Controls revolution speed
        
        [Header(Aurora Borealis)]
        _AuroraColor1 ("Aurora Primary", Color) = (0.0, 0.8, 0.5, 1)
        _AuroraColor2 ("Aurora Secondary", Color) = (0.3, 0.0, 0.8, 1)
        _AuroraSpeed ("Aurora Speed", Float) = 0.05
        _AuroraIntensity ("Aurora Intensity", Range(0.0, 2.0)) = 1.0
        _AuroraHeight ("Aurora Band (Min, Max)", Vector) = (-0.2, 0.8, 0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        Cull Off 
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            fixed4 _ColorTop, _ColorBot;
            float _StarDensity, _StarSize, _RotationSpeed;
            fixed4 _AuroraColor1, _AuroraColor2;
            float _AuroraSpeed, _AuroraIntensity;
            float2 _AuroraHeight;

            // Pseudo-random hash
            float hash21(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 3D Orbiting Flake Function
            float stars(float3 viewDir, float t) {
                // 1. Spherical Mapping
                float2 uv;
                uv.x = atan2(viewDir.z, viewDir.x) / 6.28318; // Longitude
                uv.y = asin(viewDir.y) / 3.14159;            // Latitude
                
                // 2. REVOLUTION LOGIC
                // We add time to uv.x to make them spin around the Y-axis (the poles)
                uv.x += t * _RotationSpeed; 

                // 3. Grid Logic
                uv *= _StarDensity;
                float2 gridID = floor(uv);
                
                // 4. Generate Flakes
                float n = hash21(gridID); 
                
                // Create solid white circles
                // 'n' determines which cell gets a star.
                // We compare 'n' to a threshold based on _StarSize.
                // Using a sharp smoothstep (0.01 width) makes them solid hard dots.
                float starShape = smoothstep(1.0 - _StarSize, 1.0 - _StarSize + 0.01, n);

                // Note: No twinkle math here. If starShape > 0, it is 1.0 (Solid White).
                return starShape;
            }

            float aurora(float3 viewDir, float t) {
                float2 p = viewDir.xz * 2.0 + float2(t*0.1, t*0.2);
                p += float2(sin(p.y * 0.8 - t), cos(p.x * 0.9 + t*0.7)) * 0.5;
                float noiseVal = sin(p.x * 1.5) * cos(p.y * 1.5 + t * 0.2);
                noiseVal += sin(p.x * 3.5 - t*1.2) * 0.3;
                noiseVal = noiseVal * 0.5 + 0.5;
                noiseVal = pow(noiseVal, 4.0) * 2.0;
                float mask = smoothstep(_AuroraHeight.x, _AuroraHeight.x + 0.2, viewDir.y);
                mask *= smoothstep(_AuroraHeight.y + 0.2, _AuroraHeight.y, viewDir.y);
                return saturate(noiseVal * mask * _AuroraIntensity);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = normalize(mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.worldPos);
                float t = _Time.y;

                // 1. Background
                float gradientFactor = viewDir.y * 0.5 + 0.5;
                fixed3 finalCol = lerp(_ColorBot.rgb, _ColorTop.rgb, gradientFactor);

                // 2. Aurora
                float auroraVal = aurora(viewDir, t * _AuroraSpeed);
                float3 auroraCol = lerp(_AuroraColor1.rgb, _AuroraColor2.rgb, smoothstep(0, 1, auroraVal));
                finalCol += auroraCol * auroraVal;

                // 3. Solid White Flakes
                float starVal = stars(viewDir, t);
                
                // Additive blend: Since 'starVal' is 0 or 1, this adds pure white.
                // saturate ensures we don't blow out the HDR bloom too much if not desired.
                finalCol += starVal; 

                return fixed4(finalCol, 1.0);
            }
            ENDCG
        }
    }
}