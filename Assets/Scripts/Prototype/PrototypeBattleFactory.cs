using DragonQuest.Content;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    // Monta encontros a partir das definicoes compartilhadas de conteudo.
    public static class PrototypeBattleFactory
    {
        public static readonly AbilityDefinition Attack = AbilityCatalog.Attack;
        public static readonly AbilityDefinition Defend = AbilityCatalog.Defend;
        public static readonly AbilityDefinition HeavyStrike = AbilityCatalog.HeavyStrike;
        public static readonly AbilityDefinition Heal = AbilityCatalog.Heal;
        public static readonly AbilityDefinition Cleave = AbilityCatalog.Cleave;
        public static readonly AbilityDefinition Cleanse = AbilityCatalog.Cleanse;
        public static readonly AbilityDefinition Revive = AbilityCatalog.Revive;
        public static readonly AbilityDefinition Taunt = AbilityCatalog.Taunt;
        public static readonly AbilityDefinition Protect = AbilityCatalog.Protect;
        public static readonly AbilityDefinition PoisonStrike = AbilityCatalog.PoisonStrike;
        public static readonly AbilityDefinition EnemySweep = AbilityCatalog.EnemySweep;
        public static readonly AbilityDefinition DragonClaw = AbilityCatalog.DragonClaw;
        public static readonly AbilityDefinition ChargeBreath = AbilityCatalog.ChargeBreath;
        public static readonly AbilityDefinition DragonBreath = AbilityCatalog.DragonBreath;

        public static BattleSession CreateDragonEncounter(PartyProgress party) => EncounterCatalog.CreateDragonEncounter(party);
        public static BattleSession CreateRegionEncounter(PartyProgress party, RandomEncounterType type, int enemyCount)
            => EncounterCatalog.CreateRegionEncounter(party, type, enemyCount);
        public static BattleSession CreateEncounter(PartyProgress party = null) => EncounterCatalog.CreateEncounter(party);
    }
}
