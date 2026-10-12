using System;
using System.Collections.Generic;
using DragonQuest.Equipment;
using DragonQuest.Exploration;

namespace DragonQuest.Shops
{
    // Transacoes sincronas: valida todos os limites antes de alterar ouro, estoque ou propriedade.
    public sealed class ShopService
    {
        private readonly HashSet<string> restockedMilestones = new HashSet<string>();
        public PartyProgress Party { get; }
        public ExplorationProgress Progress { get; }
        public IReadOnlyList<ShopInventory> Shops { get; }
        internal void RestoreDeliveryRestock(bool completed)
        { restockedMilestones.Clear(); if (completed) restockedMilestones.Add(Progression.DeliveryQuest.MilestoneId); }

        public ShopService(PartyProgress party, ExplorationProgress progress, params ShopInventory[] shops)
        {
            Party = party ?? throw new ArgumentNullException(nameof(party));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (shops == null) throw new ArgumentNullException(nameof(shops));
            var ids = new HashSet<string>();
            var products = new HashSet<ShopProduct>();
            foreach (ShopInventory shop in shops)
            {
                if (shop == null || !ids.Add(shop.Id)) throw new ArgumentException("Lojas nulas ou repetidas.");
                foreach (ShopProduct product in shop.Products)
                    if (!products.Add(product)) throw new ArgumentException("Estoque nao pode ser compartilhado entre lojas.");
            }
            Shops = Array.AsReadOnly((ShopInventory[])shops.Clone());
        }

        public ShopInventory FindShop(string id)
        {
            foreach (ShopInventory shop in Shops) if (shop.Id == id) return shop;
            return null;
        }

        // Chamado pelo progresso da historia; abrir loja ou vencer batalha nao chama esta reposicao.
        internal bool TryRestockConsumables(string milestoneId)
        {
            if (string.IsNullOrWhiteSpace(milestoneId) || restockedMilestones.Contains(milestoneId)) return false;
            bool found = false;
            foreach (ShopInventory shop in Shops)
                if (shop.Category == ShopCategory.Items)
                {
                    found = true;
                    foreach (ShopProduct product in shop.Products) product.Restock();
                }
            if (!found) return false;
            restockedMilestones.Add(milestoneId); return true;
        }
        public bool HasRestockedMilestone(string milestoneId) => milestoneId != null && restockedMilestones.Contains(milestoneId);
        private bool Owns(ShopInventory shop) { foreach (ShopInventory owned in Shops) if (owned == shop) return true; return false; }
        private static bool Owns(ShopInventory shop, ShopProduct product)
        { foreach (ShopProduct owned in shop.Products) if (owned == product) return true; return false; }

        public bool TryBuy(ShopInventory shop, ShopProduct product, int quantity, out string message)
        {
            if (!Owns(shop) || product == null || !Owns(shop, product) || quantity <= 0)
            { message = "Loja, produto ou quantidade invalida."; return false; }
            if (quantity > product.Stock) { message = "Estoque insuficiente ou produto esgotado."; return false; }
            long total = (long)product.Price * quantity;
            if (total > Progress.Gold) { message = "Ouro insuficiente."; return false; }
            bool added;
            if (product.Item != null) added = Party.Consumables.TryAdd(product.Item, quantity, out message);
            else if (product.Equipment != null)
            {
                var copies = new EquipmentLoadout[quantity];
                for (int i = 0; i < copies.Length; i++) copies[i] = new EquipmentLoadout(product.Equipment);
                added = Party.TryAddEquipments(copies); message = "Nao foi possivel adicionar equipamento.";
            }
            else
            {
                var copies = new MateriaInstance[quantity];
                for (int i = 0; i < copies.Length; i++) copies[i] = new MateriaInstance(Guid.NewGuid().ToString("N"), product.Materia);
                added = Party.TryAddMaterias(copies); message = "Nao foi possivel adicionar materia.";
            }
            if (!added) return false;
            Progress.TrySpendGold((int)total);
            product.Consume(quantity);
            message = "Comprou " + quantity + "x " + product.Name + " por " + total + " moedas. Enviado ao inventario.";
            return true;
        }

        public IReadOnlyList<SaleEntry> SaleEntries(ShopInventory shop)
        {
            var entries = new List<SaleEntry>();
            if (!Owns(shop)) return entries;
            if (shop.Category == ShopCategory.Items)
            {
                foreach (var stack in Party.Consumables.Items)
                    if (stack.Quantity > 0) entries.Add(Entry(shop, stack.Definition, null, null, stack.Quantity));
            }
            else if (shop.Category == ShopCategory.Equipment)
                foreach (EquipmentLoadout equipment in Party.Equipments) entries.Add(Entry(shop, null, equipment, null, 1));
            else foreach (MateriaInstance materia in Party.Materias) entries.Add(Entry(shop, null, null, materia, 1));
            return entries.AsReadOnly();
        }

        private ShopProduct ProductFor(ShopInventory shop, object definition)
        {
            foreach (ShopProduct product in shop.Products)
                if ((object)product.Item == definition || (object)product.Equipment == definition || (object)product.Materia == definition) return product;
            return null;
        }

        private SaleEntry Entry(ShopInventory shop, Inventory.ConsumableDefinition item, EquipmentLoadout equipment, MateriaInstance materia, int quantity)
        {
            ShopProduct product = ProductFor(shop, (object)item ?? (object)equipment?.Definition ?? materia?.Definition);
            string reason = product == null ? "Objeto nao comercializado por esta loja." : !product.CanSell ? "Item de historia: venda bloqueada." : null;
            if (equipment != null && Party.IsEquipped(equipment) || materia != null && Party.IsEquipped(materia)) reason = "Desequipe no inventario antes de vender.";
            if (equipment != null) foreach (MateriaInstance socket in equipment.Sockets) if (socket != null) reason = "Remova as materias antes de vender.";
            if (equipment?.Definition.IsStoryItem == true) reason = "Item de historia: venda bloqueada.";
            return new SaleEntry(item, equipment, materia, quantity, product?.Price / 2 ?? 0,
                product?.Description ?? "Objeto protegido ou sem preco de venda.", reason);
        }

        public bool TrySell(ShopInventory shop, SaleEntry entry, int quantity, out string message)
        {
            if (!Owns(shop) || entry == null || quantity <= 0) { message = "Loja, objeto ou quantidade invalida."; return false; }
            // Reconsulta propriedade, encaixes, preco e quantidade: uma selecao antiga nao autoriza outra venda.
            SaleEntry current = null;
            foreach (SaleEntry candidate in SaleEntries(shop))
                if (entry.Item != null && candidate.Item == entry.Item || entry.Equipment != null && candidate.Equipment == entry.Equipment
                    || entry.Materia != null && candidate.Materia == entry.Materia) current = candidate;
            if (current == null) { message = "Objeto nao pertence mais ao inventario ou a esta categoria."; return false; }
            if (!current.CanSell) { message = current.BlockReason; return false; }
            if (quantity > current.Quantity) { message = "Quantidade nao disponivel."; return false; }
            long total = (long)current.UnitPrice * quantity;
            if (total > int.MaxValue || !Progress.CanReceiveGold((int)total)) { message = "Ouro excederia o limite."; return false; }
            bool removed = current.Item != null ? Party.Consumables.TryRemove(current.Item, quantity)
                : current.Equipment != null ? Party.TryRemoveOwnedEquipment(current.Equipment) : Party.TryRemoveOwnedMateria(current.Materia);
            if (!removed) { message = "Objeto em uso ou indisponivel."; return false; }
            Progress.TryAddGold((int)total);
            message = "Vendeu " + quantity + "x " + current.Name + " por " + total + " moedas.";
            return true;
        }
    }
}
