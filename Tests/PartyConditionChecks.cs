using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Prototype;

internal static class PartyConditionChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static CombatantState Find(BattleSession battle, string id) => battle.Combatants.First(c => c.Id == id);

    private static void CheckBattlePersistence()
    {
        var party = PrototypePartyFactory.CreateTestParty();
        var battle = PrototypeBattleFactory.CreateEncounter(party);
        var aren = Find(battle, "aren"); var lia = Find(battle, "lia"); var bram = Find(battle, "bram");
        aren.TakeDamage(23); aren.SpendMp(9); aren.PoisonTurns = 3;
        aren.IsDefending = true; aren.GrantDamageShield(3, false);
        lia.BoundActions = 1; bram.TakeDamage(bram.Hp);
        party.CaptureBattleState(battle);
        int hp = aren.Hp, mp = aren.Mp;
        foreach (var next in new[] { PrototypeBattleFactory.CreateEncounter(party),
            PrototypeBattleFactory.CreateRegionEncounter(party, RandomEncounterType.Wolf, 3),
            PrototypeBattleFactory.CreateDragonEncounter(party), PrototypeCastleBattleFactory.Create(party) })
        {
            var hero = Find(next, "aren"); var healer = Find(next, "lia"); var tank = Find(next, "bram");
            Check(hero.Hp == hp && hero.Mp == mp && hero.IsPoisoned, "HP, MP e veneno persistem em todos os tipos de encontro.");
            Check(healer.BoundActions == 1, "Correntes pendentes persistem entre encontros.");
            Check(!tank.IsAlive && next.CurrentActor != tank, "Derrotados continuam na equipe e nao agem.");
            Check(!hero.IsDefending && hero.ShieldTurns == 0 && hero.TurnsTaken == 0 && hero.VitalChanges.Count == 0,
                "Defesa, buffs e historico visual iniciam sem efeitos antigos.");
        }
        var nextBattle = PrototypeBattleFactory.CreateEncounter(party);
        Check(nextBattle.GetValidItemTargets(PrototypePartyFactory.PhoenixFeather).Contains(Find(nextBattle, "bram")), "Aliado que entrou derrotado pode ser revivido na batalha.");
        Check(nextBattle.TryAct(nextBattle.CurrentActor, MateriaResolver.Defend, nextBattle.CurrentActor, out _), "Aliado vivo age com companheiro derrotado.");
        Check(Find(nextBattle, "lia").BoundActions == 0 && Find(nextBattle, "lia").TurnsTaken == 1, "Correntes herdadas consomem a proxima acao.");
        Check(party.Members[0].Hp == hp && aren.Hp == hp, "Instancias de encontros e estado do mapa permanecem independentes.");
        var revival = PrototypeBattleFactory.CreateEncounter(party);
        Check(revival.TryUseItem(revival.CurrentActor, PrototypePartyFactory.PhoenixFeather, Find(revival, "bram"), out _)
            && Find(revival, "bram").Hp == 45, "Reviver funciona em personagem derrotado desde o encontro anterior.");
        party.CaptureBattleState(revival);
        Check(party.Members[2].Hp == 45, "HP recuperado em batalha permanece na equipe.");

        party.RecoverAtInn();
        Check(party.Members.All(m => m.Hp == m.MaxHp && m.Mp == m.MaxMp && !m.IsPoisoned && m.BoundActions == 0), "Recuperacao da hospedaria restaura a equipe.");
        foreach (var member in party.Members) member.RestoreVitals(0, 0, false, 0);
        bool rejected = false;
        try { PrototypeBattleFactory.CreateEncounter(party); } catch (ArgumentException) { rejected = true; }
        Check(rejected && !party.HasLivingMembers, "Equipe toda derrotada nao inicia batalha.");
    }

    private static void CheckVictoryTransfer()
    {
        var party = PrototypePartyFactory.Create(); var member = party.Members[0];
        member.RestoreVitals(61, 7, true, 0);
        var battle = PrototypeBattleFactory.CreateRegionEncounter(party, RandomEncounterType.Wolf, 1);
        var enemy = battle.Combatants.First(c => c.Team == CombatTeam.Enemy);
        enemy.TakeDamage(enemy.Hp - 1);
        Check(battle.TryAct(battle.CurrentActor, MateriaResolver.Attack, enemy, out _) && battle.Outcome == BattleOutcome.Victory,
            "Vitoria real encerra o encontro com aliado envenenado.");
        party.CaptureBattleState(battle);
        Check(member.Hp == 61 && member.Mp == 7 && member.IsPoisoned, "Vitoria preserva vitais e nao cura veneno.");
        party.AdvancePoison(3);
        Check(member.Hp == 56 && member.IsPoisoned, "Veneno continua no mapa depois da vitoria.");
        var next = PrototypeBattleFactory.CreateDragonEncounter(party);
        Check(Find(next, "aren").Hp == 56 && Find(next, "aren").Mp == 7 && Find(next, "aren").IsPoisoned,
            "Proxima batalha recebe exatamente o estado deixado no mapa.");
        foreach (var hero in party.Members) hero.RestoreVitals(1, 0, true, 0);
        party.AdvancePoison(3);
        Check(!party.HasLivingMembers && party.Members.All(m => m.Hp == 0 && !m.IsPoisoned), "Poison pode derrotar toda a equipe no mapa.");
        party.RecoverAtInn();
        Check(party.HasLivingMembers && party.Members.All(m => m.Hp == m.MaxHp && m.Mp == m.MaxMp), "Recuperacao apos derrota no mapa restaura HP/MP.");
    }

    private static void CheckMapPoison()
    {
        var party = PrototypePartyFactory.Create(); var member = party.Members[0];
        member.RestoreVitals(81, 7, true, 1);
        int pulses = 0, damage = 0;
        party.PoisonDamage += (target, amount) => { Check(target == member && amount > 0, "Pulso informa personagem e dano real."); pulses++; damage += amount; };
        party.AdvancePoison(2); party.AdvancePoison(100, paused: true);
        party.AdvancePoison(float.NaN); party.AdvancePoison(float.PositiveInfinity); party.AdvancePoison(-1);
        Check(member.Hp == 81 && pulses == 0, "Pausas e deltas invalidos nao causam dano.");
        party.AdvancePoison(1);
        Check(member.Hp == 76 && member.Mp == 7 && pulses == 1 && member.IsPoisoned, "Mapa aplica 5% a cada tres segundos sem gastar MP.");
        party.AdvancePoison(6);
        Check(member.Hp == 66 && pulses == 3 && member.IsPoisoned && member.BoundActions == 1, "Veneno continua no mapa sem expirar correntes.");
        int oldLimit = member.Limit.Charge;
        party.AdvancePoison(1000);
        Check(member.Hp == 0 && !member.IsPoisoned && member.BoundActions == 0 && damage == 81, "Veneno reduz HP ate zero, sem valor negativo, e morte limpa condicoes.");
        Check(member.Limit.Charge >= oldLimit && party.HasLivingMembers, "Dano preserva Limit e permite continuar com outros aliados vivos.");
        int previous = pulses; party.AdvancePoison(100);
        Check(pulses == previous, "Personagem morto nao recebe novos pulsos.");

        member.RestoreVitals(member.MaxHp, 0, true, 0);
        var state = member.CreateCombatant(); state.TickPoison();
        var change = state.VitalChanges.Last();
        Check(change.Amount == -5 && change.IsDamageOverTime && !change.IsCritical && !change.IsMp, "Dano periodico tem metadado proprio para numero roxo.");
        state.TakeDamage(1);
        Check(!state.VitalChanges.Last().IsDamageOverTime, "Dano direto permanece distinguivel de DOT.");
        Check(CombatStatuses.Active(state).First(s => s.Status == CombatStatus.Poison).Turns == 0, "Veneno persistente nao exibe contagem de expiracao.");
        party.AdvancePoison(2);
        Check(party.TryUseItem(PrototypePartyFactory.Antidote, member, out _), "Antidoto pode ser usado durante exploracao.");
        party.AdvancePoison(100);
        int before = member.Hp;
        member.RestoreVitals(before, 0, true, 0); party.AdvancePoison(1);
        Check(member.Hp == before, "Novo envenenamento nao herda tempo de veneno ja curado.");
    }

    private static void CheckMapItems()
    {
        var party = PrototypePartyFactory.CreateTestParty(); var member = party.Members[0];
        member.RestoreVitals(11, 0, true, 0);
        Check(party.TryUseItem(PrototypePartyFactory.Potion, member, out _) && member.Hp == 51 && member.IsPoisoned, "Pocao recupera HP sem remover veneno.");
        Check(party.TryUseItem(PrototypePartyFactory.Ether, member, out _) && member.Mp == 15, "Eter recupera MP no mapa.");
        int antidotes = party.Consumables.QuantityOf(PrototypePartyFactory.Antidote);
        Check(party.TryUseItem(PrototypePartyFactory.Antidote, member, out _) && !member.IsPoisoned, "Antidoto remove veneno.");
        Check(party.Consumables.QuantityOf(PrototypePartyFactory.Antidote) == antidotes - 1, "Item usado gasta uma unidade.");
        Check(!party.TryUseItem(PrototypePartyFactory.Antidote, member, out _) && party.Consumables.QuantityOf(PrototypePartyFactory.Antidote) == antidotes - 1, "Uso sem efeito nao gasta item.");
        member.RestoreVitals(0, 3, false, 0);
        Check(!party.TryUseItem(PrototypePartyFactory.Potion, member, out _), "Pocao nao revive derrotado.");
        Check(party.TryUseItem(PrototypePartyFactory.PhoenixFeather, member, out _) && member.Hp == 30 && member.Mp == 3, "Reviver no mapa recupera 30% de HP e preserva MP.");
    }

    private static void CheckStatusColors()
    {
        var poison = BattleStatusColors.Tint(true, 0);
        Check(poison.g > poison.r && poison.g > poison.b, "Sprite envenenado recebe verde.");
        var bound = BattleStatusColors.Tint(false, 1);
        Check(bound.b > bound.r && bound.b > bound.g, "Correntes aplicam azul/lilas.");
        var taunted = BattleStatusColors.Tint(false, 0, 1);
        Check(taunted.r > taunted.g && taunted.r > taunted.b, "Provocar aplica vermelho/laranja.");
        Check(BattleStatusColors.Tint(true, 1, 1).Equals(poison), "Veneno tem prioridade visual em condicoes simultaneas.");
        Check(BattleStatusColors.Tint(false, 0).Equals(UnityEngine.Color.white), "Sprite volta a cor normal sem condicoes.");
        var dot = BattleStatusColors.VitalChange(new CombatVitalChange(1, -5, false, false, damageOverTime: true));
        Check(dot.r > dot.g && dot.b > dot.r, "Dano DOT usa numero roxo.");
        Check(BattleStatusColors.VitalChange(new CombatVitalChange(1, -5, false, false)).Equals(UnityEngine.Color.white), "Dano comum conserva branco.");
        var critical = BattleStatusColors.VitalChange(new CombatVitalChange(1, -5, true, false));
        Check(critical.r > critical.b && critical.g > critical.b, "Critico conserva amarelo.");
    }

    public static int Main()
    {
        try
        {
            CheckBattlePersistence(); CheckVictoryTransfer(); CheckMapPoison(); CheckMapItems(); CheckStatusColors();
            checks += AbilityChecks.RunPoisonChecks(); checks += EquipmentItemChecks.RunItemChecks();
            Console.WriteLine("Condicoes e itens: " + checks + " verificacoes passaram."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
