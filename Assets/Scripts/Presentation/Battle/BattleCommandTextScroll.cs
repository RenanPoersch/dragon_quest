using System;

namespace DragonQuest.Presentation.Battle
{
    public static class BattleCommandTextScroll
    {
        public const double StartDelay = .7, EndDelay = .4, PixelsPerSecond = 60;

        public static float Offset(double elapsed, float textWidth, float viewportWidth)
        {
            double distance = Math.Max(0, textWidth - viewportWidth);
            if (distance == 0 || elapsed <= 0) return 0;
            double cycle = StartDelay + distance / PixelsPerSecond + EndDelay;
            double phase = elapsed % cycle;
            return (float)Math.Max(0, Math.Min(distance, (phase - StartDelay) * PixelsPerSecond));
        }
    }
}
