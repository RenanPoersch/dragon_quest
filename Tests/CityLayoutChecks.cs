using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonQuest.Prototype;
using UnityEngine;

internal static class CityLayoutChecks
{
    private const float Step = 0.5f;
    // Metade do collider do heroi mais margem para evitar passagens apertadas.
    private const float HalfWidth = 0.275f + 0.08f;
    private const float HalfHeight = 0.3f + 0.08f;
    private static readonly List<Rect> solids = new List<Rect>();
    private static readonly HashSet<(int, int)> reachable = new HashSet<(int, int)>();
    private static int checks;
    private static void Assert(bool condition, string description) { checks++; if (!condition) throw new Exception(description); }

    private static Vector2 Position(int x, int y) => new Vector2(PrototypeCityLayout.Bounds.xMin + x * Step, PrototypeCityLayout.Bounds.yMin + y * Step);
    private static (int, int) Cell(Vector2 position) => ((int)Math.Round((position.x - PrototypeCityLayout.Bounds.xMin) / Step),
        (int)Math.Round((position.y - PrototypeCityLayout.Bounds.yMin) / Step));
    private static bool Walkable(Vector2 position)
    {
        Rect bounds = PrototypeCityLayout.Bounds;
        if (position.x - HalfWidth <= bounds.xMin || position.x + HalfWidth >= bounds.xMax
            || position.y - HalfHeight <= bounds.yMin || position.y + HalfHeight >= bounds.yMax) return false;
        foreach (Rect solid in solids)
            if (position.x + HalfWidth >= solid.xMin && position.x - HalfWidth <= solid.xMax
                && position.y + HalfHeight >= solid.yMin && position.y - HalfHeight <= solid.yMax) return false;
        return true;
    }

    private static void BuildNavigation()
    {
        foreach (CityBlock block in PrototypeCityLayout.Blocks.Concat(PrototypeCityLayout.Buildings)) if (block.Solid) solids.Add(block.Bounds);
        foreach (Vector2 npc in new[] { PrototypeCityLayout.Citizen, PrototypeCityLayout.Resident, PrototypeCityLayout.Gardener, PrototypeCityLayout.Explorer })
            solids.Add(new Rect(npc.x - 0.275f, npc.y - 0.3f, 0.55f, 0.6f));
        Vector2 guard = PrototypeCityLayout.Guard, chest = PrototypeCityLayout.Chest;
        solids.Add(new Rect(guard.x - 0.325f, guard.y - 0.35f, 0.65f, 0.7f));
        solids.Add(new Rect(chest.x - 0.45f, chest.y - 0.35f, 0.9f, 0.7f));

        var pending = new Queue<(int, int)>();
        var start = Cell(PrototypeCityLayout.Spawn);
        Assert(Walkable(PrototypeCityLayout.Spawn), "Posicao inicial precisa estar livre.");
        reachable.Add(start); pending.Enqueue(start);
        var directions = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var direction in directions)
            {
                var next = (current.Item1 + direction.Item1, current.Item2 + direction.Item2);
                if (reachable.Contains(next) || !Walkable(Position(next.Item1, next.Item2))) continue;
                reachable.Add(next); pending.Enqueue(next);
            }
        }
    }

    private static void CheckReachable(Vector2 position, string label)
        => Assert(Accessible(position), "Sem caminho livre ate " + label + ".");
    public static bool Accessible(Vector2 position) => Walkable(position) && reachable.Contains(Cell(position));

    private static void CheckPaths()
    {
        BuildNavigation();
        foreach (CityDistrict district in PrototypeCityLayout.Districts)
        {
            CheckReachable(district.VisitPoint, district.Name);
            Assert(PrototypeCityLayout.DistrictAt(district.VisitPoint) == district.Name, "Nome do bairro incorreto em " + district.Name + ".");
        }
        foreach (Vector2 door in new[] { new Vector2(-9, 2.1f), new Vector2(9, 2.1f), new Vector2(9, -7.4f) }) CheckReachable(door, "porta da loja");
        foreach (Vector2 exit in new[] { new Vector2(-9, 1.7f), new Vector2(9, 1.7f), new Vector2(9, -7.8f) }) CheckReachable(exit, "retorno da loja");
        CheckReachable(PrototypeCityLayout.Citizen + Vector2.down, "aldeao");
        CheckReachable(PrototypeCityLayout.Chest + Vector2.down, "bau");
        CheckReachable(PrototypeCityLayout.Guard + Vector2.down, "patrulha");
        CheckReachable(new Vector2(-24, 18), "capela");
        CheckReachable(new Vector2(24, -18), "pedestal nas ruinas");
        foreach (float x in new[] { -24f, 0f, 24f })
            for (int y = -15; y <= -9; y++) CheckReachable(new Vector2(x, y), "travessia da ponte em " + x);
        foreach (Vector2 point in new[] { new Vector2(-32, 0), new Vector2(32, 0), new Vector2(0, 22.5f), new Vector2(0, -22.5f) })
            CheckReachable(point, "extremidade ampliada " + point.x + ", " + point.y);
        Assert(!Walkable(new Vector2(10, -12)) && !Walkable(new Vector2(-10, -12)), "Rio so pode ser cruzado pelas pontes.");
        Assert(!Walkable(new Vector2(29, 14)), "Lago deve impedir caminhar sobre a agua.");
        foreach (Vector2 point in new[] { new Vector2(-35.5f, 0), new Vector2(35.5f, 0), new Vector2(0, -23.5f), new Vector2(0, 23.5f) })
            Assert(!Walkable(point), "Muralha deve bloquear a saida do mapa.");

        int freeCells = 0;
        for (int x = 0; x <= PrototypeCityLayout.Bounds.width / Step; x++)
            for (int y = 0; y <= PrototypeCityLayout.Bounds.height / Step; y++) if (Walkable(Position(x, y))) freeCells++;
        Assert(reachable.Count == freeCells, "Mapa contem uma area caminhavel isolada do heroi.");
        Assert(reachable.Count * Step * Step > 36 * 24 * 2, "Area caminhavel precisa crescer substancialmente, alem das medidas externas.");
    }

    private static void ExportGeometry(string path)
    {
        var blocks = PrototypeCityLayout.Blocks.Concat(PrototypeCityLayout.Buildings).Select(block => new
        {
            name = block.Name, x = block.Position.x, y = block.Position.y, width = block.Size.x, height = block.Size.y,
            color = block.Color, solid = block.Solid, order = block.Order
        });
        File.WriteAllText(path, JsonSerializer.Serialize(blocks));
    }

    private static int Main(string[] args)
    {
        try
        {
            CheckPaths();
            if (args.Length > 0) ExportGeometry(args[0]);
            Console.WriteLine(checks + " verificacoes de caminhos da cidade passaram; " + reachable.Count * Step * Step + " unidades quadradas acessiveis na amostragem.");
            Console.WriteLine(WorldMapChecks.Run(args.Length > 0 ? Path.GetDirectoryName(args[0]) : null) + " verificacoes de ilha, interiores e viagens passaram.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception.Message); return 1; }
    }
}
