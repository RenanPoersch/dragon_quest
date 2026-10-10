using UnityEngine;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Interactions;
using UnityEngine.InputSystem;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeBattleController : MonoBehaviour
    {
        private PlayerMovement movement;
        private CombatantState pendingEnemy;
        private float enemyActionTime;
        private DialogueController dialogue;

        public bool IsActive { get; private set; }
        public bool IsMenuOpen { get; private set; }
        public bool IsBusy => IsActive || IsMenuOpen;
        public PartyProgress Party { get; private set; }
        public BattleSession Session { get; private set; }
        public string ErrorMessage { get; private set; }

        public void Initialize(PlayerMovement hero, DialogueController dialogueController)
        {
            movement = hero; dialogue = dialogueController;
            Party = PrototypePartyFactory.Create();
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
            movement.SetMovementEnabled(false);
        }

        public bool TryPlayerAction(AbilityDefinition ability, CombatantState target)
        {
            if (!IsActive || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            bool accepted = Session.TryAct(Session.CurrentActor, ability, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            return accepted;
        }

        public bool TryPlayerItem(ConsumableDefinition item, CombatantState target)
        {
            if (!IsActive || Session.CurrentActor == null || Session.CurrentActor.Team != CombatTeam.Party) return false;
            bool accepted = Session.TryUseItem(Session.CurrentActor, item, target, out string message);
            ErrorMessage = accepted ? string.Empty : message;
            return accepted;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (!IsActive && !dialogue.IsOpen && keyboard != null)
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
            pendingEnemy = null;
        }

        public void ReturnToCity()
        {
            if (!IsActive || Session.Outcome == BattleOutcome.None) return;
            IsActive = false;
            movement.SetMovementEnabled(true);
            pendingEnemy = null;
        }
    }
}
