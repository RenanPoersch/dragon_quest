using DragonQuest.Content;
using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Inventory;

internal static class ChainPrisonChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static readonly AbilityDefinition Attack = AbilityCatalog.Attack;
    private static readonly AbilityDefinition Defend = new AbilityDefinition("wait", "Esperar", "", 0, 0, AbilityEffect.Defend, TargetRule.Self);
    private static readonly AbilityDefinition Bind = AbilityCatalog.Chains;
    private static readonly AbilityDefinition Magic = new AbilityDefinition("ice", "Gelo", "", 3, 5, AbilityEffect.MagicDamage, TargetRule.Enemy);
    private static readonly AbilityDefinition Area = new AbilityDefinition("area", "Area", "", 4, 7, AbilityEffect.MagicDamage, TargetRule.AllEnemies);
    private static CombatantState Actor(string id, CombatTeam team, int speed, params AbilityDefinition[] abilities)
        => new CombatantState(id, new CombatantDefinition(id, "", 1000, 100, 50, 10, 30, speed, abilities), team);
    private static void Act(BattleSession battle, AbilityDefinition ability, CombatantState target)
        => Check(battle.TryAct(battle.CurrentActor, ability, target, out string message), message);
    private static void CheckPersistenceAndRescue()
    {
        var boss = Actor("boss", CombatTeam.Enemy, 30, Bind, Defend, Attack);
        var victim = Actor("victim", CombatTeam.Party, 20, Attack, Defend);
        var rescuer = Actor("rescuer", CombatTeam.Party, 10, Attack, Magic, Defend);
        var battle = new BattleSession(boss, victim, rescuer);
        Act(battle, Bind, victim);
        Check(victim.IsBound && victim.TurnsTaken == 1 && battle.CurrentActor == rescuer, "First bound turn skipped without expiring prison.");
        Check(victim.Mp == 100 && victim.Limit.Charge == 0, "Skipped turn spends neither MP nor limit.");
        for (int round = 0; round < 4; round++)
        {
            Act(battle, Defend, rescuer); Act(battle, Defend, boss);
            Check(victim.IsBound && battle.CurrentActor == rescuer, "Chains survive multiple turns and rounds.");
        }
        Check(battle.GetValidTargets(Attack).Contains(victim), "Ally prison can be selected with attack.");
        Check(battle.GetValidTargets(Magic).Contains(victim), "Ally prison can be selected with magic.");
        victim.ProtectedBy = rescuer;
        int hp = victim.Hp, rescuerHp = rescuer.Hp, turns = rescuer.TurnsTaken;
        Act(battle, Magic, victim);
        Check(!victim.IsBound && victim.Hp == hp && rescuer.Hp == rescuerHp, "Rescue breaks prison without friendly damage or interception.");
        Check(rescuer.TurnsTaken == turns + 1 && rescuer.Mp == 97, "Rescue consumes one normal action and its normal MP cost.");
        Check(!battle.GetValidTargets(Attack).Contains(boss), "Boss cannot target itself.");
        Act(battle, Defend, boss);
        Check(battle.CurrentActor == victim, "Freed hero gets its next turn back.");
        Act(battle, Attack, boss);
        Check(boss.Hp < 1000, "Freed hero can act again.");
    }
    private static void CheckAnyHit()
    {
        var victim = Actor("victim", CombatTeam.Party, 10, Defend);
        victim.BoundActions = 1; victim.TakeDamage(0);
        Check(!victim.IsBound && victim.Hp == 1000, "Zero-damage hit breaks chains.");
        victim.BoundActions = 1; victim.GrantDamageShield(3, false); victim.IsDefending = true;
        victim.TakeDamage(int.MaxValue);
        Check(!victim.IsBound && victim.Hp == 1000 && victim.VitalChanges.Count == 0, "Prison absorbs even a lethal hit before mitigation or limit gain.");
        victim.BoundActions = 1; victim.PoisonTurns = 1; victim.TickPoison();
        Check(victim.IsBound && victim.Hp == 975, "Poison ticks cannot hit the prison; shield still mitigates poison.");
        victim.TakeDamage(10000, damageOverTime:true); victim.RecoverHp(100);
        Check(!victim.IsBound, "Death and revival cannot retain a stale prison.");

        foreach (var skill in new[] { Attack, Magic, AbilityCatalog.MindControl, AbilityCatalog.Silence })
        {
            var boss = Actor("boss", CombatTeam.Enemy, 30, skill);
            var target = Actor("target", CombatTeam.Party, 20, Defend); target.BoundActions = 1;
            var battle = new BattleSession(boss, target);
            Act(battle, skill, target);
            Check(!target.IsBound && target.Hp == 1000 && target.MindControlTurns == 0 && !target.IsSilenced,
                "Damage and zero-damage status skills each break the prison instead of affecting its prisoner.");
        }
    }
    private static void CheckAreaComboAndRecovery()
    {
        var rescuer = Actor("rescuer", CombatTeam.Party, 40, Area, Defend);
        var first = Actor("first", CombatTeam.Party, 30, Defend); first.BoundActions = 1;
        var second = Actor("second", CombatTeam.Party, 20, Defend); second.BoundActions = 1;
        var boss = Actor("boss", CombatTeam.Enemy, 10, Defend);
        var battle = new BattleSession(rescuer, first, second, boss);
        Check(battle.GetValidTargets(Area).Count == 3, "Area includes enemies and both allied prisons.");
        Act(battle, Area, boss);
        Check(!first.IsBound && !second.IsBound && first.Hp == 1000 && second.Hp == 1000 && boss.Hp < 1000,
            "One area hit frees both heroes and still hits the boss.");
        Check(battle.CurrentActor == first, "Area rescue before a hero's turn restores that same round's action.");

        var combo = HeroUltimates.Omnislash;
        rescuer = Actor("rescuer", CombatTeam.Party, 40, combo); rescuer.Limit.Restore(100);
        first = Actor("first", CombatTeam.Party, 30, Defend); first.BoundActions = 1;
        boss = Actor("boss", CombatTeam.Enemy, 10, Defend);
        battle = new BattleSession(rescuer, first, boss);
        Act(battle, combo, first);
        Check(first.Hp == 1000 && !first.IsBound && rescuer.Limit.Charge == 0, "Multi-hit rescue consumes the action without remaining hits spilling onto the ally.");

        var heal = AbilityCatalog.Heal;
        rescuer = Actor("rescuer", CombatTeam.Party, 40, heal);
        first = Actor("first", CombatTeam.Party, 30, Defend); first.BoundActions = 1;
        boss = Actor("boss", CombatTeam.Enemy, 10, Defend);
        battle = new BattleSession(rescuer, first, boss);
        Check(battle.GetValidTargets(heal).Contains(first), "Recovery can target a prison even at full HP.");
        Act(battle, heal, first); Check(!first.IsBound, "Recovery spell also breaks chains.");
        var inventory = new ItemInventory(); inventory.TryAdd(ItemCatalog.Potion, 1, out _);
        rescuer = Actor("rescuer", CombatTeam.Party, 40, Defend);
        first.BoundActions = 1;
        battle = new BattleSession(inventory, rescuer, first, boss);
        Check(battle.GetValidItemTargets(ItemCatalog.Potion).Contains(first), "Items can target a full-HP chained hero.");
        Check(battle.TryUseItem(rescuer, ItemCatalog.Potion, first, out _) && !first.IsBound, "Item hit releases the prisoner and consumes item normally.");
    }
    private static void CheckTurnSafety()
    {
        var boss = Actor("boss", CombatTeam.Enemy, 40, Attack, Defend, Bind);
        var first = Actor("first", CombatTeam.Party, 30, Defend); first.BoundActions = 1;
        var second = Actor("second", CombatTeam.Party, 20, Defend); second.BoundActions = 1;
        var battle = new BattleSession(boss, first, second);
        for (int i = 0; i < 30; i++) Act(battle, Defend, boss);
        Check(battle.CurrentActor == boss && first.IsBound && second.IsBound && first.TurnsTaken == 30,
            "Entire party chained still advances to boss safely across many rounds.");
        Check(battle.GetValidTargets(Bind).Count == 0, "Boss does not rebind an existing prison.");
        boss.BoundActions = 1;
        Check(battle.TryTakeBoundTurn(out _) && battle.CurrentActor != null, "All participants chained yields safely instead of recursion overflow.");
        Check(!battle.TryAct(battle.CurrentActor, Defend, battle.CurrentActor, out _), "Chained actor cannot force an action.");
        first.ClearHarmfulConditions();
        Check(!first.IsBound, "Full cleanse removes the prison.");
        var status = CombatStatuses.Active(second).Single(s => s.Status == CombatStatus.Bound);
        Check(status.Turns == 0, "Persistent prison indicator has no false one-turn countdown.");
    }
    public static void Main()
    {
        CheckPersistenceAndRescue(); CheckAnyHit(); CheckAreaComboAndRecovery(); CheckTurnSafety();
        Console.WriteLine("CHAIN_PRISON_CHECKS_OK: " + checks + " checks.");
    }
}
