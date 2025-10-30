Shader "Unlit/CuteWaveGradient"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // Required for UI
        _ColorA ("Color A", Color) = (1,0.8,0.9,1)
        _ColorB ("Color B", Color) = (0.7,0.9,1,1)
        _Scale ("Scale", Float) = 5
        _Speed ("Wave Speed", Float) = 1
        _Intensity ("Wave Intensity", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Scale;
            float _Speed;
            float _Intensity;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * _Scale;

                float wave = sin(uv.x + _Time.y * _Speed) * _Intensity;
                float gradient = saturate(uv.y + wave);

                fixed4 col = lerp(_ColorA, _ColorB, gradient);
                return col;
            }
            ENDCG
        }
    }
}
