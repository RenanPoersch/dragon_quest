using UnityEngine;
using UnityEngine.InputSystem;
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
            bool hasInput = keyboard != null && Application.isFocused;

            // A mesma tecla que fecha uma conversa não abre outra neste frame.
            if (context.Dialogue.IsOpen)
            {
                movement.SetMovementEnabled(false);
                if (hasInput && keyboard.escapeKey.wasPressedThisFrame) context.Dialogue.Close();
                else if (hasInput && context.Dialogue.HasChoices)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) context.Dialogue.Choose(0);
                    else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) context.Dialogue.Choose(1);
                    else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) context.Dialogue.Choose(2);
                }
                else if (hasInput && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                    context.Dialogue.Advance();
                movement.SetMovementEnabled(!context.Dialogue.IsOpen && !context.IsBusy());
                return;
            }

            movement.SetMovementEnabled(true);
            if(hasInput && context.Secrets != null && context.Secrets.KnowsPoem && keyboard.pKey.wasPressedThisFrame)
            {
                context.Dialogue.Open("Poema da lenda",LegendPoem.ExplainedPages());
                movement.SetMovementEnabled(false);return;
            }
            if (hasInput && keyboard.eKey.wasPressedThisFrame)
            {
                FindNearest()?.Interact(context);
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
