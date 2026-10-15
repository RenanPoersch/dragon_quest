using System;
using System.Linq;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using DragonQuest.Progression;

public static class CastlePresentationChecks
{
    private static int checks;
    public static void Main()
    {
        checks=CastleChecks.Run();
        CheckCastCounter();
        var rules=new CastleBattleRules(PrototypePartyFactory.Create());
        Check(!CastleMeteorPresentation.HoldCastPose(rules,false)&&CastleMeteorPresentation.HoldCastPose(rules,true),"A queda preserva a pose mesmo apos resolver o combate.");
        Check(BattleAnimationCatalog.Resource("tyrant",CastleBattleRules.Protect)=="TyrantBattle","Shield usa a postura normal antes da conjuracao.");
        Check(BattleAnimationCatalog.Resource("tyrant",CastleBattleRules.PrepareMeteor)=="TyrantMeteorCharge","A conjuracao usa o sprite do rei.");
        Check(BattleAnimationCatalog.Duration("tyrant",CastleBattleRules.Protect)==CastleMeteorPresentation.ProtectDuration,"Shield apresenta os quatro quadros completos antes da conjuracao.");
        Check(BattleAnimationCatalog.ImpactDelay("tyrant",CastleBattleRules.Meteor)==CastleMeteorPresentation.ImpactDelay,"Danos e derrotas continuam sincronizados ao impacto.");
        Check(CastleMeteorPresentation.MeteorFrame(CastleMeteorPresentation.ImpactDelay-.001f)<2
            &&CastleMeteorPresentation.MeteorFrame(CastleMeteorPresentation.ImpactDelay)==2,"Explosao so comeca no impacto.");
        Check(CastleMeteorPresentation.AbsorptionStart<CastleMeteorPresentation.ImpactDelay
            &&CastleMeteorPresentation.Duration>CastleMeteorPresentation.ImpactDelay+1,"Absorcao e impacto conservam sua duracao.");
        Console.WriteLine(checks+" verificacoes do castelo, contagem e sequencia Shield/meteoro passaram.");
    }
    private static void CheckCastCounter()
    {
        var source=PrototypeCastleBattleFactory.Create(PrototypePartyFactory.Create());
        var battle=new BattleSession(()=>1d,source.Rules,source.Inventory,source.Combatants.ToArray());
        var rules=(CastleBattleRules)battle.Rules;
        var king=battle.Combatants.Single(c=>c.Id==CastleBattleRules.TyrantId);
        king.TakeDamage(480);rules.AfterAction(battle);
        var counter=new CastleCastCounter();
        Check(rules.MeteorTurnsElapsed==0&&!counter.IsVisible(0),"Atingir o limiar nao conta como turno concluido.");
        Check(!CastleMeteorPresentation.HoldCastPose(rules,false),"O rei aguarda Shield na postura normal.");
        for(int turn=1;turn<=CastleBattleRules.MeteorCastTurns;turn++)
        {
            int guard=0;
            while(battle.CurrentActor.Id!=CastleBattleRules.TyrantId&&guard++<10)
            {
                var actor=battle.CurrentActor;
                Check(battle.TryAct(actor,actor.Definition.Abilities.Single(a=>a.Effect==AbilityEffect.Defend),actor,out _),"Aliados agem entre os turnos do cast.");
            }
            var ability=EnemyAI.ChooseAbility(battle);
            Check(EnemyAI.TryTakeTurn(battle,out _),"Cada turno do tirano progride.");
            Check(rules.MeteorTurnsElapsed==turn,"A contagem segue os cinco turnos proprios.");
            float start=turn*10;
            counter.Observe(CastleBattleRules.TyrantId,ability,rules.MeteorTurnsElapsed,start);
            Check(counter.Value==turn,"Apenas o numero do turno atual e exibido.");
            if(turn==1)
            {
                Check(!counter.IsVisible(start+.5f),"O primeiro numero aguarda o Shield terminar.");
                Check(!CastleMeteorPresentation.HoldCastPose(rules,false,true),"A pose de meteoro nao aparece durante Shield.");
                Check(counter.StartedAt==start+CastleMeteorPresentation.ProtectDuration,"Shield termina antes do primeiro numero.");
            }
            else Check(counter.StartedAt==start,"Turnos seguintes mostram o numero na apresentacao da acao.");
            if(turn<5)
            {
                Check(CastleMeteorPresentation.HoldCastPose(rules,false),"A pose do rei persiste apos Shield.");
                Check(CastleMeteorPresentation.ChargePose(rules)==Math.Min(3,turn-1),"O sprite do rei acompanha a contagem sem meteoro separado.");
            }
            float scheduled=counter.StartedAt;
            counter.Observe(CastleBattleRules.TyrantId,ability,turn,start+100);
            Check(counter.StartedAt==scheduled,"Observacoes repetidas nao reiniciam o numero flutuante.");
            Check(counter.IsVisible(scheduled+.5f)&&!counter.IsVisible(scheduled+1.5f),"O numero sobe e desaparece na janela dos danos.");
        }
        Check(counter.Value==5&&rules.Ending==GameEnding.CastleDestroyed,"O numero cinco precede a conclusao do meteoro.");
        counter.Reset();
        Check(counter.Value==0&&!counter.IsVisible(100),"Nova batalha limpa numeros pendentes.");
        counter.Observe("aren",CastleBattleRules.Protect,1,100);
        counter.Observe(CastleBattleRules.TyrantId,CastleBattleRules.RoyalStrike,1,100);
        Check(counter.Value==0,"Acoes de aliados e golpes normais nao contam.");
        counter.Observe(CastleBattleRules.TyrantId,CastleBattleRules.Protect,1,100);
        Check(counter.Value==1,"Uma nova conjuracao reinicia a contagem em um.");
    }
    private static void Check(bool value,string message) {checks++;if(!value)throw new Exception(message);}
}
