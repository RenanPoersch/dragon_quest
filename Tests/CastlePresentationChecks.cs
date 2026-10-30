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
        CheckMeteorDisplayAndAbsorption();
        CheckReleaseTiming();
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
    private static void CheckReleaseTiming()
    {
        Check(CastleMeteorPresentation.FireballScale(0)==1
            &&CastleMeteorPresentation.FireballScale(CastleMeteorPresentation.ThrowWindupAt)==1.8f,
            "A esfera cresce antes de o rei preparar o arremesso.");
        Check(CastleMeteorPresentation.ThrowPose(0)==0
            &&CastleMeteorPresentation.ThrowPose(CastleMeteorPresentation.ThrowRaiseAt)==1
            &&CastleMeteorPresentation.ThrowPose(CastleMeteorPresentation.ThrowWindupAt)==2
            &&CastleMeteorPresentation.ThrowPose(CastleMeteorPresentation.ThrowReleaseAt)==3,
            "Preparar, erguer duas maos, recuar e arremessar sao poses distintas.");
        Check(CastleMeteorPresentation.ThrowFlightProgress(CastleMeteorPresentation.ThrowReleaseAt)==0
            &&CastleMeteorPresentation.ThrowFlightProgress(CastleMeteorPresentation.ThrowImpactAt)==1,
            "A esfera so viaja depois do movimento para tras e chega antes do dano.");
        float previous=0;
        for(int frame=0;frame<=216;frame++)
        {
            float time=frame/60f, scale=CastleMeteorPresentation.FireballScale(time);
            Check(scale>=previous&&scale<=1.8f,"A carga cresce continuamente."); previous=scale;
            Check(CastleMeteorPresentation.WhiteOpacity(time)==(time>=CastleMeteorPresentation.ThrowImpactAt?1:0),
                "O clarao comeca apenas no instante do dano.");
        }
        Check(!CastleMeteorPresentation.ShouldCutToCinematic(CastleMeteorPresentation.ThrowImpactAt)
            &&CastleMeteorPresentation.ShouldCutToCinematic(CastleMeteorPresentation.CinematicCutAt),
            "O corte automatico vem logo depois do clarao.");
    }
    private static void CheckMeteorDisplayAndAbsorption()
    {
        var fatal = new CombatVitalChange(1, -120, false, false);
        Check(CastleMeteorPresentation.DisplayedAmount(fatal, true) == -9999,
            "O impacto do cometa mostra 9999 mesmo quando o alvo tinha menos HP.");
        Check(CastleMeteorPresentation.DisplayedAmount(fatal, false) == -120,
            "Outros golpes preservam o dano real.");
        var poison = new CombatVitalChange(2, -10, false, false, damageOverTime: true);
        var mana = new CombatVitalChange(3, -7, false, true);
        var heal = new CombatVitalChange(4, 25, false, false);
        Check(CastleMeteorPresentation.DisplayedAmount(poison, true) == -10
            && CastleMeteorPresentation.DisplayedAmount(mana, true) == -7
            && CastleMeteorPresentation.DisplayedAmount(heal, true) == 25,
            "Veneno, MP e cura nao viram dano do cometa.");
        Check(CastleMeteorPresentation.AbsorberAdvance(0) == 0
            && CastleMeteorPresentation.AbsorberAdvance(CastleMeteorPresentation.Duration) == 0,
            "O portador parte de sua posicao e termina nela.");
        Check(CastleMeteorPresentation.AbsorberAdvance(.7f) == 1
            && CastleMeteorPresentation.AbsorberAdvance(CastleMeteorPresentation.AbsorptionEnd) == 1,
            "O heroi chega ao ar antes da transferencia e sustenta a pose ate absorver tudo.");
        Check(CastleMeteorPresentation.AbsorptionProgress(CastleMeteorPresentation.AbsorptionStart) == 0
            && CastleMeteorPresentation.AbsorptionProgress(CastleMeteorPresentation.AbsorptionEnd) == 1,
            "A bola de fogo e totalmente consumida antes da descida.");
        Check(CastleMeteorPresentation.AbsorberPose(1.5f) == 2
            && CastleMeteorPresentation.AbsorberPose(3.2f) == 3,
            "A pose de peito aberto antecede a recuperacao.");
        for (int frame = 0; frame <= 240; frame++)
        {
            float time = CastleMeteorPresentation.Duration * frame / 240;
            float advance = CastleMeteorPresentation.AbsorberAdvance(time);
            float previous = CastleMeteorPresentation.AbsorberAdvance(time - .015f);
            Check(advance >= 0 && advance <= 1 && Math.Abs(advance - previous) < .04f,
                "Avanco, flutuacao e retorno sao continuos, sem teleporte.");
        }
    }
    private static void CheckCastCounter()
    {
        var source=PrototypeCastleBattleFactory.Create(PrototypePartyFactory.Create());
        var battle=new BattleSession(()=>1d,source.Rules,source.Inventory,source.Combatants.ToArray());
        var rules=(CastleBattleRules)battle.Rules;
        var king=battle.Combatants.Single(c=>c.Id==CastleBattleRules.TyrantId);
        king.TakeDamage(king.Hp-360);rules.AfterAction(battle);
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
            Check(EnemyAI.TryTakeTurn(battle,out _),"Cada turno do Rei Rubro progride.");
            Check(rules.MeteorTurnsElapsed==turn,"A contagem segue os cinco turnos proprios.");
            float start=turn*10;
            counter.Observe(CastleBattleRules.TyrantId,ability,rules.MeteorTurnsElapsed,start);
            Check(counter.Value==turn&&counter.DisplayValue==6-turn,"Os cinco turnos exibem 5, 4, 3, 2, 1.");
            if(turn==1)
            {
                Check(!counter.IsVisible(start+.5f),"O primeiro numero aguarda o Shield terminar.");
                Check(!CastleMeteorPresentation.HoldCastPose(rules,false,true),"A pose de meteoro nao aparece durante Shield.");
                Check(counter.StartedAt==start+CastleMeteorPresentation.ProtectDuration,"Shield termina antes do primeiro numero.");
            }
            else Check(counter.StartedAt==start,"Turnos seguintes mostram o numero na apresentacao da acao.");
            if(turn<=5)
            {
                Check(CastleMeteorPresentation.HoldCastPose(rules,false),"A pose do rei persiste apos Shield.");
                Check(CastleMeteorPresentation.ChargePose(rules)==Math.Min(3,turn-1),"O sprite do rei acompanha a contagem sem meteoro separado.");
            }
            float scheduled=counter.StartedAt;
            counter.Observe(CastleBattleRules.TyrantId,ability,turn,start+100);
            Check(counter.StartedAt==scheduled,"Observacoes repetidas nao reiniciam o numero flutuante.");
            Check(counter.IsVisible(scheduled+.5f)&&!counter.IsVisible(scheduled+1.5f),"O numero sobe e desaparece na janela dos danos.");
        }
        Check(counter.Value==5&&counter.DisplayValue==1&&rules.Ending==GameEnding.None
            &&king.IsAlive&&king.IsCharging,"O numero um ainda e preparacao, sem dano nem final.");
        while(battle.CurrentActor.Id!=CastleBattleRules.TyrantId)
        {
            var actor=battle.CurrentActor;
            Check(battle.TryAct(actor,actor.Definition.Abilities.Single(a=>a.Effect==AbilityEffect.Defend),actor,out _),
                "A equipe recebe um turno completo depois do numero um.");
        }
        Check(EnemyAI.ChooseAbility(battle)==CastleBattleRules.Meteor,"So o turno seguinte lanca o meteoro.");
        Check(EnemyAI.TryTakeTurn(battle,out _)&&rules.MeteorTurnsElapsed==6
            &&rules.Ending==GameEnding.CastleDestroyed,"O sexto turno resolve o meteoro.");
        float lastNumberAt=counter.StartedAt;
        counter.Observe(CastleBattleRules.TyrantId,CastleBattleRules.Meteor,6,60);
        Check(counter.StartedAt==lastNumberAt&&!counter.IsVisible(60),"O lancamento nao repete o numero um nem mostra zero.");
        counter.Reset();
        Check(counter.Value==0&&counter.DisplayValue==0&&!counter.IsVisible(100),"Nova batalha limpa numeros pendentes.");
        counter.Observe("aren",CastleBattleRules.Protect,1,100);
        counter.Observe(CastleBattleRules.TyrantId,CastleBattleRules.RoyalStrike,1,100);
        Check(counter.Value==0,"Acoes de aliados e golpes normais nao contam.");
        counter.Observe(CastleBattleRules.TyrantId,CastleBattleRules.Protect,1,100);
        Check(counter.Value==1&&counter.DisplayValue==5,"Uma nova conjuracao reinicia a contagem em cinco.");
    }
    private static void Check(bool value,string message) {checks++;if(!value)throw new Exception(message);}
}
