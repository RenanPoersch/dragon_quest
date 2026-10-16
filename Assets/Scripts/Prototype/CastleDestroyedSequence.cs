using System;

namespace DragonQuest.Prototype
{
    // Unscaled time keeps the foreground entrance, scroll and final fade in sync.
    public sealed class CastleDestroyedSequence
    {
        public const float FadeInDuration = 1.4f;
        public const float ArrivalStart = .5f, ArrivalDuration = 5.5f;
        public const float LandStartX = -1110, LandStopX = -112, LandWidth = 1040;
        public const float TextStart = 6.4f, TextWindowHeight = 324, LineHeight = 34, ScrollSpeed = 29;
        public const float EndHold = 1.8f, FadeOutDuration = 3, BlackHold = 1;
        public int LineCount { get; }
        public float Elapsed { get; private set; }
        public float TextEndAt => TextStart + (TextWindowHeight + LineCount * LineHeight) / ScrollSpeed;
        public float FadeOutAt => TextEndAt + EndHold;
        public float BlackAt => FadeOutAt + FadeOutDuration;
        public float CompleteAt => BlackAt + BlackHold;
        public bool IsComplete => Elapsed >= CompleteAt;
        public float TextOffset => TextWindowHeight - Math.Max(0, Elapsed - TextStart) * ScrollSpeed;
        public float LandX => LandStartX + (LandStopX - LandStartX) * Smooth((Elapsed - ArrivalStart) / ArrivalDuration);
        public float BlackOpacity => Math.Max(1 - Smooth(Elapsed / FadeInDuration), Smooth((Elapsed - FadeOutAt) / FadeOutDuration));

        public CastleDestroyedSequence(int lineCount)
        {
            if (lineCount <= 0) throw new ArgumentOutOfRangeException(nameof(lineCount));
            LineCount = lineCount;
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime) || unscaledDeltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            Elapsed = Math.Min(CompleteAt, Elapsed + unscaledDeltaTime);
        }

        private static float Smooth(float value)
        { float t = Math.Max(0, Math.Min(1, value)); return t * t * (3 - 2 * t); }
    }
}
