using System;
using System.Globalization;

// Calcula quanto texto deve aparecer ao longo do tempo durante um diálogo.
namespace DragonQuest.Interactions
{
    // Reveals whole text elements, including accented letters and surrogate pairs.
    public sealed class DialogueTextReveal
    {
        public const float CharactersPerSecond = 48f;
        private string text = string.Empty;
        // Guarda os índices dos elementos de texto para revelar caracteres compostos como uma unidade.
        private int[] elements = Array.Empty<int>();
        // Registra quantos elementos já podem aparecer na interface.
        private int shown;
        private double elapsed;

        public string VisibleText { get; private set; } = string.Empty;
        public bool IsComplete => shown == elements.Length;
        public float SecondsRemaining => (elements.Length - shown) / CharactersPerSecond;

        public void Begin(string message)
        {
            text = message ?? string.Empty;
            elements = StringInfo.ParseCombiningCharacters(text);
            shown = 0;
            elapsed = 0;
            VisibleText = string.Empty;
        }

        public void Tick(float seconds)
        {
            if (IsComplete || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            elapsed += seconds;
            int count = (int)Math.Min(elements.Length, Math.Floor(elapsed * CharactersPerSecond + .000001));
            if (count == shown) return;
            shown = count;
            VisibleText = IsComplete ? text : text.Substring(0, elements[shown]);
        }

        public void Finish()
        {
            shown = elements.Length;
            VisibleText = text;
        }
    }
}
