using UnityEngine;

namespace DragonQuest.Interactions
{
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        public virtual bool CanInteract => isActiveAndEnabled;
        public abstract void Interact(InteractionContext context);
    }
}
