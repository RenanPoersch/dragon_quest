using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Progression;

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
            Keyboard keyboard = Keyboard.current;
            bool hasInput = GameInput.IsFocused;

            // A mesma tecla que fecha uma conversa não abre outra neste frame.
            if (context.Dialogue.IsOpen)
            {
                movement.SetMovementEnabled(false);
                if (hasInput && GameInput.CancelPressed) context.Dialogue.Close();
                else if (hasInput && context.Dialogue.ChoicesReady)
                {
                    if (GameInput.Navigation == MenuDirection.Down || keyboard?.downArrowKey.wasPressedThisFrame == true) context.Dialogue.MoveChoice(1);
                    else if (GameInput.Navigation == MenuDirection.Up || keyboard?.upArrowKey.wasPressedThisFrame == true) context.Dialogue.MoveChoice(-1);
                    if (keyboard?.digit1Key.wasPressedThisFrame == true || keyboard?.numpad1Key.wasPressedThisFrame == true) context.Dialogue.Choose(0);
                    else if (keyboard?.digit2Key.wasPressedThisFrame == true || keyboard?.numpad2Key.wasPressedThisFrame == true) context.Dialogue.Choose(1);
                    else if (keyboard?.digit3Key.wasPressedThisFrame == true || keyboard?.numpad3Key.wasPressedThisFrame == true) context.Dialogue.Choose(2);
                    else if (GameInput.ConfirmPressed)
                        context.Dialogue.ConfirmChoice();
                }
                else if (hasInput && GameInput.ConfirmPressed)
                    context.Dialogue.Continue();
                GameInput.ConsumeGamepadFrame();
                movement.SetMovementEnabled(!context.Dialogue.IsOpen && !context.IsBusy());
                return;
            }

            movement.SetMovementEnabled(true);
            if(hasInput && context.Secrets != null && context.Secrets.KnowsPoem && GameInput.PoemPressed)
            {
                context.Dialogue.Open("Poema da lenda",LegendPoem.ExplainedPages());
                movement.SetMovementEnabled(false);return;
            }
            if (hasInput && GameInput.InteractPressed)
            {
                FindNearest()?.Interact(context);
                GameInput.ConsumeGamepadFrame();
                movement.SetMovementEnabled(!context.Dialogue.IsOpen && !context.IsBusy());
            }
        }

        private Interactable FindNearest()
        {
            Interactable nearest = null;
            float bestDistance = interactionRange * interactionRange;
            foreach (Interactable candidate in candidates)
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
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
