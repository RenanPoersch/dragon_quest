using System;
using DragonQuest.Combat;
using DragonQuest.Prototype;
using DragonQuest.Progression;

public static class CastlePresentationChecks
{
    public static void Main()
    {
        int checks=CastleChecks.Run();
        for(int level=1;level<CastleBattleRules.MeteorCastTurns;level++)
        {
            Check(CastleMeteorPresentation.ChargeHeight(level+1)>CastleMeteorPresentation.ChargeHeight(level),"Meteoro cresce a cada turno.");checks++;
        }
        var rules=new CastleBattleRules(PrototypePartyFactory.Create());
        Check(!CastleMeteorPresentation.HoldCastPose(rules,false)&&CastleMeteorPresentation.HoldCastPose(rules,true),"A animacao de queda preserva a pose mesmo apos resolver o combate.");checks++;
        Check(BattleAnimationCatalog.Resource("tyrant",CastleBattleRules.Protect)=="TyrantMeteorCharge","Protect conserva a pose de conjuracao.");checks++;
        Check(BattleAnimationCatalog.Duration("tyrant",CastleBattleRules.Protect)==CastleMeteorPresentation.ProtectDuration,"Protect apresenta os quatro quadros completos antes de liberar o proximo turno.");checks++;
        Check(BattleAnimationCatalog.ImpactDelay("tyrant",CastleBattleRules.Meteor)==CastleMeteorPresentation.ImpactDelay,"Numeros e derrotas sao sincronizados ao impacto.");checks++;
        Check(CastleMeteorPresentation.MeteorFrame(CastleMeteorPresentation.ImpactDelay-.001f)<2
            &&CastleMeteorPresentation.MeteorFrame(CastleMeteorPresentation.ImpactDelay)==2,"Explosao so comeca no impacto.");checks++;
        Check(CastleMeteorPresentation.AbsorptionStart<CastleMeteorPresentation.ImpactDelay
            &&CastleMeteorPresentation.Duration>CastleMeteorPresentation.ImpactDelay+1,"Ha tempo para absorver e para exibir o impacto completo.");checks++;
        Console.WriteLine(checks+" verificacoes do castelo, Protect e apresentacao do meteoro passaram.");
    }
    private static void Check(bool value,string message) {if(!value)throw new Exception(message);}
}
