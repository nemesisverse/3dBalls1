Shader "Custom/GameOverUI"
{
    Properties
    {
        [MainColor]   _BaseColor      ("Tint",              Color)   = (1, 1, 1, 1)
        [MainTexture] _BaseMap        ("Base Map",          2D)      = "white" {}

        _SkyDeep      ("Sky — Deep Space",   Color) = (0.01, 0.02, 0.10, 1)
        _SkyMid       ("Sky — Mid Night",    Color) = (0.04, 0.07, 0.22, 1)
        _SkyLow       ("Sky — Lower Night",  Color) = (0.07, 0.05, 0.18, 1)
        _SkyHorizon   ("Sky — Horizon",      Color) = (0.14, 0.07, 0.20, 1)

        _GlowColor    ("Glow — Color",       Color) = (0.95, 0.55, 0.10, 1)
        _GlowPosX     ("Glow — X Position",  Range(0,1))  = 0.35
        _GlowWidth    ("Glow — Width",       Float)       = 4.0
        _GlowHeight   ("Glow — Falloff",     Float)       = 10.0
        _GlowStrength ("Glow — Strength",    Float)       = 0.55

        _StarDensityA ("Stars — Density Far",   Float) = 90.0
        _StarDensityB ("Stars — Density Mid",   Float) = 160.0
        _StarDensityC ("Stars — Density Near",  Float) = 260.0
        _StarSize     ("Stars — Size",          Float) = 0.65
        _StarBright   ("Stars — Brightness",    Float) = 1.3
        _TwinkleSpeed ("Stars — Twinkle Speed", Float) = 1.1

        _MilkyColor   ("Milky Way — Color",     Color) = (0.12, 0.20, 0.55, 1)
        _MilkyStr     ("Milky Way — Strength",  Float) = 0.45
        _MilkyBandPos ("Milky Way — Band Pos",  Float) = 0.55
        _MilkyBandW   ("Milky Way — Band Width",Float) = 6.0

        _VigStr       ("Vignette — Strength",   Range(0,3)) = 0.9
        _VigRadius    ("Vignette — Radius",     Range(0.1,1)) = 0.65

        _TimeScale    ("Time Scale",            Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                half4  _SkyDeep;
                half4  _SkyMid;
                half4  _SkyLow;
                half4  _SkyHorizon;
                half4  _GlowColor;
                float  _GlowPosX;
                float  _GlowWidth;
                float  _GlowHeight;
                float  _GlowStrength;
                float  _StarDensityA;
                float  _StarDensityB;
                float  _StarDensityC;
                float  _StarSize;
                float  _StarBright;
                float  _TwinkleSpeed;
                half4  _MilkyColor;
                float  _MilkyStr;
                float  _MilkyBandPos;
                float  _MilkyBandW;
                float  _VigStr;
                float  _VigRadius;
                float  _TimeScale;
            CBUFFER_END

            // ── Hash helpers ──────────────────────────────────────

            float Hash2(float2 p)
            {
                float3 q = frac(float3(p.xyx) * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            float2 Hash2v(float2 p)
            {
                float3 q = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                q += dot(q, q.yzx + 33.33);
                return frac((q.xx + q.yz) * q.zy);
            }

            // ── Value noise + FBM (for milky way) ────────────────

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash2(i);
                float b = Hash2(i + float2(1, 0));
                float c = Hash2(i + float2(0, 1));
                float d = Hash2(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FBM(float2 uv, int octaves)
            {
                float val = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                float2 offset = float2(0, 0);
                for (int i = 0; i < octaves; i++)
                {
                    val   += amp * ValueNoise(uv * freq + offset);
                    amp   *= 0.48;
                    freq  *= 2.07;
                    offset += float2(0.131 * float(i), 0.237 * float(i));
                }
                return val;
            }

            // ── Star layer ────────────────────────────────────────

            float StarLayer(float2 uv, float cellSeed, float time)
            {
                float2 cell   = floor(uv);
                float2 local  = frac(uv) - 0.5;

                float  seed   = Hash2(cell + cellSeed);
                float2 jitter = (Hash2v(cell + cellSeed) - 0.5) * 0.8;
                float2 pos    = local - jitter;

                float visibility = step(0.30, seed);

                float twinkleFq = 1.0 + seed * 3.0;
                float twinkle   = sin(time * _TwinkleSpeed * twinkleFq + seed * 6.28318);
                twinkle         = lerp(0.45, 1.0, twinkle * 0.5 + 0.5);

                float dist  = dot(pos, pos);
                float sigma = _StarSize * 0.012;
                float star  = exp(-dist / (sigma + 0.0001));

                float cross = 0.0;
                [branch]
                if (seed > 0.85)
                {
                    float cx = exp(-abs(pos.x) * 55.0) * exp(-pos.y * pos.y * 220.0);
                    float cy = exp(-abs(pos.y) * 55.0) * exp(-pos.x * pos.x * 220.0);
                    cross    = (cx + cy) * 0.25;
                }

                return (star + cross) * twinkle * visibility * _StarBright;
            }

            // ── Vertex ────────────────────────────────────────────

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            // ── Fragment ──────────────────────────────────────────

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y * _TimeScale;

                // Base texture
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                // Sky gradient (4-stop bottom → top)
                float y   = uv.y;
                float t01 = smoothstep(0.00, 0.18, y);
                float t12 = smoothstep(0.10, 0.50, y);
                float t23 = smoothstep(0.35, 0.85, y);

                half3 sky = _SkyHorizon.rgb;
                sky = lerp(sky, _SkyLow.rgb,  t01);
                sky = lerp(sky, _SkyMid.rgb,  t12);
                sky = lerp(sky, _SkyDeep.rgb, t23 * t23);

                // Warm horizon glow
                float hGlow = exp(-y * _GlowHeight);
                float hSpot = exp(-pow(uv.x - _GlowPosX, 2.0) * _GlowWidth);
                sky += _GlowColor.rgb * hGlow * hSpot * _GlowStrength;

                // Milky Way band
                float2 mUV   = float2(uv.x * 1.3 + t * 0.003, uv.y * 2.1 + t * 0.002);
                float  nebula = FBM(mUV * 2.8, 5);
                float  bandPos  = uv.x * 0.28 + uv.y * 0.72;
                float  bandMask = exp(-pow(bandPos - _MilkyBandPos, 2.0) * _MilkyBandW);
                bandMask *= smoothstep(0.08, 0.45, y);
                sky += _MilkyColor.rgb * nebula * bandMask * _MilkyStr;

                // Star field (3 depth layers)
                float stars = 0.0;
                stars += StarLayer(uv * _StarDensityA + float2(0.00, 0.00), 0.0,  t) * 0.55;
                stars += StarLayer(uv * _StarDensityB + float2(0.37, 0.61), 13.7, t) * 0.90;
                stars += StarLayer(uv * _StarDensityC + float2(0.72, 0.23), 27.4, t) * 0.65;
                stars *= smoothstep(0.04, 0.30, y);

                half3 starTint = lerp(half3(0.85, 0.90, 1.00),
                                      half3(1.00, 0.95, 0.80),
                                      Hash2(uv * 6.3));
                sky += starTint * stars;

                // Atmospheric shimmer
                float shimmer = FBM(uv * 9.0 + float2(t * 0.025, t * 0.012), 3) * 0.025;
                sky += _SkyMid.rgb * shimmer;

                // Vignette
                float2 vigUV = uv - 0.5;
                float  vig   = 1.0 - dot(vigUV, vigUV) / (_VigRadius * _VigRadius);
                vig = saturate(pow(abs(vig), _VigStr));
                sky *= vig;

                // Composite with base texture
                half3 final = lerp(sky, tex.rgb, tex.a * (1.0 - tex.a * 0.4));

                // Color grade: slight desaturate + cool blue push
                float lum = dot(final, half3(0.299, 0.587, 0.114));
                final     = lerp(half3(lum, lum, lum), final, 1.08);
                final.b  *= 1.06;
                final     = pow(max(final, 0.0), 0.95);

                return half4(saturate(final), 1.0);
            }
            ENDHLSL
        }
    }
}