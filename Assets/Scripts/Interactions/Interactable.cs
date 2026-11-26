using UnityEngine;

// Oferece a base comum para os componentes de cena que o jogador pode acionar.
namespace DragonQuest.Interactions
{
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        // Usa a atividade do componente como regra padrão e deixa classes derivadas especializá-la.
        public virtual bool CanInteract => isActiveAndEnabled;
        public abstract void Interact(InteractionContext context);
    }
}
