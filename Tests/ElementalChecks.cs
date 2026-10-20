using System;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Prototype;

internal static class ElementalChecks
{
    private static int checks;
    private static void Assert(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    private static void Reject(Action action, string description)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; }
        Assert(rejected, description);
    }

    private static ElementalAffinities Weak(DamageElement element) => new ElementalAffinities(new[] { element });
    private static ElementalAffinities Resist(DamageElement element) => new ElementalAffinities(resistances: new[] { element });
    private static AbilityDefinition Spell(DamageElement element, TargetRule targets = TargetRule.Enemy)
        => new AbilityDefinition("test-spell", "Magia", "", 6, 22, AbilityEffect.MagicDamage, targets, element: element);

    private static CombatantState Caster(AbilityDefinition ability, CombatTeam team = CombatTeam.Party, int offense = 10)
        => new CombatantState("caster", new CombatantDefinition("Mago", "Teste", 100, 60, offense, 11, offense, 20,
            ability == MateriaResolver.Defend ? new[] { ability } : new[] { ability, MateriaResolver.Defend }), team);

    private static CombatantState Target(string id, ElementalAffinities affinities, int hp = 100, int defense = 11,
        CombatTeam team = CombatTeam.Enemy)
        => new CombatantState(id, new CombatantDefinition(id, "Teste", hp, 0, 1, defense, 0, 1,
            affinities, MateriaResolver.Attack, MateriaResolver.Defend), team);

    private static void Act(BattleSession battle, AbilityDefinition ability, CombatantState target)
    {
        Assert(battle.TryAct(battle.CurrentActor, ability, target, out string message), "Acao elemental recusada: " + message);
    }

    private static MateriaInstance FindMateria(PartyProgress party, string id)
    {
        foreach (MateriaInstance materia in party.Materias) if (materia.Definition.Id == id) return materia;
        throw new Exception("Materia ausente: " + id);
    }

    private static AbilityDefinition Command(CombatantState actor, string id)
    {
        foreach (AbilityDefinition command in actor.Definition.Abilities) if (command.SourceAbilityId == id) return command;
        throw new Exception("Comando ausente: " + id);
    }

    private static void Equip(PartyProgress party, PartyMember member, int slot, MateriaInstance materia)
        => Assert(party.TryEquip(member, member.Armor, slot, materia, out _), "Materia elemental deve poder ser equipada.");

    private static void CheckDefinitions()
    {
        var weaknesses = new[] { DamageElement.Lightning, DamageElement.Fire };
        var resistances = new[] { DamageElement.Ice };
        var profile = new ElementalAffinities(weaknesses, resistances);
        weaknesses[0] = DamageElement.Ice; resistances[0] = DamageElement.Fire;
        Assert(profile.DamagePercent(DamageElement.Fire) == 150 && profile.DamagePercent(DamageElement.Lightning) == 150
            && profile.DamagePercent(DamageElement.Ice) == 50, "Perfil deve copiar os dados de entrada e aceitar varias afinidades.");
        Assert(profile.Weaknesses[0] == DamageElement.Fire && profile.DamagePercent(DamageElement.None) == 100,
            "Afinidades devem ter ordem estavel; dano sem elemento e neutro.");
        Assert(profile.Description == "Fraco: Fogo, Raio / Resiste: Gelo", "Interface deve descrever todas as afinidades.");
        Assert(ElementalAffinities.Neutral.Description == "Fraco: nenhum / Resiste: nenhum", "Perfil neutro deve ser explicito.");
        Reject(() => new ElementalAffinities(new[] { DamageElement.None }), "Sem elemento nao pode ser uma fraqueza.");
        Reject(() => new ElementalAffinities(new[] { (DamageElement)99 }), "Elemento desconhecido deve ser recusado.");
        Reject(() => new ElementalAffinities(new[] { DamageElement.Fire, DamageElement.Fire }), "Fraquezas repetidas devem ser recusadas.");
        Reject(() => new ElementalAffinities(resistances: new[] { DamageElement.Ice, DamageElement.Ice }), "Resistencias repetidas devem ser recusadas.");
        Reject(() => new ElementalAffinities(new[] { DamageElement.Fire }, new[] { DamageElement.Fire }), "Fraqueza e resistencia do mesmo elemento conflitam.");
        Reject(() => profile.DamagePercent((DamageElement)99), "Consulta com elemento desconhecido deve falhar.");
        Reject(() => ElementalAffinities.NameOf((DamageElement)99), "Nome de elemento desconhecido deve falhar.");
        Reject(() => Spell((DamageElement)99), "Habilidade com elemento desconhecido deve falhar.");
        Reject(() => new AbilityDefinition("heal", "Cura", "", 0, 1, AbilityEffect.Heal, TargetRule.Ally, element: DamageElement.Fire),
            "Elemento nao pode modificar habilidades de recuperacao.");
        Reject(() => new CombatantDefinition("Teste", "Teste", 100, 0, 1, 0, 0, 1, (ElementalAffinities)null, MateriaResolver.Attack),
            "Perfil explicitamente nulo deve ser recusado.");
        Assert(Caster(Spell(DamageElement.Fire)).Definition.Affinities == ElementalAffinities.Neutral,
            "Combatentes antigos devem receber perfil neutro por padrao.");
    }

    private static void CheckDamageAndRounding()
    {
        foreach (DamageElement element in new[] { DamageElement.Fire, DamageElement.Ice, DamageElement.Lightning })
        {
            var profiles = new[] { Weak(element), Resist(element), ElementalAffinities.Neutral };
            int[] expected = { 31, 10, 21 }; // 10 magia + 22 poder - 11 defesa = 21.
            for (int i = 0; i < profiles.Length; i++)
            {
                AbilityDefinition spell = Spell(element);
                CombatantState hero = Caster(spell), enemy = Target("enemy", profiles[i]);
                var battle = new BattleSession(hero, enemy);
                Act(battle, spell, enemy);
                Assert(enemy.Hp == 100 - expected[i] && hero.Mp == 54 && hero.TurnsTaken == 1,
                    "Fraqueza/resistencia deve afetar o dano, sem alterar custo ou turno.");
                Assert(battle.LastMessage.Contains(ElementalAffinities.NameOf(element)), "Relatorio deve identificar o elemento usado.");
                if (i == 0) Assert(battle.LastMessage.Contains("fraqueza"), "Relatorio deve identificar fraqueza.");
                if (i == 1) Assert(battle.LastMessage.Contains("resistencia"), "Relatorio deve identificar resistencia.");
            }
        }

        foreach (bool weakness in new[] { true, false })
        {
            AbilityDefinition fire = Spell(DamageElement.Fire);
            CombatantState enemy = Caster(fire, CombatTeam.Enemy);
            CombatantState hero = Target("hero", weakness ? Weak(DamageElement.Fire) : Resist(DamageElement.Fire), team: CombatTeam.Party);
            hero.IsDefending = true;
            var battle = new BattleSession(enemy, hero);
            Act(battle, fire, hero);
            int actual = weakness ? 31 : 10;
            Assert(hero.Hp == 100 - actual && hero.Limit.Charge == actual,
                "Afinidade deve preceder Defender; Limit deve usar apenas o dano realmente recebido.");
        }

        foreach (ElementalAffinities profile in new[] { Weak(DamageElement.Fire), Resist(DamageElement.Fire) })
        {
            AbilityDefinition fire = Spell(DamageElement.Fire);
            CombatantState hero = Caster(fire), enemy = Target("enemy", profile, defense: 1000);
            enemy.IsDefending = true;
            var battle = new BattleSession(hero, enemy);
            Act(battle, fire, enemy);
            Assert(enemy.Hp == 99, "Dano deve continuar no minimo em 1 apos resistencia, arredondamento e Defender.");
        }

        var enormous = new AbilityDefinition("huge", "Fogo", "", 0, int.MaxValue, AbilityEffect.MagicDamage, TargetRule.Enemy, element: DamageElement.Fire);
        CombatantState hugeHero = Caster(enormous, offense: int.MaxValue), hugeEnemy = Target("enemy", Weak(DamageElement.Fire), int.MaxValue, 0);
        var hugeBattle = new BattleSession(hugeHero, hugeEnemy);
        Act(hugeBattle, enormous, hugeEnemy);
        Assert(hugeEnemy.Hp == 0 && hugeBattle.Outcome == BattleOutcome.Victory, "Calculo e multiplicador nao podem estourar inteiros.");
    }

    private static void CheckAreaAndProtection()
    {
        AbilityDefinition fireAll = Spell(DamageElement.Fire, TargetRule.AllEnemies);
        CombatantState hero = Caster(fireAll), weak = Target("weak", Weak(DamageElement.Fire)),
            resistant = Target("resistant", Resist(DamageElement.Fire)), neutral = Target("neutral", ElementalAffinities.Neutral);
        var battle = new BattleSession(hero, weak, resistant, neutral);
        Act(battle, fireAll, weak);
        Assert(weak.Hp == 69 && resistant.Hp == 90 && neutral.Hp == 79 && hero.Mp == 54 && hero.TurnsTaken == 1,
            "Area deve calcular a afinidade de cada alvo e cobrar custo/turno uma vez.");

        var infusion = new AbilityDefinition("infused", "Fogo infundido", "", 6, 22, AbilityEffect.PhysicalDamage, TargetRule.Enemy, element: DamageElement.Fire);
        CombatantState attacker = Caster(infusion, CombatTeam.Enemy);
        CombatantState ally = Target("ally", Weak(DamageElement.Fire), team: CombatTeam.Party),
            protector = Target("protector", Resist(DamageElement.Fire), team: CombatTeam.Party);
        ally.ProtectedBy = protector;
        battle = new BattleSession(attacker, ally, protector);
        Act(battle, infusion, ally);
        Assert(ally.Hp == 100 && protector.Hp == 80 && protector.Limit.Charge == 20 && ally.Limit.Charge == 0,
            "Infusao interceptada usa defesa e resistencia do protetor, e carrega somente o Limit dele.");
        Assert(ally.ProtectedBy == null && battle.LastMessage.Contains("resistencia"), "Interceptacao deve consumir a protecao e relatar a afinidade real.");

        AbilityDefinition fire = Spell(DamageElement.Fire);
        attacker = Caster(fire, CombatTeam.Enemy);
        ally = Target("ally", Weak(DamageElement.Fire), team: CombatTeam.Party);
        protector = Target("protector", Resist(DamageElement.Fire), team: CombatTeam.Party);
        ally.ProtectedBy = protector;
        battle = new BattleSession(attacker, ally, protector);
        Act(battle, fire, ally);
        Assert(ally.Hp == 38 && protector.Hp == 100 && ally.ProtectedBy == protector, "Magia elemental nao deve ser interceptada pela protecao fisica.");

        attacker = Caster(fireAll, CombatTeam.Enemy);
        ally = Target("ally", Weak(DamageElement.Fire), team: CombatTeam.Party);
        protector = Target("protector", Resist(DamageElement.Fire), team: CombatTeam.Party);
        ally.ProtectedBy = protector;
        battle = new BattleSession(attacker, ally, protector);
        Act(battle, fireAll, ally);
        Assert(ally.Hp == 38 && protector.Hp == 80, "Ataque elemental em area deve atingir ambos, sem interceptacao.");
    }

    private static void CheckMateriaCombos()
    {
        foreach (string id in new[] { "fire", "ice", "thunder", "phoenix" })
        {
            PartyProgress party = PrototypePartyFactory.CreateTestParty();
            PartyMember member = party.Members[1]; // Lia: demonstra que os comandos nao sao restritos a classe.
            MateriaInstance source = FindMateria(party, id), all = FindMateria(party, "all"), infuse = FindMateria(party, "infuse");
            AbilityDefinition original = source.Definition.Abilities[0];
            Equip(party, member, 0, source); Equip(party, member, 1, all);
            AbilityDefinition command = Command(member.CreateCombatant(), original.Id);
            Assert(command.Element == original.Element && command.Targets == TargetRule.AllEnemies
                && command.Power == original.Power * 125 / 100 && command.MpCost == original.MpCost * 3 / 2,
                "Magia/summon + All conserva elemento, afinidade da healer e custo unico em area.");

            Equip(party, member, 1, infuse);
            CombatantState caster = member.CreateCombatant();
            command = Command(caster, original.Id);
            Assert(command.Element == original.Element && command.Effect == AbilityEffect.PhysicalDamage
                && command.Targets == TargetRule.Enemy && command.Power == original.Power && command.MpCost == original.MpCost,
                "Infusao conserva elemento/custo, usa ataque e deixa de receber bonus de poder magico.");
            CombatantState enemy = Target("enemy", Weak(original.Element), hp: 500);
            var battle = new BattleSession(caster, enemy);
            Act(battle, command, enemy);
            int expected = id == "phoenix" ? 57 : 37; // Lia ATQ 14, defesa 11; poder 35 ou 22, fraqueza x1,5.
            Assert(enemy.Hp == 500 - expected, "Dano da Infusao deve usar ataque e a fraqueza elemental do alvo.");
            Assert(original.Element == command.Element && original.Effect == AbilityEffect.MagicDamage && original.Targets == TargetRule.Enemy,
                "Combos nao podem alterar as definicoes compartilhadas de materia.");
        }
    }

    private static void CheckUnrelatedEffectsAndSnapshots()
    {
        CombatantState hero = Caster(MateriaResolver.LimitBreak), enemy = Target("enemy", Weak(DamageElement.Fire), hp: 500);
        hero.Limit.AddDamage(100, 100);
        var battle = new BattleSession(hero, enemy);
        Act(battle, MateriaResolver.LimitBreak, enemy);
        Assert(enemy.Hp == 365 && hero.Limit.Charge == 0, "Omnislash sem elemento nao recebe multiplicador elemental nos 13 cortes.");

        hero = Caster(MateriaResolver.Defend);
        enemy = Target("enemy", ElementalAffinities.Neutral);
        hero.PoisonTurns = 1;
        battle = new BattleSession(hero, enemy);
        Act(battle, MateriaResolver.Defend, hero);
        Assert(hero.Hp == 95 && hero.Limit.Charge == 5, "Veneno permanece dano fixo, sem elemento e sem reducao por Defender.");

        var profile = Resist(DamageElement.Fire);
        var baseStats = new CombatantDefinition("Heroi", "Teste", 100, 60, 10, 11, 10, 20, profile, MateriaResolver.Attack);
        var member = new PartyMember("hero", "Heroi", CharacterClass.Healer, baseStats,
            new EquipmentDefinition("Arma", 0, 0, 2), new EquipmentDefinition("Armadura", 0, 0, 2, EquipmentSlot.Armor));
        hero = member.CreateCombatant();
        AbilityDefinition fire = Spell(DamageElement.Fire);
        enemy = Caster(fire, CombatTeam.Enemy);
        // A IA deve aplicar exatamente as mesmas regras elementais usadas pelo jogador.
        battle = new BattleSession(enemy, hero);
        Assert(EnemyAI.TryTakeTurn(battle, out _), "IA deve poder executar magia elemental.");
        Assert(hero.Hp == 80 && member.Limit.Charge == 20 && hero.Definition.Affinities == profile,
            "Snapshot da equipe deve conservar afinidades e compartilhar Limit sobre dano real.");
        Assert(member.CreateCombatant().Limit.Charge == 20 && member.CreateCombatant().Definition.Affinities == profile,
            "Proxima batalha conserva perfil e Limit sem compartilhar HP.");
    }

    private static void CheckPlayableEncounter()
    {
        PartyProgress party = PrototypePartyFactory.CreateTestParty();
        PartyMember aren = party.Members[0];
        Equip(party, aren, 2, FindMateria(party, "fire")); // All ja esta no encaixe 3.
        BattleSession battle = PrototypeBattleFactory.CreateEncounter(party);
        CombatantState hero = battle.Combatants[0], guard = battle.Combatants[3], soldier = battle.Combatants[4];
        AbilityDefinition fireAll = Command(hero, "fire");
        Assert(guard.Definition.Affinities.DamagePercent(DamageElement.Lightning) == 150
            && soldier.Definition.Affinities.DamagePercent(DamageElement.Ice) == 50, "Encontro deve usar os perfis anunciados na interface.");
        Act(battle, fireAll, guard);
        Assert(guard.Hp == 171 && soldier.Hp == 60 && hero.Mp == 19,
            "Fogo + All de Aren causa 9 no guarda resistente e 30 no soldado vulneravel, gastando 9 MP.");
        Assert(party.TryUnequip(aren, aren.Armor, 2, out _) && Command(hero, "fire").Element == DamageElement.Fire,
            "Mudanca de inventario nao altera o snapshot de uma batalha iniciada.");
    }

    public static int Run()
    {
        CheckDefinitions();
        CheckDamageAndRounding();
        CheckAreaAndProtection();
        CheckMateriaCombos();
        CheckUnrelatedEffectsAndSnapshots();
        CheckPlayableEncounter();
        return checks;
    }
}
