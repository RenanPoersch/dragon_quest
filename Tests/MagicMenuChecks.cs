using DragonQuest.Progression;
using DragonQuest.Content;
using DragonQuest.Authoring;
using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

internal static class MagicMenuChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static PartyProgress Party()
    {
        var party = PartyFixture.Create();
        party.Members[0].RestoreVitals(10, 20, true, 0);
        party.Members[1].RestoreVitals(10, 60, false, 0);
        party.Members[2].RestoreVitals(0, 0, false, 0);
        return party;
    }
    private static AbilityDefinition Spell(PartyMember caster, string source)
        => MateriaResolver.Resolve(caster).Single(a => a.SourceAbilityId == source);
    private static MagicMenuSession Menu(PartyProgress party, string source)
    {
        var menu = new MagicMenuSession(); menu.Reset(party); menu.SelectCaster(1, party); menu.ConfirmCaster();
        for (int i = 0; i < menu.Spells.Count; i++) if (menu.Spells[i].SourceAbilityId == source) menu.SelectSpell(i);
        return menu;
    }
    private static void Selection()
    {
        var party = Party(); var caster = party.Members[1]; var target = party.Members[0];
        var menu = new MagicMenuSession(); menu.Reset(party);
        Check(menu.ChoosingCaster && menu.Spells.Count == 0, "Offensive and physical materia cannot appear in field Magic.");
        menu.Move(-1, party); Check(menu.CasterIndex == 2, "Caster selection wraps backward.");
        menu.Move(-1, party); Check(menu.CasterIndex == 1 && menu.Spells.Count == 3, "Lia lists equipped Heal, Cleanse and Revive.");
        menu.ConfirmCaster();
        Check(!menu.ChoosingCaster && !menu.ChoosingTarget && !menu.ConfirmTarget(party), "Selecting a caster cannot cast a spell.");
        int mp = caster.Mp, hp = target.Hp, stock = party.Consumables.QuantityOf(ItemCatalog.Potion);
        menu.Move(-1, party); Check(menu.SelectedSpell.SourceAbilityId == "revive", "Spell selection wraps backward.");
        menu.Move(1, party); Check(menu.BeginUse(party), "Confirming Heal opens portrait targeting.");
        Check(caster.Mp == mp && target.Hp == hp, "Entering targets spends no MP and restores no HP.");
        menu.Move(-1, party); Check(menu.TargetIndex == 2, "Portrait selection wraps backward.");
        Check(!menu.ConfirmTarget(party) && caster.Mp == mp && menu.ChoosingTarget, "Heal cannot consume MP on a KO target.");
        menu.SelectTarget(0, party);
        Check(menu.Cancel() && !menu.ChoosingTarget && caster.Mp == mp, "Cancelling targeting spends no MP.");
        menu.BeginUse(party);
        Check(menu.ConfirmTarget(party) && target.Hp == hp + Spell(caster, "heal").Power + caster.Magic
            && caster.Mp == mp - 6, "A valid heal restores battle-equivalent HP and charges the caster six MP.");
        Check(menu.ChoosingTarget && party.Consumables.QuantityOf(ItemCatalog.Potion) == stock, "Repeated casting remains in targets and consumes no items.");
        menu.SelectTarget(1, party); mp = caster.Mp; hp = caster.Hp;
        Check(menu.ConfirmTarget(party) && caster.Hp > hp && caster.Mp == mp - 6, "Self-healing persists both HP and MP changes.");
        Check(menu.Cancel() && menu.Cancel() && menu.ChoosingCaster && !menu.Cancel(), "Cancel returns target, spell, caster, then overview one step at a time.");
        menu.Reset(party); Check(menu.CasterIndex == 0 && menu.SpellIndex == 0 && menu.Message == "", "Reopening Magic resets focus and feedback.");
    }
    private static void EffectsAndFailures()
    {
        var party = Party(); var caster = party.Members[1]; var target = party.Members[0];
        int events = 0; party.Changed += () => events++;
        var heal = Spell(caster, "heal");
        target.RestoreVitals(target.MaxHp, 20, true, 0);
        int mp = caster.Mp;
        Check(!party.TryCastMagic(caster, heal.Id, target, out _) && caster.Mp == mp && events == 0, "Full HP rejects healing without resources or events.");
        var cleanse = Menu(party, "cleanse"); cleanse.BeginUse(party);
        Check(cleanse.ConfirmTarget(party) && !target.IsPoisoned && caster.Mp == mp - 4 && events == 1, "Purify removes poison for four MP and notifies once.");
        mp = caster.Mp;
        Check(!cleanse.ConfirmTarget(party) && caster.Mp == mp, "A clean target cannot waste purify MP.");
        var revive = Menu(party, "revive"); revive.BeginUse(party);
        Check(!revive.ConfirmTarget(party) && caster.Mp == mp, "Revive rejects living targets.");
        revive.SelectTarget(2, party);
        Check(revive.ConfirmTarget(party) && party.Members[2].Hp == (party.Members[2].MaxHp * 30 + 99) / 100
            && caster.Mp == mp - AbilityCatalog.Revive.MpCost, "Revive restores its configured HP percentage and deducts its configured MP cost.");
        target.RestoreVitals(1, 20, true, 1); mp = caster.Mp;
        Check(party.TryCastMagic(caster, heal.Id, target, out _) && target.BoundActions == 0, "Recovery also breaks the target's chains, matching battle recovery.");
        caster.RestoreVitals(caster.Hp, 5, false, 0);
        var menu = Menu(party, "heal");
        Check(!menu.BeginUse(party) && menu.Message.Contains("MP") && caster.Mp == 5, "Insufficient MP blocks targeting.");
        caster.RestoreVitals(0, 60, false, 0);
        Check(!party.TryCastMagic(caster, heal.Id, target, out _), "A KO caster cannot cast.");
        caster.RestoreVitals(1, 60, false, 1);
        Check(!party.TryCastMagic(caster, heal.Id, target, out _), "A bound caster cannot cast.");
        caster.RestoreVitals(1, 60, false, 0);
        Check(!party.TryCastMagic(caster, AbilityCatalog.Heal.Id, target, out _), "Base spell IDs cannot bypass equipped-materia ownership.");
        Check(!party.TryCastMagic(caster, heal.Id, PartySetup.Create().Members[0], out _), "An outsider cannot receive the party's spells.");
        Check(!party.TryCastMagic(PartySetup.Create().Members[1], heal.Id, target, out _), "An outsider cannot spend the party's MP.");
        Check(!party.TryCastMagic(caster, HeroUltimates.ForHero("lia").Id, target, out _), "Field Magic cannot cast a limit break.");
        Check(!PartyProgress.IsFieldMagic(AbilityCatalog.Summon) && !PartyProgress.IsFieldMagic(AbilityCatalog.Fire), "Summons and attack spells are excluded.");
        menu = Menu(party, "heal"); menu.BeginUse(party);
        var materia = party.Materias.Single(m => m.Definition == MateriaCatalog.CureMateria);
        party.TryUnequip(caster, caster.Weapon, 0, out _);
        int hp = target.Hp; mp = caster.Mp;
        Check(!menu.ConfirmTarget(party) && target.Hp == hp && caster.Mp == mp, "Unequipping the source revokes a pending cast without spending MP.");
        menu.Refresh(party); Check(!menu.ChoosingTarget && !menu.Spells.Any(a => a.SourceAbilityId == "heal"), "Refreshing removes revoked spells and target focus.");
    }
    private static void AllTargets()
    {
        var party = Party(); var caster = party.Members[1];
        var all = party.Materias.First(m => m.Definition == MateriaCatalog.AllMateria);
        party.TryEquip(caster, caster.Weapon, 1, all, out _);
        var menu = Menu(party, "heal");
        Check(menu.SelectedSpell.Targets == TargetRule.AllAllies && menu.SelectedSpell.MpCost == 9, "Linked All exposes the resolved group spell and cost.");
        menu.BeginUse(party); menu.SelectTarget(2, party); int mp = caster.Mp;
        Check(menu.ConfirmTarget(party) && caster.Mp == mp - 9 && party.Members[0].Hp > 10 && caster.Hp > 10 && party.Members[2].Hp == 0,
            "All heals every eligible living ally, even from a KO portrait, and charges MP once.");
        foreach (var member in party.Members) member.RestoreVitals(member.IsAlive ? member.MaxHp : 0, member.Mp, false, 0);
        mp = caster.Mp;
        Check(!menu.ConfirmTarget(party) && caster.Mp == mp, "All with no valid target consumes no MP.");
        // Move All to Cleanse's linked slot, then Revive's linked slot.
        party.TryEquip(caster, caster.Armor, 1, all, out _);
        party.Members[0].RestoreVitals(40, 20, true, 0); caster.RestoreVitals(40, 60, true, 0);
        menu = Menu(party, "cleanse"); menu.BeginUse(party); mp = caster.Mp;
        Check(menu.ConfirmTarget(party) && !caster.IsPoisoned && !party.Members[0].IsPoisoned && caster.Mp == mp - 6,
            "All purifies every poisoned ally for one increased cost.");
        var reviveMateria = party.Materias.Single(m => m.Definition == MateriaCatalog.ReviveMateria);
        party.TryEquip(caster, caster.Weapon, 0, reviveMateria, out _); party.TryEquip(caster, caster.Weapon, 1, all, out _);
        party.Members[0].RestoreVitals(0, 0, false, 0);
        menu = Menu(party, "revive"); menu.BeginUse(party); mp = caster.Mp;
        Check(menu.ConfirmTarget(party) && party.Members[0].IsAlive && party.Members[2].IsAlive && caster.Mp == mp - 18,
            "All revives each KO ally and charges its resolved cost once.");
        party.TryEquip(caster, caster.Weapon, 0, party.Materias.Single(m => m.Definition == MateriaCatalog.CureMateria), out _);
        party.TryUnequip(caster, caster.Weapon, 1, out _);
        caster.RestoreVitals(1, 6, false, 0);
        menu = Menu(party, "heal"); menu.BeginUse(party); menu.SelectTarget(1, party);
        Check(menu.ConfirmTarget(party) && caster.Mp == 0 && !menu.ChoosingTarget, "Exhausting MP exits targets so another confirmation cannot cast.");
        var empty = new PartyProgress(new PartyMember[0], new MateriaInstance[0]); menu.Reset(empty); menu.Move(1, empty);
        Check(menu.Caster(empty) == null && menu.Spells.Count == 0 && !menu.BeginUse(empty), "An empty party remains safe to navigate.");
    }
    public static int Main()
    {
        try { Selection(); EffectsAndFailures(); AllTargets(); Console.WriteLine("MAGIC_MENU_CHECKS_OK: " + checks + " checks passed."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
