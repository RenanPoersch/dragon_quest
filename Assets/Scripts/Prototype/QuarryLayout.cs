using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // U goes left to right; V goes top to bottom in the continuous quarry painting.
    public static class QuarryLayout
    {
        public static readonly Rect Bounds = new Rect(-18, -18, 36, 30);
        public static readonly Vector2 Spawn = Ground(.50f, .88f);
        public static readonly Vector2 Worker = Ground(.59f, .77f);
        public static readonly Vector2 Inscription = Ground(.405f, .825f);
        private static readonly Rect ExitImage = new Rect(.44f, .955f, .13f, .08f);
        private static readonly Rect ExitGround = GroundBounds(ExitImage);
        public static readonly Vector2 Exit = ExitGround.center;
        public static readonly Vector2 ExitSize = ExitGround.size;
        public static IReadOnlyList<CityBlock> Monoliths { get; } = Array.AsReadOnly(new[]
        {
            Stone("Monolito - Sol", .72f, .70f),
            Stone("Monolito - Lua", .29f, .68f),
            Stone("Monolito - Estrela", .52f, .57f)
        });
        public static IReadOnlyList<CityBlock> Obstacles { get; } = CreateObstacles();
        private static readonly Vector2[] Floor = {
            new Vector2(.19f,.60f), new Vector2(.32f,.55f), new Vector2(.43f,.51f),
            new Vector2(.48f,.48f), new Vector2(.58f,.49f), new Vector2(.65f,.55f),
            new Vector2(.74f,.59f), new Vector2(.85f,.63f), new Vector2(.87f,.68f),
            new Vector2(.80f,.75f), new Vector2(.68f,.79f), new Vector2(.61f,.86f),
            new Vector2(.58f,1.035f), new Vector2(.43f,1.035f), new Vector2(.40f,.88f),
            new Vector2(.34f,.80f), new Vector2(.24f,.76f), new Vector2(.13f,.72f),
            new Vector2(.14f,.66f)
        };

        private static CityBlock Stone(string name, float u, float v)
        {
            Vector2 p = Ground(u, v);
            return new CityBlock(name, p.x, p.y, 1.25f, .90f, "8A8A7B", true, 2);
        }

        private static IReadOnlyList<CityBlock> CreateObstacles()
        {
            var blocks = new List<CityBlock>(Monoliths);
            blocks.Add(new CityBlock("Inscricao da pedreira", Inscription.x, Inscription.y, 1.6f, .9f, "8A8A7B", true, 2));
            return blocks.AsReadOnly();
        }

        public static Vector2 Ground(float u, float v) => EastForestLayout.Ground(u, v);
        public static Vector2 ImagePoint(Vector2 ground) => EastForestLayout.ImagePoint(ground);

        private static Rect GroundBounds(Rect image)
        {
            Vector2 a = Ground(image.xMin, image.yMin), b = Ground(image.xMax, image.yMin);
            Vector2 c = Ground(image.xMax, image.yMax), d = Ground(image.xMin, image.yMax);
            return Rect.MinMaxRect(Math.Min(Math.Min(a.x,b.x),Math.Min(c.x,d.x)),
                Math.Min(Math.Min(a.y,b.y),Math.Min(c.y,d.y)), Math.Max(Math.Max(a.x,b.x),Math.Max(c.x,d.x)),
                Math.Max(Math.Max(a.y,b.y),Math.Max(c.y,d.y)));
        }

        private static bool Inside(Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = Floor.Length - 1; i < Floor.Length; j = i++)
            {
                Vector2 a = Floor[i], b = Floor[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)
                    inside = !inside;
            }
            return inside;
        }

        public static bool IsWalkable(Vector2 ground)
        {
            if (!Bounds.Contains(ground)) return false;
            Vector2 p = ImagePoint(ground);
            const float margin = .006f;
            if (!Inside(p + new Vector2(-margin,-margin)) || !Inside(p + new Vector2(margin,-margin))
                || !Inside(p + new Vector2(margin,margin)) || !Inside(p + new Vector2(-margin,margin))) return false;
            foreach (CityBlock block in Obstacles)
                if (Overlaps(ground, block.Position, block.Size)) return false;
            return !Overlaps(ground, Worker, new Vector2(.55f,.6f));
        }

        private static bool Overlaps(Vector2 feet, Vector2 obstacle, Vector2 size)
            => Math.Abs(feet.x-obstacle.x) < size.x/2+.285f && Math.Abs(feet.y-obstacle.y) < size.y/2+.31f;

        public static bool IsExit(Vector2 ground) => ExitImage.Contains(ImagePoint(ground)) && IsWalkable(ground);
        public static Vector2 SafeSavedPosition(Vector2 ground) => IsWalkable(ground) ? ground : Spawn;

        public static Vector2 ConstrainMove(Vector2 position, Vector2 delta)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(delta.magnitude/.05f));
            Vector2 step = delta/steps;
            for (int i = 0; i < steps; i++)
            {
                if (IsWalkable(position+step)) { position += step; continue; }
                Vector2 screen = ObliqueProjection.ScreenPlane(step);
                Vector2 horizontal = ObliqueProjection.GroundDirection(new Vector2(screen.x,0));
                Vector2 vertical = ObliqueProjection.GroundDirection(new Vector2(0,screen.y/ObliqueProjection.GroundScale));
                if (IsWalkable(position+horizontal)) position += horizontal;
                if (IsWalkable(position+vertical)) position += vertical;
            }
            return position;
        }
    }
}
