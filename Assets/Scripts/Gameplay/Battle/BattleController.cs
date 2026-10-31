using DragonQuest.Presentation.Menus;
using DragonQuest.Presentation.Cinematics.Endings;
using DragonQuest.Gameplay;
using DragonQuest.Gameplay.Battle;
using DragonQuest.Gameplay.Exploration;
using DragonQuest.Presentation.World;
using DragonQuest.Content;
using DragonQuest.Presentation.Battle;
using UnityEngine;
using DragonQuest.Audio;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Inventory;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Input;

namespace DragonQuest.Gameplay.Battle
{
    public sealed class BattleController : MonoBehaviour
    {
        private readonly BattleActionPresentation actionPresentation = new BattleActionPresentation();
        public BattleActionPresentation ActionPresentation => actionPresentation;

        private PlayerMovement movement;
        private readonly PartyConditionPresentation conditions = new PartyConditionPresentation();
        private ExplorationSession exploration;
        private BattleResolution resolution;
        private CombatantState pendingEnemy;
        private float enemyActionTime;
        private bool pendingMeteorImpact;
        private const float EscapePreparationDuration = .75f, EscapeDepartureDuration = .45f;
        private const string BossEscapeMessage = "Nao e possivel fugir de chefes.";
        public float EscapeStartedAt { get; private set; }
        public float EscapeCompletedAt { get; private set; }
        public bool IsEscaping => IsActive && (Session.Outcome == BattleOutcome.None && Session.EscapePending || Session.Outcome == BattleOutcome.Escaped);
        public float EscapeDeparture => Session?.Outcome == BattleOutcome.Escaped
            ? Mathf.Clamp01((Time.unscaledTime - EscapeCompletedAt) / EscapeDepartureDuration) : 0;
        private DialogueController dialogue;
        public GameFlowState Flow { get; private set; }
        public void SetFlow(GameFlowState flow) => Flow = flow;
        public bool IsBattleInputBlocked => IsCheatMenuBlocking || Flow != null && Flow.Owner != GameInputOwner.Battle;
        private CheatMenu cheatMenu;
        public bool IsCheatMenuBlocking => cheatMenu != null && cheatMenu.BlocksInput;
        private System.Func<bool> otherMenuOpen = () => false;
        private AncientSecrets secrets;
        private System.Action showLoadGame;
        private System.Action<GameEnding> showEnding;
        public RandomEncounterType RandomEncounter { get; private set; }
        public event System.Action<BattleOutcome> EncounterCompleted;
        public event System.Action EncounterStarted;
        public event System.Action ActionPresented;
        public bool HasEnded {get;private set;}
        public CastleBattleRules CastleRules => Session?.Rules as CastleBattleRules;
        public bool IsCastleEncounter => CastleRules!=null;
        public DragonProgress Dragon { get; private set; }
        public bool IsDragonEncounter { get; private set; }
        public string BattleTitle => IsCastleEncounter ? (CastleRules.IsSpiritRevealed?"REI RUBRO E ESPIRITO":"REI RUBRO")
            :IsDragonEncounter ? "DRAGAO CORROMPIDO" : RandomEncounterRules.Title(RandomEncounter);
        public string ReturnLabel => IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed?"Ver final"
            :"Continuar explorando";
        public string OutcomeDescription => Session.Outcome==BattleOutcome.Completed?EndingNarrative.Title(CastleRules.Ending):Session.Outcome == BattleOutcome.Victory
            ? (IsDragonEncounter ? "A essencia do dragao foi libertada. " : "Os inimigos foram vencidos. ") + RewardMessage
            : Session.Outcome == BattleOutcome.Escaped ? "A equipe fugiu sem receber ouro ou experiencia."
            : "Sua equipe foi vencida...";

        private System.Func<string> currentLocation = () => WorldNavigation.World;
        public string BattleLocationId { get; private set; } = WorldNavigation.World;
        public void SetLocationProvider(System.Func<string> provider) => currentLocation = provider ?? (() => WorldNavigation.World);
        public bool AreRandomEncountersEnabled => exploration?.IsEnabled ?? true;
        public void SetRandomEncountersEnabled(bool enabled) => exploration?.SetEnabled(enabled, currentLocation());
        public BattleEntryTransition EntryTransition { get; private set; }
        public bool IsEnteringBattle => EntryTransition != null && EntryTransition.IsPlaying;
        public bool IsActive { get; private set; }
        public bool IsMenuOpen { get; private set; }
        public System.Func<bool> TryCancelInventorySelection { get; set; }
        public bool IsBusy => Flow?.IsBusy ?? (HasEnded || IsActive || IsMenuOpen || otherMenuOpen());
        public PartyProgress Party { get; private set; }
        public BattleSession Session { get; private set; }
        public string ErrorMessage { get; private set; }
        public BattleRewards Rewards => resolution?.Rewards;
        public string RewardMessage => resolution?.RewardMessage;
        public PostBattleResults PostBattle { get; private set; }
        public bool IsPostBattleOpen { get; private set; }
        public float PostBattleStartedAt { get; private set; }
        public CombatantState PresentingActor => actionPresentation.PresentingActor;
        public AbilityDefinition PresentingAbility => actionPresentation.PresentingAbility;
        public bool IsPresentingConfusedAction => actionPresentation.IsPresentingConfusedAction;
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingTargets => actionPresentation.PresentingTargets;
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingPartyTargets => actionPresentation.PresentingPartyTargets;
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingRevivedTargets => actionPresentation.PresentingRevivedTargets;
        private bool AwaitingSalvationImpact => actionPresentation.AwaitingSalvationImpact(IsPresentingAction, ActionImpactAt);
        public BattleActionPresentation.SalvationCastState PendingSalvationState(CombatantState member)
            => actionPresentation.PendingSalvationState(member, IsPresentingAction, ActionImpactAt);
        public string PresentedMessage => AwaitingSalvationImpact
            ? PresentingActor.Definition.Name + " esta conjurando " + PresentingAbility.Name + "..."
            : IsEscaping && !IsPresentingAction ? "A equipe esta fugindo..." : Session.LastMessage;
        public bool IsPresentingUltimate => actionPresentation.IsPresentingUltimate;
        public bool IsPresentingSummon => actionPresentation.IsPresentingSummon;
        public bool IsPresentingSpell => actionPresentation.IsPresentingSpell;
        public bool IsPresentingDragonAttack => actionPresentation.IsPresentingDragonAttack;
        public float ActionStartedAt => actionPresentation.ActionStartedAt;
        public float ActionDuration => actionPresentation.ActionDuration;
        public bool IsDestructiveMeteor => IsActive
            && CastleMeteorPresentation.IsMeteor(PresentingActor?.Id, PresentingAbility)
            && (pendingMeteorImpact || CastleRules?.Ending == GameEnding.CastleDestroyed);
        public float MeteorWhiteOpacity => actionPresentation.MeteorWhiteOpacity(IsDestructiveMeteor);
        public float ActionEndsAt => actionPresentation.EndsAt(IsDestructiveMeteor);
        public bool IsPresentingAction => IsActive && actionPresentation.IsPlaying(IsDestructiveMeteor);
        public float ActionImpactAt => actionPresentation.ImpactAt(IsDestructiveMeteor);
        public int AttackPose => actionPresentation.AttackPose;

        public void Initialize(PlayerMovement hero, DialogueController dialogueController, ExplorationProgress progress,
            System.Func<bool> externalMenuOpen = null, DragonProgress dragonProgress = null,
            AncientSecrets ancientSecrets = null, System.Action defeatPresentation = null, System.Action<GameEnding> endingPresentation = null,
            PartyProgress restoredParty = null)
        {
            movement = hero; dialogue = dialogueController;
            cheatMenu = GetComponent<CheatMenu>();
            otherMenuOpen = externalMenuOpen ?? (() => false);
            Party = restoredParty ?? PartySetup.Create();
            conditions.Bind(hero, Party);
            var postBattleHud = GetComponent<PostBattleHud>();
            if (postBattleHud == null) postBattleHud = gameObject.AddComponent<PostBattleHud>();
            postBattleHud.Initialize(this);
            EntryTransition = GetComponent<BattleEntryTransition>();
            if (EntryTransition == null) EntryTransition = gameObject.AddComponent<BattleEntryTransition>();
            Dragon = dragonProgress ?? new DragonProgress(); secrets = ancientSecrets; showLoadGame = defeatPresentation;
            showEnding=endingPresentation;
            exploration = new ExplorationSession(Party, currentLocation(), () => UnityEngine.Random.value);
            resolution = new BattleResolution(Party, progress, Dragon);
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
            if (IsBusy || !Party.HasLivingMembers) return;
            StartEncounter(EncounterCatalog.CreateEncounter(Party),false);
        }

        public bool StartRandomBattle()
        {
            if (!AreRandomEncountersEnabled || IsBusy || dialogue.IsOpen || !Party.HasLivingMembers) return false;
            RandomEncounterType type = RandomEncounterRules.TypeAt(currentLocation());
            if (type == RandomEncounterType.None) return false;
            StartEncounter(EncounterCatalog.CreateRegionEncounter(Party, type, UnityEngine.Random.Range(1, 4)), false);
            RandomEncounter = type;
            return true;
        }

        private void LateUpdate()
        {
            if (movement == null || dialogue == null) return;
            var step = exploration.Advance(currentLocation(), movement.transform.position, movement.TeleportRevision,
                movement.HasMovementInput, Time.deltaTime, Flow?.BlocksMovement ?? (IsBusy || dialogue.IsOpen || !GameInput.IsFocused));
            conditions.RefreshTint();
            if (step == ExplorationStep.Defeat) FinishDefeat();
            else if (step == ExplorationStep.Encounter) StartRandomBattle();
        }
        private void OnGUI() => conditions.Draw(IsBusy || dialogue == null || dialogue.IsOpen);
        private void OnDestroy() => conditions.Dispose();

        public void StartDragonBattle()
        {
            if (IsBusy || dialogue.IsOpen || !Party.HasLivingMembers || Dragon.IsDefeated || secrets?.PassageOpen != true) return;
            StartEncounter(EncounterCatalog.CreateDragonEncounter(Party),true);
        }

        private void StartEncounter(BattleSession encounter, bool dragonEncounter)
        {
            GameInput.ConsumeFrame();
            Session = encounter; IsDragonEncounter = dragonEncounter;
            EscapeStartedAt = EscapeCompletedAt = 0;
            PostBattle = new PostBattleResults(Party, encounter);
            IsPostBattleOpen = false; PostBattleStartedAt = 0;
            RandomEncounter = RandomEncounterType.None;
            actionPresentation.Reset();
            pendingMeteorImpact = false;
            BattleLocationId = currentLocation();
            resolution.Begin();
            IsActive = true;
            pendingEnemy = null;
            ErrorMessage = string.Empty;
            movement.SetMovementEnabled(false);
            EncounterStarted?.Invoke();
            EntryTransition.Begin();
        }

        public void StartCastleBattle()
        {
            if(IsBusy||dialogue.IsOpen||!Party.HasLivingMembers) return;
            StartEncounter(EncounterCatalog.CreateCastle(Party),false);
        }

        public bool TryPlayerAction(AbilityDefinition ability, CombatantState target)
        {
            if (IsEnteringBattle || IsBattleInputBlocked) return false;
            if (!IsActive || IsPresentingAction || IsEscaping || GameInput.EscapeHeld || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            CombatantState actor = Session.CurrentActor;
            target = Session.ResolveLivingEnemyTarget(ability, target);
            var effectTargets = new System.Collections.Generic.List<CombatantState>();
            if (ability != null && (ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies))
                effectTargets.AddRange(Session.GetValidTargets(ability));
            else if (target != null)
                effectTargets.Add(ability != null && ability.Effect == AbilityEffect.PhysicalDamage
                    && !target.IsBound && target.ProtectedBy != null && target.ProtectedBy.IsAlive ? target.ProtectedBy : target);
            var revivedTargets = actionPresentation.CaptureRecovery(ability, effectTargets);
            actionPresentation.CaptureChains(Session.Combatants);
            bool accepted = Session.TryAct(actor, ability, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                actionPresentation.CapturePartyTargets(ability, actor, Session.Combatants, revivedTargets);
                PresentAction(actor, ability, effectTargets);
                AwardIfComplete();
            }
            return accepted;
        }

        public bool TryPlayerItem(ConsumableDefinition item, CombatantState target)
        {
            if (IsEnteringBattle || IsBattleInputBlocked) return false;
            if (!IsActive || IsPresentingAction || IsEscaping || GameInput.EscapeHeld || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            CombatantState actor = Session.CurrentActor;
            actionPresentation.CaptureChains(Session.Combatants);
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

        public bool ChainsVisible(CombatantState member)
            => actionPresentation.ChainsVisible(member, IsPresentingAction, ActionImpactAt);
        public float ChainBreakAge(CombatantState member)
            => actionPresentation.ChainBreakAge(member, IsPresentingAction, ActionImpactAt);
        public bool IsChainBreakTarget(CombatantState member)
            => actionPresentation.IsChainBreakTarget(member, IsPresentingAction);

        private void PresentAction(CombatantState actor, AbilityDefinition ability,
            System.Collections.Generic.IReadOnlyList<CombatantState> targets)
        {
            actionPresentation.Begin(actor, ability, targets);
            ActionPresented?.Invoke();
        }

        private void AwardIfComplete()
        {
            if (resolution.Resolve(Session, IsDragonEncounter, PostBattle)) EncounterCompleted?.Invoke(Session.Outcome);
        }

        private void UpdateEscapeInput()
        {
            if (!IsActive || Session.Outcome != BattleOutcome.None) return;
            bool held = !IsCheatMenuBlocking && GameInput.EscapeHeld;
            bool wasPending = Session.EscapePending;
            if (Session.SetEscapeHeld(held, IsPresentingAction) && !wasPending)
            {
                EscapeStartedAt = Time.unscaledTime;
                ErrorMessage = string.Empty;
            }
            if (held && !Session.CanEscape) ErrorMessage = BossEscapeMessage;
            else if (ErrorMessage == BossEscapeMessage) ErrorMessage = string.Empty;
        }

        private bool AdvanceEscape()
        {
            if (!IsActive) return false;
            if (Session.Outcome == BattleOutcome.Escaped)
            {
                if (EscapeDeparture >= 1) ReturnToCity();
                return true;
            }
            if (!Session.EscapePending || Time.unscaledTime - EscapeStartedAt < EscapePreparationDuration
                || !Session.TryTakeEscapeTurn(out _)) return false;
            if (Session.Outcome == BattleOutcome.Escaped) EscapeCompletedAt = Time.unscaledTime;
            AwardIfComplete();
            return true;
        }

        private void ResolveMeteorImpact()
        {
            if (!IsActive || !pendingMeteorImpact || Time.unscaledTime < ActionImpactAt) return;
            pendingMeteorImpact = false;
            if (Session.TryAct(PresentingActor, PresentingAbility, PresentingActor, out string message))
            { ErrorMessage = string.Empty; AwardIfComplete(); }
            else ErrorMessage = message;
        }

        private void FinishDefeat()
        {
            if (HasEnded) return;
            if (IsActive) AwardIfComplete();
            HasEnded = true; IsActive = false; IsMenuOpen = false; IsPostBattleOpen = false;
            pendingEnemy = null; exploration.ResetWalk();
            movement.SetMovementEnabled(false);
            GameInput.ConsumeFrame();
            showLoadGame?.Invoke();
        }

        private void FinishCastleBattle()
        {
            if (HasEnded)
            {
                if (GetComponent<EndingController>()?.IsFadingBattle != true) IsActive = false;
                return;
            }
            HasEnded = true; pendingEnemy = null;
            movement.SetMovementEnabled(false);
            showEnding?.Invoke(CastleRules.Ending);
            if (GetComponent<EndingController>()?.IsFadingBattle != true) IsActive = false;
        }

        private void Update()
        {
            if (HasEnded) { if (IsActive) FinishCastleBattle(); return; }
            if (IsEnteringBattle) { pendingEnemy = null; return; }
            UpdateEscapeInput();
            if (IsCheatMenuBlocking) { pendingEnemy = null; return; }
            ResolveMeteorImpact();
            if (IsDestructiveMeteor && CastleRules.Ending == GameEnding.CastleDestroyed
                && CastleMeteorPresentation.ShouldCutToCinematic(Time.unscaledTime - ActionStartedAt))
            { FinishCastleBattle(); return; }
            if (IsPresentingAction) return;
            if (IsActive && Session.Outcome == BattleOutcome.Defeat)
            { FinishDefeat(); return; }
            // The final action finishes before the cinematic; no result confirmation interrupts it.
            if (IsActive && IsCastleEncounter && Session.Outcome == BattleOutcome.Completed)
            { FinishCastleBattle(); return; }
            if (IsActive && Session.CurrentActor?.IsBound == true)
            { Session.TryTakeBoundTurn(out _); AwardIfComplete(); return; }
            if (IsActive && Session.CurrentActor?.MindControlTurns > 0)
            {
                actionPresentation.CaptureChains(Session.Combatants);
                CombatantState confused = Session.CurrentActor;
                if (Session.TryTakeMindControlTurn(System.Math.Min(UnityEngine.Random.value, .999999d), out var victim, out _))
                {
                    PresentAction(confused, BattleSession.MindControlAttack, new[] { victim });
                    ErrorMessage = string.Empty; AwardIfComplete();
                    return;
                }
            }
            if (AdvanceEscape()) return;
            if (IsActive && PostBattle?.IsComplete == true)
            {
                if (!IsPostBattleOpen) { IsPostBattleOpen = true; PostBattleStartedAt = Time.unscaledTime; }
                return;
            }
            if (Flow?.CanToggleInventory ?? (!HasEnded && !IsActive && !otherMenuOpen() && !dialogue.IsOpen && GameInput.IsFocused))
            {
                if (IsMenuOpen && GameInput.CancelPressed) { if (TryCancelInventorySelection?.Invoke() != true) CloseMenu(); GameInput.ConsumeFrame(); }
                else if (GameInput.InventoryPressed)
                {
                    if (IsMenuOpen) CloseMenu();
                    else { IsMenuOpen = true; movement.SetMovementEnabled(false); }
                    GameInput.ConsumeFrame();
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
            CombatantState selectedTarget = EnemyAI.ChooseTarget(Session, ability, System.Math.Min(UnityEngine.Random.value, .999999d));
            var effectTargets = new System.Collections.Generic.List<CombatantState>();
            if (ability != null)
            {
                var valid = Session.GetValidTargets(ability);
                if (ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies) effectTargets.AddRange(valid);
                else if (selectedTarget != null)
                    effectTargets.Add(ability.Effect == AbilityEffect.PhysicalDamage
                        && !selectedTarget.IsBound && selectedTarget.ProtectedBy != null && selectedTarget.ProtectedBy.IsAlive ? selectedTarget.ProtectedBy : selectedTarget);
            }
            if (CastleMeteorPresentation.IsMeteor(actor.Id, ability))
            {
                // Todos os derrotados esperam o impacto; a absorcao aponta para o portador real.
                effectTargets.Clear();
                CombatantState bearer = CastleRules.LivingArmorBearer(Session);
                if (bearer != null) effectTargets.Add(bearer);
                else effectTargets.AddRange(Session.Combatants);
            }
            string message = "Inimigo sem acao valida.";
            actionPresentation.CaptureChains(Session.Combatants);
            if (CastleMeteorPresentation.IsMeteor(actor.Id, ability) && CastleRules.LivingArmorBearer(Session) == null)
            {
                // O HP e o resultado so mudam quando a carga termina, junto com o clarao.
                pendingMeteorImpact = true;
                PresentAction(actor, ability, effectTargets);
                pendingEnemy = null;
                return;
            }
            bool accepted = ability != null && selectedTarget != null && Session.TryAct(actor, ability, selectedTarget, out message);
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
            if (IsEnteringBattle || IsCheatMenuBlocking) return;
            if (!IsActive || IsPresentingAction || Session.Outcome == BattleOutcome.None) return;
            if (Session.Outcome == BattleOutcome.Defeat) { FinishDefeat(); return; }
            if (PostBattleResults.IsVictory(Session) && (!IsPostBattleOpen || !PostBattle.CanContinue(Time.unscaledTime - PostBattleStartedAt))) return;
            IsPostBattleOpen = false;
            if(IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed)
            {
                FinishCastleBattle(); return;
            }
            AwardIfComplete();
            IsActive = false;
            exploration.Reset(currentLocation());
            movement.SetMovementEnabled(true);
            pendingEnemy = null;
            if (IsDragonEncounter && Session.Outcome == BattleOutcome.Victory)
            {
                dialogue.Open(DialogueCatalog.DragonEssence, () =>
                    {
                        MenuSounds.Play(MenuSound.Confirm);
                        dialogue.Open(DialogueCatalog.DraconicArmorReceived);
                    });
                movement.SetMovementEnabled(false);
            }
        }

    }
}
