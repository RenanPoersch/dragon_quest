using UnityEngine;
using UnityEngine.InputSystem;
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
        private Interactable current;

        public string CurrentPrompt => current != null ? "E - " + current.Prompt : string.Empty;

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
                current = null;
                movement.SetMovementEnabled(false);
                return;
            }
            Keyboard keyboard = Keyboard.current;
            bool hasInput = keyboard != null && Application.isFocused;

            // A mesma tecla que fecha uma conversa não abre outra neste frame.
            if (context.Dialogue.IsOpen)
            {
                current = null;
                movement.SetMovementEnabled(false);
                if (hasInput && keyboard.escapeKey.wasPressedThisFrame) context.Dialogue.Close();
                else if (hasInput && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                    context.Dialogue.Advance();
                movement.SetMovementEnabled(!context.Dialogue.IsOpen && !context.IsBusy());
                return;
            }

            movement.SetMovementEnabled(true);
            current = FindNearest();
            if (hasInput && current != null && keyboard.eKey.wasPressedThisFrame)
            {
                current.Interact(context);
                current = null;
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
            current = null;
            if (movement != null) movement.SetMovementEnabled(true);
        }
    }
}
