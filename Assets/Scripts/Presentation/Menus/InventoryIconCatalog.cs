using DragonQuest.Content;
using System.Collections.Generic;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Presentation.Menus
{
    // Equipment uses definition identity, never the instance ID or translated display name.
    public static class InventoryIconCatalog
    {
        private static readonly Dictionary<EquipmentDefinition, string> equipment = new Dictionary<EquipmentDefinition, string>
        {
            { EquipmentCatalog.InitialWeapon, "initial-weapon" },
            { EquipmentCatalog.IronSword, "iron-sword" },
            { EquipmentCatalog.RunicStaff, "runic-staff" },
            { EquipmentCatalog.InitialArmor, "initial-armor" },
            { EquipmentCatalog.IronArmor, "iron-armor" },
            { EquipmentCatalog.ArcaneRobe, "arcane-robe" },
            { EquipmentCatalog.PowerRing, "power-ring" },
            { EquipmentCatalog.GuardianBracelet, "guardian-bracelet" },
            { EquipmentCatalog.ArcanePendant, "arcane-pendant" },
            { EquipmentCatalog.DragonArmor, "dragon-armor" }
        };

        public static string Key(EquipmentDefinition definition) => definition != null
            && equipment.TryGetValue(definition, out string key) ? key : null;

        public static string Key(MateriaDefinition definition)
        {
            if (definition == null) return null;
            switch (definition.Type)
            {
                case MateriaType.Magic: return "materia-green";
                case MateriaType.Support: return "materia-blue";
                case MateriaType.Summon: return "materia-red";
                case MateriaType.PhysicalEnhance: return "materia-purple";
                default: return null;
            }
        }

        public static string Key(ConsumableDefinition definition)
        {
            if (definition == null) return null;
            switch (definition.Id)
            {
                case "potion": case "ether": case "antidote": case "phoenix-feather": return definition.Id;
                default: return null;
            }
        }
    }
}
