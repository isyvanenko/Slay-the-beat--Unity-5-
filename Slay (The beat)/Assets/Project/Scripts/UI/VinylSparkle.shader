Shader "Sprites/VinylSparkle"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0
        
        // Sparkle properties
        _SparkleColor("Sparkle Color", Color) = (1,1,1,1)
        _SparkleIntensity("Sparkle Intensity", Range(0, 5)) = 2.0
        _SparkleWidth("Sparkle Width", Range(0.01, 0.5)) = 0.1
        _SparkleSpeed("Sparkle Speed", Range(0, 5)) = 1.0
        _LineCount("Line Count", Range(1, 4)) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _SparkleColor;
            float _SparkleIntensity;
            float _SparkleWidth;
            float _SparkleSpeed;
            float _LineCount;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            // Function to calculate line intensity from a corner
            float cornerLine(float2 uv, float2 corner, float time, float offset)
            {
                // Direction from corner to current pixel
                float2 dir = uv - corner;
                
                // Direction from corner to opposite corner
                float2 cornerDir = normalize(float2(1 - 2*corner.x, 1 - 2*corner.y));
                
                // Project onto diagonal direction
                float projection = dot(dir, cornerDir);
                
                // Distance perpendicular to the line
                float2 perpDir = float2(-cornerDir.y, cornerDir.x);
                float perpDist = abs(dot(dir, perpDir));
                
                // Moving line position
                float movingPos = frac(projection * 2 - time * _SparkleSpeed + offset);
                
                // Create sparkle line with smooth falloff
                float sparkleAmount = 1 - smoothstep(0, _SparkleWidth * 2, abs(perpDist));
                float intensity = sparkleAmount * (1 - abs(movingPos * 2 - 1)) * 2; // Pulse as it moves
                
                return intensity;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Only apply sparkle effect on non-transparent areas
                if (c.a > 0)
                {
                    float time = _Time.y;
                    float totalIntensity = 0;
                    
                    // Define the four corners
                    float2 corners[4] = {
                        float2(0, 0), // Bottom-left
                        float2(1, 0), // Bottom-right
                        float2(0, 1), // Top-left
                        float2(1, 1)  // Top-right
                    };
                    
                    // Calculate lines from each corner
                    int cornerCount = min(4, (int)_LineCount);
                    
                    if (_LineCount == 2)
                    {
                        // Opposite corners
                        totalIntensity = cornerLine(IN.texcoord, corners[0], time, 0) + 
                                        cornerLine(IN.texcoord, corners[3], time, 0.5);
                    }
                    else if (_LineCount == 3)
                    {
                        // Three corners
                        totalIntensity = cornerLine(IN.texcoord, corners[0], time, 0) + 
                                        cornerLine(IN.texcoord, corners[1], time, 0.33) +
                                        cornerLine(IN.texcoord, corners[2], time, 0.66);
                    }
                    else if (_LineCount == 4)
                    {
                        // All four corners
                        for (int i = 0; i < 4; i++)
                        {
                            float offset = i * 0.25;
                            totalIntensity += cornerLine(IN.texcoord, corners[i], time, offset);
                        }
                    }
                    else
                    {
                        // Single corner (bottom-left)
                        totalIntensity = cornerLine(IN.texcoord, corners[0], time, 0);
                    }
                    
                    // Apply sparkle effect
                    float3 sparkleEffect = _SparkleColor.rgb * _SparkleIntensity * totalIntensity;
                    
                    // Blend sparkle with original color
                    c.rgb += sparkleEffect * c.a;
                }
                
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}