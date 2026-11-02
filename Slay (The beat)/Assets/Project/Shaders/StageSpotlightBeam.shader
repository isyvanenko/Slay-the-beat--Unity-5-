Shader "Custom/StageSpotlightBeam"
{
    // Volumetric-looking stage light beam for a Unity Cylinder (height 2, radius 0.5).
    // The lamp sits at the top of the cylinder; the beam widens and fades toward the bottom.
    // Animate "Beam Color" (_BaseColor) from Timeline. Its alpha fades the whole beam.
    Properties
    {
        [MainColor][HDR] _BaseColor ("Beam Color", Color) = (1, 0.85, 0.6, 1)
        _Intensity ("Intensity", Range(0, 10)) = 1.5

        [Header(Shape)]
        _TopWidth ("Width At Lamp", Range(0.05, 1)) = 0.25
        _EdgeSoftness ("Edge Softness", Range(0.5, 8)) = 2.5
        _LengthFalloff ("Length Falloff", Range(0.1, 5)) = 1.2
        _SourceGlow ("Glow At Lamp", Range(0, 5)) = 1

        [Header(Haze Animation)]
        _NoiseScale ("Haze Scale", Range(0.5, 20)) = 4
        _NoiseStrength ("Haze Strength", Range(0, 1)) = 0.5
        _NoiseSpeed ("Haze Speed", Range(0, 3)) = 0.4

        [Header(Flicker)]
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.1
        _FlickerSpeed ("Flicker Speed", Range(0, 30)) = 8
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
            Name "Beam"
            Tags { "LightMode" = "UniversalForward" }

            // Additive: light only brightens what's behind it, like a real beam through haze.
            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
                half _TopWidth;
                half _EdgeSoftness;
                half _LengthFalloff;
                half _SourceGlow;
                half _NoiseScale;
                half _NoiseStrength;
                half _NoiseSpeed;
                half _FlickerStrength;
                half _FlickerSpeed;
            CBUFFER_END

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
                float3 positionOS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            // 0 at the lamp (top of the cylinder), 1 at the far end (bottom).
            float BeamDistance(float yOS) { return saturate(0.5 - yOS * 0.5); }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(Hash(i + float3(0, 0, 0)), Hash(i + float3(1, 0, 0)), f.x),
                         lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                         lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
            }

            float Haze(float3 p)
            {
                float n = ValueNoise(p) * 0.5;
                n += ValueNoise(p * 2.03 + 11.7) * 0.3;
                n += ValueNoise(p * 4.01 + 23.1) * 0.2;
                return n;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Taper the cylinder into a cone: narrow at the lamp, full width at the far end.
                float3 pos = input.positionOS.xyz;
                pos.xz *= lerp(_TopWidth, 1.0, BeamDistance(pos.y));

                VertexPositionInputs positions = GetVertexPositionInputs(pos);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                // Bright through the middle of the beam, fading to nothing at its silhouette.
                float facing = saturate(abs(dot(normalWS, viewDirWS)));
                float edge = pow(facing, _EdgeSoftness);

                // Hide the flat end caps so the beam has no hard disc at either end.
                float sideMask = 1.0 - abs(input.normalOS.y);

                // Fade along the length, with an extra hot spot right at the lamp.
                float dist = BeamDistance(input.positionOS.y);
                float lengthFade = pow(1.0 - dist, _LengthFalloff);
                float glow = 1.0 + _SourceGlow * pow(1.0 - dist, 8.0);

                // Drifting haze/dust moving down the beam.
                float3 hazePos = input.positionOS * _NoiseScale + float3(0, _Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.3);
                float haze = lerp(1.0, Haze(hazePos) * 1.6, _NoiseStrength);

                // Subtle irregular flicker.
                float t = _Time.y * _FlickerSpeed;
                float flicker = 1.0 - _FlickerStrength * (0.5 + 0.25 * sin(t) + 0.25 * sin(t * 2.37 + 1.3));

                float strength = edge * sideMask * lengthFade * glow * haze * flicker * _Intensity * _BaseColor.a;
                half3 color = _BaseColor.rgb * strength;
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
