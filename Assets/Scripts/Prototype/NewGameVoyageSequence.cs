using System;

namespace DragonQuest.Prototype
{
    // A single unscaled clock keeps the ship, party, scroll and transition in sync.
    public sealed class NewGameVoyageSequence
    {
        public const float FadeInDelay = 2;
        public const float FadeInDuration = 3.2f;
        // Let the opening fade finish before the voyage starts moving.
        public const float ArrivalStart = FadeInDelay + FadeInDuration + .4f, ArrivalDuration = 6.2f;
        public const float ShipWidth = 1560, ShipStartX = 1300, ShipStopX = 548, ShipExitX = -ShipWidth - 64;
        public const float HeroSize = 96;
        public const float WalkStart = ArrivalStart + 1.7f, WalkDuration = 4.4f, HeroDelay = .48f;
        public const float TextStart = ArrivalStart + ArrivalDuration + .4f;
        public const float TextWindowHeight = 380, LineHeight = 32, ScrollSpeed = 30;
        // Match the ending's gentle fade and full second of black.
        public const float EndHold = 1.8f, DepartureDuration = 8, FadeOutDuration = 3;
        public const float BlackHold = 1, RevealDuration = 1.2f;
        private static readonly string[] lines =
        {
            "Além do mar, uma ilha",
            "vive sob o domínio de",
            "um rei que espalha medo.",
            "",
            "As estradas se calaram.",
            "Nas vilas, a esperança",
            "se tornou um sussurro.",
            "",
            "Entre ruínas esquecidas,",
            "uma antiga lenda ainda",
            "guarda a promessa de",
            "romper essa escuridão.",
            "",
            "Aren, Lia e Bram cruzam",
            "o oceano em busca de",
            "respostas — e de um",
            "futuro para esse povo.",
            "",
            "Diante deles, uma costa",
            "desconhecida. Além dela,",
            "segredos, perigos e uma",
            "força que aguarda",
            "para ser despertada.",
            "",
            "Quando a terra surgir",
            "no horizonte, cada passo",
            "decidirá o destino",
            "da ilha."
        };

        public static int LineCount => lines.Length;
        public static string Line(int index) => lines[index];
        public static float TextEndAt => TextStart + (TextWindowHeight + LineCount * LineHeight) / ScrollSpeed;
        public static float DepartureAt => TextEndAt + EndHold;
        public static float FadeOutAt => DepartureAt + DepartureDuration - FadeOutDuration;
        public static float BlackAt => DepartureAt + DepartureDuration;
        public static float RevealAt => BlackAt + BlackHold;
        public static float CompleteAt => RevealAt + RevealDuration;
        public float Elapsed { get; private set; }
        public float AnimationElapsed => Math.Max(0, Elapsed - FadeInDelay - FadeInDuration);
        public bool IsPlaying => Elapsed < CompleteAt;
        public bool ShowsScene => Elapsed < RevealAt;
        public bool IsDeparting => Elapsed >= DepartureAt;
        public float NarrativeOpacity => Clamp((Elapsed - TextStart) / 1.4f)
            * (1 - Clamp((Elapsed - TextEndAt) / 1.2f));
        public float TextOffset => TextWindowHeight - Math.Max(0, Elapsed - TextStart) * ScrollSpeed;
        // A tracking camera sees the coast and water pass right as the ship sails left.
        public float SailingDistance => AnimationElapsed * 12 + (ShipStartX - ShipX) * .15f;
        public float BackgroundOffset => SailingDistance * .35f;
        public float ShipX => Elapsed < DepartureAt
            ? Lerp(ShipStartX, ShipStopX, Smooth((Elapsed - ArrivalStart) / ArrivalDuration))
            : Lerp(ShipStopX, ShipExitX, Smooth((Elapsed - DepartureAt) / DepartureDuration));
        public float BlackOpacity => Elapsed >= RevealAt
            ? 1 - Smooth((Elapsed - RevealAt) / RevealDuration)
            : Math.Max(1 - Smooth((Elapsed - FadeInDelay) / FadeInDuration),
                Smooth((Elapsed - FadeOutAt) / FadeOutDuration));

        public float HeroLocalX(int index) => Lerp(.64f + index * .055f, .245f + index * .065f, HeroWalkProgress(index));

        public float HeroWalkProgress(int index)
        {
            if (index < 0 || index > 2) throw new ArgumentOutOfRangeException(nameof(index));
            return Clamp((Elapsed - WalkStart - index * HeroDelay) / WalkDuration);
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime) || unscaledDeltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            Elapsed = Math.Min(CompleteAt, Elapsed + unscaledDeltaTime);
        }

        private static float Clamp(float value) => Math.Max(0, Math.Min(1, value));
        private static float Smooth(float value) { float t = Clamp(value); return t * t * (3 - 2 * t); }
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
