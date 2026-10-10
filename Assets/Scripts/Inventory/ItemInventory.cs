using System;
using System.Collections.Generic;

namespace DragonQuest.Inventory
{
    public sealed class ItemStack
    {
        public ConsumableDefinition Definition { get; }
        public int Quantity { get; private set; }
        public ItemStack(ConsumableDefinition definition, int quantity)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            Quantity = quantity;
        }
        internal void Add(int amount) => Quantity += amount;
        internal void Consume() => Quantity--;
    }

    public sealed class ItemInventory
    {
        private readonly List<ItemStack> items = new List<ItemStack>();
        public IReadOnlyList<ItemStack> Items { get; }
        public ItemInventory(params ItemStack[] initialItems)
        {
            if (initialItems == null) throw new ArgumentNullException(nameof(initialItems));
            var ids = new HashSet<string>();
            foreach (ItemStack stack in initialItems)
            {
                if (stack == null || !ids.Add(stack.Definition.Id)) throw new ArgumentException("Pilhas nulas ou IDs repetidos.");
                items.Add(new ItemStack(stack.Definition, stack.Quantity));
            }
            Items = items.AsReadOnly();
        }

        private ItemStack Find(ConsumableDefinition item)
        {
            foreach (ItemStack stack in items) if (stack.Definition == item) return stack;
            return null;
        }
        public int QuantityOf(ConsumableDefinition item) => Find(item)?.Quantity ?? 0;
        public bool TryAdd(ConsumableDefinition item, int amount, out string message)
        {
            if (item == null || amount <= 0) { message = "Item ou quantidade invalida."; return false; }
            ItemStack existing = Find(item);
            foreach (ItemStack stack in items)
                if (stack.Definition.Id == item.Id && stack != existing)
                { message = "ID de item ja cadastrado com outra definicao."; return false; }
            if (existing != null && existing.Quantity > int.MaxValue - amount)
            { message = "Quantidade excede o limite do inventario."; return false; }
            if (existing == null) items.Add(new ItemStack(item, amount)); else existing.Add(amount);
            message = "Item adicionado."; return true;
        }
        internal bool TryConsume(ConsumableDefinition item)
        {
            ItemStack stack = Find(item);
            if (stack == null || stack.Quantity == 0) return false;
            stack.Consume(); return true;
        }
    }
}
