using System;
using DragonQuest.Progression;
using DragonQuest.Prototype;
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

        Assert(PrototypeVolcanoLayout.Spawn.x < PrototypeVolcanoLayout.Dragon.x, "Percurso segue da esquerda ao dragao.");
        Assert(PrototypeVolcanoLayout.MinX <= PrototypeVolcanoLayout.Exit.x, "Saida fica dentro do percurso lateral.");
        Assert(Math.Abs(PrototypeVolcanoLayout.MaxX - PrototypeVolcanoLayout.Dragon.x) < 1.6f, "Dragao fica ao alcance da tecla E.");
        float previous = 0;
        for (float x = PrototypeVolcanoLayout.MinX; x <= PrototypeVolcanoLayout.MaxX; x += .025f)
        {
            float floor = PrototypeVolcanoLayout.FloorAt(x);
            Assert(floor >= previous - .0001f && floor - previous < .03f, "Degraus formam caminho continuo.");
            Assert(PrototypeVolcanoLayout.Bounds.Contains(new Vector2(x, floor)), "Todo percurso fica dentro do mapa.");
            previous = floor;
        }
        Assert(Math.Abs(previous - .9f) < .0001f, "Patamar do dragao fica alinhado aos tres degraus.");
        Assert(PrototypeVolcanoLayout.SafeSavedPosition(new Vector2(0, 8)) == PrototypeVolcanoLayout.Spawn, "Save antigo recupera a entrada.");
        var valid = new Vector2(10, PrototypeVolcanoLayout.FloorAt(10));
        Assert(PrototypeVolcanoLayout.SafeSavedPosition(valid) == valid, "Save lateral conserva o ponto do piso.");
        var secrets = new AncientSecrets();
        var travel = new PrototypeTravel(secrets);
        Assert(!travel.TryTravel(PrototypeTravel.Volcano, out _), "Barreira permanece protegida pelos segredos.");
        secrets.LearnPoem(); secrets.TrySolveAltar(LegendPoem.Flame);
        secrets.Activate(MonolithSymbol.Moon); secrets.Activate(MonolithSymbol.Star); secrets.Activate(MonolithSymbol.Sun);
        secrets.TryCollectScale(); secrets.TryOpenPassage();
        Assert(travel.TryTravel(PrototypeTravel.Volcano, out Vector2 arrival) && arrival == PrototypeVolcanoLayout.Spawn, "Entrada usa o novo corredor.");
        Assert(!travel.Current.FixedCamera && travel.Current.CameraSize == PrototypeVolcanoLayout.CameraSize, "Camera acompanha como nas ruinas.");
        var exit = travel.RouteFrom(PrototypeTravel.Volcano, PrototypeTravel.World).Entrance;
        Assert(!exit.Contains(arrival) && exit.Contains(PrototypeVolcanoLayout.Exit), "Chegada nao retorna imediatamente e saida permanece acessivel.");
        Assert(travel.TryRestore(PrototypeTravel.Volcano, new Vector2(0,-10)), "Save do vulcao antigo continua aceito.");
        Assert(travel.TryRecoverAtInn(out _), "Derrota ainda retorna a hospedaria.");
        int combatChecks = DragonChecks.Run();
        Console.WriteLine("PASS: " + checks + " verificacoes do covil e " + combatChecks + " verificacoes do combate, armadura e recompensa do dragao.");
    }
}
