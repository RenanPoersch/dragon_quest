namespace DragonQuest.Interactions
{
    public sealed class NpcInteraction : Interactable
    {
        private string npcName;
        private string[] messages;

        public override string Prompt => "Conversar com " + npcName;

        public void Initialize(string name, params string[] dialogue)
        {
            npcName = name;
            messages = dialogue;
        }

        public override void Interact(InteractionContext context) => context.Dialogue.Open(npcName, messages);
    }
}
