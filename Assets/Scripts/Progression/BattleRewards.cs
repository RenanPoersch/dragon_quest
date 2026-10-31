using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DragonQuest.Combat;
using DragonQuest.Exploration;

namespace DragonQuest.Progression
{
    public sealed class BattleRewards
    {
        private sealed class Receipt
        {
            public bool GoldClaimed;
            public bool ExperienceClaimed;
            public string ExperienceMessage;
        }
        // Receipts live as long as their battle; completing encounters must not retain old sessions.
        private readonly ConditionalWeakTable<BattleSession, Receipt> receipts = new ConditionalWeakTable<BattleSession, Receipt>();
        private readonly ExplorationProgress progress;
        private readonly PartyProgress party;
        public BattleRewards(ExplorationProgress explorationProgress, PartyProgress party = null)
        {
            progress = explorationProgress ?? throw new ArgumentNullException(nameof(explorationProgress));
            this.party = party;
        }
        public bool TryClaimExperience(BattleSession battle, out string message)
        {
            bool won = battle != null && (battle.Outcome == BattleOutcome.Victory
                || battle.Outcome == BattleOutcome.Completed && battle.Rules is CastleBattleRules castle && castle.Ending == GameEnding.KingFreed);
            if (!won || receipts.TryGetValue(battle, out Receipt previous) && previous.ExperienceClaimed)
            { message = "XP exige uma vitoria ainda nao recompensada."; return false; }
            long experience = 0;
            foreach (CombatantState enemy in battle.Combatants)
                if (enemy.Team == CombatTeam.Enemy && !enemy.IsAlive) experience += enemy.Definition.ExperienceReward;
            message = party?.AwardExperience(battle, experience) ?? string.Empty;
            Receipt receipt = receipts.GetValue(battle, _ => new Receipt());
            receipt.ExperienceClaimed = true;
            receipt.ExperienceMessage = message;
            return true;
        }

        public bool HasClaimed(BattleSession battle) => battle != null && receipts.TryGetValue(battle, out Receipt receipt) && receipt.GoldClaimed;
        public bool TryClaim(BattleSession battle, out string message)
        {
            if (battle == null || battle.Outcome != BattleOutcome.Victory) { message = "Recompensa exige uma batalha vencida."; return false; }
            if (HasClaimed(battle)) { message = "Recompensa desta batalha ja recebida."; return false; }
            long total = 0;
            var breakdown = new List<string>();
            foreach (CombatantState enemy in battle.Combatants)
            {
                if (enemy.Team != CombatTeam.Enemy || enemy.IsAlive) continue;
                total += enemy.Definition.GoldReward;
                breakdown.Add(enemy.Definition.Name + " (" + enemy.Definition.GoldReward + ")");
            }
            // XP e entregue mesmo com saldo de ouro cheio; repetir o pagamento pendente nao duplica XP.
            Receipt receipt = receipts.GetValue(battle, _ => new Receipt());
            if (!receipt.ExperienceClaimed) TryClaimExperience(battle, out _);
            string experienceMessage = receipt.ExperienceMessage ?? string.Empty;
            if (total > int.MaxValue || (total > 0 && !progress.TryAddGold((int)total)))
            { message = "Nao foi possivel receber ouro: saldo no limite. " + experienceMessage; return false; }
            receipt.GoldClaimed = true;
            message = "+" + total + " moedas: " + string.Join(" + ", breakdown) + "." + (experienceMessage.Length > 0 ? " " + experienceMessage : "");
            return true;
        }
    }
}
