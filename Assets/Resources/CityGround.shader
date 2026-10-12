Shader "DragonQuest/CityGround"
{
    Properties { _MainTex ("Pintura unica do chao", 2D) = "white" {} }
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
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // UV absoluto da planta: sem repetir, dividir atlas ou quantizar uma celula de material.
                half3 albedo = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, input.uv, 0).rgb;
                return half4(albedo, 1);
            }
            ENDHLSL
        }
    }
}
