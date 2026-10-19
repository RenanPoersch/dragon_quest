Shader "Hidden/DragonQuest/BattleEntrySwirl"
{
    Properties
    {
        _MainTex ("Exploration screen", 2D) = "black" {}
        _Progress ("Vortex progress", Range(0,1)) = 0
        _Aspect ("Screen aspect", Float) = 1.777778
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
            float _Progress, _Aspect;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float t = saturate(_Progress);
                float2 aspect = float2(_Aspect, 1);
                float2 p = (i.uv - .5) * aspect;
                float radius = length(p);
                float outerRadius = .5 * length(aspect);
                float inward = 1 - saturate(radius / outerRadius);
                // The whole picture rotates; its centre winds faster into a tight spiral.
                float angle = t * t * (5.2 + 14 * inward * inward);
                float s, c;
                sincos(angle, s, c);
                float2 source = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                source *= 1 + 3 * t * t;
                source = source / aspect + .5;
                // Mirrored edges keep every corner covered while the image rotates and contracts.
                source = 1 - abs(frac(source * .5) * 2 - 1);
                float3 color = tex2D(_MainTex, source).rgb;
                float closing = smoothstep(.48, 1, t);
                float iris = outerRadius * (1 - closing);
                float feather = outerRadius * .085;
                float visible = 1 - smoothstep(iris, iris + feather, radius);
                color *= visible * (1 - smoothstep(.86, 1, t));
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
