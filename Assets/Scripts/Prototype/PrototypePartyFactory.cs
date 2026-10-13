using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Prototype
{
    public static class PrototypePartyFactory
    {
        public static readonly EquipmentDefinition InitialWeapon = new EquipmentDefinition("Arma inicial", 5, 0, 2);
        public static readonly EquipmentDefinition InitialArmor = new EquipmentDefinition("Armadura inicial", 0, 6, 4, EquipmentSlot.Armor);
        public static readonly EquipmentDefinition IronSword = new EquipmentDefinition("Espada de ferro", 9, 0, 2);
        public static readonly EquipmentDefinition RunicStaff = new EquipmentDefinition("Cajado runico", 3, 0, 4, magicBonus: 6);
        public static readonly EquipmentDefinition IronArmor = new EquipmentDefinition("Armadura de ferro", 0, 10, 2, EquipmentSlot.Armor);
        public static readonly EquipmentDefinition ArcaneRobe = new EquipmentDefinition("Manto arcano", 0, 3, 6, EquipmentSlot.Armor, 4);
        public static readonly ConsumableDefinition Potion = new ConsumableDefinition("potion", "Pocao", "Recupera 40 HP de um aliado vivo e ferido. Usa uma unidade e um turno.", ItemEffect.RestoreHp, 40);
        public static readonly ConsumableDefinition Ether = new ConsumableDefinition("ether", "Eter", "Recupera 15 MP de um aliado vivo. Usa uma unidade e um turno.", ItemEffect.RestoreMp, 15);
        public static readonly ConsumableDefinition Antidote = new ConsumableDefinition("antidote", "Antidoto", "Remove veneno de um aliado vivo. Usa uma unidade e um turno.", ItemEffect.CleansePoison, 0);
        public static readonly ConsumableDefinition PhoenixFeather = new ConsumableDefinition("phoenix-feather", "Pluma da Fenix", "Revive um aliado derrotado com 30% do HP maximo. Usa uma unidade e um turno.", ItemEffect.Revive, 30);
        public static readonly AbilityDefinition Thunder = new AbilityDefinition("thunder", "Thunder",
            "Magia de Raio contra um inimigo, poder 22. Custa 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy,
            element: DamageElement.Lightning);
        public static readonly AbilityDefinition Fire = new AbilityDefinition("fire", "Fogo",
            "Magia de Fogo contra um inimigo, poder 22. Custa 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy,
            element: DamageElement.Fire);
        public static readonly AbilityDefinition Ice = new AbilityDefinition("ice", "Gelo",
            "Magia de Gelo contra um inimigo, poder 22. Custa 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy,
            element: DamageElement.Ice);
        public static readonly AbilityDefinition Summon = new AbilityDefinition("summon", "Invocar Fenix",
            "Invoca a Fenix com dano de Fogo, poder 35. Custa 12 MP.", 12, 35, AbilityEffect.MagicDamage, TargetRule.Enemy,
            element: DamageElement.Fire);
        public static readonly MateriaDefinition ThunderMateria = new MateriaDefinition("thunder", "Raio", "Verde: concede Thunder.", MateriaType.Magic, abilities: new[] { Thunder });
        public static readonly MateriaDefinition FireMateria = new MateriaDefinition("fire", "Fogo", "Verde: magia de Fogo, poder 22, 6 MP. Fraqueza x1,5; resistencia x0,5.", MateriaType.Magic, abilities: new[] { Fire });
        public static readonly MateriaDefinition IceMateria = new MateriaDefinition("ice", "Gelo", "Verde: magia de Gelo, poder 22, 6 MP. Fraqueza x1,5; resistencia x0,5.", MateriaType.Magic, abilities: new[] { Ice });
        public static readonly MateriaDefinition CureMateria = new MateriaDefinition("cure", "Cura", "Verde: concede Curar.", MateriaType.Magic, abilities: new[] { PrototypeBattleFactory.Heal });
        public static readonly MateriaDefinition CleanseMateria = new MateriaDefinition("cleanse", "Purificacao", "Verde: remove veneno.", MateriaType.Magic, abilities: new[] { PrototypeBattleFactory.Cleanse });
        public static readonly MateriaDefinition ReviveMateria = new MateriaDefinition("revive", "Vida", "Verde: concede Reviver.", MateriaType.Magic, abilities: new[] { PrototypeBattleFactory.Revive });
        public static readonly MateriaDefinition SummonMateria = new MateriaDefinition("phoenix", "Fenix", "Vermelha: concede Invocar Fenix. A arte da invocacao ainda e provisoria.", MateriaType.Summon, abilities: new[] { Summon });
        public static readonly MateriaDefinition AllMateria = new MateriaDefinition("all", "All", "Azul: ligada a verde/vermelha, aplica o comando a todos os alvos validos; MP x1,5. Sozinha nao concede comando.", MateriaType.Support, MateriaModifier.All);
        public static readonly MateriaDefinition InfuseMateria = new MateriaDefinition("infuse", "Infusao", "Roxa: ligada a magia elemental, infunde o ataque basico na arma (0 MP) ou concede resistencia de 50% na armadura. Primeiro par valido define o elemento da arma.", MateriaType.PhysicalEnhance, MateriaModifier.Infuse);
        public static readonly MateriaDefinition HeavyMateria = new MateriaDefinition("heavy", "Forca", "Roxa: aumenta ataque em 20%. Nao concede comando.", MateriaType.PhysicalEnhance, MateriaModifier.AttackUp);
        public static readonly MateriaDefinition CleaveMateria = new MateriaDefinition("cleave", "Corte", "Roxa: concede Corte amplo a qualquer personagem.", MateriaType.PhysicalEnhance, abilities: new[] { PrototypeBattleFactory.Cleave });
        public static readonly MateriaDefinition GuardianMateria = new MateriaDefinition("guardian", "Guardiao", "Roxa: concede Provocar e Proteger a qualquer personagem.", MateriaType.PhysicalEnhance, abilities: new[] { PrototypeBattleFactory.Taunt, PrototypeBattleFactory.Protect });

        public static readonly AbilityDefinition Earth = new AbilityDefinition("earth", "Terra", "Magia de Terra, poder 22, 6 MP.", 6, 22, AbilityEffect.MagicDamage, TargetRule.Enemy, element: DamageElement.Earth);
        public static readonly AbilityDefinition Chaos = new AbilityDefinition("chaos", "Caos", "Magia de Caos, poder 28, 8 MP.", 8, 28, AbilityEffect.MagicDamage, TargetRule.Enemy, element: DamageElement.Chaos);
        public static readonly AbilityDefinition Bahamut = new AbilityDefinition("bahamut", "Invocar Bahamut", "Dano sem elemento em todos os inimigos, poder 50, 20 MP. Arte provisoria.", 20, 50, AbilityEffect.MagicDamage, TargetRule.AllEnemies);
        public static readonly AbilityDefinition Titan = new AbilityDefinition("titan", "Invocar Titan", "Dano de Terra em todos os inimigos, poder 40, 16 MP. Arte provisoria.", 16, 40, AbilityEffect.MagicDamage, TargetRule.AllEnemies, element: DamageElement.Earth);
        public static readonly MateriaDefinition EarthMateria = new MateriaDefinition("earth", "Terra", "Verde: concede Terra.", MateriaType.Magic, abilities: new[] { Earth });
        public static readonly MateriaDefinition ChaosMateria = new MateriaDefinition("chaos", "Caos", "Verde: concede Caos.", MateriaType.Magic, abilities: new[] { Chaos });
        public static readonly MateriaDefinition BahamutMateria = new MateriaDefinition("bahamut", "Bahamut", "Vermelha: invoca Bahamut, 20 MP, todos os inimigos, sem elemento. Arte provisoria.", MateriaType.Summon, abilities: new[] { Bahamut });
        public static readonly MateriaDefinition TitanMateria = new MateriaDefinition("titan", "Titan", "Vermelha: invoca Titan, 16 MP, todos os inimigos, Terra. Arte provisoria.", MateriaType.Summon, abilities: new[] { Titan });
        public static readonly MateriaDefinition DefenseMateria = new MateriaDefinition("defense", "Defesa", "Roxa: aumenta defesa em 20%.", MateriaType.PhysicalEnhance, MateriaModifier.DefenseUp);
        public static readonly MateriaDefinition HpMateria = new MateriaDefinition("hp", "HP", "Roxa: aumenta HP maximo em 20%.", MateriaType.PhysicalEnhance, MateriaModifier.HpUp);
        public static readonly MateriaDefinition MpMateria = new MateriaDefinition("mp", "MP", "Roxa: aumenta MP maximo em 20%.", MateriaType.PhysicalEnhance, MateriaModifier.MpUp);
        public static PartyProgress Create()
        {
            var aren = new PartyMember("aren", "Aren", CharacterClass.Attacker,
                new CombatantDefinition("Aren", "Atacante", 100, 28, 22, 7, 4, 14, MateriaResolver.Attack, MateriaResolver.Defend), InitialWeapon, InitialArmor);
            var lia = new PartyMember("lia", "Lia", CharacterClass.Healer,
                new CombatantDefinition("Lia", "Healer", 80, 60, 9, 5, 20, 12, MateriaResolver.Attack, MateriaResolver.Defend), InitialWeapon, InitialArmor);
            var bram = new PartyMember("bram", "Bram", CharacterClass.Tank,
                new CombatantDefinition("Bram", "Tank", 150, 30, 14, 13, 3, 8, MateriaResolver.Attack, MateriaResolver.Defend), InitialWeapon, InitialArmor);
            var materias = new[] { new MateriaInstance("start-heavy", HeavyMateria), new MateriaInstance("start-thunder", ThunderMateria),
                new MateriaInstance("start-cure", CureMateria), new MateriaInstance("start-cleanse", CleanseMateria), new MateriaInstance("start-guardian", GuardianMateria) };
            var party = new PartyProgress(new[] { aren, lia, bram }, materias, consumables:
                new ItemInventory(new ItemStack(Potion, 2), new ItemStack(Antidote, 1)));
            party.TryEquip(aren, aren.Weapon, 0, materias[0], out _);
            party.TryEquip(aren, aren.Armor, 0, materias[1], out _);
            party.TryEquip(lia, lia.Weapon, 0, materias[2], out _);
            party.TryEquip(lia, lia.Armor, 0, materias[3], out _);
            party.TryEquip(bram, bram.Weapon, 0, materias[4], out _);
            return party;
        }

        // Fixture das entregas 5/6A para manter os testes de combos e troca independentes da economia inicial.
        public static PartyProgress CreateTestParty()
        {
            var weapon = InitialWeapon;
            var armor = InitialArmor;
            var aren = new PartyMember("aren", "Aren", CharacterClass.Attacker,
                new CombatantDefinition("Aren", "Atacante", 100, 28, 22, 7, 4, 14, MateriaResolver.Attack, MateriaResolver.Defend), weapon, armor);
            var lia = new PartyMember("lia", "Lia", CharacterClass.Healer,
                new CombatantDefinition("Lia", "Healer", 80, 60, 9, 5, 20, 12, MateriaResolver.Attack, MateriaResolver.Defend), weapon, armor);
            var bram = new PartyMember("bram", "Bram", CharacterClass.Tank,
                new CombatantDefinition("Bram", "Tank", 150, 30, 14, 13, 3, 8, MateriaResolver.Attack, MateriaResolver.Defend), weapon, armor);
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
