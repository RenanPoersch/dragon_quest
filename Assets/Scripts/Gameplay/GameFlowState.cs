using System;

namespace DragonQuest.Gameplay
{
    public enum GameInputOwner { None, Exploration, Dialogue, Inventory, Shop, Battle, Save, Cinematic, Cheats, Ending }

    // Queries the live components, so loading a journey never leaves a stale copy of their state.
    public sealed class GameFlowState
    {
        private readonly Func<bool> ended, battle, inventory, dialogue, shop, save, cheats, cinematic, focused;
        public GameFlowState(Func<bool> ended, Func<bool> battle, Func<bool> inventory, Func<bool> dialogue,
            Func<bool> shop, Func<bool> save, Func<bool> cheats, Func<bool> cinematic, Func<bool> focused)
        {
            this.ended = ended; this.battle = battle; this.inventory = inventory; this.dialogue = dialogue;
            this.shop = shop; this.save = save; this.cheats = cheats; this.cinematic = cinematic; this.focused = focused;
        }
        public bool ExternalMenuOpen => shop() || save() || cheats() || cinematic();
        // Dialogue is handled by PlayerInteraction itself; it must not hide behind this busy gate.
        public bool IsBusy => ended() || battle() || inventory() || ExternalMenuOpen;
        public bool BlocksTravel => IsBusy || dialogue();
        public bool BlocksMovement => BlocksTravel || !focused();
        public bool CanReceiveInput(GameInputOwner owner) => owner != GameInputOwner.None && Owner == owner;
        public bool CanInteract => CanReceiveInput(GameInputOwner.Exploration);
        public bool CanTravel => CanInteract;
        public bool CanOpenShop => CanInteract;
        public bool CanStartBattle => CanInteract;
        public bool CanOpenSaveMenu => CanInteract;
        public bool CanToggleCheats => focused() && !ended() && !save() && !cinematic();
        public bool CanReturnToTitle => CanReceiveInput(GameInputOwner.Inventory);
        public bool CanToggleInventory => AllowsWorldMenu;
        public bool CanSave => AllowsWorldMenu;
        private bool AllowsWorldMenu
        {
            get
            {
                GameInputOwner owner = Owner;
                return owner == GameInputOwner.Exploration || owner == GameInputOwner.Inventory;
            }
        }
        public GameInputOwner Owner
        {
            get
            {
                if (!focused()) return GameInputOwner.None;
                if (ended()) return GameInputOwner.Ending;
                if (cheats()) return GameInputOwner.Cheats;
                if (save()) return GameInputOwner.Save;
                if (cinematic()) return GameInputOwner.Cinematic;
                if (battle()) return GameInputOwner.Battle;
                if (shop()) return GameInputOwner.Shop;
                if (inventory()) return GameInputOwner.Inventory;
                if (dialogue()) return GameInputOwner.Dialogue;
                return GameInputOwner.Exploration;
            }
        }
    }
}
