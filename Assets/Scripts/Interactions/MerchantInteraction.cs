namespace DragonQuest.Interactions
{
    public sealed class MerchantInteraction : Interactable
    {
        private string shopId;
        public void Initialize(string id) => shopId = id;
        public override void Interact(InteractionContext context) => context.OpenShop?.Invoke(shopId);
    }
}
