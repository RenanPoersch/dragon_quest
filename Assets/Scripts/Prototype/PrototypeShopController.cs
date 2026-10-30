using DragonQuest.Content;
using UnityEngine;
using DragonQuest.Audio;
using UnityEngine.InputSystem;
using DragonQuest.Input;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using DragonQuest.Shops;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeShopController : MonoBehaviour
    {
        private PlayerMovement movement;
        private DialogueController dialogue;
        private PrototypeBattleController battle;
        public ShopService Service { get; private set; }
        public ShopInventory ActiveShop { get; private set; }
        public ShopMenuSession Menu { get; } = new ShopMenuSession();
        public int ActionIndex { get; private set; }
        public bool IsOpen => ActiveShop != null;
        public bool IsInputBlocked => battle == null || battle.IsCheatMenuBlocking;
        public string Message => Menu.Message;
        public void Initialize(PlayerMovement hero, DialogueController dialogueController, PrototypeBattleController battleController,
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
            movement.SetMovementEnabled(false); GameInput.ConsumeGamepadFrame();
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
            GameInput.ConsumeGamepadFrame();
        }
        public void ChooseAction(int index)
        {
            if (!IsOpen || IsInputBlocked) return;
            MenuSounds.Play(index == 2 ? MenuSound.Cancel : MenuSound.Confirm);
            ActionIndex = index;
            if (index == 0 || index == 1) Menu.Browse(index == 1);
            else Close();
            GameInput.ConsumeGamepadFrame();
        }
        private void Update()
        {
            if (!IsOpen || IsInputBlocked || !GameInput.IsFrameAvailable) return;
            if (GameInput.CancelPressed) { Back(); return; }
            var before = (ActionIndex, Menu.Index, Menu.Filter, Menu.Quantity);
            var keys = Keyboard.current;
            MenuDirection direction = GameInput.Navigation;
            int horizontal = direction == MenuDirection.Left || keys?.leftArrowKey.wasPressedThisFrame == true || keys?.aKey.wasPressedThisFrame == true ? -1
                : direction == MenuDirection.Right || keys?.rightArrowKey.wasPressedThisFrame == true || keys?.dKey.wasPressedThisFrame == true ? 1 : 0;
            int vertical = direction == MenuDirection.Up || keys?.upArrowKey.wasPressedThisFrame == true || keys?.wKey.wasPressedThisFrame == true ? -1
                : direction == MenuDirection.Down || keys?.downArrowKey.wasPressedThisFrame == true || keys?.sKey.wasPressedThisFrame == true ? 1 : 0;
            if (Menu.Phase == ShopMenuPhase.Actions)
            {
                const int count = 3;
                int delta = horizontal != 0 ? horizontal : vertical;
                ActionIndex = (ActionIndex + delta + count) % count;
                if (GameInput.ConfirmPressed) ChooseAction(ActionIndex);
            }
            else if (Menu.Phase == ShopMenuPhase.Browse)
            {
                if (vertical != 0) Menu.Move(vertical);
                if (horizontal != 0) Menu.ChangeFilter(horizontal);
                if (GameInput.ConfirmPressed) Menu.BeginQuantity();
            }
            else
            {
                if (horizontal != 0) Menu.AdjustQuantity(horizontal);
                if (vertical != 0) Menu.AdjustQuantity(-vertical * 10);
                if (keys?.homeKey.wasPressedThisFrame == true) Menu.AdjustQuantity(int.MinValue);
                if (keys?.endKey.wasPressedThisFrame == true) Menu.SetMaximum();
                if (GameInput.ConfirmPressed) Menu.Confirm();
            }
            MenuSounds.Navigate(before, (ActionIndex, Menu.Index, Menu.Filter, Menu.Quantity));
            if (GameInput.ConfirmPressed) MenuSounds.Play(MenuSound.Confirm);
            if (horizontal != 0 || vertical != 0 || GameInput.ConfirmPressed) GameInput.ConsumeGamepadFrame();
        }
    }
}
