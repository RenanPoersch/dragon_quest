using DragonQuest.Content;
using DragonQuest.Combat;
using DragonQuest.Equipment;

namespace DragonQuest.Prototype
{
    public static class PrototypeCastleBattleFactory
    {
        public static readonly AbilityDefinition DrainLife = AbilityCatalog.DrainLife;
        public static readonly AbilityDefinition MindControl = AbilityCatalog.MindControl;
        public static readonly AbilityDefinition Silence = AbilityCatalog.Silence;
        public static readonly AbilityDefinition Chains = AbilityCatalog.Chains;
        public static readonly AbilityDefinition ChaosMagic = AbilityCatalog.ChaosMagic;
        public static BattleSession Create(PartyProgress party) => EncounterCatalog.CreateCastle(party);
    }
}
