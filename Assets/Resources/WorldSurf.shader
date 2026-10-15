Shader "DragonQuest/WorldSurf"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite Off ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 ground : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float phase = _Time.y * 1.3 + input.color.a * 6;
                float4 p = input.positionOS;
                p.xy *= 1 + floor(sin(phase) * 6) * 0.002;
                output.positionCS = TransformObjectToHClip(p.xyz);
                output.ground = p.xy;
                output.color = half4(input.color.rgb, 0.5 + 0.35 * sin(phase));
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 pixel = floor(input.ground * 18);
                float breaks = frac(sin(dot(pixel, float2(12.9898, 78.233))) * 43758.5453);
                half3 color = input.color.rgb;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                color = SRGBToLinear(color);
                #endif
                return half4(color, input.color.a * step(0.18, breaks));
            }
            ENDHLSL
        }
    }
}
