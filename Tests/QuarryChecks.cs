using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEngine;

class QuarryChecks
{
    static int checks;
    static void Check(bool ok, string reason) { checks++; if (!ok) throw new Exception(reason); }
    static void Main()
    {
        Check(QuarryLayout.IsWalkable(QuarryLayout.Spawn), "Spawn is on gravel");
        Check(QuarryLayout.IsExit(QuarryLayout.Exit), "Exit is on the bottom path");
        foreach (var uv in new[] { new Vector2(.1f,.4f),new Vector2(.85f,.4f),new Vector2(.5f,.3f),new Vector2(.1f,.9f),new Vector2(.85f,.9f) })
            Check(!QuarryLayout.IsWalkable(QuarryLayout.Ground(uv.x,uv.y)), "Cliffs and foreground boulders block walking");
        foreach (CityBlock block in QuarryLayout.Monoliths)
        {
            Check(!QuarryLayout.IsWalkable(block.Position), "Totem has a physical footprint");
            Check(QuarryLayout.SafeSavedPosition(block.Position) == QuarryLayout.Spawn, "Save inside totem recovers");
        }
        Check(QuarryLayout.SafeSavedPosition(QuarryLayout.Spawn) == QuarryLayout.Spawn, "Valid saved position remains valid");
        var seen = new HashSet<(int,int)>(); var pending = new Queue<(int,int)>();
        Vector2 spawn = QuarryLayout.ImagePoint(QuarryLayout.Spawn);
        var first = ((int)Math.Round(spawn.x*200),(int)Math.Round(spawn.y*200));
        seen.Add(first); pending.Enqueue(first);
        var reached = new bool[5]; bool exit = false;
        var targets = new[] {QuarryLayout.Monoliths[0].Position,QuarryLayout.Monoliths[1].Position,
            QuarryLayout.Monoliths[2].Position,QuarryLayout.Inscription,QuarryLayout.Worker};
        while (pending.Count > 0)
        {
            var cell = pending.Dequeue(); Vector2 feet = QuarryLayout.Ground(cell.Item1/200f,cell.Item2/200f);
            for (int i=0;i<targets.Length;i++) reached[i] |= Vector2.Distance(feet,targets[i]) < 1.55f;
            exit |= QuarryLayout.IsExit(feet);
            if ((cell.Item1+cell.Item2)%19 == 0)
                foreach (var delta in new[] {new Vector2(0,4),new Vector2(4,0),new Vector2(-3,-3),new Vector2(3,3)})
                    Check(QuarryLayout.IsWalkable(QuarryLayout.ConstrainMove(feet,delta)), "Large moves stay on gravel and avoid totems");
            foreach (var d in new[] {(1,0),(-1,0),(0,1),(0,-1)})
            {
                var next = (cell.Item1+d.Item1,cell.Item2+d.Item2);
                if (!seen.Contains(next) && QuarryLayout.IsWalkable(QuarryLayout.Ground(next.Item1/200f,next.Item2/200f)))
                { seen.Add(next); pending.Enqueue(next); }
            }
        }
        foreach (bool target in reached) Check(target, "Spawn reaches every totem, inscription and keeper within E range");
        Check(exit, "Spawn connects to island exit");
        for (float u=.46f;u<=.55f;u+=.01f)
        {
            var travel = new PrototypeTravel();
            Check(travel.TryTravel(PrototypeTravel.WestForest,out var arrival) && arrival == QuarryLayout.Spawn, "Island arrival matches quarry");
            var feet = QuarryLayout.Ground(u,.92f); bool left=false;
            for (int step=0;step<40;step++)
            {
                feet=QuarryLayout.ConstrainMove(feet,ObliqueProjection.GroundDirection(Vector2.down)*.08f);
                if (travel.TryEnter(feet,false,out arrival))
                { Check(arrival == PrototypeWorldLayout.WestForest+Vector2.down*2.5f,"Exit returns to quarry icon"); left=true; break; }
            }
            Check(left, "Entire entrance path permits leaving");
        }
        var blocked = new PrototypeTravel(); blocked.TryTravel(PrototypeTravel.WestForest,out _);
        Check(!blocked.TryEnter(QuarryLayout.Exit,true,out _), "Menus suspend travel");
        Check(!blocked.TryEnter(QuarryLayout.Spawn,false,out _), "Spawn cannot instantly exit");
        Check(!blocked.TryEnter(QuarryLayout.Ground(.15f,.98f),false,out _), "Foreground does not exit");
        var secrets=new AncientSecrets();
        Check(secrets.Activate(MonolithSymbol.Moon)==MonolithResult.Accepted && secrets.IsLit(MonolithSymbol.Moon),"Moon starts challenge");
        Check(secrets.Activate(MonolithSymbol.Sun)==MonolithResult.Reset && !secrets.IsLit(MonolithSymbol.Moon),"Wrong totem resets lights");
        secrets.Activate(MonolithSymbol.Moon); secrets.Activate(MonolithSymbol.Star);
        Check(secrets.Activate(MonolithSymbol.Sun)==MonolithResult.Completed,"Moon, Star, Sun solves challenge");
        foreach (MonolithSymbol symbol in Enum.GetValues(typeof(MonolithSymbol))) Check(secrets.IsLit(symbol),"Completed runes stay lit");
        Check(secrets.Activate(MonolithSymbol.Moon)==MonolithResult.AlreadyCompleted,"Solved challenge is preserved");
        Console.WriteLine(checks+" quarry navigation, collisions, interaction access, exit, save recovery and challenge checks passed.");
    }
}
