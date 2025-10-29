Shader "Unlit/GradientDotsGrid"
{
     Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {} // required for UI
        _ColorA ("Gradient Color A", Color) = (1, 0.4, 0.8, 1)
        _ColorB ("Gradient Color B", Color) = (0, 1, 1, 1)
        _DotSize ("Dot Size", Range(0.001, 0.2)) = 0.05
        _GridScale ("Grid Scale", Range(1, 100)) = 10
        _Speed ("Fade Speed", Range(0.1, 10)) = 1
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
            float _DotSize;
            float _GridScale;
            float _Speed;
            float _RandomSeed;
            // ❌ removed _Time declaration (Unity provides it automatically)

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
                // base texture sample (needed for UI compatibility)
                float4 baseCol = tex2D(_MainTex, i.uv);

                // grid logic
                float2 gridUV = i.uv * _GridScale;
                float2 cell = floor(gridUV);
                float2 local = frac(gridUV) - 0.5;

                float rnd = random(cell);
                float dist = length(local);
                float dotMask = smoothstep(_DotSize, _DotSize * 1.2, dist);
                dotMask = 1 - dotMask;

                // use built-in _Time (Unity provides this automatically)
                float fade = sin((_Time.y * _Speed) + rnd * 6.283);
                fade = fade * 0.5 + 0.5;

                float4 color = lerp(_ColorA, _ColorB, rnd);

                float alpha = dotMask * fade;
                color.a = alpha;

                return color * baseCol;
            }
            ENDHLSL
        }
    }
}