using DragonQuest.Combat;
using DragonQuest.Equipment;

namespace DragonQuest.Content
{
    public static class EquipmentCatalog
    {
        public static readonly EquipmentDefinition InitialWeapon = new EquipmentDefinition("Arma inicial", 5, 0, 2);
        public static readonly EquipmentDefinition InitialArmor = new EquipmentDefinition("Armadura inicial", 0, 6, 4, EquipmentSlot.Armor);
        public static readonly EquipmentDefinition IronSword = new EquipmentDefinition("Espada de ferro", 9, 0, 2);
        public static readonly EquipmentDefinition RunicStaff = new EquipmentDefinition("Cajado runico", 3, 0, 4, magicBonus: 6);
        public static readonly EquipmentDefinition IronArmor = new EquipmentDefinition("Armadura de ferro", 0, 10, 2, EquipmentSlot.Armor);
        public static readonly EquipmentDefinition ArcaneRobe = new EquipmentDefinition("Manto arcano", 0, 3, 6, EquipmentSlot.Armor, 4);
        public static readonly EquipmentDefinition PowerRing = new EquipmentDefinition("Anel da Forca", 4, 0, 0, EquipmentSlot.Accessory);
        public static readonly EquipmentDefinition GuardianBracelet = new EquipmentDefinition("Bracelete Guardiao", 0, 4, 0, EquipmentSlot.Accessory);
        public static readonly EquipmentDefinition ArcanePendant = new EquipmentDefinition("Pingente Arcano", 0, 0, 0, EquipmentSlot.Accessory, 4);
        public static readonly EquipmentDefinition DragonArmor = new EquipmentDefinition("Armadura draconica", 0, 16, 4,
            EquipmentSlot.Armor, affinities: new ElementalAffinities(resistances: new[] { DamageElement.Fire }), isStoryItem: true);
    }
}
