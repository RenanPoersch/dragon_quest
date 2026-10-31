using DragonQuest.Exploration.Layouts;
using DragonQuest.Exploration;
using System;
using DragonQuest.Progression;
using UnityEngine;

internal static class DragonLairChecks
{
    private static int checks;
    private static void Assert(bool condition, string reason)
    { checks++; if (!condition) throw new Exception(reason); }

    public static void Main()
    {
        var state = new DragonAwakening();
        Assert(state.State == DragonRestState.Sleeping && state.Frame == 0, "A visita comeca com o dragao dormindo.");
        state.Advance(20);
        Assert(state.State == DragonRestState.Sleeping, "Tempo e proximidade nao acordam o dragao.");
        Assert(state.Wake() && !state.Wake() && state.Frame == 1, "A interacao inicia um unico despertar.");
        state.Advance(-1); state.Advance(float.NaN);
        Assert(state.Elapsed == 0, "Tempo invalido nao altera o despertar.");
        state.Advance(.6f);
        Assert(state.State == DragonRestState.Waking && state.Frame == 1, "Cabeca sobe antes da pose acordada.");
        state.Advance(.4f);
        Assert(state.State == DragonRestState.Waking && state.Frame == 2, "Asas abrem ao fim do despertar.");
        state.Advance(100);
        Assert(state.State == DragonRestState.Awake && state.Elapsed == DragonAwakening.Duration, "Despertar termina sem repetir.");
        state.Reset(); state.Finish();
        Assert(state.State == DragonRestState.Sleeping, "Visita nova reinicia o sono.");
        state.Wake(); state.Finish();
        Assert(state.State == DragonRestState.Awake && state.Frame == 2, "Dialogo rapido termina na pose acordada.");

        Assert(VolcanoLayout.Spawn.x < VolcanoLayout.Dragon.x, "Percurso segue da esquerda ao dragao.");
        Assert(VolcanoLayout.MinX <= VolcanoLayout.Exit.x, "Saida fica dentro do percurso lateral.");
        Assert(Math.Abs(VolcanoLayout.MaxX - VolcanoLayout.Dragon.x) < 1.6f, "Dragao fica ao alcance da tecla E.");
        float previous = 0;
        for (float x = VolcanoLayout.MinX; x <= VolcanoLayout.MaxX; x += .025f)
        {
            float floor = VolcanoLayout.FloorAt(x);
            Assert(floor >= previous - .0001f && floor - previous < .03f, "Degraus formam caminho continuo.");
            Assert(VolcanoLayout.Bounds.Contains(new Vector2(x, floor)), "Todo percurso fica dentro do mapa.");
            previous = floor;
        }
        Assert(Math.Abs(previous - .9f) < .0001f, "Patamar do dragao fica alinhado aos tres degraus.");
        Assert(VolcanoLayout.SafeSavedPosition(new Vector2(0, 8)) == VolcanoLayout.Spawn, "Save antigo recupera a entrada.");
        var valid = new Vector2(10, VolcanoLayout.FloorAt(10));
        Assert(VolcanoLayout.SafeSavedPosition(valid) == valid, "Save lateral conserva o ponto do piso.");
        var secrets = new AncientSecrets();
        var travel = new WorldNavigation(secrets);
        Assert(!travel.TryTravel(WorldNavigation.Volcano, out _), "Barreira permanece protegida pelos segredos.");
        secrets.LearnPoem(); secrets.TrySolveAltar(LegendPoem.Flame);
        secrets.Activate(MonolithSymbol.Moon); secrets.Activate(MonolithSymbol.Star); secrets.Activate(MonolithSymbol.Sun);
        secrets.TryCollectScale(); secrets.TryOpenPassage();
        Assert(travel.TryTravel(WorldNavigation.Volcano, out Vector2 arrival) && arrival == VolcanoLayout.Spawn, "Entrada usa o novo corredor.");
        Assert(!travel.Current.FixedCamera && travel.Current.CameraSize == VolcanoLayout.CameraSize, "Camera acompanha como nas ruinas.");
        var exit = travel.RouteFrom(WorldNavigation.Volcano, WorldNavigation.World).Entrance;
        Assert(!exit.Contains(arrival) && exit.Contains(VolcanoLayout.Exit), "Chegada nao retorna imediatamente e saida permanece acessivel.");
        Assert(travel.TryRestore(WorldNavigation.Volcano, new Vector2(0,-10)), "Save do vulcao antigo continua aceito.");
        Assert(travel.TryRecoverAtInn(out _), "Derrota ainda retorna a hospedaria.");
        int combatChecks = DragonChecks.Run();
        Console.WriteLine("PASS: " + checks + " verificacoes do covil e " + combatChecks + " verificacoes do combate, armadura e recompensa do dragao.");
    }
}
