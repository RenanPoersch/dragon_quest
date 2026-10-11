using System;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Inventory;
using DragonQuest.Prototype;
using DragonQuest.Shops;

internal static class ShopChecks
{
    private static int checks;
    private static void Assert(bool condition, string description) { checks++; if (!condition) throw new Exception(description); }
    private static void Reject(Action action, string description)
    {
        bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; }
        Assert(rejected, description);
    }
    private static ShopService Create(int gold = PrototypeShopFactory.StartingGold) => PrototypeShopFactory.Create(PrototypePartyFactory.Create(), new ExplorationProgress(gold));
    private static ShopProduct Product(ShopInventory shop, string id)
    { foreach (ShopProduct product in shop.Products) if (product.Id == id) return product; throw new Exception("Produto ausente: " + id); }
    private static SaleEntry Sale(ShopService service, ShopInventory shop, object owned)
    {
        foreach (SaleEntry entry in service.SaleEntries(shop))
            if ((object)entry.Item == owned || (object)entry.Equipment == owned || (object)entry.Materia == owned) return entry;
        throw new Exception("Objeto ausente na lista de venda.");
    }
    private static void Buy(ShopService service, ShopInventory shop, ShopProduct product, int quantity)
        => Assert(service.TryBuy(shop, product, quantity, out string message), "Compra recusada: " + message);

    private static void CheckStartAndPurchases()
    {
        ShopService service = Create();
        Assert(service.Progress.Gold == 50 && service.Shops.Count == 3, "Inicio deve ter 50 moedas e tres lojas.");
        Assert(service.Party.Materias.Count == 5 && service.Party.Equipments.Count == 6
            && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 2
            && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Antidote) == 1
            && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Ether) == 0,
            "Kit economico deve conter somente equipamento simples, materias iniciais, duas pocoes e um antidoto.");
        foreach (MateriaInstance materia in service.Party.Materias)
            Assert(materia.Definition.Type != MateriaType.Summon && service.Party.IsEquipped(materia), "Materias iniciais devem estar equipadas; nao ha summon gratuito.");
        ShopInventory items = service.FindShop("item-shop"), gear = service.FindShop("equipment-shop");
        ShopProduct potion = Product(items, "potion");
        Buy(service, items, potion, 2);
        Assert(service.Progress.Gold == 30 && potion.Stock == 10 && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 4,
            "Compra multipla deve debitar o total, reduzir estoque e adicionar unidades.");
        Assert(!service.TryBuy(items, potion, 0, out _) && !service.TryBuy(items, potion, -1, out _), "Quantidade nao positiva deve falhar.");
        Assert(!service.TryBuy(items, potion, 11, out _), "Compra acima do estoque deve falhar.");
        Assert(!service.TryBuy(items, Product(items, "ether"), 2, out _), "Ouro insuficiente deve falhar.");
        var counterfeit = new ShopProduct("potion", PrototypePartyFactory.Potion, 2, 100);
        Assert(!service.TryBuy(items, counterfeit, 1, out _) && !service.TryBuy(gear, potion, 1, out _), "Produto falso ou de outra loja deve ser recusado.");
        ShopService foreign = Create();
        Assert(!service.TryBuy(foreign.FindShop("item-shop"), potion, 1, out _) && !service.TryBuy(null, potion, 1, out _), "Loja estrangeira ou nula deve falhar.");
        Assert(service.Progress.Gold == 30 && potion.Stock == 10 && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 4,
            "Falhas nao podem alterar ouro, estoque ou quantidade.");
        Assert(foreign.Progress.Gold == 50 && Product(foreign.FindShop("item-shop"), "potion").Stock == 12, "Sessoes devem ter estoques e ouro independentes.");
    }

    private static void CheckDistinctEquipmentAndMateria()
    {
        ShopService service = Create(1000);
        ShopInventory gear = service.FindShop("equipment-shop"), magic = service.FindShop("materia-shop");
        ShopProduct sword = Product(gear, "iron-sword"), fire = Product(magic, "fire"), all = Product(magic, "all");
        Buy(service, gear, sword, 2);
        EquipmentLoadout one = service.Party.Equipments[6], two = service.Party.Equipments[7];
        Assert(one != two && one.Id != two.Id && one.Definition == two.Definition && !service.Party.IsEquipped(one)
            && one.Sockets[0] == null && sword.Stock == 0 && service.Progress.Gold == 880, "Cada equipamento comprado deve ser uma copia distinta e vazia, sem equipar automaticamente.");
        Assert(!service.TryBuy(gear, sword, 1, out _), "Produto esgotado nao pode ser comprado novamente.");
        Buy(service, magic, fire, 2); Buy(service, magic, all, 2);
        MateriaInstance first = service.Party.Materias[5], second = service.Party.Materias[6];
        Assert(first != second && first.Id != second.Id && first.Definition == second.Definition && !service.Party.IsEquipped(first),
            "Copias compradas da mesma materia devem ter identidades independentes.");
        PartyMember aren = service.Party.Members[0], bram = service.Party.Members[2];
        Assert(service.Party.TryEquip(aren, aren.Armor, 2, first, out _)
            && service.Party.TryEquip(aren, aren.Armor, 3, service.Party.Materias[7], out _), "Materias compradas devem formar combo.");
        bool combo = false;
        foreach (var command in aren.CreateCombatant().Definition.Abilities)
            if (command.SourceAbilityId == "fire") combo = command.Targets == DragonQuest.Combat.TargetRule.AllEnemies && command.MpCost == 9;
        Assert(combo, "Fogo comprado + All comprado deve conceder comando elemental em area.");
        Assert(service.Party.TryEquip(bram, bram.Armor, 0, second, out _), "Tank tambem pode usar magia comprada.");
        MateriaInstance heavy = aren.Weapon.Sockets[0];
        Assert(service.Party.TryEquipEquipment(aren, one, out _) && aren.Attack == 32 && !service.Party.IsEquipped(heavy),
            "Equipamento comprado usa afinidade e devolve as materias do anterior.");
        Assert(service.Progress.Gold == 600 && fire.Stock == 0 && all.Stock == 0, "Precos e estoques devem refletir todas as compras.");
    }

    private static void CheckSalesAndStaleSelections()
    {
        ShopService service = Create(100);
        ShopInventory items = service.FindShop("item-shop"), gear = service.FindShop("equipment-shop"), magic = service.FindShop("materia-shop");
        ShopProduct ether = Product(items, "ether");
        Buy(service, items, ether, 2);
        SaleEntry etherSale = Sale(service, items, PrototypePartyFactory.Ether);
        Assert(etherSale.UnitPrice == 12, "Venda deve arredondar metade do preco por unidade para baixo.");
        Assert(service.TrySell(items, etherSale, 2, out _) && service.Progress.Gold == 74 && ether.Stock == 4
            && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Ether) == 0, "Venda multipla deve remover unidades e nao repor estoque do vendedor.");
        Assert(!service.TrySell(items, etherSale, 1, out _) && service.Progress.Gold == 74, "Selecao antiga nao pode vender estoque ja removido.");

        PartyMember aren = service.Party.Members[0];
        EquipmentLoadout weapon = aren.Weapon;
        MateriaInstance heavy = weapon.Sockets[0];
        SaleEntry equipped = Sale(service, gear, weapon), equippedMateria = Sale(service, magic, heavy);
        Assert(!equipped.CanSell && !equippedMateria.CanSell && !service.TrySell(gear, equipped, 1, out _)
            && !service.TrySell(magic, equippedMateria, 1, out _), "Objetos equipados devem ficar bloqueados.");
        Assert(service.Party.TryRemoveEquipment(aren, EquipmentSlot.Weapon, out _) && service.Party.LocationOf(heavy) == "Inventario",
            "Desequipar para venda deve preservar todas as materias.");
        Assert(service.TrySell(gear, equipped, 1, out _) && service.Progress.Gold == 89 && service.Party.Equipments.Count == 5,
            "Selecao antiga deve revalidar estado atual e permitir vender arma desequipada por 15.");
        Assert(!service.TrySell(gear, equipped, 1, out _) && !service.TrySell(items, equippedMateria, 1, out _), "Nao pode vender objeto removido ou na categoria errada.");
        Assert(service.TrySell(magic, equippedMateria, 1, out _) && service.Progress.Gold == 109 && service.Party.Materias.Count == 4,
            "Materia devolvida deve poder ser vendida individualmente por metade do valor.");
        SaleEntry potionSale = Sale(service, items, PrototypePartyFactory.Potion);
        Assert(!service.TrySell(items, potionSale, 3, out _) && !service.TrySell(items, potionSale, 0, out _)
            && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 2, "Venda invalida deve preservar unidades.");
    }

    private static void CheckProtectionAndOverflow()
    {
        var storyArmor = new EquipmentDefinition("Armadura da historia", 0, 20, 2, EquipmentSlot.Armor);
        var storyCopy = new EquipmentLoadout(storyArmor);
        PartyProgress party = PrototypePartyFactory.Create();
        Assert(party.TryAddEquipments(storyCopy), "Recompensa de equipamento deve poder entrar no inventario.");
        var shop = new ShopInventory("story-shop", "Equipamentos", ShopCategory.Equipment, new ShopProduct("story", storyArmor, 100, 0, canSell: false));
        var service = new ShopService(party, new ExplorationProgress(50), shop);
        SaleEntry protectedEntry = Sale(service, shop, storyCopy);
        Assert(!protectedEntry.CanSell && !service.TrySell(shop, protectedEntry, 1, out _) && service.Progress.Gold == 50,
            "Item de historia explicitamente protegido nao pode ser vendido mesmo com preco cadastrado.");

        service = Create(int.MaxValue);
        ShopInventory items = service.FindShop("item-shop");
        SaleEntry potion = Sale(service, items, PrototypePartyFactory.Potion);
        Assert(!service.TrySell(items, potion, 1, out _) && service.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 2,
            "Overflow de ouro deve rejeitar venda antes de remover item.");
        service = Create(100);
        items = service.FindShop("item-shop");
        Assert(service.Party.Consumables.TryAdd(PrototypePartyFactory.Potion, int.MaxValue - 2, out _), "Fixture deve encher pilha.");
        Assert(!service.TryBuy(items, Product(items, "potion"), 1, out _) && service.Progress.Gold == 100 && Product(items, "potion").Stock == 12,
            "Overflow do inventario deve rejeitar compra sem debitar ouro/estoque.");
        var hugeProduct = new ShopProduct("huge", PrototypePartyFactory.Potion, int.MaxValue, int.MaxValue);
        var hugeShop = new ShopInventory("huge", "Itens", ShopCategory.Items, hugeProduct);
        service = new ShopService(PrototypePartyFactory.Create(), new ExplorationProgress(int.MaxValue), hugeShop);
        Assert(!service.TryBuy(hugeShop, hugeProduct, 2, out _) && hugeProduct.Stock == int.MaxValue,
            "Preco total deve usar inteiro longo e rejeitar compra acima do saldo sem overflow.");
        Assert(!service.Progress.TryAddGold(-1) && !service.Progress.TrySpendGold(0) && !service.Progress.TrySpendGold(-1), "Operacoes invalidas de ouro devem falhar.");
        Reject(() => new ExplorationProgress(-1), "Saldo inicial negativo deve ser recusado.");
    }

    private static void CheckOwnershipAndCatalogs()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        MateriaInstance original = party.Materias[0];
        var newMateria = new MateriaInstance("fresh", PrototypePartyFactory.FireMateria);
        Assert(!party.TryAddMaterias(newMateria, new MateriaInstance(original.Id, original.Definition)) && party.Materias.Count == 5,
            "Lote com ID repetido deve falhar sem inserir parte dos objetos.");
        EquipmentLoadout weapon = party.Equipments[0];
        Assert(!party.TryAddEquipments(new EquipmentLoadout(weapon.Definition), new EquipmentLoadout(weapon.Definition, weapon.Id)) && party.Equipments.Count == 6,
            "Lote de equipamentos deve validar todas as identidades antes de inserir.");
        Assert(!party.TryRemoveOwnedEquipment(weapon) && !party.TryRemoveOwnedMateria(original), "Remocao direta de objetos equipados deve ser bloqueada.");
        var product = new ShopProduct("potion", PrototypePartyFactory.Potion, 10, 12);
        Reject(() => new ShopProduct("bad", PrototypePartyFactory.Potion, 0, 1), "Produto gratuito deve ser recusado.");
        Reject(() => new ShopInventory("bad", "Loja", ShopCategory.Items, product, product), "Produtos repetidos devem ser recusados.");
        Reject(() => new ShopInventory("bad", "Loja", ShopCategory.Equipment, product), "Categoria errada deve ser recusada.");
        var a = new ShopInventory("a", "Loja A", ShopCategory.Items, product);
        var b = new ShopInventory("b", "Loja B", ShopCategory.Items, product);
        Reject(() => new ShopService(party, new ExplorationProgress(), a, b), "Mesmo estoque nao pode pertencer a duas lojas da sessao.");
        ShopService service = Create();
        Assert(service.FindShop("unknown") == null && service.SaleEntries(null).Count == 0, "Consultas desconhecidas nao devem criar lojas.");
    }

    public static int Run()
    {
        CheckStartAndPurchases(); CheckDistinctEquipmentAndMateria(); CheckSalesAndStaleSelections();
        CheckProtectionAndOverflow(); CheckOwnershipAndCatalogs();
        return checks;
    }
}
