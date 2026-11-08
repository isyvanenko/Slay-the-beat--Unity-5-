Shader "Hidden/MoneyRainParticle"
{
    // Solid cut-out particles for MoneyRain. Bills and coins get brighter when they face the
    // camera and darker edge-on, so they flash as they tumble.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Cutoff", Range(0, 1)) = 0.5
        _EdgeDark ("Edge-on Darkness", Range(0, 1)) = 0.45
        _Shine ("Facing Shine", Range(0, 2)) = 1.15
    }

    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            half _Cutoff, _EdgeDark, _Shine;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 n = normalize(UnityObjectToWorldNormal(v.normal));
                float3 toCam = normalize(_WorldSpaceCameraPos - worldPos);
                half facing = abs(dot(n, toCam));
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.color.rgb *= lerp(1 - _EdgeDark, _Shine, facing);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv) * i.color;
                clip(c.a - _Cutoff);
                return half4(c.rgb, 1);
            }
            ENDCG
        }
    }
}
