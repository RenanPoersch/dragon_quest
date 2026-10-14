using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Equipment;

namespace DragonQuest.Progression
{
    public sealed class BattleRewards
    {
        private readonly HashSet<BattleSession> claimed = new HashSet<BattleSession>();
        private readonly ExplorationProgress progress;
        private readonly PartyProgress party;
        private readonly Dictionary<BattleSession, string> experienceClaims = new Dictionary<BattleSession, string>();
        public BattleRewards(ExplorationProgress explorationProgress, PartyProgress party = null)
        {
            progress = explorationProgress ?? throw new ArgumentNullException(nameof(explorationProgress));
            this.party = party;
        }
        public bool TryClaimExperience(BattleSession battle, out string message)
        {
            bool won = battle != null && (battle.Outcome == BattleOutcome.Victory
                || battle.Outcome == BattleOutcome.Completed && battle.Rules is CastleBattleRules castle && castle.Ending == GameEnding.KingFreed);
            if (!won || experienceClaims.ContainsKey(battle))
            { message = "XP exige uma vitoria ainda nao recompensada."; return false; }
            long experience = 0;
            foreach (CombatantState enemy in battle.Combatants)
                if (enemy.Team == CombatTeam.Enemy && !enemy.IsAlive) experience += enemy.Definition.ExperienceReward;
            message = party?.AwardExperience(battle, experience) ?? string.Empty;
            experienceClaims.Add(battle, message);
            return true;
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
            // XP e entregue mesmo com saldo de ouro cheio; repetir o pagamento pendente nao duplica XP.
            if (!experienceClaims.TryGetValue(battle, out string experienceMessage))
            {
                TryClaimExperience(battle, out experienceMessage);
            }
            if (total > int.MaxValue || (total > 0 && !progress.TryAddGold((int)total)))
            { message = "Nao foi possivel receber ouro: saldo no limite. " + experienceMessage; return false; }
            claimed.Add(battle);
            message = "+" + total + " moedas: " + string.Join(" + ", breakdown) + "." + (experienceMessage.Length > 0 ? " " + experienceMessage : "");
            return true;
        }
    }
}
