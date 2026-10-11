using System;
using DragonQuest.Combat;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using DragonQuest.Shops;

internal static class ProgressionChecks
{
    private static int checks;
    private static void Assert(bool condition, string description) { checks++; if (!condition) throw new Exception(description); }
    private static ShopService Create(int gold = 50) => PrototypeShopFactory.Create(PrototypePartyFactory.Create(), new ExplorationProgress(gold));
    private static ShopProduct Product(ShopInventory shop, string id)
    { foreach (ShopProduct product in shop.Products) if (product.Id == id) return product; throw new Exception("Produto ausente: " + id); }

    private static BattleSession Fixture(bool victory)
    {
        var hero = new CombatantState("hero", new CombatantDefinition("Heroi", "Teste", 10, 0, victory ? 100 : 1, 0, 0, victory ? 20 : 1, PrototypeBattleFactory.Attack), CombatTeam.Party);
        var enemy = new CombatantState("enemy", new CombatantDefinition("Inimigo", "Teste", 10, 0, victory ? 1 : 100, 0, 0, victory ? 1 : 20, PrototypeBattleFactory.Attack), CombatTeam.Enemy);
        return new BattleSession(hero, enemy);
    }
    private static void Finish(BattleSession battle)
    {
        CombatantState target = null;
        foreach (CombatantState candidate in battle.Combatants) if (candidate != battle.CurrentActor) target = candidate;
        Assert(battle.TryAct(battle.CurrentActor, PrototypeBattleFactory.Attack, target, out _), "Fixture de recompensa deve terminar.");
    }

    private static void CheckBattleRewards()
    {
        ShopService shops = Create();
        var rewards = new BattleRewards(shops.Progress);
        BattleSession battle = Fixture(true);
        Assert(!rewards.TryClaim(battle, out _) && !rewards.HasClaimed(battle) && shops.Progress.Gold == 50, "Batalha em andamento nao concede ouro nem consome direito de recompensa.");
        Finish(battle);
        Assert(rewards.TryClaim(battle, out _) && shops.Progress.Gold == 85 && rewards.HasClaimed(battle), "Vitoria concede exatamente 35 moedas.");
        for (int i = 0; i < 3; i++) Assert(!rewards.TryClaim(battle, out _) && shops.Progress.Gold == 85, "Reabrir resultado ou retornar a cidade nao pode duplicar ouro.");
        ShopInventory items = shops.FindShop("item-shop");
        Assert(shops.TryBuy(items, Product(items, "ether"), 1, out _) && shops.Progress.Gold == 60, "Ouro ganho deve estar imediatamente disponivel nas lojas.");
        Assert(!rewards.TryClaim(battle, out _) && shops.Progress.Gold == 60 && Product(items, "ether").Stock == 5,
            "Gastar ouro nao libera recompensa antiga nem repoe consumiveis.");
        battle = Fixture(true); Finish(battle);
        Assert(rewards.TryClaim(battle, out _) && shops.Progress.Gold == 95 && Product(items, "ether").Stock == 5,
            "Outra vitoria legitima concede mais 35, mantendo estoque gasto.");
        battle = Fixture(false); Finish(battle);
        Assert(!rewards.TryClaim(battle, out _) && shops.Progress.Gold == 95 && !rewards.HasClaimed(battle), "Derrota nao concede recompensa nem remove ouro.");
        Assert(!rewards.TryClaim(null, out _), "Batalha nula deve ser recusada.");

        var maximum = new ExplorationProgress(int.MaxValue);
        rewards = new BattleRewards(maximum);
        battle = Fixture(true); Finish(battle);
        Assert(!rewards.TryClaim(battle, out _) && !rewards.HasClaimed(battle) && maximum.Gold == int.MaxValue,
            "Saldo cheio nao pode estourar ouro ou marcar recompensa nao recebida.");
        Assert(maximum.TrySpendGold(35) && rewards.TryClaim(battle, out _) && maximum.Gold == int.MaxValue,
            "Recompensa pendente pode ser recebida quando ha espaco no saldo.");
    }

    private static void CheckDeliveryAndRestock()
    {
        ShopService shops = Create(1000);
        ShopInventory items = shops.FindShop("item-shop"), materia = shops.FindShop("materia-shop"), gear = shops.FindShop("equipment-shop");
        Assert(shops.TryBuy(items, Product(items, "potion"), 3, out _) && shops.TryBuy(materia, Product(materia, "fire"), 2, out _)
            && shops.TryBuy(gear, Product(gear, "iron-sword"), 2, out _), "Fixture deve gastar estoques das tres categorias.");
        int beforeGold = shops.Progress.Gold, ownedPotions = shops.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion);
        int ownedMateria = shops.Party.Materias.Count, ownedGear = shops.Party.Equipments.Count;
        var quest = new DeliveryQuest(shops);
        Assert(quest.Stage == DeliveryStage.Available && quest.ActionLabel("item-shop") == "Aceitar entrega", "Missao deve iniciar disponivel no vendedor de itens.");
        Assert(!quest.TryInteract(materia, out _) && !quest.TryInteract(gear, out _) && !quest.TryInteract(null, out _), "Entrega fora de ordem ou em vendedor errado deve ser recusada.");
        ShopService foreign = Create();
        Assert(!quest.TryInteract(foreign.FindShop("item-shop"), out _), "Vendedor de outra sessao nao pode avancar a missao.");
        Assert(quest.TryInteract(items, out _) && quest.Stage == DeliveryStage.CarryingPackage && shops.Progress.Gold == beforeGold,
            "Aceitar entrega concede pacote de historia, sem ouro adiantado.");
        Assert(!quest.TryInteract(items, out _) && quest.ActionLabel("materia-shop") == "Entregar pacote", "Nao pode aceitar outro pacote enquanto carrega o primeiro.");
        Assert(quest.TryInteract(materia, out _) && quest.Stage == DeliveryStage.Delivered && shops.Progress.Gold == beforeGold,
            "Entregar pacote exige retorno ao remetente para receber recompensa.");
        Assert(!quest.TryInteract(materia, out _), "Pacote entregue nao pode ser entregue novamente.");
        var secondView = new DeliveryQuest(shops); // Duas referencias de UI nao podem duplicar o mesmo marco.
        Assert(secondView.TryInteract(items, out _) && secondView.TryInteract(materia, out _), "Fixture cria outra selecao pendente da mesma missao.");
        Assert(quest.TryInteract(items, out _) && quest.Stage == DeliveryStage.Completed && shops.Progress.Gold == beforeGold + 30,
            "Retorno concede 30 moedas e encerra a missao.");
        Assert(Product(items, "potion").Stock == 12 && Product(materia, "fire").Stock == 0 && Product(gear, "iron-sword").Stock == 0,
            "Marco deve repor apenas consumiveis aos valores iniciais, preservando equipamentos/materias esgotados.");
        Assert(shops.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == ownedPotions && shops.Party.Materias.Count == ownedMateria
            && shops.Party.Equipments.Count == ownedGear, "Reposicao do vendedor nao deve dar mercadorias gratis nem pacote vendavel ao jogador.");
        Assert(!secondView.TryInteract(items, out _) && secondView.Stage == DeliveryStage.Completed && shops.Progress.Gold == beforeGold + 30,
            "Outra selecao pendente deve reconhecer marco concluido antes de conceder ouro.");
        Assert(!quest.TryInteract(items, out _) && new DeliveryQuest(shops).Stage == DeliveryStage.Completed, "Missao concluida nao pode reiniciar na mesma sessao.");
        Assert(shops.TryBuy(items, Product(items, "potion"), 1, out _), "Consumivel reposto deve poder ser comprado novamente.");
        int afterGold = shops.Progress.Gold;
        Assert(!quest.TryInteract(items, out _) && !shops.TryRestockConsumables(DeliveryQuest.MilestoneId)
            && Product(items, "potion").Stock == 11 && shops.Progress.Gold == afterGold, "Revisitar o vendedor nao repete recompensa ou reposicao.");
        Assert(!shops.TryRestockConsumables(null) && !shops.TryRestockConsumables(" "), "Marco invalido nao pode repor estoque.");
        Assert(new DeliveryQuest(foreign).Stage == DeliveryStage.Available && foreign.Progress.Gold == 50,
            "Missao e marcos devem ser independentes entre sessoes.");
    }

    private static void CheckPendingQuestReward()
    {
        ShopService shops = Create(int.MaxValue);
        ShopInventory items = shops.FindShop("item-shop"), materia = shops.FindShop("materia-shop");
        Assert(shops.TryBuy(items, Product(items, "potion"), 1, out _), "Fixture abre somente 10 moedas de espaco.");
        var quest = new DeliveryQuest(shops);
        Assert(quest.TryInteract(items, out _) && quest.TryInteract(materia, out _), "Entrega deve progredir mesmo com saldo alto.");
        Assert(!quest.TryInteract(items, out _) && quest.Stage == DeliveryStage.Delivered && Product(items, "potion").Stock == 11
            && !shops.HasRestockedMilestone(DeliveryQuest.MilestoneId), "Falha ao receber nao conclui missao ou repoe estoque.");
        Assert(shops.Progress.TrySpendGold(20) && quest.TryInteract(items, out _) && shops.Progress.Gold == int.MaxValue
            && Product(items, "potion").Stock == 12, "Recompensa pendente deve completar quando saldo permitir.");
    }

    private static AbilityDefinition Command(CombatantState actor, string id)
    {
        foreach (AbilityDefinition ability in actor.Definition.Abilities) if (ability.SourceAbilityId == id) return ability;
        return null;
    }
    private static void CheckEconomicalPartyCanWin()
    {
        ShopService shops = Create();
        var rewards = new BattleRewards(shops.Progress);
        BattleSession battle = PrototypeBattleFactory.CreateEncounter(shops.Party);
        CombatantState guard = battle.Combatants[3];
        int turns = 0;
        while (battle.Outcome == BattleOutcome.None && turns++ < 500)
        {
            CombatantState actor = battle.CurrentActor;
            if (actor.Team == CombatTeam.Enemy) { Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve progredir no encontro economico."); continue; }
            AbilityDefinition ability = Command(actor, "attack");
            if (actor.Id == "aren" && actor.Mp >= 4) ability = Command(actor, "heavy-strike");
            if (actor.Id == "lia")
            {
                AbilityDefinition cleanse = Command(actor, "cleanse"), heal = Command(actor, "heal");
                if (actor.Mp >= 4 && battle.GetValidTargets(cleanse).Count > 0) ability = cleanse;
                else if (actor.Mp >= 6 && battle.GetValidTargets(heal).Count > 0) ability = heal;
            }
            if (actor.Id == "bram" && guard.IsAlive && guard.TauntTurns == 0 && actor.Mp >= 3) ability = Command(actor, "taunt");
            var targets = battle.GetValidTargets(ability);
            Assert(targets.Count > 0 && battle.TryAct(actor, ability, targets[0], out _), "Equipe inicial deve ter acao valida durante todo o encontro.");
        }
        Assert(turns < 500 && battle.Outcome == BattleOutcome.Victory, "Kit economico deve permitir primeira vitoria sem exigir compras ou consumiveis extras.");
        Assert(rewards.TryClaim(battle, out _) && shops.Progress.Gold == 85
            && shops.Party.Consumables.QuantityOf(PrototypePartyFactory.Potion) == 2 && shops.Party.Consumables.QuantityOf(PrototypePartyFactory.Antidote) == 1,
            "Primeira vitoria viavel concede ouro e preserva itens que nao foram usados.");
        var quest = new DeliveryQuest(shops);
        ShopInventory items = shops.FindShop("item-shop"), materiaShop = shops.FindShop("materia-shop");
        Assert(quest.TryInteract(items, out _) && quest.TryInteract(materiaShop, out _) && quest.TryInteract(items, out _)
            && shops.Progress.TryOpenChest("praca-primeiro-bau", 25) && shops.Progress.Gold == 140,
            "Uma vitoria, a entrega e o bau devem permitir juntar 140 moedas sem repetir encontros.");
        Assert(shops.TryBuy(materiaShop, Product(materiaShop, "fire"), 1, out _)
            && shops.TryBuy(materiaShop, Product(materiaShop, "all"), 1, out _) && shops.Progress.Gold == 0,
            "Economia inicial deve permitir comprar Fogo + All usando as tres fontes de ouro.");
        var aren = shops.Party.Members[0];
        Assert(shops.Party.TryEquip(aren, aren.Armor, 2, shops.Party.Materias[5], out _)
            && shops.Party.TryEquip(aren, aren.Armor, 3, shops.Party.Materias[6], out _), "Combo comprado deve poder ser montado na armadura inicial.");
        var next = PrototypeBattleFactory.CreateEncounter(shops.Party);
        AbilityDefinition fireAll = Command(next.Combatants[0], "fire");
        Assert(fireAll.Targets == TargetRule.AllEnemies && fireAll.Element == DamageElement.Fire && fireAll.MpCost == 9,
            "Proxima batalha deve reconhecer o combo comprado com as recompensas.");
    }

    public static int Run()
    {
        CheckBattleRewards(); CheckDeliveryAndRestock(); CheckPendingQuestReward(); CheckEconomicalPartyCanWin();
        return checks;
    }
}
