using System;
using DragonQuest.Shops;

namespace DragonQuest.Progression
{
    public enum DeliveryStage { Available, CarryingPackage, Delivered, Completed }

    // Missao pequena com passos explicitos; pacote e um objeto de historia, fora do catalogo comercial.
    public sealed class DeliveryQuest
    {
        public const string MilestoneId = "merchant-delivery";
        public const int GoldReward = 30;
        private readonly ShopService shops;
        public DeliveryStage Stage { get; private set; }
        public event Action Changed;
        internal void Restore(DeliveryStage stage) => Stage = stage;
        public string Summary => Stage == DeliveryStage.Available ? "Entrega: fale com o vendedor de itens."
            : Stage == DeliveryStage.CarryingPackage ? "Entrega: leve o pacote ao vendedor de materias."
            : Stage == DeliveryStage.Delivered ? "Entrega: volte ao vendedor de itens para receber."
            : "Entrega concluida. Consumiveis repostos.";

        public DeliveryQuest(ShopService service)
        {
            shops = service ?? throw new ArgumentNullException(nameof(service));
            if (shops.FindShop("item-shop")?.Category != ShopCategory.Items || shops.FindShop("materia-shop")?.Category != ShopCategory.Materia)
                throw new ArgumentException("Entrega precisa das lojas de itens e materias.");
            if (shops.HasRestockedMilestone(MilestoneId)) Stage = DeliveryStage.Completed;
        }
        public string ActionLabel(string shopId)
        {
            if (Stage == DeliveryStage.Available && shopId == "item-shop") return "Aceitar entrega";
            if (Stage == DeliveryStage.CarryingPackage && shopId == "materia-shop") return "Entregar pacote";
            if (Stage == DeliveryStage.Delivered && shopId == "item-shop") return "Receber 30 moedas";
            return null;
        }
        public string ContextText(string shopId) => Stage == DeliveryStage.Available && shopId == "item-shop"
            ? "Leve um pacote a loja de materias e volte. Recompensa: 30 moedas e reposicao de consumiveis." : Summary;

        public bool TryInteract(ShopInventory shop, out string message)
        {
            if (shops.HasRestockedMilestone(MilestoneId)) Stage = DeliveryStage.Completed;
            if (shop == null || shops.FindShop(shop.Id) != shop || ActionLabel(shop.Id) == null)
            { message = "Este vendedor nao tem um passo de entrega disponivel agora."; return false; }
            if (Stage == DeliveryStage.Available)
            {
                Stage = DeliveryStage.CarryingPackage;
                message = "Pacote recebido. Entregue ao vendedor de materias, na loja a nordeste.";
            }
            else if (Stage == DeliveryStage.CarryingPackage)
            {
                Stage = DeliveryStage.Delivered;
                message = "Pacote entregue! Volte ao vendedor de itens para receber 30 moedas.";
            }
            else
            {
                if (!shops.Progress.TryAddGold(GoldReward)) { message = "Saldo no limite; a recompensa continua disponivel."; return false; }
                Stage = DeliveryStage.Completed;
                shops.TryRestockConsumables(MilestoneId);
                message = "Entrega concluida: +30 moedas. Estoque de consumiveis reposto; materias e equipamentos conservam seu estoque.";
            }
            Changed?.Invoke(); return true;
        }
    }
}
