using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Prototype
{
    public static class PrototypePartyFactory
    {
        public static readonly ConsumableDefinition Potion = new ConsumableDefinition("potion", "Pocao", "Recupera 40 HP de um aliado vivo e ferido. Usa uma unidade e um turno.", ItemEffect.RestoreHp, 40);
        public static readonly ConsumableDefinition Ether = new ConsumableDefinition("ether", "Eter", "Recupera 15 MP de um aliado vivo. Usa uma unidade e um turno.", ItemEffect.RestoreMp, 15);
        public static readonly ConsumableDefinition Antidote = new ConsumableDefinition("antidote", "Antidoto", "Remove veneno de um aliado vivo. Usa uma unidade e um turno.", ItemEffect.CleansePoison, 0);
        public static readonly ConsumableDefinition PhoenixFeather = new ConsumableDefinition("phoenix-feather", "Pluma da Fenix", "Revive um aliado derrotado com 30% do HP maximo. Usa uma unidade e um turno.", ItemEffect.Revive, 30);
        public static readonly AbilityDefinition Thunder = new AbilityDefinition("thunder", "Thunder",
            "Magia de raio contra um inimigo, poder 22. Custa 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy);
        public static readonly AbilityDefinition Summon = new AbilityDefinition("summon", "Invocar Fenix",
            "Invoca a Fenix para atingir um inimigo, poder 35. Custa 12 MP.", 12, 35, AbilityEffect.MagicDamage, TargetRule.Enemy);
        public static readonly MateriaDefinition ThunderMateria = new MateriaDefinition("thunder", "Raio", "Verde: concede Thunder.", MateriaType.Magic, abilities: new[] { Thunder });
        public static readonly MateriaDefinition CureMateria = new MateriaDefinition("cure", "Cura", "Verde: concede Curar.", MateriaType.Magic, abilities: new[] { PrototypeBattleFactory.Heal });
        public static readonly MateriaDefinition CleanseMateria = new MateriaDefinition("cleanse", "Purificacao", "Verde: remove veneno.", MateriaType.Magic, abilities: new[] { PrototypeBattleFactory.Cleanse });
        public static readonly MateriaDefinition ReviveMateria = new MateriaDefinition("revive", "Vida", "Verde: concede Reviver.", MateriaType.Magic, abilities: new[] { PrototypeBattleFactory.Revive });
        public static readonly MateriaDefinition SummonMateria = new MateriaDefinition("phoenix", "Fenix", "Vermelha: concede Invocar Fenix. A arte da invocacao ainda e provisoria.", MateriaType.Summon, abilities: new[] { Summon });
        public static readonly MateriaDefinition AllMateria = new MateriaDefinition("all", "All", "Azul: ligada a verde/vermelha, aplica o comando a todos os alvos validos; MP x1,5. Sozinha nao concede comando.", MateriaType.Support, MateriaModifier.All);
        public static readonly MateriaDefinition InfuseMateria = new MateriaDefinition("infuse", "Infusao", "Roxa: ligada a magia/invocacao de dano, converte em golpe fisico. Usa ataque; nao modifica cura. Sozinha nao concede comando.", MateriaType.PhysicalEnhance, MateriaModifier.Infuse);
        public static readonly MateriaDefinition HeavyMateria = new MateriaDefinition("heavy", "Forca", "Roxa: concede Golpe forte a qualquer personagem.", MateriaType.PhysicalEnhance, abilities: new[] { PrototypeBattleFactory.HeavyStrike });
        public static readonly MateriaDefinition CleaveMateria = new MateriaDefinition("cleave", "Corte", "Roxa: concede Corte amplo a qualquer personagem.", MateriaType.PhysicalEnhance, abilities: new[] { PrototypeBattleFactory.Cleave });
        public static readonly MateriaDefinition GuardianMateria = new MateriaDefinition("guardian", "Guardiao", "Roxa: concede Provocar e Proteger a qualquer personagem.", MateriaType.PhysicalEnhance, abilities: new[] { PrototypeBattleFactory.Taunt, PrototypeBattleFactory.Protect });

        public static PartyProgress Create()
        {
            var weapon = new EquipmentDefinition("Arma inicial", 5, 0, 2);
            var armor = new EquipmentDefinition("Armadura inicial", 0, 6, 4, EquipmentSlot.Armor);
            var aren = new PartyMember("aren", "Aren", CharacterClass.Attacker,
                new CombatantDefinition("Aren", "Atacante", 100, 28, 22, 7, 4, 14, MateriaResolver.Attack, MateriaResolver.Defend), weapon, armor);
            var lia = new PartyMember("lia", "Lia", CharacterClass.Healer,
                new CombatantDefinition("Lia", "Healer", 80, 60, 9, 5, 20, 12, MateriaResolver.Attack, MateriaResolver.Defend), weapon, armor);
            var bram = new PartyMember("bram", "Bram", CharacterClass.Tank,
                new CombatantDefinition("Bram", "Tank", 150, 30, 14, 13, 3, 8, MateriaResolver.Attack, MateriaResolver.Defend), weapon, armor);
            var inventory = new List<MateriaInstance>();
            foreach (MateriaDefinition definition in new[] { HeavyMateria, CleaveMateria, ThunderMateria, AllMateria,
                CureMateria, CleanseMateria, ReviveMateria, SummonMateria, GuardianMateria, InfuseMateria, AllMateria, ThunderMateria })
                inventory.Add(new MateriaInstance("materia-" + inventory.Count, definition));
            var spareEquipment = new[]
            {
                new EquipmentLoadout(new EquipmentDefinition("Espada de ferro", 9, 0, 2), "iron-sword"),
                new EquipmentLoadout(new EquipmentDefinition("Cajado runico", 3, 0, 4, magicBonus: 6), "runic-staff"),
                new EquipmentLoadout(new EquipmentDefinition("Armadura de ferro", 0, 10, 2, EquipmentSlot.Armor), "iron-armor"),
                new EquipmentLoadout(new EquipmentDefinition("Manto arcano", 0, 3, 6, EquipmentSlot.Armor, 4), "arcane-robe")
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
