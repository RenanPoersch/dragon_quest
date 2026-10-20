using System;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Prototype;

internal static class ItemMenuChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static ItemMenuSession Menu(PartyProgress party, ConsumableDefinition item)
    {
        var menu = new ItemMenuSession(); menu.Reset(party);
        for (int i = 0; i < menu.Items.Count; i++) if (menu.Items[i].Definition == item) menu.SelectItem(i);
        return menu;
    }
    private static PartyProgress Party()
    {
        var party = PrototypePartyFactory.CreateTestParty();
        party.Members[0].RestoreVitals(20, 1, true, 0);
        party.Members[1].RestoreVitals(30, 2, false, 0);
        party.Members[2].RestoreVitals(0, 0, false, 0);
        return party;
    }
    private static void CheckSelectionAndCancellation()
    {
        var party = Party(); var menu = Menu(party, PrototypePartyFactory.Potion);
        int stock = party.Consumables.QuantityOf(menu.SelectedItem), hp = party.Members[0].Hp;
        Check(menu.BeginUse(party) && menu.ChoosingTarget, "Selecting an item moves the cursor to the portraits.");
        Check(party.Consumables.QuantityOf(menu.SelectedItem) == stock && party.Members[0].Hp == hp,
            "Item selection cannot consume stock or heal before target confirmation.");
        menu.Move(-1, party); Check(menu.TargetIndex == 2, "Portrait navigation wraps backwards.");
        menu.Move(1, party); Check(menu.TargetIndex == 0, "Portrait navigation wraps forwards.");
        menu.SelectTarget(1, party); menu.CancelTarget();
        Check(!menu.ChoosingTarget && party.Consumables.QuantityOf(menu.SelectedItem) == stock && party.Members[1].Hp == 30,
            "Cancelling target selection leaves the stock and all allies unchanged.");
        Check(!menu.ConfirmTarget(party), "Confirmation outside target mode cannot use an item.");
        menu.BeginUse(party); menu.SelectTarget(1, party);
        Check(menu.ConfirmTarget(party) && party.Members[1].Hp == 70 && party.Members[0].Hp == hp,
            "Clicking Lia's portrait heals Lia rather than the previously selected party member.");
        Check(party.Consumables.QuantityOf(menu.SelectedItem) == stock - 1, "One confirmed portrait consumes exactly one item.");
        Check(menu.ChoosingTarget, "An item with stock remaining can be used again from portrait selection.");
    }
    private static void CheckInvalidTargetsAndStock()
    {
        var party = Party(); var menu = Menu(party, PrototypePartyFactory.Potion);
        menu.BeginUse(party); menu.SelectTarget(2, party);
        int stock = party.Consumables.QuantityOf(menu.SelectedItem);
        Check(!menu.ConfirmTarget(party) && menu.ChoosingTarget && menu.Message.Length > 0
            && party.Consumables.QuantityOf(menu.SelectedItem) == stock, "Healing a KO ally fails without consuming the item or losing target focus.");
        party.Members[0].RestoreVitals(party.Members[0].MaxHp, 1, false, 0); menu.SelectTarget(0, party);
        Check(!menu.ConfirmTarget(party) && party.Consumables.QuantityOf(menu.SelectedItem) == stock,
            "A full-health ally does not consume a potion.");
        party.Consumables.TryRemove(menu.SelectedItem, stock - 1);
        menu.SelectTarget(1, party);
        Check(menu.ConfirmTarget(party) && !menu.ChoosingTarget && party.Consumables.QuantityOf(menu.SelectedItem) == 0,
            "Using the final copy returns the cursor to the item list.");
        Check(!menu.BeginUse(party) && !menu.ChoosingTarget, "An exhausted item cannot enter portrait selection.");
        Check(!menu.ConfirmTarget(party), "A second confirmation after exhaustion cannot consume another item.");
    }
    private static void CheckAllEffects()
    {
        var party = Party(); var menu = Menu(party, PrototypePartyFactory.Ether);
        menu.BeginUse(party); menu.SelectTarget(1, party);
        Check(menu.ConfirmTarget(party) && party.Members[1].Mp == 17, "Ether restores MP on the selected portrait.");
        menu = Menu(party, PrototypePartyFactory.Antidote); menu.BeginUse(party);
        Check(menu.ConfirmTarget(party) && !party.Members[0].IsPoisoned, "Antidote cleanses the selected ally.");
        menu = Menu(party, PrototypePartyFactory.PhoenixFeather); menu.BeginUse(party);
        int stock = party.Consumables.QuantityOf(menu.SelectedItem);
        Check(!menu.ConfirmTarget(party) && party.Consumables.QuantityOf(menu.SelectedItem) == stock,
            "A revival item cannot be spent on a living ally.");
        menu.SelectTarget(2, party);
        Check(menu.ConfirmTarget(party) && party.Members[2].Hp == 45, "A revival item revives the selected KO ally.");
    }
    private static void CheckCategoriesAndArrangement()
    {
        var party = Party(); var menu = Menu(party, PrototypePartyFactory.Potion);
        var selected = menu.SelectedItem;
        int stock = party.Consumables.QuantityOf(selected);
        menu.BeginUse(party); menu.ShowKeyItems(true);
        Check(menu.ShowingKeyItems && !menu.ChoosingTarget && !menu.BeginUse(party) && !menu.ConfirmTarget(party),
            "Key Items leaves target selection and cannot use consumables.");
        menu.Arrange(party);
        Check(!menu.ShowingKeyItems && menu.SelectedItem == selected && party.Consumables.QuantityOf(selected) == stock,
            "Arrange preserves the selected definition and shared stock.");
        for (int i = 1; i < menu.Items.Count; i++)
            Check(StringComparer.OrdinalIgnoreCase.Compare(menu.Items[i - 1].Definition.Name, menu.Items[i].Definition.Name) <= 0,
                "Arrange orders the display alphabetically.");
        menu.Reset(party); menu.Move(-1, party);
        Check(menu.ItemIndex == menu.Items.Count - 1, "Item navigation wraps at the start of the list.");
        menu.Reset(party); Check(menu.ItemIndex == 0 && !menu.ChoosingTarget && !menu.ShowingKeyItems && menu.Message.Length == 0,
            "Reopening Item resets the list and focus.");
        party = new PartyProgress(new[] { party.Members[0] }, new MateriaInstance[0], consumables: new ItemInventory());
        menu.Reset(party); menu.Move(1, party);
        Check(menu.Items.Count == 0 && menu.SelectedItem == null && !menu.BeginUse(party), "An empty inventory remains navigable and cannot use items.");
    }
    private static void CheckSubmenus()
    {
        var party = Party(); var menu = Menu(party, PrototypePartyFactory.Potion);
        var selected = menu.SelectedItem; int stock = party.Consumables.QuantityOf(selected);
        menu.MoveSubmenu(1, party);
        Check(menu.SubmenuIndex == 1 && menu.BrowsingSubmenus && menu.SelectedItem == selected,
            "Right switches from Use to Arrange and preserves the selected item.");
        for (int i = 1; i < menu.Items.Count; i++)
            Check(StringComparer.OrdinalIgnoreCase.Compare(menu.Items[i - 1].Definition.Name, menu.Items[i].Definition.Name) <= 0,
                "Switching to Arrange applies alphabetical ordering.");
        menu.MoveSubmenu(1, party);
        Check(menu.SubmenuIndex == 2 && menu.ShowingKeyItems && !menu.BeginUse(party),
            "Right switches Arrange to Key Items and changes the usable content.");
        menu.MoveSubmenu(1, party);
        Check(menu.SubmenuIndex == 0 && !menu.ShowingKeyItems, "Right wraps Key Items back to Use.");
        menu.MoveSubmenu(-1, party);
        Check(menu.ShowingKeyItems, "Left wraps Use to Key Items.");
        menu.ShowKeyItems(false); menu.BeginUse(party); menu.MoveSubmenu(-1, party);
        Check(menu.ShowingKeyItems && !menu.ChoosingTarget && party.Consumables.QuantityOf(selected) == stock,
            "Changing submenu cancels a pending target without consuming an item.");
        menu.Reset(party);
        Check(menu.SubmenuIndex == 0 && !menu.BrowsingSubmenus, "Reopening resets submenu focus.");
    }
    public static int Main()
    {
        try
        {
            CheckSelectionAndCancellation(); CheckInvalidTargetsAndStock(); CheckAllEffects(); CheckCategoriesAndArrangement(); CheckSubmenus();
            Console.WriteLine("ITEM_MENU_CHECKS_OK: " + checks + " checks passed."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
