using System;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Inventory;
using DragonQuest.Prototype;

internal static class EquipmentItemChecks
{
    private static int checks;
    private static ConsumableDefinition Potion => PrototypePartyFactory.Potion;
    private static ConsumableDefinition Ether => PrototypePartyFactory.Ether;
    private static ConsumableDefinition Antidote => PrototypePartyFactory.Antidote;
    private static ConsumableDefinition Feather => PrototypePartyFactory.PhoenixFeather;

    private static void Assert(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }
    private static PartyMember Member(PartyProgress party, string id)
    {
        foreach (PartyMember member in party.Members) if (member.Id == id) return member;
        throw new Exception("Membro ausente: " + id);
    }
    private static EquipmentLoadout Gear(PartyProgress party, string id)
    {
        foreach (EquipmentLoadout item in party.Equipments) if (item.Id == id) return item;
        throw new Exception("Equipamento ausente: " + id);
    }
    private static bool HasCommand(PartyMember member, string sourceId)
    {
        foreach (AbilityDefinition command in member.CreateCombatant().Definition.Abilities)
            if (command.SourceAbilityId == sourceId) return true;
        return false;
    }
    private static void Equip(PartyProgress party, PartyMember member, EquipmentLoadout item)
        => Assert(party.TryEquipEquipment(member, item, out string message), "Troca recusada: " + message);
    private static void EquipMateria(PartyProgress party, PartyMember member, EquipmentLoadout item, int slot, MateriaInstance materia)
        => Assert(party.TryEquip(member, item, slot, materia, out string message), "Materia recusada: " + message);
    private static bool Empty(EquipmentLoadout item)
    {
        foreach (MateriaInstance materia in item.Sockets) if (materia != null) return false;
        return true;
    }

    private static void CheckGearAndMateriaReturn()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        PartyMember aren = Member(party, "aren"), lia = Member(party, "lia"), bram = Member(party, "bram");
        var sword = Gear(party, "iron-sword");
        EquipmentLoadout oldWeapon = aren.Weapon, oldArmor = aren.Armor;
        MateriaInstance heavy = oldWeapon.Sockets[0], thunder = oldArmor.Sockets[2], all = oldArmor.Sockets[3];
        aren.CreateCombatant().TakeDamage(17);
        Equip(party, aren, sword);
        Assert(aren.Attack == 32 && Empty(sword) && Empty(oldWeapon), "Troca deve aplicar afinidade e deixar os dois equipamentos sem materias.");
        Assert(party.LocationOf(oldWeapon) == "Inventario" && party.LocationOf(heavy) == "Inventario", "Equipamento retirado e suas materias devem voltar ao inventario.");
        Assert(!HasCommand(aren, "heavy-strike") && HasCommand(aren, "thunder"), "Trocar arma retira seus comandos e preserva as materias da armadura.");
        Assert(aren.Limit.Charge == 17 && oldArmor.Sockets[2] == thunder && oldArmor.Sockets[3] == all, "Troca nao pode apagar Limit nem desmontar o outro equipamento.");
        EquipMateria(party, aren, sword, 0, heavy);
        Assert(HasCommand(aren, "heavy-strike"), "Reequipar materia deve restaurar comando.");
        Assert(!party.TryEquipEquipment(aren, sword, out _) && sword.Sockets[0] == heavy, "Selecionar equipamento atual nao deve desmontar materias.");
        var foreign = new EquipmentLoadout(sword.Definition, "iron-sword");
        Assert(!party.TryEquipEquipment(aren, foreign, out _) && sword.Sockets[0] == heavy, "Clone nao possuido deve falhar mesmo com ID igual, sem alterar materias.");

        Equip(party, bram, Gear(party, "iron-armor"));
        Assert(bram.Defense == 25 && Empty(bram.Armor), "Armadura de ferro aplica +25% somente sobre a defesa do item.");
        MateriaInstance guardian = bram.Weapon.Sockets[0];
        EquipmentLoadout bramWeapon = bram.Weapon;
        Equip(party, bram, sword);
        Assert(bram.Weapon == sword && aren.Weapon == bramWeapon, "Transferir arma entre aliados deve trocar os equipamentos.");
        Assert(Empty(sword) && Empty(bramWeapon) && party.LocationOf(heavy) == "Inventario" && party.LocationOf(guardian) == "Inventario",
            "Troca entre aliados deve devolver as materias dos dois equipamentos ao inventario.");
        Assert(bram.Attack == 23 && !HasCommand(aren, "protect") && !HasCommand(bram, "heavy-strike"), "Classes permitem qualquer arma, com bonus e comandos derivados do estado atual.");
        EquipMateria(party, bram, sword, 1, guardian);
        Assert(party.TryRemoveEquipment(bram, EquipmentSlot.Weapon, out _), "Deve ser possivel ficar sem arma.");
        Assert(bram.Weapon == null && bram.Attack == bram.BaseStats.Attack && Empty(sword), "Remover arma deve deixar atributos base e encaixes vazios.");
        Assert(party.LocationOf(guardian) == "Inventario" && !HasCommand(bram, "protect"), "Remocao deve devolver materias e retirar comandos.");
        Assert(!party.TryRemoveEquipment(bram, EquipmentSlot.Weapon, out _), "Remover espaco vazio deve ser no-op.");
        Assert(!party.TryEquip(bram, sword, 0, guardian, out _), "Nao pode equipar materia em equipamento guardado.");
        Equip(party, bram, sword);
        Assert(Empty(sword), "Reequipar a mesma arma guardada nao deve restaurar materias automaticamente.");

        Equip(party, lia, Gear(party, "runic-staff"));
        Equip(party, lia, Gear(party, "arcane-robe"));
        Assert(lia.Magic == 30 && lia.CreateCombatant().Definition.Magic == 30 && lia.Armor.Sockets.Count == 6, "Cajado e manto devem somar magia e expor tres pares de armadura.");
        EquipMateria(party, lia, lia.Armor, 4, thunder);
        EquipMateria(party, lia, lia.Armor, 5, all);
        bool combo = false;
        foreach (AbilityDefinition command in lia.CreateCombatant().Definition.Abilities)
            if (command.SourceAbilityId == "thunder") combo = command.Targets == TargetRule.AllEnemies && command.Power == 27;
        Assert(combo, "Ultimo par de seis encaixes deve resolver combo e afinidade magica.");
        EquipmentLoadout robe = lia.Armor;
        Assert(party.TryRemoveEquipment(lia, EquipmentSlot.Armor, out _), "Armadura deve poder ser removida.");
        Assert(Empty(robe) && party.LocationOf(thunder) == "Inventario" && party.LocationOf(all) == "Inventario",
            "Remover armadura com seis encaixes devolve todas as materias.");
        Assert(party.TryRemoveEquipment(lia, EquipmentSlot.Weapon, out _), "Arma deve poder ser removida depois da armadura.");
        Assert(lia.CreateCombatant().Definition.Abilities.Count == 3 && lia.Magic == 20, "Personagem sem equipamento deve continuar jogavel com comandos comuns e atributos base.");
        foreach (MateriaInstance materia in party.Materias)
        {
            int count = 0;
            foreach (EquipmentLoadout item in party.Equipments)
                foreach (MateriaInstance socket in item.Sockets) if (socket == materia) count++;
            Assert(count <= 1, "Trocas nao podem duplicar materias.");
        }
    }

    private static CombatantState Fighter(string id, CombatTeam team, int speed, int hp = 100, int mp = 20, int attack = 5)
        => new CombatantState(id, new CombatantDefinition(id, "Teste", hp, mp, attack, 0, 50, speed,
            MateriaResolver.Attack, MateriaResolver.Defend), team);
    private static void Act(BattleSession battle, AbilityDefinition ability, CombatantState target)
        => Assert(battle.TryAct(battle.CurrentActor, ability, target, out string message), "Acao recusada: " + message);
    private static void Use(BattleSession battle, ConsumableDefinition item, CombatantState target)
        => Assert(battle.TryUseItem(battle.CurrentActor, item, target, out string message), "Item recusado: " + message);

    private static void CheckConsumptionAndInvalidActions()
    {
        var inventory = new ItemInventory(new ItemStack(Potion, 2), new ItemStack(Ether, 1), new ItemStack(Antidote, 2));
        var hero = Fighter("hero", CombatTeam.Party, 30);
        var ally = Fighter("ally", CombatTeam.Party, 20);
        var enemy = Fighter("enemy", CombatTeam.Enemy, 10, hp: 1000);
        hero.TakeDamage(30);
        var battle = new BattleSession(inventory, hero, ally, enemy);
        Assert(battle.GetValidItemTargets(Potion).Count == 1 && battle.GetValidItemTargets(Potion)[0] == hero, "Pocao lista apenas aliados vivos e feridos.");
        Assert(!battle.TryUseItem(ally, Potion, hero, out _), "Nao pode usar item fora do turno.");
        var fake = new ConsumableDefinition(Potion.Id, "Falsa", "", ItemEffect.RestoreHp, 999);
        Assert(!battle.TryUseItem(hero, fake, hero, out _), "Item falso com ID igual nao pode substituir a definicao possuida.");
        Assert(!battle.TryUseItem(hero, Potion, Fighter("foreign", CombatTeam.Party, 1), out _), "Item em alvo de outra batalha deve falhar.");
        Assert(!battle.TryUseItem(hero, Potion, enemy, out _), "Itens da equipe nao podem beneficiar inimigos.");
        Assert(inventory.QuantityOf(Potion) == 2 && hero.Hp == 70 && hero.TurnsTaken == 0 && battle.CurrentActor == hero,
            "Acoes invalidas preservam estoque, HP e turno.");
        Use(battle, Potion, hero);
        Assert(hero.Hp == 100 && hero.Mp == 20 && hero.Limit.Charge == 30 && inventory.QuantityOf(Potion) == 1,
            "Pocao respeita HP maximo, nao usa magia, MP ou Limit, e consome uma unidade.");
        Assert(battle.CurrentActor == ally && hero.TurnsTaken == 1, "Uso valido deve consumir um turno.");
        Assert(!battle.TryUseItem(ally, Potion, hero, out _) && inventory.QuantityOf(Potion) == 1, "Pocao em HP cheio nao deve ser gasta.");
        ally.TakeDamage(60);
        Use(battle, Potion, ally);
        Assert(ally.Hp == 80 && inventory.QuantityOf(Potion) == 0, "Pocao recupera exatamente 40 HP sem bonus magico.");
        Assert(!battle.TryUseItem(enemy, Ether, hero, out _) && inventory.QuantityOf(Ether) == 1, "Inimigo nao pode usar o estoque da equipe.");
        Act(battle, MateriaResolver.Defend, enemy);
        Assert(!battle.TryUseItem(hero, Potion, ally, out _) && hero.TurnsTaken == 1, "Item esgotado nao pode consumir novo turno.");
        hero.SpendMp(20);
        Use(battle, Ether, hero);
        Assert(hero.Mp == 15 && inventory.QuantityOf(Ether) == 0 && battle.CurrentActor == ally, "Eter deve recuperar MP e consumir uma unidade e um turno.");
        Assert(!battle.TryUseItem(ally, Antidote, ally, out _) && inventory.QuantityOf(Antidote) == 2, "Antidoto em aliado sem veneno nao deve gastar recursos.");
        ally.PoisonTurns = 2;
        Use(battle, Antidote, ally);
        Assert(ally.PoisonTurns == 0 && ally.Hp == 80 && inventory.QuantityOf(Antidote) == 1, "Antidoto deve limpar veneno antes do dano de fim de turno.");
        Act(battle, MateriaResolver.Defend, enemy);
        Assert(inventory.TryAdd(Potion, 1, out _), "Inventario deve permitir adicionar estoque para entregas seguintes.");
        hero.PoisonTurns = 3;
        Use(battle, Potion, ally);
        Assert(hero.Hp == 95 && hero.PoisonTurns == 2 && hero.Limit.Charge == 35 && ally.Hp == 100,
            "Usar item em outro aliado nao deve evitar veneno nem acumo de Limit ao fim da propria acao.");

        inventory = new ItemInventory(new ItemStack(Ether, 1));
        hero = Fighter("hero", CombatTeam.Party, 30);
        enemy = Fighter("enemy", CombatTeam.Enemy, 1);
        battle = new BattleSession(inventory, hero, enemy);
        Assert(!battle.TryUseItem(hero, Ether, hero, out _) && inventory.QuantityOf(Ether) == 1, "Eter com MP cheio deve ser recusado.");
        hero.SpendMp(1);
        Use(battle, Ether, hero);
        Assert(hero.Mp == 20 && inventory.QuantityOf(Ether) == 0, "Eter nao pode ultrapassar o MP maximo.");
    }

    private static void CheckItemReviveAndOutcomes()
    {
        var inventory = new ItemInventory(new ItemStack(Feather, 1));
        var victim = Fighter("victim", CombatTeam.Party, 10, hp: 101);
        var healer = Fighter("healer", CombatTeam.Party, 20);
        var enemy = Fighter("enemy", CombatTeam.Enemy, 30, hp: 1000, attack: 120);
        var battle = new BattleSession(inventory, victim, healer, enemy);
        Act(battle, MateriaResolver.Attack, victim);
        Assert(victim.Hp == 0 && victim.Limit.IsReady && battle.CurrentActor == healer, "Derrotado deve conservar Limit antes de ressuscitar.");
        Assert(!battle.TryUseItem(healer, Feather, healer, out _) && inventory.QuantityOf(Feather) == 1, "Pluma em aliado vivo deve falhar sem gasto.");
        Assert(battle.GetValidItemTargets(Feather).Count == 1 && battle.GetValidItemTargets(Feather)[0] == victim, "Pluma deve listar somente aliados derrotados.");
        Use(battle, Feather, victim);
        Assert(victim.Hp == 31 && victim.Limit.IsReady && healer.Mp == 20 && inventory.QuantityOf(Feather) == 0,
            "Pluma revive com 30% arredondados para cima, sem gasto de MP ou Limit.");
        Assert(battle.CurrentActor == victim && battle.Round == 1, "Revivido antes de sua posicao deve conservar somente o turno pendente.");
        Act(battle, MateriaResolver.Defend, victim);
        Assert(battle.Round == 2 && victim.TurnsTaken == 1, "Item de ressuscitar nao pode duplicar turnos.");

        // Veneno pode derrotar o usuario depois de usar um item de MP.
        inventory = new ItemInventory(new ItemStack(Ether, 1));
        var fragile = Fighter("fragile", CombatTeam.Party, 30, hp: 20);
        enemy = Fighter("enemy", CombatTeam.Enemy, 1);
        fragile.TakeDamage(19); fragile.SpendMp(10); fragile.PoisonTurns = 1;
        battle = new BattleSession(inventory, fragile, enemy);
        Use(battle, Ether, fragile);
        Assert(battle.Outcome == BattleOutcome.Defeat && inventory.QuantityOf(Ether) == 0 && fragile.Limit.IsReady,
            "Uso valido ainda gasta item quando veneno encerra a batalha depois da acao.");
        Assert(!battle.TryUseItem(fragile, Ether, fragile, out _), "Nao pode usar item depois do resultado.");

        inventory = new ItemInventory(new ItemStack(Potion, 2));
        var winner = Fighter("winner", CombatTeam.Party, 30);
        enemy = Fighter("enemy", CombatTeam.Enemy, 1, hp: 1);
        battle = new BattleSession(inventory, winner, enemy);
        Act(battle, MateriaResolver.Attack, enemy);
        Assert(!battle.TryUseItem(winner, Potion, winner, out _) && inventory.QuantityOf(Potion) == 2,
            "Tela de vitoria nao pode gastar consumiveis.");
    }

    private static void CheckStockPersistenceAndValidation()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        BattleSession battle = PrototypeBattleFactory.CreateEncounter(party);
        CombatantState hero = battle.CurrentActor;
        hero.TakeDamage(10);
        Use(battle, Potion, hero);
        BattleSession next = PrototypeBattleFactory.CreateEncounter(party);
        Assert(next.Inventory.QuantityOf(Potion) == 5 && next.CurrentActor.Hp == 100 && Member(party, "aren").Limit.Charge == 10,
            "Nova batalha deve preservar estoque e Limit, restaurando HP nesta fase.");
        Assert(PrototypePartyFactory.Create().Consumables.QuantityOf(Potion) == 6, "Sessoes separadas devem ter estoque independente.");

        var originalStack = new ItemStack(Potion, 1);
        var inventory = new ItemInventory(originalStack);
        Assert(inventory.TryAdd(Potion, 2, out _) && inventory.QuantityOf(Potion) == 3 && originalStack.Quantity == 1,
            "Inventario deve copiar pilhas iniciais e empilhar por definicao possuida.");
        Assert(!inventory.TryAdd(Potion, -1, out _) && !inventory.TryAdd(Potion, 0, out _), "Quantidades invalidas nao podem alterar estoque.");
        var counterfeit = new ConsumableDefinition(Potion.Id, "Falsa", "", ItemEffect.RestoreHp, 999);
        Assert(!inventory.TryAdd(counterfeit, 1, out _) && inventory.QuantityOf(Potion) == 3, "Mesmo ID com outra definicao deve ser recusado.");
        inventory = new ItemInventory(new ItemStack(Potion, int.MaxValue));
        Assert(!inventory.TryAdd(Potion, 1, out _) && inventory.QuantityOf(Potion) == int.MaxValue, "Overflow nao pode corromper estoque.");

        int rejected = 0;
        try { new ItemStack(Potion, -1); } catch (ArgumentException) { rejected++; }
        try { new ItemInventory(originalStack, originalStack); } catch (ArgumentException) { rejected++; }
        try { new ConsumableDefinition("bad", "", "", ItemEffect.RestoreHp, 0); } catch (ArgumentException) { rejected++; }
        try { new ConsumableDefinition("bad", "", "", ItemEffect.Revive, 101); } catch (ArgumentException) { rejected++; }
        try { new EquipmentDefinition("bad", 0, 0, 3); } catch (ArgumentException) { rejected++; }
        try { new PartyProgress(new[] { Member(party, "aren") }, new MateriaInstance[0], new[] { Member(party, "aren").Weapon }); } catch (ArgumentException) { rejected++; }
        Assert(rejected == 6, "Definicoes e identidades invalidas devem ser recusadas.");
    }

    public static int Run()
    {
        CheckGearAndMateriaReturn();
        CheckConsumptionAndInvalidActions();
        CheckItemReviveAndOutcomes();
        CheckStockPersistenceAndValidation();
        return checks;
    }
}
