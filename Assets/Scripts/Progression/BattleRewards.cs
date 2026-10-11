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
        public BattleRewards(ExplorationProgress explorationProgress)
        {
            progress = explorationProgress ?? throw new ArgumentNullException(nameof(explorationProgress));
        }
        public bool HasClaimed(BattleSession battle) => battle != null && claimed.Contains(battle);
        public bool TryClaim(BattleSession battle, out string message)
        {
            if (battle == null || battle.Outcome != BattleOutcome.Victory) { message = "Recompensa exige uma batalha vencida."; return false; }
            if (claimed.Contains(battle)) { message = "Recompensa desta batalha ja recebida."; return false; }
            long total = 0;
            var breakdown = new List<string>();
            foreach (CombatantState enemy in battle.Combatants)
            {
                if (enemy.Team != CombatTeam.Enemy || enemy.IsAlive) continue;
                total += enemy.Definition.GoldReward;
                breakdown.Add(enemy.Definition.Name + " (" + enemy.Definition.GoldReward + ")");
            }
            if (total > int.MaxValue || (total > 0 && !progress.TryAddGold((int)total)))
            { message = "Nao foi possivel receber ouro: saldo no limite."; return false; }
            claimed.Add(battle);
            message = "+" + total + " moedas: " + string.Join(" + ", breakdown) + ".";
            return true;
        }
    }
}
