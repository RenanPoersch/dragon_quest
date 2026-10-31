using DragonQuest.Progression;
using DragonQuest.Combat;

namespace DragonQuest.Content
{
    public static class CharacterCatalog
    {
        public const string ArenId = "aren", LiaId = "lia", BramId = "bram";
        public static readonly CombatantDefinition Aren = new CombatantDefinition("Aren", "Atacante",
            100, 28, 22, 7, 4, 14, ElementalAffinities.Neutral, 0, 0, .05, AbilityCatalog.Attack, AbilityCatalog.Defend);
        public static readonly CombatantDefinition Lia = new CombatantDefinition("Lia", "Healer",
            80, 60, 9, 5, 20, 12, ElementalAffinities.Neutral, 0, 0, .05, AbilityCatalog.Attack, AbilityCatalog.Defend);
        public static readonly CombatantDefinition Bram = new CombatantDefinition("Bram", "Tank",
            150, 30, 14, 13, 3, 8, ElementalAffinities.Neutral, 0, 0, .05, AbilityCatalog.Attack, AbilityCatalog.Defend);

        public static PartyMember CreateAren() => Create(ArenId, "Aren", CharacterClass.Attacker, Aren);
        public static PartyMember CreateLia() => Create(LiaId, "Lia", CharacterClass.Healer, Lia);
        public static PartyMember CreateBram() => Create(BramId, "Bram", CharacterClass.Tank, Bram);

        private static PartyMember Create(string id, string name, CharacterClass role, CombatantDefinition stats)
            => new PartyMember(id, name, role, stats, EquipmentCatalog.InitialWeapon, EquipmentCatalog.InitialArmor);
    }
}
