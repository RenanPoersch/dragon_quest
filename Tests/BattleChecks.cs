using System;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Prototype;

internal static class BattleChecks
{
    private static int checks;
    private static AbilityDefinition Attack => PrototypeBattleFactory.Attack;
    private static AbilityDefinition Defend => PrototypeBattleFactory.Defend;
    private static AbilityDefinition Heal => PrototypeBattleFactory.Heal;
    private static AbilityDefinition Heavy => PrototypeBattleFactory.HeavyStrike;

    // Encontro original mantido como fixture de regressao das regras da parte 3.
    private static BattleSession CreateBasicEncounter()
    {
        return new BattleSession(
            new CombatantState("aren", new CombatantDefinition("Aren", "Atacante", 100, 20, 22, 7, 4, 14, Attack, Heavy, Defend), CombatTeam.Party),
            new CombatantState("lia", new CombatantDefinition("Lia", "Healer", 80, 36, 9, 5, 20, 12, Attack, Heal, Defend), CombatTeam.Party),
            new CombatantState("bram", new CombatantDefinition("Bram", "Tank", 150, 15, 14, 13, 3, 8, Attack, Defend), CombatTeam.Party),
            new CombatantState("guard", new CombatantDefinition("Guarda", "Inimigo", 150, 0, 22, 8, 0, 9, Attack, Defend), CombatTeam.Enemy));
    }

    private static void Assert(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    private static CombatantState Find(BattleSession battle, string id)
    {
        foreach (CombatantState participant in battle.Combatants)
            if (participant.Id == id) return participant;
        throw new Exception("Combatente ausente: " + id);
    }

    private static void Act(BattleSession battle, AbilityDefinition ability, CombatantState target)
    {
        Assert(battle.TryAct(battle.CurrentActor, ability, target, out string message), "Acao recusada: " + message);
    }

    private static void CheckBasicRound()
    {
        BattleSession battle = CreateBasicEncounter();
        CombatantState guard = Find(battle, "guard");
        CombatantState aren = Find(battle, "aren");
        CombatantState lia = Find(battle, "lia");
        CombatantState bram = Find(battle, "bram");
        Assert(battle.Round == 1 && battle.CurrentActor == aren, "Velocidade deve colocar Aren primeiro.");
        Assert(!battle.TryAct(lia, Attack, guard, out _), "Personagem fora do turno nao pode agir.");
        Assert(guard.Hp == 150 && battle.CurrentActor == aren, "Tentativa invalida nao pode causar dano ou consumir turno.");
        Act(battle, Attack, guard);
        Assert(guard.Hp == 136 && battle.CurrentActor == lia, "Ataque deve aplicar defesa e passar turno.");
        Assert(battle.GetValidTargets(Heal).Count == 0, "Aliados com HP cheio nao devem ser alvos de cura.");
        Assert(!battle.TryAct(lia, Heal, guard, out _), "Cura em inimigo deve ser recusada.");
        Assert(lia.Mp == 36 && battle.CurrentActor == lia, "Cura invalida nao pode consumir MP ou turno.");
        Assert(!EnemyAI.TryTakeTurn(battle, out _), "IA nao pode agir no turno do jogador.");
        Act(battle, Defend, lia);
        Assert(battle.CurrentActor == guard, "Guarda deve agir antes do tank.");
        Assert(EnemyAI.TryTakeTurn(battle, 0, out _), "IA deve realizar uma acao valida.");
        Assert(aren.Hp == 70 && battle.CurrentActor == bram, "IA deve atacar aliado vivo com dano dobrado e avancar.");
        Act(battle, Defend, bram);
        Assert(battle.Round == 2 && battle.CurrentActor == aren, "Fim da ordem deve iniciar nova rodada.");
    }

    private static void CheckResourcesAndTargets()
    {
        BattleSession battle = CreateBasicEncounter();
        CombatantState guard = Find(battle, "guard");
        CombatantState aren = Find(battle, "aren");
        Act(battle, Heavy, guard);
        Assert(aren.Mp == 16 && guard.Hp == 122, "Golpe forte deve consumir 4 MP e causar 28 de dano.");

        var limited = new CombatantState("hero", new CombatantDefinition("Heroi", "Atacante", 100, 3, 10, 0, 0, 20, Attack, Heavy), CombatTeam.Party);
        var opponent = new CombatantState("enemy", new CombatantDefinition("Inimigo", "Inimigo", 100, 0, 1, 0, 0, 1, Attack), CombatTeam.Enemy);
        battle = new BattleSession(limited, opponent);
        Assert(!battle.TryAct(limited, Heavy, opponent, out _), "MP insuficiente deve impedir a habilidade.");
        Assert(limited.Mp == 3 && opponent.Hp == 100 && battle.CurrentActor == limited, "Falha por MP nao pode alterar estado.");
        CombatantState foreign = Find(CreateBasicEncounter(), "guard");
        Assert(!battle.TryAct(limited, Attack, foreign, out _), "Alvo de outra batalha deve ser recusado.");
        var counterfeit = new AbilityDefinition("attack", "Ataque falso", "", 0, 999, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
        Assert(!battle.TryAct(limited, counterfeit, opponent, out _), "Habilidade nao aprendida deve ser recusada mesmo com ID igual.");
        Assert(!battle.TryAct(limited, Attack, limited, out _), "Ataque nao pode atingir um aliado.");
        Assert(opponent.Hp == 100 && limited.Mp == 3, "Alvos ou habilidades invalidos nao podem consumir recursos.");
    }

    private static void CheckHealingAndDefense()
    {
        BattleSession battle = CreateBasicEncounter();
        CombatantState aren = Find(battle, "aren");
        CombatantState lia = Find(battle, "lia");
        Act(battle, Defend, aren);
        Act(battle, Attack, Find(battle, "guard"));
        EnemyAI.TryTakeTurn(battle, 0, out _);
        Assert(aren.Hp == 85, "Defesa deve reduzir 30 de dano inimigo para 15.");
        Act(battle, Defend, Find(battle, "bram"));
        Assert(!aren.IsDefending, "Defesa deve expirar no inicio do proximo turno do usuario.");
        Act(battle, Attack, Find(battle, "guard"));
        Act(battle, Heal, aren);
        Assert(aren.Hp == 100 && lia.Mp == 30, "Cura deve limitar HP ao maximo e consumir MP.");

        var victim = new CombatantState("victim", new CombatantDefinition("Alvo", "Aliado", 1, 0, 1, 0, 0, 1, Attack), CombatTeam.Party);
        var healer = new CombatantState("healer", new CombatantDefinition("Healer", "Healer", 100, 36, 1, 0, 20, 20, Heal, Defend), CombatTeam.Party);
        var enemy = new CombatantState("enemy", new CombatantDefinition("Inimigo", "Inimigo", 100, 0, 10, 0, 0, 10, Attack), CombatTeam.Enemy);
        battle = new BattleSession(victim, healer, enemy);
        Act(battle, Defend, healer);
        EnemyAI.TryTakeTurn(battle, 0, out _);
        Assert(victim.Hp == 0 && battle.CurrentActor == healer && battle.Round == 2, "Turno de derrotado deve ser pulado.");
        Assert(!battle.TryAct(healer, Heal, victim, out _) && healer.Mp == 36, "Cura comum nao pode reviver nem gastar MP em derrotado.");
        Assert(battle.Outcome == BattleOutcome.None, "Um aliado derrotado nao significa derrota da equipe.");
    }

    private static void CheckOutcomeAndIndependentStates()
    {
        var shared = new CombatantDefinition("Heroi", "Aliado", 20, 0, 50, 0, 0, 20, Attack);
        var one = new CombatantState("z", shared, CombatTeam.Party);
        var two = new CombatantState("a", shared, CombatTeam.Party);
        var enemy = new CombatantState("enemy", new CombatantDefinition("Inimigo", "Inimigo", 20, 0, 1, 0, 0, 1, Attack), CombatTeam.Enemy);
        var battle = new BattleSession(one, two, enemy);
        Assert(battle.CurrentActor == two, "Empates de velocidade devem usar ID ordinal.");
        Act(battle, Attack, enemy);
        Assert(battle.Outcome == BattleOutcome.Victory && enemy.Hp == 0 && battle.CurrentActor == null, "Ultimo inimigo derrotado deve encerrar a batalha.");
        Assert(!battle.TryAct(two, Attack, enemy, out _), "Nao pode agir apos o resultado.");
        var challenger = new CombatantState("challenger", new CombatantDefinition("Outro inimigo", "Inimigo", 100, 0, 3, 0, 0, 30, Attack), CombatTeam.Enemy);
        var independent = new BattleSession(one, two, challenger);
        Assert(EnemyAI.TryTakeTurn(independent, 0, out _), "IA deve atacar uma das instancias compartilhadas.");
        Assert(one.Hp == 14 && two.Hp == 20 && shared.MaxHp == 20, "Dano deve alterar somente a instancia, preservando outra instancia e a definicao.");

        var fragile = new CombatantState("hero", new CombatantDefinition("Heroi", "Aliado", 1, 0, 1, 0, 0, 1, Attack), CombatTeam.Party);
        var strong = new CombatantState("enemy", new CombatantDefinition("Inimigo", "Inimigo", 100, 0, 99, 0, 0, 20, Attack), CombatTeam.Enemy);
        battle = new BattleSession(fragile, strong);
        EnemyAI.TryTakeTurn(battle, out _);
        Assert(battle.Outcome == BattleOutcome.Defeat && fragile.Hp == 0 && battle.CurrentActor == null, "Ultimo aliado derrotado deve encerrar com derrota.");
        Assert(!EnemyAI.TryTakeTurn(battle, out _), "IA nao pode agir apos a batalha.");
        bool rejected = false;
        try { new BattleSession(one, one, challenger); } catch (ArgumentException) { rejected = true; }
        Assert(rejected, "Batalha deve recusar participantes repetidos.");
    }

    private static void CheckFullEncounters()
    {
        for (int strategy = 0; strategy < 2; strategy++)
        {
            BattleSession battle = CreateBasicEncounter();
            int actions = 0;
            while (battle.Outcome == BattleOutcome.None && actions++ < 500)
            {
                CombatantState actor = battle.CurrentActor;
                if (actor.Team == CombatTeam.Enemy)
                {
                    Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve progredir durante uma partida completa.");
                    continue;
                }
                AbilityDefinition ability = strategy == 1 ? Defend : Attack;
                if (strategy == 0 && actor.Id == "aren" && actor.Mp >= Heavy.MpCost) ability = Heavy;
                if (strategy == 0 && actor.Id == "lia" && actor.Mp >= Heal.MpCost && battle.GetValidTargets(Heal).Count > 0) ability = Heal;
                CombatantState target = ability.Targets == TargetRule.Self ? actor : battle.GetValidTargets(ability)[0];
                Act(battle, ability, target);
            }
            Assert(actions < 500, "Batalha nao pode ficar presa na ordem de turnos.");
            Assert(battle.Outcome == (strategy == 0 ? BattleOutcome.Victory : BattleOutcome.Defeat), "Estrategias completas devem permitir vitoria e derrota.");
        }
        BattleSession fresh = CreateBasicEncounter();
        Assert(Find(fresh, "aren").Hp == 100 && Find(fresh, "aren").Mp == 20, "Nova batalha deve iniciar com estado independente.");
    }

    private static void CheckVitalFeedback()
    {
        BattleSession battle = CreateBasicEncounter();
        CombatantState guard = Find(battle, "guard");
        Act(battle, Attack, guard);
        Assert(guard.VitalChanges.Count == 1 && guard.VitalChanges[0].Amount == -14,
            "Feedback deve registrar dano real no alvo atingido.");
        Assert(!guard.VitalChanges[0].IsCritical, "Dano comum nao pode ser apresentado como critico.");
        guard.TakeDamage(5, critical: true);
        Assert(guard.VitalChanges[1].IsCritical && guard.VitalChanges[1].Amount == -5,
            "Golpe marcado como critico deve preservar sua identificacao.");
        guard.RecoverHp(999);
        Assert(guard.VitalChanges[2].Amount == 19, "Cura flutuante deve respeitar o limite real de HP.");
        guard.RecoverHp(10);
        Assert(guard.VitalChanges.Count == 3, "Cura sem efeito nao deve produzir numeros.");
        CombatantState lia = Find(battle, "lia");
        lia.SpendMp(6); lia.RecoverMp(99);
        Assert(lia.VitalChanges.Count == 1 && lia.VitalChanges[0].IsMp && lia.VitalChanges[0].Amount == 6,
            "Restauracao de MP deve indicar o recurso e o valor real.");
        for (int i = 0; i < 100; i++) { guard.TakeDamage(1); guard.RecoverHp(1); }
        Assert(guard.VitalChanges.Count == 64 && guard.VitalChanges[63].Sequence == 203,
            "Historico visual deve ser limitado sem repetir a sequencia de efeitos.");
    }

    private static void CheckCommandMenu()
    {
        var bare = new CombatantState("hero", new CombatantDefinition("Heroi", "", 100, 20, 20, 5, 5, 10,
            MateriaResolver.Attack, MateriaResolver.Defend, MateriaResolver.LimitBreak), CombatTeam.Party);
        var root = BattleCommandMenu.Root(bare);
        Assert(root.Count == 2 && root[0].Ability == MateriaResolver.Attack && root[1].Kind == BattleCommandKind.Items,
            "Sem materias, menu principal deve mostrar Ataque e Itens; Defender substitui a primeira linha ao pressionar esquerda.");
        Assert(BattleCommandMenu.Defend(bare) == MateriaResolver.Defend,
            "A primeira linha deve usar a habilidade Defender do proprio personagem.");
        bare.Limit.Restore(99);
        Assert(BattleCommandMenu.Root(bare)[0].Ability == MateriaResolver.Attack, "Limit incompleto deve manter Ataque.");
        bare.Limit.Restore(100);
        root = BattleCommandMenu.Root(bare);
        Assert(root.Count == 2 && root[0].Label == "LIMIT" && root[0].Ability == MateriaResolver.LimitBreak,
            "Limit pronto deve substituir Ataque, sem acrescentar outra linha.");
        foreach (var command in root)
            Assert(command.Ability != MateriaResolver.Attack && command.Ability != MateriaResolver.Defend,
                "Limit pronto nao deve duplicar Ataque nem trazer Defender para a lista principal.");
        Assert(BattleCommandMenu.Defend(bare) == MateriaResolver.Defend && bare.Limit.IsReady,
            "Defender continua disponivel com Limit pronto e consultar o menu nao gasta a carga.");
        var noDefense = new CombatantState("no-defense", new CombatantDefinition("Heroi", "", 100, 20, 20, 5, 5, 10,
            MateriaResolver.Attack), CombatTeam.Party);
        Assert(BattleCommandMenu.Defend(noDefense) == null, "A primeira linha nao deve oferecer uma defesa nao aprendida.");
        bare.Limit.Spend();
        Assert(BattleCommandMenu.Root(bare)[0].Ability == MateriaResolver.Attack, "Gastar Limit deve devolver Ataque ao menu.");

        var party = PrototypePartyFactory.CreateTestParty();
        var lia = party.Members[1].CreateCombatant();
        root = BattleCommandMenu.Root(lia);
        int magic = 0, summon = 0;
        foreach (var command in root)
        {
            if (command.Kind == BattleCommandKind.Magic) magic++;
            if (command.Kind == BattleCommandKind.Summon) summon++;
            Assert(command.Ability == null || command.Ability.MenuGroup == AbilityMenuGroup.Direct,
                "Magias e invocacoes devem sair da lista principal de acoes.");
        }
        Assert(magic == 1 && summon == 1, "Magic e Summon devem aparecer uma vez para quem possui ambos.");
        Assert(BattleCommandMenu.Group(lia, AbilityMenuGroup.Magic).Count == 3,
            "Magic deve agrupar Curar, Purificar e Reviver.");
        Assert(BattleCommandMenu.Group(lia, AbilityMenuGroup.Summon).Count == 1,
            "Summon deve conter somente a invocacao equipada.");
        lia.SpendMp(lia.Mp);
        Assert(BattleCommandMenu.Root(lia).Count == root.Count, "Falta de MP nao deve esconder categorias de habilidades existentes.");

        MateriaInstance infuse = null;
        foreach (var materia in party.Materias) if (materia.Definition == PrototypePartyFactory.InfuseMateria) infuse = materia;
        var aren = party.Members[0];
        Assert(party.TryEquip(aren, aren.Armor, 3, infuse, out _), "Fixture deve equipar Infusao ligada a Thunder.");
        var spells = BattleCommandMenu.Group(aren.CreateCombatant(), AbilityMenuGroup.Magic);
        Assert(spells.Count == 1 && spells[0].Effect == AbilityEffect.MagicDamage,
            "Infusao deve preservar Thunder como magia no menu Magic.");
    }

    private static void CheckMenuScrolling()
    {
        foreach (float height in new[] { 120f, 88f })
        {
            float scroll = 0;
            for (int step = 0; step < 12; step++)
            {
                int index = step % 6;
                scroll = BattleCommandMenu.KeepRowVisible(scroll, index, 6, height);
                Assert(index * 36 >= scroll && index * 36 + 34 <= scroll + height,
                    "Navegar para baixo e voltar ao inicio deve manter toda a linha e o cursor visiveis.");
            }
            for (int index = 5; index >= 0; index--)
            {
                scroll = BattleCommandMenu.KeepRowVisible(scroll, index, 6, height);
                Assert(index * 36 >= scroll && index * 36 + 34 <= scroll + height,
                    "Navegar para cima deve manter toda a linha selecionada visivel.");
            }
        }
        Assert(BattleCommandMenu.KeepRowVisible(0, 3, 5, 120) == 22,
            "Quarta linha deve rolar antes que o cursor seja cortado na borda inferior.");
        Assert(BattleCommandMenu.KeepRowVisible(50, 0, 1, 88) == 0,
            "Alvo de grupo em uma unica linha nao deve rolar para uma selecao invisivel.");
    }

    public static int Main()
    {
        try
        {
            CheckMenuScrolling();
            CheckCommandMenu();
            CheckVitalFeedback();
            CheckBasicRound();
            CheckResourcesAndTargets();
            CheckHealingAndDefense();
            CheckOutcomeAndIndependentStates();
            CheckFullEncounters();
            checks += AbilityChecks.Run();
            checks += MateriaLimitChecks.Run();
            checks += EquipmentItemChecks.Run();
            checks += ElementalChecks.Run();
            checks += ShopChecks.Run();
            checks += ProgressionChecks.Run();
            checks += DragonChecks.Run();
            checks += CastleChecks.Run();
            Console.WriteLine(checks + " verificacoes de combate passaram.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.ToString());
            return 1;
        }
    }
}
