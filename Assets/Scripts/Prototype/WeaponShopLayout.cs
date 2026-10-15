using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class WeaponShopLayout
    {
        public const string MapId = "equipment-shop";
        public const float CameraSize = 5f, ArtAspect = 1672f / 941f;
        public static readonly Rect Bounds = GroundBounds(new Rect(0,0,1,1));
        public static readonly Vector2 Spawn = Ground(.535f,.80f), Exit = Ground(.535f,.95f);
        public static readonly Vector2 Merchant = Ground(.635f,.38f), Counter = Ground(.565f,.585f);
        public static readonly Vector2[] Floor = {
            new Vector2(.14f,.51f),new Vector2(.255f,.51f),new Vector2(.255f,.39f),new Vector2(.345f,.39f),
            new Vector2(.345f,.55f),new Vector2(.80f,.55f),new Vector2(.84f,.65f),new Vector2(.895f,.79f),
            new Vector2(.915f,.835f),new Vector2(.65f,.835f),new Vector2(.65f,.985f),new Vector2(.415f,.985f),
            new Vector2(.415f,.835f),new Vector2(.07f,.835f),new Vector2(.07f,.80f),new Vector2(.215f,.80f),
            new Vector2(.215f,.655f),new Vector2(.14f,.655f)
        };
        public static readonly Vector2[][] Fixtures = {
            new[] { new Vector2(.355f,.435f),new Vector2(.768f,.435f),new Vector2(.768f,.519f),new Vector2(.355f,.519f) },
            new[] { new Vector2(.15f,.40f),new Vector2(.24f,.40f),new Vector2(.24f,.49f),new Vector2(.15f,.49f) },
            new[] { new Vector2(.091f,.70f),new Vector2(.20f,.70f),new Vector2(.20f,.775f),new Vector2(.091f,.775f) }
        };
        private static readonly Vector2[] Footprint = { new Vector2(-.022f,-.024f),new Vector2(.022f,-.024f),
            new Vector2(.022f,.024f),new Vector2(-.022f,.024f),Vector2.zero };

        public static Vector2 ScreenPoint(Vector2 image) => new Vector2((image.x - .5f) * CameraSize * 2 * ArtAspect, (.5f - image.y) * CameraSize * 2);
        public static Vector2 Ground(float u, float v)
        {
            Vector2 screen = ScreenPoint(new Vector2(u, v));
            return ObliqueProjection.GroundDirection(new Vector2(screen.x, screen.y / ObliqueProjection.GroundScale));
        }
        public static Vector2 ImagePoint(Vector2 ground)
        {
            Vector2 screen = ObliqueProjection.ScreenPlane(ground);
            return new Vector2(.5f + screen.x / (CameraSize * 2 * ArtAspect), .5f - screen.y / (CameraSize * 2));
        }
        private static Rect GroundBounds(Rect image)
        {
            Vector2 a = Ground(image.xMin,image.yMin), b = Ground(image.xMax,image.yMin);
            Vector2 c = Ground(image.xMax,image.yMax), d = Ground(image.xMin,image.yMax);
            return Rect.MinMaxRect(Math.Min(Math.Min(a.x,b.x),Math.Min(c.x,d.x)),Math.Min(Math.Min(a.y,b.y),Math.Min(c.y,d.y)),
                Math.Max(Math.Max(a.x,b.x),Math.Max(c.x,d.x)),Math.Max(Math.Max(a.y,b.y),Math.Max(c.y,d.y)));
        }
        public static bool Inside(Vector2 p, IReadOnlyList<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside = !inside;
            }
            return inside;
        }
        public static bool IsWalkable(Vector2 ground)
        {
            if (float.IsNaN(ground.x) || float.IsNaN(ground.y) || float.IsInfinity(ground.x) || float.IsInfinity(ground.y) || !Bounds.Contains(ground)) return false;
            Vector2 p = ImagePoint(ground);
            // Projecao da metade do collider do heroi e uma pequena folga.
            foreach (Vector2 offset in Footprint)
            {
                Vector2 sample = p + offset;
                if (!Inside(sample, Floor)) return false;
                foreach (var fixture in Fixtures) if (Inside(sample, fixture)) return false;
            }
            return Math.Abs(ground.x-Merchant.x) > .56f || Math.Abs(ground.y-Merchant.y) > .62f;
        }
        public static Vector2 SafeSavedPosition(Vector2 position) => IsWalkable(position) ? position : Spawn;
        public static Vector2 ConstrainMove(Vector2 position, Vector2 delta)
        {
            int steps = Math.Max(1,(int)Math.Ceiling(delta.magnitude / .05f));
            Vector2 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                if (IsWalkable(position + step)) { position += step; continue; }
                Vector2 screen = ObliqueProjection.ScreenPlane(step);
                Vector2 horizontal = ObliqueProjection.GroundDirection(new Vector2(screen.x,0));
                Vector2 vertical = ObliqueProjection.GroundDirection(new Vector2(0,screen.y/ObliqueProjection.GroundScale));
                if (IsWalkable(position + horizontal)) position += horizontal;
                if (IsWalkable(position + vertical)) position += vertical;
            }
            return position;
        }
    }
}
