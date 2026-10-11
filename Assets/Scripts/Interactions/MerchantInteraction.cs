namespace DragonQuest.Interactions
{
    public sealed class MerchantInteraction : Interactable
    {
        private string shopId;
        private string shopName;
        public override string Prompt => "Negociar / " + shopName;
        public void Initialize(string id, string name) { shopId = id; shopName = name; }
        public override void Interact(InteractionContext context) => context.OpenShop?.Invoke(shopId);
    }
}
