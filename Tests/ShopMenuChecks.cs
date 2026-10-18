using System;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Prototype;
using DragonQuest.Shops;

internal static class ShopMenuChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static ShopMenuSession Open(string id, int gold = 1000)
    {
        var service = PrototypeShopFactory.Create(PrototypePartyFactory.Create(), new ExplorationProgress(gold));
        var menu = new ShopMenuSession(); menu.Open(service, service.FindShop(id)); return menu;
    }
    private static void BuyAndSellItems()
    {
        var menu = Open("item-shop", 35);
        Check(menu.Phase == ShopMenuPhase.Actions && menu.Selected.Name == "Pocao", "Opening previews the catalog and focuses the actions.");
        menu.Browse(false);
        Check(menu.Owned == 2 && menu.Equipped == 0 && menu.Maximum == 3, "Owned count and affordable quantity.");
        Check(menu.BeginQuantity() && menu.Quantity == 1, "Selecting a product opens the quantity stage.");
        menu.AdjustQuantity(100);
        Check(menu.Quantity == 3 && menu.Total == 30 && menu.RemainingGold == 5, "Quantity clamps to affordability and previews the exact balance.");
        Check(!menu.Back() && menu.Service.Progress.Gold == 35 && menu.Owned == 2, "Cancelling never mutates gold or items.");
        menu.BeginQuantity(); menu.SetMaximum();
        Check(menu.Confirm() && menu.Owned == 5 && menu.Service.Progress.Gold == 5 && menu.Selected.Available == 9,
            "Confirm changes gold, stock and ownership once.");
        Check(!menu.Confirm(), "A repeated confirmation cannot repeat the transaction.");
        Check(!menu.BeginQuantity() && menu.Message == "Ouro insuficiente.", "Cannot purchase with insufficient gold.");
        menu.Browse(true); Check(menu.Maximum == 5, "Sale maximum uses owned stack.");
        menu.BeginQuantity(); menu.SetMaximum();
        Check(menu.Total == 25 && menu.RemainingGold == 30 && menu.Confirm(), "Multiple-unit sale uses half the catalog price.");
        Check(menu.Service.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 0 && menu.Selected.Name == "Antidoto", "Selling the last stack safely selects a remaining row.");
        Check(menu.Service.FindShop("item-shop").Products[0].Stock == 9, "Selling never replenishes stock.");
        Check(!menu.Back() && menu.Phase == ShopMenuPhase.Actions && menu.Back(), "Cancel returns through stages to closing.");
    }
    private static void EquipmentAndPreviews()
    {
        var menu = Open("equipment-shop"); menu.Browse(false);
        Check(menu.Entries.Count == 2 && menu.Selected.Equipment.Slot == EquipmentSlot.Weapon, "Weapons filter excludes sale-only initial gear.");
        foreach (ShopEquipmentFilter filter in Enum.GetValues(typeof(ShopEquipmentFilter)))
        {
            menu.SelectFilter(filter);
            foreach (var member in menu.Service.Party.Members)
            {
                var weapon = member.Weapon; var armor = member.Armor; var accessory = member.Accessory;
                var preview = menu.Preview(member);
                var candidate = new EquipmentLoadout(menu.Selected.Equipment);
                var slot = candidate.Definition.Slot;
                var expected = new EquipmentMenuStats(member, slot == EquipmentSlot.Weapon ? candidate : weapon,
                    slot == EquipmentSlot.Armor ? candidate : armor, slot == EquipmentSlot.Accessory ? candidate : accessory);
                for (int i = 0; i < EquipmentMenuStats.Count; i++) Check(preview[i] == expected[i], "Shop preview respects class bonuses and replacement slots.");
                Check(member.Weapon == weapon && member.Armor == armor && member.Accessory == accessory, "Preview preserves current equipment and sockets.");
            }
            Check(menu.BeginQuantity(), "Every gear category can be purchased."); menu.SetMaximum();
            int gold = menu.Service.Progress.Gold, total = (int)menu.Total;
            menu.ChangeFilter(1); Check(menu.Filter == filter, "Category is frozen during confirmation.");
            Check(menu.Confirm() && menu.Service.Progress.Gold == gold - total && menu.Owned >= 2, "Gear purchase grants distinct copies in each category.");
        }
        menu.SelectFilter(ShopEquipmentFilter.Weapons); menu.Browse(true);
        int initial = -1;
        for (int i = 0; i < menu.Entries.Count; i++) if (menu.Entries[i].Equipment == PrototypePartyFactory.InitialWeapon) { initial = i; break; }
        menu.Select(initial);
        Check(menu.Owned == 3 && menu.Equipped == 3 && !menu.BeginQuantity(), "Equipped copies are counted and protected from selling.");
        for (int i = 0; i < menu.Entries.Count; i++)
            if (menu.Entries[i].Equipment == PrototypePartyFactory.IronSword)
            { menu.Select(i); break; }
        Check(menu.BeginQuantity() && menu.Confirm(), "An unequipped weapon can be sold.");
        menu.SelectFilter(ShopEquipmentFilter.Accessories);
        Check(menu.BeginQuantity() && menu.Confirm(), "An unequipped accessory can be sold.");
    }
    private static void MateriaAndStaleSelection()
    {
        var menu = Open("materia-shop"); menu.Browse(false);
        Check(menu.Entries.Count == 18, "All materia products are available through scrolling.");
        menu.Move(-1); Check(menu.Index == 17, "Navigation wraps to the last catalog row.");
        menu.Move(1); Check(menu.Index == 0, "Navigation wraps back to the first row.");
        Check(menu.BeginQuantity() && menu.Confirm() && menu.Owned == 1 && menu.Equipped == 0, "Materia purchase adds a free copy.");
        menu.Browse(true);
        for (int i = 0; i < menu.Entries.Count; i++) if (menu.Entries[i].Materia == PrototypePartyFactory.FireMateria) { menu.Select(i); break; }
        Check(menu.BeginQuantity() && menu.Confirm(), "Materia can be sold from the matching shop.");
        menu = Open("item-shop"); menu.Browse(false); menu.BeginQuantity();
        var product = menu.Selected.Product;
        menu.Service.TryBuy(menu.Shop, product, product.Stock, out _);
        int gold = menu.Service.Progress.Gold;
        Check(!menu.Confirm() && menu.Service.Progress.Gold == gold, "Stale stock is revalidated by the transaction service.");
        menu.Refresh(); Check(!menu.BeginQuantity() && menu.Message == "Produto esgotado.", "Sold-out products remain visible but cannot be confirmed.");
    }
    private static void LimitsAndEmptyCategories()
    {
        var menu = Open("item-shop", int.MaxValue); menu.Browse(true);
        Check(!menu.BeginQuantity() && menu.Message == "Saldo de ouro no limite.", "Selling cannot overflow the balance.");
        menu.Service.Progress.SetInfiniteGold(true);
        Check(menu.BeginQuantity() && menu.RemainingGold == int.MaxValue && menu.Confirm(), "Infinite gold remains compatible with sales and balance previews.");
        menu = Open("item-shop"); menu.Browse(false);
        menu.Service.Party.Consumables.TryAdd(PrototypePartyFactory.Potion, int.MaxValue - 2, out _);
        Check(menu.Maximum == 0 && !menu.BeginQuantity() && menu.Message == "Inventario no limite.", "Purchases respect stack capacity before confirmation.");
        var empty = new ShopInventory("empty", "Equipamentos", ShopCategory.Equipment);
        var service = new ShopService(PrototypePartyFactory.Create(), new ExplorationProgress(), empty);
        menu = new ShopMenuSession(); menu.Open(service, empty); menu.Browse(false); menu.Move(1); menu.ChangeFilter(-1);
        Check(menu.Selected == null && !menu.BeginQuantity() && menu.Owned == 0 && menu.Equipped == 0, "Empty filters remain navigable and safe.");
    }
    public static void Main()
    {
        BuyAndSellItems(); EquipmentAndPreviews(); MateriaAndStaleSelection(); LimitsAndEmptyCategories();
        int commerceChecks = ShopChecks.Run();
        Console.WriteLine("SHOP_MENU_CHECKS_OK: " + checks + " menu checks; " + commerceChecks + " existing commerce checks.");
    }
}
