using DragonQuest.Content;
using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Inventory;
using DragonQuest.Progression;

internal static class LevelProgressionChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { checks++; return; }
        throw new Exception("Invalid progression or combat attribute accepted.");
    }
    private static CombatantDefinition Stats(int attack, double chance = 0, int xp = 0, int gold = 0,
        ElementalAffinities affinities = null, int speed = 20, params AbilityDefinition[] abilities)
        => new CombatantDefinition("Test", "Test", 1000, 100, attack, 4, attack, speed,
            affinities ?? ElementalAffinities.Neutral, gold, xp, chance,
            abilities.Length == 0 ? new[] { AbilityCatalog.Attack } : abilities);
    private static void Levels()
    {
        var progress = new CharacterProgression();
        Check(progress.Level == 1 && progress.Experience == 0 && progress.ExperienceToNextLevel == 100, "All heroes start at level 1.");
        Check(progress.AddExperience(99) == 0 && progress.ExperienceToNextLevel == 1, "XP before the threshold must not level up.");
        Check(progress.AddExperience(1) == 1 && progress.Level == 2 && progress.ExperienceInLevel == 0
            && progress.ExperienceToNextLevel == 150, "Exact threshold levels up and uses the next cost.");
        Check(progress.AddExperience(325) == 1 && progress.Level == 3 && progress.ExperienceInLevel == 175,
            "XP remainder must persist across levels.");
        Check(progress.AddExperience(long.MaxValue) == 96 && progress.Level == 99
            && progress.Experience == CharacterProgression.MaxExperience && progress.ExperienceToNextLevel == 0,
            "Huge XP awards cannot overflow and can cross many levels.");
        Check(progress.AddExperience(100) == 0 && progress.ExperienceInLevel == 0, "Maximum level stops growth.");
        Reject(() => progress.AddExperience(-1)); Reject(() => progress.Restore(-1));
        Reject(() => progress.Restore(CharacterProgression.MaxExperience + 1));
        for (int level = 1; level <= 99; level++)
        {
            progress.Restore(CharacterProgression.ExperienceForLevel(level));
            Check(progress.Level == level && progress.ExperienceInLevel == 0, "Each saved XP threshold must restore its level.");
        }
        var party = PartySetup.Create();
        int[,] growth = { { 10, 2, 1, 1, 1 }, { 7, 5, 1, 1, 2 }, { 14, 2, 1, 1, 1 } };
        for (int i = 0; i < 3; i++)
        {
            var member = party.Members[i];
            Check(member.Level == 1 && member.Experience == 0 && member.CriticalChance == .05, "Fresh hero critical chance is 5%.");
            // Remove materia percentages to isolate the class growth from equipment bonuses.
            member.Weapon.ClearMaterias(); member.Armor.ClearMaterias();
            int hp = member.MaxHp, mp = member.MaxMp, attack = member.Attack, defense = member.Defense, magic = member.Magic, speed = member.Speed;
            member.Progression.AddExperience(250);
            Check(member.Level == 3 && member.MaxHp == hp + 2 * growth[i, 0] && member.MaxMp == mp + 2 * growth[i, 1]
                && member.Attack == attack + 2 * growth[i, 2] && member.Defense == defense + 2 * growth[i, 3]
                && member.Magic == magic + 2 * growth[i, 4] && member.Speed == speed + 2
                && Math.Abs(member.CriticalChance - .055) < 1e-10, "Every attribute grows according to class.");
            var actor = member.CreateCombatant();
            Check(actor.Definition.MaxHp == member.MaxHp && actor.Definition.MaxMp == member.MaxMp
                && actor.Definition.Attack == member.Attack && actor.Definition.Defense == member.Defense
                && actor.Definition.Magic == member.Magic && actor.Definition.Speed == member.Speed
                && actor.Definition.CriticalChance == member.CriticalChance && actor.Hp == member.MaxHp, "New battles use evolved attributes.");
        }
        var aren = party.Members[0];
        int beforeMateria = aren.Attack;
        party.TryEquip(aren, aren.Weapon, 0, party.Materias.First(m => m.Definition == MateriaCatalog.HeavyMateria), out _);
        Check(aren.Attack == beforeMateria * 120 / 100, "Materia bonuses apply after class growth and equipment affinity.");
        party.TryRemoveEquipment(aren, EquipmentSlot.Weapon, out _);
        Check(aren.Attack == aren.BaseStats.Attack + 2 && aren.Level == 3, "Removing equipment cannot remove levels or innate growth.");
        Check(PartySetup.Create().Members.All(m => m.Level == 1 && m.Experience == 0), "New journeys reset each hero independently.");
    }
    private static void Criticals()
    {
        foreach (AbilityEffect effect in new[] { AbilityEffect.PhysicalDamage, AbilityEffect.MagicDamage, AbilityEffect.Drain })
        foreach (bool critical in new[] { false, true })
        {
            var strike = new AbilityDefinition("test", "Test", "", 0, 4, effect, TargetRule.Enemy);
            var hero = new CombatantState("hero", Stats(20, .05, abilities: new[] { strike }), CombatTeam.Party);
            var enemy = new CombatantState("enemy", Stats(1, speed: 1), CombatTeam.Enemy);
            var battle = new BattleSession(() => critical ? .04999 : .05, null, new ItemInventory(), hero, enemy);
            hero.TakeDamage(50);
            Check(battle.TryAct(hero, strike, enemy, out string message), "Critical test action must succeed.");
            Check(enemy.Hp == 1000 - (critical ? 28 : 23) && enemy.VitalChanges[0].IsCritical == critical,
                "Damage critical multiplies by exactly 1.25 and marks actual damage feedback.");
            Check(message.Contains("critico x1,25") == critical, "Critical report follows the probability threshold.");
            if (effect == AbilityEffect.Drain) Check(hero.Hp == 950 + (critical ? 28 : 23), "Drain heals the actual critical HP loss.");
        }
        foreach (int percent in new[] { 50, 100, 150 })
        {
            var affinities = percent == 50 ? new ElementalAffinities(resistances: new[] { DamageElement.Fire })
                : percent == 150 ? new ElementalAffinities(weaknesses: new[] { DamageElement.Fire }) : ElementalAffinities.Neutral;
            var fire = new AbilityDefinition("fire-test", "Fire", "", 0, 4, AbilityEffect.MagicDamage, TargetRule.Enemy, element: DamageElement.Fire);
            var hero = new CombatantState("hero", Stats(20, 1, abilities: new[] { fire }), CombatTeam.Party);
            var enemy = new CombatantState("enemy", Stats(1, affinities: affinities, speed: 1), CombatTeam.Enemy);
            var battle = new BattleSession(() => .999, null, new ItemInventory(), hero, enemy);
            enemy.IsDefending = true; enemy.GrantDamageShield(3, false);
            int expected = ((23 * percent / 100 * 125 / 100 + 1) / 2 + 1) / 2;
            Check(battle.TryAct(hero, fire, enemy, out _) && enemy.Hp == 1000 - expected,
                "Affinity, critical, defending and shield combine in the documented order.");
        }
        int rolls = 0;
        var area = AbilityCatalog.Cleave;
        var attacker = new CombatantState("hero", Stats(20, .05, abilities: new[] { area }), CombatTeam.Party);
        var first = new CombatantState("first", Stats(1, speed: 1), CombatTeam.Enemy);
        var second = new CombatantState("second", Stats(1, speed: 1), CombatTeam.Enemy);
        var areaBattle = new BattleSession(() => ++rolls == 1 ? 0 : .9, null, new ItemInventory(), attacker, first, second);
        Check(areaBattle.TryAct(attacker, area, first, out _) && rolls == 2 && first.VitalChanges[0].IsCritical
            && !second.VitalChanges[0].IsCritical, "Area attacks roll separately for each living target.");
        var giant = new CombatantState("hero", Stats(int.MaxValue, 1), CombatTeam.Party);
        var target = new CombatantState("enemy", Stats(1, speed: 1), CombatTeam.Enemy);
        Check(new BattleSession(() => 0, null, new ItemInventory(), giant, target)
            .TryAct(giant, AbilityCatalog.Attack, target, out _) && target.Hp == 0, "Critical damage saturates without overflow.");
        var zero = new CombatantState("hero", Stats(20), CombatTeam.Party);
        target = new CombatantState("enemy", Stats(1, speed: 1), CombatTeam.Enemy);
        Check(new BattleSession(() => throw new Exception("Zero chance rolled"), null, new ItemInventory(), zero, target)
            .TryAct(zero, AbilityCatalog.Attack, target, out _) && !target.VitalChanges[0].IsCritical, "Zero chance never rolls.");
        Reject(() => Stats(1, double.NaN)); Reject(() => Stats(1, double.PositiveInfinity));
        Reject(() => Stats(1, -.01)); Reject(() => Stats(1, 1.01)); Reject(() => Stats(1, xp: -1));
        Check(CombatantDefinition.CriticalDamageMultiplier == 1.25, "Critical multiplier is fixed at 1.25.");
    }
    private static BattleSession RewardBattle(PartyProgress party, params int[] experience)
    {
        var actors = party.Members.Select(m => m.CreateCombatant()).ToList();
        for (int i = 0; i < experience.Length; i++)
            actors.Add(new CombatantState("enemy-" + i, new CombatantDefinition("Enemy " + i, "", 1, 0, 1, 0, 0, 0,
                ElementalAffinities.Neutral, 1, experience[i], 0, AbilityCatalog.Attack), CombatTeam.Enemy));
        return new BattleSession(() => 1, null, party.Consumables, actors.ToArray());
    }
    private static void Win(BattleSession battle)
    {
        for (int i = 0; i < 50 && battle.Outcome == BattleOutcome.None; i++)
        {
            var actor = battle.CurrentActor;
            var attack = actor.Definition.Abilities.First(a => a.Id == "attack");
            Check(battle.TryAct(actor, attack, battle.GetValidTargets(attack)[0], out _), "Reward battle should complete normally.");
        }
        Check(battle.Outcome == BattleOutcome.Victory, "Reward fixture must win.");
    }
    private static void Rewards()
    {
        var party = PartySetup.Create(); var gold = new ExplorationProgress();
        var rewards = new BattleRewards(gold, party); var battle = RewardBattle(party, 30, 60);
        Check(!rewards.TryClaim(battle, out _) && party.Members.All(m => m.Experience == 0), "Live battles never grant XP.");
        Win(battle);
        Check(rewards.TryClaim(battle, out string message) && party.Members.All(m => m.Experience == 30)
            && message.Contains("XP") && gold.Gold == 2, "Each surviving participant receives an equal share of the enemy XP sum.");
        Check(!rewards.TryClaim(battle, out _) && !rewards.TryClaimExperience(battle, out _)
            && party.Members.All(m => m.Experience == 30), "Reopening rewards cannot duplicate XP.");
        battle = RewardBattle(party, 210); Win(battle);
        Check(rewards.TryClaim(battle, out message) && party.Members.All(m => m.Level == 2 && m.Experience == 100)
            && message.Contains("nivel 2"), "A later encounter crosses the XP threshold for all participants.");
        var full = new ExplorationProgress(int.MaxValue); rewards = new BattleRewards(full, party);
        battle = RewardBattle(party, 150); Win(battle);
        Check(!rewards.TryClaim(battle, out _) && party.Members.All(m => m.Experience == 150), "Full gold balance must not block victory XP.");
        Check(!rewards.TryClaim(battle, out _) && party.Members.All(m => m.Experience == 150), "Retrying pending gold never repeats XP.");
        full.TrySpendGold(1);
        Check(rewards.TryClaim(battle, out _) && party.Members.All(m => m.Experience == 150), "Pending gold succeeds later without more XP.");
        battle = RewardBattle(party, int.MaxValue, int.MaxValue); Win(battle);
        Check(rewards.TryClaimExperience(battle, out _) && party.Members.All(m => m.Level == 99), "Combined rewards use wide sums and cap XP.");
        party = PartySetup.Create(); rewards = new BattleRewards(new ExplorationProgress(), party);
        battle = new BattleSession(() => 1, null, party.Consumables, party.Members[0].CreateCombatant(),
            party.Members[1].CreateCombatant(), party.Members[2].CreateCombatant(),
            new CombatantState("doom", Stats(1000, xp: 100), CombatTeam.Enemy));
        battle.Combatants[0].TakeDamage(1000); battle.Combatants[1].TakeDamage(1000);
        battle.Combatants[2].TakeDamage(battle.Combatants[2].Hp - 1);
        Check(battle.TryAct(battle.CurrentActor, AbilityCatalog.Attack, battle.Combatants[2], out _)
            && battle.Outcome == BattleOutcome.Defeat && !rewards.TryClaimExperience(battle, out _)
            && party.Members.All(m => m.Experience == 0), "Defeat gives no XP.");
        var armor = new EquipmentLoadout(DragonProgress.Armor); party.TryAddEquipments(armor);
        party.TryEquipEquipment(party.Members[2], armor, out _);
        battle = EncounterCatalog.CreateCastle(party);
        var spirit = battle.Combatants.First(c => c.Id == CastleBattleRules.SpiritId); spirit.TakeDamage(spirit.Hp - 1);
        Check(battle.TryAct(battle.CurrentActor, battle.CurrentActor.Definition.Abilities.First(a => a.Id == "attack"), spirit, out _)
            && battle.Outcome == BattleOutcome.Completed && rewards.TryClaimExperience(battle, out _)
            && party.Members.Select(m => m.Experience).SequenceEqual(new[] { 167, 167, 166 }), "Freeing the king awards only defeated spirit XP, preserving final-story gold rules.");
        party = PartySetup.Create(); battle = EncounterCatalog.CreateCastle(party);
        var tyrant = battle.Combatants.First(c => c.Id == CastleBattleRules.TyrantId); tyrant.TakeDamage(tyrant.Hp - 1);
        Check(battle.TryAct(battle.CurrentActor, battle.CurrentActor.Definition.Abilities.First(a => a.Id == "attack"), tyrant, out _)
            && !new BattleRewards(new ExplorationProgress(), party).TryClaimExperience(battle, out _)
            && party.Members.All(m => m.Experience == 0), "Hero possession is not a victory XP award.");
        foreach (var type in new[] { RandomEncounterType.Pirate, RandomEncounterType.Golem, RandomEncounterType.Wolf })
        {
            var encounter = EncounterCatalog.CreateRegionEncounter(party, type, 3);
            Check(encounter.Combatants.Where(c => c.Team == CombatTeam.Enemy).All(c => c.Definition.ExperienceReward
                == (type == RandomEncounterType.Pirate ? 30 : type == RandomEncounterType.Golem ? 50 : 20)), "Region enemy types have individual XP values.");
        }
        Check(EncounterCatalog.CreateDragonEncounter(party).Combatants.Last().Definition.ExperienceReward == 300, "Dragon has boss XP.");
        var patrol = EncounterCatalog.CreateEncounter(party);
        Check(patrol.Combatants.Single(c => c.Id == "guard").Definition.ExperienceReward == 60
            && patrol.Combatants.Single(c => c.Id == "soldier").Definition.ExperienceReward == 25, "Patrol XP comes from individual enemies.");
    }
    public static int Main()
    {
        try { Levels(); Criticals(); Rewards(); Console.WriteLine(checks + " checks of levels, class growth, criticals and victory XP passed."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
