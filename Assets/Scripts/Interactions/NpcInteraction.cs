using DragonQuest.Content;
// Abre o diálogo configurado para um personagem do mundo e disponibiliza uma ação de conclusão.
namespace DragonQuest.Interactions
{
    public sealed class NpcInteraction : Interactable
    {
        // Guarda o conteúdo que será enviado ao controlador de diálogos.
        private DialogueDefinition definition;
        // Recebe o contexto da interação quando a ação de conclusão é acionada.
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
