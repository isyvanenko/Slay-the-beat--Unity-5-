// LED screen look for UI images (RawImage / Image).
// The picture is split into a grid of square cells; each cell becomes one round LED
// lit with the colour at the cell's centre. Everything between the LEDs is transparent.
Shader "UI/LED Screen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _DotCount ("Dots Across", Float) = 160
        _DotSize ("Dot Size", Range(0.05, 1)) = 0.8
        _Softness ("Dot Edge Softness", Range(0.001, 0.5)) = 0.2
        _Brightness ("Brightness", Float) = 1.25
        _Aspect ("Display Aspect (width / height)", Float) = 1.7778
        _UVRect ("Displayed UV Rect", Vector) = (0, 0, 1, 1)

        // Standard UI masking support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _DotCount;
            float _DotSize;
            float _Softness;
            float _Brightness;
            float _Aspect;
            float4 _UVRect;

            v2f vert (appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                // Square cells across the displayed area (independent of any UV cropping).
                float2 grid = float2(_DotCount, _DotCount / max(_Aspect, 0.0001));
                float2 display = (IN.texcoord - _UVRect.xy) / max(_UVRect.zw, 0.0001);
                float2 cell = display * grid;

                // Every LED shows one solid colour: the pixel at its cell centre.
                float2 centre = (floor(cell) + 0.5) / grid;
                float2 sampleUV = _UVRect.xy + centre * _UVRect.zw;
                half4 color = (tex2Dlod(_MainTex, float4(sampleUV, 0, 0)) + _TextureSampleAdd) * IN.color;
                color.rgb *= _Brightness;

                // Round LED with a soft edge; outside it is fully transparent.
                float dist = length(frac(cell) - 0.5) * 2.0;
                float ledMask = 1.0 - smoothstep(_DotSize - _Softness, _DotSize, dist);
                color.a *= ledMask;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
