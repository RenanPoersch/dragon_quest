using UnityEngine;

namespace DragonQuest.Interactions
{
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        public abstract void Interact(InteractionContext context);
    }
}
