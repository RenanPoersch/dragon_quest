using DragonQuest.Exploration.Layouts;
using DragonQuest.Exploration;
using System;
using System.Collections.Generic;
using UnityEngine;
class ForestNavigationChecks
{
    static int checks;
    static void Assert(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    static void Main()
    {
        Assert(EastForestLayout.IsWalkable(EastForestLayout.Spawn),"Arrival is on the floor");
        Assert(EastForestLayout.IsWalkable(EastForestLayout.Exit),"Exit is on the floor");
        Assert(EastForestLayout.ImagePoint(EastForestLayout.Exit).y>.95f,"Exit is at the bottom of the painting");
        foreach(var p in new[]{new Vector2(.5f,.25f),new Vector2(.27f,.43f),new Vector2(.82f,.44f),new Vector2(.84f,.83f),new Vector2(.20f,.89f)})
            Assert(!EastForestLayout.IsWalkable(EastForestLayout.Ground(p.x,p.y)),"Canopy, trunks and foreground roots are blocked");
        Assert(EastForestLayout.SafeSavedPosition(SecretLayout.HollowTree)==EastForestLayout.Spawn,"Legacy save inside a trunk is recovered");
        var travel=new WorldNavigation();
        Assert(travel.TryTravel(WorldNavigation.EastForest,out var arrival)&&arrival==EastForestLayout.Spawn,"Travel arrival matches clearing");
        Assert(!travel.RouteFrom(WorldNavigation.EastForest,WorldNavigation.World).Entrance.Contains(WorldLayout.LocalExit),"Old exit near center no longer exits");
        Assert(travel.RouteFrom(WorldNavigation.EastForest,WorldNavigation.World).Entrance.Contains(EastForestLayout.Exit),"Bottom exit route matches floor");
        var seen=new HashSet<(int,int)>(); var queue=new Queue<(int,int)>();
        var uv=EastForestLayout.ImagePoint(EastForestLayout.Spawn); var first=((int)Math.Round(uv.x*200),(int)Math.Round(uv.y*200));
        seen.Add(first);queue.Enqueue(first);bool stone=false,tree=false,npc=false,exit=false;
        while(queue.Count>0)
        {
            var c=queue.Dequeue();var ground=EastForestLayout.Ground(c.Item1/200f,c.Item2/200f);
            stone|=Vector2.Distance(ground,SecretLayout.ClawStone)<1.55f;
            tree|=Vector2.Distance(ground,SecretLayout.HollowTree)<1.55f;
            npc|=Vector2.Distance(ground,EastForestLayout.Gardener)<1.55f;
            exit|=travel.RouteFrom(WorldNavigation.EastForest,WorldNavigation.World).Entrance.Contains(ground);
            foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
            {
                var next=(c.Item1+d.Item1,c.Item2+d.Item2);
                if(seen.Contains(next)||!EastForestLayout.IsWalkable(EastForestLayout.Ground(next.Item1/200f,next.Item2/200f)))continue;
                seen.Add(next);queue.Enqueue(next);
            }
        }
        Assert(stone,"Connected floor reaches stone within interaction range");
        Assert(tree,"Connected floor reaches scale within interaction range");
        Assert(npc,"Gardener is accessible");Assert(exit,"Bottom exit is accessible");
        foreach(var c in seen)
        {
            var start=EastForestLayout.Ground(c.Item1/200f,c.Item2/200f);
            foreach(var d in new[]{new Vector2(0,4),new Vector2(3,3),new Vector2(-3,3),new Vector2(-4,0)})
                Assert(EastForestLayout.IsWalkable(EastForestLayout.ConstrainMove(start,d)),"Large diagonal or vertical step cannot enter obstacles");
        }
        Console.WriteLine(checks+" forest floor, exits, interaction access, save recovery and movement checks passed.");
    }
}
