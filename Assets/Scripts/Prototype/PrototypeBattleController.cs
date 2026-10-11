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

        public bool IsActive { get; private set; }
        public bool IsMenuOpen { get; private set; }
        public bool IsBusy => IsActive || IsMenuOpen || otherMenuOpen();
        public PartyProgress Party { get; private set; }
        public BattleSession Session { get; private set; }
        public string ErrorMessage { get; private set; }
        public BattleRewards Rewards { get; private set; }
        public string RewardMessage { get; private set; }

        public void Initialize(PlayerMovement hero, DialogueController dialogueController, ExplorationProgress progress,
            System.Func<bool> externalMenuOpen = null)
        {
            movement = hero; dialogue = dialogueController;
            otherMenuOpen = externalMenuOpen ?? (() => false);
            Party = PrototypePartyFactory.Create();
            Rewards = new BattleRewards(progress);
        }

        public void CloseMenu()
        {
            IsMenuOpen = false;
            movement.SetMovementEnabled(!IsActive && !dialogue.IsOpen);
        }

        public void StartBattle()
        {
            if (IsBusy) return;
            Session = PrototypeBattleFactory.CreateEncounter(Party);
            IsActive = true;
            pendingEnemy = null;
            ErrorMessage = string.Empty;
            RewardMessage = string.Empty;
            movement.SetMovementEnabled(false);
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
            if (Session.Outcome != BattleOutcome.Victory || Rewards.HasClaimed(Session)) return;
            Rewards.TryClaim(Session, out string message);
            RewardMessage = message;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (!IsActive && !otherMenuOpen() && !dialogue.IsOpen && keyboard != null && Application.isFocused)
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
            AwardIfComplete();
            IsActive = false;
            movement.SetMovementEnabled(true);
            pendingEnemy = null;
        }
    }
}
