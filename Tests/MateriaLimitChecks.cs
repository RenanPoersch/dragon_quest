using System;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Prototype;

internal static class MateriaLimitChecks
{
    private static int checks;
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
    private static MateriaInstance Materia(PartyProgress party, string id)
    {
        foreach (MateriaInstance materia in party.Materias) if (materia.Definition.Id == id) return materia;
        throw new Exception("Materia ausente: " + id);
    }
    private static AbilityDefinition Command(CombatantState actor, string sourceId)
    {
        foreach (AbilityDefinition command in actor.Definition.Abilities)
            if (command.SourceAbilityId == sourceId) return command;
        throw new Exception("Comando ausente: " + sourceId);
    }
    private static bool HasCommand(CombatantState actor, string sourceId)
    {
        foreach (AbilityDefinition command in actor.Definition.Abilities)
            if (command.SourceAbilityId == sourceId) return true;
        return false;
    }
    private static void Equip(PartyProgress party, PartyMember member, EquipmentLoadout equipment, int slot, MateriaInstance materia)
    {
        Assert(party.TryEquip(member, equipment, slot, materia, out string message), "Equipar falhou: " + message);
    }
    private static void Remove(PartyProgress party, PartyMember member, EquipmentLoadout equipment, int slot)
    {
        Assert(party.TryUnequip(member, equipment, slot, out string message), "Remover falhou: " + message);
    }
    private static void Act(BattleSession battle, AbilityDefinition ability, CombatantState target)
    {
        Assert(battle.TryAct(battle.CurrentActor, ability, target, out string message), "Acao falhou: " + message);
    }
    private static CombatantState Enemy(string id, int hp = 1000, int speed = 1, int attack = 10)
    {
        return new CombatantState(id, new CombatantDefinition(id, "Inimigo", hp, 0, attack, 0, 0, speed,
            MateriaResolver.Attack, MateriaResolver.Defend), CombatTeam.Enemy);
    }

    private static void CheckOwnershipAndFreeUse()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        PartyMember aren = Member(party, "aren"), lia = Member(party, "lia"), bram = Member(party, "bram");
        MateriaInstance cure = Materia(party, "cure"), guardian = Materia(party, "guardian");
        Assert(!HasCommand(aren.CreateCombatant(), "heal"), "Atacante nao possui cura sem a materia.");
        Equip(party, aren, aren.Weapon, 1, cure);
        Assert(HasCommand(aren.CreateCombatant(), "heal") && !HasCommand(lia.CreateCombatant(), "heal"), "Transferencia deve conceder cura ao atacante e retirar da healer.");
        Assert(lia.Class == CharacterClass.Healer && aren.Class == CharacterClass.Attacker, "Transferencia nao altera a classe.");
        Equip(party, lia, lia.Weapon, 0, guardian);
        Assert(HasCommand(lia.CreateCombatant(), "taunt") && HasCommand(lia.CreateCombatant(), "protect"), "Healer deve poder usar comandos da materia fisica Guardiao.");
        Assert(!HasCommand(bram.CreateCombatant(), "protect"), "Tank sem Guardiao nao conserva Proteger.");
        Remove(party, aren, aren.Weapon, 1);
        Assert(!HasCommand(aren.CreateCombatant(), "heal") && party.LocationOf(cure) == "Inventario", "Remover deve retornar o item e retirar o comando.");
        Equip(party, bram, bram.Weapon, 1, cure);
        Assert(HasCommand(bram.CreateCombatant(), "heal"), "Tank tambem pode equipar cura.");

        MateriaInstance first = aren.Weapon.Sockets[0], second = bram.Weapon.Sockets[1];
        Equip(party, aren, aren.Weapon, 0, second);
        Assert(aren.Weapon.Sockets[0] == second && bram.Weapon.Sockets[1] == first, "Troca entre encaixes deve preservar as duas materias.");
        Assert(!party.TryEquip(aren, aren.Weapon, 0, second, out _), "Equipar na mesma posicao deve ser no-op.");
        Assert(!party.TryEquip(aren, lia.Armor, 0, first, out _), "Equipamento de outro membro nao e destino valido.");
        Assert(!party.TryEquip(aren, aren.Weapon, 99, first, out _), "Indice invalido deve falhar.");
        var foreign = new MateriaInstance("foreign", PrototypePartyFactory.CureMateria);
        Assert(!party.TryEquip(aren, aren.Weapon, 1, foreign, out _), "Item nao possuido deve ser recusado.");
        Assert(aren.Weapon.Sockets[0] == second && bram.Weapon.Sockets[1] == first, "Falhas de validacao nao podem alterar inventario.");
        foreach (MateriaInstance materia in party.Materias)
        {
            int count = 0;
            foreach (PartyMember member in party.Members)
                foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor })
                    foreach (MateriaInstance socket in equipment.Sockets) if (socket == materia) count++;
            Assert(count <= 1, "Cada instancia deve ocupar no maximo um encaixe.");
        }

        foreach (PartyMember member in party.Members)
            foreach (EquipmentLoadout equipment in new[] { member.Weapon, member.Armor })
                for (int i = 0; i < equipment.Sockets.Count; i++)
                    if (equipment.Sockets[i] != null) Remove(party, member, equipment, i);
        Assert(aren.CreateCombatant().Definition.Abilities.Count == 3 && lia.CreateCombatant().Definition.Abilities.Count == 3,
            "Sem materias, todas as classes conservam apenas Ataque, Defender e Limit Break.");
    }

    private static void CheckFourComboTypes()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        PartyMember aren = Member(party, "aren"), bram = Member(party, "bram");
        MateriaInstance thunder = Materia(party, "thunder"), all = Materia(party, "all"), summon = Materia(party, "phoenix"), infuse = Materia(party, "infuse");
        CombatantState hero = aren.CreateCombatant();
        AbilityDefinition thunderAll = Command(hero, "thunder");
        Assert(thunderAll.Targets == TargetRule.AllEnemies && thunderAll.MpCost == 9 && thunderAll.Effect == AbilityEffect.MagicDamage,
            "Verde + azul deve produzir Thunder multi-alvo a 9 MP.");
        CombatantState one = Enemy("one"), two = Enemy("two");
        var battle = new BattleSession(hero, one, two);
        Act(battle, thunderAll, one);
        Assert(one.Hp == 974 && two.Hp == 974 && hero.Mp == 19, "Thunder + All deve aplicar magia em ambos e cobrar uma vez.");

        Equip(party, aren, aren.Armor, 3, infuse);
        hero = aren.CreateCombatant();
        AbilityDefinition thunderPhysical = Command(hero, "thunder");
        Assert(thunderPhysical.Effect == AbilityEffect.PhysicalDamage && thunderPhysical.Targets == TargetRule.Enemy && thunderPhysical.MpCost == 6,
            "Verde + roxa deve produzir golpe fisico individual, mantendo custo original.");
        one = Enemy("one"); two = Enemy("two");
        battle = new BattleSession(hero, one, two);
        Act(battle, thunderPhysical, one);
        Assert(one.Hp == 950 && two.Hp == 1000, "Thunder + Infusao deve usar ataque do portador em um alvo.");

        Equip(party, bram, bram.Armor, 0, summon);
        Equip(party, bram, bram.Armor, 1, all);
        hero = bram.CreateCombatant();
        AbilityDefinition summonAll = Command(hero, "summon");
        Assert(summonAll.Targets == TargetRule.AllEnemies && summonAll.MpCost == 18 && summonAll.Effect == AbilityEffect.MagicDamage,
            "Vermelha + azul deve modificar a invocacao.");
        one = Enemy("one"); two = Enemy("two");
        battle = new BattleSession(hero, one, two);
        Act(battle, summonAll, one);
        Assert(one.Hp == 962 && two.Hp == 962 && hero.Mp == 12, "Summon + All deve atingir ambos com custo unico.");

        Equip(party, bram, bram.Armor, 1, infuse);
        hero = bram.CreateCombatant();
        AbilityDefinition summonPhysical = Command(hero, "summon");
        Assert(summonPhysical.Effect == AbilityEffect.PhysicalDamage && summonPhysical.Targets == TargetRule.Enemy, "Vermelha + roxa deve infundir a invocacao no ataque.");
        one = Enemy("one"); battle = new BattleSession(hero, one);
        Act(battle, summonPhysical, one);
        Assert(one.Hp == 946 && hero.Mp == 18, "Summon infundida deve usar ataque do tank e custo original.");

        // Nao atravessa outro par nem conecta arma e armadura.
        Remove(party, bram, bram.Armor, 1);
        Equip(party, bram, bram.Weapon, 1, all);
        Assert(Command(bram.CreateCombatant(), "summon").Targets == TargetRule.Enemy, "Suporte em outro equipamento nao deve conectar.");
        Equip(party, bram, bram.Armor, 2, all);
        Assert(Command(bram.CreateCombatant(), "summon").Targets == TargetRule.Enemy, "Suporte no outro par da armadura nao deve conectar.");
        Equip(party, bram, bram.Armor, 1, all);
        Assert(Command(bram.CreateCombatant(), "summon").Targets == TargetRule.AllEnemies, "Suporte no par certo deve conectar.");

        MateriaInstance heavy = Materia(party, "heavy");
        Equip(party, bram, bram.Armor, 0, heavy);
        Assert(Command(bram.CreateCombatant(), "heavy-strike").Targets == TargetRule.Enemy, "Azul + roxa nao deve aplicar All a comando fisico.");
        Assert(bram.Armor.LinkedSocket(0) == 1 && bram.Armor.LinkedSocket(3) == 2, "Ligacoes devem ser simetricas por par.");
        Assert(!HasCommand(bram.CreateCombatant(), "all"), "Suporte All nao concede um comando independente.");
        party = PrototypePartyFactory.Create(); aren = Member(party, "aren");
        Equip(party, aren, aren.Armor, 2, aren.Armor.Sockets[3]);
        Assert(Command(aren.CreateCombatant(), "thunder").Targets == TargetRule.AllEnemies, "Combo deve funcionar com o suporte antes da magia no par.");
    }

    private static void CheckHealingCombosAndAffinity()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        PartyMember aren = Member(party, "aren"), lia = Member(party, "lia"), bram = Member(party, "bram");
        MateriaInstance cure = Materia(party, "cure"), all = Materia(party, "all"), infuse = Materia(party, "infuse");
        Equip(party, lia, lia.Weapon, 1, all);
        CombatantState healer = lia.CreateCombatant();
        AbilityDefinition healAll = Command(healer, "heal");
        Assert(healAll.Targets == TargetRule.AllAllies && healAll.MpCost == 9 && healAll.Power == 22,
            "Cura + All deve manter bonus da healer sobre o poder base, arredondado para baixo.");
        var attacker = aren.CreateCombatant(); var tank = bram.CreateCombatant(); var enemy = Enemy("enemy");
        attacker.TakeDamage(50); tank.TakeDamage(80);
        var battle = new BattleSession(attacker, healer, tank, enemy);
        Act(battle, MateriaResolver.Defend, attacker);
        Assert(battle.GetValidTargets(healAll).Count == 2, "Cura em area deve excluir aliado com HP cheio.");
        Act(battle, healAll, attacker);
        Assert(attacker.Hp == 92 && tank.Hp == 112 && healer.Hp == 80 && healer.Mp == 51,
            "Cura + All aplica o mesmo poder a cada aliado ferido com custo unico.");
        Equip(party, lia, lia.Weapon, 1, infuse);
        Assert(Command(lia.CreateCombatant(), "heal").Effect == AbilityEffect.Heal && Command(lia.CreateCombatant(), "heal").Targets == TargetRule.Ally,
            "Infusao incompativel com cura deve deixar a habilidade original.");
        Equip(party, aren, aren.Weapon, 1, cure);
        Assert(Command(aren.CreateCombatant(), "heal").Power == 18, "Atacante pode curar sem bonus magico da healer.");
        Assert(aren.Attack == 28 && lia.Attack == 14 && bram.Attack == 19, "Afinidade fisica modifica somente o ataque base da arma de Aren.");
        Assert(aren.Defense == 13 && lia.Defense == 11 && bram.Defense == 20, "Afinidade do tank modifica somente a defesa base da armadura.");

        // Duas materias iguais podem gerar comandos distintos sem IDs duplicados.
        MateriaInstance firstThunder = Materia(party, "thunder"), secondThunder = null;
        foreach (MateriaInstance materia in party.Materias)
            if (materia != firstThunder && materia.Definition == firstThunder.Definition) secondThunder = materia;
        Equip(party, aren, aren.Weapon, 0, secondThunder);
        Equip(party, aren, aren.Armor, 3, all);
        var duplicate = aren.CreateCombatant();
        int thunderCount = 0;
        foreach (AbilityDefinition command in duplicate.Definition.Abilities) if (command.SourceAbilityId == "thunder") thunderCount++;
        Assert(thunderCount == 2, "Copias distintas permitem comandos distintos e IDs unicos.");
        Assert(firstThunder.Definition.Abilities[0].Targets == TargetRule.Enemy && firstThunder.Definition.Abilities[0].MpCost == 6,
            "Resolver combos nao pode alterar a definicao compartilhada.");
        bool single = false, multi = false;
        foreach (AbilityDefinition command in duplicate.Definition.Abilities)
            if (command.SourceAbilityId == "thunder") { single |= command.Targets == TargetRule.Enemy; multi |= command.Targets == TargetRule.AllEnemies; }
        Assert(single && multi, "Duas copias devem preservar Thunder individual e Thunder + All separadamente.");
    }

    private static void CheckCarryAfterCompletedEncounter()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        BattleSession battle = PrototypeBattleFactory.CreateEncounter(party);
        int turns = 0;
        while (battle.Outcome == BattleOutcome.None && turns++ < 600)
        {
            if (battle.CurrentActor.Team == CombatTeam.Enemy)
                Assert(EnemyAI.TryTakeTurn(battle, out _), "Encontro de persistencia deve avancar os inimigos.");
            else Act(battle, Command(battle.CurrentActor, "defend"), battle.CurrentActor);
        }
        Assert(battle.Outcome == BattleOutcome.Defeat && turns < 600, "Encontro deve chegar ao resultado para testar carry over real.");
        BattleSession next = PrototypeBattleFactory.CreateEncounter(party);
        foreach (CombatantState actor in next.Combatants)
        {
            if (actor.Team == CombatTeam.Enemy) continue;
            Assert(actor.Hp == actor.Definition.MaxHp && actor.PoisonTurns == 0 && actor.Limit.IsReady,
                "Encontro seguinte deve restaurar estado de batalha e conservar as barras cheias apos derrota.");
        }
    }

    private static void CheckMagicEnemyAndTaunt()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        CombatantState tank = Member(party, "bram").CreateCombatant(), ally = Member(party, "lia").CreateCombatant();
        var mage = new CombatantState("mage", new CombatantDefinition("Mago", "Inimigo", 100, 60, 0, 0, 20, 1,
            PrototypePartyFactory.Thunder), CombatTeam.Enemy);
        var battle = new BattleSession(tank, ally, mage);
        Act(battle, MateriaResolver.Defend, ally);
        Act(battle, Command(tank, "taunt"), mage);
        Assert(EnemyAI.ChooseAbility(battle) == PrototypePartyFactory.Thunder, "IA deve reconhecer dano magico como ofensiva.");
        Assert(battle.GetValidTargets(PrototypePartyFactory.Thunder).Count == 1 && battle.GetValidTargets(PrototypePartyFactory.Thunder)[0] == tank,
            "Provocacao tambem deve atrair magia individual.");
        Assert(EnemyAI.TryTakeTurn(battle, out _), "Inimigo mago deve executar sua acao.");
        Assert(tank.Hp == 128 && ally.Hp == 80 && tank.Limit.Charge == 15, "Magia deve usar atributo magico, defesa do tank e carregar Limit real.");
    }

    private static void CheckLimitCarryAndSpending()
    {
        PartyProgress party = PrototypePartyFactory.Create();
        PartyMember member = Member(party, "aren");
        var hero = member.CreateCombatant(); var enemy = Enemy("enemy", attack: 25);
        var battle = new BattleSession(hero, enemy);
        AbilityDefinition limit = Command(hero, "limit-break");
        AbilityDefinition oldMateriaCommand = Command(hero, "heavy-strike");
        Assert(!battle.TryAct(hero, limit, enemy, out _) && hero.Limit.Charge == 0 && hero.TurnsTaken == 0,
            "Limit incompleto nao pode consumir turno, MP ou carga.");
        Act(battle, MateriaResolver.Defend, hero);
        Act(battle, MateriaResolver.Attack, hero);
        Assert(hero.Hp == 94 && member.Limit.Charge == 6, "Limit deve usar dano real apos armadura e defender.");
        hero.TakeDamage(44);
        Assert(member.Limit.Charge == 50, "Dano deve somar carga proporcional ao HP maximo.");
        var next = member.CreateCombatant();
        Assert(next.Hp == 100 && next.Mp == 28 && next.Limit.Charge == 50, "Nova batalha restaura HP/MP e preserva Limit do mesmo membro.");
        Assert(Member(party, "lia").Limit.Charge == 0, "Carga deve ser individual.");
        next.TakeDamage(10000);
        Assert(next.Hp == 0 && member.Limit.Charge == 100, "Overkill deve considerar HP realmente perdido e limitar carga a 100.");
        next = member.CreateCombatant(); enemy = Enemy("enemy");
        battle = new BattleSession(next, enemy);
        var foreign = Enemy("foreign");
        Assert(!battle.TryAct(next, oldMateriaCommand, enemy, out _), "Comando de materia de snapshot anterior nao pode ser aceito automaticamente.");
        // O comando basico Limit e compartilhado; o alvo estrangeiro permanece invalido.
        limit = Command(next, "limit-break");
        Assert(!battle.TryAct(next, limit, foreign, out _) && next.Limit.Charge == 100, "Alvo invalido nao pode gastar Limit cheio.");
        int previousMp = next.Mp;
        Act(battle, limit, enemy);
        Assert(next.Limit.Charge == 0 && next.Mp == previousMp && enemy.Hp == 937, "Limit deve consumir toda a carga, sem MP, e aplicar o golpe.");
        Assert(member.CreateCombatant().Limit.Charge == 0, "Limit gasto deve permanecer gasto no proximo encontro.");
        Assert(!battle.TryAct(battle.CurrentActor, limit, next, out _), "Outro combatente nao recebe comandos nem carga da equipe.");
        Assert(Member(PrototypePartyFactory.Create(), "aren").Limit.Charge == 0, "Uma nova sessao deve ter barras independentes.");

        // Veneno conta; a barra nao e limpa por cura ou ressuscitar.
        hero = member.CreateCombatant(); hero.PoisonTurns = 1;
        enemy = Enemy("enemy"); battle = new BattleSession(hero, enemy);
        Act(battle, MateriaResolver.Defend, hero);
        Assert(member.Limit.Charge == 5, "Veneno deve acumular Limit sobre HP perdido.");
        hero.RecoverHp(100);
        Assert(member.Limit.Charge == 5, "Cura nao deve esvaziar Limit.");
        hero.TakeDamage(100);
        Assert(hero.Limit.Charge == 100, "Morte deve conservar a carga acumulada.");
        hero.ClearConditions(); hero.RecoverHp(30);
        Assert(hero.Limit.Charge == 100, "Reviver nao deve apagar a barra.");

        // Dano interceptado carrega o protetor, nunca o aliado poupado.
        party = PrototypePartyFactory.Create();
        var tank = Member(party, "bram").CreateCombatant(); hero = Member(party, "lia").CreateCombatant();
        enemy = Enemy("enemy", speed: 1, attack: 40);
        battle = new BattleSession(tank, hero, enemy);
        Act(battle, MateriaResolver.Defend, hero);
        Act(battle, Command(tank, "protect"), hero);
        Act(battle, MateriaResolver.Attack, hero);
        Assert(tank.Limit.Charge == 14 && hero.Limit.Charge == 0 && hero.Hp == 80,
            "Protecao deve carregar somente quem recebe os 20 de dano reais, 14% de 150 HP.");
    }

    public static int Run()
    {
        CheckOwnershipAndFreeUse();
        CheckFourComboTypes();
        CheckHealingCombosAndAffinity();
        CheckLimitCarryAndSpending();
        CheckCarryAfterCompletedEncounter();
        CheckMagicEnemyAndTaunt();
        return checks;
    }
}
