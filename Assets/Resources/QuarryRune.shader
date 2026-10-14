Shader "DragonQuest/QuarryRune"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pixel rune", 2D) = "white" {}
        _Tint ("Stone or awakened color", Color) = (1,1,1,1)
        _Desaturate ("Engraved stone", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Desaturate;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 pixel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(pixel.a - .5h);
                half stone = dot(pixel.rgb, half3(.2126h,.7152h,.0722h));
                return half4(lerp(pixel.rgb,stone.xxx,saturate(_Desaturate)) * _Tint.rgb, 1);
            }
            ENDHLSL
        }
    }
}
