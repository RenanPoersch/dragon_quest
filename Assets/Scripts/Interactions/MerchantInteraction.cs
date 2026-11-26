// Abre a loja vinculada ao comerciante com quem o jogador interage.
namespace DragonQuest.Interactions
{
    public sealed class MerchantInteraction : Interactable
    {
        // Identifica o estoque solicitado ao contexto de interação.
        private string shopId;
        public void Initialize(string id) => shopId = id;
        public override void Interact(InteractionContext context) => context.OpenShop?.Invoke(shopId);
    }
}
