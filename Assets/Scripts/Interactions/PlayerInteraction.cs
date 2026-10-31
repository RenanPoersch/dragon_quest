using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Input;
using DragonQuest.Exploration;

namespace DragonQuest.Interactions
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float interactionRange = 1.6f;
        private PlayerMovement movement;
        private Interactable[] candidates;
        private InteractionContext context;

        public void Initialize(InteractionContext interactionContext, Interactable[] targets)
        {
            context = interactionContext;
            candidates = targets;
            movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (context == null) return;
            if (context.IsBusy())
            {
                movement.SetMovementEnabled(false);
                return;
            }
            // A mesma tecla que fecha uma conversa não abre outra neste frame.
            if (context.Dialogue.IsOpen)
            {
                movement.SetMovementEnabled(false);
                MenuInput input = GameInput.ReadMenu();
                if (input.Cancel) context.Dialogue.Close();
                else if (input.Available && context.Dialogue.ChoicesReady)
                {
                    // Every dialogue selector uses the same previous/next controls, regardless of its options.
                    if (input.Step != 0) context.Dialogue.MoveChoice(input.Step);
                    if (GameInput.KeyPressed(Key.Digit1) || GameInput.KeyPressed(Key.Numpad1)) context.Dialogue.Choose(0);
                    else if (GameInput.KeyPressed(Key.Digit2) || GameInput.KeyPressed(Key.Numpad2)) context.Dialogue.Choose(1);
                    else if (GameInput.KeyPressed(Key.Digit3) || GameInput.KeyPressed(Key.Numpad3)) context.Dialogue.Choose(2);
                    else if (input.Confirm)
                        context.Dialogue.ConfirmChoice();
                }
                else if (input.Confirm)
                    context.Dialogue.Continue();
                GameInput.ConsumeFrame();
                movement.SetMovementEnabled(!context.Dialogue.IsOpen && !context.IsBusy());
                return;
            }

            movement.SetMovementEnabled(true);
            if (GameInput.InteractPressed)
            {
                FindNearest()?.Interact(context);
                GameInput.ConsumeFrame();
                movement.SetMovementEnabled(!context.Dialogue.IsOpen && !context.IsBusy());
            }
        }

        private Interactable FindNearest()
        {
            Interactable nearest = null;
            float bestDistance = interactionRange * interactionRange;
            foreach (Interactable candidate in candidates)
            {
                if (candidate == null || !candidate.CanInteract) continue;
                float distance = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                nearest = candidate;
            }
            return nearest;
        }

        private void OnDisable()
        {
            if (movement != null) movement.SetMovementEnabled(true);
        }
    }
}
