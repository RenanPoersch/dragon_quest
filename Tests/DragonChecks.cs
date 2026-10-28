using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using DragonQuest.Shops;

internal static class DragonChecks
{
    private static int checks;
    private static void Assert(bool condition,string description) { checks++;if(!condition) throw new Exception(description); }
    private static CombatantState Dragon(BattleSession battle) => battle.Combatants.Single(c=>c.Id==DragonProgress.EnemyId);
    private static void Defend(BattleSession battle)
    {
        var actor=battle.CurrentActor;
        Assert(battle.TryAct(actor,actor.Definition.Abilities.Single(a=>a.Effect==AbilityEffect.Defend),actor,out _),"Aliado deve poder defender antes do sopro.");
    }

    private static void CheckTelegraph()
    {
        var battle=PrototypeBattleFactory.CreateDragonEncounter(PrototypePartyFactory.Create());
        var dragon=Dragon(battle);
        Assert(dragon.Definition.Affinities.DamagePercent(DamageElement.Ice)==150
            &&dragon.Definition.Affinities.DamagePercent(DamageElement.Fire)==50,"Dragao exige fraqueza a Gelo e resistencia a Fogo.");
        for(int round=0;round<2;round++)
        {
            while(battle.CurrentActor.Team==CombatTeam.Party) Defend(battle);
            Assert(EnemyAI.ChooseAbility(battle)==(round==0?PrototypeBattleFactory.DragonClaw:PrototypeBattleFactory.ChargeBreath),"Dragao deve seguir garra, preparacao, sopro.");
            Assert(EnemyAI.TryTakeTurn(battle,out _),"IA do dragao deve avancar.");
        }
        Assert(dragon.IsCharging&&battle.LastMessage.Contains("proximo turno"),"Aviso deve permanecer durante os turnos aliados.");
        var party=battle.Combatants.Where(c=>c.Team==CombatTeam.Party).ToArray();
        var hp=party.Select(c=>c.Hp).ToArray();int responses=0;
        while(battle.CurrentActor.Team==CombatTeam.Party) { Defend(battle);responses++; }
        Assert(responses==3,"Cada aliado vivo precisa de um turno de resposta antes do sopro.");
        Assert(EnemyAI.ChooseAbility(battle)==PrototypeBattleFactory.DragonBreath&&EnemyAI.TryTakeTurn(battle,out _),"Sopro deve acontecer no turno seguinte a preparacao.");
        var damage=new[]{107,109,98};
        for(int i=0;i<party.Length;i++) Assert(hp[i]-party[i].Hp==Math.Min(hp[i],damage[i]),"Defender deve reduzir o sopro para todo aliado que escolheu a acao.");
        Assert(!dragon.IsCharging,"Aviso deve terminar depois do sopro.");
        while(battle.CurrentActor.Team==CombatTeam.Party) Defend(battle);
        Assert(EnemyAI.ChooseAbility(battle)==PrototypeBattleFactory.DragonClaw,"Ciclo deve reiniciar sem sopro surpresa.");
    }

    private static BattleSession Win(PartyProgress party)
    {
        var ice=new MateriaInstance("dragon-test-ice",PrototypePartyFactory.IceMateria);
        Assert(party.TryAddMaterias(ice)&&party.TryEquip(party.Members[1],party.Members[1].Armor,2,ice,out _),"Gelo comprado deve poder ser equipado por Lia.");
        var battle=PrototypeBattleFactory.CreateDragonEncounter(party);
        int turns=0;
        while(battle.Outcome==BattleOutcome.None && turns++<250)
        {
            var actor=battle.CurrentActor;
            if(actor.Team==CombatTeam.Enemy) { Assert(EnemyAI.TryTakeTurn(battle,out _),"IA deve concluir uma batalha completa.");continue; }
            if(Dragon(battle).IsCharging) { Defend(battle);continue; }
            AbilityDefinition action=actor.Definition.Abilities.Single(a=>a.Id=="attack");
            var heal=actor.Definition.Abilities.FirstOrDefault(a=>a.Effect==AbilityEffect.Heal);
            if(heal!=null&&actor.Mp>=heal.MpCost && battle.Combatants.Any(c=>c.Team==CombatTeam.Party&&c.IsAlive&&c.Hp*2<c.Definition.MaxHp)) action=heal;
            else
            {
                var special=actor.Definition.Abilities.FirstOrDefault(a=>a.Element==DamageElement.Ice)
                    ??actor.Definition.Abilities.FirstOrDefault(a=>a.Id=="heavy-strike");
                if(special!=null&&actor.Mp>=special.MpCost) action=special;
            }
            var target=battle.GetValidTargets(action).OrderBy(c=>(double)c.Hp/c.Definition.MaxHp).First();
            Assert(battle.TryAct(actor,action,target,out _),"Estrategia de cura, gelo e defesa deve progredir.");
        }
        Assert(turns<250&&battle.Outcome==BattleOutcome.Victory,"Equipe inicial com Gelo e defesa do sopro deve vencer o boss.");
        return battle;
    }

    private static void CheckRewardsAndArmor()
    {
        var party=PrototypePartyFactory.Create();var progress=new DragonProgress();
        Assert(!progress.TryComplete(null,party)&&!progress.TryComplete(PrototypeBattleFactory.CreateDragonEncounter(party),party),"Armadura exige vitoria concluida.");
        var other=new CombatantState("other-enemy",new CombatantDefinition("Outro","Inimigo",1,0,1,0,0,1,PrototypeBattleFactory.Attack),CombatTeam.Enemy);
        var wrongBattle=new BattleSession(party.Members[0].CreateCombatant(),other);
        wrongBattle.TryAct(wrongBattle.CurrentActor,wrongBattle.CurrentActor.Definition.Abilities.Single(a=>a.Id=="attack"),other,out _);
        Assert(!progress.TryComplete(wrongBattle,party),"Vencer outro inimigo nao entrega a recompensa do dragao.");
        var victory=Win(party);var gold=new ExplorationProgress(50);var rewards=new BattleRewards(gold);
        Assert(rewards.TryClaim(victory,out _)&&gold.Gold==150&&!rewards.TryClaim(victory,out _),"Dragao deve pagar suas 100 moedas apenas uma vez.");
        Assert(progress.TryComplete(victory,party)&&progress.IsDefeated&&!progress.TryComplete(victory,party),"Armadura deve ser entregue apenas uma vez.");
        var armor=party.Equipments.Single(e=>e.Definition==DragonProgress.Armor);
        Assert(armor.Sockets.Count==4&&armor.LinkedSocket(0)==1&&armor.LinkedSocket(2)==3,"Armadura exige dois pares ligados.");
        Assert(!progress.HasEquippedArmor(party),"Guardar a armadura nao deve contar como usa-la para revelar o espirito.");
        foreach(var member in party.Members)
        {
            Assert(party.TryEquipEquipment(member,armor,out _)&&progress.HasEquippedArmor(party),"Qualquer classe pode usar a armadura.");
            Assert(member.Defense==member.BaseStats.Defense+(member.Class==CharacterClass.Tank?20:16),"Tank recebe 25% sobre a defesa base da armadura.");
            Assert(member.CreateCombatant().Definition.Affinities.DamagePercent(DamageElement.Fire)==50,"Resistencia equipada deve entrar na batalha.");
            var state=member.CreateCombatant();
            var flame=new AbilityDefinition("test-flame","Fogo","",0,0,AbilityEffect.MagicDamage,TargetRule.Enemy,element:DamageElement.Fire);
            var caster=new CombatantState("caster",new CombatantDefinition("Chama","Inimigo",100,0,0,0,60,99,flame),CombatTeam.Enemy);
            var fireBattle=new BattleSession(state,caster);
            Assert(EnemyAI.TryTakeTurn(fireBattle,out _)&&state.Definition.MaxHp-state.Hp==(member.Id=="aren"?72:member.Id=="lia"?76:52),"Armadura deve cortar o dano de Fogo pela metade depois da defesa numerica.");
            Assert(party.TryRemoveEquipment(member,EquipmentSlot.Armor,out _)&&!progress.HasEquippedArmor(party),"Remover a armadura retira sua condicao narrativa.");
            Assert(member.CreateCombatant().Definition.Affinities.DamagePercent(DamageElement.Fire)==100,"Resistencia nao pode ficar no personagem depois de desequipar.");
        }
        var service=PrototypeShopFactory.Create(party,gold);var shop=service.FindShop("equipment-shop");
        var entry=service.SaleEntries(shop).Single(e=>e.Equipment==armor);
        Assert(!entry.CanSell&&entry.BlockReason.Contains("historia")&&!service.TrySell(shop,entry,1,out _),"Armadura de historia deve ficar protegida na loja.");
        Assert(!party.TryRemoveOwnedEquipment(armor),"Protecao deve existir tambem na regra de inventario.");
        Assert(new DragonProgress().IsDefeated==false,"Nova sessao nao pode herdar a vitoria.");
        var combined=ElementalAffinities.Combine(new ElementalAffinities(new[]{DamageElement.Fire},new[]{DamageElement.Lightning}),DragonProgress.Armor.Affinities,DragonProgress.Armor.Affinities);
        Assert(combined.DamagePercent(DamageElement.Fire)==50&&combined.DamagePercent(DamageElement.Lightning)==50&&combined.DamagePercent(DamageElement.Ice)==100,"Equipamentos combinam protecoes sem multiplicar resistencias duplicadas; protecao supera fraqueza.");
    }

    private static void CheckDefeatAndRetry()
    {
        var party=PrototypePartyFactory.Create();var battle=PrototypeBattleFactory.CreateDragonEncounter(party);
        bool used=false;int actions=0;
        while(battle.Outcome==BattleOutcome.None&&actions++<250)
        {
            var actor=battle.CurrentActor;
            if(actor.Team==CombatTeam.Enemy) { Assert(EnemyAI.TryTakeTurn(battle,out _),"IA deve continuar ate a derrota.");continue; }
            if(!used&&battle.GetValidItemTargets(PrototypePartyFactory.Potion).Count>0)
            { Assert(battle.TryUseItem(actor,PrototypePartyFactory.Potion,battle.GetValidItemTargets(PrototypePartyFactory.Potion)[0],out _),"Uso de pocao deve consumir uma unidade.");used=true; }
            else Defend(battle);
        }
        Assert(battle.Outcome==BattleOutcome.Defeat&&!new DragonProgress().TryComplete(battle,party),"Derrota nao entrega armadura nem completa o boss.");
        var charges=party.Members.Select(m=>m.Limit.Charge).ToArray();
        Assert(charges.Any(c=>c>0)&&used&&party.Consumables.QuantityOf(PrototypePartyFactory.Potion)==1,"Derrota conserva dano no Limit e consumiveis gastos.");
        var retry=PrototypeBattleFactory.CreateDragonEncounter(party);
        foreach(var member in party.Members)
        {
            var state=retry.Combatants.Single(c=>c.Id==member.Id);
            Assert(state.Hp==state.Definition.MaxHp&&state.Mp==state.Definition.MaxMp&&state.Limit.Charge==charges[Array.IndexOf(party.Members.ToArray(),member)],"Tentativa seguinte inicia recuperada sem zerar Limit.");
        }
        Assert(!Dragon(retry).IsCharging&&Dragon(retry).TurnsTaken==0,"Tentativa seguinte nao herda fase do sopro.");
    }

    public static int Run() { CheckTelegraph();CheckRewardsAndArmor();CheckDefeatAndRetry();return checks; }
}
