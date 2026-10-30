using DragonQuest.Content;
using DragonQuest.Combat;
using DragonQuest.Equipment;

namespace DragonQuest.Progression
{
    public sealed class DragonProgress
    {
        public const string EnemyId = "ancient-dragon";
        public static readonly EquipmentDefinition Armor = EquipmentCatalog.DragonArmor;
        public bool IsDefeated { get; private set; }
        internal void Restore(bool defeated) => IsDefeated = defeated;

        public bool TryComplete(BattleSession battle, PartyProgress party)
        {
            if (IsDefeated || battle == null || party == null || battle.Outcome != BattleOutcome.Victory) return false;
            int enemies = 0; bool dragon = false;
            foreach (CombatantState participant in battle.Combatants)
                if (participant.Team == CombatTeam.Enemy)
                { enemies++; dragon = participant.Id == EnemyId && !participant.IsAlive; }
            if (enemies != 1 || !dragon || !party.TryAddEquipments(new EquipmentLoadout(Armor, "dragon-armor"))) return false;
            IsDefeated = true;
            return true;
        }

        public bool HasEquippedArmor(PartyProgress party)
        {
            if (!IsDefeated || party == null) return false;
            foreach (PartyMember member in party.Members)
                if (member.Armor?.Definition == Armor) return true;
            return false;
        }
    }
}
