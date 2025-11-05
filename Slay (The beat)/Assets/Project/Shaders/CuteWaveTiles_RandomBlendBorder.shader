Shader "Unlit/CuteWaveTiles_RandomBlendBorder"
{
   Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // UI Compatibility
        _ColorA ("Base Color A", Color) = (1,0.8,0.9,1)
        _ColorB ("Base Color B", Color) = (0.7,0.9,1,1)
        _Scale ("Tile Scale", Float) = 5
        _BlendSpeed ("Color Blend Speed", Float) = 1

        _BorderColor ("Border Color", Color) = (0.1,0.1,0.1,1)
        _BorderWidth ("Border Width", Range(0,0.4)) = 0.1
        _BorderSoftness ("Border Softness", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
            float _BlendSpeed;

            fixed4 _BorderColor;
            float _BorderWidth;
            float _BorderSoftness;

            struct appdata 
            { 
                float4 vertex : POSITION; 
                float2 uv : TEXCOORD0; 
                fixed4 color : COLOR; // <-- CHANGED: Add vertex color
            };
            
            struct v2f 
            { 
                float2 uv : TEXCOORD0; 
                float4 vertex : SV_POSITION; 
                fixed4 color : COLOR; // <-- CHANGED: Add vertex color
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color; // <-- CHANGED: Pass vertex color to fragment
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture (your PNG)
                fixed4 tex = tex2D(_MainTex, i.uv); // <-- CHANGED: Sample the texture

                float2 uv = i.uv * _Scale;

                // Tile ID
                float2 tile = floor(uv);

                // Random tile color mixing factor
                float rnd = hash(tile);
                float t = sin(_Time.y * _BlendSpeed + rnd * 6.283) * 0.5 + 0.5;

                // Random tile color from A to B
                fixed4 tileColor = lerp(_ColorA, _ColorB, t);

                // Get fractional UV inside tile
                float2 tileUV = frac(uv);

                // Distance to tile edge (for border)
                float distX = min(tileUV.x, 1.0 - tileUV.x);
                float distY = min(tileUV.y, 1.0 - tileUV.y);
                float borderDist = min(distX, distY);

                // Border mask: border fully covers tile edges
                float borderMask = smoothstep(_BorderWidth, _BorderWidth + _BorderSoftness, borderDist);

                // Composite: border replaces tile edges fully
                fixed4 final = lerp(_BorderColor, tileColor, borderMask);

                // Use the texture's alpha AND the UI's vertex alpha
                final.a = tex.a * i.color.a; // <-- CHANGED: This is the fix!
                
                return final;
            }
            ENDCG
        }
    }
}