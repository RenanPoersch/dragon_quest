using System;

namespace DragonQuest.Prototype
{
    // Speech and prosperity share one unscaled clock; neither text is lost in the dissolve.
    public sealed class KingFreedSequence
    {
        public const float FadeInDuration = 1.4f, TextStart = 2.2f;
        public const float TextWindowHeight = 324, LineHeight = 34, ScrollSpeed = 29;
        public const float EndHold = 1.8f, DissolveDuration = 3, ProsperityHold = 1.4f;
        public const float FadeOutDuration = 3, BlackHold = 1;
        public int SpeechLineCount { get; }
        public int EpilogueLineCount { get; }
        public float Elapsed { get; private set; }
        public float SpeechTextEndAt => TextStart + ScrollDuration(SpeechLineCount);
        public float DissolveAt => SpeechTextEndAt + EndHold;
        public float ProsperityAt => DissolveAt + DissolveDuration;
        public float EpilogueTextStart => ProsperityAt + ProsperityHold;
        public float TextEndAt => EpilogueTextStart + ScrollDuration(EpilogueLineCount);
        public float FadeOutAt => TextEndAt + EndHold;
        public float BlackAt => FadeOutAt + FadeOutDuration;
        public float CompleteAt => BlackAt + BlackHold;
        public bool IsComplete => Elapsed >= CompleteAt;
        public float ProsperityOpacity => Smooth((Elapsed - DissolveAt) / DissolveDuration);
        public float SpeechProgress => Clamp(Elapsed / ProsperityAt);
        public float ProsperityProgress => Clamp((Elapsed - DissolveAt) / (BlackAt - DissolveAt));
        public float SpeechTextOffset => TextOffset(TextStart);
        public float EpilogueTextOffset => TextOffset(EpilogueTextStart);
        public float SpeechTextOpacity => TextOpacity(TextStart, SpeechTextEndAt);
        public float EpilogueTextOpacity => TextOpacity(EpilogueTextStart, TextEndAt);
        public float BlackOpacity => Math.Max(1 - Smooth(Elapsed / FadeInDuration), Smooth((Elapsed - FadeOutAt) / FadeOutDuration));

        public KingFreedSequence(int speechLineCount, int epilogueLineCount)
        {
            if (speechLineCount <= 0) throw new ArgumentOutOfRangeException(nameof(speechLineCount));
            if (epilogueLineCount <= 0) throw new ArgumentOutOfRangeException(nameof(epilogueLineCount));
            SpeechLineCount = speechLineCount; EpilogueLineCount = epilogueLineCount;
        }
        public void Advance(float unscaledDeltaTime)
        {
            if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime) || unscaledDeltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            Elapsed = Math.Min(CompleteAt, Elapsed + unscaledDeltaTime);
        }
        private static float ScrollDuration(int lineCount) => (TextWindowHeight + lineCount * LineHeight) / ScrollSpeed;
        private float TextOffset(float start) => TextWindowHeight - Math.Max(0, Elapsed - start) * ScrollSpeed;
        private float TextOpacity(float start, float end)
            => Clamp((Elapsed - start) / 1.4f) * (1 - Clamp((Elapsed - end) / 1.2f));
        private static float Clamp(float value) => Math.Max(0, Math.Min(1, value));
        private static float Smooth(float value) { float t = Clamp(value); return t * t * (3 - 2 * t); }
    }
}
