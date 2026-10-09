using System;
using DragonQuest.Combat;
using DragonQuest.Prototype;

internal static class AbilityChecks
{
    private static int checks;
    private static AbilityDefinition Attack => PrototypeBattleFactory.Attack;
    private static AbilityDefinition Defend => PrototypeBattleFactory.Defend;

    private static void Assert(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    private static CombatantState Fighter(string id, CombatTeam team, int speed, int hp = 100,
        int mp = 60, int attack = 20, int defense = 5, params AbilityDefinition[] abilities)
    {
        if (abilities.Length == 0) abilities = new[] { Attack, Defend };
        return new CombatantState(id, new CombatantDefinition(id, "Teste", hp, mp, attack, defense, 0, speed, abilities), team);
    }

    private static CombatantState Find(BattleSession battle, string id)
    {
        foreach (CombatantState fighter in battle.Combatants)
            if (fighter.Id == id) return fighter;
        throw new Exception("Combatente ausente: " + id);
    }

    private static void Act(BattleSession battle, AbilityDefinition ability, CombatantState target)
    {
        Assert(battle.TryAct(battle.CurrentActor, ability, target, out string message), "Acao recusada: " + message);
    }

    private static void CheckAreaDamage()
    {
        AbilityDefinition cleave = PrototypeBattleFactory.Cleave;
        var hero = Fighter("hero", CombatTeam.Party, 30, abilities: new[] { Attack, cleave, Defend });
        var one = Fighter("one", CombatTeam.Enemy, 20, hp: 30);
        var two = Fighter("two", CombatTeam.Enemy, 10, hp: 10);
        var battle = new BattleSession(hero, one, two);
        Assert(battle.GetValidTargets(cleave).Count == 2, "Area deve incluir os dois inimigos vivos.");
        Assert(!battle.TryAct(hero, cleave, hero, out _) && hero.Mp == 60, "Area em alvo aliado deve falhar sem custo.");
        Act(battle, cleave, one);
        Assert(one.Hp == 10 && two.Hp == 0 && hero.Mp == 53, "Area deve atingir cada inimigo e gastar MP uma vez.");
        Assert(battle.CurrentActor == one && hero.TurnsTaken == 1, "Area deve consumir somente um turno.");
        Act(battle, Defend, one);
        Assert(battle.Round == 2 && battle.CurrentActor == hero, "Inimigo derrotado em area deve perder seu turno.");
        Assert(battle.GetValidTargets(cleave).Count == 1, "Area deve excluir derrotados.");
        Assert(!battle.TryAct(hero, cleave, two, out _) && hero.Mp == 53, "Alvo morto nao pode confirmar area.");
        Act(battle, cleave, one);
        Assert(battle.Outcome == BattleOutcome.Victory && hero.Mp == 46, "Area deve encerrar a batalha se vencer todos.");

        hero = Fighter("poor", CombatTeam.Party, 30, mp: 6, abilities: new[] { cleave });
        one = Fighter("one", CombatTeam.Enemy, 20);
        two = Fighter("two", CombatTeam.Enemy, 10);
        battle = new BattleSession(hero, one, two);
        Assert(!battle.TryAct(hero, cleave, one, out _), "Area sem MP deve falhar.");
        Assert(one.Hp == 100 && two.Hp == 100 && hero.Mp == 6 && hero.TurnsTaken == 0, "Falha nao pode aplicar dano parcial ou consumir turno.");
    }

    private static void CheckPoisonAndCleanse()
    {
        AbilityDefinition poison = PrototypeBattleFactory.PoisonStrike;
        AbilityDefinition cleanse = PrototypeBattleFactory.Cleanse;
        var enemy = Fighter("enemy", CombatTeam.Enemy, 20, attack: 10, abilities: new[] { poison, Defend });
        var hero = Fighter("hero", CombatTeam.Party, 10, hp: 101, defense: 0, abilities: new[] { cleanse, Attack, Defend });
        var battle = new BattleSession(enemy, hero);
        Act(battle, poison, hero);
        Assert(hero.Hp == 91 && hero.PoisonTurns == 3 && enemy.Mp == 56, "Golpe venenoso deve causar dano e aplicar tres turnos.");
        Act(battle, Defend, hero);
        Assert(hero.Hp == 85 && hero.PoisonTurns == 2, "Veneno arredonda 5% para cima e ignora defesa.");
        Act(battle, Defend, enemy);
        Act(battle, Defend, hero);
        Act(battle, Defend, enemy);
        Act(battle, Defend, hero);
        Assert(hero.Hp == 73 && hero.PoisonTurns == 0, "Veneno deve terminar depois de tres acoes do afetado.");
        Act(battle, Defend, enemy);
        Assert(battle.GetValidTargets(cleanse).Count == 0, "Purificar nao deve listar aliados saudaveis.");
        Assert(!battle.TryAct(hero, cleanse, hero, out _) && hero.Mp == 60, "Purificar sem condicao deve falhar sem gasto.");
        Act(battle, Defend, hero);
        Assert(hero.Hp == 73, "Veneno expirado nao causa dano.");
        Act(battle, poison, hero);
        int before = hero.Hp;
        Act(battle, cleanse, hero);
        Assert(hero.Hp == before && hero.PoisonTurns == 0 && hero.Mp == 56, "Purificar remove veneno antes do dano de fim de turno.");

        // Reaplicar veneno renova a duracao, sem acumular duas fontes de dano.
        Act(battle, poison, hero);
        Act(battle, Defend, hero);
        Act(battle, poison, hero);
        Assert(hero.PoisonTurns == 3, "Veneno reaplicado deve renovar a duracao.");
        before = hero.Hp;
        Act(battle, Defend, hero);
        Assert(hero.Hp == before - 6 && hero.PoisonTurns == 2, "Reaplicacao nao duplica o dano por turno.");

        var fragile = Fighter("fragile", CombatTeam.Party, 10, hp: 20, defense: 100);
        var weak = Fighter("weak", CombatTeam.Enemy, 20, attack: 0, abilities: new[] { poison, Defend });
        battle = new BattleSession(weak, fragile);
        Act(battle, poison, fragile);
        fragile.TakeDamage(18); // Deixa 1 HP para isolar a morte por veneno.
        Act(battle, Defend, fragile);
        Assert(battle.Outcome == BattleOutcome.Defeat && fragile.PoisonTurns == 0, "Morte pelo veneno deve encerrar e limpar condicoes.");

        hero = Fighter("winner", CombatTeam.Party, 10);
        weak = Fighter("weak", CombatTeam.Enemy, 20, hp: 1, attack: 0, abilities: new[] { poison });
        battle = new BattleSession(weak, hero);
        Act(battle, poison, hero);
        before = hero.Hp;
        Act(battle, Attack, weak);
        Assert(battle.Outcome == BattleOutcome.Victory && hero.Hp == before, "Veneno nao deve continuar apos o golpe que encerra a batalha.");
    }

    private static void CheckTauntAndProtection()
    {
        AbilityDefinition taunt = PrototypeBattleFactory.Taunt;
        AbilityDefinition protect = PrototypeBattleFactory.Protect;
        var tank = Fighter("tank", CombatTeam.Party, 30, abilities: new[] { taunt, protect, Attack, Defend });
        var ally = Fighter("ally", CombatTeam.Party, 20);
        var enemy = Fighter("enemy", CombatTeam.Enemy, 10);
        var battle = new BattleSession(tank, ally, enemy);
        Act(battle, taunt, enemy);
        Act(battle, Defend, ally);
        Assert(battle.GetValidTargets(Attack).Count == 1 && battle.GetValidTargets(Attack)[0] == tank, "Provocar deve restringir ataques individuais ao tank.");
        Assert(!battle.TryAct(enemy, Attack, ally, out _) && enemy.TurnsTaken == 0, "Inimigo nao pode ignorar provocacao nem gastar turno na falha.");
        Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve respeitar provocacao.");
        Assert(tank.Hp == 85 && ally.Hp == 100 && enemy.TauntTurns == 1, "Provocar deve durar por turnos do inimigo.");
        Act(battle, Defend, tank);
        Act(battle, Defend, ally);
        Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve agir no segundo turno provocado.");
        Assert(tank.Hp == 77 && enemy.TauntTurns == 0 && enemy.TauntedBy == null, "Provocacao deve terminar apos a segunda acao do alvo.");
        Act(battle, Defend, tank);
        Act(battle, Defend, ally);
        Assert(battle.GetValidTargets(Attack).Count == 2, "Provocacao expirada libera ambos os alvos.");
        Act(battle, Defend, enemy);

        Assert(!battle.TryAct(tank, protect, tank, out _) && tank.Mp == 57, "Proteger deve exigir outro aliado sem gasto na falha.");
        Act(battle, protect, ally);
        Act(battle, Defend, ally);
        Act(battle, Attack, ally);
        Assert(tank.Hp == 62 && ally.Hp == 100 && ally.ProtectedBy == null, "Tank recebe dano usando sua defesa; protecao e consumida.");
        Act(battle, protect, ally);
        Act(battle, Defend, ally);
        Act(battle, Defend, enemy);
        Assert(ally.ProtectedBy == null, "Protecao sem golpe deve expirar ao iniciar o proximo turno do tank.");

        // Ataques em area ignoram provocacao e nao sao interceptados por protecao.
        tank = Fighter("tank", CombatTeam.Party, 30, abilities: new[] { taunt, protect, Defend });
        ally = Fighter("ally", CombatTeam.Party, 20);
        enemy = Fighter("enemy", CombatTeam.Enemy, 10, abilities: new[] { PrototypeBattleFactory.EnemySweep, Defend });
        battle = new BattleSession(tank, ally, enemy);
        Act(battle, taunt, enemy);
        Act(battle, Defend, ally);
        Assert(battle.GetValidTargets(PrototypeBattleFactory.EnemySweep).Count == 2, "Area inclui a equipe apesar da provocacao.");
        Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve conseguir usar area provocada.");
        Assert(tank.Hp == 85 && ally.Hp == 92, "Area aplica defesa de cada alvo separadamente.");
        Act(battle, protect, ally);
        Act(battle, Defend, ally);
        Act(battle, PrototypeBattleFactory.EnemySweep, ally);
        Assert(tank.Hp == 70 && ally.Hp == 84 && ally.ProtectedBy == null, "Area nao transfere dano e protecao expira no inicio do turno seguinte.");

        // O golpe interceptado tambem transfere o veneno novo para o protetor.
        tank = Fighter("tank", CombatTeam.Party, 30, abilities: new[] { protect, Defend });
        ally = Fighter("ally", CombatTeam.Party, 20);
        enemy = Fighter("enemy", CombatTeam.Enemy, 10, abilities: new[] { PrototypeBattleFactory.PoisonStrike });
        battle = new BattleSession(tank, ally, enemy);
        Act(battle, protect, ally);
        Act(battle, Defend, ally);
        Act(battle, PrototypeBattleFactory.PoisonStrike, ally);
        Assert(tank.PoisonTurns == 3 && ally.PoisonTurns == 0 && ally.Hp == 100, "Veneno do golpe interceptado deve afetar somente o tank.");

        // Morte do protetor remove os vinculos antes da proxima acao inimiga.
        tank = Fighter("tank", CombatTeam.Party, 30, hp: 1, abilities: new[] { protect, taunt, Defend });
        ally = Fighter("ally", CombatTeam.Party, 20);
        enemy = Fighter("enemy", CombatTeam.Enemy, 10);
        battle = new BattleSession(tank, ally, enemy);
        Act(battle, protect, ally);
        Act(battle, Defend, ally);
        Act(battle, Attack, tank);
        Assert(!tank.IsAlive && ally.ProtectedBy == null, "Protetor derrotado nao pode permanecer vinculado.");

        tank = Fighter("tank", CombatTeam.Party, 30, hp: 1, abilities: new[] { taunt, Defend });
        ally = Fighter("ally", CombatTeam.Party, 20);
        enemy = Fighter("enemy", CombatTeam.Enemy, 10);
        battle = new BattleSession(tank, ally, enemy);
        Act(battle, taunt, enemy);
        Act(battle, Defend, ally);
        Act(battle, Attack, tank);
        Assert(enemy.TauntedBy == null && enemy.TauntTurns == 0, "Morte do provocador deve liberar o inimigo imediatamente.");
    }

    private static void CheckReviveAndTurnOrder()
    {
        AbilityDefinition revive = PrototypeBattleFactory.Revive;
        var fast = Fighter("fast", CombatTeam.Party, 30, hp: 101, defense: 0);
        var healer = Fighter("healer", CombatTeam.Party, 20, abilities: new[] { revive, PrototypeBattleFactory.Heal, Attack, Defend });
        var enemy = Fighter("enemy", CombatTeam.Enemy, 10, hp: 1000, attack: 120, defense: 0);
        var battle = new BattleSession(fast, healer, enemy);
        Act(battle, Attack, enemy);
        Assert(!battle.TryAct(healer, revive, fast, out _) && healer.Mp == 60, "Reviver aliado vivo deve falhar sem custo.");
        Act(battle, Defend, healer);
        fast.PoisonTurns = 3;
        Act(battle, Attack, fast);
        Assert(fast.Hp == 0 && fast.PoisonTurns == 0 && battle.Round == 2 && battle.CurrentActor == healer, "Morte limpa condicoes e remove o derrotado da nova rodada.");
        Assert(!battle.TryAct(healer, PrototypeBattleFactory.Heal, fast, out _), "Cura comum continua sem reviver.");
        Assert(battle.GetValidTargets(revive).Count == 1 && battle.GetValidTargets(revive)[0] == fast, "Reviver deve listar somente aliado derrotado.");
        Act(battle, revive, fast);
        Assert(fast.Hp == 31 && healer.Mp == 48 && fast.PoisonTurns == 0, "Reviver deve restaurar 30% arredondado para cima e gastar 12 MP.");
        Assert(battle.CurrentActor == enemy && fast.TurnsTaken == 1, "Reviver nao adiciona turno a quem ficou fora da rodada.");
        Act(battle, Defend, enemy);
        Assert(battle.Round == 3 && battle.CurrentActor == fast, "Revivido deve retornar na ordem por velocidade da rodada seguinte.");

        // Quem ainda tem uma posicao futura na rodada conserva somente aquele turno.
        fast = Fighter("victim", CombatTeam.Party, 10, hp: 101, defense: 0);
        healer = Fighter("healer", CombatTeam.Party, 20, abilities: new[] { revive, Defend });
        enemy = Fighter("enemy", CombatTeam.Enemy, 30, hp: 1000, attack: 120);
        battle = new BattleSession(enemy, healer, fast);
        Act(battle, Attack, fast);
        Act(battle, revive, fast);
        Assert(battle.CurrentActor == fast && battle.Round == 1, "Revivido antes da sua posicao pode usar o turno ainda pendente.");
        Act(battle, Defend, fast);
        Assert(battle.CurrentActor == enemy && battle.Round == 2 && fast.TurnsTaken == 1, "Ressurreicao nao pode duplicar turnos.");
    }

    private static void CheckEncounterStrategies()
    {
        for (int strategy = 0; strategy < 2; strategy++)
        {
            BattleSession battle = PrototypeBattleFactory.CreateEncounter();
            Assert(battle.Combatants.Count == 5, "Encontro atual deve ter tres aliados e dois inimigos.");
            int actions = 0;
            bool usedArea = false, usedCleanse = false, usedTaunt = false;
            while (battle.Outcome == BattleOutcome.None && actions++ < 500)
            {
                CombatantState actor = battle.CurrentActor;
                if (actor.Team == CombatTeam.Enemy)
                {
                    Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve progredir no encontro com condicoes.");
                    continue;
                }
                AbilityDefinition ability = strategy == 1 ? Defend : Attack;
                if (strategy == 0)
                {
                    if (actor.Id == "aren")
                    {
                        if (actor.Mp >= PrototypeBattleFactory.Cleave.MpCost && battle.GetValidTargets(Attack).Count > 1)
                        { ability = PrototypeBattleFactory.Cleave; usedArea = true; }
                        else if (actor.Mp >= PrototypeBattleFactory.HeavyStrike.MpCost) ability = PrototypeBattleFactory.HeavyStrike;
                    }
                    else if (actor.Id == "lia")
                    {
                        if (actor.Mp >= PrototypeBattleFactory.Revive.MpCost && battle.GetValidTargets(PrototypeBattleFactory.Revive).Count > 0)
                            ability = PrototypeBattleFactory.Revive;
                        else if (actor.Mp >= PrototypeBattleFactory.Cleanse.MpCost && battle.GetValidTargets(PrototypeBattleFactory.Cleanse).Count > 0)
                        { ability = PrototypeBattleFactory.Cleanse; usedCleanse = true; }
                        else if (actor.Mp >= PrototypeBattleFactory.Heal.MpCost && battle.GetValidTargets(PrototypeBattleFactory.Heal).Count > 0)
                            ability = PrototypeBattleFactory.Heal;
                    }
                    else if (actor.Mp >= PrototypeBattleFactory.Taunt.MpCost && Find(battle, "guard").IsAlive && Find(battle, "guard").TauntTurns == 0)
                    { ability = PrototypeBattleFactory.Taunt; usedTaunt = true; }
                }
                CombatantState target = ability.Targets == TargetRule.Self ? actor : battle.GetValidTargets(ability)[0];
                Act(battle, ability, target);
            }
            Assert(actions < 500, "Encontro atual nao deve travar.");
            Assert(battle.Outcome == (strategy == 0 ? BattleOutcome.Victory : BattleOutcome.Defeat), "Encontro atual deve permitir vencer e perder.");
            if (strategy == 0) Assert(usedArea && usedCleanse && usedTaunt, "Estrategia vencedora deve exercitar habilidades dos tres papeis.");
        }
        BattleSession fresh = PrototypeBattleFactory.CreateEncounter();
        Assert(Find(fresh, "lia").Mp == 60 && Find(fresh, "bram").Mp == 30 && Find(fresh, "aren").Mp == 28, "Repeticao inicia com recursos novos.");
        foreach (CombatantState actor in fresh.Combatants)
            Assert(actor.PoisonTurns == 0 && actor.TauntTurns == 0 && actor.ProtectedBy == null && actor.TurnsTaken == 0, "Efeitos nao podem vazar entre encontros.");
    }

    private static void CheckEnemyFallbackAndDefinitions()
    {
        var hero = Fighter("hero", CombatTeam.Party, 10);
        var enemy = Fighter("enemy", CombatTeam.Enemy, 20, mp: 0,
            abilities: new[] { PrototypeBattleFactory.PoisonStrike, Attack, PrototypeBattleFactory.EnemySweep, Defend });
        var battle = new BattleSession(hero, enemy);
        Assert(EnemyAI.ChooseAbility(battle) == Attack, "Inimigo sem MP deve escolher ataque basico.");
        Assert(enemy.TurnsTaken == 0 && enemy.Mp == 0, "Consultar intencao da IA nao deve consumir recursos.");
        Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve agir mesmo sem MP.");
        Act(battle, Defend, hero);
        Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve executar o segundo ataque sem MP.");
        Act(battle, Defend, hero);
        Assert(enemy.TurnsTaken == 2, "Proximo ataque planejado deve ser a terceira ofensiva.");
        Assert(EnemyAI.ChooseAbility(battle) == Attack, "Alternancia sem MP nao pode travar no ataque em area.");

        int rejected = 0;
        try { new AbilityDefinition("bad", "", "", 0, 0, AbilityEffect.Revive, TargetRule.Ally); } catch (ArgumentException) { rejected++; }
        try { new AbilityDefinition("bad", "", "", 0, 101, AbilityEffect.Revive, TargetRule.Ally); } catch (ArgumentException) { rejected++; }
        try { new AbilityDefinition("bad", "", "", 0, 1, AbilityEffect.Taunt, TargetRule.Ally); } catch (ArgumentException) { rejected++; }
        try { new AbilityDefinition("bad", "", "", 0, 1, AbilityEffect.Heal, TargetRule.AllEnemies); } catch (ArgumentException) { rejected++; }
        try { new AbilityDefinition("bad", "", "", 0, 1, AbilityEffect.Heal, TargetRule.Ally, 3); } catch (ArgumentException) { rejected++; }
        Assert(rejected == 5, "Definicoes incompatíveis devem falhar antes do combate.");
    }

    public static int Run()
    {
        CheckAreaDamage();
        CheckPoisonAndCleanse();
        CheckTauntAndProtection();
        CheckReviveAndTurnOrder();
        CheckEnemyFallbackAndDefinitions();
        CheckEncounterStrategies();
        return checks;
    }
}
