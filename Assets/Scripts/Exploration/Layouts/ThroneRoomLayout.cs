using System;
using UnityEngine;

namespace DragonQuest.Exploration.Layouts
{
    // A fisica continua no plano XY. A camera em perspectiva projeta esse piso sobre a pintura aprovada.
    public static class ThroneRoomLayout
    {
        public const float ArtAspect=1672f/941f, FieldOfView=45f, PitchDegrees=-85.26f;
        public static readonly Vector3 CameraPosition=new Vector3(0,-16,-5);
        public static Quaternion CameraRotation => new Quaternion((float)Math.Sin(PitchDegrees*Math.PI/360),0,0,(float)Math.Cos(PitchDegrees*Math.PI/360));
        public static readonly Rect Bounds=new Rect(-10,-8,20,16);
        public static readonly Vector2 Spawn=new Vector2(0,-4.6f), Exit=new Vector2(0,-5.65f);
        public static readonly Vector2 King=new Vector2(0,5), Spirit=new Vector2(-2.1f,4.8f), Audience=new Vector2(0,4);
        public static readonly Vector2[] Floor={ new Vector2(.04f,.99f),new Vector2(.14f,.71f),new Vector2(.35f,.71f),
            new Vector2(.39f,.55f),new Vector2(.61f,.55f),new Vector2(.65f,.71f),new Vector2(.86f,.71f),new Vector2(.96f,.99f) };
        private static readonly Vector2[] Footprint={ new Vector2(-.31f,-.34f),new Vector2(.31f,-.34f),
            new Vector2(.31f,.34f),new Vector2(-.31f,.34f),Vector2.zero };
        private static readonly float Sin=(float)Math.Sin(PitchDegrees*Math.PI/180), Cos=(float)Math.Cos(PitchDegrees*Math.PI/180);
        private static readonly float HalfTan=(float)Math.Tan(FieldOfView*Math.PI/360);

        public static float FloorHeight(Vector2 ground) => 2.5f*Math.Max(0,Math.Min(1,(ground.y-2)/3));
        public static Vector3 WorldPoint(Vector2 ground) => new Vector3(ground.x,ground.y,-FloorHeight(ground));
        public static Vector3 CameraPoint(Vector3 world)
        {
            Vector3 p=world-CameraPosition;
            return new Vector3(p.x,Cos*p.y+Sin*p.z,-Sin*p.y+Cos*p.z);
        }
        public static Vector2 ImagePoint(Vector2 ground)
        {
            Vector3 p=CameraPoint(WorldPoint(ground));
            return new Vector2(.5f+p.x/(2*p.z*HalfTan*ArtAspect),.5f-p.y/(2*p.z*HalfTan));
        }
        public static Vector3 ImagePlane(Vector2 uv,float depth)
        {
            float x=(uv.x-.5f)*2*depth*HalfTan*ArtAspect, y=(.5f-uv.y)*2*depth*HalfTan;
            return CameraPosition+new Vector3(x,Cos*y-Sin*depth,Sin*y+Cos*depth);
        }
        private static bool Inside(Vector2 p)
        {
            bool inside=false;
            for(int i=0,j=Floor.Length-1;i<Floor.Length;j=i++)
            { Vector2 a=Floor[i],b=Floor[j]; if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside; }
            return inside;
        }
        public static bool IsWalkable(Vector2 ground)
        {
            if(float.IsNaN(ground.x)||float.IsNaN(ground.y)||float.IsInfinity(ground.x)||float.IsInfinity(ground.y)||!Bounds.Contains(ground)) return false;
            foreach(Vector2 offset in Footprint)
            {
                Vector2 p=ground+offset;
                if(p.y>4.65f || p.y>2 && Math.Abs(p.x)>2.6f || !Inside(ImagePoint(p))) return false;
            }
            return true;
        }
        public static Vector2 SafeSavedPosition(Vector2 ground) => IsWalkable(ground)?ground:Spawn;
        public static Vector2 ConstrainMove(Vector2 position,Vector2 delta)
        {
            int steps=Math.Max(1,(int)Math.Ceiling(delta.magnitude/.05f)); Vector2 step=delta/steps;
            for(int i=0;i<steps;i++)
            {
                if(IsWalkable(position+step)) { position+=step; continue; }
                Vector2 horizontal=new Vector2(step.x,0),vertical=new Vector2(0,step.y);
                if(IsWalkable(position+horizontal)) position+=horizontal;
                if(IsWalkable(position+vertical)) position+=vertical;
            }
            return position;
        }
    }
}
