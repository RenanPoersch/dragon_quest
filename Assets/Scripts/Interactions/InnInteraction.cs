namespace DragonQuest.Interactions
{
    public sealed class InnInteraction : Interactable
    {
        private System.Action save;
        public void Initialize(System.Action manualSave) => save = manualSave;
        public override void Interact(InteractionContext context)
        {
            context.Dialogue.OpenChoices("Hospedeira", "Aqui voces estao em seguranca. Posso registrar sua jornada neste slot.",
                new[] {"Salvar jornada", "Pedir conselho", "Voltar"}, choice =>
                {
                    if (choice == 0) save?.Invoke();
                    else if (choice == 1) context.Dialogue.Open("Hospedeira",
                        "As lojas vendem Gelo e suprimentos. Protejam-se quando o dragao preparar o sopro.",
                        "Antes de enfrentar o trono, reorganizem os equipamentos e salvem sua jornada. O Limit permanece entre as batalhas.");
                });
        }
    }
}
