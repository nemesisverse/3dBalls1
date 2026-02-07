Shader "Unlit/AnimeNightSky_v3"
{
    Properties
    {
        [Header(Painterly Gradient)]
        _ColorTop ("Deep Space (Top)", Color) = (0.02, 0.02, 0.1, 1)
        _ColorMid ("Midnight (Middle)", Color) = (0.1, 0.1, 0.35, 1)
        _ColorBot ("Horizon (Bottom)", Color) = (0.0, 0.6, 0.8, 1)
        _GradientSpread ("Gradient Spread", Range(0.1, 2.0)) = 1.2

        [Header(Stylized Stars)]
        _StarDensity ("Star Density", Range(50, 400)) = 150.0
        _StarSize ("Star Size", Range(0.001, 0.02)) = 0.008
        _TwinkleSpeed ("Twinkle Speed", Float) = 2.0

        [Header(Anime Clouds)]
        _CloudColor ("Cloud Color", Color) = (0.2, 0.3, 0.6, 0.4)
        _CloudSpeed ("Cloud Drift", Float) = 0.02
        _CloudDensity ("Cloud Cover", Range(0.0, 1.0)) = 0.5
        _CloudSharpness ("Brush Stroke Hardness", Range(0.0, 1.0)) = 0.3

        [Header(Shooting Star)]
        _ShootingStarSpeed ("Shooting Star Speed", Float) = 3.0
        _ShootingStarFreq ("Shooting Star Frequency", Range(0.0, 1.0)) = 0.1
        _ShootingStarSize ("Shooting Star Thickness", Range(0.5, 2.0)) = 1.0
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

            fixed4 _ColorTop, _ColorMid, _ColorBot;
            float _GradientSpread;
            float _StarDensity, _StarSize, _TwinkleSpeed;
            fixed4 _CloudColor;
            float _CloudSpeed, _CloudDensity, _CloudSharpness;
            float _ShootingStarSpeed, _ShootingStarFreq, _ShootingStarSize;

            // --- NOISE FUNCTIONS ---
            float hash21(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv) {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f); 
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // --- SHOOTING STAR LOGIC ---
            float ShootingStar(float3 viewDir, float t) {
                // 1. Map to Spherical UVs
                float2 uv = float2(atan2(viewDir.z, viewDir.x) / 6.28318, asin(viewDir.y) / 3.14159);
                
                // 2. EQUATOR BIAS (Probability Mask)
                // Stars are more likely near y=0 (Equator/Horizon)
                float equatorProb = 1.0 - abs(uv.y * 2.5); 
                equatorProb = smoothstep(0.0, 1.0, equatorProb); 

                // 3. Grid Setup
                // Skew the UVs for diagonal fall
                float2 gridUV = uv;
                gridUV.x += gridUV.y * 0.4; 
                gridUV.x *= 12.0; // Fewer lanes for thicker feel
                
                // 4. Randomized Time Flow
                float id = floor(gridUV.x);
                float randomOffset = hash21(float2(id, 42.0)); 
                
                // Random speed per lane
                float speed = _ShootingStarSpeed * (0.8 + 0.5 * randomOffset);
                float laneTime = t * speed + randomOffset * 100.0;
                
                float slotID = floor(laneTime);
                float slotUV = frac(laneTime); // 0 to 1 progress within the slot

                // 5. Spawn Check
                float rnd = hash21(float2(id, slotID));
                
                // Frequency check combined with equator bias
                if (rnd > (1.0 - _ShootingStarFreq * equatorProb)) {
                    
                    // --- DRAWING THE STAR SHAPE ---
                    // 'pos' is the distance from the head (0.0) to the tail (1.0)
                    float pos = slotUV; 
                    
                    // Randomize Length: Some stars fade out faster
                    float lengthMod = 0.5 + 0.5 * hash21(float2(rnd, 99.0));
                    pos = pos / lengthMod; // Scale position by length

                    // If pos > 1.0, we are past the tail, so return 0
                    if(pos > 1.0) return 0.0;

                    // 1. Horizontal Centering
                    float center = 0.5 + (hash21(float2(rnd, 1.0)) - 0.5) * 0.3;
                    float distFromCenter = abs(frac(gridUV.x) - center);
                    
                    // 2. Tapering Thickness (Teardrop shape)
                    // The star is thickest at pos=0 (head) and 0 width at pos=1 (tail)
                    float thickness = 0.03 * _ShootingStarSize * (1.0 - pos);
                    
                    // Draw the body line
                    float body = smoothstep(thickness, 0.0, distFromCenter);

                    // 3. The Head (Glowing Nucleus)
                    // A small circle at the very front (pos near 0)
                    float headSize = 0.04 * _ShootingStarSize;
                    float distToHead = length(float2(distFromCenter, pos));
                    float head = smoothstep(headSize, 0.0, distToHead) * 4.0; // Extra Bright

                    // 4. The Tail Fade
                    // Brightness falls off exponentially towards the back
                    float tailFade = smoothstep(1.0, 0.0, pos); // Linear fade
                    tailFade = pow(tailFade, 2.0); // Exponential fade (comet look)

                    return (body + head) * tailFade;
                }
                
                return 0.0;
            }

            float Stars(float3 viewDir, float t) {
                float2 uv = float2(atan2(viewDir.z, viewDir.x) / 6.28318, asin(viewDir.y) / 3.14159);
                uv.x += t * 0.005;
                uv *= _StarDensity;
                float2 gridID = floor(uv);
                float2 gridUV = frac(uv) - 0.5;
                float n = hash21(gridID);
                float2 offset = (float2(n, frac(n*10.0)) - 0.5) * 0.5;
                float d = length(gridUV - offset);
                float twinkle = 0.5 + 0.5 * sin(t * _TwinkleSpeed + n * 100.0);
                float s = 0.0;
                if (n > 0.95) {
                    s = smoothstep(_StarSize, _StarSize * 0.5, d);
                    s *= twinkle;
                }
                return s;
            }

            float Clouds(float3 viewDir, float t) {
                float2 uv = viewDir.xz / (viewDir.y + 0.5); 
                uv *= 2.0;
                uv += float2(t * 0.5, t * 0.1); 
                float n = valueNoise(uv);
                n += valueNoise(uv * 2.0 + t) * 0.5;
                n *= 0.66; 
                float cloudShape = smoothstep(1.0 - _CloudDensity, 1.0 - _CloudDensity + _CloudSharpness, n);
                float zenithFade = smoothstep(0.8, 0.2, viewDir.y);
                return cloudShape * zenithFade;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.worldPos);
                float t = _Time.y;

                float y = saturate(viewDir.y * 0.5 + 0.5);
                y = pow(y, _GradientSpread); 

                float3 skyColor = lerp(_ColorBot.rgb, _ColorMid.rgb, smoothstep(0.0, 0.5, y));
                skyColor = lerp(skyColor, _ColorTop.rgb, smoothstep(0.5, 1.0, y));

                float stars = Stars(viewDir, t);
                skyColor += stars;

                float clouds = Clouds(viewDir, t * _CloudSpeed);
                skyColor = lerp(skyColor, _CloudColor.rgb, clouds * _CloudColor.a);
                skyColor += clouds * 0.2 * _ColorBot.rgb;

                // Call Updated Shooting Star
                float shoot = ShootingStar(viewDir, t);
                skyColor += shoot;

                return fixed4(skyColor, 1.0);
            }
            ENDCG
        }
    }
}