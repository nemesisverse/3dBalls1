Shader "Unlit/cg3"
{
    Properties
    {
        [Header(Painterly Gradient)]
        _ColorTop ("Deep Space (Top)", Color) = (0.02, 0.02, 0.1, 1) // Dark Navy
        _ColorMid ("Midnight (Middle)", Color) = (0.1, 0.1, 0.35, 1) // Indigo
        _ColorBot ("Horizon (Bottom)", Color) = (0.0, 0.6, 0.8, 1)   // Cyan/Teal Glow
        _GradientSpread ("Gradient Spread", Range(0.1, 2.0)) = 1.2

        [Header(Stylized Stars)]
        _StarDensity ("Star Density", Range(50, 400)) = 150.0
        _StarSize ("Star Size", Range(0.001, 0.02)) = 0.008
        _TwinkleSpeed ("Twinkle Speed", Float) = 2.0

        [Header(Anime Clouds)]
        _CloudColor ("Cloud Color", Color) = (0.2, 0.3, 0.6, 0.4) // Pale Blue-Grey
        _CloudSpeed ("Cloud Drift", Float) = 0.02
        _CloudDensity ("Cloud Cover", Range(0.0, 1.0)) = 0.5
        _CloudSharpness ("Brush Stroke Hardness", Range(0.0, 1.0)) = 0.3

        [Header(Shooting Star)]
        _ShootingStarSpeed ("Shooting Star Speed", Float) = 3.0
        _ShootingStarFreq ("Shooting Star Frequency", Range(0.0, 1.0)) = 0.15
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
                float3 viewDir : TEXCOORD1;
            };

            fixed4 _ColorTop, _ColorMid, _ColorBot;
            float _GradientSpread;
            float _StarDensity, _StarSize, _TwinkleSpeed;
            fixed4 _CloudColor;
            float _CloudSpeed, _CloudDensity, _CloudSharpness;
            float _ShootingStarSpeed, _ShootingStarFreq;

            // --- NOISE & HASH FUNCTIONS ---
            float hash21(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Value Noise for "Painted" look (Softer than Perlin)
            float valueNoise(float2 uv) {
                float2 i = floor(uv);
                float2 f = frac(uv);
                // Hermite interpolation (Smoothstep) for that soft "brush" feel
                f = f * f * (3.0 - 2.0 * f); 
                
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // --- LAYERS ---

            // 1. Anime Stars (Diamond/Cross shape hint + Twinkle)
            float Stars(float3 viewDir, float t) {
                // Spherical Mapping
                float2 uv = float2(atan2(viewDir.z, viewDir.x) / 6.28318, asin(viewDir.y) / 3.14159);
                
                // Parallax drift (very slow rotation)
                uv.x += t * 0.005;

                uv *= _StarDensity;
                float2 gridID = floor(uv);
                float2 gridUV = frac(uv) - 0.5;

                float n = hash21(gridID);
                
                // Randomize star center slightly
                float2 offset = (float2(n, frac(n*10.0)) - 0.5) * 0.5;
                float d = length(gridUV - offset);

                // Twinkle: Sine wave based on time and random ID
                float twinkle = 0.5 + 0.5 * sin(t * _TwinkleSpeed + n * 100.0);
                
                // Size threshold
                float s = 0.0;
                if (n > 0.95) { // Only top 5% of cells get a star (Sparse but distinct)
                     // Soft glow circle
                    s = smoothstep(_StarSize, _StarSize * 0.5, d);
                    s *= twinkle;
                }
                return s;
            }

            // 2. Painted Clouds
            float Clouds(float3 viewDir, float t) {
                // Map to 2D
                float2 uv = viewDir.xz / (viewDir.y + 0.5); // Dome projection pushes clouds to horizon
                uv *= 2.0;
                uv += float2(t * 0.5, t * 0.1); // Diagonal drift

                // Layer 1
                float n = valueNoise(uv);
                // Layer 2 (Smaller details)
                n += valueNoise(uv * 2.0 + t) * 0.5;
                n *= 0.66; // Normalize

                // "Painterly" Thresholding
                // Creates distinct islands of cloud rather than full noise
                float cloudShape = smoothstep(1.0 - _CloudDensity, 1.0 - _CloudDensity + _CloudSharpness, n);
                
                // Fade out near Zenith (Straight up) so it doesn't look weird at the pole
                float zenithFade = smoothstep(0.8, 0.2, viewDir.y);
                
                return cloudShape * zenithFade;
            }

            // 3. Shooting Star (The Anime Trope)
            float ShootingStar(float3 viewDir, float t) {
                // We map a specific band of the sky to check for shooting stars
                float2 uv = float2(atan2(viewDir.z, viewDir.x) / 6.28318, asin(viewDir.y) / 3.14159);
                
                // Rotate UVs so shooting star falls diagonally
                float angle = 0.785; // 45 degrees
                float s = sin(angle), c = cos(angle);
                float2 rotUV = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);

                // Grid for timing
                // We split the sky into "lanes". Only some lanes get a star.
                rotUV.y += t * _ShootingStarFreq; // Move through time slots
                float2 gridID = floor(rotUV * 10.0); 
                float2 gridUV = frac(rotUV * 10.0);

                // Random check: Does this lane have a star right now?
                float rnd = hash21(gridID);
                if (rnd > 0.98) { // Rare occurrence
                    // Draw the line
                    // The "head" is at the bottom of the cell (moving down)
                    float pos = gridUV.y; 
                    float width = 1.0 - gridUV.x; // Taper width? No, keep simple line.
                    
                    // Trail math: 
                    // Bright at head (near 0), fading tail (near 1)
                    float trail = smoothstep(1.0, 0.0, gridUV.y); 
                    
                    // Thin line check
                    float lineShape = smoothstep(0.02, 0.0, abs(gridUV.x - 0.5));
                    
                    return trail * lineShape * 2.0; // Boost brightness
                }
                return 0;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Get world direction
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(o.worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.worldPos);
                float t = _Time.y;

                // --- 1. PAINTERLY GRADIENT ---
                // Map Y to 0-1. Use 'pow' to squash the horizon color for a dramatic sky.
                float y = saturate(viewDir.y * 0.5 + 0.5);
                y = pow(y, _GradientSpread); 

                // 3-Color Mix
                float3 skyColor = lerp(_ColorBot.rgb, _ColorMid.rgb, smoothstep(0.0, 0.5, y));
                skyColor = lerp(skyColor, _ColorTop.rgb, smoothstep(0.5, 1.0, y));

                // --- 2. STARS ---
                float stars = Stars(viewDir, t);
                skyColor += stars;

                // --- 3. CLOUDS ---
                // Clouds are alpha blended, not additive (to look like paint)
                float clouds = Clouds(viewDir, t * _CloudSpeed);
                // Mix cloud color based on alpha
                skyColor = lerp(skyColor, _CloudColor.rgb, clouds * _CloudColor.a);
                
                // Add a subtle "Rim Light" to the clouds (Moonlight)
                skyColor += clouds * 0.2 * _ColorBot.rgb;

                // --- 4. SHOOTING STAR ---
                float shoot = ShootingStar(viewDir, t * _ShootingStarSpeed);
                skyColor += shoot;

                return fixed4(skyColor, 1.0);
            }
            ENDCG
        }
    }
}