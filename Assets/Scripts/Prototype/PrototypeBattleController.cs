using UnityEngine;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using UnityEngine.InputSystem;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeBattleController : MonoBehaviour
    {
        private PlayerMovement movement;
        private CombatantState pendingEnemy;
        private float enemyActionTime;
        private DialogueController dialogue;
        private System.Func<bool> otherMenuOpen = () => false;
        private AncientSecrets secrets;
        private System.Action recoverAtInn;
        private System.Action<GameEnding> showEnding;
        private bool notifiedCompletion;
        private readonly RandomEncounterRules randomEncounters = new RandomEncounterRules(() => UnityEngine.Random.value);
        private Vector2 lastWalkPosition;
        private int lastTeleportRevision;
        private bool trackingWalk;
        public RandomEncounterType RandomEncounter { get; private set; }
        public event System.Action<BattleOutcome> EncounterCompleted;
        public bool HasEnded {get;private set;}
        public CastleBattleRules CastleRules => Session?.Rules as CastleBattleRules;
        public bool IsCastleEncounter => CastleRules!=null;
        public DragonProgress Dragon { get; private set; }
        public bool IsDragonEncounter { get; private set; }
        public string BattleTitle => IsCastleEncounter ? (CastleRules.IsSpiritRevealed?"TIRANO E ESPIRITO":"TIRANO DOMINADO")
            :IsDragonEncounter ? "DRAGAO CORROMPIDO" : RandomEncounterRules.Title(RandomEncounter);
        public string ReturnLabel => IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed?"Ver final"
            :Session.Outcome==BattleOutcome.Defeat?"Voltar para a hospedaria":"Continuar explorando";
        public string OutcomeDescription => Session.Outcome==BattleOutcome.Completed?EndingNarrative.Title(CastleRules.Ending):Session.Outcome == BattleOutcome.Victory
            ? (IsDragonEncounter ? "A essencia do dragao foi libertada. " : "Os inimigos foram vencidos. ") + RewardMessage
            : "Sua equipe foi vencida. Acordara na hospedaria; Limit e progresso serao mantidos.";

        private System.Func<string> currentLocation = () => PrototypeTravel.World;
        public string BattleLocationId { get; private set; } = PrototypeTravel.World;
        public void SetLocationProvider(System.Func<string> provider) => currentLocation = provider ?? (() => PrototypeTravel.World);
        public bool IsActive { get; private set; }
        public bool IsMenuOpen { get; private set; }
        public bool IsBusy => HasEnded || IsActive || IsMenuOpen || otherMenuOpen();
        public PartyProgress Party { get; private set; }
        public BattleSession Session { get; private set; }
        public string ErrorMessage { get; private set; }
        public BattleRewards Rewards { get; private set; }
        public string RewardMessage { get; private set; }
        public CombatantState PresentingActor { get; private set; }
        public AbilityDefinition PresentingAbility { get; private set; }
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingTargets { get; private set; } = new CombatantState[0];
        public bool IsPresentingSummon => SummonAnimation.Resource(PresentingAbility) != null;
        public float ActionDuration => IsPresentingDragonAttack ? DragonAttackAnimation.Duration(PresentingAbility.Id)
            : IsPresentingSummon ? SummonAnimation.Duration : IsPresentingSpell ? SpellAnimation.Duration
            : BattleAnimationCatalog.Duration(PresentingActor?.Id, PresentingAbility);
        public bool IsPresentingSpell => SpellAnimation.Resource(PresentingAbility) != null;
        public float ActionStartedAt { get; private set; }
        public bool IsPresentingDragonAttack => PresentingActor?.Id == DragonProgress.EnemyId
            && DragonAttackAnimation.Resource(PresentingAbility?.Id) != null;
        public bool IsPresentingAction => IsActive && PresentingActor != null && Time.unscaledTime < ActionStartedAt
            + ActionDuration;
        public float ActionImpactAt => ActionStartedAt
            + (IsPresentingDragonAttack ? DragonAttackAnimation.ImpactDelay(PresentingAbility.Id) : IsPresentingSummon ? SummonAnimation.ImpactDelay
            : IsPresentingSpell ? SpellAnimation.ImpactDelay : BattleAnimationCatalog.ImpactDelay(PresentingActor?.Id, PresentingAbility));
        public int AttackPose => BattleAnimationCatalog.Frame(BattleAnimationCatalog.Resource(PresentingActor?.Id, PresentingAbility),
            Time.unscaledTime - ActionStartedAt, ActionDuration);

        public void Initialize(PlayerMovement hero, DialogueController dialogueController, ExplorationProgress progress,
            System.Func<bool> externalMenuOpen = null, DragonProgress dragonProgress = null,
            AncientSecrets ancientSecrets = null, System.Action recovery = null, System.Action<GameEnding> endingPresentation = null,
            PartyProgress restoredParty = null)
        {
            movement = hero; dialogue = dialogueController;
            otherMenuOpen = externalMenuOpen ?? (() => false);
            Party = restoredParty ?? PrototypePartyFactory.Create();
            Rewards = new BattleRewards(progress);
            Dragon = dragonProgress ?? new DragonProgress(); secrets = ancientSecrets; recoverAtInn = recovery;
            showEnding=endingPresentation;
            trackingWalk = false;
            randomEncounters.Reset(currentLocation());
        }

        public void RestoreEnding(GameEnding ending)
        {
            if (ending == GameEnding.None) return;
            HasEnded = true; movement.SetMovementEnabled(false); showEnding?.Invoke(ending);
        }

        public void CloseMenu()
        {
            IsMenuOpen = false;
            movement.SetMovementEnabled(!IsActive && !dialogue.IsOpen);
        }

        public void StartBattle()
        {
            if (IsBusy) return;
            StartEncounter(PrototypeBattleFactory.CreateEncounter(Party),false);
        }

        public bool StartRandomBattle()
        {
            if (IsBusy || dialogue.IsOpen) return false;
            RandomEncounterType type = RandomEncounterRules.TypeAt(currentLocation());
            if (type == RandomEncounterType.None) return false;
            StartEncounter(PrototypeBattleFactory.CreateRegionEncounter(Party, type, UnityEngine.Random.Range(1, 4)), false);
            RandomEncounter = type;
            return true;
        }

        private void LateUpdate()
        {
            if (movement == null || dialogue == null) return;
            Vector2 position = movement.transform.position;
            bool teleported = !trackingWalk || lastTeleportRevision != movement.TeleportRevision;
            float distance = trackingWalk ? Vector2.Distance(position, lastWalkPosition) : 0;
            lastWalkPosition = position; lastTeleportRevision = movement.TeleportRevision; trackingWalk = true;
            bool blocked = IsBusy || dialogue.IsOpen || !Application.isFocused || !movement.HasMovementInput;
            if (randomEncounters.Advance(currentLocation(), distance, blocked, teleported)) StartRandomBattle();
        }

        public void StartDragonBattle()
        {
            if (IsBusy || dialogue.IsOpen || Dragon.IsDefeated || secrets?.PassageOpen != true) return;
            StartEncounter(PrototypeBattleFactory.CreateDragonEncounter(Party),true);
        }

        private void StartEncounter(BattleSession encounter, bool dragonEncounter)
        {
            Session = encounter; IsDragonEncounter = dragonEncounter;
            RandomEncounter = RandomEncounterType.None;
            PresentingActor = null; PresentingAbility = null; PresentingTargets = new CombatantState[0];
            BattleLocationId = currentLocation();
            notifiedCompletion = false;
            IsActive = true;
            pendingEnemy = null;
            ErrorMessage = string.Empty;
            RewardMessage = string.Empty;
            movement.SetMovementEnabled(false);
        }

        public void StartCastleBattle()
        {
            if(IsBusy||dialogue.IsOpen) return;
            StartEncounter(PrototypeCastleBattleFactory.Create(Party),false);
        }

        public bool TryPlayerAction(AbilityDefinition ability, CombatantState target)
        {
            if (!IsActive || IsPresentingAction || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            CombatantState actor = Session.CurrentActor;
            var effectTargets = new System.Collections.Generic.List<CombatantState>();
            if (ability != null && (ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies))
                effectTargets.AddRange(Session.GetValidTargets(ability));
            else if (target != null)
                effectTargets.Add(ability != null && ability.Effect == AbilityEffect.PhysicalDamage
                    && target.ProtectedBy != null && target.ProtectedBy.IsAlive ? target.ProtectedBy : target);
            bool accepted = Session.TryAct(actor, ability, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                PresentAction(actor, ability, effectTargets);
                AwardIfComplete();
            }
            return accepted;
        }

        public bool TryPlayerItem(ConsumableDefinition item, CombatantState target)
        {
            if (!IsActive || IsPresentingAction || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            CombatantState actor = Session.CurrentActor;
            bool accepted = Session.TryUseItem(actor, item, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                PresentAction(actor, new AbilityDefinition("use-item", item.Name, "Usa um item.", 0, 0, AbilityEffect.Defend, TargetRule.Self),
                    new[] { target });
                AwardIfComplete();
            }
            return accepted;
        }

        private void PresentAction(CombatantState actor, AbilityDefinition ability,
            System.Collections.Generic.IReadOnlyList<CombatantState> targets)
        {
            PresentingActor = actor; PresentingAbility = ability; PresentingTargets = targets;
            ActionStartedAt = Time.unscaledTime;
        }

        private void AwardIfComplete()
        {
            if (Session.Outcome == BattleOutcome.None) return;
            if (Session.Outcome == BattleOutcome.Victory)
            {
                if (IsDragonEncounter) Dragon.TryComplete(Session,Party);
                if (!Rewards.HasClaimed(Session))
                {
                    Rewards.TryClaim(Session,out string message);
                    RewardMessage = message + (IsDragonEncounter && Dragon.IsDefeated ? " Armadura draconica recebida!" : "");
                }
            }
            if (notifiedCompletion) return;
            notifiedCompletion = true; EncounterCompleted?.Invoke(Session.Outcome);
        }

        private void Update()
        {
            if (IsPresentingAction) return;
            Keyboard keyboard = Keyboard.current;
            if (!HasEnded && !IsActive && !otherMenuOpen() && !dialogue.IsOpen && keyboard != null && Application.isFocused)
            {
                if (IsMenuOpen && keyboard.escapeKey.wasPressedThisFrame) CloseMenu();
                else if (keyboard.iKey.wasPressedThisFrame)
                {
                    if (IsMenuOpen) CloseMenu();
                    else { IsMenuOpen = true; movement.SetMovementEnabled(false); }
                }
            }
            if (!IsActive || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Enemy)
            { pendingEnemy = null; return; }
            if (pendingEnemy != Session.CurrentActor)
            {
                pendingEnemy = Session.CurrentActor;
                enemyActionTime = Time.unscaledTime + 0.9f;
            }
            if (Time.unscaledTime < enemyActionTime) return;
            CombatantState actor = Session.CurrentActor;
            AbilityDefinition ability = EnemyAI.ChooseAbility(Session);
            var effectTargets = new System.Collections.Generic.List<CombatantState>();
            if (ability != null)
            {
                var valid = Session.GetValidTargets(ability);
                if (ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies) effectTargets.AddRange(valid);
                else if (ability.Targets == TargetRule.Self) effectTargets.Add(actor);
                else
                {
                    CombatantState weakest = null;
                    foreach (CombatantState target in valid)
                        if (weakest == null || (long)target.Hp * weakest.Definition.MaxHp < (long)weakest.Hp * target.Definition.MaxHp) weakest = target;
                    if (weakest != null) effectTargets.Add(ability.Effect == AbilityEffect.PhysicalDamage
                        && weakest.ProtectedBy != null && weakest.ProtectedBy.IsAlive ? weakest.ProtectedBy : weakest);
                }
            }
            bool accepted = EnemyAI.TryTakeTurn(Session, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                PresentAction(actor, ability, effectTargets);
                AwardIfComplete();
            }
            pendingEnemy = null;
        }

        public void ReturnToCity()
        {
            if (!IsActive || IsPresentingAction || Session.Outcome == BattleOutcome.None) return;
            if(IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed)
            {
                IsActive=false;HasEnded=true;pendingEnemy=null;movement.SetMovementEnabled(false);
                showEnding?.Invoke(CastleRules.Ending);return;
            }
            AwardIfComplete();
            IsActive = false;
            randomEncounters.Reset(currentLocation());
            trackingWalk = false;
            movement.SetMovementEnabled(true);
            pendingEnemy = null;
            if (Session.Outcome == BattleOutcome.Defeat)
            {
                recoverAtInn?.Invoke();
                dialogue.Open("Hospedaria","A equipe desperta em seguranca. Seus ferimentos foram tratados e o MP foi recuperado.",
                    "O Limit acumulado permanece. Os consumiveis usados foram gastos; seus segredos, ouro e equipamentos continuam com voce.");
                movement.SetMovementEnabled(false);
            }
            else if (IsDragonEncounter && Session.Outcome == BattleOutcome.Victory)
            {
                dialogue.Open("Essencia do dragao","A sombra abandona o dragao, revelando uma chama serena. A mesma presenca que ocupava seu corpo ainda vive no castelo.",
                    "A essencia deixou a Armadura draconica. Equipe-a pelo inventario: ela podera revelar quem se esconde atras do trono.");
                movement.SetMovementEnabled(false);
            }
        }

    }
}
