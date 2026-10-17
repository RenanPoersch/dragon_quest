using System;

namespace DragonQuest.Prototype
{
    // One unscaled clock drives the upward camera, narration, lightning and fades.
    public sealed class HeroPossessedSequence
    {
        public const float FadeInDuration = CastleDestroyedSequence.FadeInDuration;
        public const float CameraStart = 1.4f, CarpetDuration = 9, StairDuration = 10, RevealDuration = 8;
        public const float CameraStopAt = CameraStart + CarpetDuration + StairDuration + RevealDuration;
        public const float FinalCameraFocusY = .16f;
        public const float TextStart = 2.2f, TextWindowHeight = 324, LineHeight = 34, ScrollSpeed = 29;
        public const float EndHold = CastleDestroyedSequence.EndHold;
        public const float FadeOutDuration = CastleDestroyedSequence.FadeOutDuration;
        public const float BlackHold = CastleDestroyedSequence.BlackHold;
        public const float FirstLightningAt = CameraStopAt + 2.4f;
        private static readonly float[] strikes = { 2.4f, 7.9f, 15.2f, 20.7f };
        public int LineCount { get; }
        public float Elapsed { get; private set; }
        public float TextEndAt => TextStart + (TextWindowHeight + LineCount * LineHeight) / ScrollSpeed;
        // Even short/localized copy leaves time to see two strikes after the camera locks.
        public float FadeOutAt => Math.Max(TextEndAt + EndHold, CameraStopAt + 11);
        public float BlackAt => FadeOutAt + FadeOutDuration;
        public float CompleteAt => BlackAt + BlackHold;
        public bool IsComplete => Elapsed >= CompleteAt;
        public bool CameraStopped => Elapsed >= CameraStopAt;
        public float TextOffset => TextWindowHeight - Math.Max(0, Elapsed - TextStart) * ScrollSpeed;
        public float BlackOpacity => Math.Max(1 - Smooth(Elapsed / FadeInDuration), Smooth((Elapsed - FadeOutAt) / FadeOutDuration));

        // Normalized image coordinates, measured from the top of the continuous painting.
        public float CameraFocusY
        {
            get
            {
                float t = Elapsed - CameraStart;
                if (t <= CarpetDuration) return Lerp(.84f, .64f, Smooth(t / CarpetDuration));
                t -= CarpetDuration;
                if (t <= StairDuration) return Lerp(.64f, .44f, Smooth(t / StairDuration));
                return Lerp(.44f, FinalCameraFocusY, Smooth((t - StairDuration) / RevealDuration));
            }
        }
        public float CameraImageWidth => Lerp(1500, 1040, Smooth((Elapsed - CameraStart) / (CameraStopAt - CameraStart)));
        public float Lightning => SampleLightning(out _);
        public int LightningFrame
        {
            get { SampleLightning(out int frame); return frame; }
        }
        private float SampleLightning(out int frame)
        {
            frame = -1;
            if (!CameraStopped || Elapsed >= FadeOutAt) return 0;
            float t = (Elapsed - CameraStopAt) % 26;
            foreach (float strike in strikes)
            {
                float first = Pulse(t - strike, .08f, .30f);
                if (first > 0) { frame = 0; return first; }
                float second = .65f * Pulse(t - strike - .43f, .05f, .22f);
                if (second > 0) { frame = 1; return second; }
            }
            return 0;
        }

        public HeroPossessedSequence(int lineCount)
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
        private static float Pulse(float t, float rise, float decay)
        {
            if (t < 0 || t > rise + decay) return 0;
            return t < rise ? t / rise : 1 - Smooth((t - rise) / decay);
        }
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Smooth(float value)
        { float t = Math.Max(0, Math.Min(1, value)); return t * t * (3 - 2 * t); }
    }
}
