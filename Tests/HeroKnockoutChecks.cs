using System;
using DragonQuest.Combat;

internal static class HeroKnockoutChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static CombatantState Hero(string id, CombatTeam team = CombatTeam.Party)
        => new CombatantState(id, new CombatantDefinition(id, "", 100, 20, 20, 5, 10, 10,
            new AbilityDefinition("attack", "Attack", "", 0, 10, AbilityEffect.PhysicalDamage, TargetRule.Enemy)), team);

    private static void Lifecycle(string id)
    {
        var hero = Hero(id);
        var poses = new HeroKnockoutPresentation();
        poses.Observe(hero, 0, 0);
        Check(!poses.IsKnockedOut(hero), "Healthy hero must stand: " + id);
        hero.TakeDamage(99); poses.Observe(hero, 1, 1);
        Check(!poses.IsKnockedOut(hero), "One HP must not kneel: " + id);
        hero.TakeDamage(1); poses.Observe(hero, 5, 4);
        Check(hero.Hp == 0 && !poses.IsKnockedOut(hero), "Lethal hit must wait for impact: " + id);
        poses.Observe(hero, 5, 5);
        Check(poses.IsKnockedOut(hero), "Kneel at impact: " + id);
        poses.Observe(hero, 20, 6);
        Check(poses.IsKnockedOut(hero), "Repeated observations must retain KO: " + id);
        hero.RecoverHp(30); poses.Observe(hero, 10, 9);
        Check(poses.IsKnockedOut(hero), "Revival must wait for its impact: " + id);
        poses.Observe(hero, 10, 10);
        Check(!poses.IsKnockedOut(hero), "Revived hero must stand: " + id);
        hero.TakeDamage(30); poses.Observe(hero, 11, 11);
        Check(poses.IsKnockedOut(hero), "Hero must kneel again after another KO: " + id);
        hero.RecoverHp(100); poses.Observe(hero, 12, 12);
        Check(!poses.IsKnockedOut(hero), "Immediate recovery must clear KO: " + id);
        Check(hero.Hp == 100 && hero.Mp == 20 && hero.Limit.Charge > 0, "Presentation must preserve battle data: " + id);
    }
    public static void Main()
    {
        foreach (string id in new[] { "aren", "lia", "bram" }) Lifecycle(id);
        var poses = new HeroKnockoutPresentation();
        var fallen = Hero("aren"); fallen.TakeDamage(100);
        poses.Observe(fallen, 20, 1);
        Check(poses.IsKnockedOut(fallen), "An ally entering battle at zero HP must already kneel");
        var living = Hero("lia"); poses.Observe(living, 1, 1);
        Check(!poses.IsKnockedOut(living) && poses.IsKnockedOut(fallen), "Each ally keeps its own pose");
        var enemy = Hero("enemy", CombatTeam.Enemy); enemy.TakeDamage(100); poses.Observe(enemy, 1, 1);
        Check(!poses.IsKnockedOut(enemy), "Enemy defeat uses its existing presentation");
        living.TakeDamage(100); poses.Observe(living, 5, 2);
        living.RecoverHp(100); poses.Observe(living, 3, 3);
        poses.Observe(living, 6, 6);
        Check(!poses.IsKnockedOut(living), "A cancelled pending knockout must not reappear later");
        poses.Reset();
        fallen.RecoverHp(100); poses.Observe(fallen, 7, 7);
        Check(!poses.IsKnockedOut(fallen), "A new battle must not retain a previous knockout");
        Console.WriteLine(checks + " verificacoes de nocaute e ressurreicao passaram.");
    }
}
