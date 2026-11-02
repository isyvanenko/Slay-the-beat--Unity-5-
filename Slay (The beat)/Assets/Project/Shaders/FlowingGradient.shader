Shader "Custom/FlowingGradient"
{
    // Procedural gradient that flows and warps like water. No textures needed.
    // Every property can be keyed from a Timeline Animation Track.
    // To scrub the motion in Timeline, set Flow Speed to 0 and animate Flow Offset instead.
    Properties
    {
        [Header(Colors)]
        [HDR] _ColorA ("Color A", Color) = (0.02, 0.12, 0.35, 1)
        [HDR] _ColorB ("Color B", Color) = (0.0, 0.55, 0.75, 1)
        [HDR] _ColorC ("Color C", Color) = (0.55, 0.95, 1.0, 1)
        _MidPoint ("Color B Position", Range(0.01, 0.99)) = 0.5
        _Opacity ("Opacity", Range(0, 1)) = 1

        [Header(Gradient)]
        _GradientAngle ("Gradient Angle", Range(0, 360)) = 90
        _NoiseAmount ("Noise vs Straight Gradient", Range(0, 1)) = 0.6
        _Contrast ("Contrast", Range(0.2, 4)) = 1.2

        [Header(Flow)]
        _Scale ("Pattern Scale", Range(0.1, 20)) = 3
        _Warp ("Warp Strength", Range(0, 5)) = 1.5
        _FlowAngle ("Flow Direction", Range(0, 360)) = 45
        _FlowSpeed ("Flow Speed", Range(0, 3)) = 0.3
        _FlowOffset ("Flow Offset (animate for scrubbing)", Float) = 0

        [Header(Water Highlights)]
        [HDR] _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightStrength ("Highlight Strength", Range(0, 3)) = 0.6
        _HighlightDensity ("Highlight Density", Range(1, 20)) = 6
        _HighlightSharpness ("Highlight Sharpness", Range(1, 64)) = 16
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FlowingGradient"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                half4 _ColorC;
                half _MidPoint;
                half _Opacity;
                half _GradientAngle;
                half _NoiseAmount;
                half _Contrast;
                half _Scale;
                half _Warp;
                half _FlowAngle;
                half _FlowSpeed;
                float _FlowOffset;
                half4 _HighlightColor;
                half _HighlightStrength;
                half _HighlightDensity;
                half _HighlightSharpness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float n = 0.0;
                float amp = 0.5;
                for (int o = 0; o < 4; o++)
                {
                    n += ValueNoise(p) * amp;
                    p = p * 2.02 + float2(17.3, 9.1);
                    amp *= 0.5;
                }
                return n / 0.9375;
            }

            float2 AngleToDir(float degrees)
            {
                float r = radians(degrees);
                return float2(cos(r), sin(r));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _FlowSpeed + _FlowOffset;
                float2 flow = AngleToDir(_FlowAngle);
                float2 p = input.uv * _Scale;

                // Domain warping: noise pushes noise around, which gives the liquid swirl.
                float2 q = float2(Fbm(p + flow * t), Fbm(p + float2(5.2, 1.3) - flow * t * 0.7));
                float n = Fbm(p + _Warp * q + flow * t * 0.5);

                // Blend a straight gradient with the flowing noise.
                float g = dot(input.uv - 0.5, AngleToDir(_GradientAngle)) + 0.5;
                float v = lerp(g, n, _NoiseAmount);
                v = saturate((v - 0.5) * _Contrast + 0.5);

                // Three-color ramp: A -> B -> C.
                half4 col = v < _MidPoint
                    ? lerp(_ColorA, _ColorB, v / _MidPoint)
                    : lerp(_ColorB, _ColorC, (v - _MidPoint) / (1.0 - _MidPoint));

                // Thin bright ripple lines, like light caustics on water.
                float lines = 1.0 - abs(sin((n + q.x * 0.5) * _HighlightDensity * PI));
                col.rgb += _HighlightColor.rgb * pow(lines, _HighlightSharpness) * _HighlightStrength * _HighlightColor.a;

                col.rgb = MixFog(col.rgb, input.fogFactor);
                col.a *= _Opacity;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
