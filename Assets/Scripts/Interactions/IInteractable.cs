// Define o contrato que o sistema de exploração usa para interagir com um objeto.
namespace DragonQuest.Interactions
{
    // Permite que diferentes alvos ofereçam suas próprias regras de interação.
    public interface IInteractable
    {
        void Interact(InteractionContext context);
    }
}
