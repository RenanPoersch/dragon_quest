using System.Collections.Generic;
using DragonQuest.Content;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Progression;

namespace DragonQuest.Authoring
{
    // Sample party used only by authoring and standalone checks.
    public static class PartyFixture
    {
        public static PartyProgress Create()
        {
            var aren = CharacterCatalog.CreateAren();
            var lia = CharacterCatalog.CreateLia();
            var bram = CharacterCatalog.CreateBram();
            var inventory = new List<MateriaInstance>();
            foreach (MateriaDefinition definition in new[] { MateriaCatalog.HeavyMateria, MateriaCatalog.CleaveMateria, MateriaCatalog.ThunderMateria, MateriaCatalog.AllMateria,
                MateriaCatalog.CureMateria, MateriaCatalog.CleanseMateria, MateriaCatalog.ReviveMateria, MateriaCatalog.SummonMateria, MateriaCatalog.GuardianMateria, MateriaCatalog.InfuseMateria, MateriaCatalog.AllMateria, MateriaCatalog.ThunderMateria,
                MateriaCatalog.FireMateria, MateriaCatalog.IceMateria, MateriaCatalog.EarthMateria, MateriaCatalog.ChaosMateria, MateriaCatalog.BahamutMateria, MateriaCatalog.TitanMateria, MateriaCatalog.DefenseMateria, MateriaCatalog.HpMateria, MateriaCatalog.MpMateria })
                inventory.Add(new MateriaInstance("materia-" + inventory.Count, definition));
            var spareEquipment = new[]
            {
                new EquipmentLoadout(EquipmentCatalog.IronSword, "iron-sword"),
                new EquipmentLoadout(EquipmentCatalog.RunicStaff, "runic-staff"),
                new EquipmentLoadout(EquipmentCatalog.IronArmor, "iron-armor"),
                new EquipmentLoadout(EquipmentCatalog.ArcaneRobe, "arcane-robe")
            };
            var consumables = new ItemInventory(new ItemStack(ItemCatalog.Potion, 6), new ItemStack(ItemCatalog.Ether, 3),
                new ItemStack(ItemCatalog.Antidote, 3), new ItemStack(ItemCatalog.PhoenixFeather, 2));
            var party = new PartyProgress(new[] { aren, lia, bram }, inventory.ToArray(), spareEquipment, consumables);
            party.TryEquip(aren, aren.Weapon, 0, inventory[0], out _);
            party.TryEquip(aren, aren.Armor, 0, inventory[1], out _);
            party.TryEquip(aren, aren.Armor, 2, inventory[2], out _);
            party.TryEquip(aren, aren.Armor, 3, inventory[3], out _);
            party.TryEquip(lia, lia.Weapon, 0, inventory[4], out _);
            party.TryEquip(lia, lia.Armor, 0, inventory[5], out _);
            party.TryEquip(lia, lia.Armor, 1, inventory[6], out _);
            party.TryEquip(lia, lia.Armor, 2, inventory[7], out _);
            party.TryEquip(bram, bram.Weapon, 0, inventory[8], out _);
            return party;
        }
    }
}
