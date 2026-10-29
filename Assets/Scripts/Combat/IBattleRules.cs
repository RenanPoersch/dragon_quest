namespace DragonQuest.Combat
{
    // Limites opcionais de dano para transicoes de fase, antes de alterar HP e registrar impactos.
    public interface IBattleDamageRules
    {
        int LimitDamage(CombatantState target, int damage);
    }

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
