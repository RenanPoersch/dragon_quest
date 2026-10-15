using UnityEngine;
using UnityEngine.InputSystem;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Shops;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeShopController : MonoBehaviour
    {
        private PlayerMovement movement;
        private DialogueController dialogue;
        private PrototypeBattleController battle;
        public ShopService Service { get; private set; }
        public ShopInventory ActiveShop { get; private set; }
        public bool IsOpen => ActiveShop != null;
        public string Message { get; private set; }
        public DeliveryQuest Quest { get; private set; }
        public void Initialize(PlayerMovement hero, DialogueController dialogueController, PrototypeBattleController battleController,
            ExplorationProgress progress, ShopService restoredService = null, DeliveryQuest restoredQuest = null)
        {
            movement = hero; dialogue = dialogueController; battle = battleController;
            Service = restoredService ?? PrototypeShopFactory.Create(battle.Party, progress);
            Quest = restoredQuest ?? new DeliveryQuest(Service);
        }
        public void Open(string id)
        {
            if (battle.IsBusy || dialogue.IsOpen) return;
            ShopInventory shop = Service.FindShop(id);
            if (shop == null) return;
            ActiveShop = shop;
            Message = "Escolha Comprar ou Vender. Produtos comprados vao ao inventario.";
            movement.SetMovementEnabled(false);
        }
        public void Close()
        {
            ActiveShop = null;
            movement.SetMovementEnabled(!battle.IsBusy && !dialogue.IsOpen);
        }
        public bool Buy(ShopProduct product, int quantity)
        {
            if (!IsOpen) return false;
            bool accepted = Service.TryBuy(ActiveShop, product, quantity, out string message);
            Message = message; return accepted;
        }
        public bool Sell(SaleEntry entry, int quantity)
        {
            if (!IsOpen) return false;
            bool accepted = Service.TrySell(ActiveShop, entry, quantity, out string message);
            Message = message; return accepted;
        }
        public void InteractQuest()
        {
            if (!IsOpen) return;
            Quest.TryInteract(ActiveShop, out string message);
            Message = message;
        }
        private void Update()
        {
            if (battle == null || battle.IsCheatMenuBlocking) return;
            if (IsOpen && Application.isFocused && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }
    }
}
