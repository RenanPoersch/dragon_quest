using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Exploration.Layouts
{
    // A lateral corridor, following the same presentation and controls as the ruins.
    public static class VolcanoLayout
    {
        public static readonly Rect Bounds = new Rect(-22, -4, 44, 14);
        public static readonly Rect LegacyBounds = new Rect(-10, -14, 20, 28);
        public const float CameraSize = 5.4f, CameraHeight = 3.2f;
        public const float MinX = -20, MaxX = 12.9f;
        public const float WalkingSpeedMultiplier = 1.15f;
        public static readonly Vector2 Spawn = new Vector2(-17.5f, 0);
        public static readonly Vector2 Exit = new Vector2(-19.6f, 0);
        public static readonly Vector2 Dragon = new Vector2(14.1f, .9f);

        // Retained for existing map inspection; the painting now supplies all visible terrain.
        public static IReadOnlyList<CityBlock> Blocks { get; } = Array.AsReadOnly(new CityBlock[0]);

        public static float FloorAt(float x)
        {
            float height = 0;
            for (int i = 0; i < 3; i++)
                height += .3f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4.7f + i * 1.1f, 5.3f + i * 1.1f, x));
            return height;
        }

        public static Vector2 SafeSavedPosition(Vector2 position)
        {
            if (Mathf.Abs(position.y - FloorAt(position.x)) > .2f) return Spawn;
            float x = Mathf.Clamp(position.x, MinX, MaxX);
            return new Vector2(x, FloorAt(x));
        }
    }
}
