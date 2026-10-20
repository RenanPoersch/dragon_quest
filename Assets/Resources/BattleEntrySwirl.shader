Shader "Hidden/DragonQuest/BattleEntrySwirl"
{
    Properties
    {
        _MainTex ("Exploration screen", 2D) = "black" {}
        _Progress ("Vortex progress", Range(0,1)) = 0
        _Aspect ("Screen aspect", Float) = 1.777778
        _WhiteShadows ("Replace dark final-encounter pixels with white", Range(0,1)) = 0
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
            float _Progress, _Aspect, _WhiteShadows;

            float3 FlowSample(float2 radialPosition, float angle, float2 aspect)
            {
                float s, c;
                sincos(angle, s, c);
                float2 source = float2(c * radialPosition.x - s * radialPosition.y, s * radialPosition.x + c * radialPosition.y) / aspect + .5;
                // Keep the viewport filled without scaling the image toward the centre.
                source = 1 - abs(frac(source * .5) * 2 - 1);
                float3 color = tex2D(_MainTex, source).rgb;
                if (_WhiteShadows > 0)
                {
                    float3 displayColor = color;
                    #if !defined(UNITY_COLORSPACE_GAMMA)
                        displayColor = LinearToGammaSpace(color);
                    #endif
                    float dark = 1 - smoothstep(.26, .44, max(displayColor.r, max(displayColor.g, displayColor.b)));
                    // Replace the source blacks before they spread through the liquid blend.
                    // This is enabled only while the final encounter's overlay is displayed.
                    color = lerp(color, float3(1, 1, 1), _WhiteShadows * dark);
                }
                return color;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float t = saturate(_Progress);
                float flow = t * t * (3 - 2 * t);
                float2 aspect = float2(_Aspect, 1);
                float2 p = (i.uv - .5) * aspect;
                float radius = length(p);
                float inward = 1 - saturate(radius / (.5 * length(aspect)));
                float polar = atan2(p.y, p.x);
                // Uneven angular currents stretch the picture into soft, liquid spiral bands.
                // Every sample stays at the same radius: the screen never contracts or drips.
                float current = .6 * sin(radius * 10 - flow * 3.5 + .35 * sin(polar * 3 + flow * 2));
                current += .25 * sin(radius * 22 - flow * 5.5 + polar * 2);
                float angle = flow * (4 + 12 * inward * inward)
                    + sin(t * UNITY_PI) * current * (.35 + .65 * inward);
                float softness = .14 * smoothstep(.15, .85, t);
                // Blend along the circulating flow so details dissolve into the vortex itself.
                float3 color = FlowSample(p, angle, aspect) * .45;
                color += FlowSample(p, angle - softness * .3, aspect) * .25;
                color += FlowSample(p, angle - softness * .6, aspect) * .15;
                color += FlowSample(p, angle - softness * .9, aspect) * .10;
                color += FlowSample(p, angle - softness * 1.2, aspect) * .05;
                // The last battle passes through white; other encounters keep their black fade.
                color = lerp(color, float3(1, 1, 1) * _WhiteShadows, smoothstep(.82, 1, t));
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
