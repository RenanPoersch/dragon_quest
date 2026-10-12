Shader "DragonQuest/ObliqueWorld"
{
    Properties { _PixelAtlas ("Materiais pixel art", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_PixelAtlas); SAMPLER(sampler_PixelAtlas);
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 surface : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; nointerpolation float2 surface : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv; output.surface = input.surface;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if (input.surface.x >= 0)
                {
                    float index = input.surface.x;
                    float2 cell = float2(fmod(index, 4), 2 - floor(index / 4));
                    float2 pixelUV = (floor(frac(input.uv) * 64) + 0.5) / 64;
                    float2 uv = (cell + pixelUV) / float2(4, 3);
                    half3 albedo = SAMPLE_TEXTURE2D_LOD(_PixelAtlas, sampler_PixelAtlas, uv, 0).rgb;
                    if (index == 0)
                    {
                        half3 green = half3(0.29h, 0.37h, 0.23h);
                        #if !defined(UNITY_COLORSPACE_GAMMA)
                        green = SRGBToLinear(green);
                        #endif
                        albedo = lerp(green, albedo, 0.4h);
                    }
                    return half4(albedo * input.surface.y, 1);
                }
                #if defined(UNITY_COLORSPACE_GAMMA)
                return half4(input.color.rgb, 1);
                #else
                return half4(SRGBToLinear(input.color.rgb), 1);
                #endif
            }
            ENDHLSL
        }
    }
}
