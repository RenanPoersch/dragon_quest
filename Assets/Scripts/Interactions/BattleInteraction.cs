using DragonQuest.Gameplay.Battle;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Content;
using System;
using UnityEngine;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Input;

namespace DragonQuest.Interactions
{
    public sealed class BattleInteraction : Interactable
    {
        private PlayerMovement player;
        private DialogueController dialogue;
        private BattleController battle;
        private Func<ExplorationProgress> getProgress;
        private BattleSession encounter;
        private bool challenged;

        public void Initialize(PlayerMovement hero, DialogueController conversation,
            BattleController controller, Func<ExplorationProgress> progress)
        {
            if (battle != null) battle.EncounterCompleted -= CompleteEncounter;
            player = hero; dialogue = conversation; battle = controller; getProgress = progress;
            battle.EncounterCompleted += CompleteEncounter;
            RestoreState();
        }

        public void RestoreState()
        {
            challenged = false; encounter = null;
            gameObject.SetActive(getProgress?.Invoke()?.CastleGuardsDefeated != true);
        }

        private void LateUpdate()
        {
            if (player == null || battle == null || getProgress().CastleGuardsDefeated) return;
            if (challenged)
            {
                // Closing the warning with Cancel also commits the encounter.
                if (dialogue.IsOpen || battle.IsBusy) return;
                battle.StartBattle();
                if (battle.IsActive) { encounter = battle.Session; challenged = false; }
                return;
            }
            if (battle.IsBusy || dialogue.IsOpen || !GameInput.IsFocused) return;
            Vector2 position = player.transform.position;
            if (Mathf.Abs(position.x - CityLayout.Bridge.x) <= 2.35f
                && position.y >= CityLayout.Bridge.y - 1f
                && position.y <= CityLayout.CastleDoor.y + .5f)
                Challenge();
        }

        public override void Interact(InteractionContext context)
        {
            if (battle == null) { context.StartBattle?.Invoke(); return; }
            if (!challenged && !getProgress().CastleGuardsDefeated && !battle.IsBusy && !dialogue.IsOpen)
                Challenge();
        }

        private void Challenge()
        {
            challenged = true;
            player.SetMovementEnabled(false);
            dialogue.Open(DialogueCatalog.BridgeGuards);
        }

        private void CompleteEncounter(BattleOutcome outcome)
        {
            if (encounter == null || battle.Session != encounter) return;
            encounter = null;
            if (outcome != BattleOutcome.Victory) return;
            getProgress().DefeatCastleGuards();
            gameObject.SetActive(false);
        }

        private void OnDisable() { challenged = false; encounter = null; }
        private void OnDestroy()
        {
            if (battle != null) battle.EncounterCompleted -= CompleteEncounter;
        }
    }
}
