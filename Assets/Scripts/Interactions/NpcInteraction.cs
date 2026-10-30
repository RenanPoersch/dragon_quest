using DragonQuest.Content;
namespace DragonQuest.Interactions
{
    public sealed class NpcInteraction : Interactable
    {
        private string npcName;
        private DialogueDefinition definition;
        private string[] messages;
        private System.Action<InteractionContext> completed;


        public void Initialize(DialogueDefinition definition, System.Action<InteractionContext> onCompleted = null)
        {
            Initialize(definition.Title, definition.CreatePages());
            completed = onCompleted; this.definition = definition;
        }

        public void Initialize(string name, params string[] dialogue)
        {
            npcName = name;
            messages = dialogue;
            completed = null; definition = null;
        }

        public void InitializeWithCompletion(string name, System.Action<InteractionContext> onCompleted, params string[] dialogue)
        {
            Initialize(name, dialogue);
            completed = onCompleted;
        }

        public override void Interact(InteractionContext context)
        {
            System.Action callback = completed == null ? null : (System.Action)(() => completed(context));
            if (definition != null) context.Dialogue.Open(definition, callback);
            else context.Dialogue.OpenWithCompletion(npcName, callback, messages);
        }
    }
}
