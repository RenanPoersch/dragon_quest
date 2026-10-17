Shader "Hidden/DragonQuest/PossessedThroneComposite"
{
    Properties
    {
        _MainTex ("Throne", 2D) = "black" {}
        _SpiritTex ("Spirit silhouette", 2D) = "black" {}
        _LightningTex ("Lightning animation frame", 2D) = "black" {}
        _Lightning ("Lightning", Float) = 0
        _SkyPadding ("Black space above the painting", Float) = .18
        _SpiritRect ("Frontal spirit in room coordinates", Vector) = (.22,.49,.56,.56)
        _SpiritOpacity ("Natural spirit opacity", Range(0,1)) = .76
        _LightningRect ("Lightning center and size", Vector) = (.74,.92,.20,.24)
        _LightningAngle ("Lightning angle in radians", Float) = 0
        _LightningMirror ("Lightning horizontal direction", Float) = 1
        _RoomAspect ("Source art aspect", Float) = .6666667
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
            sampler2D _MainTex, _SpiritTex, _LightningTex;
            float _Lightning, _SkyPadding, _SpiritOpacity, _LightningAngle, _LightningMirror, _RoomAspect;
            float4 _SpiritRect, _LightningRect;
            fixed4 frag(v2f_img i) : SV_Target
            {
                // Extend the black recess above the original art without changing its pixels.
                float2 roomUV = float2(i.uv.x, i.uv.y * (1 + _SkyPadding));
                fixed4 room = roomUV.y <= 1 ? tex2D(_MainTex, roomUV) : fixed4(0,0,0,1);
                room.rgb *= 1-smoothstep(.94,1,roomUV.y);
                if (_Lightning <= 0) return room;
                // The original painting's black recess acts as a matte: gold, skin,
                // cloak and columns occlude the apparition without modifying the PNG.
                float brightness = max(room.r, max(room.g, room.b));
                float recess = (1-smoothstep(.014,.055,brightness)) * smoothstep(.52,.56,roomUV.y);
                // Protect pupils and central cloth folds, which also contain pure black.
                float fromTop = 1-roomUV.y;
                float foregroundHalfWidth = fromTop < .27 ? .085 : .13;
                float foreground = step(.195,fromTop)*step(fromTop,.55)*step(abs(roomUV.x-.5),foregroundHalfWidth);
                recess *= 1-foreground;
                float2 offset=(roomUV-_LightningRect.xy)*float2(_RoomAspect,1);
                float sine=sin(_LightningAngle),cosine=cos(_LightningAngle);
                float2 rotated=float2(cosine*offset.x+sine*offset.y,-sine*offset.x+cosine*offset.y);
                float2 boltUV = rotated/(_LightningRect.zw*float2(_RoomAspect,1))+.5;
                boltUV.x=.5+(boltUV.x-.5)*_LightningMirror;
                float boltInside = step(0,boltUV.x)*step(boltUV.x,1)*step(0,boltUV.y)*step(boltUV.y,1);
                fixed4 bolt = tex2D(_LightningTex,saturate(boltUV));
                room.rgb += recess * _Lightning * boltInside * pow(bolt.a,1.5) * bolt.rgb * .85;
                float2 spiritUV = (roomUV-_SpiritRect.xy)/_SpiritRect.zw;
                float inside = step(0,spiritUV.x)*step(spiritUV.x,1)*step(0,spiritUV.y)*step(spiritUV.y,1);
                fixed4 spirit = tex2D(_SpiritTex, saturate(spiritUV));
                // Preserve the original ivory/cyan/cobalt RGB, with only a slight
                // transparency. Composite over the ray so it stays behind the spirit.
                float opacity=spirit.a*inside*recess*_SpiritOpacity*_Lightning;
                room.rgb=lerp(room.rgb,spirit.rgb,saturate(opacity));
                return room;
            }
            ENDCG
        }
    }
    Fallback Off
}
