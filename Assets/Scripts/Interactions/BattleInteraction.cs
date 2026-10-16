namespace DragonQuest.Interactions
{
    public sealed class BattleInteraction : Interactable
    {
        public override void Interact(InteractionContext context) => context.StartBattle?.Invoke();
    }
}
