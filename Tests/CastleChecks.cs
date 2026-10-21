using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using DragonQuest.Prototype;

internal static class CastleChecks
{
    private static int checks;
    private static void Assert(bool condition,string text) {checks++;if(!condition) throw new Exception(text);}
    private static CombatantState Find(BattleSession battle,string id) => battle.Combatants.Single(c=>c.Id==id);
    private static CastleBattleRules Rules(BattleSession battle) => (CastleBattleRules)battle.Rules;
    private static PartyProgress Party(bool armor,int bearer=2)
    {
        var party=PrototypePartyFactory.Create();
        if(armor)
        {
            var item=new EquipmentLoadout(DragonProgress.Armor);
            Assert(party.TryAddEquipments(item)&&party.TryEquipEquipment(party.Members[bearer],item,out _),"Armadura pode ser usada por qualquer membro no castelo.");
        }
        return party;
    }
    private static BattleSession Create(PartyProgress party)
    {
        var original=PrototypeCastleBattleFactory.Create(party);
        return new BattleSession(()=>1d,original.Rules,original.Inventory,original.Combatants.ToArray());
    }
    private static void Defend(BattleSession battle)
    {
        var actor=battle.CurrentActor;
        if(actor.MindControlTurns>0) {Assert(battle.TryTakeMindControlTurn(.5,out _,out _),"Controle mental deve consumir uma acao automatica.");return;}
        Assert(battle.TryAct(actor,actor.Definition.Abilities.Single(a=>a.Effect==AbilityEffect.Defend),actor,out _),"Aliado deve poder defender durante a contagem.");
    }
    private static void BeforeKing(BattleSession battle,bool revive=false)
    {
        int steps=0;
        while(battle.Outcome==BattleOutcome.None&&battle.CurrentActor.Id!=CastleBattleRules.TyrantId&&steps++<20)
        {
            var actor=battle.CurrentActor;
            // Alvo fixo impede Silence aleatorio de bloquear a pluma de Lia neste roteiro.
            if(actor.Team==CombatTeam.Enemy) Assert(EnemyAI.TryTakeTurn(battle,0,out _),"Espirito deve agir durante a preparacao.");
            else if(revive&&actor.Id=="lia"&&!Find(battle,"bram").IsAlive)
                Assert(battle.TryUseItem(actor,PrototypePartyFactory.PhoenixFeather,Find(battle,"bram"),out _),"Pluma pode reviver o portador antes do meteoro.");
            else Defend(battle);
        }
        Assert(battle.Outcome==BattleOutcome.None&&battle.CurrentActor.Id==CastleBattleRules.TyrantId,"Contagem deve chegar ao turno do Rei Rubro.");
    }
    private static void Threshold(BattleSession battle)
    {
        var king=Find(battle,CastleBattleRules.TyrantId);
        king.TakeDamage(king.Hp-180);Rules(battle).AfterAction(battle);
    }
    private static void PrepareUntilMeteor(BattleSession battle)
    {
        Threshold(battle);
        for(int turn=0;turn<CastleBattleRules.MeteorCastTurns;turn++)
        {
            // Isola a contagem do meteoro sem deixar os golpes do espirito encerrarem o roteiro.
            foreach(var ally in battle.Combatants.Where(c=>c.Team==CombatTeam.Party&&c.IsAlive))
            { ally.RecoverHp(int.MaxValue);ally.GrantDamageShield(99,false); }
            BeforeKing(battle);
            Assert(EnemyAI.ChooseAbility(battle)==(turn==0?CastleBattleRules.Protect:CastleBattleRules.PrepareMeteor),"Protect inicia a conjuracao; os turnos seguintes mantem a carga.");
            if(turn==0) Assert(!battle.TryAct(battle.CurrentActor,CastleBattleRules.PrepareMeteor,battle.CurrentActor,out _),"Preparacao nao pode pular Protect.");
            Assert(!battle.TryAct(battle.CurrentActor,CastleBattleRules.Meteor,battle.CurrentActor,out _),"Lancamento antecipado deve ser recusado.");
            Assert(EnemyAI.TryTakeTurn(battle,out _)&&Rules(battle).PreparationsRemaining==CastleBattleRules.MeteorCastTurns-1-turn&&battle.Outcome==BattleOutcome.None,"Preparar nao pode explodir o castelo.");
            Assert(Find(battle,CastleBattleRules.TyrantId).IsCharging,"Pose de cast persiste entre todos os turnos.");
            Assert(Find(battle,CastleBattleRules.TyrantId).ShieldTurns==CastleBattleRules.MeteorCastTurns-turn,"Protect cobre cada turno anterior ao impacto.");
            Assert(Rules(battle).MeteorTurnsElapsed==turn+1,"Contagem incrementa uma vez por turno proprio da conjuracao.");
            int remaining=Rules(battle).PreparationsRemaining;
            Rules(battle).AfterAction(battle);
            Assert(Rules(battle).PreparationsRemaining==remaining&&Rules(battle).MeteorTurnsElapsed==turn+1,"Turnos alheios e verificacoes de estado nao aceleram o meteoro nem a contagem.");
        }
    }
    private static void CheckVisibilityAndThreshold()
    {
        var party=Party(false);var armor=new EquipmentLoadout(DragonProgress.Armor);party.TryAddEquipments(armor);
        var battle=Create(party);
        Assert(battle.Combatants.Count==4&&!Rules(battle).IsSpiritRevealed,"Armadura guardada nao revela o espirito.");
        var king=Find(battle,CastleBattleRules.TyrantId);
        king.TakeDamage(419);Rules(battle).AfterAction(battle);
        Assert(king.Hp==181&&!Rules(battle).MeteorStarted,"Acima de 30% nao inicia o meteoro.");
        king.TakeDamage(1);Rules(battle).AfterAction(battle);
        Assert(Rules(battle).MeteorStarted&&Rules(battle).PreparationsRemaining==5&&king.IsCharging,"Em exatamente 30% inicia a conjuracao de cinco turnos.");
        for(int bearer=0;bearer<3;bearer++)
        {
            battle=Create(Party(true,bearer));
            Assert(battle.Combatants.Count==5&&Find(battle,CastleBattleRules.SpiritId).IsAlive,"Espirito entra desde o inicio com armadura em qualquer classe.");
        }
        battle=Create(Party(false));king=Find(battle,CastleBattleRules.TyrantId);
        var crossingAttack=battle.CurrentActor.Definition.Abilities.Single(a=>a.Id=="attack");
        int crossingDamage=(int)Math.Max(1L,((long)battle.CurrentActor.Definition.Attack+crossingAttack.Power)*100/(100L+king.Definition.Defense));
        king.TakeDamage(king.Hp-(179+crossingDamage));
        Assert(battle.TryAct(battle.CurrentActor,battle.CurrentActor.Definition.Abilities.Single(a=>a.Id=="attack"),king,out _)
            &&king.Hp==179&&Rules(battle).MeteorStarted,"Golpe que cruza o limiar precisa ativar a contagem automaticamente.");
        battle=Create(Party(false));BeforeKing(battle);
        int turns=battle.CurrentActor.TurnsTaken,hp=Find(battle,CastleBattleRules.TyrantId).Hp;
        Assert(!battle.TryAct(battle.CurrentActor,CastleBattleRules.Meteor,battle.CurrentActor,out _)
            &&battle.CurrentActor.TurnsTaken==turns&&battle.CurrentActor.Hp==hp,"Nao pode lancar meteoro antes do limiar nem gastar um turno invalido.");
        Assert(EnemyAI.ChooseAbility(battle)==CastleBattleRules.RoyalStrike&&CastleBattleRules.RoyalStrike.Element==DamageElement.None
            &&CastleBattleRules.RoyalHeavy.Element==DamageElement.None,"Golpes normais do Rei Rubro devem ser fisicos e sem elemento.");
    }
    private static void CheckMeteor()
    {
        var battle=Create(Party(false));PrepareUntilMeteor(battle);BeforeKing(battle);
        Assert(EnemyAI.ChooseAbility(battle)==CastleBattleRules.Meteor&&EnemyAI.TryTakeTurn(battle,out _),"Sexto turno do Rei Rubro lanca o meteoro depois do numero um.");
        Assert(Rules(battle).MeteorTurnsElapsed==6,"O sexto turno e registrado ao resolver o lancamento.");
        Assert(battle.Outcome==BattleOutcome.Completed&&Rules(battle).Ending==GameEnding.CastleDestroyed
            &&battle.Combatants.All(c=>!c.IsAlive),"Meteoro ignora defender e derrota inclusive o Rei Rubro, com final de destruicao.");
        Assert(!new BattleRewards(new ExplorationProgress()).TryClaim(battle,out _),"Fim da historia nao pode pagar vitoria comum.");
        for(int bearer=0;bearer<3;bearer++)
        {
            battle=Create(Party(true,bearer));PrepareUntilMeteor(battle);BeforeKing(battle);
            var hp=battle.Combatants.Select(c=>c.Hp).ToArray();
            Assert(EnemyAI.TryTakeTurn(battle,out _)&&Rules(battle).MeteorAbsorbed&&Rules(battle).Ending==GameEnding.None
                &&hp.SequenceEqual(battle.Combatants.Select(c=>c.Hp)),"Portador vivo absorve tudo sem dano aos participantes ou ao castelo.");
            Assert(!Find(battle,CastleBattleRules.TyrantId).IsCharging&&Find(battle,CastleBattleRules.TyrantId).ShieldTurns==0,"Absorcao encerra conjuracao e Protect no sexto turno.");
            Assert(Rules(battle).MeteorAbsorberId==partyBearerId(bearer),"Animacao de absorcao aponta ao portador real.");
            BeforeKing(battle);
            Assert(EnemyAI.ChooseAbility(battle)!=CastleBattleRules.Meteor&&EnemyAI.ChooseAbility(battle)!=CastleBattleRules.PrepareMeteor,"Meteoro nao reinicia em loop abaixo de 30%.");
        }
        var party=Party(true);battle=Create(party);Find(battle,"bram").TakeDamage(150);
        PrepareUntilMeteor(battle);BeforeKing(battle);
        Assert(EnemyAI.TryTakeTurn(battle,out _)&&Rules(battle).Ending==GameEnding.CastleDestroyed,"Portador derrotado nao pode absorver o meteoro.");
        party=Party(true);party.Consumables.TryAdd(PrototypePartyFactory.PhoenixFeather,1,out _);
        battle=Create(party);Find(battle,"bram").TakeDamage(150);
        PrepareUntilMeteor(battle);BeforeKing(battle,true);
        Assert(EnemyAI.TryTakeTurn(battle,out _)&&Rules(battle).MeteorAbsorbed&&party.Consumables.QuantityOf(PrototypePartyFactory.PhoenixFeather)==0,"Reviver antes do lancamento restaura a absorcao e gasta a pluma.");
    }
    private static void CheckEndings()
    {
        var battle=Create(Party(true));Threshold(battle);
        var spirit=Find(battle,CastleBattleRules.SpiritId);spirit.TakeDamage(spirit.Hp-1);
        Assert(battle.TryAct(battle.CurrentActor,battle.CurrentActor.Definition.Abilities.Single(a=>a.Id=="attack"),spirit,out _)
            &&Rules(battle).Ending==GameEnding.KingFreed&&Find(battle,CastleBattleRules.TyrantId).IsAlive
            &&!Find(battle,CastleBattleRules.TyrantId).IsCharging&&Rules(battle).PreparationsRemaining==0,"Matar espirito com rei vivo encerra a luta e cancela o meteoro.");
        Assert(!EnemyAI.TryTakeTurn(battle,out _)&&battle.CurrentActor==null,"Nao deve haver ataques depois do final.");
        battle=Create(Party(false));var king=Find(battle,CastleBattleRules.TyrantId);king.TakeDamage(king.Hp-1);
        Assert(battle.TryAct(battle.CurrentActor,battle.CurrentActor.Definition.Abilities.Single(a=>a.Id=="attack"),king,out _)
            &&Rules(battle).Ending==GameEnding.HeroPossessed,"Matar Rei Rubro sem armadura deve levar a possessao de Aren.");
        var party=Party(true);var cleave=new MateriaInstance("ending-cleave",PrototypePartyFactory.CleaveMateria);party.TryAddMaterias(cleave);party.TryEquip(party.Members[0],party.Members[0].Armor,2,cleave,out _);
        battle=Create(party);
        foreach(var enemy in battle.Combatants.Where(c=>c.Team==CombatTeam.Enemy)) enemy.TakeDamage(enemy.Hp-1);
        Assert(battle.TryAct(battle.CurrentActor,battle.CurrentActor.Definition.Abilities.Single(a=>a.SourceAbilityId=="cleave"),Find(battle,CastleBattleRules.SpiritId),out _)
            &&Rules(battle).Ending==GameEnding.HeroPossessed,"Area que mata rei e espirito simultaneamente nao pode dar o final bom.");
        foreach(var ending in new[]{GameEnding.KingFreed,GameEnding.CastleDestroyed,GameEnding.HeroPossessed})
            Assert(EndingNarrative.Pages(ending).Length==3&&!string.IsNullOrEmpty(EndingNarrative.Title(ending)),"Tres finais precisam de suas cenas e textos completos.");
    }
    private static void CheckSpiritEffects()
    {
        var hero=new CombatantState("hero",new CombatantDefinition("Heroi","Aliado",10,0,1,0,0,5,PrototypeBattleFactory.Attack),CombatTeam.Party);
        var spirit=new CombatantState("spirit",new CombatantDefinition("Espirito","Inimigo",100,0,0,0,30,20,PrototypeCastleBattleFactory.DrainLife),CombatTeam.Enemy);
        spirit.TakeDamage(80);var battle=new BattleSession(hero,spirit);
        Assert(EnemyAI.TryTakeTurn(battle,out _)&&hero.Hp==0&&spirit.Hp==30,"Drenagem recupera apenas HP realmente perdido pelo alvo, sem excesso do golpe fatal.");
        hero=new CombatantState("hero",new CombatantDefinition("Heroi","Aliado",100,20,1,0,0,5,PrototypeBattleFactory.Attack),CombatTeam.Party);
        spirit=new CombatantState("spirit",new CombatantDefinition("Espirito","Inimigo",100,0,0,0,0,20,PrototypeCastleBattleFactory.Chains),CombatTeam.Enemy);
        battle=new BattleSession(hero,spirit);
        Assert(EnemyAI.TryTakeTurn(battle,out _)&&hero.TurnsTaken==1&&hero.IsBound&&hero.Mp==20&&hero.Limit.Charge==0,"Correntes impede a acao e permanece sem gastar MP nem Limit.");
        Assert(battle.LastMessage.Contains("acorrentado")&&battle.CurrentActor==spirit,"Perda de acao deve ter feedback e avancar ordem de turnos.");
        hero.BoundActions=1;hero.TakeDamage(100,damageOverTime:true);hero.RecoverHp(30);
        Assert(hero.BoundActions==0,"Morte e reviver nao podem herdar correntes antigas.");
    }
    private static void CheckFullGoodAndDefeat()
    {
        var party=Party(true);var ice=new MateriaInstance("castle-ice",PrototypePartyFactory.IceMateria);party.TryAddMaterias(ice);party.TryEquip(party.Members[1],party.Members[1].Armor,2,ice,out _);
        foreach(var member in party.Members) member.Progression.AddExperience(CharacterProgression.ExperienceForLevel(20));
        party.RecoverAtInn();
        party.Consumables.TryAdd(PrototypePartyFactory.Ether,2,out _);
        var battle=Create(party);int actions=0;
        while(battle.Outcome==BattleOutcome.None&&actions++<250)
        {
            var actor=battle.CurrentActor;
            if(actor.MindControlTurns>0) {Assert(battle.TryTakeMindControlTurn(.5,out _,out _),"Confusao deve progredir no castelo.");continue;}
            if(actor.Team==CombatTeam.Enemy) {Assert(EnemyAI.TryTakeTurn(battle,out _),"IA deve progredir em combate completo do castelo.");continue;}
            AbilityDefinition ability=actor.Definition.Abilities.Single(a=>a.Id=="attack");var target=Find(battle,CastleBattleRules.SpiritId);
            if(actor.Id=="lia"&&actor.Mp<6&&battle.Inventory.QuantityOf(PrototypePartyFactory.Ether)>0)
            {Assert(battle.TryUseItem(actor,PrototypePartyFactory.Ether,actor,out _),"Eter deve permitir sustentar a equipe.");continue;}
            if(actor.Id=="bram"&&Find(battle,CastleBattleRules.TyrantId).TauntTurns==0&&actor.Mp>=3)
            {ability=actor.Definition.Abilities.Single(a=>a.Effect==AbilityEffect.Taunt);target=Find(battle,CastleBattleRules.TyrantId);}
            else if(actor.Id=="lia"&&actor.Mp>=6&&!actor.IsSilenced)
            {
                var wounded=battle.Combatants.Where(c=>c.Team==CombatTeam.Party&&c.IsAlive).OrderBy(c=>(double)c.Hp/c.Definition.MaxHp).First();
                if(wounded.Hp*100<wounded.Definition.MaxHp*65) {ability=actor.Definition.Abilities.Single(a=>a.Effect==AbilityEffect.Heal&&!a.UsesLimit);target=wounded;}
                else ability=actor.Definition.Abilities.Single(a=>a.Element==DamageElement.Ice);
            }
            else if(actor.Id=="aren"&&actor.Mp>=4) ability=actor.Definition.Abilities.FirstOrDefault(a=>a.SourceAbilityId=="heavy-strike")??ability;
            Assert(battle.TryAct(actor,ability,target,out _),"Materias, cura e provocar devem funcionar na luta final.");
        }
        Assert(actions<250&&Rules(battle).Ending==GameEnding.KingFreed,"Equipe de nivel 20 com armadura, Gelo e dois eteres deve poder libertar o rei.");
        battle=Create(Party(false));actions=0;
        while(battle.Outcome==BattleOutcome.None&&actions++<500)
            if(battle.CurrentActor.Team==CombatTeam.Enemy) Assert(EnemyAI.TryTakeTurn(battle,out _),"IA deve permitir uma derrota comum."); else Defend(battle);
        Assert(battle.Outcome==BattleOutcome.Defeat&&Rules(battle).Ending==GameEnding.None&&!Rules(battle).MeteorStarted,"Derrota por golpes normais nao pode virar final nem iniciar meteoro com rei saudavel.");
        Assert(battle.Combatants.Where(c=>c.Team==CombatTeam.Party).Any(c=>c.Limit.Charge>0),"Derrota comum conserva Limit para uma tentativa seguinte.");
    }
    private static string partyBearerId(int index) => new[]{"aren","lia","bram"}[index];
    private static void CheckProtectDamage()
    {
        var physical=new AbilityDefinition("hit","Ataque","",0,1,AbilityEffect.PhysicalDamage,TargetRule.Enemy);
        var magic=new AbilityDefinition("spell","Magia","",0,11,AbilityEffect.MagicDamage,TargetRule.Enemy);
        var wait=new AbilityDefinition("wait","Esperar","",0,0,AbilityEffect.Prepare,TargetRule.Self);
        var hero=new CombatantState("hero",new CombatantDefinition("Heroi","Aliado",500,20,20,0,24,20,physical,magic),CombatTeam.Party);
        var king=new CombatantState("tyrant",new CombatantDefinition("Rei","Inimigo",500,0,1,10,0,5,wait),CombatTeam.Enemy);
        king.GrantDamageShield(3,false);
        var battle=new BattleSession(hero,king);
        Assert(battle.TryAct(hero,physical,king,out _)&&king.Hp==490,"Protect reduz 19 de dano fisico para 10.");
        Assert(king.VitalChanges.Last().IsShielded,"Impacto protegido registra o escudo para a animacao.");
        Assert(battle.TryAct(king,wait,king,out _)&&battle.TryAct(hero,magic,king,out _)&&king.Hp==474,"Protect reduz 31 de dano magico para 16.");
        king.TakeDamage(int.MaxValue);
        Assert(!king.IsAlive&&king.ShieldTurns==0&&king.VitalChanges.Last().IsShielded,"Um golpe fatal ainda guarda o evento visual de escudo.");
        battle=Create(Party(false));
        PrepareUntilMeteor(battle);BeforeKing(battle);
        foreach(var member in battle.Combatants) member.GrantDamageShield(5,false);
        Assert(EnemyAI.TryTakeTurn(battle,out _)&&battle.Combatants.All(c=>!c.IsAlive),"O impacto aniquilador nao deixa sobreviventes por causa de Shield ou Protect.");
    }
    public static int Run() {CheckProtectDamage();CheckVisibilityAndThreshold();CheckMeteor();CheckEndings();CheckSpiritEffects();CheckFullGoodAndDefeat();return checks;}
}
