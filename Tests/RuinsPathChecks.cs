using System;
using DragonQuest.Prototype;
using UnityEngine;
internal static class RuinsPathChecks
{
    static void Assert(bool value,string message) { if(!value) throw new Exception(message); }
    public static void Main()
    {
        Assert(PrototypeRuinsLayout.Spawn.x > PrototypeRuinsLayout.Altar.x,"Avanco deve seguir para a esquerda.");
        Assert(PrototypeRuinsLayout.WalkingSpeedMultiplier == 1.15f,"Velocidade deve ficar apenas 15% acima do ritmo normal.");
        Assert(PrototypeRuinsLayout.FloorAt(-25)==0,"Aproximacao plana.");
        Assert(Math.Abs(PrototypeRuinsLayout.FloorAt(-30)-1.05f)<.0001f,"Tres degraus de 0.35.");
        float previous=0;
        for(float x=PrototypeRuinsLayout.MaxX;x>=PrototypeRuinsLayout.MinX;x-=.05f)
        {
            float height=PrototypeRuinsLayout.FloorAt(x);
            Assert(height>=previous && height-previous<.045f,"Subida precisa ser continua.");previous=height;
        }
        var travel=new PrototypeTravel();
        Assert(travel.TryRestore(PrototypeTravel.Ruins,new Vector2(-3,-2.5f)),"Save antigo precisa ser aceito.");
        Assert(PrototypeRuinsLayout.SafeSavedPosition(new Vector2(-3,-2.5f))==PrototypeRuinsLayout.Spawn,"Save antigo deve recuperar a entrada.");
        Assert(!travel.Current.FixedCamera,"Camera precisa acompanhar o corredor.");
        Assert(!travel.RouteFrom(PrototypeTravel.Ruins,PrototypeTravel.World).Entrance.Contains(PrototypeRuinsLayout.Spawn),"Entrada nao deve provocar retorno imediato.");
        Assert(travel.RouteFrom(PrototypeTravel.Ruins,PrototypeTravel.World).Entrance.Contains(PrototypeRuinsLayout.Exit),"Saida deve ser alcancavel voltando a direita.");
        Assert(travel.TryTravel(PrototypeTravel.World,out Vector2 arrival),"Retorno a ilha deve funcionar.");
        Console.WriteLine("PASS: direcao, velocidade, tres degraus, subida continua, save antigo, camera e retorno.");
    }
}
