Shader "Hidden/DragonQuest/CastleImpactComposite"
{
    Properties
    {
        _MainTex ("Storm over the intact castle", 2D) = "black" {}
        _RedCloudTex ("Painted crimson clouds only", 2D) = "black" {}
        _MeteorTex ("Meteor with transparent fire", 2D) = "black" {}
        _MeteorRect ("Meteor in picture coordinates", Vector) = (0,0,1,1)
        _CinematicTime ("Unscaled clock", Float) = 0
        _MeteorOpacity ("Emergence through the clouds", Range(0,1)) = 0
        _CameraZoom ("Slow camera push", Float) = 1
        _CropY ("Aspect preserving sky crop", Float) = 1
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
            sampler2D _MainTex, _MeteorTex, _RedCloudTex;
            float4 _MeteorRect;
            float _CinematicTime, _MeteorOpacity, _CameraZoom, _CropY;
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Rain(float2 uv, float density, float speed)
            {
                float2 p = float2(uv.x*1.94 + uv.y*.13, uv.y + _CinematicTime*speed)*density;
                float2 cell = floor(p), local = frac(p);
                float seed = Hash(cell);
                float streak = (1-smoothstep(.01,.045,abs(local.x-lerp(.12,.88,seed))));
                return streak*(1-smoothstep(.03,.26,local.y))*step(.52,seed);
            }
            fixed4 frag(v2f_img i) : SV_Target
            {
                float t = _CinematicTime;
                float2 scene = (i.uv-.5)/_CameraZoom+.5;
                float2 source = float2(scene.x,(scene.y-.5)*_CropY+.5);
                // Drift only the upper-left cloud bank; masonry and towers stay rigid.
                float cloud = smoothstep(.48,.83,source.y)*(1-smoothstep(.55,.73,source.x));
                source.x += cloud*sin(source.y*9+t*.12)*.0028;
                source.y += cloud*sin(source.x*13+t*.09)*.0015;
                float3 color = tex2D(_MainTex,source).rgb;
                // Replace only crimson cloud pixels from the sky variant. Cool stone/water and
                // original warm windows remain the original painting, including above the horizon.
                float3 redCloud = tex2D(_RedCloudTex,source).rgb;
                float crimson = (redCloud.r-max(redCloud.g,redCloud.b))/max(.0001,redCloud.r);
                float originalWarmth = (color.r-max(color.g,color.b))/max(.0001,color.r);
                float cloudMask = smoothstep(.45,.65,crimson)
                    * (1-smoothstep(.22,.42,originalWarmth)) * smoothstep(.40,.52,source.y);
                color = lerp(color,redCloud,_MeteorOpacity*cloudMask);
                float thunder = .10*exp(-pow((t-1.9)/.10,2))+.06*exp(-pow((t-2.2)/.15,2));
                color += thunder*float3(.55,.66,.85);
                float rain = Rain(scene,75,.31)*.045+Rain(scene,115,.21)*.025;
                color += rain*float3(.55,.66,.78);

                float2 uv = (scene-_MeteorRect.xy)/_MeteorRect.zw;
                float inside = step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
                // Keep the dense boulder stable while the tail's flame wisps flow upstream.
                float tail = smoothstep(.31,.66,uv.y)*(1-smoothstep(.48,.84,uv.x));
                float2 flameUV = uv + tail*float2(sin(uv.y*36-t*4.7),sin(uv.x*29-t*3.9))*.005;
                float4 meteor = tex2D(_MeteorTex,saturate(flameUV));
                float brightness = max(meteor.r,max(meteor.g,meteor.b));
                meteor.rgb *= 1+tail*.10*sin(t*9+uv.y*18)*smoothstep(.3,.8,brightness);
                color = lerp(color,meteor.rgb,saturate(meteor.a*inside*_MeteorOpacity));
                return fixed4(color,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
