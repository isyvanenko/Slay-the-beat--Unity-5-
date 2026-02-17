Shader "UI/Glitter4ColorCycle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        _Color1 ("Color 1", Color) = (1,0,0,1)
        _Color2 ("Color 2", Color) = (0,1,0,1)
        _Color3 ("Color 3", Color) = (0,0,1,1)
        _Color4 ("Color 4", Color) = (1,1,0,1)

        _CycleSpeed ("Color Cycle Speed", Float) = 1

        _GlitterIntensity ("Glitter Intensity", Range(0,5)) = 1
        _GlitterSize ("Glitter Size", Range(1,200)) = 80
        _GlitterSpeed ("Glitter Speed", Float) = 1

        _MaskSoftnessX ("Mask Softness X", Float) = 0
        _MaskSoftnessY ("Mask Softness Y", Float) = 0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;
            fixed4 _Color4;

            float _CycleSpeed;
            float _GlitterIntensity;
            float _GlitterSize;
            float _GlitterSpeed;

            float4 _ClipRect;
            float _MaskSoftnessX;
            float _MaskSoftnessY;

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
                float4 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = v.vertex;
                return o;
            }

            // Simple hash noise for glitter
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _CycleSpeed;

                // 4 color smooth cycle
                float phase = frac(t / 4.0) * 4.0;

                fixed4 colA, colB;
                float lerpVal;

                if (phase < 1)
                {
                    colA = _Color1;
                    colB = _Color2;
                    lerpVal = phase;
                }
                else if (phase < 2)
                {
                    colA = _Color2;
                    colB = _Color3;
                    lerpVal = phase - 1;
                }
                else if (phase < 3)
                {
                    colA = _Color3;
                    colB = _Color4;
                    lerpVal = phase - 2;
                }
                else
                {
                    colA = _Color4;
                    colB = _Color1;
                    lerpVal = phase - 3;
                }

                fixed4 cycleColor = lerp(colA, colB, lerpVal);

                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;

                // Glitter
                float2 glitterUV = floor(i.uv * _GlitterSize + _Time.y * _GlitterSpeed);
                float sparkle = rand(glitterUV);

                sparkle = step(0.97, sparkle) * _GlitterIntensity;

                fixed4 finalColor = tex * cycleColor;
                finalColor.rgb += sparkle;

                // UI Masking
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}
