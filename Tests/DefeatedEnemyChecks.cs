using DragonQuest.Presentation.Battle;
using System;
using DragonQuest.Combat;

internal static class DefeatedEnemyChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static readonly AbilityDefinition Strike = new AbilityDefinition("strike", "Golpe", "", 4, 20, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
    private static readonly AbilityDefinition Blast = new AbilityDefinition("blast", "Explosao", "", 5, 20, AbilityEffect.MagicDamage, TargetRule.AllEnemies);
    private static readonly AbilityDefinition Revive = new AbilityDefinition("revive", "Reviver", "", 3, 30, AbilityEffect.Revive, TargetRule.Ally);
    private static CombatantState Fighter(string id, CombatTeam team, int speed, int hp = 120)
        => new CombatantState(id, new CombatantDefinition(id, "", hp, 30, 30, 0, 25, speed, Strike, Blast, Revive), team);

    private static void Retarget()
    {
        var firstHero = Fighter("first-hero", CombatTeam.Party, 30);
        var secondHero = Fighter("second-hero", CombatTeam.Party, 20);
        var first = Fighter("enemy-1", CombatTeam.Enemy, 3, 10);
        var second = Fighter("enemy-2", CombatTeam.Enemy, 2);
        var battle = new BattleSession(firstHero, secondHero, first, second);
        Check(battle.TryAct(firstHero, Strike, first, out _), "First lethal strike failed");
        Check(!first.IsAlive && second.Hp == 120, "Lethal hit must not hit two enemies");
        Check(battle.CurrentActor == secondHero, "Wrong turn after lethal hit");
        Check(battle.NextLivingEnemyAfter(first) == second, "Cursor did not advance to surviving enemy");
        Check(battle.GetValidTargets(Strike).Count == 1 && battle.GetValidTargets(Strike)[0] == second, "Dead enemy selectable");
        Check(battle.ResolveLivingEnemyTarget(Strike, first) == second, "Stale selection not advanced");
        Check(battle.TryAct(secondHero, Strike, first, out _), "Stale hit not redirected");
        Check(first.Hp == 0 && second.Hp < 120, "Redirected hit damaged corpse or missed survivor");
        Check(secondHero.Mp == 26 && secondHero.TurnsTaken == 1, "Redirect spent extra MP/turns");
    }
    private static void FormationAndInvalidTargets()
    {
        var hero = Fighter("hero", CombatTeam.Party, 30);
        var ally = Fighter("ally", CombatTeam.Party, 20);
        var first = Fighter("enemy-1", CombatTeam.Enemy, 3);
        var second = Fighter("enemy-2", CombatTeam.Enemy, 2);
        var third = Fighter("enemy-3", CombatTeam.Enemy, 1);
        var battle = new BattleSession(hero, ally, first, second, third);
        first.TakeDamage(120); second.TakeDamage(120);
        Check(battle.ResolveLivingEnemyTarget(Strike, first) == third, "Did not skip two corpses");
        Check(battle.NextLivingEnemyAfter(first) == third, "Cursor did not skip two corpses");
        Check(battle.ResolveLivingEnemyTarget(Strike, second) == third, "Wrong next survivor");
        hero.TauntedBy = third; hero.TauntTurns = 2;
        Check(battle.ResolveLivingEnemyTarget(Strike, first) == third, "Taunt ignored by redirect");
        Check(!battle.TryAct(hero, Strike, ally, out _), "Live ally accepted as damage target");
        var foreign = Fighter("foreign", CombatTeam.Enemy, 1); foreign.TakeDamage(120);
        Check(!battle.TryAct(hero, Strike, foreign, out _), "Foreign corpse redirected");
        Check(!battle.TryAct(hero, Strike, null, out _), "Null target silently redirected");
        Check(!battle.TryAct(ally, Strike, first, out _), "Out-of-turn redirect allowed");
        Check(hero.Mp == 30 && hero.TurnsTaken == 0, "Invalid selections spent resources");
        third.TakeDamage(120);
        Check(battle.ResolveLivingEnemyTarget(Strike, first) == null, "No survivors should produce no target");
        Check(battle.NextLivingEnemyAfter(first) == null, "Cursor selected a corpse with no survivors");
        Check(!battle.TryAct(hero, Strike, first, out _), "Hit accepted with no survivors");
        Check(hero.Mp == 30 && hero.TurnsTaken == 0, "No survivors spent resources");

        first = Fighter("enemy-1", CombatTeam.Enemy, 3); second = Fighter("enemy-2", CombatTeam.Enemy, 2); third = Fighter("enemy-3", CombatTeam.Enemy, 1);
        battle = new BattleSession(Fighter("hero-2", CombatTeam.Party, 30), first, second, third);
        third.TakeDamage(120);
        Check(battle.ResolveLivingEnemyTarget(Strike, third) == first, "Formation redirect failed to wrap");
        Check(battle.NextLivingEnemyAfter(third) == first, "Cursor failed to wrap formation order");
    }
    private static void AreaAndRevive()
    {
        var hero = Fighter("hero", CombatTeam.Party, 30);
        var first = Fighter("enemy-1", CombatTeam.Enemy, 3);
        var second = Fighter("enemy-2", CombatTeam.Enemy, 2);
        var third = Fighter("enemy-3", CombatTeam.Enemy, 1);
        var battle = new BattleSession(hero, first, second, third);
        first.TakeDamage(120);
        Check(battle.TryAct(hero, Blast, first, out _), "Area ability not redirected from stale seed");
        Check(first.Hp == 0 && first.VitalChanges.Count == 1, "Area attack hit corpse");
        Check(second.Hp < 120 && third.Hp < 120, "Area attack missed survivor");
        Check(hero.Mp == 25 && hero.TurnsTaken == 1, "Area attack spent multiple actions");

        hero = Fighter("hero", CombatTeam.Party, 30);
        var ally = Fighter("ally", CombatTeam.Party, 20);
        var enemy = Fighter("enemy", CombatTeam.Enemy, 1);
        battle = new BattleSession(hero, ally, enemy);
        ally.TakeDamage(120);
        Check(battle.ResolveLivingEnemyTarget(Revive, ally) == ally, "Revive target redirected");
        Check(battle.TryAct(hero, Revive, ally, out _), "Reviving dead ally broken");
        Check(ally.IsAlive && ally.Hp == 36, "Incorrect ally revival");

        hero = Fighter("hero", CombatTeam.Party, 30); enemy = Fighter("enemy", CombatTeam.Enemy, 1, 10);
        battle = new BattleSession(hero, enemy);
        Check(battle.TryAct(hero, Strike, enemy, out _) && battle.Outcome == BattleOutcome.Victory, "Last hit did not end battle");
        int mp = hero.Mp;
        Check(!battle.TryAct(hero, Strike, enemy, out _) && hero.Mp == mp, "Hit accepted after victory");
    }
    private static void Disappearance()
    {
        var enemy = Fighter("enemy", CombatTeam.Enemy, 1);
        var hero = Fighter("hero", CombatTeam.Party, 2);
        var presentation = new DefeatedEnemyPresentation();
        presentation.Observe(enemy, 5);
        Check(presentation.IsVisible(enemy, 20) && !presentation.IsRed(enemy, 20), "Living enemy changed");
        enemy.TakeDamage(120);
        presentation.Observe(enemy, 5);
        Check(presentation.Alpha(enemy, 4.9f) == 1 && !presentation.IsRed(enemy, 4.9f), "Enemy disappeared before impact");
        Check(presentation.Alpha(enemy, 5.1f) == 1 && presentation.IsRed(enemy, 5.1f), "Missing red death flash");
        Check(presentation.Alpha(enemy, 5.3f) > 0 && presentation.Alpha(enemy, 5.3f) < 1, "Missing fade");
        presentation.Observe(enemy, 6);
        Check(!presentation.IsVisible(enemy, 6), "Repeated observation restarted death animation");
        Check(presentation.Alpha(enemy, 6) == 0 && !presentation.IsRed(enemy, 6), "Dead enemy still drawn");
        Check(DefeatedEnemyPresentation.EndOfAction(5.4f, 5, new[] { enemy }) >= 5.55f, "Final blow exits before disappearance");
        Check(DefeatedEnemyPresentation.EndOfAction(6, 5, new[] { enemy }) == 6, "Long animation shortened");
        Check(DefeatedEnemyPresentation.EndOfAction(5.4f, 5, new[] { hero }) == 5.4f, "Living target extended animation");
        hero.TakeDamage(120); presentation.Observe(hero, 5);
        Check(presentation.IsVisible(hero, 8) && !presentation.IsRed(hero, 8), "Dead ally removed from battle");
        enemy.RecoverHp(30); presentation.Observe(enemy, 7);
        Check(presentation.IsVisible(enemy, 7) && presentation.Alpha(enemy, 7) == 1, "Restored fighter remains hidden");
        enemy.TakeDamage(30); presentation.Observe(enemy, 8);
        Check(presentation.IsRed(enemy, 8), "Second death did not get fresh flash");
        presentation.Reset(); presentation.Observe(enemy, 10);
        Check(presentation.IsVisible(enemy, 10), "New battle did not reset presentation");
    }
    public static void Main() { Retarget(); FormationAndInvalidTargets(); AreaAndRevive(); Disappearance(); Console.WriteLine(checks + " verificacoes de alvos derrotados e desaparecimento passaram."); }
}
