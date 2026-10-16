Shader "Hidden/DragonQuest/PossessedThroneComposite"
{
    Properties
    {
        _MainTex ("Throne", 2D) = "black" {}
        _SpiritTex ("Spirit silhouette", 2D) = "black" {}
        _Lightning ("Lightning", Float) = 0
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
            sampler2D _MainTex, _SpiritTex;
            float _Lightning;
            float segment(float2 p, float2 a, float2 b)
            {
                float2 d = b-a;
                return length(p-a-d*saturate(dot(p-a,d)/dot(d,d)));
            }
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 room = tex2D(_MainTex, i.uv);
                if (_Lightning <= 0) return room;
                // The original painting's black recess acts as a matte: gold, skin,
                // cloak and columns occlude the apparition without modifying the PNG.
                float brightness = max(room.r, max(room.g, room.b));
                float recess = (1-smoothstep(.001,.008,brightness)) * smoothstep(.52,.56,i.uv.y);
                // Protect the entire seated figure, including truly black pupils and
                // cloth folds. Color keying alone would also illuminate these shadows.
                float fromTop = 1-i.uv.y;
                float foregroundHalfWidth = fromTop < .27 ? .085 : .13;
                float foreground = step(.195,fromTop)*step(fromTop,.55)*step(abs(i.uv.x-.5),foregroundHalfWidth);
                recess *= 1-foreground;
                float2 spiritUV = (i.uv-float2(.14,.49))/float2(.72,.51);
                float inside = step(0,spiritUV.x)*step(spiritUV.x,1)*step(0,spiritUV.y)*step(spiritUV.y,1);
                fixed4 spirit = tex2D(_SpiritTex, saturate(spiritUV));
                float silhouette = spirit.a * inside * recess;
                float detail = dot(spirit.rgb,float3(.2,.4,.4));
                room.rgb += _Lightning * (silhouette * lerp(float3(.08,.09,.14),float3(.20,.24,.34),detail)
                    + recess * float3(.012,.016,.028));
                float2 p = i.uv;
                float bolt = min(segment(p,float2(.76,1),float2(.744,.974)), segment(p,float2(.744,.974),float2(.756,.963)));
                bolt = min(bolt,segment(p,float2(.756,.963),float2(.728,.932)));
                bolt = min(bolt,segment(p,float2(.728,.932),float2(.741,.917)));
                bolt = min(bolt,segment(p,float2(.741,.917),float2(.707,.879)));
                bolt = min(bolt,segment(p,float2(.707,.879),float2(.722,.863)));
                bolt = min(bolt,segment(p,float2(.722,.863),float2(.691,.817)));
                bolt = min(bolt,segment(p,float2(.691,.817),float2(.700,.799)));
                bolt = min(bolt,segment(p,float2(.700,.799),float2(.673,.754)));
                bolt = min(bolt,segment(p,float2(.728,.932),float2(.762,.909)));
                bolt = min(bolt,segment(p,float2(.762,.909),float2(.752,.891)));
                room.rgb += recess * _Lightning * (1-smoothstep(.0008,.0025,bolt))*float3(.48,.57,.75);
                return room;
            }
            ENDCG
        }
    }
    Fallback Off
}
