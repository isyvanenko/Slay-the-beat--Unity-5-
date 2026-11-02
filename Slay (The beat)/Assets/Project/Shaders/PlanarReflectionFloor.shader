Shader "Custom/PlanarReflectionFloor"
{
    // Reflection overlay for a floor that has the PlanarReflection component.
    // Add this as a SECOND material on the floor's renderer, so the floor keeps its own material underneath.
    Properties
    {
        _Tint ("Reflection Tint", Color) = (1, 1, 1, 1)
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.6
        _FresnelAmount ("Stronger At Grazing Angles", Range(0, 1)) = 0.5
        _FresnelPower ("Grazing Falloff", Range(0.5, 8)) = 3

        [Header(Softening)]
        _Blur ("Blur", Range(0, 4)) = 1
        _Distortion ("Ripple Distortion", Range(0, 0.05)) = 0.005
        _DistortionScale ("Ripple Scale", Range(0.1, 20)) = 4
        _DistortionSpeed ("Ripple Speed", Range(0, 3)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "PlanarReflection"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _ReflectionStrength;
                half _FresnelAmount;
                half _FresnelPower;
                half _Blur;
                half _Distortion;
                half _DistortionScale;
                half _DistortionSpeed;
            CBUFFER_END

            // Set globally by the PlanarReflection component.
            TEXTURE2D(_PlanarReflectionTex);
            SAMPLER(sampler_PlanarReflectionTex);
            float4 _PlanarReflectionTex_TexelSize;
            float _PlanarReflectionActive;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
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

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // The reflection camera's image is mirrored horizontally, so flip X back.
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                uv.x = 1.0 - uv.x;

                // Gentle moving ripple, also hides the reduced resolution.
                float2 p = input.positionWS.xz * _DistortionScale + _Time.y * _DistortionSpeed;
                uv += (float2(ValueNoise(p), ValueNoise(p + 7.3)) - 0.5) * _Distortion;

                // Cheap 4-tap blur.
                float2 o = _PlanarReflectionTex_TexelSize.xy * _Blur;
                half3 refl = SAMPLE_TEXTURE2D(_PlanarReflectionTex, sampler_PlanarReflectionTex, uv + float2( o.x,  o.y)).rgb;
                refl += SAMPLE_TEXTURE2D(_PlanarReflectionTex, sampler_PlanarReflectionTex, uv + float2(-o.x,  o.y)).rgb;
                refl += SAMPLE_TEXTURE2D(_PlanarReflectionTex, sampler_PlanarReflectionTex, uv + float2( o.x, -o.y)).rgb;
                refl += SAMPLE_TEXTURE2D(_PlanarReflectionTex, sampler_PlanarReflectionTex, uv + float2(-o.x, -o.y)).rgb;
                refl *= 0.25;

                // Floors reflect more when viewed at a shallow angle.
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDirWS)), _FresnelPower);
                float strength = _ReflectionStrength * lerp(1.0, fresnel, _FresnelAmount) * _Tint.a * _PlanarReflectionActive;

                half3 color = MixFog(refl * _Tint.rgb, input.fogFactor);
                return half4(color, strength);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
