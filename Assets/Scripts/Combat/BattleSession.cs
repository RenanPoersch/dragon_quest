using System;
using System.Collections.Generic;
using System.Text;
using DragonQuest.Inventory;

namespace DragonQuest.Combat
{
    public enum BattleOutcome { None, Victory, Defeat, Completed }

    // Regras puras em C#: não dependem de cenas, menus ou animações da Unity.
    public sealed class BattleSession
    {
        public static readonly AbilityDefinition MindControlAttack = new AbilityDefinition("confused-attack", "Ataque confuso",
            "Ataca um combatente vivo aleatorio por controle mental.", 0, 0, AbilityEffect.PhysicalDamage,
            TargetRule.Enemy, sourceAbilityId: "attack");
        private readonly List<CombatantState> combatants;
        private readonly List<CombatantState> turnOrder = new List<CombatantState>();
        private int turnIndex;
        private bool skippingBoundTurns;
        private readonly Func<double> criticalRoll;

        public IReadOnlyList<CombatantState> Combatants { get; }
        public ItemInventory Inventory { get; }
        public IBattleRules Rules { get; }
        public BattleOutcome Outcome { get; private set; }
        public int Round { get; private set; }
        public string LastMessage { get; private set; }
        public CombatantState CurrentActor => Outcome == BattleOutcome.None ? turnOrder[turnIndex] : null;

        public BattleSession(params CombatantState[] participants) : this(new ItemInventory(), participants) { }

        public BattleSession(ItemInventory inventory, params CombatantState[] participants) : this(null,inventory,participants) { }

        public BattleSession(IBattleRules rules, ItemInventory inventory, params CombatantState[] participants)
            : this(new Random().NextDouble, rules, inventory, participants) { }

        public BattleSession(Func<double> criticalRoll, IBattleRules rules, ItemInventory inventory, params CombatantState[] participants)
        {
            this.criticalRoll = criticalRoll ?? throw new ArgumentNullException(nameof(criticalRoll));
            Rules = rules;
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            combatants = new List<CombatantState>(participants);
            var ids = new HashSet<string>();
            bool party = false, enemy = false;
            foreach (CombatantState participant in combatants)
            {
                if (participant == null || (participant.Team == CombatTeam.Enemy && !participant.IsAlive) || !ids.Add(participant.Id))
                    throw new ArgumentException("Inimigos devem estar vivos e participantes devem ter IDs unicos.");
                party |= participant.Team == CombatTeam.Party && participant.IsAlive;
                enemy |= participant.Team == CombatTeam.Enemy;
            }
            if (!party || !enemy) throw new ArgumentException("Batalha precisa de aliados e inimigos.");
            Combatants = combatants.AsReadOnly();
            LastMessage = string.Empty;
            BeginRound();
        }

        public IReadOnlyList<CombatantState> GetValidItemTargets(ConsumableDefinition item)
        {
            var targets = new List<CombatantState>();
            if (item == null || CurrentActor == null || CurrentActor.Team != CombatTeam.Party || Inventory.QuantityOf(item) == 0) return targets;
            foreach (CombatantState target in combatants)
                if (IsValidItemTarget(CurrentActor, item, target)) targets.Add(target);
            return targets;
        }

        private static bool IsValidItemTarget(CombatantState actor, ConsumableDefinition item, CombatantState target)
        {
            if (target.Team != actor.Team) return false;
            if (item.Effect == ItemEffect.Revive) return !target.IsAlive;
            if (!target.IsAlive) return false;
            if (target.IsBound) return true;
            if (item.Effect == ItemEffect.RestoreHp) return target.Hp < target.Definition.MaxHp;
            if (item.Effect == ItemEffect.RestoreMp) return target.Mp < target.Definition.MaxMp;
            return target.PoisonTurns > 0;
        }

        public bool TryUseItem(CombatantState actor, ConsumableDefinition item, CombatantState target, out string message)
        {
            if (Outcome != BattleOutcome.None) { message = "A batalha ja terminou."; return false; }
            if (actor == null || actor != CurrentActor || actor.Team != CombatTeam.Party)
            { message = "Itens so podem ser usados no turno de um aliado."; return false; }
            if (actor.IsBound) { message = "Este personagem esta acorrentado."; return false; }
            if (actor.MindControlTurns > 0) { message = "O controle mental obriga um ataque aleatorio."; return false; }
            if (item == null || Inventory.QuantityOf(item) == 0) { message = "Item indisponivel no inventario."; return false; }
            if (target == null || !combatants.Contains(target) || !IsValidItemTarget(actor, item, target))
            { message = "Este alvo nao precisa ou nao pode receber o item."; return false; }
            if (!Inventory.TryConsume(item)) { message = "Item esgotado."; return false; }
            bool brokeChains = target.TryBreakChains();
            message = actor.Definition.Name + " usou " + item.Name + ": " + target.Definition.Name;
            if (brokeChains) message += " (correntes quebradas)";
            if (item.Effect == ItemEffect.RestoreHp)
            {
                int before = target.Hp;
                target.RecoverHp(item.Power);
                message += " +" + (target.Hp - before) + " HP.";
            }
            else if (item.Effect == ItemEffect.RestoreMp)
            {
                int before = target.Mp;
                target.RecoverMp(item.Power);
                message += " +" + (target.Mp - before) + " MP.";
            }
            else if (item.Effect == ItemEffect.CleansePoison)
            {
                target.PoisonTurns = 0;
                message += " sem veneno.";
            }
            else
            {
                target.ClearConditions();
                target.RecoverHp((int)(((long)target.Definition.MaxHp * item.Power + 99) / 100));
                message += " reviveu com " + target.Hp + " HP.";
            }
            CompleteAction(actor, ref message);
            return true;
        }

        public IReadOnlyList<CombatantState> GetValidTargets(AbilityDefinition ability)
        {
            var targets = new List<CombatantState>();
            if (ability == null || CurrentActor == null) return targets;
            foreach (CombatantState target in combatants)
                if (IsValidTarget(CurrentActor, ability, target)) targets.Add(target);
            return targets;
        }

        public CombatantState NextLivingEnemyAfter(CombatantState previous)
        {
            int start = combatants.IndexOf(previous);
            if (start < 0) return null;
            for (int offset = 1; offset <= combatants.Count; offset++)
            {
                CombatantState next = combatants[(start + offset) % combatants.Count];
                if (next.Team == CombatTeam.Enemy && next.IsAlive) return next;
            }
            return null;
        }

        // A new action advances a stale enemy selection in formation order.
        public CombatantState ResolveLivingEnemyTarget(AbilityDefinition ability, CombatantState target)
        {
            if (ability == null || CurrentActor == null || target == null || target.IsAlive
                || target.Team == CurrentActor.Team || !combatants.Contains(target)
                || (ability.Targets != TargetRule.Enemy && ability.Targets != TargetRule.AllEnemies)) return target;
            int start = combatants.IndexOf(target);
            for (int offset = 1; offset < combatants.Count; offset++)
            {
                CombatantState next = combatants[(start + offset) % combatants.Count];
                if (next.Team != CurrentActor.Team && next.IsAlive && IsValidTarget(CurrentActor, ability, next)) return next;
            }
            return null;
        }

        private static bool IsValidTarget(CombatantState actor, AbilityDefinition ability, CombatantState target)
        {
            if (ability.PartyEffect == PartyEffect.RestoreCleanseAndRevive) return target.Team == actor.Team;
            if (ability.Effect == AbilityEffect.Revive) return target.Team == actor.Team && !target.IsAlive;
            if (!target.IsAlive) return false;
            if (ability.Targets == TargetRule.Self && target != actor) return false;
            if (ability.Effect == AbilityEffect.Bind && target.IsBound) return false;
            // The prison is an offensive target over its ally. Area attacks also hit these prisons.
            if ((ability.Targets == TargetRule.Enemy || ability.Targets == TargetRule.AllEnemies)
                && target != actor && target.IsBound && ability.Effect != AbilityEffect.Bind) return true;
            if ((ability.Targets == TargetRule.Enemy || ability.Targets == TargetRule.AllEnemies) && target.Team == actor.Team) return false;
            if ((ability.Targets == TargetRule.Ally || ability.Targets == TargetRule.AllAllies) && target.Team != actor.Team) return false;
            if (ability.Effect == AbilityEffect.Heal && target.Hp >= target.Definition.MaxHp && !target.IsBound) return false;
            if (ability.Effect == AbilityEffect.Cleanse && target.PoisonTurns == 0 && !target.IsBound) return false;
            if (ability.Effect == AbilityEffect.Protect && target == actor) return false;
            if ((ability.Effect == AbilityEffect.PhysicalDamage || ability.Effect == AbilityEffect.MagicDamage || ability.Effect == AbilityEffect.Drain || ability.Effect == AbilityEffect.Bind
                || ability.Effect == AbilityEffect.MindControl || ability.Effect == AbilityEffect.Silence) && ability.Targets == TargetRule.Enemy
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
            if (actor.IsBound) { message = "Este personagem esta acorrentado."; return false; }
            if (actor.MindControlTurns > 0) { message = "O controle mental obriga um ataque aleatorio."; return false; }
            if (actor.IsSilenced && ability.IsMagic) { message = "Silencio: nao pode usar magia neste turno."; return false; }
            if(Rules!=null&&!Rules.ValidateAction(actor,ability,out message)) return false;
            if (actor.Mp < ability.MpCost) { message = "MP insuficiente."; return false; }
            if (ability.UsesLimit && !actor.Limit.IsReady) { message = "Limit Break exige a barra em 100%."; return false; }
            target = ResolveLivingEnemyTarget(ability, target);
            if (target == null || !combatants.Contains(target) || !IsValidTarget(actor, ability, target))
            { message = "Escolha um alvo valido para esta habilidade."; return false; }

            actor.SpendMp(ability.MpCost);
            if (ability.UsesLimit) actor.Limit.Spend();
            if (ability.Targets == TargetRule.Enemy && ability.Effect != AbilityEffect.Bind && target.IsBound)
            {
                // One action breaks one prison. Multi-hit attacks never spill onto the freed ally.
                target.TryBreakChains();
                actor.IsCharging = false;
                message = actor.Definition.Name + " usou " + ability.Name + ": correntes de "
                    + target.Definition.Name + " quebradas!";
            }
            else if (Rules != null && Rules.TryResolveAction(this,actor,ability,out message)) { }
            else if (ability.PartyEffect == PartyEffect.RestoreCleanseAndRevive)
                message = actor.Definition.Name + " usou " + ability.Name + ": ";
            else if (ability.Effect == AbilityEffect.PhysicalDamage || ability.Effect == AbilityEffect.MagicDamage || ability.Effect == AbilityEffect.Drain)
            {
                var report = new StringBuilder(actor.Definition.Name + " usou " + ability.Name + ": ");
                if (ability.Targets == TargetRule.AllEnemies)
                {
                    // Custo e turno sao consumidos uma vez; protecao nao intercepta ataques em area.
                    foreach (CombatantState enemy in GetValidTargets(ability))
                        ApplyDamage(actor, ability, enemy, false, report);
                }
                else if (ability.HitCount > 1)
                {
                    // Protection intercepts the whole sequence once. Never spill cuts onto another enemy.
                    CombatantState recipient = target;
                    if (target.ProtectedBy != null && target.ProtectedBy.IsAlive)
                    {
                        recipient = target.ProtectedBy; target.ProtectedBy = null;
                        report.Append(recipient.Definition.Name).Append(" interceptou a sequencia. ");
                    }
                    bool prisonIntercepted = recipient.IsBound;
                    int total = 0, landed = 0, lastDamage = 0;
                    var hitReport = new StringBuilder();
                    for (int hit = 1; hit <= (prisonIntercepted ? 1 : ability.HitCount); hit++)
                    {
                        hitReport.Clear();
                        lastDamage = ApplyDamage(actor, ability, recipient, false, hitReport, hit, returnDisplayedDamage: true);
                        total += lastDamage; landed++;
                    }
                    if (prisonIntercepted) report.Append("Correntes de ").Append(recipient.Definition.Name).Append(" quebradas. ");
                    report.Append(recipient.Definition.Name).Append(" -").Append(total).Append(" HP em ").Append(landed).Append(" cortes");
                    if (landed == ability.HitCount) report.Append(" (final -").Append(lastDamage).Append(" HP)");
                    report.Append(". ");
                }
                else
                {
                    int drained=ApplyDamage(actor, ability, target, true, report);
                    if(ability.Effect==AbilityEffect.Drain)
                    {
                        int before=actor.Hp;actor.RecoverHp(drained);
                        report.Append(actor.Definition.Name).Append(" +").Append(actor.Hp-before).Append(" HP drenados. ");
                    }
                }
                actor.IsCharging = false;
                message = report.ToString();
            }
            else if (ability.Effect == AbilityEffect.Heal || ability.Effect == AbilityEffect.Cleanse || ability.Effect == AbilityEffect.Revive)
            {
                var report = new StringBuilder(actor.Definition.Name + " usou " + ability.Name + ": ");
                if (ability.Targets == TargetRule.AllAllies)
                    foreach (CombatantState ally in GetValidTargets(ability)) ApplyRecovery(actor, ability, ally, report);
                else ApplyRecovery(actor, ability, target, report);
                message = report.ToString();
            }
            else if (ability.Effect == AbilityEffect.Defend)
            {
                actor.IsDefending = true;
                message = actor.Definition.Name + " esta defendendo ate o inicio de seu proximo turno.";
            }
            else if (ability.Effect == AbilityEffect.Prepare)
            {
                actor.IsCharging = true;
                message = actor.Definition.Name + " " + ability.Description;
            }
            else if (ability.Effect == AbilityEffect.Taunt)
            {
                target.TauntedBy = actor;
                target.TauntTurns = ability.Power;
                message = actor.Definition.Name + " provocou " + target.Definition.Name + " por " + ability.Power + " turnos do alvo.";
            }
            else if (ability.Effect == AbilityEffect.MindControl || ability.Effect == AbilityEffect.Silence)
            {
                if (ability.Effect == AbilityEffect.MindControl) target.MindControlTurns = 1;
                else target.SilenceTurns = 1;
                message = actor.Definition.Name + " usou " + ability.Name + ": " + target.Definition.Name
                    + (ability.Effect == AbilityEffect.MindControl
                        ? " esta confuso e atacara um alvo aleatorio na proxima acao."
                        : " nao podera usar magia na proxima acao.");
            }
            else if(ability.Effect==AbilityEffect.Bind)
            {
                target.BoundActions=1;
                message=actor.Definition.Name+" usou Correntes: "+target.Definition.Name+" esta acorrentado.";
            }
            else
            {
                bool brokeChains = target.TryBreakChains();
                target.ProtectedBy = actor;
                message = (brokeChains ? "Correntes de " + target.Definition.Name + " quebradas. " : "")
                    + actor.Definition.Name + " protegera " + target.Definition.Name + " do proximo golpe individual, ate seu proximo turno.";
            }

            if (ability.PartyEffect != PartyEffect.None)
                message += ApplyPartyEffect(actor, ability);
            CompleteAction(actor, ref message);
            return true;
        }

        // The controller presents this forced action through the same attack/impact timeline as normal attacks.
        // The roll is supplied by the caller, keeping target choice deterministic in combat checks.
        public bool TryTakeMindControlTurn(double targetRoll, out CombatantState target, out string message)
        {
            target = null; message = string.Empty;
            CombatantState actor = CurrentActor;
            if (actor == null || !actor.IsAlive || actor.IsBound || actor.MindControlTurns <= 0) return false;
            if (double.IsNaN(targetRoll) || targetRoll < 0 || targetRoll >= 1)
                throw new ArgumentOutOfRangeException(nameof(targetRoll));
            var candidates = new List<CombatantState>();
            foreach (CombatantState participant in combatants)
                if (participant.IsAlive && participant != actor) candidates.Add(participant);
            if (candidates.Count == 0) return false;
            target = candidates[(int)(targetRoll * candidates.Count)];
            CombatantState intended = target;
            if (!target.IsBound && target.ProtectedBy != null && target.ProtectedBy.IsAlive) target = target.ProtectedBy;
            var report = new StringBuilder(actor.Definition.Name + " esta confuso! ");
            ApplyDamage(actor, MindControlAttack, intended, true, report);
            message = report.ToString();
            CompleteAction(actor, ref message);
            return true;
        }

        private string ApplyPartyEffect(CombatantState actor, AbilityDefinition ability)
        {
            var report = new StringBuilder();
            foreach (CombatantState ally in combatants)
            {
                if (ally.Team != actor.Team) continue;
                ReportBrokenChains(ally, report);
                if (ability.PartyEffect == PartyEffect.RestoreAndRevive || ability.PartyEffect == PartyEffect.RestoreCleanseAndRevive)
                {
                    bool defeated = !ally.IsAlive;
                    if (defeated) ally.ClearConditions();
                    if (ability.PartyEffect == PartyEffect.RestoreCleanseAndRevive) ally.ClearHarmfulConditions();
                    int before = ally.Hp;
                    ally.RecoverHp((int)(((long)ally.Definition.MaxHp * ability.RecoveryPercent + 99) / 100));
                    report.Append(ally.Definition.Name).Append(defeated ? " reviveu: +" : " +")
                        .Append(ally.Hp-before).Append(" HP. ");
                    if (ability.PartyEffect == PartyEffect.RestoreCleanseAndRevive) report.Append("Purificado. ");
                }
                else if (ally.IsAlive)
                {
                    ally.GrantDamageShield(ability.ShieldDuration, ally == actor);
                    report.Append(ally.Definition.Name).Append(" recebeu Shield: -50% de dano por ")
                        .Append(ability.ShieldDuration).Append(" turnos proprios. ");
                }
            }
            return report.ToString();
        }

        private void CompleteAction(CombatantState actor, ref string message)
        {
            actor.TurnsTaken++;
            if (actor.MindControlTurns > 0) actor.MindControlTurns--;
            if (actor.SilenceTurns > 0) actor.SilenceTurns--;
            if (actor.TauntTurns > 0 && --actor.TauntTurns == 0) actor.TauntedBy = null;
            RemoveDeadSources();
            Rules?.AfterAction(this);
            CheckOutcome();
            // Veneno causa dano ao fim da acao do afetado, sem criar turnos extras.
            if (Outcome == BattleOutcome.None && actor.IsAlive && actor.PoisonTurns > 0)
            {
                int previousHp = actor.Hp;
                actor.TickPoison();
                message += " Veneno: " + actor.Definition.Name + " perdeu " + (previousHp - actor.Hp) + " HP.";
                if (!actor.IsAlive) message += " Foi derrotado!";
                RemoveDeadSources();
                Rules?.AfterAction(this);
                CheckOutcome();
            }
            LastMessage = message;
            actor.AdvanceShieldDuration();
            if (Outcome == BattleOutcome.None) NextTurn();
        }

        private static void ApplyRecovery(CombatantState actor, AbilityDefinition ability, CombatantState target, StringBuilder report)
        {
            ReportBrokenChains(target, report);
            report.Append(target.Definition.Name);
            if (ability.Effect == AbilityEffect.Heal)
            {
                int previousHp = target.Hp;
                target.RecoverHp((int)Math.Min(int.MaxValue, (long)ability.Power + actor.Definition.Magic));
                report.Append(" +").Append(target.Hp - previousHp).Append(" HP. ");
            }
            else if (ability.Effect == AbilityEffect.Cleanse)
            {
                target.PoisonTurns = 0;
                report.Append(" sem veneno. ");
            }
            else
            {
                target.ClearConditions();
                target.RecoverHp((int)(((long)target.Definition.MaxHp * ability.Power + 99) / 100));
                report.Append(" reviveu com ").Append(target.Hp).Append(" HP. ");
            }
        }

        private int ApplyDamage(CombatantState actor, AbilityDefinition ability, CombatantState target,
            bool allowProtection, StringBuilder report, int hitIndex = 0, bool returnDisplayedDamage = false)
        {
            if (ReportBrokenChains(target, report)) return 0;
            if (allowProtection && ability.Effect == AbilityEffect.PhysicalDamage && target.ProtectedBy != null && target.ProtectedBy.IsAlive)
            {
                CombatantState protector = target.ProtectedBy;
                target.ProtectedBy = null;
                report.Append(protector.Definition.Name).Append(" protegeu ").Append(target.Definition.Name).Append(". ");
                target = protector;
            }
            if (ReportBrokenChains(target, report)) return 0;
            int offense = ability.Effect == AbilityEffect.MagicDamage || ability.Effect == AbilityEffect.Drain ? actor.Definition.Magic : actor.Definition.Attack;
            decimal defense = target.Definition.Defense;
            int enemyMultiplier = actor.Team == CombatTeam.Enemy ? 4 : 1;
            // Defesa 0/10/30 conserva 100%/95%/72% do dano; inclui o multiplicador antes de arredondar.
            long damage = (long)Math.Max(enemyMultiplier, ((decimal)offense + ability.Power) * enemyMultiplier * 205200m
                / (205200m + 290m * defense + 79m * defense * defense));
            int elementalPercent = target.Definition.Affinities.DamagePercent(ability.Element);
            // Defesa, afinidade e critico arredondam para baixo; Defender e Shield arredondam para cima.
            damage = Math.Max(1L, damage * elementalPercent / 100);
            if (hitIndex == ability.HitCount) damage *= ability.FinalHitMultiplier;
            bool critical = actor.Definition.CriticalChance > 0 && criticalRoll() < actor.Definition.CriticalChance;
            if (critical) damage = damage * CombatantDefinition.CriticalDamagePercent / 100;
            if (target.IsDefending) damage = (damage + 1) / 2;
            int previousHp = target.Hp;
            int calculatedDamage = (int)Math.Min(int.MaxValue, damage);
            int appliedDamage = calculatedDamage;
            if (Rules is IBattleDamageRules damageRules) appliedDamage = damageRules.LimitDamage(target, appliedDamage);
            int? displayDamage = appliedDamage < calculatedDamage ? calculatedDamage : (int?)null;
            target.TakeDamage(appliedDamage, critical, actionHitIndex: hitIndex,
                deferDefeatCleanup: hitIndex > 0 && hitIndex < ability.HitCount, displayDamage: displayDamage);
            int visibleDamage = displayDamage.HasValue ? -target.VitalChanges[target.VitalChanges.Count - 1].Amount : previousHp - target.Hp;
            report.Append(target.Definition.Name).Append(" -").Append(visibleDamage).Append(" HP");
            if (ability.Element != DamageElement.None)
            {
                report.Append(" (").Append(ElementalAffinities.NameOf(ability.Element));
                if (elementalPercent == 150) report.Append(", fraqueza");
                else if (elementalPercent == 50) report.Append(", resistencia");
                report.Append(")");
            }
            if (critical) report.Append(" (critico x1,25)");
            if (!target.IsAlive) report.Append(" (derrotado)");
            else if (ability.PoisonTurns > 0)
            {
                target.PoisonTurns = Math.Max(target.PoisonTurns, ability.PoisonTurns);
                report.Append(" (veneno)");
            }
            report.Append(". ");
            return returnDisplayedDamage ? visibleDamage : previousHp-target.Hp;
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

        private static bool ReportBrokenChains(CombatantState target, StringBuilder report)
        {
            if (!target.TryBreakChains()) return false;
            report.Append("Correntes de ").Append(target.Definition.Name).Append(" quebradas! ");
            return true;
        }

        public bool TryTakeBoundTurn(out string message)
        {
            message = string.Empty;
            if (CurrentActor == null || !CurrentActor.IsBound) return false;
            CombatantState actor = CurrentActor;
            message = actor.Definition.Name + " nao pode agir: esta acorrentado.";
            CompleteAction(actor, ref message);
            return true;
        }

        private void StartTurn()
        {
            CurrentActor.IsDefending = false;
            foreach (CombatantState participant in combatants)
                if (participant.ProtectedBy == CurrentActor) participant.ProtectedBy = null;
            if (skippingBoundTurns) return;
            skippingBoundTurns = true;
            try
            {
                // Iterate instead of nesting CompleteAction calls when several heroes are chained.
                // If every participant is bound, yield after one pass so the controller can advance safely.
                int skipped = 0;
                while (CurrentActor != null && CurrentActor.IsBound && skipped++ < combatants.Count)
                    TryTakeBoundTurn(out _);
            }
            finally { skippingBoundTurns = false; }
        }

        private void CheckOutcome()
        {
            BattleOutcome scripted=Rules?.EvaluateOutcome(this)??BattleOutcome.None;
            if(scripted!=BattleOutcome.None) {Outcome=scripted;return;}
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
