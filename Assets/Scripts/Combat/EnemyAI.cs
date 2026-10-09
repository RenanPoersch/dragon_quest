namespace DragonQuest.Combat
{
    public static class EnemyAI
    {
        public static AbilityDefinition ChooseAbility(BattleSession battle)
        {
            CombatantState actor = battle.CurrentActor;
            if (actor == null || actor.Team != CombatTeam.Enemy) return null;
            // Alterna as ofensivas na ordem cadastrada; falta de MP usa a proxima opcao viavel.
            int attacks = 0;
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
                if (ability.Effect == AbilityEffect.PhysicalDamage) attacks++;
            for (int offset = 0; offset < attacks; offset++)
            {
                int desired = (int)(((long)actor.TurnsTaken + offset) % attacks);
                int index = 0;
                foreach (AbilityDefinition ability in actor.Definition.Abilities)
                {
                    if (ability.Effect != AbilityEffect.PhysicalDamage) continue;
                    if (index++ == desired && ability.MpCost <= actor.Mp && battle.GetValidTargets(ability).Count > 0)
                        return ability;
                }
            }
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
                if (ability.Effect == AbilityEffect.Defend && ability.MpCost <= actor.Mp) return ability;
            return null;
        }

        public static bool TryTakeTurn(BattleSession battle, out string message)
        {
            CombatantState actor = battle.CurrentActor;
            if (actor == null || actor.Team != CombatTeam.Enemy)
            { message = "Nao e o turno do inimigo."; return false; }
            AbilityDefinition ability = ChooseAbility(battle);
            if (ability != null)
            {
                if (ability.Effect == AbilityEffect.Defend) return battle.TryAct(actor, ability, actor, out message);
                CombatantState weakest = null;
                foreach (CombatantState target in battle.GetValidTargets(ability))
                    if (weakest == null || (long)target.Hp * weakest.Definition.MaxHp < (long)weakest.Hp * target.Definition.MaxHp)
                        weakest = target;
                if (weakest != null) return battle.TryAct(actor, ability, weakest, out message);
            }
            message = "Inimigo sem acao valida.";
            return false;
        }
    }
}
