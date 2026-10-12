namespace DragonQuest.Combat
{
    // Regras particulares de um encontro, sem acoplar o combate a uma cena ou boss.
    public interface IBattleRules
    {
        AbilityDefinition ChooseEnemyAbility(BattleSession battle);
        bool ValidateAction(CombatantState actor, AbilityDefinition ability, out string message);
        bool TryResolveAction(BattleSession battle, CombatantState actor, AbilityDefinition ability, out string message);
        void AfterAction(BattleSession battle);
        BattleOutcome EvaluateOutcome(BattleSession battle);
    }
}
