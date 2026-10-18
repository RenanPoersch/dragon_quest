using System;

namespace DragonQuest.Prototype
{
    // The seated figure shares the hill's movement; only the scenery, scroll and fade advance.
    public sealed class CastleDestroyedSequence
    {
        public const float FadeInDuration = 1.4f;
        public const float ArrivalStart = .5f, ArrivalDuration = 16;
        public const float LandStartX = -1080, LandStopX = -144, LandWidth = 1040;
        public const float LandDrift = 32, BackgroundDrift = 90;
        public const float ArrivalEnd = ArrivalStart + ArrivalDuration;
        public const float TextStart = ArrivalEnd + .8f, TextWindowHeight = 324, LineHeight = 34, ScrollSpeed = 29;
        public const float EndHold = 1.8f, FadeOutDuration = 3, BlackHold = 1;
        public int LineCount { get; }
        public float Elapsed { get; private set; }
        public float MusicGain => 1 - Smooth((Elapsed - FadeOutAt) / FadeOutDuration);
        public float TextEndAt => TextStart + (TextWindowHeight + LineCount * LineHeight) / ScrollSpeed;
        public float FadeOutAt => TextEndAt + EndHold;
        public float BlackAt => FadeOutAt + FadeOutDuration;
        public float CompleteAt => BlackAt + BlackHold;
        public bool IsComplete => Elapsed >= CompleteAt;
        public float TextOffset => TextWindowHeight - Math.Max(0, Elapsed - TextStart) * ScrollSpeed;
        public float LandX => LandStartX + (LandStopX - LandStartX) * Smooth((Elapsed - ArrivalStart) / ArrivalDuration)
            + LandDrift * Clamp((Elapsed - ArrivalEnd) / (CompleteAt - ArrivalEnd));
        public float BackgroundX => -BackgroundDrift * Smooth(Elapsed / CompleteAt);
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

        private static float Clamp(float value) => Math.Max(0, Math.Min(1, value));
        private static float Smooth(float value)
        { float t = Clamp(value); return t * t * (3 - 2 * t); }
    }
}
