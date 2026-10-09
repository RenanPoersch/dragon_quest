namespace DragonQuest.Interactions
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(InteractionContext context);
    }
}
