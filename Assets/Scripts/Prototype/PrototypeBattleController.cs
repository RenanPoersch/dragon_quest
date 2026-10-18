using UnityEngine;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Interactions;
using DragonQuest.Progression;
using DragonQuest.Input;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeBattleController : MonoBehaviour
    {
        private PlayerMovement movement;
        private SpriteRenderer explorationSprite;
        private GUIStyle poisonNumber;
        private readonly System.Collections.Generic.List<MapPoisonNumber> mapPoisonNumbers = new System.Collections.Generic.List<MapPoisonNumber>();
        private sealed class MapPoisonNumber
        {
            public string Name;
            public int Damage, Lane;
            public float Started;
            public Vector3 Position;
        }
        private CombatantState pendingEnemy;
        private float enemyActionTime;
        private bool pendingMeteorImpact;
        private DialogueController dialogue;
        private PrototypeCheatMenu cheatMenu;
        public bool IsCheatMenuBlocking => cheatMenu != null && cheatMenu.BlocksInput;
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
        public bool AreRandomEncountersEnabled => randomEncounters.IsEnabled;
        public void SetRandomEncountersEnabled(bool enabled) => randomEncounters.SetEnabled(enabled, currentLocation());
        public bool IsActive { get; private set; }
        public bool IsMenuOpen { get; private set; }
        public System.Func<bool> TryCancelInventorySelection { get; set; }
        public bool IsBusy => HasEnded || IsActive || IsMenuOpen || otherMenuOpen();
        public PartyProgress Party { get; private set; }
        public BattleSession Session { get; private set; }
        public string ErrorMessage { get; private set; }
        public BattleRewards Rewards { get; private set; }
        public string RewardMessage { get; private set; }
        public PostBattleResults PostBattle { get; private set; }
        public bool IsPostBattleOpen { get; private set; }
        public float PostBattleStartedAt { get; private set; }
        public CombatantState PresentingActor { get; private set; }
        public AbilityDefinition PresentingAbility { get; private set; }
        public bool IsPresentingConfusedAction { get; private set; }
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingTargets { get; private set; } = new CombatantState[0];
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingPartyTargets { get; private set; } = new CombatantState[0];
        public System.Collections.Generic.IReadOnlyList<CombatantState> PresentingRevivedTargets { get; private set; } = new CombatantState[0];
        public sealed class SalvationCastState
        {
            public int Hp { get; }
            public Color Tint { get; }
            public System.Collections.Generic.IReadOnlyList<StatusIndicator> Statuses { get; }
            public SalvationCastState(CombatantState member)
            {
                Hp = member.Hp;
                Tint = member.IsAlive ? BattleStatusColors.Tint(member) : new Color(.4f, .4f, .4f);
                Statuses = CombatStatuses.Active(member);
            }
        }
        private readonly System.Collections.Generic.Dictionary<CombatantState, SalvationCastState> salvationBeforeCast
            = new System.Collections.Generic.Dictionary<CombatantState, SalvationCastState>();
        private bool AwaitingSalvationImpact => IsPresentingAction && PresentingAbility.SourceAbilityId == "lia-salvation"
            && Time.unscaledTime < ActionImpactAt;
        public SalvationCastState PendingSalvationState(CombatantState member) => AwaitingSalvationImpact
            && salvationBeforeCast.TryGetValue(member, out var before) ? before : null;
        public string PresentedMessage => AwaitingSalvationImpact
            ? PresentingActor.Definition.Name + " esta conjurando " + PresentingAbility.Name + "..." : Session.LastMessage;
        public bool IsPresentingUltimate => UltimateTimeline.IsUltimate(PresentingAbility);
        public bool IsPresentingSummon => SummonAnimation.Resource(PresentingAbility) != null;
        public float ActionDuration => IsPresentingDragonAttack ? DragonAttackAnimation.Duration(PresentingAbility.Id)
            : IsPresentingUltimate ? UltimateTimeline.Duration(PresentingAbility)
            : IsPresentingSummon ? SummonAnimation.Duration : IsPresentingSpell ? SpellAnimation.Duration
            : BattleAnimationCatalog.Duration(PresentingActor?.Id, PresentingAbility);
        public bool IsDestructiveMeteor => IsActive
            && CastleMeteorPresentation.IsMeteor(PresentingActor?.Id, PresentingAbility)
            && (pendingMeteorImpact || CastleRules?.Ending == GameEnding.CastleDestroyed);
        public float MeteorWhiteOpacity => IsDestructiveMeteor
            ? CastleMeteorPresentation.WhiteOpacity(Time.unscaledTime - ActionStartedAt) : 0;
        public bool IsPresentingSpell => SpellAnimation.Resource(PresentingAbility) != null;
        public float ActionStartedAt { get; private set; }
        public bool IsPresentingDragonAttack => PresentingActor?.Id == DragonProgress.EnemyId
            && DragonAttackAnimation.Resource(PresentingAbility?.Id) != null;
        public float ActionEndsAt => Mathf.Max(
            DefeatedEnemyPresentation.EndOfAction(ActionStartedAt + ActionDuration, ActionImpactAt, PresentingTargets),
            HasChainBreak ? ActionImpactAt + ChainPrisonAnimation.BreakDuration : 0);
        public bool IsPresentingAction => IsActive && PresentingActor != null && Time.unscaledTime < ActionEndsAt;
        public float ActionImpactAt => ActionStartedAt
            + (IsPresentingDragonAttack ? DragonAttackAnimation.ImpactDelay(PresentingAbility.Id)
            : IsPresentingUltimate ? UltimateTimeline.ImpactDelay(PresentingAbility) : IsPresentingSummon ? SummonAnimation.ImpactDelay
            : IsPresentingSpell ? SpellAnimation.ImpactDelay : BattleAnimationCatalog.ImpactDelay(PresentingActor?.Id, PresentingAbility));
        public int AttackPose => BattleAnimationCatalog.Frame(BattleAnimationCatalog.Resource(PresentingActor?.Id, PresentingAbility),
            Time.unscaledTime - ActionStartedAt, ActionDuration);

        public void Initialize(PlayerMovement hero, DialogueController dialogueController, ExplorationProgress progress,
            System.Func<bool> externalMenuOpen = null, DragonProgress dragonProgress = null,
            AncientSecrets ancientSecrets = null, System.Action recovery = null, System.Action<GameEnding> endingPresentation = null,
            PartyProgress restoredParty = null)
        {
            movement = hero; dialogue = dialogueController;
            cheatMenu = GetComponent<PrototypeCheatMenu>();
            otherMenuOpen = externalMenuOpen ?? (() => false);
            if (Party != null) Party.PoisonDamage -= ShowMapPoison;
            Party = restoredParty ?? PrototypePartyFactory.Create();
            Party.PoisonDamage += ShowMapPoison;
            mapPoisonNumbers.Clear();
            explorationSprite = hero.GetComponentInChildren<PixelCharacter>()?.GetComponent<SpriteRenderer>();
            Rewards = new BattleRewards(progress, Party);
            var postBattleHud = GetComponent<PrototypePostBattleHud>();
            if (postBattleHud == null) postBattleHud = gameObject.AddComponent<PrototypePostBattleHud>();
            postBattleHud.Initialize(this);
            Dragon = dragonProgress ?? new DragonProgress(); secrets = ancientSecrets; recoverAtInn = recovery;
            showEnding=endingPresentation;
            trackingWalk = false;
            randomEncounters.Reset(currentLocation());
            randomEncounters.SetEnabled(true, currentLocation());
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
            StartEncounter(PrototypeBattleFactory.CreateEncounter(Party),false);
        }

        public bool StartRandomBattle()
        {
            if (!AreRandomEncountersEnabled || IsBusy || dialogue.IsOpen || !Party.HasLivingMembers) return false;
            RandomEncounterType type = RandomEncounterRules.TypeAt(currentLocation());
            if (type == RandomEncounterType.None) return false;
            StartEncounter(PrototypeBattleFactory.CreateRegionEncounter(Party, type, UnityEngine.Random.Range(1, 4)), false);
            RandomEncounter = type;
            return true;
        }

        private void LateUpdate()
        {
            if (movement == null || dialogue == null) return;
            bool paused = IsBusy || dialogue.IsOpen || !GameInput.IsFocused;
            Party.AdvancePoison(Time.deltaTime, paused);
            if (explorationSprite != null && Party.Members.Count > 0)
            {
                var leader = Party.Members[0];
                explorationSprite.color = leader.IsAlive ? BattleStatusColors.Tint(leader.IsPoisoned, leader.BoundActions) : new Color(.4f, .4f, .4f);
            }
            if (!paused && !Party.HasLivingMembers)
            {
                Party.RecoverAtInn();
                recoverAtInn?.Invoke();
                trackingWalk = false;
                dialogue.Open("Hospedaria", "O veneno venceu a equipe. Voces despertam na hospedaria com HP e MP recuperados.");
                movement.SetMovementEnabled(false);
                return;
            }
            Vector2 position = movement.transform.position;
            bool teleported = !trackingWalk || lastTeleportRevision != movement.TeleportRevision;
            float distance = trackingWalk ? Vector2.Distance(position, lastWalkPosition) : 0;
            lastWalkPosition = position; lastTeleportRevision = movement.TeleportRevision; trackingWalk = true;
            bool blocked = IsBusy || dialogue.IsOpen || !GameInput.IsFocused || !movement.HasMovementInput;
            if (randomEncounters.Advance(currentLocation(), distance, blocked, teleported)) StartRandomBattle();
        }

        private void ShowMapPoison(PartyMember member, int damage)
        {
            mapPoisonNumbers.Add(new MapPoisonNumber { Name = member.Name, Damage = damage,
                Lane = mapPoisonNumbers.Count % Party.Members.Count, Started = Time.unscaledTime,
                Position = movement.transform.position });
        }

        private void OnGUI()
        {
            mapPoisonNumbers.RemoveAll(number => Time.unscaledTime - number.Started > 1.4f);
            if (IsBusy || dialogue == null || dialogue.IsOpen || Camera.main == null) return;
            if (poisonNumber == null)
            {
                poisonNumber = PrototypeTypography.Style(GUI.skin.label, 24, true);
                poisonNumber.alignment = TextAnchor.MiddleCenter;
            }
            foreach (var number in mapPoisonNumbers)
            {
                float elapsed = Time.unscaledTime - number.Started;
                Vector3 point = Camera.main.WorldToScreenPoint(number.Position);
                Rect rect = new Rect(point.x - 120, Screen.height - point.y - 75 - elapsed * 40 - number.Lane * 28, 240, 36);
                Color color = BattleStatusColors.DamageOverTime;
                color.a = Mathf.Clamp01((1.4f - elapsed) / .35f);
                string label = number.Name + " -" + number.Damage;
                poisonNumber.normal.textColor = new Color(0, 0, 0, color.a);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), label, poisonNumber);
                poisonNumber.normal.textColor = color;
                GUI.Label(rect, label, poisonNumber);
            }
        }

        public void StartDragonBattle()
        {
            if (IsBusy || dialogue.IsOpen || !Party.HasLivingMembers || Dragon.IsDefeated || secrets?.PassageOpen != true) return;
            StartEncounter(PrototypeBattleFactory.CreateDragonEncounter(Party),true);
        }

        private void StartEncounter(BattleSession encounter, bool dragonEncounter)
        {
            GameInput.ConsumeGamepadFrame();
            Session = encounter; IsDragonEncounter = dragonEncounter;
            PostBattle = new PostBattleResults(Party, encounter);
            IsPostBattleOpen = false; PostBattleStartedAt = 0;
            RandomEncounter = RandomEncounterType.None;
            PresentingActor = null; PresentingAbility = null; PresentingTargets = new CombatantState[0];
            PresentingPartyTargets = new CombatantState[0];
            PresentingRevivedTargets = new CombatantState[0];
            salvationBeforeCast.Clear();
            pendingMeteorImpact = false;
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
            if(IsBusy||dialogue.IsOpen||!Party.HasLivingMembers) return;
            StartEncounter(PrototypeCastleBattleFactory.Create(Party),false);
        }

        public bool TryPlayerAction(AbilityDefinition ability, CombatantState target)
        {
            if (IsCheatMenuBlocking) return false;
            if (!IsActive || IsPresentingAction || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            CombatantState actor = Session.CurrentActor;
            target = Session.ResolveLivingEnemyTarget(ability, target);
            var effectTargets = new System.Collections.Generic.List<CombatantState>();
            if (ability != null && (ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies))
                effectTargets.AddRange(Session.GetValidTargets(ability));
            else if (target != null)
                effectTargets.Add(ability != null && ability.Effect == AbilityEffect.PhysicalDamage
                    && !target.IsBound && target.ProtectedBy != null && target.ProtectedBy.IsAlive ? target.ProtectedBy : target);
            var revivedTargets = new System.Collections.Generic.List<CombatantState>();
            salvationBeforeCast.Clear();
            if (ability?.PartyEffect == PartyEffect.RestoreCleanseAndRevive)
                foreach (CombatantState ally in effectTargets)
                {
                    salvationBeforeCast.Add(ally, new SalvationCastState(ally));
                    if (!ally.IsAlive) revivedTargets.Add(ally);
                }
            CaptureChains();
            bool accepted = Session.TryAct(actor, ability, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted)
            {
                var partyTargets = new System.Collections.Generic.List<CombatantState>();
                if (ability.PartyEffect != PartyEffect.None)
                    foreach (CombatantState ally in Session.Combatants)
                        if (ally.Team == actor.Team) partyTargets.Add(ally);
                PresentingPartyTargets = partyTargets;
                PresentingRevivedTargets = revivedTargets;
                PresentAction(actor, ability, effectTargets);
                AwardIfComplete();
            }
            return accepted;
        }

        public bool TryPlayerItem(ConsumableDefinition item, CombatantState target)
        {
            if (IsCheatMenuBlocking) return false;
            if (!IsActive || IsPresentingAction || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            CombatantState actor = Session.CurrentActor;
            CaptureChains();
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

        private readonly System.Collections.Generic.Dictionary<CombatantState, bool> chainsBeforeAction
            = new System.Collections.Generic.Dictionary<CombatantState, bool>();
        private void CaptureChains()
        {
            chainsBeforeAction.Clear();
            foreach (CombatantState member in Session.Combatants) chainsBeforeAction[member] = member.IsBound;
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
        public bool ChainsVisible(CombatantState member) => IsPresentingAction && Time.unscaledTime < ActionImpactAt
            && chainsBeforeAction.TryGetValue(member, out bool before) ? before : member.IsBound;
        public float ChainBreakAge(CombatantState member) => IsPresentingAction && !member.IsBound
            && chainsBeforeAction.TryGetValue(member, out bool before) && before ? Time.unscaledTime - ActionImpactAt : -1;
        public bool IsChainBreakTarget(CombatantState member) => IsPresentingAction && !member.IsBound
            && chainsBeforeAction.TryGetValue(member, out bool before) && before;

        private void PresentAction(CombatantState actor, AbilityDefinition ability,
            System.Collections.Generic.IReadOnlyList<CombatantState> targets)
        {
            PresentingActor = actor; PresentingAbility = ability; PresentingTargets = targets;
            IsPresentingConfusedAction = ability == BattleSession.MindControlAttack;
            ActionStartedAt = Time.unscaledTime;
        }

        private void AwardIfComplete()
        {
            if (Session.Outcome == BattleOutcome.None) return;
            if (!notifiedCompletion) Party.CaptureBattleState(Session);
            if (Session.Outcome == BattleOutcome.Victory)
            {
                if (IsDragonEncounter) Dragon.TryComplete(Session,Party);
                if (!Rewards.HasClaimed(Session))
                {
                    Rewards.TryClaim(Session,out string message);
                    RewardMessage = message + (IsDragonEncounter && Dragon.IsDefeated ? " Armadura draconica recebida!" : "");
                }
            }
            else if (Session.Outcome == BattleOutcome.Completed && CastleRules?.Ending == GameEnding.KingFreed)
            {
                if (Rewards.TryClaimExperience(Session, out string message)) RewardMessage = message;
            }
            if (PostBattleResults.IsVictory(Session) && !PostBattle.IsComplete)
                PostBattle.Complete(Party, Rewards.HasClaimed(Session),
                    IsDragonEncounter && Dragon.IsDefeated ? "Armadura draconica recebida!" : "");
            if (notifiedCompletion) return;
            notifiedCompletion = true; EncounterCompleted?.Invoke(Session.Outcome);
        }

        private void ResolveMeteorImpact()
        {
            if (!IsActive || !pendingMeteorImpact || Time.unscaledTime < ActionImpactAt) return;
            pendingMeteorImpact = false;
            if (Session.TryAct(PresentingActor, PresentingAbility, PresentingActor, out string message))
            { ErrorMessage = string.Empty; AwardIfComplete(); }
            else ErrorMessage = message;
        }

        private void FinishCastleBattle()
        {
            IsActive = false; HasEnded = true; pendingEnemy = null;
            movement.SetMovementEnabled(false);
            showEnding?.Invoke(CastleRules.Ending);
        }

        private void Update()
        {
            if (IsCheatMenuBlocking) { pendingEnemy = null; return; }
            ResolveMeteorImpact();
            if (IsDestructiveMeteor && CastleRules.Ending == GameEnding.CastleDestroyed
                && CastleMeteorPresentation.ShouldCutToCinematic(Time.unscaledTime - ActionStartedAt))
            { FinishCastleBattle(); return; }
            if (IsPresentingAction) return;
            // The final action finishes before the cinematic; no result confirmation interrupts it.
            if (IsActive && IsCastleEncounter && Session.Outcome == BattleOutcome.Completed)
            { FinishCastleBattle(); return; }
            if (IsActive && Session.CurrentActor?.IsBound == true)
            { Session.TryTakeBoundTurn(out _); AwardIfComplete(); return; }
            if (IsActive && Session.CurrentActor?.MindControlTurns > 0)
            {
                CaptureChains();
                CombatantState confused = Session.CurrentActor;
                if (Session.TryTakeMindControlTurn(System.Math.Min(UnityEngine.Random.value, .999999d), out var victim, out _))
                {
                    PresentAction(confused, BattleSession.MindControlAttack, new[] { victim });
                    ErrorMessage = string.Empty; AwardIfComplete();
                    return;
                }
            }
            if (IsActive && PostBattle?.IsComplete == true)
            {
                if (!IsPostBattleOpen) { IsPostBattleOpen = true; PostBattleStartedAt = Time.unscaledTime; }
                return;
            }
            if (!HasEnded && !IsActive && !otherMenuOpen() && !dialogue.IsOpen && GameInput.IsFocused)
            {
                if (IsMenuOpen && GameInput.CancelPressed) { if (TryCancelInventorySelection?.Invoke() != true) CloseMenu(); GameInput.ConsumeGamepadFrame(); }
                else if (GameInput.InventoryPressed)
                {
                    if (IsMenuOpen) CloseMenu();
                    else { IsMenuOpen = true; movement.SetMovementEnabled(false); }
                    GameInput.ConsumeGamepadFrame();
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
            CaptureChains();
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
            if (IsCheatMenuBlocking) return;
            if (!IsActive || IsPresentingAction || Session.Outcome == BattleOutcome.None) return;
            if (PostBattleResults.IsVictory(Session) && (!IsPostBattleOpen || !PostBattle.CanContinue(Time.unscaledTime - PostBattleStartedAt))) return;
            IsPostBattleOpen = false;
            if(IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed)
            {
                FinishCastleBattle(); return;
            }
            AwardIfComplete();
            IsActive = false;
            randomEncounters.Reset(currentLocation());
            trackingWalk = false;
            movement.SetMovementEnabled(true);
            pendingEnemy = null;
            if (Session.Outcome == BattleOutcome.Defeat)
            {
                Party.RecoverAtInn();
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
