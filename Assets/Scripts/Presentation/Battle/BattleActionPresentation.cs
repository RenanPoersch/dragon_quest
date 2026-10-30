using System.Collections.Generic;
using DragonQuest.Combat;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEngine;

namespace DragonQuest.Presentation.Battle
{
    // Visual state and timing of an accepted action. Combat execution remains with the session/controller.
    public sealed class BattleActionPresentation
    {
        public CombatantState PresentingActor { get; private set; }
        public AbilityDefinition PresentingAbility { get; private set; }
        public bool IsPresentingConfusedAction { get; private set; }
        public IReadOnlyList<CombatantState> PresentingTargets { get; private set; } = new CombatantState[0];
        public IReadOnlyList<CombatantState> PresentingPartyTargets { get; private set; } = new CombatantState[0];
        public IReadOnlyList<CombatantState> PresentingRevivedTargets { get; private set; } = new CombatantState[0];
        public float ActionStartedAt { get; private set; }
        private readonly Dictionary<CombatantState, SalvationCastState> salvationBeforeCast = new Dictionary<CombatantState, SalvationCastState>();
        private readonly Dictionary<CombatantState, bool> chainsBeforeAction = new Dictionary<CombatantState, bool>();

        public sealed class SalvationCastState
        {
            public int Hp { get; }
            public Color Tint { get; }
            public IReadOnlyList<StatusIndicator> Statuses { get; }
            public SalvationCastState(CombatantState member)
            {
                Hp = member.Hp;
                Tint = member.IsAlive ? BattleStatusColors.Tint(member) : new Color(.4f, .4f, .4f);
                Statuses = CombatStatuses.Active(member);
            }
        }

        public bool IsPresentingUltimate => UltimateTimeline.IsUltimate(PresentingAbility);
        public bool IsPresentingSummon => SummonAnimation.Resource(PresentingAbility) != null;
        public bool IsPresentingSpell => SpellAnimation.Resource(PresentingAbility) != null;
        public bool IsPresentingDragonAttack => PresentingActor?.Id == DragonProgress.EnemyId
            && DragonAttackAnimation.Resource(PresentingAbility?.Id) != null;
        public float ActionDuration => IsPresentingDragonAttack ? DragonAttackAnimation.Duration(PresentingAbility.Id)
            : IsPresentingUltimate ? UltimateTimeline.Duration(PresentingAbility)
            : IsPresentingSummon ? SummonAnimation.DurationFor(PresentingAbility) : IsPresentingSpell ? SpellAnimation.DurationFor(PresentingAbility)
            : BattleAnimationCatalog.Duration(PresentingActor?.Id, PresentingAbility);
        public int AttackPose => BattleAnimationCatalog.Frame(BattleAnimationCatalog.Resource(PresentingActor?.Id, PresentingAbility),
            Time.unscaledTime - ActionStartedAt, ActionDuration);

        public float ImpactAt(bool destructiveMeteor) => ActionStartedAt
            + (destructiveMeteor ? CastleMeteorPresentation.ThrowImpactAt
            : IsPresentingDragonAttack ? DragonAttackAnimation.ImpactDelay(PresentingAbility.Id)
            : IsPresentingUltimate ? UltimateTimeline.ImpactDelay(PresentingAbility) : IsPresentingSummon ? SummonAnimation.ImpactDelayFor(PresentingAbility)
            : IsPresentingSpell ? SpellAnimation.ImpactDelayFor(PresentingAbility) : BattleAnimationCatalog.ImpactDelay(PresentingActor?.Id, PresentingAbility));

        public float EndsAt(bool destructiveMeteor)
        {
            float impact = ImpactAt(destructiveMeteor);
            return Mathf.Max(DefeatedEnemyPresentation.EndOfAction(ActionStartedAt + ActionDuration, impact, PresentingTargets),
                HasChainBreak ? impact + ChainPrisonAnimation.BreakDuration : 0);
        }

        public bool IsPlaying(bool destructiveMeteor) => PresentingActor != null && Time.unscaledTime < EndsAt(destructiveMeteor);
        public float MeteorWhiteOpacity(bool destructiveMeteor) => destructiveMeteor
            ? CastleMeteorPresentation.WhiteOpacity(Time.unscaledTime - ActionStartedAt) : 0;

        public void Reset()
        {
            PresentingActor = null; PresentingAbility = null;
            PresentingTargets = new CombatantState[0];
            PresentingPartyTargets = new CombatantState[0];
            PresentingRevivedTargets = new CombatantState[0];
            salvationBeforeCast.Clear(); chainsBeforeAction.Clear();
            IsPresentingConfusedAction = false; ActionStartedAt = 0;
        }

        public void Begin(CombatantState actor, AbilityDefinition ability, IReadOnlyList<CombatantState> targets)
        {
            PresentingActor = actor; PresentingAbility = ability; PresentingTargets = targets;
            IsPresentingConfusedAction = ability == BattleSession.MindControlAttack;
            ActionStartedAt = Time.unscaledTime;
        }

        public IReadOnlyList<CombatantState> CaptureRecovery(AbilityDefinition ability, IReadOnlyList<CombatantState> targets)
        {
            var revived = new List<CombatantState>();
            salvationBeforeCast.Clear();
            if (ability?.PartyEffect == PartyEffect.RestoreCleanseAndRevive)
                foreach (CombatantState ally in targets)
                {
                    salvationBeforeCast.Add(ally, new SalvationCastState(ally));
                    if (!ally.IsAlive) revived.Add(ally);
                }
            return revived;
        }

        public void CapturePartyTargets(AbilityDefinition ability, CombatantState actor,
            IReadOnlyList<CombatantState> combatants, IReadOnlyList<CombatantState> revived)
        {
            var targets = new List<CombatantState>();
            if (ability.PartyEffect != PartyEffect.None)
                foreach (CombatantState ally in combatants)
                    if (ally.Team == actor.Team) targets.Add(ally);
            PresentingPartyTargets = targets; PresentingRevivedTargets = revived;
        }

        public bool AwaitingSalvationImpact(bool presenting, float impactAt) => presenting
            && PresentingAbility.SourceAbilityId == "lia-salvation" && Time.unscaledTime < impactAt;
        public SalvationCastState PendingSalvationState(CombatantState member, bool presenting, float impactAt)
            => AwaitingSalvationImpact(presenting, impactAt) && salvationBeforeCast.TryGetValue(member, out var before) ? before : null;

        public void CaptureChains(IReadOnlyList<CombatantState> combatants)
        {
            chainsBeforeAction.Clear();
            foreach (CombatantState member in combatants) chainsBeforeAction[member] = member.IsBound;
        }

        private bool HasChainBreak
        {
            get
            {
                foreach (var before in chainsBeforeAction)
                    if (before.Value && !before.Key.IsBound) return true;
                return false;
            }
        }

        public bool ChainsVisible(CombatantState member, bool presenting, float impactAt) => presenting && Time.unscaledTime < impactAt
            && chainsBeforeAction.TryGetValue(member, out bool before) ? before : member.IsBound;
        public float ChainBreakAge(CombatantState member, bool presenting, float impactAt) => presenting && !member.IsBound
            && chainsBeforeAction.TryGetValue(member, out bool before) && before ? Time.unscaledTime - impactAt : -1;
        public bool IsChainBreakTarget(CombatantState member, bool presenting) => presenting && !member.IsBound
            && chainsBeforeAction.TryGetValue(member, out bool before) && before;
    }
}
