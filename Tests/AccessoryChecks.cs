using DragonQuest.Content;
using System;
using System.Linq;
using System.Text;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using DragonQuest.Shops;

internal static class AccessoryChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static void Reject(Action action, string message)
    { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } Check(rejected, message); }
    private static JourneyData Capture(ShopService shop) => JourneySnapshot.Capture(shop.Party, shop.Progress, new AncientSecrets(), new DragonProgress(), shop,
        WorldNavigation.World, 0, -7);
    private static JourneyData Clone(JourneyData data) => SaveStore.Decode(SaveStore.Encode(data));
    private static void Corrupt(JourneyData data, Action<JourneyData> change, string message)
    { var copy = Clone(data); change(copy); Reject(() => JourneySnapshot.Restore(copy), message); }
    private static void ShopStatsAndTransfer()
    {
        var party = PartySetup.Create(); var shops = ShopCatalog.Create(party, new ExplorationProgress(1000));
        var shop = shops.FindShop("equipment-shop");
        foreach (string key in new[] { "power-ring", "guardian-bracelet", "arcane-pendant" })
        {
            var product = shop.Products.First(p => p.Id == key);
            Check(product.Price == 90 && product.Stock == 2 && product.Equipment.Slot == EquipmentSlot.Accessory,
                "Accessory is registered in the equipment shop with stock and price.");
            Check(shops.TryBuy(shop, product, 1, out _) && product.Stock == 1, "Buying creates an owned accessory and decrements stock.");
        }
        Check(shops.Progress.Gold == 730 && party.Equipments.Count == 9, "Accessory purchases debit the wallet without changing the starting kit.");
        var ring = party.Equipments.First(g => g.Definition == EquipmentCatalog.PowerRing);
        var bracelet = party.Equipments.First(g => g.Definition == EquipmentCatalog.GuardianBracelet);
        var pendant = party.Equipments.First(g => g.Definition == EquipmentCatalog.ArcanePendant);
        var aren = party.Members[0]; var lia = party.Members[1]; var bram = party.Members[2]; var materia = aren.Weapon.Sockets[0];
        Check(party.TryEquipEquipment(aren, ring, out _) && aren.Attack == 38 && aren.Weapon.Sockets[0] == materia,
            "The ring adds four base attack before existing materia modifiers and preserves weapon materias.");
        Check(party.TryEquipEquipment(bram, bracelet, out _) && bram.Defense == 24, "The bracelet adds four defense without a class multiplier.");
        Check(party.TryEquipEquipment(lia, pendant, out _) && lia.Magic == 24, "The pendant adds four magic.");
        foreach (var member in party.Members)
        {
            var combat = member.CreateCombatant();
            Check(combat.Definition.Attack == member.Attack && combat.Definition.Defense == member.Defense
                && combat.Definition.Magic == member.Magic, "Accessory stats reach actual combat definitions.");
        }
        Check(!party.TryEquipEquipment(aren, ring, out _) && !party.TryEquipEquipment(aren, new EquipmentLoadout(ring.Definition, ring.Id), out _),
            "Same accessory and foreign identity cannot be equipped again.");
        Reject(() => new EquipmentDefinition("Invalid accessory", 0, 0, 2, EquipmentSlot.Accessory), "Accessories reject materia slots.");
        Check(!party.TryEquip(aren, ring, 0, materia, out _), "Materia cannot be equipped on an accessory.");
        var menu = new EquipmentMenuSession(); menu.Reset(party, 1); menu.SelectCategory(EquipmentMenuCategory.Accessories, party);
        menu.SelectItem(menu.Items.ToList().IndexOf(ring)); var preview = menu.PreviewStats;
        Check(preview.Attack > menu.CurrentStats.Attack && preview.Magic < menu.CurrentStats.Magic,
            "Accessory replacement previews simultaneous green and red differences.");
        Check(aren.Accessory == ring && lia.Accessory == pendant, "Reading accessory preview does not mutate owners.");
        Check(menu.Apply(party) && lia.Accessory == ring && aren.Accessory == pendant, "Accessory exchange between allies is atomic.");
        for (int i = 0; i < EquipmentMenuStats.Count; i++) Check(preview[i] == menu.CurrentStats[i], "Accessory confirmation matches preview stat " + i);
        Check(aren.Weapon.Sockets[0] == materia && party.IsEquipped(ring) && party.LocationOf(ring) == "Lia",
            "Accessory transfer preserves materias and updates ownership/location.");
        var sale = shops.SaleEntries(shop).First(e => e.Equipment == ring);
        Check(!sale.CanSell && !shops.TrySell(shop, sale, 1, out _), "Equipped accessories are protected from sale.");
        var data = Capture(shops); var restored = JourneySnapshot.Restore(Clone(data));
        foreach (var member in party.Members)
        {
            var loaded = restored.Party.Members.First(m => m.Id == member.Id);
            Check(loaded.Accessory?.Id == member.Accessory?.Id && loaded.Attack == member.Attack
                && loaded.Defense == member.Defense && loaded.Magic == member.Magic, "Save roundtrip preserves accessory identity, owner and stats.");
        }
        Check(restored.Shops.FindShop("equipment-shop").Products.First(p => p.Id == "power-ring").Stock == 1,
            "Accessory stock remains persisted.");
        Corrupt(data, d => d.Members[0].Accessory = d.Members[1].Accessory, "Duplicate equipped accessory IDs are rejected.");
        Corrupt(data, d => d.Members[0].Weapon = d.Members[0].Accessory, "An accessory cannot load in a weapon slot.");
        Corrupt(data, d => d.Members[0].Accessory = "missing", "Unknown accessory IDs are rejected.");
        Corrupt(data, d => d.Gear.First(g => g.Definition == "power-ring").Sockets = new[] { "start-heavy" }, "Accessory socket injection is rejected.");
        Corrupt(data, d => d.EquipmentCatalogVersion = 2, "Unknown equipment catalog versions are rejected.");
        Corrupt(data, d => d.Stocks = d.Stocks.Where(s => s.Product != "power-ring").ToArray(), "Current saves require complete accessory stock.");
        menu.SelectItem(0); preview = menu.PreviewStats;
        Check(menu.Apply(party) && lia.Accessory == null && aren.Weapon.Sockets[0] == materia, "Removing an accessory preserves weapon materias.");
        for (int i = 0; i < EquipmentMenuStats.Count; i++) Check(preview[i] == menu.CurrentStats[i], "Accessory removal matches preview stat " + i);
        sale = shops.SaleEntries(shop).First(e => e.Equipment == ring);
        Check(sale.CanSell && shops.TrySell(shop, sale, 1, out _) && shops.Progress.Gold == 775 && !party.Equipments.Contains(ring),
            "An unequipped accessory can be sold once for half price.");
    }
    private static void LegacySaves()
    {
        var party = PartySetup.Create(); var shops = ShopCatalog.Create(party, new ExplorationProgress(50));
        var legacy = Capture(shops); legacy.EquipmentCatalogVersion = 0;
        legacy.Stocks = legacy.Stocks.Where(s => s.Product != "power-ring" && s.Product != "guardian-bracelet" && s.Product != "arcane-pendant").ToArray();
        string json = Encoding.UTF8.GetString(SaveStore.Encode(legacy));
        Check(!json.Contains("Accessory") && !json.Contains("EquipmentCatalogVersion"), "Legacy payload has no accessory or catalog field.");
        var restored = JourneySnapshot.Restore(Clone(legacy));
        Check(restored.Party.Members.All(m => m.Accessory == null) && restored.Party.Equipments.Count == 6 && restored.Progress.Gold == 50,
            "Old journeys retain their original inventory, empty accessory slots and wallet.");
        Check(restored.Shops.FindShop("equipment-shop").Products.Where(p => p.Equipment.Slot == EquipmentSlot.Accessory).All(p => p.Stock == 2),
            "Old journeys gain the new accessory products with their initial shop stock.");
        Corrupt(legacy, d => d.Stocks = d.Stocks.Skip(1).ToArray(), "Legacy migration still rejects missing original products.");
        var migrated = Capture(restored.Shops);
        Check(migrated.EquipmentCatalogVersion == 1 && migrated.Stocks.Length == legacy.Stocks.Length + 3,
            "Saving an old journey upgrades its equipment catalog explicitly.");
    }
    public static int Main(string[] args)
    {
        try { ShopStatsAndTransfer(); LegacySaves(); Console.WriteLine("ACCESSORY_CHECKS_OK: " + checks + " checks."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
