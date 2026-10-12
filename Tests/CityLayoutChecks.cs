using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonQuest.Prototype;
using UnityEngine;

internal static class CityLayoutChecks
{
    private const float Step = 0.125f;
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
        foreach (Vector2 door in new[] {PrototypeCityLayout.ItemDoor, PrototypeCityLayout.EquipmentDoor,
            PrototypeCityLayout.MateriaDoor, PrototypeCityLayout.InnDoor})
        {
            CheckReachable(door, "porta da loja/hospedaria em " + door.x + ", " + door.y);
            CheckReachable(PrototypeCityLayout.DoorReturn(door), "retorno da loja/hospedaria");
        }
        CheckReachable(PrototypeCityLayout.Citizen + Vector2.down, "aldeao");
        CheckReachable(PrototypeCityLayout.Chest + Vector2.down, "bau");
        CheckReachable(PrototypeCityLayout.Guard + Vector2.down, "patrulha");
        CheckReachable(PrototypeCityLayout.Resident + Vector2.down, "camponesa e poema da capela");
        CheckReachable(PrototypeCityLayout.Explorer + Vector2.down, "exploradora");
        Assert(!PrototypeCityLayout.Blocks.Any(block=>block.Name=="Altar antigo"),"Altar do segredo nao pode permanecer na cidade.");
        foreach (Vector2 point in PrototypeCityLayout.Street) CheckReachable(point, "trecho da viela principal");
        for (float y = 9.6f; y <= 16.2f; y += 0.25f) CheckReachable(new Vector2(0, y), "travessia da ponte de pedra");
        CheckReachable(PrototypeCityLayout.CastleDoor, "entrada do castelo apos a ponte");
        Assert(!Walkable(new Vector2(-4, PrototypeCityLayout.Bridge.y)) && !Walkable(new Vector2(4, PrototypeCityLayout.Bridge.y)),
            "Fosso seco precisa impedir atravessar fora da ponte.");
        Assert(!PrototypeCityLayout.Blocks.Any(b=>b.Name.StartsWith("Lago")||b.Name=="Rio")
            && PrototypeCityLayout.Blocks.Count(b=>b.Name=="Ponte de pedra - piso")==1,
            "Periferia deve ter uma unica ponte ao norte, sem lago ou rio no inicio.");
        Assert(PrototypeCityLayout.CastleDoor.y>PrototypeCityLayout.Bridge.y && PrototypeCityLayout.Bridge.y>PrototypeCityLayout.Spawn.y,
            "Ponte e entrada do castelo precisam ficar ao norte do bairro.");
        Assert(PrototypeCityLayout.Bounds.width<32 && PrototypeCityLayout.Bounds.height>PrototypeCityLayout.Bounds.width,
            "Bairro deve ser compacto e alongado na orientacao norte-sul.");
        Assert(PrototypeCityLayout.SafeSavedPosition(new Vector2(-3.8f, -6)) == PrototypeCityLayout.Spawn
            && PrototypeCityLayout.SafeSavedPosition(new Vector2(-25.5f, -3)) == PrototypeCityLayout.Spawn
            && PrototypeCityLayout.SafeSavedPosition(new Vector2(-4, 12.5f)) == PrototypeCityLayout.Spawn,
            "Save antigo em casa, fora do bairro compacto ou dentro do fosso deve reaparecer na rua central.");
        Assert(PrototypeCityLayout.SafeSavedPosition(PrototypeCityLayout.Spawn) == PrototypeCityLayout.Spawn
            && PrototypeCityLayout.SafeSavedPosition(PrototypeCityLayout.ItemDoor) == PrototypeCityLayout.ItemDoor,
            "Posicoes livres salvas e portas devem conservar suas coordenadas.");
        foreach (Vector2 point in new[] {new Vector2(-12.5f, 0), new Vector2(12.5f, 0), new Vector2(0, -21.5f), new Vector2(0, 21.5f)})
            Assert(!Walkable(point), "Muralha deve bloquear a saida do mapa.");

        int freeCells = 0; var isolated = new List<Vector2>();
        for (int x = 0; x <= PrototypeCityLayout.Bounds.width / Step; x++)
            for (int y = 0; y <= PrototypeCityLayout.Bounds.height / Step; y++) if (Walkable(Position(x, y)))
            { freeCells++; if (!reachable.Contains((x,y))) isolated.Add(Position(x,y)); }
        Assert(reachable.Count == freeCells, "Mapa contem uma area caminhavel isolada do heroi: "
            + string.Join("; ", isolated.Take(12).Select(p=>p.x+", "+p.y)));
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
