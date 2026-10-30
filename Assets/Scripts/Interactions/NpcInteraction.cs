using DragonQuest.Content;
namespace DragonQuest.Interactions
{
    public sealed class NpcInteraction : Interactable
    {
        private string npcName;
        private string[] messages;
        private System.Action<InteractionContext> completed;


        public void Initialize(DialogueDefinition definition, System.Action<InteractionContext> onCompleted = null)
        {
            Initialize(definition.Title, definition.CreatePages());
            completed = onCompleted;
        }

        public void Initialize(string name, params string[] dialogue)
        {
            npcName = name;
            messages = dialogue;
            completed = null;
        }

        public void InitializeWithCompletion(string name, System.Action<InteractionContext> onCompleted, params string[] dialogue)
        {
            Initialize(name, dialogue);
            completed = onCompleted;
        }

        public override void Interact(InteractionContext context)
            => context.Dialogue.OpenWithCompletion(npcName, completed == null ? null : () => completed(context), messages);
    }
}
