namespace DragonQuest.Combat
{
    public static class EnemyAI
    {
        private static bool IsCycledAction(AbilityDefinition ability) => ability.Effect == AbilityEffect.PhysicalDamage
            || ability.Effect == AbilityEffect.MagicDamage || ability.Effect == AbilityEffect.Prepare
            || ability.Effect == AbilityEffect.Drain || ability.Effect == AbilityEffect.Bind
            || ability.Effect == AbilityEffect.MindControl || ability.Effect == AbilityEffect.Silence;
        public static AbilityDefinition ChooseAbility(BattleSession battle)
        {
            CombatantState actor = battle.CurrentActor;
            if (actor == null || actor.Team != CombatTeam.Enemy) return null;
            AbilityDefinition scripted=battle.Rules?.ChooseEnemyAbility(battle);
            if(scripted!=null) return scripted;
            // Alterna ataques e preparacoes na ordem cadastrada; falta de MP usa a proxima opcao viavel.
            int attacks = 0;
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
                if (IsCycledAction(ability)) attacks++;
            for (int offset = 0; offset < attacks; offset++)
            {
                int desired = (int)(((long)actor.TurnsTaken + offset) % attacks);
                int index = 0;
                foreach (AbilityDefinition ability in actor.Definition.Abilities)
                {
                    if (!IsCycledAction(ability)) continue;
                    if (index++ == desired && (!actor.IsSilenced || !ability.IsMagic) && ability.MpCost <= actor.Mp && battle.GetValidTargets(ability).Count > 0)
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
                if (ability.Targets == TargetRule.Self) return battle.TryAct(actor, ability, actor, out message);
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
