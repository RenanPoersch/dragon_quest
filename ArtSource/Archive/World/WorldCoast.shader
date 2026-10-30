Shader "DragonQuest/WorldCoast"
{
    Properties { _ShoreAtlas ("Costa pintada em pixel art", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_ShoreAtlas); SAMPLER(sampler_ShoreAtlas);
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 surface : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 ground : TEXCOORD0; nointerpolation float surface : TEXCOORD1; };
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.ground = input.positionOS.xy;
                output.color = input.color; output.surface = input.surface.x;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Coordinates snap to small world pixels, independently of screen size.
                float2 p = floor(input.ground * 32) / 32;
                float3 color = input.color.rgb;
                if (input.surface > 8.5) // Water: travelling currents and broken wave crests.
                {
                    float t = floor(_Time.y * 12) / 12;
                    float swell = sin(p.y * 7.5 + p.x * 1.4 - t * 1.6
                        + sin(p.x * 2.2 + t * 0.45) * 0.8);
                    float cross = sin(p.x * 4.5 - p.y * 2.1 + t * 0.7);
                    float grain = Hash(floor(float2(p.x - t * 0.12, p.y) * 5));
                    float crest = step(0.91, swell) * step(0.42, grain) * (0.35 + 0.25 * cross);
                    color *= 0.92 + 0.06 * floor((swell + cross * 0.35) * 3) / 3;
                    color = lerp(color, float3(0.49, 0.76, 0.8), crest);
                    float glint = step(0.985, swell) * step(0.78, grain);
                    color = lerp(color, float3(0.72, 0.88, 0.85), glint * 0.35);
                }
                else if (input.surface > 6.5) // Painted rock; the geometry remains completely flat.
                {
                    float2 cell = float2(input.ground.x > 3 && input.ground.y > 0 ? 0.5 : 0, 0.5);
                    float2 uv = cell + 0.001 + frac(input.ground * 0.35) * 0.498;
                    float3 painted = SAMPLE_TEXTURE2D_LOD(_ShoreAtlas, sampler_ShoreAtlas, uv, 0).rgb;
                    #if !defined(UNITY_COLORSPACE_GAMMA)
                    painted = LinearToSRGB(painted);
                    #endif
                    color = painted * (0.8 + input.color.g * 0.3);
                }
                else if (input.surface > 2.5 && input.surface < 3.5) // Painted dry/wet sand.
                {
                    float2 cell = float2(input.color.r > 0.8 ? 0 : 0.5, 0);
                    float2 uv = cell + 0.001 + frac(input.ground * 0.35) * 0.498;
                    color = SAMPLE_TEXTURE2D_LOD(_ShoreAtlas, sampler_ShoreAtlas, uv, 0).rgb;
                    #if !defined(UNITY_COLORSPACE_GAMMA)
                    color = LinearToSRGB(color);
                    #endif
                }
                #if !defined(UNITY_COLORSPACE_GAMMA)
                color = SRGBToLinear(color);
                #endif
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
