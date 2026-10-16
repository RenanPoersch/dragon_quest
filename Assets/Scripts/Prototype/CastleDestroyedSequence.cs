using System;

namespace DragonQuest.Prototype
{
    // Unscaled time keeps the opposing scenery drift, walk, scroll and fade in sync.
    public sealed class CastleDestroyedSequence
    {
        public const float FadeInDuration = 1.4f;
        public const float ArrivalStart = .5f, ArrivalDuration = 16;
        public const float LandStartX = -1080, LandStopX = -144, LandWidth = 1040;
        public const float LandDrift = 48, BackgroundDrift = 72;
        public const float ArrivalEnd = ArrivalStart + ArrivalDuration;
        public const float ElderEntranceStart = ArrivalEnd + .7f, ElderWalkDuration = 12;
        public const float ElderAtCastleAt = ElderEntranceStart + ElderWalkDuration;
        public const float ElderStartLocalX = -24, ElderStopLocalX = 710, ElderWidth = 88;
        public const float TextStart = ElderAtCastleAt + .8f, TextWindowHeight = 324, LineHeight = 34, ScrollSpeed = 29;
        public const float EndHold = .8f, FadeOutDuration = 3, BlackHold = 1;
        public int LineCount { get; }
        public float Elapsed { get; private set; }
        public float TextEndAt => TextStart + (TextWindowHeight + LineCount * LineHeight) / ScrollSpeed;
        public float ElderExitAt => TextEndAt + .8f;
        public float ElderExitEnd => ElderExitAt + ElderWalkDuration;
        public float FadeOutAt => ElderExitEnd + EndHold;
        public float BlackAt => FadeOutAt + FadeOutDuration;
        public float CompleteAt => BlackAt + BlackHold;
        public bool IsComplete => Elapsed >= CompleteAt;
        public float TextOffset => TextWindowHeight - Math.Max(0, Elapsed - TextStart) * ScrollSpeed;
        public float LandX => LandStartX + (LandStopX - LandStartX) * Smooth((Elapsed - ArrivalStart) / ArrivalDuration)
            + LandDrift * Clamp((Elapsed - ArrivalEnd) / (CompleteAt - ArrivalEnd));
        public float BackgroundX => -BackgroundDrift * Elapsed / CompleteAt;
        public bool ElderVisible => Elapsed >= ElderEntranceStart && Elapsed < ElderExitEnd;
        public bool ElderFacingCastle => Elapsed >= ElderAtCastleAt && Elapsed < ElderExitAt;
        public bool ElderWalking => ElderVisible && !ElderFacingCastle;
        public float ElderLocalX => Elapsed < ElderExitAt
            ? Lerp(ElderStartLocalX, ElderStopLocalX, (Elapsed - ElderEntranceStart) / ElderWalkDuration)
            : Lerp(ElderStopLocalX, ElderStartLocalX, (Elapsed - ElderExitAt) / ElderWalkDuration);
        public float ElderX => LandX + ElderLocalX;
        public int ElderFrame
        {
            get
            {
                if (ElderFacingCastle) return 4 + (int)(Math.Max(0, Elapsed - ElderAtCastleAt) * 1.5f) % 4;
                bool leaving = Elapsed >= ElderExitAt;
                float walkingTime = Math.Max(0, Elapsed - (leaving ? ElderExitAt : ElderEntranceStart));
                return (leaving ? 8 : 0) + (int)(walkingTime * 6) % 4;
            }
        }
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
        private static float Lerp(float a, float b, float value) => a + (b - a) * Clamp(value);
        private static float Smooth(float value)
        { float t = Clamp(value); return t * t * (3 - 2 * t); }
    }
}
