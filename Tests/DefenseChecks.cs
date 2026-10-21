using System;
using DragonQuest.Combat;
using DragonQuest.Inventory;

internal static class DefenseChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    private static int Strike(int defense, AbilityEffect effect = AbilityEffect.PhysicalDamage,
        CombatTeam team = CombatTeam.Party, int offense = 25, int power = 5,
        bool critical = false, bool defending = false, bool shield = false,
        ElementalAffinities affinities = null)
    {
        var attack = new AbilityDefinition("test", "Test", "", 0, power, effect, TargetRule.Enemy,
            element: affinities == null ? DamageElement.None : DamageElement.Fire);
        var actor = new CombatantState("actor", new CombatantDefinition("Actor", "", int.MaxValue, 0,
            effect == AbilityEffect.PhysicalDamage ? offense : 3, 0,
            effect == AbilityEffect.PhysicalDamage ? 3 : offense, 20,
            ElementalAffinities.Neutral, 0, 0, critical ? 1 : 0, attack), team);
        var target = new CombatantState("target", new CombatantDefinition("Target", "", int.MaxValue, 0,
            0, defense, 0, 1, affinities ?? ElementalAffinities.Neutral, attack),
            team == CombatTeam.Party ? CombatTeam.Enemy : CombatTeam.Party);
        target.IsDefending = defending;
        if (shield) target.GrantDamageShield(3, false);
        var battle = new BattleSession(() => 0, null, new ItemInventory(), actor, target);
        Check(battle.TryAct(actor, attack, target, out string message), "Strike must succeed: " + message);
        return int.MaxValue - target.Hp;
    }

    private static void Curve()
    {
        int[] defenses = { 0, 10, 30, 50, 100, 200 };
        int[] damages = { 30, 27, 23, 20, 15, 10 };
        foreach (AbilityEffect effect in new[] { AbilityEffect.PhysicalDamage, AbilityEffect.MagicDamage, AbilityEffect.Drain })
        foreach (CombatTeam team in new[] { CombatTeam.Party, CombatTeam.Enemy })
        for (int i = 0; i < defenses.Length; i++)
            Check(Strike(defenses[i], effect, team) == damages[i] * (team == CombatTeam.Enemy ? 4 : 1),
                "Physical, magical and drain damage must use the same gradual defense curve for both teams.");

        int previous = int.MaxValue;
        for (int defense = 0; defense <= 300; defense++)
        {
            int damage = Strike(defense, offense: 1000, power: 0);
            Check(damage > 0 && damage <= previous, "Adding defense cannot increase damage or grant immunity.");
            previous = damage;
        }
        Check(Strike(30, team: CombatTeam.Enemy) == 92,
            "A 30-point enemy strike against 30 defense must deal 92 damage instead of collapsing to 4.");
        Check(Strike(100, offense: 1000, power: 0) == 500
            && Strike(200, offense: 1000, power: 0) == 333,
            "The second 100 defense grants less additional percentage reduction than the first.");
    }

    private static void ModifiersAndLimits()
    {
        var weak = new ElementalAffinities(weaknesses: new[] { DamageElement.Fire });
        var resistant = new ElementalAffinities(resistances: new[] { DamageElement.Fire });
        Check(Strike(50, critical: true, defending: true, shield: true, affinities: weak) == 10,
            "Defense, weakness, critical, Defender and Shield must preserve their rounding order.");
        Check(Strike(50, team: CombatTeam.Enemy, critical: true, defending: true, shield: true, affinities: weak) == 37,
            "Enemy scaling occurs after defense, affinity and critical, before Defender and Shield.");
        Check(Strike(50, affinities: resistant) == 10, "Resistance halves damage after the defense curve.");
        Check(Strike(50, defending: true) == 10 && Strike(50, shield: true) == 10
            && Strike(50, defending: true, shield: true) == 5, "Defender and Shield retain their separate halves.");
        Check(Strike(int.MaxValue, offense: 0, power: 0) == 1
            && Strike(int.MaxValue, team: CombatTeam.Enemy, offense: 0, power: 0) == 4,
            "Very high defense preserves positive minimum damage and enemy scaling.");
        Check(Strike(int.MaxValue, offense: int.MaxValue, power: int.MaxValue) == 199,
            "Maximum offense, power and defense must use wide arithmetic without overflow.");
        Check(Strike(0, offense: int.MaxValue, power: int.MaxValue, critical: true) == int.MaxValue,
            "Damage beyond the HP integer range must saturate safely.");
    }

    public static int Main()
    {
        try
        {
            Curve(); ModifiersAndLimits();
            Console.WriteLine(checks + " checks of gradual defense, both teams, magic, drain, modifiers and overflow passed.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
