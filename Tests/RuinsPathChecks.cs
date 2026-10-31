using DragonQuest.Presentation.World.Animations;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Exploration;
using System;
using UnityEngine;
internal static class RuinsPathChecks
{
    static void Assert(bool value,string message) { if(!value) throw new Exception(message); }
    public static void Main()
    {
        Assert(RuinsFlameAnimation.FrameAt(0)==0 && RuinsFlameAnimation.FrameAt(.11f)==1,"Chama precisa avancar os quadros.");
        Assert(RuinsFlameAnimation.FrameAt(.79f)==7 && RuinsFlameAnimation.FrameAt(.8f)==0,"Chama precisa repetir o ciclo continuamente.");
        Assert(RuinsLayout.Spawn.x > RuinsLayout.Altar.x,"Avanco deve seguir para a esquerda.");
        Assert(RuinsLayout.WalkingSpeedMultiplier == 1.15f,"Velocidade deve ficar apenas 15% acima do ritmo normal.");
        Assert(RuinsLayout.FloorAt(-25)==0,"Aproximacao plana.");
        Assert(Math.Abs(RuinsLayout.FloorAt(-30)-1.05f)<.0001f,"Tres degraus de 0.35.");
        float previous=0;
        for(float x=RuinsLayout.MaxX;x>=RuinsLayout.MinX;x-=.05f)
        {
            float height=RuinsLayout.FloorAt(x);
            Assert(height>=previous && height-previous<.045f,"Subida precisa ser continua.");previous=height;
        }
        var travel=new WorldNavigation();
        Assert(travel.TryRestore(WorldNavigation.Ruins,new Vector2(-3,-2.5f)),"Save antigo precisa ser aceito.");
        Assert(RuinsLayout.SafeSavedPosition(new Vector2(-3,-2.5f))==RuinsLayout.Spawn,"Save antigo deve recuperar a entrada.");
        Assert(!travel.Current.FixedCamera,"Camera precisa acompanhar o corredor.");
        Assert(!travel.RouteFrom(WorldNavigation.Ruins,WorldNavigation.World).Entrance.Contains(RuinsLayout.Spawn),"Entrada nao deve provocar retorno imediato.");
        Assert(travel.RouteFrom(WorldNavigation.Ruins,WorldNavigation.World).Entrance.Contains(RuinsLayout.Exit),"Saida deve ser alcancavel voltando a direita.");
        Assert(travel.TryTravel(WorldNavigation.World,out Vector2 arrival),"Retorno a ilha deve funcionar.");
        Console.WriteLine("PASS: ciclo da chama, direcao, velocidade, tres degraus, subida continua, save antigo, camera e retorno.");
    }
}
