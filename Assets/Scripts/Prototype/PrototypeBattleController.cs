using UnityEngine;
using DragonQuest.Combat;
using DragonQuest.Exploration;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeBattleController : MonoBehaviour
    {
        private PlayerMovement movement;
        private CombatantState pendingEnemy;
        private float enemyActionTime;

        public bool IsActive { get; private set; }
        public BattleSession Session { get; private set; }
        public string ErrorMessage { get; private set; }

        public void Initialize(PlayerMovement hero) => movement = hero;

        public void StartBattle()
        {
            if (IsActive) return;
            Session = PrototypeBattleFactory.CreateEncounter();
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

        private void Update()
        {
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
