using DragonQuest.Presentation.World.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Exploration.Layouts
{
    // Duas plantas pequenas sobre pinturas 4:3; UV tem origem no canto superior esquerdo.
    public sealed class CompactShopLayout
    {
        public const float CameraSize = 3.6f, ArtAspect = 4f / 3f;
        public readonly string MapId, Name, Resource;
        public readonly Rect Bounds;
        public readonly Vector2 Spawn, Exit, Merchant, Counter;
        public readonly Vector2[] Floor;
        public readonly Vector2[][] Fixtures, Foregrounds;
        public readonly Vector2[] ForegroundAnchors;
        private static readonly Vector2[] Footprint = { new Vector2(-.04f,-.033f),new Vector2(.04f,-.033f),
            new Vector2(.04f,.033f),new Vector2(-.04f,.033f),Vector2.zero };

        public static CompactShopLayout Items { get; } = new CompactShopLayout(
            "item-shop","Loja de itens","ItemShopInterior",new Vector2(.50f,.755f),new Vector2(.50f,.94f),
            new Vector2(.52f,.405f),new Vector2(.53f,.59f),
            new[] { new Vector2(.16f,.415f),new Vector2(.30f,.415f),new Vector2(.30f,.535f),new Vector2(.79f,.535f),
                new Vector2(.79f,.415f),new Vector2(.83f,.415f),new Vector2(.87f,.675f),new Vector2(.85f,.785f),
                new Vector2(.60f,.785f),new Vector2(.60f,.99f),new Vector2(.40f,.99f),new Vector2(.40f,.785f),
                new Vector2(.18f,.785f),new Vector2(.18f,.68f),new Vector2(.13f,.68f),new Vector2(.13f,.53f) },
            new[] { new Vector2(.305f,.425f),new Vector2(.785f,.425f),new Vector2(.785f,.515f),new Vector2(.305f,.515f) },
            new[] { new Vector2(.300f,.39f),new Vector2(.321f,.343f),new Vector2(.323f,.303f),new Vector2(.345f,.285f),
                new Vector2(.362f,.302f),new Vector2(.38f,.313f),new Vector2(.42f,.319f),new Vector2(.45f,.329f),
                new Vector2(.623f,.329f),new Vector2(.638f,.302f),new Vector2(.654f,.302f),new Vector2(.642f,.35f),
                new Vector2(.72f,.329f),new Vector2(.762f,.315f),new Vector2(.785f,.36f),new Vector2(.79f,.513f),
                new Vector2(.306f,.513f) },new Vector2(.55f,.51f));

        public static CompactShopLayout Materia { get; } = new CompactShopLayout(
            "materia-shop","Loja de materias","MateriaShopInterior",new Vector2(.51f,.75f),new Vector2(.51f,.94f),
            new Vector2(.51f,.415f),new Vector2(.51f,.61f),
            new[] { new Vector2(.23f,.55f),new Vector2(.755f,.55f),new Vector2(.84f,.65f),new Vector2(.84f,.78f),
                new Vector2(.63f,.78f),new Vector2(.63f,.99f),new Vector2(.39f,.99f),new Vector2(.39f,.78f),
                new Vector2(.16f,.78f),new Vector2(.16f,.70f),new Vector2(.23f,.61f) },
            new[] { new Vector2(.28f,.435f),new Vector2(.74f,.435f),new Vector2(.74f,.529f),new Vector2(.28f,.529f) },
            new[] { new Vector2(.273f,.416f),new Vector2(.282f,.346f),new Vector2(.30f,.341f),new Vector2(.306f,.289f),
                new Vector2(.323f,.277f),new Vector2(.334f,.30f),new Vector2(.333f,.348f),new Vector2(.36f,.32f),
                new Vector2(.377f,.35f),new Vector2(.65f,.35f),new Vector2(.655f,.341f),new Vector2(.72f,.343f),
                new Vector2(.742f,.399f),new Vector2(.74f,.53f),new Vector2(.28f,.53f) },new Vector2(.51f,.525f));

        private CompactShopLayout(string id,string name,string resource,Vector2 spawn,Vector2 exit,Vector2 merchant,Vector2 counter,
            Vector2[] floor,Vector2[] fixture,Vector2[] foreground,Vector2 anchor)
        {
            MapId=id; Name=name; Resource=resource; Floor=floor;
            Spawn=Ground(spawn.x,spawn.y); Exit=Ground(exit.x,exit.y); Merchant=Ground(merchant.x,merchant.y); Counter=Ground(counter.x,counter.y);
            Fixtures=new[] { fixture }; Foregrounds=new[] { foreground }; ForegroundAnchors=new[] { anchor };
            Vector2 a=Ground(0,0),b=Ground(0,1),c=Ground(1,0),d=Ground(1,1);
            Bounds=Rect.MinMaxRect(Math.Min(Math.Min(a.x,b.x),Math.Min(c.x,d.x)),Math.Min(Math.Min(a.y,b.y),Math.Min(c.y,d.y)),
                Math.Max(Math.Max(a.x,b.x),Math.Max(c.x,d.x)),Math.Max(Math.Max(a.y,b.y),Math.Max(c.y,d.y)));
        }
        public static CompactShopLayout Find(string id) => id == Items.MapId ? Items : id == Materia.MapId ? Materia : null;
        public Vector2 ScreenPoint(Vector2 image) => new Vector2((image.x-.5f)*CameraSize*2*ArtAspect,(.5f-image.y)*CameraSize*2);
        public Vector2 Ground(float u,float v)
        {
            Vector2 screen=ScreenPoint(new Vector2(u,v));
            return ObliqueProjection.GroundDirection(new Vector2(screen.x,screen.y/ObliqueProjection.GroundScale));
        }
        public Vector2 ImagePoint(Vector2 ground)
        {
            Vector2 screen=ObliqueProjection.ScreenPlane(ground);
            return new Vector2(.5f+screen.x/(CameraSize*2*ArtAspect),.5f-screen.y/(CameraSize*2));
        }
        private static bool Inside(Vector2 p,IReadOnlyList<Vector2> polygon)
        {
            bool inside=false;
            for (int i=0,j=polygon.Count-1;i<polygon.Count;j=i++)
            {
                Vector2 a=polygon[i],b=polygon[j];
                if ((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            return inside;
        }
        public bool IsWalkable(Vector2 ground)
        {
            if (float.IsNaN(ground.x)||float.IsNaN(ground.y)||float.IsInfinity(ground.x)||float.IsInfinity(ground.y)||!Bounds.Contains(ground)) return false;
            Vector2 p=ImagePoint(ground);
            foreach (Vector2 offset in Footprint)
            {
                Vector2 sample=p+offset;
                if (!Inside(sample,Floor)) return false;
                foreach (var fixture in Fixtures) if (Inside(sample,fixture)) return false;
            }
            return Math.Abs(ground.x-Merchant.x)>.56f || Math.Abs(ground.y-Merchant.y)>.62f;
        }
        public Vector2 SafeSavedPosition(Vector2 position) => IsWalkable(position) ? position : Spawn;
        public Vector2 ConstrainMove(Vector2 position,Vector2 delta)
        {
            int steps=Math.Max(1,(int)Math.Ceiling(delta.magnitude/.05f)); Vector2 step=delta/steps;
            for (int i=0;i<steps;i++)
            {
                if (IsWalkable(position+step)) { position+=step; continue; }
                Vector2 screen=ObliqueProjection.ScreenPlane(step);
                Vector2 horizontal=ObliqueProjection.GroundDirection(new Vector2(screen.x,0));
                Vector2 vertical=ObliqueProjection.GroundDirection(new Vector2(0,screen.y/ObliqueProjection.GroundScale));
                if (IsWalkable(position+horizontal)) position+=horizontal;
                if (IsWalkable(position+vertical)) position+=vertical;
            }
            return position;
        }
    }
}
