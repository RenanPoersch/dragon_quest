using DragonQuest.Progression;
using System;
using System.Collections.Generic;

namespace DragonQuest.Inventory
{
    // Selecting an item only moves the cursor; resources change when a target is confirmed.
    public sealed class ItemMenuSession
    {
        private readonly List<ItemStack> items = new List<ItemStack>();
        private bool arranged;
        public IReadOnlyList<ItemStack> Items { get; }
        public int ItemIndex { get; private set; }
        public int TargetIndex { get; private set; }
        public bool ChoosingTarget { get; private set; }
        public bool ShowingKeyItems { get; private set; }
        public int SubmenuIndex { get; private set; }
        public bool BrowsingSubmenus { get; private set; }
        public string Message { get; private set; } = "";
        public ConsumableDefinition SelectedItem => items.Count == 0 ? null : items[ItemIndex].Definition;

        public ItemMenuSession() { Items = items.AsReadOnly(); }

        public void Reset(PartyProgress party)
        {
            items.Clear(); ItemIndex = TargetIndex = 0;
            ChoosingTarget = ShowingKeyItems = arranged = false; Message = "";
            SubmenuIndex = 0; BrowsingSubmenus = false;
            Refresh(party);
        }

        public void Refresh(PartyProgress party)
        {
            var selected = SelectedItem;
            items.Clear();
            foreach (var stack in party.Consumables.Items) items.Add(stack);
            if (arranged) items.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Definition.Name, b.Definition.Name));
            int index = items.FindIndex(stack => stack.Definition == selected);
            ItemIndex = index >= 0 ? index : Math.Max(0, Math.Min(ItemIndex, items.Count - 1));
            TargetIndex = Math.Max(0, Math.Min(TargetIndex, party.Members.Count - 1));
            if (SelectedItem == null || party.Consumables.QuantityOf(SelectedItem) == 0) ChoosingTarget = false;
        }

        public void ShowKeyItems(bool show)
        { ShowingKeyItems = show; ChoosingTarget = BrowsingSubmenus = false; SubmenuIndex = show ? 2 : 0; Message = ""; }

        public void Arrange(PartyProgress party)
        { arranged = true; ShowKeyItems(false); SubmenuIndex = 1; Refresh(party); }

        public void MoveSubmenu(int step, PartyProgress party)
        {
            int next = (SubmenuIndex + step % 3 + 3) % 3;
            if (next == 1) Arrange(party); else ShowKeyItems(next == 2);
            BrowsingSubmenus = true;
        }

        public void FocusContent() { BrowsingSubmenus = false; }

        public void SelectItem(int index)
        {
            if (index < 0 || index >= items.Count) return;
            ItemIndex = index; ChoosingTarget = false; Message = "";
        }

        public bool BeginUse(PartyProgress party)
        {
            if (ShowingKeyItems || SelectedItem == null) return false;
            SubmenuIndex = 0; BrowsingSubmenus = false;
            if (party.Consumables.QuantityOf(SelectedItem) == 0)
            { Message = "Item esgotado."; return false; }
            ChoosingTarget = true; Message = ""; return true;
        }

        public void SelectTarget(int index, PartyProgress party)
        {
            if (!ChoosingTarget || index < 0 || index >= party.Members.Count) return;
            TargetIndex = index; Message = "";
        }

        public void Move(int step, PartyProgress party)
        {
            if (ShowingKeyItems) return;
            int count = ChoosingTarget ? party.Members.Count : items.Count;
            if (count == 0) return;
            int index = ((ChoosingTarget ? TargetIndex : ItemIndex) + step % count + count) % count;
            if (ChoosingTarget) SelectTarget(index, party); else SelectItem(index);
        }

        public bool ConfirmTarget(PartyProgress party)
        {
            if (!ChoosingTarget || ShowingKeyItems || SelectedItem == null) return false;
            bool used = party.TryUseItem(SelectedItem, party.Members[TargetIndex], out string result);
            Message = result;
            Refresh(party);
            return used;
        }

        public void CancelTarget() { ChoosingTarget = false; Message = ""; }
    }
}
