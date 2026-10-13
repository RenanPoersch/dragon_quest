using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeRuinsLayout
    {
        public static readonly Rect Bounds = new Rect(-42, -4, 44, 14);
        public static readonly Rect LegacyBounds = new Rect(-9, -7, 18, 14);
        public static readonly Vector2 Spawn = new Vector2(-2.5f, 0);
        public static readonly Vector2 Exit = new Vector2(0, 0);
        public const float CameraSize = 4.5f, CameraHeight = 3.2f;
        public const float MinX = -38.5f, MaxX = .4f, AltarX = -31;
        public const float WalkingSpeedMultiplier = 1.15f;
        public static readonly Vector2 Altar = new Vector2(AltarX, 1.05f);
        public static IReadOnlyList<CityBlock> Icon { get; } = Array.AsReadOnly(new[]
        {
            new CityBlock("Ruinas - base",0,.1f,1.8f,.8f,"85887B",true,2)
        });

        // Small ramps over the three worn stair edges keep the walking cycle continuous.
        public static float FloorAt(float x)
        {
            x = -x;
            float height = 0;
            for (int i = 0; i < 3; i++)
                height += .35f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(26 + i * .8f, 26.7f + i * .8f, x));
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
