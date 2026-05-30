Shader "Custom/AnimeSkybox"
{
    Properties
    {
        _SkyTop      ("Sky Top Color",     Color) = (0.04, 0.18, 0.70, 1)
        _SkyMid      ("Sky Mid Color",     Color) = (0.18, 0.50, 0.90, 1)
        _SkyHorizon  ("Sky Horizon Color", Color) = (0.50, 0.76, 0.97, 1)
        _CloudBright ("Cloud Bright",      Color) = (1.00, 0.98, 1.00, 1)
        _CloudShadow ("Cloud Shadow",      Color) = (0.78, 0.76, 0.94, 1)
        _CloudScale  ("Scale",             Float) = 3.5
        _CloudSpeed  ("Drift Speed",       Float) = 0.012
        _CloudCover  ("Coverage",          Range(0, 1)) = 0.52
        _CloudSoft   ("Edge Softness",     Range(0.01, 0.40)) = 0.18
        _CloudCenterY("Band Center Y",     Range(0, 1)) = 0.28
        _CloudBandW  ("Band Width",        Range(0.05, 1)) = 0.52
        _StarColor   ("Star Color",        Color) = (1.00, 0.84, 0.20, 1)
        _StarGrid    ("Grid Density",      Float) = 6.0
        _StarSzMin   ("Min Radius (UV)",   Float) = 0.015
        _StarSzMax   ("Max Radius (UV)",   Float) = 0.045
        _StarGlow    ("Glow Amount",       Range(0, 2)) = 0.9
        _StarFloor   ("Appear Above Y",    Range(0, 1)) = 0.28
        _StarAspect  ("UV Aspect Ratio",   Float) = 1.0
        _ShootColor  ("Shoot Color",       Color) = (1.00, 0.94, 0.60, 1)
        _ShootSpeed  ("Speed",             Float) = 0.10
        _TimeScale   ("Time Scale",        Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "RenderPipeline" = "UniversalPipeline" "Queue" = "Background" }
        Cull Front
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4  _SkyTop, _SkyMid, _SkyHorizon;
                half4  _CloudBright, _CloudShadow;
                float  _CloudScale, _CloudSpeed, _CloudCover, _CloudSoft;
                float  _CloudCenterY, _CloudBandW;
                half4  _StarColor;
                float  _StarGrid, _StarSzMin, _StarSzMax, _StarGlow;
                float  _StarFloor, _StarAspect;
                half4  _ShootColor;
                float  _ShootSpeed, _TimeScale;
            CBUFFER_END

            float h21(float2 p)
            {
                p  = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 17.3);
                return frac(p.x * p.y);
            }

            float2 h22(float2 p)
            {
                p  = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 74.11);
                return frac((p.xx + p.yx) * p.xy);
            }

            float vNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(h21(i),                h21(i + float2(1, 0)), u.x),
                    lerp(h21(i + float2(0, 1)), h21(i + float2(1, 1)), u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                float2x2 rot = float2x2(0.80, 0.60, -0.60, 0.80);
                for (int i = 0; i < 5; i++)
                {
                    v += a * vNoise(p);
                    p  = mul(rot, p) * 2.0;
                    a *= 0.5;
                }
                return v;
            }

            float sdStar5(float2 p, float r, float rf)
            {
                const float2 k1 = float2( 0.809016994375, -0.587785252192);
                const float2 k2 = float2(-0.809016994375, -0.587785252192);
                p.x  = abs(p.x);
                p   -= 2.0 * max(dot(k1, p), 0.0) * k1;
                p   -= 2.0 * max(dot(k2, p), 0.0) * k2;
                p.x  = abs(p.x);
                p.y -= r;
                float2 ba = rf * float2(-k1.y, k1.x) - float2(0, 1);
                float  h  = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
                return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
            }

            float drawStar(float2 loc, float2 ctr, float r, float asp)
            {
                float2 p = (loc - ctr) * float2(asp, 1.0);
                return 1.0 - smoothstep(-r * 0.12, r * 0.22, sdStar5(p, r, 0.40));
            }

            float drawGlow(float2 loc, float2 ctr, float r, float asp)
            {
                float2 p = (loc - ctr) * float2(asp, 1.0);
                return exp(-dot(p, p) / (r * r * 5.0)) * _StarGlow;
            }

            float crossFlare(float2 loc, float2 ctr, float r, float asp)
            {
                float2 p = (loc - ctr) * float2(asp, 1.0);
                float  d = length(p);
                return (exp(-abs(p.y) / (r * 0.09)) + exp(-abs(p.x) / (r * 0.09)))
                       * exp(-d / (r * 3.0)) * 0.28;
            }

            float3 StarField(float2 uv)
            {
                float2 sc   = uv * _StarGrid;
                float2 cell = floor(sc);
                float2 loc  = frac(sc);
                float3 col  = 0;

                for (int xi = -1; xi <= 1; xi++)
                for (int yi = -1; yi <= 1; yi++)
                {
                    float2 nb  = float2(xi, yi);
                    float2 rng = h22(cell + nb);
                    float  prs = step(0.50, rng.x);
                    float2 pos = nb + rng;
                    float  r   = lerp(_StarSzMin, _StarSzMax, rng.y) * _StarGrid;
                    float  br  = 0.55 + rng.y;
                    col += _StarColor.rgb * br * prs *
                           (drawStar(loc, pos, r, _StarAspect)
                          + drawGlow(loc, pos, r, _StarAspect)
                          + crossFlare(loc, pos, r, _StarAspect));
                }
                return saturate(col);
            }

            float ShootingStar(float2 uv, float t, float2 seed)
            {
                float2 rng  = h22(seed);
                float2 rng2 = h22(seed + 6.1);
                float2 start = float2(rng.x, 0.46 + rng.y * 0.46);
                float2 dir   = normalize(float2(0.55 + rng2.x * 0.30, -0.18 - rng2.y * 0.22));
                float  per   = 7.0 + rng.x * 11.0;
                float  pha   = frac(t * _ShootSpeed / per + rng2.y);
                float  tLen  = 0.07 + rng2.x * 0.06;
                float  vis   = smoothstep(0.015, 0.07, pha) * (1.0 - smoothstep(0.82, 0.98, pha));
                float2 head  = start + dir * pha * 0.18;
                float2 d     = uv - head;
                float  alon  = dot(d, dir);
                float  perp  = abs(dot(d, float2(-dir.y, dir.x)));
                float  inTrl = step(0, -alon) * step(0, alon + tLen);
                float  tn    = clamp(-alon / tLen, 0.0, 1.0);
                float  w     = lerp(0.0020, 0.0005, tn);
                float  trail = (1.0 - smoothstep(0.0, w, perp)) * pow(1.0 - tn, 1.5) * vis * inTrl;
                float  hd    = length(uv - head);
                return trail + exp(-hd * hd * 6000.0) * 0.45 * vis;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posHCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv     = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y * _TimeScale;
                float  y  = uv.y;

                half3 bot    = lerp(_SkyHorizon.rgb, _SkyMid.rgb, pow(saturate(y / 0.55), 1.3));
                half3 top    = lerp(_SkyMid.rgb, _SkyTop.rgb, saturate((y - 0.55) / 0.45));
                half3 skyCol = (y < 0.55) ? bot : top;

                float  starVis = smoothstep(_StarFloor, _StarFloor + 0.14, y);
                float3 stars   = StarField(uv) * starVis;

                float shoot = 0;
                shoot += ShootingStar(uv, t, float2(1.43, 2.91));
                shoot += ShootingStar(uv, t, float2(3.87, 5.12));
                shoot += ShootingStar(uv, t, float2(6.21, 0.74));
                shoot += ShootingStar(uv, t, float2(8.55, 4.33));
                float3 shootCol = saturate(shoot) * _ShootColor.rgb * starVis;

                float cDist = saturate(abs(y - _CloudCenterY) / (_CloudBandW * 0.5));
                float fade  = 1.0 - smoothstep(0.0, 1.0, cDist);

                float2 cUV = uv * _CloudScale         + float2(t * _CloudSpeed, 0);
                float2 wUV = uv * (_CloudScale * 0.4) + float2(t * _CloudSpeed * 0.6, 0);
                float  w1  = fbm(wUV);
                float  w2  = fbm(wUV + float2(w1, w1 * 0.55));
                float  dn  = fbm(cUV + float2(w1 * 0.35, w2 * 0.22));

                float  thr   = 1.0 - _CloudCover;
                float  cloud = smoothstep(thr - _CloudSoft, thr + _CloudSoft, dn) * fade;

                float  det  = fbm(cUV * 2.0 + 0.5);
                half3  cCol = lerp(_CloudShadow.rgb, _CloudBright.rgb, smoothstep(0.30, 0.75, det));
                float  etnt = (1.0 - smoothstep(thr - _CloudSoft * 0.2, thr + _CloudSoft, dn)) * fade;
                cCol        = lerp(cCol, half3(0.72, 0.70, 0.93), etnt * 0.45);
                float  wisp = smoothstep(thr - _CloudSoft * 2.2, thr - _CloudSoft * 0.2, dn) * fade;

                half3 col = skyCol;
                col += stars;
                col += shootCol;
                col  = lerp(col, lerp(skyCol, cCol, 0.45), wisp * (1.0 - cloud) * 0.55);
                col  = lerp(col, cCol, cloud);
                col  = lerp(col, half3(0.70, 0.86, 1.0), (1.0 - smoothstep(0.0, 0.22, y)) * 0.28);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}