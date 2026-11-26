using DragonQuest.Content;

// Apresenta ao jogador as opções de salvar e de descansar na estalagem.
namespace DragonQuest.Interactions
{
    public sealed class InnInteraction : Interactable
    {
        // Mantém as ações entregues pelo bootstrap para as duas escolhas da estalagem.
        private System.Action save, spendNight;
        public void Initialize(System.Action manualSave, System.Action rest)
        { save=manualSave; spendNight=rest; }

        public override void Interact(InteractionContext context)
        {
            context.Dialogue.Open(DialogueCatalog.Inn, selected: choice =>
                {
                    if (choice==0) save?.Invoke();
                    else if (choice==1) spendNight?.Invoke();
                });
        }
    }
}
