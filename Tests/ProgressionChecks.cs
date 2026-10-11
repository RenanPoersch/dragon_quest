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

    private static BattleSession Fixture(bool victory, int goldReward = 35)
    {
        var hero = new CombatantState("hero", new CombatantDefinition("Heroi", "Teste", 10, 0, victory ? 100 : 1, 0, 0, victory ? 20 : 1, PrototypeBattleFactory.Attack), CombatTeam.Party);
        var enemy = new CombatantState("enemy", new CombatantDefinition("Inimigo", "Teste", 10, 0, victory ? 1 : 100, 0, 0, victory ? 1 : 20,
            ElementalAffinities.Neutral, goldReward, PrototypeBattleFactory.Attack), CombatTeam.Enemy);
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
        Assert(rewards.TryClaim(battle, out _) && shops.Progress.Gold == 85 && rewards.HasClaimed(battle), "Vitoria concede as 35 moedas definidas neste inimigo.");
        for (int i = 0; i < 3; i++) Assert(!rewards.TryClaim(battle, out _) && shops.Progress.Gold == 85, "Reabrir resultado ou retornar a cidade nao pode duplicar ouro.");
        ShopInventory items = shops.FindShop("item-shop");
        Assert(shops.TryBuy(items, Product(items, "ether"), 1, out _) && shops.Progress.Gold == 60, "Ouro ganho deve estar imediatamente disponivel nas lojas.");
        Assert(!rewards.TryClaim(battle, out _) && shops.Progress.Gold == 60 && Product(items, "ether").Stock == 5,
            "Gastar ouro nao libera recompensa antiga nem repoe consumiveis.");
        battle = Fixture(true); Finish(battle);
        Assert(rewards.TryClaim(battle, out _) && shops.Progress.Gold == 95 && Product(items, "ether").Stock == 5,
            "Outro inimigo com recompensa 35 concede esse valor, mantendo estoque gasto.");
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

    private static CombatantDefinition RewardEnemy(string name, int gold, int attack = 1)
        => new CombatantDefinition(name, "Inimigo", 10, 0, attack, 0, 0, 1,
            ElementalAffinities.Neutral, gold, PrototypeBattleFactory.Attack);

    private static BattleSession RewardEncounter(params CombatantDefinition[] enemies)
    {
        var participants = new CombatantState[enemies.Length + 1];
        participants[0] = new CombatantState("hero", new CombatantDefinition("Heroi", "Teste", 10, 7, 1000, 0, 0, 20,
            ElementalAffinities.Neutral, 999, PrototypeBattleFactory.Attack, PrototypeBattleFactory.Cleave), CombatTeam.Party);
        for (int i = 0; i < enemies.Length; i++) participants[i + 1] = new CombatantState("enemy-" + i, enemies[i], CombatTeam.Enemy);
        return new BattleSession(participants);
    }

    private static void FinishAll(BattleSession battle)
    {
        var targets = battle.GetValidTargets(PrototypeBattleFactory.Cleave);
        Assert(targets.Count > 0 && battle.TryAct(battle.CurrentActor, PrototypeBattleFactory.Cleave, targets[0], out _)
            && battle.Outcome == BattleOutcome.Victory, "Ataque em area deve concluir fixture com varios inimigos.");
    }

    private static void CheckEnemyBasedRewards()
    {
        var progress = new ExplorationProgress();
        var rewards = new BattleRewards(progress);
        BattleSession battle = Fixture(true, 7); Finish(battle);
        Assert(rewards.TryClaim(battle, out _) && progress.Gold == 7, "Inimigo de recompensa 7 deve conceder 7, sem valor fixo de vitoria.");
        battle = Fixture(true, 120); Finish(battle);
        Assert(rewards.TryClaim(battle, out _) && progress.Gold == 127, "Outro tipo de inimigo deve conceder seu valor proprio de 120.");

        battle = RewardEncounter(RewardEnemy("Batedor", 12), RewardEnemy("Capitao", 48));
        Assert(!rewards.TryClaim(battle, out _) && progress.Gold == 127, "Inimigos vivos nao concedem ouro antecipado.");
        Assert(battle.TryAct(battle.CurrentActor, PrototypeBattleFactory.Attack, battle.Combatants[1], out _)
            && !battle.Combatants[1].IsAlive && battle.Combatants[2].IsAlive && battle.Outcome == BattleOutcome.None,
            "Fixture deve derrotar somente o primeiro inimigo.");
        Assert(!rewards.TryClaim(battle, out _) && !rewards.HasClaimed(battle) && progress.Gold == 127,
            "Uma eliminacao parcial aguarda a vitoria para pagar.");
        Assert(battle.TryAct(battle.CurrentActor, PrototypeBattleFactory.Attack, battle.Combatants[0], out _), "Inimigo restante deve concluir seu turno.");
        Assert(battle.TryAct(battle.CurrentActor, PrototypeBattleFactory.Attack, battle.Combatants[2], out _)
            && battle.Outcome == BattleOutcome.Victory, "Segundo inimigo derrotado deve concluir a vitoria.");
        Assert(rewards.TryClaim(battle, out string message) && progress.Gold == 187,
            "Recompensa soma 12 + 48; ignora as 999 moedas na definicao do aliado vivo.");
        Assert(message.Contains("+60 moedas") && message.Contains("Batedor (12)") && message.Contains("Capitao (48)"),
            "Resultado deve identificar cada inimigo, seu valor e a soma recebida.");
        Assert(!rewards.TryClaim(battle, out _) && progress.Gold == 187, "Soma de varios inimigos nao pode ser recebida duas vezes.");

        CombatantDefinition repeated = RewardEnemy("Gemeo", 13);
        battle = RewardEncounter(repeated, repeated); FinishAll(battle);
        Assert(rewards.TryClaim(battle, out _) && progress.Gold == 213, "Duas instancias do mesmo tipo concedem 13 cada, sem deduplicar a definicao.");
        battle = RewardEncounter(RewardEnemy("Sem ouro", 0), RewardEnemy("Com ouro", 9)); FinishAll(battle);
        Assert(rewards.TryClaim(battle, out _) && progress.Gold == 222, "Inimigo com recompensa zero pode acompanhar outro remunerado.");
        battle = RewardEncounter(RewardEnemy("Sem ouro", 0)); FinishAll(battle);
        Assert(rewards.TryClaim(battle, out _) && rewards.HasClaimed(battle) && progress.Gold == 222,
            "Vitoria sem ouro deve ser registrada sem exigir deposito positivo.");
        Assert(!rewards.TryClaim(battle, out _) && progress.Gold == 222, "Recompensa zero tambem deve ser registrada uma unica vez.");

        battle = RewardEncounter(RewardEnemy("Batedor", 12), RewardEnemy("Capitao", 48, 1000));
        Assert(battle.TryAct(battle.CurrentActor, PrototypeBattleFactory.Attack, battle.Combatants[1], out _)
            && battle.TryAct(battle.CurrentActor, PrototypeBattleFactory.Attack, battle.Combatants[0], out _)
            && battle.Outcome == BattleOutcome.Defeat, "Fixture deve perder apos derrotar um inimigo.");
        Assert(!rewards.TryClaim(battle, out _) && !rewards.HasClaimed(battle) && progress.Gold == 222,
            "Derrota com eliminacao parcial continua sem conceder ouro.");

        var ally = new CombatantState("fallen", RewardEnemy("Aliado caido", 600), CombatTeam.Party);
        battle = RewardEncounter(RewardEnemy("Batedor", 12));
        battle = new BattleSession(battle.Combatants[0], ally, battle.Combatants[1]);
        ally.TakeDamage(ally.Hp);
        FinishAll(battle);
        Assert(rewards.TryClaim(battle, out _) && progress.Gold == 234, "Aliado derrotado nao pode entrar na recompensa de inimigos.");

        battle = RewardEncounter(RewardEnemy("Muito ouro", int.MaxValue), RewardEnemy("Uma moeda", 1)); FinishAll(battle);
        Assert(!rewards.TryClaim(battle, out _) && !rewards.HasClaimed(battle) && progress.Gold == 234,
            "Soma acima do limite deve falhar sem pagamento parcial, overflow ou registro de recebimento.");
        var maximum = new ExplorationProgress();
        var maximumRewards = new BattleRewards(maximum);
        battle = Fixture(true, int.MaxValue); Finish(battle);
        Assert(maximumRewards.TryClaim(battle, out _) && maximum.Gold == int.MaxValue, "Recompensa no limite exato deve ser permitida quando o saldo esta vazio.");

        Assert(new CombatantDefinition("Sem configuracao", "Teste", 10, 0, 1, 0, 0, 1, PrototypeBattleFactory.Attack).GoldReward == 0
            && new CombatantDefinition("Com afinidades", "Teste", 10, 0, 1, 0, 0, 1, ElementalAffinities.Neutral, PrototypeBattleFactory.Attack).GoldReward == 0,
            "Definicoes antigas sem recompensa explicita devem assumir zero.");
        bool rejected = false;
        try { RewardEnemy("Invalido", -1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Assert(rejected, "Recompensa negativa deve ser recusada na definicao do inimigo.");
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
        Assert(guard.Definition.GoldReward == 25 && battle.Combatants[4].Definition.GoldReward == 10,
            "Patrulha jogavel deve configurar 25 no guarda e 10 no soldado.");
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
        CheckBattleRewards(); CheckEnemyBasedRewards(); CheckDeliveryAndRestock(); CheckPendingQuestReward(); CheckEconomicalPartyCanWin();
        return checks;
    }
}
