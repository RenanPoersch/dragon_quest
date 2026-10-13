using System;
using DragonQuest.Prototype;
using DragonQuest.Exploration;
using UnityEngine;
class ForestExitChecks
{
    static int checks;
    static void Assert(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Main()
    {
        // Simulate holding Down across the entire visible path, rather than finding one reachable point.
        foreach(float u in new[]{.42f,.45f,.47f,.49f,.52f,.55f})
        {
            var travel=new PrototypeTravel();travel.TryTravel(PrototypeTravel.EastForest,out _);
            Vector2 feet=EastForestLayout.Ground(u,.91f);
            Assert(EastForestLayout.IsWalkable(feet),"Starting foot position must be on the path");
            bool exited=false;
            for(int tick=0;tick<240;tick++)
            {
                feet=EastForestLayout.ConstrainMove(feet,ObliqueProjection.GroundDirection(Vector2.down)*.08f);
                if(travel.TryEnter(feet,false,out var arrival))
                {
                    Assert(travel.Current.Id==PrototypeTravel.World,"Down must return to the island");
                    Assert(EastForestLayout.ImagePoint(feet).y>=.95f,"Exit remains at the bottom");
                    Assert(arrival==PrototypeWorldLayout.EastForest+Vector2.down*2.5f,"Correct island return position");
                    exited=true;break;
                }
            }
            Assert(exited,"Holding Down must exit from path column "+u);
        }
        var blocked=new PrototypeTravel();blocked.TryTravel(PrototypeTravel.EastForest,out _);
        Assert(!blocked.TryEnter(EastForestLayout.Exit,true,out _),"Menus still block travel");
        Assert(!blocked.TryEnter(EastForestLayout.Spawn,false,out _),"Entrance does not instantly return");
        Assert(!blocked.TryEnter(EastForestLayout.Ground(.30f,.99f),false,out _),"Foreground roots do not provide an exit");
        Console.WriteLine(checks+" bottom exit movement checks passed.");
    }
}
