namespace DragonQuest.Interactions
{
    public sealed class DoorInteraction : Interactable
    {
        private string destination;
        private string prompt;

        public override string Prompt => prompt;

        public void Initialize(string locationId, string actionLabel)
        {
            destination = locationId;
            prompt = actionLabel;
        }

        public override void Interact(InteractionContext context) => context.TravelTo(destination);
    }
}
