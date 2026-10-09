namespace DragonQuest.Interactions
{
    public sealed class BattleInteraction : Interactable
    {
        public override string Prompt => "Desafiar o guarda do tirano";
        public override void Interact(InteractionContext context) => context.StartBattle?.Invoke();
    }
}
