using System;
using System.Collections.Generic;

namespace DragonQuest.Progression
{
    // Poema original da lenda ficticia da ilha; nao e uma citacao de autor antigo.
    public static class LegendPoem
    {
        public const string Flame = "Flamma ardet, regnum perit.";
        public const string Sky = "Luna surgit, stella lucet, sol redit.";
        public const string Forest = "Silva antiqua memoriam servat.";
        public static IReadOnlyList<string> Verses { get; } = Array.AsReadOnly(new[] { Flame, Sky, Forest });
        // A posicao da resposta no altar nao segue a ordem do poema.
        public static IReadOnlyList<string> AltarChoices { get; } = Array.AsReadOnly(new[] { Forest, Flame, Sky });
        public static string Text => string.Join("\n", Verses);
        public static string[] ExplainedPages() => new[]
        {
            Text,
            Flame + "\n\nA chama arde; o reino perece. Conta-se que um antigo reino caiu quando a chama despertou.",
            Sky + "\n\nA Lua se ergue, a Estrela brilha, o Sol retorna. O verso conta a passagem da noite ate o novo dia.",
            Forest + "\n\nA floresta antiga guarda a memoria. Mesmo quando as pessoas esquecem, as arvores conservam vestigios do passado."
        };
    }
}
