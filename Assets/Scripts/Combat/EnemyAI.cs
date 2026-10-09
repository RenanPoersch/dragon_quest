namespace DragonQuest.Combat
{
    public static class EnemyAI
    {
        public static bool TryTakeTurn(BattleSession battle, out string message)
        {
            CombatantState actor = battle.CurrentActor;
            if (actor == null || actor.Team != CombatTeam.Enemy)
            { message = "Nao e o turno do inimigo."; return false; }
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
            {
                if (ability.Effect != AbilityEffect.PhysicalDamage || ability.MpCost > actor.Mp) continue;
                CombatantState weakest = null;
                foreach (CombatantState target in battle.GetValidTargets(ability))
                    if (weakest == null || (long)target.Hp * weakest.Definition.MaxHp < (long)weakest.Hp * target.Definition.MaxHp)
                        weakest = target;
                if (weakest != null) return battle.TryAct(actor, ability, weakest, out message);
            }
            foreach (AbilityDefinition ability in actor.Definition.Abilities)
                if (ability.Effect == AbilityEffect.Defend && ability.MpCost <= actor.Mp)
                    return battle.TryAct(actor, ability, actor, out message);
            message = "Inimigo sem acao valida.";
            return false;
        }
    }
}
