using DragonQuest.Content;
using DragonQuest.Authoring;
using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Persistence;

public static class MateriaRevisionChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Main()
    {
        var party = PartyFixture.Create();
        var hero = party.Members[0];
        var magic = party.Materias.First(m => m.Definition == MateriaCatalog.FireMateria);
        var infuse = party.Materias.First(m => m.Definition == MateriaCatalog.InfuseMateria);
        foreach (var equipment in new[] { hero.Weapon, hero.Armor }) equipment.ClearMaterias();
        hero.Weapon.Set(0, magic); hero.Weapon.Set(1, infuse);
        var commands = MateriaResolver.Resolve(hero);
        var attack = commands.Single(a => a.Id == "attack");
        Check(attack.Element == DamageElement.Fire && attack.MpCost == 0 && attack.Effect == AbilityEffect.PhysicalDamage && attack.Power == 0, "Infusion must affect free basic attack.");
        Check(commands.Single(a => a.SourceAbilityId == "fire").Effect == AbilityEffect.MagicDamage, "Original spell stays magical.");
        hero.Weapon.ClearMaterias(); hero.Armor.Set(0, magic); hero.Armor.Set(1, infuse);
        Check(hero.Affinities.DamagePercent(DamageElement.Fire) == 50, "Armor grants resistance.");
        Check(MateriaResolver.Resolve(hero)[0].Element == DamageElement.None, "Armor does not infuse attack.");
        hero.Armor.ClearMaterias();
        int baseAttack = hero.Attack, baseDefense = hero.Defense;
        hero.Weapon.Set(0, new MateriaInstance("str", MateriaCatalog.HeavyMateria));
        hero.Weapon.Set(1, new MateriaInstance("def", MateriaCatalog.DefenseMateria));
        hero.Armor.Set(0, new MateriaInstance("hp", MateriaCatalog.HpMateria));
        hero.Armor.Set(1, new MateriaInstance("mp", MateriaCatalog.MpMateria));
        Check(hero.Attack == baseAttack * 120 / 100 && hero.Defense == baseDefense * 120 / 100, "Stat bonuses.");
        Check(hero.MaxHp == 120 && hero.MaxMp == 33 && hero.CreateCombatant().Definition.MaxHp == 120, "HP/MP bonuses reach combat.");
        Check(!MateriaResolver.Resolve(hero).Any(a => a.SourceAbilityId == "heavy-strike"), "Strength grants no command.");
        var added = new[] { MateriaCatalog.EarthMateria, MateriaCatalog.ChaosMateria,
            MateriaCatalog.BahamutMateria, MateriaCatalog.TitanMateria,
            MateriaCatalog.DefenseMateria, MateriaCatalog.HpMateria, MateriaCatalog.MpMateria };
        var shop = ShopCatalog.Create(party, new ExplorationProgress()).FindShop("materia-shop");
        foreach (var definition in added)
        {
            Check(SaveCatalog.Materia(definition.Id) == definition, "Save catalog: " + definition.Id);
            Check(shop.Products.Any(p => p.Id == definition.Id), "Shop: " + definition.Id);
        }
        foreach (var definition in added.Take(4))
        {
            hero.Weapon.ClearMaterias(); hero.Weapon.Set(0, new MateriaInstance(definition.Id, definition));
            Check(MateriaResolver.Resolve(hero).Any(a => a.SourceAbilityId == definition.Abilities[0].Id), "New command available.");
        }
        hero.Weapon.ClearMaterias(); hero.Armor.ClearMaterias();
        Check(hero.Attack == baseAttack && hero.Defense == baseDefense && hero.MaxHp == 100 && hero.MaxMp == 28, "Unequip restores stats.");
        Console.WriteLine("Materia revision checks passed.");
    }
}
