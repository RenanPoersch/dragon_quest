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
        public const float HeroCenterHeight = 0.72f;
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

        public static float FrameSize(float requested, Rect bounds, float aspect, bool fixedFrame, Rect? coveredGroundBounds = null)
        {
            if (!fixedFrame) return coveredGroundBounds.HasValue
                ? CoveredFrameSize(requested, bounds, coveredGroundBounds.Value, aspect) : requested;
            float width = bounds.width * YawCos + bounds.height * YawSin;
            float depth = (bounds.width * YawSin + bounds.height * YawCos) * GroundScale;
            return Math.Max(requested, Math.Max((width / 2 + 0.6f) / Math.Max(0.1f, aspect), depth / 2 + 1.8f));
        }

        public static Vector2 CameraFocus(Vector2 hero, Rect bounds, float size, float aspect, bool fixedFrame, Rect? coveredGroundBounds = null)
        {
            if (fixedFrame) return bounds.center;
            // Mapas com terreno estendido conservam o foco dentro da cidade, sem atrasar o acompanhamento.
            // Os demais mapas seguem o enquadramento existente.
            return coveredGroundBounds.HasValue ? new Vector2(
                Math.Max(bounds.xMin, Math.Min(bounds.xMax, hero.x)),
                Math.Max(bounds.yMin, Math.Min(bounds.yMax, hero.y))) : hero;
        }

        // Intersecao do raio ortografico com um nivel do chao. Inclui yaw, inclinacao e altura do foco.
        public static Vector2 GroundViewportOffset(Vector2 screen, float focusHeight, float groundHeight = 0)
            => GroundDirection(new Vector2(screen.x, (screen.y + (focusHeight - groundHeight) * HeightScale) / GroundScale));

        private static float CoveredFrameSize(float requested, Rect cameraBounds, Rect terrain, float aspect)
        {
            // Abrange o fosso (-0.7) e o tabuleiro da ponte; uma folga evita expor a borda por arredondamento.
            Vector2 a = GroundViewportOffset(Vector2.zero, HeroCenterHeight, -0.7f);
            Vector2 b = GroundViewportOffset(Vector2.zero, HeroCenterHeight, 0.1f);
            Vector2 minShift = Vector2.Min(a, b), maxShift = Vector2.Max(a, b);
            float horizontal = Math.Min(cameraBounds.xMin - terrain.xMin + minShift.x,
                terrain.xMax - cameraBounds.xMax - maxShift.x) - 0.05f;
            float vertical = Math.Min(cameraBounds.yMin - terrain.yMin + minShift.y,
                terrain.yMax - cameraBounds.yMax - maxShift.y) - 0.05f;
            if (horizontal <= 0 || vertical <= 0) throw new ArgumentException("O terreno precisa cobrir os limites da camera.");
            aspect = Math.Max(0.1f, aspect);
            float xPerSize = aspect * YawCos + YawSin / GroundScale;
            float yPerSize = aspect * YawSin + YawCos / GroundScale;
            return Math.Min(requested, Math.Min(horizontal / xPerSize, vertical / yPerSize));
        }

        public static Vector3 CameraPosition(Vector2 presentedHero, Rect bounds, float size, float aspect,
            bool fixedFrame, Vector3 forward, Rect? coveredGroundBounds = null)
        {
            Vector2 focus = CameraFocus(presentedHero, bounds, size, aspect, fixedFrame, coveredGroundBounds);
            return Point(focus, fixedFrame ? 0 : HeroCenterHeight) - forward * Distance;
        }
    }
}
