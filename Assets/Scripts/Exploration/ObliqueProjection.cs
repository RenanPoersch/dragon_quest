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

        public static float FrameSize(float requested, Rect bounds, float aspect, bool fixedFrame)
        {
            if (!fixedFrame) return requested;
            float width = bounds.width * YawCos + bounds.height * YawSin;
            float depth = (bounds.width * YawSin + bounds.height * YawCos) * GroundScale;
            return Math.Max(requested, Math.Max((width / 2 + 0.6f) / Math.Max(0.1f, aspect), depth / 2 + 1.8f));
        }

        public static Vector2 CameraFocus(Vector2 hero, Rect bounds, float size, float aspect, bool fixedFrame)
        {
            // Os colliders limitam o heroi. Limitar o foco pelo retangulo do mapa
            // deixa-o sair do centro da tela, especialmente com a camera rodada.
            return fixedFrame ? bounds.center : hero;
        }

        public static Vector3 CameraPosition(Vector2 presentedHero, Rect bounds, float size, float aspect,
            bool fixedFrame, Vector3 forward)
        {
            Vector2 focus = CameraFocus(presentedHero, bounds, size, aspect, fixedFrame);
            return Point(focus, fixedFrame ? 0 : HeroCenterHeight) - forward * Distance;
        }
    }
}
