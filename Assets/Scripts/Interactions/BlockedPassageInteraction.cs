namespace DragonQuest.Interactions
{
    public sealed class BlockedPassageInteraction : Interactable
    {
        public override string Prompt => "Examinar passagem entre as rochas";
        public override void Interact(InteractionContext context)
            => context.Dialogue.Open("Passagem oculta", "Uma trilha estreita segue por entre as montanhas ate o vulcao.",
                "As rochas estao marcadas com tres selos. A passagem permanece bloqueada.");
    }
}
