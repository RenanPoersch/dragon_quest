using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Planta compacta 4:3 com a mesma metragem das lojas de itens e materias. UV tem origem no alto.
    public static class WeaponShopLayout
    {
        public const string MapId = "equipment-shop";
        public const float CameraSize = 3.6f, ArtAspect = 4f/3f;
        public static readonly Rect Bounds = GroundBounds(new Rect(0,0,1,1));
        public static readonly Rect LegacyBounds = GroundBounds(new Rect(0,0,1,1),5f,1672f/941f);
        public static readonly Vector2 Spawn = Ground(.50f,.75f), Exit = Ground(.50f,.94f);
        public static readonly Vector2 Merchant = Ground(0.55f,0.39f), Counter = Ground(0.56f,0.59f);
        public static readonly Vector2[] Floor = new[] { new Vector2(0.16f,0.53f),new Vector2(0.34f,0.53f),new Vector2(0.34f,0.55f),new Vector2(0.8f,0.55f),new Vector2(0.85f,0.65f),new Vector2(0.84f,0.785f),new Vector2(0.6f,0.785f),new Vector2(0.6f,0.99f),new Vector2(0.4f,0.99f),new Vector2(0.4f,0.785f),new Vector2(0.24f,0.785f),new Vector2(0.24f,0.68f),new Vector2(0.16f,0.68f) };
        public static readonly Vector2[][] Fixtures = {
            new[] { new Vector2(0.345f,0.4f),new Vector2(0.79f,0.4f),new Vector2(0.79f,0.515f),new Vector2(0.345f,0.515f) },
            new[] { new Vector2(0.14f,0.425f),new Vector2(0.235f,0.425f),new Vector2(0.235f,0.505f),new Vector2(0.14f,0.505f) },
            new[] { new Vector2(0.1f,0.705f),new Vector2(0.22f,0.705f),new Vector2(0.22f,0.78f),new Vector2(0.1f,0.78f) }
        };
        private static readonly Vector2[] Footprint = { new Vector2(-.04f,-.033f),new Vector2(.04f,-.033f),
            new Vector2(.04f,.033f),new Vector2(-.04f,.033f),Vector2.zero };

        public static Vector2 ScreenPoint(Vector2 image) => new Vector2((image.x-.5f)*CameraSize*2*ArtAspect,(.5f-image.y)*CameraSize*2);
        public static Vector2 Ground(float u,float v) => Ground(u,v,CameraSize,ArtAspect);
        private static Vector2 Ground(float u,float v,float halfHeight,float aspect)
            => ObliqueProjection.GroundDirection(new Vector2((u-.5f)*halfHeight*2*aspect,(.5f-v)*halfHeight*2/ObliqueProjection.GroundScale));
        public static Vector2 ImagePoint(Vector2 ground)
        {
            Vector2 screen=ObliqueProjection.ScreenPlane(ground);
            return new Vector2(.5f+screen.x/(CameraSize*2*ArtAspect),.5f-screen.y/(CameraSize*2));
        }
        private static Rect GroundBounds(Rect image,float halfHeight=CameraSize,float aspect=ArtAspect)
        {
            Vector2 a=Ground(image.xMin,image.yMin,halfHeight,aspect),b=Ground(image.xMax,image.yMin,halfHeight,aspect);
            Vector2 c=Ground(image.xMax,image.yMax,halfHeight,aspect),d=Ground(image.xMin,image.yMax,halfHeight,aspect);
            return Rect.MinMaxRect(Math.Min(Math.Min(a.x,b.x),Math.Min(c.x,d.x)),Math.Min(Math.Min(a.y,b.y),Math.Min(c.y,d.y)),
                Math.Max(Math.Max(a.x,b.x),Math.Max(c.x,d.x)),Math.Max(Math.Max(a.y,b.y),Math.Max(c.y,d.y)));
        }
        public static bool Inside(Vector2 p,IReadOnlyList<Vector2> polygon)
        {
            bool inside=false;
            for(int i=0,j=polygon.Count-1;i<polygon.Count;j=i++)
            { Vector2 a=polygon[i],b=polygon[j]; if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside; }
            return inside;
        }
        public static bool IsWalkable(Vector2 ground)
        {
            if(float.IsNaN(ground.x)||float.IsNaN(ground.y)||float.IsInfinity(ground.x)||float.IsInfinity(ground.y)||!Bounds.Contains(ground)) return false;
            Vector2 p=ImagePoint(ground);
            foreach(Vector2 offset in Footprint)
            {
                Vector2 sample=p+offset;
                if(!Inside(sample,Floor)) return false;
                foreach(var fixture in Fixtures) if(Inside(sample,fixture)) return false;
            }
            return Math.Abs(ground.x-Merchant.x)>.56f||Math.Abs(ground.y-Merchant.y)>.62f;
        }
        public static Vector2 SafeSavedPosition(Vector2 position) => IsWalkable(position)?position:Spawn;
        public static Vector2 ConstrainMove(Vector2 position,Vector2 delta)
        {
            int steps=Math.Max(1,(int)Math.Ceiling(delta.magnitude/.05f));Vector2 step=delta/steps;
            for(int i=0;i<steps;i++)
            {
                if(IsWalkable(position+step)){position+=step;continue;}
                Vector2 screen=ObliqueProjection.ScreenPlane(step);
                Vector2 horizontal=ObliqueProjection.GroundDirection(new Vector2(screen.x,0));
                Vector2 vertical=ObliqueProjection.GroundDirection(new Vector2(0,screen.y/ObliqueProjection.GroundScale));
                if(IsWalkable(position+horizontal))position+=horizontal;
                if(IsWalkable(position+vertical))position+=vertical;
            }
            return position;
        }
    }
}
