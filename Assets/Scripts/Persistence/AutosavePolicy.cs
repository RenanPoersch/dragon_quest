using DragonQuest.Combat;

namespace DragonQuest.Persistence
{
    // Alteracoes durante um combate so sao gravadas depois de vitoria/desfecho.
    public sealed class AutosavePolicy
    {
        public bool Pending { get; private set; }
        public void Changed(bool battleActive) { if (!battleActive) Pending = true; }
        public void BattleCompleted(BattleOutcome outcome)
        { Pending = outcome == BattleOutcome.Victory || outcome == BattleOutcome.Completed; }
        public void Acknowledge() => Pending = false;
    }
}
