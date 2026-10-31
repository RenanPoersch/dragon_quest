using DragonQuest.Content;
namespace DragonQuest.Interactions
{
    public sealed class NpcInteraction : Interactable
    {
        private DialogueDefinition definition;
        private System.Action<InteractionContext> completed;


        public void Initialize(DialogueDefinition definition, System.Action<InteractionContext> onCompleted = null)
        {
            completed = onCompleted; this.definition = definition;
        }

        public override void Interact(InteractionContext context)
        {
            System.Action callback = completed == null ? null : (System.Action)(() => completed(context));
            context.Dialogue.Open(definition, callback);
        }
    }
}
