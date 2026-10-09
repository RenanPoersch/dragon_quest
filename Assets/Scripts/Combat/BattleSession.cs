using System;
using System.Collections.Generic;

namespace DragonQuest.Combat
{
    public enum BattleOutcome { None, Victory, Defeat }

    // Regras puras em C#: não dependem de cenas, menus ou animações da Unity.
    public sealed class BattleSession
    {
        private readonly List<CombatantState> combatants;
        private readonly List<CombatantState> turnOrder = new List<CombatantState>();
        private int turnIndex;

        public IReadOnlyList<CombatantState> Combatants { get; }
        public BattleOutcome Outcome { get; private set; }
        public int Round { get; private set; }
        public string LastMessage { get; private set; }
        public CombatantState CurrentActor => Outcome == BattleOutcome.None ? turnOrder[turnIndex] : null;

        public BattleSession(params CombatantState[] participants)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            combatants = new List<CombatantState>(participants);
            var ids = new HashSet<string>();
            bool party = false, enemy = false;
            foreach (CombatantState participant in combatants)
            {
                if (participant == null || !participant.IsAlive || !ids.Add(participant.Id))
                    throw new ArgumentException("Participantes devem estar vivos e ter IDs unicos.");
                party |= participant.Team == CombatTeam.Party;
                enemy |= participant.Team == CombatTeam.Enemy;
            }
            if (!party || !enemy) throw new ArgumentException("Batalha precisa de aliados e inimigos.");
            Combatants = combatants.AsReadOnly();
            LastMessage = "Prepare sua equipe para a batalha.";
            BeginRound();
        }

        public IReadOnlyList<CombatantState> GetValidTargets(AbilityDefinition ability)
        {
            var targets = new List<CombatantState>();
            if (ability == null || CurrentActor == null) return targets;
            foreach (CombatantState target in combatants)
                if (IsValidTarget(CurrentActor, ability, target)) targets.Add(target);
            return targets;
        }

        private static bool IsValidTarget(CombatantState actor, AbilityDefinition ability, CombatantState target)
        {
            if (!target.IsAlive) return false;
            if (ability.Targets == TargetRule.Self && target != actor) return false;
            if (ability.Targets == TargetRule.Enemy && target.Team == actor.Team) return false;
            if (ability.Targets == TargetRule.Ally && target.Team != actor.Team) return false;
            if (ability.Effect == AbilityEffect.Heal && target.Hp >= target.Definition.MaxHp) return false;
            return true;
        }

        public bool TryAct(CombatantState actor, AbilityDefinition ability, CombatantState target, out string message)
        {
            if (Outcome != BattleOutcome.None) { message = "A batalha ja terminou."; return false; }
            if (actor == null || actor != CurrentActor) { message = "Nao e o turno deste personagem."; return false; }
            bool knownAbility = false;
            foreach (AbilityDefinition known in actor.Definition.Abilities)
                if (known == ability) knownAbility = true;
            if (!knownAbility) { message = "Habilidade indisponivel para este personagem."; return false; }
            if (actor.Mp < ability.MpCost) { message = "MP insuficiente."; return false; }
            if (target == null || !combatants.Contains(target) || !IsValidTarget(actor, ability, target))
            { message = "Escolha um alvo vivo e valido para esta habilidade."; return false; }

            actor.SpendMp(ability.MpCost);
            if (ability.Effect == AbilityEffect.PhysicalDamage)
            {
                long baseDamage = Math.Max(1L, (long)actor.Definition.Attack + ability.Power - target.Definition.Defense);
                if (target.IsDefending) baseDamage = (baseDamage + 1) / 2;
                int damage = (int)Math.Min(int.MaxValue, baseDamage);
                int previousHp = target.Hp;
                target.TakeDamage(damage);
                message = actor.Definition.Name + " usou " + ability.Name + ": " + target.Definition.Name
                    + " recebeu " + (previousHp - target.Hp) + " de dano.";
                if (!target.IsAlive) message += " " + target.Definition.Name + " foi derrotado!";
            }
            else if (ability.Effect == AbilityEffect.Heal)
            {
                int healing = (int)Math.Min(int.MaxValue, (long)ability.Power + actor.Definition.Magic);
                int previousHp = target.Hp;
                target.RecoverHp(healing);
                message = actor.Definition.Name + " curou " + (target.Hp - previousHp) + " HP de " + target.Definition.Name + ".";
            }
            else
            {
                actor.IsDefending = true;
                message = actor.Definition.Name + " esta defendendo ate o inicio de seu proximo turno.";
            }

            LastMessage = message;
            CheckOutcome();
            if (Outcome == BattleOutcome.None) NextTurn();
            return true;
        }

        private void CheckOutcome()
        {
            bool partyAlive = false, enemyAlive = false;
            foreach (CombatantState participant in combatants)
            {
                if (!participant.IsAlive) continue;
                partyAlive |= participant.Team == CombatTeam.Party;
                enemyAlive |= participant.Team == CombatTeam.Enemy;
            }
            if (!enemyAlive) Outcome = BattleOutcome.Victory;
            else if (!partyAlive) Outcome = BattleOutcome.Defeat;
        }

        private void BeginRound()
        {
            Round++;
            turnOrder.Clear();
            foreach (CombatantState participant in combatants)
                if (participant.IsAlive) turnOrder.Add(participant);
            turnOrder.Sort((left, right) =>
            {
                int speed = right.Definition.Speed.CompareTo(left.Definition.Speed);
                return speed != 0 ? speed : StringComparer.Ordinal.Compare(left.Id, right.Id);
            });
            turnIndex = 0;
            CurrentActor.IsDefending = false;
        }

        private void NextTurn()
        {
            turnIndex++;
            while (turnIndex < turnOrder.Count && !turnOrder[turnIndex].IsAlive) turnIndex++;
            if (turnIndex >= turnOrder.Count) BeginRound();
            else CurrentActor.IsDefending = false;
        }
    }
}
