Shader "Hidden/DragonQuest/EndingArtworkMotion"
{
    Properties
    {
        _MainTex ("Original artwork", 2D) = "black" {}
        _ArtRect ("Artwork bounds in viewport coordinates", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _ArtRect, _MainTex_TexelSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = (float2(i.uv.x, 1-i.uv.y) - _ArtRect.xy) / _ArtRect.zw;
                if (any(uv < 0) || any(uv > 1)) return fixed4(0,0,0,0);
                uv.y = 1-uv.y;
                // Interpolate original texels explicitly: shared pixel-art resources
                // retain Point filtering, and non-readable imports need no CPU copy.
                float2 pixel = uv * _MainTex_TexelSize.zw - .5;
                float2 blend = frac(pixel);
                float2 origin = (floor(pixel) + .5) * _MainTex_TexelSize.xy;
                float2 texel = _MainTex_TexelSize.xy;
                fixed4 bottom = lerp(tex2D(_MainTex, origin),
                    tex2D(_MainTex, origin + float2(texel.x,0)), blend.x);
                fixed4 top = lerp(tex2D(_MainTex, origin + float2(0,texel.y)),
                    tex2D(_MainTex, origin + texel), blend.x);
                return lerp(bottom, top, blend.y);
            }
            ENDCG
        }
    }
    Fallback Off
}
