using System;

namespace DragonQuest.Prototype
{
    // A separate unscaled clock keeps the existing hillside epilogue at its original pace.
    public sealed class CastleImpactSequence
    {
        public const float FadeInDuration = 1.4f, MeteorStart = 3, FallDuration = 13;
        public const float ImpactAt = MeteorStart + FallDuration;
        public const float FlashDuration = .45f, WhiteHold = 1.4f;
        public const float CompleteAt = ImpactAt + FlashDuration + WhiteHold;
        public float Elapsed { get; private set; }
        public bool IsComplete => Elapsed >= CompleteAt;
        public float FallProgress => Clamp((Elapsed - MeteorStart) / FallDuration);
        // Almost linear motion preserves the weight and deliberate speed of the descent.
        public float Approach => FallProgress * (.8f + .2f * FallProgress);
        public float MeteorX => 300 + 580 * Approach;
        public float MeteorY => -70 + 200 * Approach;
        public float MeteorSize => 460 + 220 * Approach;
        public float MeteorOpacity => Smooth((Elapsed - MeteorStart) / 2.1f);
        public float Heat => MeteorOpacity * (.18f + .82f * Approach * Approach);
        public float BlackOpacity => 1 - Smooth(Elapsed / FadeInDuration);
        public float WhiteOpacity => Smooth((Elapsed - ImpactAt) / FlashDuration);
        public float CameraZoom => 1 + .025f * Smooth(Elapsed / ImpactAt);
        public float ImpactShake => Elapsed < ImpactAt ? 0 : 4 * (1 - WhiteOpacity);

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
