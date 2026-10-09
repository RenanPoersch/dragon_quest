using System;
using DragonQuest.Combat;
using DragonQuest.Prototype;

internal static class BattleChecks
{
    private static int checks;
    private static AbilityDefinition Attack => PrototypeBattleFactory.Attack;
    private static AbilityDefinition Defend => PrototypeBattleFactory.Defend;
    private static AbilityDefinition Heal => PrototypeBattleFactory.Heal;
    private static AbilityDefinition Heavy => PrototypeBattleFactory.HeavyStrike;

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
        BattleSession battle = PrototypeBattleFactory.CreateEncounter();
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
        Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve realizar uma acao valida.");
        Assert(aren.Hp == 85 && battle.CurrentActor == bram, "IA deve atacar aliado vivo e avancar.");
        Act(battle, Defend, bram);
        Assert(battle.Round == 2 && battle.CurrentActor == aren, "Fim da ordem deve iniciar nova rodada.");
    }

    private static void CheckResourcesAndTargets()
    {
        BattleSession battle = PrototypeBattleFactory.CreateEncounter();
        CombatantState guard = Find(battle, "guard");
        CombatantState aren = Find(battle, "aren");
        Act(battle, Heavy, guard);
        Assert(aren.Mp == 16 && guard.Hp == 122, "Golpe forte deve consumir 4 MP e causar 28 de dano.");

        var limited = new CombatantState("hero", new CombatantDefinition("Heroi", "Atacante", 100, 3, 10, 0, 0, 20, Attack, Heavy), CombatTeam.Party);
        var opponent = new CombatantState("enemy", new CombatantDefinition("Inimigo", "Inimigo", 100, 0, 1, 0, 0, 1, Attack), CombatTeam.Enemy);
        battle = new BattleSession(limited, opponent);
        Assert(!battle.TryAct(limited, Heavy, opponent, out _), "MP insuficiente deve impedir a habilidade.");
        Assert(limited.Mp == 3 && opponent.Hp == 100 && battle.CurrentActor == limited, "Falha por MP nao pode alterar estado.");
        CombatantState foreign = Find(PrototypeBattleFactory.CreateEncounter(), "guard");
        Assert(!battle.TryAct(limited, Attack, foreign, out _), "Alvo de outra batalha deve ser recusado.");
        var counterfeit = new AbilityDefinition("attack", "Ataque falso", "", 0, 999, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
        Assert(!battle.TryAct(limited, counterfeit, opponent, out _), "Habilidade nao aprendida deve ser recusada mesmo com ID igual.");
        Assert(!battle.TryAct(limited, Attack, limited, out _), "Ataque nao pode atingir um aliado.");
        Assert(opponent.Hp == 100 && limited.Mp == 3, "Alvos ou habilidades invalidos nao podem consumir recursos.");
    }

    private static void CheckHealingAndDefense()
    {
        BattleSession battle = PrototypeBattleFactory.CreateEncounter();
        CombatantState aren = Find(battle, "aren");
        CombatantState lia = Find(battle, "lia");
        Act(battle, Defend, aren);
        Act(battle, Attack, Find(battle, "guard"));
        EnemyAI.TryTakeTurn(battle, out _);
        Assert(aren.Hp == 92, "Defesa deve reduzir 15 de dano para 8.");
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
        EnemyAI.TryTakeTurn(battle, out _);
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
        Assert(EnemyAI.TryTakeTurn(independent, out _), "IA deve atacar uma das instancias compartilhadas.");
        Assert(one.Hp == 17 && two.Hp == 20 && shared.MaxHp == 20, "Dano deve alterar somente a instancia, preservando outra instancia e a definicao.");

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
            BattleSession battle = PrototypeBattleFactory.CreateEncounter();
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
        BattleSession fresh = PrototypeBattleFactory.CreateEncounter();
        Assert(Find(fresh, "aren").Hp == 100 && Find(fresh, "aren").Mp == 20, "Nova batalha deve iniciar com estado independente.");
    }

    public static int Main()
    {
        try
        {
            CheckBasicRound();
            CheckResourcesAndTargets();
            CheckHealingAndDefense();
            CheckOutcomeAndIndependentStates();
            CheckFullEncounters();
            Console.WriteLine(checks + " verificacoes de combate passaram.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
