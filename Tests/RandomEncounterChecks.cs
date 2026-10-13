using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using DragonQuest.Progression;
using DragonQuest.Exploration;

internal static class RandomEncounterChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static void Meter()
    {
        Check(RandomEncounterRules.TypeAt("world") == RandomEncounterType.Pirate, "World pirates");
        Check(RandomEncounterRules.TypeAt("west-forest") == RandomEncounterType.Golem, "Quarry golems");
        Check(RandomEncounterRules.TypeAt("east-forest") == RandomEncounterType.Wolf, "Forest wolves");
        foreach (string map in new[] { "city", "castle", "inn", "volcano", "ancient-ruins", "shop", null })
            Check(RandomEncounterRules.TypeAt(map) == RandomEncounterType.None, "Safe map " + map);
        var meter = new RandomEncounterRules(() => 0);
        meter.Reset("world");
        for (int i = 0; i < 40; i++)
        {
            Check(!meter.Advance("world", 0, false, false), "Stationary encounter");
            Check(!meter.Advance("world", 1, true, false), "Paused encounter");
        }
        Check(!meter.Advance("world", 50, false, false), "Teleport-sized displacement counted");
        Check(!meter.Advance("world", float.NaN, false, false), "NaN distance");
        for (int i = 0; i < 17; i++) Check(!meter.Advance("world", 1, false, false), "Premature world encounter");
        Check(meter.Advance("world", 1, false, false), "Missing world encounter");
        Check(!meter.Advance("world", 1, false, false), "Immediate repeated encounter");
        Check(!meter.Advance("east-forest", 1, false, false), "Travel triggered encounter");
        for (int i = 0; i < 8; i++) Check(!meter.Advance("east-forest", 1, false, false), "Premature forest encounter");
        Check(meter.Advance("east-forest", 1, false, false), "Forest encounter missing");
        Check(!meter.Advance("east-forest", 1, false, true), "Save load triggered encounter");
        for (int i = 0; i < 8; i++) Check(!meter.Advance("east-forest", 1, false, false), "Teleport grace missing");
        Check(meter.Advance("east-forest", 1, false, false), "Teleport grace never ends");
        var maximum = new RandomEncounterRules(() => 1);
        maximum.Reset("world");
        for (int i = 0; i < 29; i++) Check(!maximum.Advance("world", 1, false, false), "Random maximum ignored");
        Check(maximum.Advance("world", 1, false, false), "Random maximum encounter missing");
    }
    private static void Encounters()
    {
        foreach (RandomEncounterType type in new[] { RandomEncounterType.Pirate, RandomEncounterType.Golem, RandomEncounterType.Wolf })
            for (int count = 1; count <= 3; count++)
            {
                var party = PrototypePartyFactory.Create();
                BattleSession battle = PrototypeBattleFactory.CreateRegionEncounter(party, type, count);
                int enemies = 0;
                var ids = new HashSet<string>();
                foreach (CombatantState fighter in battle.Combatants)
                {
                    Check(ids.Add(fighter.Id), "Duplicate combatant ID");
                    if (fighter.Team != CombatTeam.Enemy) continue;
                    enemies++;
                    Check(BattleAnimationCatalog.Idle(fighter.Id) != null, "Missing enemy idle animation");
                    Check(BattleAnimationCatalog.Resource(fighter.Id, PrototypeBattleFactory.Attack) != null, "Missing enemy attack");
                }
                Check(enemies == count, "Wrong group size");
                Check(object.ReferenceEquals(battle.Inventory, party.Consumables), "Inventory not shared");
                int turns = 0;
                while (battle.Outcome == BattleOutcome.None && turns++ < 300)
                {
                    if (battle.CurrentActor.Team == CombatTeam.Enemy) Check(EnemyAI.TryTakeTurn(battle, out _), "Enemy cannot act");
                    else
                    {
                        AbilityDefinition attack = null;
                        foreach (AbilityDefinition known in battle.CurrentActor.Definition.Abilities)
                            if (known.SourceAbilityId == "attack") { attack = known; break; }
                        var targets = battle.GetValidTargets(attack);
                        Check(battle.TryAct(battle.CurrentActor, attack, targets[0], out string message), "Hero cannot act: " + message);
                    }
                }
                Check(battle.Outcome != BattleOutcome.None, "Encounter never ends");
                if (battle.Outcome == BattleOutcome.Victory)
                {
                    var progress = new ExplorationProgress();
                    var rewards = new BattleRewards(progress);
                    Check(rewards.TryClaim(battle, out _), "Missing reward");
                    Check(!rewards.TryClaim(battle, out _), "Duplicate reward");
                }
            }
        try { PrototypeBattleFactory.CreateRegionEncounter(PrototypePartyFactory.Create(), RandomEncounterType.None, 1); throw new Exception("Invalid type allowed"); }
        catch (ArgumentOutOfRangeException) { checks++; }
        try { PrototypeBattleFactory.CreateRegionEncounter(PrototypePartyFactory.Create(), RandomEncounterType.Wolf, 4); throw new Exception("Invalid group allowed"); }
        catch (ArgumentOutOfRangeException) { checks++; }
    }
    private static void Animations()
    {
        Check(BattleAnimationCatalog.Resource("tyrant", CastleBattleRules.RoyalStrike) == "TyrantRoyalStrike", "Royal strike registration");
        Check(BattleAnimationCatalog.Resource("tyrant", CastleBattleRules.RoyalHeavy) == "TyrantRoyalHeavy", "Heavy registration");
        Check(BattleAnimationCatalog.Resource("tyrant", CastleBattleRules.PrepareMeteor) == "TyrantMeteorCharge", "Charge registration");
        Check(BattleAnimationCatalog.Duration("tyrant", CastleBattleRules.Meteor) == 1.6f, "Meteor duration");
        Check(BattleAnimationCatalog.Resource("evil-spirit", PrototypeCastleBattleFactory.DrainLife) == "SpiritBattle", "Spirit cast registration");
        foreach (string id in new[] { "aren", "lia", "bram", "guard", "soldier", "pirate-1", "golem-2", "wolf-3", "tyrant", "evil-spirit" })
            Check(BattleAnimationCatalog.Idle(id) != null, "Missing fighter " + id);
        Check(BattleAnimationCatalog.Resource("aren", CastleBattleRules.RoyalStrike) == null, "Enemy action incorrectly mapped to hero");
        foreach (string resource in new[] { "TyrantRoyalStrike", "TyrantRoyalHeavy", "TyrantMeteorCharge", "TyrantFlamingMeteor" })
        {
            for (int i = 0; i < 4; i++) Check(BattleAnimationCatalog.Frame(resource, i * .25f + .01f, 1) == i, "Sequential frame timing");
            Check(BattleAnimationCatalog.Frame(resource, -1, 1) == 0, "Negative time");
            Check(BattleAnimationCatalog.Frame(resource, 5, 1) == 3, "Animation overrun");
        }
    }
    private static void Main() { Meter(); Encounters(); Animations(); Console.WriteLine(checks + " verificacoes de encontros e animacoes passaram."); }
}
