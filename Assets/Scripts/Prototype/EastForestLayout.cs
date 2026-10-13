using System;
using DragonQuest.Exploration;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class EastForestLayout
    {
        public static readonly Rect Bounds = new Rect(-18, -18, 36, 30);
        public static readonly Vector2 Spawn = Ground(.47f, .83f);
        public static readonly Vector2 Exit = Ground(.48f, .975f);
        public static readonly Vector2 Traveler = Ground(.47f, .64f);
        // Coordinates follow the visible clearing, excluding trunks, distant forest and foreground roots.
        // U runs left-to-right and V top-to-bottom in the continuous 16:9 painting.
        private static readonly Vector2[] Floor = {
            new Vector2(.16f,.59f), new Vector2(.28f,.54f), new Vector2(.37f,.535f),
            new Vector2(.45f,.50f), new Vector2(.53f,.54f), new Vector2(.58f,.55f),
            new Vector2(.65f,.57f), new Vector2(.73f,.57f), new Vector2(.77f,.525f),
            new Vector2(.81f,.54f), new Vector2(.84f,.60f), new Vector2(.79f,.69f),
            new Vector2(.69f,.74f), new Vector2(.62f,.80f), new Vector2(.56f,.89f),
            new Vector2(.58f,1.03f), new Vector2(.40f,1.03f), new Vector2(.38f,.89f),
            new Vector2(.30f,.79f), new Vector2(.22f,.76f), new Vector2(.18f,.68f)
        };

        public static Vector2 Ground(float u, float v)
            => ObliqueProjection.GroundDirection(new Vector2((u-.5f)*256f/9f, (.5f-v)*16f/ObliqueProjection.GroundScale));

        public static Vector2 ImagePoint(Vector2 ground)
        {
            Vector2 screen = ObliqueProjection.ScreenPlane(ground);
            return new Vector2(.5f + screen.x*9f/256f, .5f-screen.y/16f);
        }

        private static bool Inside(Vector2 point)
        {
            bool inside = false;
            for (int i=0, j=Floor.Length-1; i<Floor.Length; j=i++)
            {
                Vector2 a=Floor[i], b=Floor[j];
                if ((a.y>point.y)!=(b.y>point.y)
                    && point.x < (b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            return inside;
        }

        public static bool IsWalkable(Vector2 ground)
        {
            if (!Bounds.Contains(ground)) return false;
            Vector2 image=ImagePoint(ground);
            // The footprint has to fit on the floor; the sprite's head may naturally overlap foliage.
            foreach (Vector2 offset in new[] {new Vector2(-.006f,-.006f),new Vector2(.006f,-.006f),
                new Vector2(.006f,.006f),new Vector2(-.006f,.006f)})
                if (!Inside(image+offset)) return false;
            return !Overlaps(ground,PrototypeSecretLayout.ClawStone,new Vector2(1.4f,1))
                && !Overlaps(ground,PrototypeSecretLayout.HollowTree,new Vector2(1.6f,1))
                && !Overlaps(ground,Traveler,new Vector2(.55f,.6f));
        }

        private static bool Overlaps(Vector2 feet,Vector2 obstacle,Vector2 size)
            => Math.Abs(feet.x-obstacle.x)<size.x/2+.285f && Math.Abs(feet.y-obstacle.y)<size.y/2+.31f;

        public static Vector2 SafeSavedPosition(Vector2 position) => IsWalkable(position) ? position : Spawn;

        public static Vector2 ConstrainMove(Vector2 position, Vector2 delta)
        {
            // Substeps prevent crossing a root or a narrow blocked corner during a long physics step.
            int steps=Math.Max(1,(int)Math.Ceiling(delta.magnitude/.05f));
            Vector2 step=delta/steps;
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
