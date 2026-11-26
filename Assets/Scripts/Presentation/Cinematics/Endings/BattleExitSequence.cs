using System;

// Coordena o escurecimento usado para sair da batalha antes de mostrar a próxima cena.
namespace DragonQuest.Presentation.Cinematics.Endings
{
    // Both ends of the fade wait for a repaint; loading cannot skip the battle or expose the cut.
    public sealed class BattleExitSequence
    {
        // Define o tempo do escurecimento e o período de tela totalmente preta.
        public const float FadeOutDuration = 1.4f, BlackHold = 2;
        public float Elapsed { get; private set; }
        public bool IsPlaying { get; private set; }
        // Acompanha se a transição já foi desenhada e se a tela já foi coberta.
        private bool hasDrawn, covered;
        public float BlackOpacity
        {
            get
            {
                float t = Math.Min(1, Elapsed / FadeOutDuration);
                return t * t * (3 - 2 * t);
            }
        }
        public float MusicGain => 1 - BlackOpacity;
        public void Begin() { Elapsed = 0; hasDrawn = covered = false; IsPlaying = true; }
        public void MarkDrawn()
        {
            if (!IsPlaying) return;
            hasDrawn = true;
            if (Elapsed >= FadeOutDuration) covered = true;
        }
        // True exactly once, after a fully black repaint and the pause before the ending.
        public bool Advance(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
            if (!IsPlaying || !hasDrawn) return false;
            Elapsed = Math.Min(covered ? FadeOutDuration + BlackHold : FadeOutDuration, Elapsed + delta);
            if (!covered || Elapsed < FadeOutDuration + BlackHold) return false;
            IsPlaying = false;
            return true;
        }
    }
}
