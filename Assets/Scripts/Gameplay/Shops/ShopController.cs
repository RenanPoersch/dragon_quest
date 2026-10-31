using DragonQuest.Gameplay.Battle;
using DragonQuest.Gameplay;
using DragonQuest.Content;
using UnityEngine;
using DragonQuest.Audio;
using UnityEngine.InputSystem;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Shops;

namespace DragonQuest.Gameplay.Shops
{
    public sealed class ShopController : MonoBehaviour
    {
        private PlayerMovement movement;
        private DialogueController dialogue;
        private BattleController battle;
        public ShopService Service { get; private set; }
        public ShopInventory ActiveShop { get; private set; }
        public ShopMenuSession Menu { get; } = new ShopMenuSession();
        public int ActionIndex { get; private set; }
        public bool IsOpen => ActiveShop != null;
        public bool IsInputBlocked => battle == null || battle.IsCheatMenuBlocking
            || battle.Flow != null && battle.Flow.Owner != GameInputOwner.Shop;
        public string Message => Menu.Message;
        public void Initialize(PlayerMovement hero, DialogueController dialogueController, BattleController battleController,
            ExplorationProgress progress, ShopService restoredService = null)
        {
            movement = hero; dialogue = dialogueController; battle = battleController;
            Service = restoredService ?? ShopCatalog.Create(battle.Party, progress);
        }
        public void Open(string id)
        {
            if (battle.IsBusy || dialogue.IsOpen) return;
            ShopInventory shop = Service.FindShop(id);
            if (shop == null) return;
            MenuSounds.Play(MenuSound.Confirm);
            ActiveShop = shop; Menu.Open(Service, shop); ActionIndex = 0;
            movement.SetMovementEnabled(false); GameInput.ConsumeFrame();
        }
        public void Close()
        {
            ActiveShop = null;
            movement.SetMovementEnabled(!battle.IsBusy && !dialogue.IsOpen);
        }
        public void Back()
        {
            MenuSounds.Play(MenuSound.Cancel);
            if (Menu.Back()) Close();
            ActionIndex = Menu.Selling ? 1 : 0;
            GameInput.ConsumeFrame();
        }
        public void ChooseAction(int index)
        {
            if (!IsOpen || IsInputBlocked) return;
            MenuSounds.Play(index == 2 ? MenuSound.Cancel : MenuSound.Confirm);
            ActionIndex = index;
            if (index == 0 || index == 1) Menu.Browse(index == 1);
            else Close();
            GameInput.ConsumeFrame();
        }
        private void Update()
        {
            if (!IsOpen || IsInputBlocked) return;
            MenuInput input = GameInput.ReadMenu();
            if (!input.Available) return;
            if (input.Cancel) { Back(); return; }
            var before = (ActionIndex, Menu.Index, Menu.Filter, Menu.Quantity);
            int horizontal = input.Horizontal, vertical = input.Vertical;
            bool shortcutHandled = false;
            if (Menu.Phase == ShopMenuPhase.Actions)
            {
                const int count = 3;
                int delta = horizontal != 0 ? horizontal : vertical;
                ActionIndex = (ActionIndex + delta + count) % count;
                if (input.Confirm) ChooseAction(ActionIndex);
            }
            else if (Menu.Phase == ShopMenuPhase.Browse)
            {
                if (vertical != 0) Menu.Move(vertical);
                if (horizontal != 0) Menu.ChangeFilter(horizontal);
                if (input.Confirm) Menu.BeginQuantity();
            }
            else
            {
                if (horizontal != 0) Menu.AdjustQuantity(horizontal);
                if (vertical != 0) Menu.AdjustQuantity(-vertical * 10);
                if (GameInput.KeyPressed(Key.Home)) { Menu.AdjustQuantity(int.MinValue); shortcutHandled = true; }
                if (GameInput.KeyPressed(Key.End)) { Menu.SetMaximum(); shortcutHandled = true; }
                if (input.Confirm) Menu.Confirm();
            }
            MenuSounds.Navigate(before, (ActionIndex, Menu.Index, Menu.Filter, Menu.Quantity));
            if (input.Confirm && GameInput.IsFrameAvailable) MenuSounds.Play(MenuSound.Confirm);
            if (horizontal != 0 || vertical != 0 || input.Confirm || shortcutHandled) GameInput.ConsumeFrame();
        }
    }
}
