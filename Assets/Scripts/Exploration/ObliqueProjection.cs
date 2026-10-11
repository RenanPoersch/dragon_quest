using System;
using UnityEngine;

namespace DragonQuest.Exploration
{
    // XY continua sendo o chao fisico; -Z representa altura apenas na apresentacao.
    public static class ObliqueProjection
    {
        public const float TiltDegrees = 55f;
        public const float YawDegrees = 22f;
        public const float Distance = 60f;
        public static readonly float GroundScale = (float)Math.Cos(TiltDegrees * Math.PI / 180);
        public static readonly float HeightScale = (float)Math.Sin(TiltDegrees * Math.PI / 180);
        public static readonly float YawCos = (float)Math.Cos(YawDegrees * Math.PI / 180);
        public static readonly float YawSin = (float)Math.Sin(YawDegrees * Math.PI / 180);

        public static Vector3 Point(Vector2 ground, float height = 0) => new Vector3(ground.x, ground.y, -height);

        public static Vector2 ScreenPlane(Vector2 ground, float height = 0)
            => new Vector2(ground.x * YawCos + ground.y * YawSin,
                (-ground.x * YawSin + ground.y * YawCos) * GroundScale + height * HeightScale);

        public static Vector2 GroundDirection(Vector2 input)
            => new Vector2(input.x * YawCos - input.y * YawSin, input.x * YawSin + input.y * YawCos);

        public static float FrameSize(float requested, Rect bounds, float aspect, bool fixedFrame)
        {
            if (!fixedFrame) return requested;
            float width = bounds.width * YawCos + bounds.height * YawSin;
            float depth = (bounds.width * YawSin + bounds.height * YawCos) * GroundScale;
            return Math.Max(requested, Math.Max((width / 2 + 0.6f) / Math.Max(0.1f, aspect), depth / 2 + 1.8f));
        }

        public static Vector2 CameraFocus(Vector2 hero, Rect bounds, float size, float aspect, bool fixedFrame)
        {
            if (fixedFrame) return bounds.center;
            float halfWidth = size * aspect, halfDepth = size / GroundScale;
            float halfX = halfWidth * YawCos + halfDepth * YawSin;
            float halfY = halfWidth * YawSin + halfDepth * YawCos;
            var focus = new Vector2(ClampAxis(hero.x, bounds.xMin, bounds.xMax, halfX),
                ClampAxis(hero.y, bounds.yMin, bounds.yMax, halfY));
            var boundedHero = new Vector2(Math.Max(bounds.xMin,Math.Min(bounds.xMax,hero.x)),
                Math.Max(bounds.yMin,Math.Min(bounds.yMax,hero.y)));
            Vector2 relative = ScreenPlane(new Vector2(boundedHero.x-focus.x,boundedHero.y-focus.y));
            float visibleX = Math.Max(0.1f,halfWidth-1), visibleY = Math.Max(0.1f,size-1.9f);
            Vector2 correction = GroundDirection(new Vector2(
                relative.x-Math.Max(-visibleX,Math.Min(visibleX,relative.x)),
                (relative.y-Math.Max(-visibleY,Math.Min(visibleY,relative.y)))/GroundScale));
            // Nos cantos, a planta rodada nao cabe no retangulo conservador: prioriza ver o heroi.
            return new Vector2(Math.Max(bounds.xMin+0.01f,Math.Min(bounds.xMax-0.01f,focus.x+correction.x)),
                Math.Max(bounds.yMin+0.01f,Math.Min(bounds.yMax-0.01f,focus.y+correction.y)));
        }

        private static float ClampAxis(float value, float min, float max, float half)
            => max - min <= half * 2 ? (min + max) / 2 : Math.Max(min + half, Math.Min(max - half, value));
    }
}
