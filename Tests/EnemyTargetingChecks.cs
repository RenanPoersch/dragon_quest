using System;
using DragonQuest.Combat;

internal static class EnemyTargetingChecks
{
    private static int checks;
    private static readonly AbilityDefinition Attack = new AbilityDefinition("attack", "Ataque", "", 0, 0, AbilityEffect.PhysicalDamage, TargetRule.Enemy);
    private static readonly AbilityDefinition Defend = new AbilityDefinition("defend", "Defender", "", 0, 0, AbilityEffect.Defend, TargetRule.Self);
    private static readonly AbilityDefinition Area = new AbilityDefinition("area", "Area", "", 0, 0, AbilityEffect.MagicDamage, TargetRule.AllEnemies);

    private static void Check(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    private static CombatantState Fighter(string id, CombatTeam team, int hp = 1000, int speed = 10, params AbilityDefinition[] abilities)
        => new CombatantState(id, new CombatantDefinition(id, "", hp, 100, 10, 0, 10, speed,
            abilities.Length == 0 ? new[] { Defend } : abilities), team);

    private static BattleSession Encounter(out CombatantState[] heroes, out CombatantState enemy)
    {
        heroes = new[] { Fighter("aren", CombatTeam.Party), Fighter("lia", CombatTeam.Party, 800), Fighter("bram", CombatTeam.Party, 1500) };
        enemy = Fighter("enemy", CombatTeam.Enemy, speed: 100, abilities: new[] { Attack, Area, Defend });
        var fallen = Fighter("fallen", CombatTeam.Party); fallen.TakeDamage(fallen.Hp);
        var otherEnemy = Fighter("other-enemy", CombatTeam.Enemy);
        // Interleave teams and a defeated hero to catch selection from the wrong list.
        return new BattleSession(heroes[0], fallen, enemy, heroes[1], otherEnemy, heroes[2]);
    }

    private static void CheckEqualChances()
    {
        var battle = Encounter(out var heroes, out var enemy);
        var counts = new int[3];
        for (int wound = -1; wound < heroes.Length; wound++)
        {
            foreach (var hero in heroes) hero.RecoverHp(hero.Definition.MaxHp);
            if (wound >= 0) heroes[wound].TakeDamage(heroes[wound].Hp - 1);
            Array.Clear(counts, 0, counts.Length);
            for (int draw = 0; draw < 300; draw++)
            {
                var selected = EnemyAI.ChooseTarget(battle, Attack, (draw + .5) / 300);
                int index = Array.IndexOf(heroes, selected);
                Check(index >= 0, "Somente herois vivos podem ser sorteados, sem selecionar aliados do inimigo.");
                counts[index]++;
            }
            Check(counts[0] == 100 && counts[1] == 100 && counts[2] == 100,
                "Todos os herois devem ter a mesma chance com HP cheio ou qualquer um quase derrotado.");
        }
        Check(EnemyAI.ChooseTarget(battle, Attack, 0) == heroes[0]
            && EnemyAI.ChooseTarget(battle, Attack, .999999999) == heroes[2], "Extremos do sorteio alcancam primeiro e ultimo herois vivos.");

        var seeded = new Random(173);
        Array.Clear(counts, 0, counts.Length);
        for (int draw = 0; draw < 6000; draw++)
            counts[Array.IndexOf(heroes, EnemyAI.ChooseTarget(battle, Attack, seeded.NextDouble()))]++;
        foreach (int count in counts) Check(count > 1800 && count < 2200, "Sorteios com semente fixa distribuem ataques pelos tres herois.");
        Console.WriteLine("6000 sorteios: Aren=" + counts[0] + ", Lia=" + counts[1] + ", Bram=" + counts[2] + ".");
    }

    private static void CheckAllOffensiveEffects()
    {
        var battle = Encounter(out var heroes, out _);
        foreach (var effect in new[] { AbilityEffect.PhysicalDamage, AbilityEffect.MagicDamage, AbilityEffect.Drain,
            AbilityEffect.Bind, AbilityEffect.MindControl, AbilityEffect.Silence })
        {
            var ability = new AbilityDefinition("effect-" + effect, "", "", 0, 1, effect, TargetRule.Enemy);
            Check(EnemyAI.ChooseTarget(battle, ability, .5) == heroes[1], effect + " usa sorteio para o alvo individual.");
        }
        heroes[1].TakeDamage(heroes[1].Hp);
        Check(EnemyAI.ChooseTarget(battle, Attack, .49) == heroes[0]
            && EnemyAI.ChooseTarget(battle, Attack, .5) == heroes[2], "Chance e redistribuida igualmente entre os sobreviventes.");
        heroes[0].TakeDamage(heroes[0].Hp);
        Check(EnemyAI.ChooseTarget(battle, Attack, 0) == heroes[2]
            && EnemyAI.ChooseTarget(battle, Attack, .999999) == heroes[2], "Um unico sobrevivente recebe todos os sorteios.");
    }

    private static void NextEnemyTurn(BattleSession battle, CombatantState enemy)
    {
        while (battle.CurrentActor != enemy)
        {
            var actor = battle.CurrentActor;
            Check(battle.TryAct(actor, Defend, actor, out _), "Outros combatentes podem passar o turno.");
        }
    }

    private static void CheckExecutionAndRepeats()
    {
        var battle = Encounter(out var heroes, out var enemy);
        // Only basic attacks here; choice of ability is tested separately in the combat suite.
        enemy.RefreshDefinition(Fighter("definition", CombatTeam.Enemy, speed: 100, abilities: new[] { Attack }).Definition);
        heroes[0].TakeDamage(500);
        foreach (double draw in new[] { .5, .5, .999999, 0d })
        {
            var expected = draw == .5 ? heroes[1] : draw == 0 ? heroes[0] : heroes[2];
            int[] before = { heroes[0].Hp, heroes[1].Hp, heroes[2].Hp };
            Check(EnemyAI.TryTakeTurn(battle, draw, out _), "Inimigo executa ataque com o sorteio fornecido.");
            for (int i = 0; i < heroes.Length; i++)
                Check((heroes[i].Hp < before[i]) == (heroes[i] == expected), "Dano atinge somente o heroi sorteado, inclusive em repeticoes consecutivas.");
            NextEnemyTurn(battle, enemy);
        }
    }

    private static void CheckTauntAndArea()
    {
        var battle = Encounter(out var heroes, out var enemy);
        enemy.RefreshDefinition(Fighter("definition", CombatTeam.Enemy, speed: 100, abilities: new[] { Attack }).Definition);
        enemy.TauntedBy = heroes[2]; enemy.TauntTurns = 2;
        for (int turn = 0; turn < 2; turn++)
        {
            Check(EnemyAI.ChooseTarget(battle, Attack, 0) == heroes[2]
                && EnemyAI.ChooseTarget(battle, Attack, .999999) == heroes[2], "Provocar forca seu usuario independentemente do sorteio.");
            Check(battle.GetValidTargets(Area).Count == 3, "Provocar nao restringe ataques de area.");
            Check(EnemyAI.TryTakeTurn(battle, 0, out _), "IA executa o ataque provocado.");
            NextEnemyTurn(battle, enemy);
        }
        Check(enemy.TauntTurns == 0 && EnemyAI.ChooseTarget(battle, Attack, 0) == heroes[0], "Fim de Provocar restaura o sorteio normal.");
        enemy.TauntedBy = heroes[2]; enemy.TauntTurns = 2;
        heroes[2].TakeDamage(heroes[2].Hp);
        Check(EnemyAI.ChooseTarget(battle, Attack, .999999) == heroes[1], "Provocador derrotado nao impede o sorteio entre sobreviventes.");

        battle = Encounter(out heroes, out enemy);
        int[] before = { heroes[0].Hp, heroes[1].Hp, heroes[2].Hp };
        Check(EnemyAI.ChooseTarget(battle, Defend, .9) == enemy, "Defender continua direcionado ao proprio inimigo.");
        Check(battle.TryAct(enemy, Area, EnemyAI.ChooseTarget(battle, Area, .9), out _), "Area aceita o alvo inicial selecionado.");
        for (int i = 0; i < heroes.Length; i++) Check(heroes[i].Hp < before[i], "Area continua atingindo cada heroi vivo.");
    }

    private static void CheckProtectionAndHealing()
    {
        var battle = Encounter(out var heroes, out var enemy);
        heroes[1].ProtectedBy = heroes[2];
        Check(EnemyAI.ChooseTarget(battle, Attack, .5) == heroes[1], "Proteger nao altera o heroi sorteado.");
        Check(EnemyAI.TryTakeTurn(battle, .5, out _) && heroes[1].Hp == 800 && heroes[2].Hp == 1460,
            "Proteger intercepta o golpe dirigido ao heroi sorteado.");

        battle = Encounter(out _, out enemy);
        var heal = new AbilityDefinition("heal", "", "", 0, 10, AbilityEffect.Heal, TargetRule.Ally);
        foreach (var participant in battle.Combatants)
            if (participant.Team == CombatTeam.Enemy) participant.TakeDamage(participant == enemy ? 1 : 500);
        Check(EnemyAI.ChooseTarget(battle, heal, 0).Id == "other-enemy", "Cura aliada conserva a preferencia pelo ferido.");
        Check(EnemyAI.ChooseTarget(battle, null, 0) == null, "Sem habilidade nao ha alvo.");
        foreach (double invalid in new[] { -1d, 1d, double.NaN, double.PositiveInfinity })
        {
            bool rejected = false;
            try { EnemyAI.ChooseTarget(battle, Attack, invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Sorteios invalidos devem ser rejeitados.");
        }
    }

    public static int Main()
    {
        try
        {
            CheckEqualChances();
            CheckAllOffensiveEffects();
            CheckExecutionAndRepeats();
            CheckTauntAndArea();
            CheckProtectionAndHealing();
            Console.WriteLine(checks + " verificacoes de alvos inimigos passaram.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
