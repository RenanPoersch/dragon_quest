using System;
using System.Collections.Generic;
using DragonQuest.Equipment;
using DragonQuest.Inventory;

namespace DragonQuest.Shops
{
    public enum ShopCategory { Items, Equipment, Materia }

    public sealed class ShopProduct
    {
        public string Id { get; }
        public ShopCategory Category { get; }
        public ConsumableDefinition Item { get; }
        public EquipmentDefinition Equipment { get; }
        public MateriaDefinition Materia { get; }
        public int Price { get; }
        public int Stock { get; private set; }
        public int InitialStock { get; }
        public bool CanSell { get; }
        public string Name => Item?.Name ?? Equipment?.Name ?? Materia.Name;
        public string Description => Item?.Description ?? Materia?.Description ??
            "ATQ +" + Equipment.AttackBonus + " / DEF +" + Equipment.DefenseBonus + " / MAG +" + Equipment.MagicBonus
            + " / " + Equipment.SocketCount / 2 + " pares ligados. Valores base antes da afinidade de classe.";

        public ShopProduct(string id, ConsumableDefinition item, int price, int stock, bool canSell = true)
            : this(id, ShopCategory.Items, price, stock, canSell) { Item = item ?? throw new ArgumentNullException(nameof(item)); }
        public ShopProduct(string id, EquipmentDefinition equipment, int price, int stock, bool canSell = true)
            : this(id, ShopCategory.Equipment, price, stock, canSell) { Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment)); }
        public ShopProduct(string id, MateriaDefinition materia, int price, int stock, bool canSell = true)
            : this(id, ShopCategory.Materia, price, stock, canSell) { Materia = materia ?? throw new ArgumentNullException(nameof(materia)); }
        private ShopProduct(string id, ShopCategory category, int price, int stock, bool canSell)
        {
            if (string.IsNullOrWhiteSpace(id) || price < 2 || stock < 0) throw new ArgumentException("Produto exige ID, preco minimo 2 e estoque nao negativo.");
            Id = id; Category = category; Price = price; Stock = stock; InitialStock = stock; CanSell = canSell;
        }
        internal void Consume(int quantity) => Stock -= quantity;
        internal void Restock() => Stock = InitialStock;
    }

    public sealed class ShopInventory
    {
        public string Id { get; }
        public string Name { get; }
        public ShopCategory Category { get; }
        public IReadOnlyList<ShopProduct> Products { get; }
        public ShopInventory(string id, string name, ShopCategory category, params ShopProduct[] products)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || !Enum.IsDefined(typeof(ShopCategory), category) || products == null)
                throw new ArgumentException("Dados da loja invalidos.");
            var ids = new HashSet<string>();
            var definitions = new HashSet<object>();
            foreach (ShopProduct product in products)
                if (product == null || product.Category != category || !ids.Add(product.Id)
                    || !definitions.Add((object)product.Item ?? (object)product.Equipment ?? product.Materia))
                    throw new ArgumentException("Produtos da loja devem ser distintos e pertencer a sua categoria.");
            Id = id; Name = name; Category = category;
            Products = Array.AsReadOnly((ShopProduct[])products.Clone());
        }
    }

    // Cada arma/materia e uma copia individual; consumiveis aparecem como pilhas.
    public sealed class SaleEntry
    {
        public ConsumableDefinition Item { get; }
        public EquipmentLoadout Equipment { get; }
        public MateriaInstance Materia { get; }
        public string Name => Item?.Name ?? Equipment?.Definition.Name ?? Materia.Definition.Name;
        public int Quantity { get; }
        public int UnitPrice { get; }
        public string Description { get; }
        public string BlockReason { get; }
        public bool CanSell => string.IsNullOrEmpty(BlockReason);
        internal SaleEntry(ConsumableDefinition item, EquipmentLoadout equipment, MateriaInstance materia,
            int quantity, int price, string description, string blockReason)
        {
            Item = item; Equipment = equipment; Materia = materia;
            Quantity = quantity; UnitPrice = price; Description = description; BlockReason = blockReason;
        }
    }
}
