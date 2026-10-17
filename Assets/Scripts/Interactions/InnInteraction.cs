namespace DragonQuest.Interactions
{
    public sealed class InnInteraction : Interactable
    {
        private System.Action save, spendNight;
        public void Initialize(System.Action manualSave, System.Action rest)
        { save=manualSave; spendNight=rest; }

        public override void Interact(InteractionContext context)
        {
            context.Dialogue.OpenChoices("Hospedeira", "Bem-vindos à pousada. O que desejam?",
                new[] { "Salvar", "Passar a noite" }, choice =>
                {
                    if (choice==0) save?.Invoke();
                    else if (choice==1) spendNight?.Invoke();
                });
        }
    }
}
