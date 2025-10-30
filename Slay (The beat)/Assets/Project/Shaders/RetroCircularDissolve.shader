Shader "Unlit/RetroCircularDissolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress (0=black, 1=revealed)", Range(0,1)) = 0
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Scale ("Radius Scale", Float) = 1.2
        _Feather ("Feather", Range(0.001,0.5)) = 0.08
        _NoiseScale ("Noise Scale", Float) = 8.0
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.35
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.15
        _EdgeColor ("Edge Color (rim glow)", Color) = (1,1,1,1)
        _EdgeStrength ("Edge Strength", Range(0,1)) = 0.25
        _TimeSpeed ("Noise Time Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Progress;
            float4 _Center;
            float _Scale;
            float _Feather;
            float _NoiseScale;
            float _NoiseStrength;
            float _ScanlineIntensity;
            float4 _EdgeColor;
            float _EdgeStrength;
            float _TimeSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Simple hash-based noise
            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // four corners
                float a = hash12(i + float2(0.0,0.0));
                float b = hash12(i + float2(1.0,0.0));
                float c = hash12(i + float2(0.0,1.0));
                float d = hash12(i + float2(1.0,1.0));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                float2 pp = p;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    v += amp * noise(pp);
                    pp *= 2.0;
                    amp *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV and center
                float2 uv = i.uv;
                float2 center = _Center.xy;

                // radial distance (0 at center)
                float dist = distance(uv, center);

                // scaled radius in UV space
                float radius = _Progress * _Scale;

                // procedural noise
                float t = _Time.y * _TimeSpeed;
                float n = fbm(uv * _NoiseScale + t);

                // apply noise as dither to the radius
                float noiseOffset = (n - 0.5) * _NoiseStrength;

                // compute raw mask (0 = inside revealed, 1 = covered/black)
                // we want inside circle (dist <= radius+noiseOffset) to be revealed (transparent)
                float edge = dist - (radius + noiseOffset);

                // feathered alpha: smoothstep to make a soft rim
                float alpha = saturate( smoothstep(_Feather, 0.0, edge) );

                // Add retro scanlines (modulate alpha a little)
                float scan = sin((uv.y + t*0.2) * 120.0) * 0.5 + 0.5;
                alpha = lerp(alpha, alpha * (1.0 - _ScanlineIntensity * scan), _ScanlineIntensity);

                // Rim/edge glow: highlight near the boundary
                float rim = saturate(1.0 - abs(edge) / (_Feather + 0.001));
                float3 rimCol = _EdgeColor.rgb * pow(rim, 2.0) * _EdgeStrength;

                // Compose final color: black background with optional rim color
                float3 bg = float3(0.0, 0.0, 0.0);
                float3 col = lerp(rimCol, bg, alpha);

                // output alpha should be 1 for opaque black, 0 for fully revealed (transparent)
                float outAlpha = alpha;

                return float4(col, outAlpha);
            }
            ENDCG
        }
    }
    FallBack Off
}