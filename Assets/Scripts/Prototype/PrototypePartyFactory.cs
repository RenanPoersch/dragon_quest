using DragonQuest.Content;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Prototype
{
    // Compatibility fields share catalog instances; the factory only assembles a party.
    public static class PrototypePartyFactory
    {
        public static readonly EquipmentDefinition InitialWeapon = EquipmentCatalog.InitialWeapon;
        public static readonly EquipmentDefinition InitialArmor = EquipmentCatalog.InitialArmor;
        public static readonly EquipmentDefinition IronSword = EquipmentCatalog.IronSword;
        public static readonly EquipmentDefinition RunicStaff = EquipmentCatalog.RunicStaff;
        public static readonly EquipmentDefinition IronArmor = EquipmentCatalog.IronArmor;
        public static readonly EquipmentDefinition ArcaneRobe = EquipmentCatalog.ArcaneRobe;
        public static readonly EquipmentDefinition PowerRing = EquipmentCatalog.PowerRing;
        public static readonly EquipmentDefinition GuardianBracelet = EquipmentCatalog.GuardianBracelet;
        public static readonly EquipmentDefinition ArcanePendant = EquipmentCatalog.ArcanePendant;
        public static readonly ConsumableDefinition Potion = ItemCatalog.Potion;
        public static readonly ConsumableDefinition Ether = ItemCatalog.Ether;
        public static readonly ConsumableDefinition Antidote = ItemCatalog.Antidote;
        public static readonly ConsumableDefinition PhoenixFeather = ItemCatalog.PhoenixFeather;
        public static readonly AbilityDefinition Thunder = AbilityCatalog.Thunder;
        public static readonly AbilityDefinition Fire = AbilityCatalog.Fire;
        public static readonly AbilityDefinition Ice = AbilityCatalog.Ice;
        public static readonly AbilityDefinition Summon = AbilityCatalog.Summon;
        public static readonly MateriaDefinition ThunderMateria = MateriaCatalog.ThunderMateria;
        public static readonly MateriaDefinition FireMateria = MateriaCatalog.FireMateria;
        public static readonly MateriaDefinition IceMateria = MateriaCatalog.IceMateria;
        public static readonly MateriaDefinition CureMateria = MateriaCatalog.CureMateria;
        public static readonly MateriaDefinition CleanseMateria = MateriaCatalog.CleanseMateria;
        public static readonly MateriaDefinition ReviveMateria = MateriaCatalog.ReviveMateria;
        public static readonly MateriaDefinition SummonMateria = MateriaCatalog.SummonMateria;
        public static readonly MateriaDefinition AllMateria = MateriaCatalog.AllMateria;
        public static readonly MateriaDefinition InfuseMateria = MateriaCatalog.InfuseMateria;
        public static readonly MateriaDefinition HeavyMateria = MateriaCatalog.HeavyMateria;
        public static readonly MateriaDefinition CleaveMateria = MateriaCatalog.CleaveMateria;
        public static readonly MateriaDefinition GuardianMateria = MateriaCatalog.GuardianMateria;

        public static readonly AbilityDefinition Earth = AbilityCatalog.Earth;
        public static readonly AbilityDefinition Chaos = AbilityCatalog.Chaos;
        public static readonly AbilityDefinition Bahamut = AbilityCatalog.Bahamut;
        public static readonly AbilityDefinition Titan = AbilityCatalog.Titan;
        public static readonly MateriaDefinition EarthMateria = MateriaCatalog.EarthMateria;
        public static readonly MateriaDefinition ChaosMateria = MateriaCatalog.ChaosMateria;
        public static readonly MateriaDefinition BahamutMateria = MateriaCatalog.BahamutMateria;
        public static readonly MateriaDefinition TitanMateria = MateriaCatalog.TitanMateria;
        public static readonly MateriaDefinition DefenseMateria = MateriaCatalog.DefenseMateria;
        public static readonly MateriaDefinition HpMateria = MateriaCatalog.HpMateria;
        public static readonly MateriaDefinition MpMateria = MateriaCatalog.MpMateria;
        public static PartyProgress Create() => PartySetup.Create();

        // Fixture das entregas 5/6A para manter os testes de combos e troca independentes da economia inicial.
        public static PartyProgress CreateTestParty()
        {
            var aren = CharacterCatalog.CreateAren();
            var lia = CharacterCatalog.CreateLia();
            var bram = CharacterCatalog.CreateBram();
            var inventory = new List<MateriaInstance>();
            foreach (MateriaDefinition definition in new[] { HeavyMateria, CleaveMateria, ThunderMateria, AllMateria,
                CureMateria, CleanseMateria, ReviveMateria, SummonMateria, GuardianMateria, InfuseMateria, AllMateria, ThunderMateria,
                FireMateria, IceMateria, EarthMateria, ChaosMateria, BahamutMateria, TitanMateria, DefenseMateria, HpMateria, MpMateria })
                inventory.Add(new MateriaInstance("materia-" + inventory.Count, definition));
            var spareEquipment = new[]
            {
                new EquipmentLoadout(IronSword, "iron-sword"),
                new EquipmentLoadout(RunicStaff, "runic-staff"),
                new EquipmentLoadout(IronArmor, "iron-armor"),
                new EquipmentLoadout(ArcaneRobe, "arcane-robe")
            };
            var consumables = new ItemInventory(new ItemStack(Potion, 6), new ItemStack(Ether, 3),
                new ItemStack(Antidote, 3), new ItemStack(PhoenixFeather, 2));
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
