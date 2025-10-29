Shader "Unlit/AnimatedGradient"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {} // for UI compatibility
        _ColorA ("Gradient Color A", Color) = (1, 0.4, 0.8, 1)
        _ColorB ("Gradient Color B", Color) = (0, 1, 1, 1)
        _GridScale ("Gradient Scale", Range(0.1, 10)) = 1
        _Speed ("Movement Speed", Range(0.1, 5)) = 0.5
        _RandomSeed ("Random Seed", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanvasOverlay"="True"
        }

        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _ColorA;
            float4 _ColorB;
            float _GridScale;
            float _Speed;
            float _RandomSeed;

            // use built-in _Time provided by Unity

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453 * _RandomSeed);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 baseCol = tex2D(_MainTex, i.uv);

                // Add movement over time
                float2 move = float2(sin(_Time.y * _Speed), cos(_Time.y * _Speed * 0.7)) * 0.3;

                // Scale and offset UVs
                float2 uv = i.uv * _GridScale + move;

                // Generate some pseudo-random gradient noise pattern
                float n = sin(uv.x * 2.0 + random(uv) * 6.283 + _Time.y * _Speed) * 0.5 +
                          cos(uv.y * 2.0 + random(uv + 3.14) * 6.283 + _Time.y * _Speed * 0.8) * 0.5;

                // Normalize to 0–1 range
                n = n * 0.5 + 0.5;

                // Mix between ColorA and ColorB
                float4 col = lerp(_ColorA, _ColorB, n);

                // Alpha always full for background
                col.a = 1;

                return col * baseCol;
            }
            ENDHLSL
        }
    }
}