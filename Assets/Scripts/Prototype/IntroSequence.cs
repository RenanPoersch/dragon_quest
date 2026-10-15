using System;

namespace DragonQuest.Prototype
{
    // Advances with unscaled time so opening a paused game cannot freeze the intro.
    public sealed class IntroSequence
    {
        public const string Title = "DRAGON QUEST";
        public const float FadeInDuration = 1.8f;
        public const float TitleDelay = 1.45f;
        public const float LetterInterval = .11f;
        public const float AutoExitTime = 7.8f;
        public const float FadeOutDuration = .85f;
        public const float EarliestSkip = .35f;
        private float exitAt = AutoExitTime;

        public float Elapsed { get; private set; }
        public bool IsPlaying => Elapsed < exitAt + FadeOutDuration;
        public bool IsExiting => Elapsed >= exitAt;
        public float Opacity => Smooth(Elapsed / FadeInDuration)
            * (1 - Smooth((Elapsed - exitAt) / FadeOutDuration));
        public float TitleOpacity => Smooth((Elapsed - TitleDelay) / .6f);
        public int VisibleLetters => Math.Min(Title.Length,
            Math.Max(0, (int)Math.Floor((Elapsed - TitleDelay) / LetterInterval) + 1));

        public void Advance(float unscaledDeltaTime)
        {
            if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime) || unscaledDeltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            Elapsed += unscaledDeltaTime;
        }

        public bool TrySkip()
        {
            if (Elapsed < EarliestSkip || IsExiting || !IsPlaying) return false;
            exitAt = Elapsed;
            return true;
        }

        private static float Smooth(float value)
        {
            float t = Math.Max(0, Math.Min(1, value));
            return t * t * (3 - 2 * t);
        }
    }
}
