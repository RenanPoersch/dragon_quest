using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Exploration;

namespace DragonQuest.Progression
{
    public sealed class BattleRewards
    {
        private readonly HashSet<BattleSession> claimed = new HashSet<BattleSession>();
        private readonly ExplorationProgress progress;
        public int GoldPerVictory { get; }
        public BattleRewards(ExplorationProgress explorationProgress, int goldPerVictory = 35)
        {
            progress = explorationProgress ?? throw new ArgumentNullException(nameof(explorationProgress));
            if (goldPerVictory <= 0) throw new ArgumentOutOfRangeException(nameof(goldPerVictory));
            GoldPerVictory = goldPerVictory;
        }
        public bool HasClaimed(BattleSession battle) => battle != null && claimed.Contains(battle);
        public bool TryClaim(BattleSession battle, out string message)
        {
            if (battle == null || battle.Outcome != BattleOutcome.Victory) { message = "Recompensa exige uma batalha vencida."; return false; }
            if (claimed.Contains(battle)) { message = "Recompensa desta batalha ja recebida."; return false; }
            if (!progress.TryAddGold(GoldPerVictory)) { message = "Nao foi possivel receber ouro: saldo no limite."; return false; }
            claimed.Add(battle);
            message = "+" + GoldPerVictory + " moedas pela vitoria.";
            return true;
        }
    }
}
