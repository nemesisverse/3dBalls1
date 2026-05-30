Shader "Skybox/Cubemap Blend"
{
    Properties
    {
        [Header(Sky)]
        _SkyColor        ("Sky Color",          Color)            = (0.0, 0.001, 0.008, 1)

        [Header(Milky Way)]
        _NebulaColorOuter("Nebula Outer Color",  Color)           = (0.02, 0.05, 0.28, 1)
        _NebulaColorInner("Nebula Core Color",   Color)           = (0.15, 0.30, 0.85, 1)
        _NebulaStrength  ("Nebula Strength",     Range(0.0, 1.5)) = 0.72
        _NebulaScale     ("Cloud Scale",         Range(0.5, 5.0)) = 2.0
        _BandTilt        ("Band Tilt",           Range(-1.0, 1.0))= 0.12
        _BandWidth       ("Band Width",          Range(0.1,  3.0))= 0.72

        [Header(Stars)]
        _StarDensity     ("Density",             Range(50,  900))   = 480
        _StarSizeA       ("Fine Star Size",      Range(0.001,0.02)) = 0.003
        _StarSizeB       ("Mid Star Size",       Range(0.003,0.05)) = 0.008
        _StarSizeC       ("Bright Star Size",    Range(0.005,0.08)) = 0.016
        _StarBrightness  ("Brightness",          Range(0.5,  5.0))  = 2.0
        _StarThreshold   ("Threshold",           Range(0.1, 0.95))  = 0.36
        _StarBlueTint    ("Blue Tint",           Range(0.0,  1.0))  = 0.38

        [Header(Twinkling)]
        _TwinkleSpeed    ("Twinkle Speed",       Range(0.0,  8.0))  = 1.0
        _TwinkleAmount   ("Twinkle Amount",      Range(0.0,  1.0))  = 0.20
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #include "UnityCG.cginc"

            float4 _SkyColor;
            float4 _NebulaColorOuter, _NebulaColorInner;
            float  _NebulaStrength, _NebulaScale, _BandTilt, _BandWidth;
            float  _StarDensity, _StarSizeA, _StarSizeB, _StarSizeC;
            float  _StarBrightness, _StarThreshold, _StarBlueTint;
            float  _TwinkleSpeed, _TwinkleAmount;

            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f     { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            // ─── Hash Utilities ───────────────────────────────────────────────
            float  h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float  h31(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }
            float2 h22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // ─── 3-D Value Noise (no equirectangular seam on sphere) ──────────
            float vnoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);           // smooth-step curve
                float a = h31(i);                              float b = h31(i + float3(1,0,0));
                float c = h31(i + float3(0,1,0));              float d = h31(i + float3(1,1,0));
                float e = h31(i + float3(0,0,1));              float g = h31(i + float3(1,0,1));
                float hv= h31(i + float3(0,1,1));              float k = h31(i + float3(1,1,1));
                return lerp(lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y),
                            lerp(lerp(e,g,u.x), lerp(hv,k,u.x), u.y), u.z);
            }

            // ─── Fractional Brownian Motion over 3-D view direction ──────────
            //     Runs on the raw direction vector → zero UV seam anywhere
            float fbm3(float3 p)
            {
                float v = 0.0, amp = 0.5, freq = 1.0;
                [unroll] for (int n = 0; n < 4; n++)
                {
                    v    += amp * vnoise3(p * freq);
                    freq *= 2.1;
                    amp  *= 0.45;
                }
                return v;
            }

            // ─── Single Star-Field Layer (equirectangular UV grid) ────────────
            float3 StarLayer(float2 uv, float scale, float size)
            {
                float2 cell  = floor(uv * scale);
                float2 local = frac(uv * scale);
                float3 col   = 0;

                [unroll] for (int dx = -1; dx <= 1; dx++)
                [unroll] for (int dy = -1; dy <= 1; dy++)
                {
                    float2 nid    = cell + float2(dx, dy);
                    float2 spos   = h22(nid);
                    float  bright = h21(nid + float2(5.3,  9.1));
                    float  tPhase = h21(nid + float2(7.1, 23.9));
                    float  blueC  = h21(nid + float2(3.7, 17.3));

                    if (bright < _StarThreshold) continue;

                    // Per-star twinkling animation
                    float tw = lerp(1.0 - _TwinkleAmount, 1.0,
                        0.5 + 0.5 * sin(_Time.y * _TwinkleSpeed * (0.4 + tPhase * 0.6)
                                        + tPhase * UNITY_PI * 2.0));

                    float2 diff = local - spos - float2(dx, dy);
                    float  dist = length(diff);
                    float  norm = (bright - _StarThreshold) / max(1.0 - _StarThreshold, 0.001);

                    // Crisp hard dot — size grows with brightness
                    float core = smoothstep(size * (0.3 + norm * 0.7), 0.0, dist);

                    // White → blue-white tint per star
                    float3 sc = lerp(float3(1,1,1), float3(0.72, 0.87, 1.0), blueC * _StarBlueTint);

                    col += sc * core * tw * _StarBrightness * (0.4 + norm * 0.6);
                }
                return col;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);

                // Equirectangular UV — used only for the star grid
                float2 uv = float2(
                    (atan2(d.z, d.x) + UNITY_PI) / (UNITY_PI * 2.0),
                    acos(clamp(d.y, -1.0, 1.0)) / UNITY_PI);

                // ── Sky base ──────────────────────────────────────────────────
                float3 col = _SkyColor.rgb;

                // ── Milky Way band (seam-free 3-D FBM) ────────────────────────
                // Tilted great-circle band: bandDot → 0 at band centre
                float bandDot  = d.y - _BandTilt * d.x;
                float bandMask = exp(-bandDot * bandDot / (_BandWidth * _BandWidth));

                // Three FBM passes at different frequencies for cloud detail
                float3 np     = d * _NebulaScale;
                float  cloud  = fbm3(np);
                float  wisp   = fbm3(np * 1.9 + float3(4.3, 2.1, 8.7));
                float  detail = fbm3(np * 3.8 + float3(1.1, 6.5, 3.3));

                // Multiply cloud layers → patchy masses, mask to band
                float nebula = saturate(cloud * wisp * 3.2 * bandMask);
                // Fine detail brightens the densest cores
                float glow   = saturate(nebula * detail * 2.5) * 0.75;

                // Blend outer blue → bright core colour
                float3 nebulaCol = lerp(_NebulaColorOuter.rgb, _NebulaColorInner.rgb, glow)
                                 * (nebula + glow);
                col += nebulaCol * _NebulaStrength;

                // ── Three star layers (fine / mid / sparse-bright) ────────────
                float s = sqrt(_StarDensity);
                col += StarLayer(uv, s * 2.9,  _StarSizeA);    // dense fine
                col += StarLayer(uv, s * 1.8,  _StarSizeB);    // mid
                col += StarLayer(uv, s * 0.95, _StarSizeC);    // sparse bright

                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}