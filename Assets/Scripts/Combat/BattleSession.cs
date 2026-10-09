using System;
using System.Collections.Generic;
using System.Text;

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
            if (ability.Effect == AbilityEffect.Revive) return target.Team == actor.Team && !target.IsAlive;
            if (!target.IsAlive) return false;
            if (ability.Targets == TargetRule.Self && target != actor) return false;
            if ((ability.Targets == TargetRule.Enemy || ability.Targets == TargetRule.AllEnemies) && target.Team == actor.Team) return false;
            if (ability.Targets == TargetRule.Ally && target.Team != actor.Team) return false;
            if (ability.Effect == AbilityEffect.Heal && target.Hp >= target.Definition.MaxHp) return false;
            if (ability.Effect == AbilityEffect.Cleanse && target.PoisonTurns == 0) return false;
            if (ability.Effect == AbilityEffect.Protect && target == actor) return false;
            if (ability.Effect == AbilityEffect.PhysicalDamage && ability.Targets == TargetRule.Enemy
                && actor.TauntTurns > 0 && actor.TauntedBy != null && actor.TauntedBy.IsAlive && target != actor.TauntedBy) return false;
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
            { message = "Escolha um alvo valido para esta habilidade."; return false; }

            actor.SpendMp(ability.MpCost);
            if (ability.Effect == AbilityEffect.PhysicalDamage)
            {
                var report = new StringBuilder(actor.Definition.Name + " usou " + ability.Name + ": ");
                if (ability.Targets == TargetRule.AllEnemies)
                {
                    // Custo e turno sao consumidos uma vez; protecao nao intercepta ataques em area.
                    foreach (CombatantState enemy in GetValidTargets(ability))
                        ApplyDamage(actor, ability, enemy, false, report);
                }
                else ApplyDamage(actor, ability, target, true, report);
                message = report.ToString();
            }
            else if (ability.Effect == AbilityEffect.Heal)
            {
                int healing = (int)Math.Min(int.MaxValue, (long)ability.Power + actor.Definition.Magic);
                int previousHp = target.Hp;
                target.RecoverHp(healing);
                message = actor.Definition.Name + " curou " + (target.Hp - previousHp) + " HP de " + target.Definition.Name + ".";
            }
            else if (ability.Effect == AbilityEffect.Defend)
            {
                actor.IsDefending = true;
                message = actor.Definition.Name + " esta defendendo ate o inicio de seu proximo turno.";
            }
            else if (ability.Effect == AbilityEffect.Cleanse)
            {
                target.PoisonTurns = 0;
                message = actor.Definition.Name + " removeu o veneno de " + target.Definition.Name + ".";
            }
            else if (ability.Effect == AbilityEffect.Revive)
            {
                target.ClearConditions();
                int recovery = (int)(((long)target.Definition.MaxHp * ability.Power + 99) / 100);
                target.RecoverHp(recovery);
                message = actor.Definition.Name + " reviveu " + target.Definition.Name + " com " + target.Hp + " HP.";
            }
            else if (ability.Effect == AbilityEffect.Taunt)
            {
                target.TauntedBy = actor;
                target.TauntTurns = ability.Power;
                message = actor.Definition.Name + " provocou " + target.Definition.Name + " por " + ability.Power + " turnos do alvo.";
            }
            else
            {
                target.ProtectedBy = actor;
                message = actor.Definition.Name + " protegera " + target.Definition.Name + " do proximo golpe individual, ate seu proximo turno.";
            }

            actor.TurnsTaken++;
            if (actor.TauntTurns > 0 && --actor.TauntTurns == 0) actor.TauntedBy = null;
            RemoveDeadSources();
            CheckOutcome();
            // Veneno causa dano ao fim da acao do afetado, sem criar turnos extras.
            if (Outcome == BattleOutcome.None && actor.IsAlive && actor.PoisonTurns > 0)
            {
                int previousHp = actor.Hp;
                actor.PoisonTurns--;
                actor.TakeDamage((int)(((long)actor.Definition.MaxHp + 19) / 20));
                message += " Veneno: " + actor.Definition.Name + " perdeu " + (previousHp - actor.Hp) + " HP.";
                if (!actor.IsAlive) message += " Foi derrotado!";
                RemoveDeadSources();
                CheckOutcome();
            }
            LastMessage = message;
            if (Outcome == BattleOutcome.None) NextTurn();
            return true;
        }

        private static void ApplyDamage(CombatantState actor, AbilityDefinition ability, CombatantState target,
            bool allowProtection, StringBuilder report)
        {
            if (allowProtection && target.ProtectedBy != null && target.ProtectedBy.IsAlive)
            {
                CombatantState protector = target.ProtectedBy;
                target.ProtectedBy = null;
                report.Append(protector.Definition.Name).Append(" protegeu ").Append(target.Definition.Name).Append(". ");
                target = protector;
            }
            long damage = Math.Max(1L, (long)actor.Definition.Attack + ability.Power - target.Definition.Defense);
            if (target.IsDefending) damage = (damage + 1) / 2;
            int previousHp = target.Hp;
            target.TakeDamage((int)Math.Min(int.MaxValue, damage));
            report.Append(target.Definition.Name).Append(" -").Append(previousHp - target.Hp).Append(" HP");
            if (!target.IsAlive) report.Append(" (derrotado)");
            else if (ability.PoisonTurns > 0)
            {
                target.PoisonTurns = Math.Max(target.PoisonTurns, ability.PoisonTurns);
                report.Append(" (veneno)");
            }
            report.Append(". ");
        }

        private void RemoveDeadSources()
        {
            foreach (CombatantState participant in combatants)
            {
                if (participant.ProtectedBy != null && !participant.ProtectedBy.IsAlive) participant.ProtectedBy = null;
                if (participant.TauntedBy != null && !participant.TauntedBy.IsAlive)
                { participant.TauntedBy = null; participant.TauntTurns = 0; }
            }
        }

        private void StartTurn()
        {
            CurrentActor.IsDefending = false;
            foreach (CombatantState participant in combatants)
                if (participant.ProtectedBy == CurrentActor) participant.ProtectedBy = null;
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
            StartTurn();
        }

        private void NextTurn()
        {
            turnIndex++;
            while (turnIndex < turnOrder.Count && !turnOrder[turnIndex].IsAlive) turnIndex++;
            if (turnIndex >= turnOrder.Count) BeginRound();
            else StartTurn();
        }
    }
}
