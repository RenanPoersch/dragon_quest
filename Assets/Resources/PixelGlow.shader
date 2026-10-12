Shader "DragonQuest/PixelGlow"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite Off ZTest LEqual
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                return input.color;
                #else
                return half4(SRGBToLinear(input.color.rgb), input.color.a);
                #endif
            }
            ENDHLSL
        }
    }
}
