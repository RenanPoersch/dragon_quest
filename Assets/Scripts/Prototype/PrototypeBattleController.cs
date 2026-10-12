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
        public event System.Action<BattleOutcome> EncounterCompleted;
        public bool HasEnded {get;private set;}
        public CastleBattleRules CastleRules => Session?.Rules as CastleBattleRules;
        public bool IsCastleEncounter => CastleRules!=null;
        public DragonProgress Dragon { get; private set; }
        public bool IsDragonEncounter { get; private set; }
        public string BattleTitle => IsCastleEncounter ? (CastleRules.IsSpiritRevealed?"TIRANO E ESPIRITO":"TIRANO DOMINADO")
            :IsDragonEncounter ? "DRAGAO CORROMPIDO" : "PATRULHA DO TIRANO";
        public string ReturnLabel => IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed?"Ver final"
            :(IsDragonEncounter||IsCastleEncounter)&&Session.Outcome==BattleOutcome.Defeat?"Voltar para a hospedaria":"Continuar explorando";
        public string OutcomeDescription => Session.Outcome==BattleOutcome.Completed?EndingNarrative.Title(CastleRules.Ending):Session.Outcome == BattleOutcome.Victory
            ? (IsDragonEncounter ? "A essencia do dragao foi libertada. " : "A patrulha foi vencida. ") + RewardMessage
            : IsDragonEncounter||IsCastleEncounter ? "Sua equipe foi vencida. Acordara na hospedaria; Limit e progresso serao mantidos."
            : "Sua equipe foi vencida. Reorganize-se para tentar novamente.";

        public bool IsActive { get; private set; }
        public bool IsMenuOpen { get; private set; }
        public bool IsBusy => HasEnded || IsActive || IsMenuOpen || otherMenuOpen();
        public PartyProgress Party { get; private set; }
        public BattleSession Session { get; private set; }
        public string ErrorMessage { get; private set; }
        public BattleRewards Rewards { get; private set; }
        public string RewardMessage { get; private set; }

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

        public void StartDragonBattle()
        {
            if (IsBusy || dialogue.IsOpen || Dragon.IsDefeated || secrets?.PassageOpen != true) return;
            StartEncounter(PrototypeBattleFactory.CreateDragonEncounter(Party),true);
        }

        private void StartEncounter(BattleSession encounter, bool dragonEncounter)
        {
            Session = encounter; IsDragonEncounter = dragonEncounter;
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
            if (!IsActive || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            bool accepted = Session.TryAct(Session.CurrentActor, ability, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted) AwardIfComplete();
            return accepted;
        }

        public bool TryPlayerItem(ConsumableDefinition item, CombatantState target)
        {
            if (!IsActive || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            bool accepted = Session.TryUseItem(Session.CurrentActor, item, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted) AwardIfComplete();
            return accepted;
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
            bool accepted = EnemyAI.TryTakeTurn(Session, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            if (accepted) AwardIfComplete();
            pendingEnemy = null;
        }

        public void ReturnToCity()
        {
            if (!IsActive || Session.Outcome == BattleOutcome.None) return;
            if(IsCastleEncounter&&Session.Outcome==BattleOutcome.Completed)
            {
                IsActive=false;HasEnded=true;pendingEnemy=null;movement.SetMovementEnabled(false);
                showEnding?.Invoke(CastleRules.Ending);return;
            }
            AwardIfComplete();
            IsActive = false;
            movement.SetMovementEnabled(true);
            pendingEnemy = null;
            if ((IsDragonEncounter||IsCastleEncounter) && Session.Outcome == BattleOutcome.Defeat)
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
