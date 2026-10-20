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
        public const float MaximumShake = 20;
        public float Elapsed { get; private set; }
        public bool IsComplete => Elapsed >= CompleteAt;
        public float FallProgress => Clamp((Elapsed - MeteorStart) / FallDuration);
        // Start gently and accelerate continuously toward the castle, without braking before impact.
        public float Approach => FallProgress * FallProgress;
        public float MeteorX => 300 + 580 * Approach;
        public float MeteorY => -70 + 200 * Approach;
        public float MeteorSize => 460 + 220 * Approach;
        public float MeteorOpacity => Smooth((Elapsed - MeteorStart) / 2.1f);
        public float Heat => MeteorOpacity * (.18f + .82f * Approach * Approach);
        public float BlackOpacity => 1 - Smooth(Elapsed / FadeInDuration);
        public float MusicGain => 1 - BlackOpacity;
        public float WhiteOpacity => Smooth((Elapsed - ImpactAt) / FlashDuration);
        public float CameraZoom => 1 + .025f * Smooth(Elapsed / ImpactAt);
        public float ScreenShake
        {
            get
            {
                // Continuous rumble with repeated stronger pulses grows throughout the descent.
                float pulse = (float)Math.Pow(Math.Max(0, Math.Sin(Elapsed * 2.6f)), 4);
                float approach = MeteorOpacity * (3 + 9 * Approach + 4 * pulse);
                float collision = Elapsed < ImpactAt ? 0 : MaximumShake;
                return Math.Max(approach, collision) * (1 - WhiteOpacity);
            }
        }
        // Layered frequencies avoid a regular circular camera wobble and remain frame-rate independent.
        public float ShakeX => ScreenShake * (float)(.62 * Math.Sin(Elapsed * 43)
            + .25 * Math.Sin(Elapsed * 79) + .13 * Math.Sin(Elapsed * 127));
        public float ShakeY => ScreenShake * (float)(.58 * Math.Sin(Elapsed * 51 + 1.3)
            + .28 * Math.Sin(Elapsed * 91) + .14 * Math.Cos(Elapsed * 137));

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
