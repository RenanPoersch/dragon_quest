using DragonQuest.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Shops
{
    public enum ShopMenuPhase { Actions, Browse, Quantity }
    public enum ShopEquipmentFilter { Weapons, Armor, Accessories }

    public sealed class ShopMenuEntry
    {
        public ShopProduct Product { get; }
        public SaleEntry Sale { get; }
        public ConsumableDefinition Item => Product?.Item ?? Sale?.Item;
        public EquipmentDefinition Equipment => Product?.Equipment ?? Sale?.Equipment?.Definition;
        public MateriaDefinition Materia => Product?.Materia ?? Sale?.Materia?.Definition;
        public string Name => Product?.Name ?? Sale.Name;
        public string Description => Product?.Description ?? Sale.Description;
        public int Price => Product?.Price ?? Sale.UnitPrice;
        public int Available => Product?.Stock ?? Sale.Quantity;
        public string BlockReason => Sale?.BlockReason;
        public ShopMenuEntry(ShopProduct product) { Product = product; }
        public ShopMenuEntry(SaleEntry sale) { Sale = sale; }
        internal bool Same(ShopMenuEntry other) => other != null && (Product != null ? Product == other.Product
            : Sale.Item != null ? Sale.Item == other.Sale?.Item
            : Sale.Equipment != null ? Sale.Equipment == other.Sale?.Equipment : Sale.Materia == other.Sale?.Materia);
    }

    // Selection, limits and previews are independent of Unity and never mutate equipment.
    public sealed class ShopMenuSession
    {
        private readonly List<ShopMenuEntry> entries = new List<ShopMenuEntry>();
        public IReadOnlyList<ShopMenuEntry> Entries { get; }
        public ShopService Service { get; private set; }
        public ShopInventory Shop { get; private set; }
        public ShopMenuPhase Phase { get; private set; }
        public ShopEquipmentFilter Filter { get; private set; }
        public bool Selling { get; private set; }
        public int Index { get; private set; }
        public int Quantity { get; private set; } = 1;
        public string Message { get; private set; } = "";
        public ShopMenuEntry Selected => entries.Count == 0 ? null : entries[Index];
        public bool HasEquipment => Shop?.Category == ShopCategory.Equipment;
        public long Total => (long)(Selected?.Price ?? 0) * Quantity;
        public long RemainingGold => Service.Progress.HasInfiniteGold ? Service.Progress.Gold
            : (long)Service.Progress.Gold + (Selling ? Total : -Total);

        public ShopMenuSession() { Entries = entries.AsReadOnly(); }
        public void Open(ShopService service, ShopInventory shop)
        {
            if (service == null || shop == null || service.FindShop(shop.Id) != shop)
                throw new ArgumentException("Loja nao pertence a esta sessao.");
            Service = service; Shop = shop; Selling = false; Filter = ShopEquipmentFilter.Weapons;
            Phase = ShopMenuPhase.Actions; Index = 0; Quantity = 1; Message = ""; Refresh();
        }
        public void Browse(bool selling)
        {
            Selling = selling; Phase = ShopMenuPhase.Browse; Index = 0; Quantity = 1; Message = ""; entries.Clear(); Refresh();
        }
        public void Refresh()
        {
            if (Shop == null) return;
            ShopMenuEntry previous = Selected;
            entries.Clear();
            if (Selling)
            {
                foreach (SaleEntry sale in Service.SaleEntries(Shop))
                    if (Matches(sale.Equipment?.Definition)) entries.Add(new ShopMenuEntry(sale));
            }
            else foreach (ShopProduct product in Shop.Products)
                if (product.InitialStock > 0 && Matches(product.Equipment)) entries.Add(new ShopMenuEntry(product));
            if (Shop.Category == ShopCategory.Materia)
            {
                var ordered = entries.OrderBy(entry => entry.Materia.Type).ToArray();
                entries.Clear(); entries.AddRange(ordered);
            }
            int found = entries.FindIndex(entry => entry.Same(previous));
            Index = found >= 0 ? found : Math.Max(0, Math.Min(Index, entries.Count - 1));
            if (Phase == ShopMenuPhase.Quantity && (found < 0 || Maximum == 0)) Phase = ShopMenuPhase.Browse;
            Quantity = Math.Max(1, Math.Min(Quantity, Maximum));
        }
        private bool Matches(EquipmentDefinition equipment) => !HasEquipment || equipment != null
            && equipment.Slot == (Filter == ShopEquipmentFilter.Weapons ? EquipmentSlot.Weapon
                : Filter == ShopEquipmentFilter.Armor ? EquipmentSlot.Armor : EquipmentSlot.Accessory);
        public void SelectFilter(ShopEquipmentFilter filter)
        {
            if (!HasEquipment || Phase == ShopMenuPhase.Quantity || !Enum.IsDefined(typeof(ShopEquipmentFilter), filter) || Filter == filter) return;
            Filter = filter; Index = 0; Message = ""; entries.Clear(); Refresh();
        }
        public void ChangeFilter(int step) => SelectFilter((ShopEquipmentFilter)(((int)Filter + step % 3 + 3) % 3));
        public void Select(int index)
        {
            if (Phase == ShopMenuPhase.Quantity || index < 0 || index >= entries.Count || Index == index) return;
            Index = index; Quantity = 1; Message = "";
        }
        public void Move(int step)
        { if (entries.Count > 0) Select((Index + step % entries.Count + entries.Count) % entries.Count); }
        public int Owned
        {
            get
            {
                var entry = Selected; if (entry == null) return 0;
                if (entry.Item != null) return Service.Party.Consumables.QuantityOf(entry.Item);
                int count = 0;
                if (entry.Equipment != null)
                { foreach (var gear in Service.Party.Equipments) if (gear.Definition == entry.Equipment) count++; }
                else foreach (var materia in Service.Party.Materias) if (materia.Definition == entry.Materia) count++;
                return count;
            }
        }
        public int Equipped
        {
            get
            {
                var entry = Selected; if (entry == null || entry.Item != null) return 0;
                int count = 0;
                if (entry.Equipment != null)
                { foreach (var gear in Service.Party.Equipments) if (gear.Definition == entry.Equipment && Service.Party.IsEquipped(gear)) count++; }
                else foreach (var materia in Service.Party.Materias)
                    if (materia.Definition == entry.Materia && Service.Party.IsEquipped(materia)) count++;
                return count;
            }
        }
        public int Maximum
        {
            get
            {
                var entry = Selected; if (entry == null || entry.BlockReason != null) return 0;
                long limit = entry.Available;
                if (Selling)
                {
                    if (!Service.Progress.HasInfiniteGold && entry.Price > 0)
                        limit = Math.Min(limit, (int.MaxValue - Service.Progress.Gold) / entry.Price);
                }
                else
                {
                    limit = Math.Min(limit, Service.Progress.Gold / entry.Price);
                    if (entry.Item != null) limit = Math.Min(limit, int.MaxValue - Owned);
                }
                return (int)Math.Max(0, limit);
            }
        }
        public string UnavailableReason => Selected == null ? "Nenhum objeto disponivel."
            : Selected.BlockReason ?? (Selected.Available == 0 ? "Produto esgotado."
            : !Selling && Service.Progress.Gold < Selected.Price ? "Ouro insuficiente."
            : Maximum == 0 ? Selling ? "Saldo de ouro no limite." : "Inventario no limite." : "");
        public bool BeginQuantity()
        {
            Refresh();
            if (Maximum == 0) { Message = UnavailableReason; return false; }
            Phase = ShopMenuPhase.Quantity; Quantity = 1; Message = ""; return true;
        }
        public void AdjustQuantity(int delta)
        { if (Phase == ShopMenuPhase.Quantity) Quantity = (int)Math.Max(1, Math.Min((long)Quantity + delta, Math.Max(1, Maximum))); }
        public void SetMaximum() { if (Phase == ShopMenuPhase.Quantity) Quantity = Math.Max(1, Maximum); }
        public bool Confirm()
        {
            if (Phase != ShopMenuPhase.Quantity) return false;
            var entry = Selected;
            string message;
            bool accepted = Selling ? Service.TrySell(Shop, entry?.Sale, Quantity, out message)
                : Service.TryBuy(Shop, entry?.Product, Quantity, out message);
            Message = message; Phase = ShopMenuPhase.Browse; Quantity = 1; Refresh(); return accepted;
        }
        // Returns true only at the root, when the shop should close.
        public bool Back()
        {
            if (Phase == ShopMenuPhase.Actions) return true;
            Phase = Phase == ShopMenuPhase.Quantity ? ShopMenuPhase.Browse : ShopMenuPhase.Actions;
            Quantity = 1; Message = ""; return false;
        }
        public void Notify(string message) { Message = message ?? ""; Refresh(); }
        public EquipmentMenuStats Preview(PartyMember member)
        {
            var weapon = member.Weapon; var armor = member.Armor; var accessory = member.Accessory;
            var definition = Selected?.Equipment;
            if (definition != null)
            {
                var candidate = Selected.Sale?.Equipment;
                if (candidate == null || candidate != member.EquipmentIn(definition.Slot))
                    candidate = new EquipmentLoadout(definition, "shop-preview");
                if (definition.Slot == EquipmentSlot.Weapon) weapon = candidate;
                else if (definition.Slot == EquipmentSlot.Armor) armor = candidate;
                else accessory = candidate;
            }
            // Shop comparisons change only the equipment's base values, preserving every current materia bonus.
            return new EquipmentMenuStats(member, weapon, armor, accessory, preserveEquippedMaterias: true);
        }
    }
}
