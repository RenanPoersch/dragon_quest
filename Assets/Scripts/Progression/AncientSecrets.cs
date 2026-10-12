namespace DragonQuest.Progression
{
    public enum MonolithSymbol { Sun, Moon, Star }
    public enum MonolithResult { Invalid, Reset, Accepted, Completed, AlreadyCompleted }

    // Estado unico da sessao: viajar ou reabrir um dialogo nao recria os segredos.
    public sealed class AncientSecrets
    {
        public bool KnowsPoem { get; private set; }
        public bool AltarSolved { get; private set; }
        public bool MonolithsSolved { get; private set; }
        public bool HasScale { get; private set; }
        public bool PassageOpen { get; private set; }
        public int MonolithStep { get; private set; }
        public event System.Action Changed;
        internal void Restore(bool poem, bool altar, int monolithStep, bool scale, bool passage)
        {
            KnowsPoem = poem; AltarSolved = altar; MonolithStep = monolithStep;
            MonolithsSolved = monolithStep == 3; HasScale = scale; PassageOpen = passage;
        }
        public int CompletedCount => (AltarSolved ? 1 : 0) + (MonolithsSolved ? 1 : 0) + (HasScale ? 1 : 0);
        public bool IsDiscovered => CompletedCount > 0;
        public bool CanOpenPassage => CompletedCount == 3 && !PassageOpen;
        public string Summary => !IsDiscovered ? string.Empty : PassageOpen ? "A trilha antiga esta aberta."
            : "Sinais antigos: " + CompletedCount + "/3" + (CompletedCount == 3 ? " / A escama reage ao norte da ilha." : "");

        public void LearnPoem() { if (KnowsPoem) return; KnowsPoem = true; Changed?.Invoke(); }

        public bool TrySolveAltar(string verse)
        {
            if (!KnowsPoem || AltarSolved || verse != LegendPoem.Flame) return false;
            AltarSolved = true; Changed?.Invoke(); return true;
        }

        public MonolithResult Activate(MonolithSymbol symbol)
        {
            if (symbol < MonolithSymbol.Sun || symbol > MonolithSymbol.Star) return MonolithResult.Invalid;
            if (MonolithsSolved) return MonolithResult.AlreadyCompleted;
            MonolithSymbol expected = MonolithStep == 0 ? MonolithSymbol.Moon : MonolithStep == 1 ? MonolithSymbol.Star : MonolithSymbol.Sun;
            if (symbol != expected) { MonolithStep = 0; Changed?.Invoke(); return MonolithResult.Reset; }
            MonolithStep++;
            if (MonolithStep == 3) { MonolithsSolved = true; Changed?.Invoke(); return MonolithResult.Completed; }
            Changed?.Invoke();
            return MonolithResult.Accepted;
        }

        public bool IsLit(MonolithSymbol symbol) => symbol == MonolithSymbol.Moon ? MonolithStep >= 1
            : symbol == MonolithSymbol.Star ? MonolithStep >= 2 : symbol == MonolithSymbol.Sun && MonolithStep == 3;

        public bool TryCollectScale()
        {
            if (HasScale) return false;
            HasScale = true; Changed?.Invoke(); return true;
        }

        public bool TryOpenPassage()
        {
            if (!CanOpenPassage) return false;
            PassageOpen = true; Changed?.Invoke(); return true;
        }
    }
}
