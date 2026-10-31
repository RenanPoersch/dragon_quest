using DragonQuest.Progression;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Content
{
    // Each journey receives independent members, inventory and equipment.
    public static class PartySetup
    {
        public static PartyProgress Create()
        {
            var aren = CharacterCatalog.CreateAren();
            var lia = CharacterCatalog.CreateLia();
            var bram = CharacterCatalog.CreateBram();
            var materias = new[] { new MateriaInstance("start-heavy", MateriaCatalog.HeavyMateria), new MateriaInstance("start-thunder", MateriaCatalog.ThunderMateria),
                new MateriaInstance("start-cure", MateriaCatalog.CureMateria), new MateriaInstance("start-cleanse", MateriaCatalog.CleanseMateria), new MateriaInstance("start-guardian", MateriaCatalog.GuardianMateria) };
            var party = new PartyProgress(new[] { aren, lia, bram }, materias, consumables:
                new ItemInventory(new ItemStack(ItemCatalog.Potion, 2), new ItemStack(ItemCatalog.Antidote, 1)));
            party.TryEquip(aren, aren.Weapon, 0, materias[0], out _);
            party.TryEquip(aren, aren.Armor, 0, materias[1], out _);
            party.TryEquip(lia, lia.Weapon, 0, materias[2], out _);
            party.TryEquip(lia, lia.Armor, 0, materias[3], out _);
            party.TryEquip(bram, bram.Weapon, 0, materias[4], out _);
            return party;
        }
    }
}
