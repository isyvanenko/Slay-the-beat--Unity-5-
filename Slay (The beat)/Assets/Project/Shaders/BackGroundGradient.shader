Shader "Unlit/BackGroundGradient"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1, 0, 1, 1) // Neon Pink
        _Color2 ("Color 2", Color) = (0, 1, 1, 1) // Neon Cyan
        _Color3 ("Color 3", Color) = (1, 1, 0, 1) // Neon Yellow
        _Color4 ("Color 4", Color) = (0.5, 0, 0.5, 1) // Neon Purple
        _Speed ("Speed", Float) = 1.0
    }
    SubShader
    {
        // Tags tell Unity how and when to render this shader
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            // Start of the HLSL code block
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Include Unity's core shader library
            #include "UnityCG.cginc"

            // --- Structures ---
            
            // Input data for the vertex shader
            struct appdata
            {
                float4 vertex : POSITION; // Vertex position
                float2 uv : TEXCOORD0;     // UV (texture) coordinates
            };

            // Data passed from the vertex to the fragment shader
            struct v2f
            {
                float2 uv : TEXCOORD0;     // Pass UVs to the fragment shader
                float4 vertex : SV_POSITION; // Pass clip-space vertex position
            };

            // --- Properties (matched from above) ---
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;
            fixed4 _Color4;
            float _Speed;

            // --- Vertex Shader (vert) ---
            // This shader simply transforms vertices to clip space
            // and passes the UV coordinates to the fragment shader.
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            // --- Fragment Shader (frag) ---
            // This is where the color calculation happens for every pixel.
            fixed4 frag (v2f i) : SV_Target
            {
                // Get the current time, scaled by our speed property
                // _Time.y is a built-in Unity variable for time
                float t = _Time.y * _Speed;

                // Create two separate, moving patterns (noise) using sine waves.
                // We use UVs and time to make the values change across the screen and over time.
                // The (sin(...) * 0.5 + 0.5) trick maps the -1..1 range of sin() to the 0..1 range needed for lerp.
                float noise1 = (sin(i.uv.x * 3.0 + t) + cos(i.uv.y * 4.0 - t)) * 0.5;
                float noise2 = (sin(i.uv.y * 2.0 - t) + cos(i.uv.x * 5.0 + t)) * 0.5;

                // 1. Blend Color 1 and Color 2 using the first noise pattern
                fixed4 colorA = lerp(_Color1, _Color2, (noise1 * 0.5 + 0.5));
                
                // 2. Blend Color 3 and Color 4 using the second noise pattern
                fixed4 colorB = lerp(_Color3, _Color4, (noise2 * 0.5 + 0.5));

                // 3. Blend the two resulting colors (colorA and colorB) together.
                // We'll use another moving sine wave based on the x-coordinate and time for the final blend.
                float finalBlend = sin(i.uv.x * 5.0 + t) * 0.5 + 0.5;
                fixed4 finalColor = lerp(colorA, colorB, finalBlend);

                // Return the final calculated color
                return finalColor;
            }
            ENDCG
        }
    }
}