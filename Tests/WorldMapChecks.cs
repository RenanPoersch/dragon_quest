using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonQuest.Prototype;
using UnityEngine;

internal static class WorldMapChecks
{
    private static int checks;
    private static void Assert(bool condition, string description) { checks++; if (!condition) throw new Exception(description); }

    private static IEnumerable<CityBlock> At(IReadOnlyList<CityBlock> blocks, Vector2 offset)
        => blocks.Select(block => new CityBlock(block.Name, block.Position.x + offset.x, block.Position.y + offset.y,
            block.Size.x, block.Size.y, block.Color, block.Solid, block.Order));
    private static IEnumerable<CityBlock> WorldBlocks()
        => PrototypeWorldLayout.Blocks.Concat(At(PrototypeWorldLayout.CastleIcon, PrototypeWorldLayout.Castle))
            .Concat(At(PrototypeWorldLayout.ForestIcon, PrototypeWorldLayout.WestForest))
            .Concat(At(PrototypeWorldLayout.ForestIcon, PrototypeWorldLayout.EastForest))
            .Concat(At(PrototypeWorldLayout.VolcanoIcon, PrototypeWorldLayout.Volcano));

    private sealed class Paths
    {
        private const float Step = 0.5f, HalfWidth = 0.355f, HalfHeight = 0.38f;
        private readonly Rect bounds;
        private readonly List<Rect> solids;
        private readonly bool island;
        private readonly HashSet<(int, int)> reachable = new HashSet<(int, int)>();
        private (int, int) Cell(Vector2 point) => ((int)Math.Round((point.x - bounds.xMin) / Step), (int)Math.Round((point.y - bounds.yMin) / Step));
        private Vector2 Point((int, int) cell) => new Vector2(bounds.xMin + cell.Item1 * Step, bounds.yMin + cell.Item2 * Step);
        public bool Walkable(Vector2 point)
        {
            if (point.x - HalfWidth <= bounds.xMin || point.x + HalfWidth >= bounds.xMax
                || point.y - HalfHeight <= bounds.yMin || point.y + HalfHeight >= bounds.yMax) return false;
            if (island)
                foreach (Vector2 corner in new[] { new Vector2(-HalfWidth, -HalfHeight), new Vector2(-HalfWidth, HalfHeight),
                    new Vector2(HalfWidth, -HalfHeight), new Vector2(HalfWidth, HalfHeight) })
                    if (!PrototypeWorldLayout.IsLand(point + corner)) return false;
            foreach (Rect solid in solids)
                if (point.x + HalfWidth >= solid.xMin && point.x - HalfWidth <= solid.xMax
                    && point.y + HalfHeight >= solid.yMin && point.y - HalfHeight <= solid.yMax) return false;
            return true;
        }
        public bool Accessible(Vector2 point) => Walkable(point) && reachable.Contains(Cell(point));
        public Paths(Rect limits, IEnumerable<CityBlock> blocks, Vector2 spawn, bool isIsland = false, Vector2? npc = null)
        {
            bounds = limits; island = isIsland;
            solids = blocks.Where(block => block.Solid).Select(block => block.Bounds).ToList();
            if (npc.HasValue) solids.Add(new Rect(npc.Value.x - 0.275f, npc.Value.y - 0.3f, 0.55f, 0.6f));
            Assert(Walkable(spawn), "Entrada de mapa deve estar fora de colisores e do mar.");
            var pending = new Queue<(int, int)>();
            reachable.Add(Cell(spawn)); pending.Enqueue(Cell(spawn));
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var offset in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var next = (current.Item1 + offset.Item1, current.Item2 + offset.Item2);
                    if (reachable.Contains(next) || !Walkable(Point(next))) continue;
                    reachable.Add(next); pending.Enqueue(next);
                }
            }
        }
    }

    private static void NavigateTo(PrototypeTravel travel, string target)
    {
        if (travel.Current.Id == target) return;
        var pending = new Queue<string>();
        var previous = new Dictionary<string, string> { { travel.Current.Id, null } };
        pending.Enqueue(travel.Current.Id);
        while (pending.Count > 0 && !previous.ContainsKey(target))
        {
            string from = pending.Dequeue();
            foreach (MapRoute route in travel.Routes)
                if (route.From == from && !previous.ContainsKey(route.To)) { previous.Add(route.To, from); pending.Enqueue(route.To); }
        }
        var path = new List<string>();
        for (string to = target; previous[to] != null; to = previous[to]) path.Add(to);
        path.Reverse();
        foreach (string to in path) if (!travel.TryTravel(to, out _)) throw new Exception("Caminho de teste recusado.");
    }

    private static void CheckNavigation(Dictionary<string, Paths> paths)
    {
        var travel = new PrototypeTravel();
        Assert(travel.Current.Id == PrototypeTravel.World && !travel.Current.IsShop, "Demo deve iniciar na ilha e nao numa loja.");
        foreach (MapDefinition map in travel.Maps)
            Assert(map.IsShop == map.Id.EndsWith("-shop"), "Florestas, periferia e castelo nao devem ser tratados como lojas.");
        foreach (string destination in new[] { PrototypeTravel.Castle, "item-shop", "volcano", "missing", PrototypeTravel.World, null })
        {
            MapDefinition before = travel.Current;
            Assert(!travel.TryTravel(destination, out _) && travel.Current == before, "Atalho invalido nao pode alterar o mapa atual.");
        }
        foreach (MapRoute route in travel.Routes)
        {
            travel = new PrototypeTravel(); NavigateTo(travel, route.From);
            Assert(travel.TryTravel(route.To, out Vector2 arrival) && travel.Current.Id == route.To, "Rota declarada deve trocar para o destino correto.");
            Assert(arrival == route.Arrival && travel.Current.Bounds.Contains(arrival), "Rota deve usar sua chegada especifica dentro do novo mapa.");
            Assert(route.To == PrototypeTravel.City ? CityLayoutChecks.Accessible(arrival) : paths[route.To].Accessible(arrival),
                "Chegada bloqueada ou sem caminho de retorno: " + route.From + " -> " + route.To);
        }
        travel = new PrototypeTravel();
        for (int i = 0; i < 3; i++)
        {
            Assert(travel.TryTravel(PrototypeTravel.City, out _) && travel.TryTravel("item-shop", out _)
                && travel.TryTravel(PrototypeTravel.City, out Vector2 shopReturn) && shopReturn == new Vector2(-9, 1.7f),
                "Entrar e sair da loja deve devolver a mesma porta da periferia.");
            Assert(travel.TryTravel(PrototypeTravel.Castle, out _) && travel.TryTravel(PrototypeTravel.City, out _)
                && travel.TryTravel(PrototypeTravel.World, out Vector2 worldReturn) && worldReturn == PrototypeWorldLayout.Castle + Vector2.down * 2.5f,
                "Percurso loja/castelo/periferia/ilha nao pode confundir o retorno global.");
        }
        travel.TryTravel(PrototypeTravel.WestForest, out _);
        Assert(!travel.TryTravel(PrototypeTravel.EastForest, out _) && !travel.TryTravel("item-shop", out _) && travel.Current.Id == PrototypeTravel.WestForest,
            "Floresta deve exigir retorno a ilha antes de entrar noutro ponto.");
        travel.TryTravel(PrototypeTravel.World, out Vector2 westReturn);
        travel.TryTravel(PrototypeTravel.EastForest, out _); travel.TryTravel(PrototypeTravel.World, out Vector2 eastReturn);
        Assert(westReturn != eastReturn, "Florestas diferentes precisam de retornos globais diferentes.");
        Assert(new PrototypeTravel().Current.Id == PrototypeTravel.World, "Uma nova sessao nao pode herdar o mapa de outra.");
    }

    public static int Run(string exportDirectory = null)
    {
        var world = new Paths(PrototypeWorldLayout.Bounds, WorldBlocks(), PrototypeWorldLayout.Spawn, true);
        var west = new Paths(PrototypeWorldLayout.ForestBounds, PrototypeWorldLayout.CreateForestBlocks(false), PrototypeWorldLayout.ForestSpawn, npc: new Vector2(1.5f, 1));
        var east = new Paths(PrototypeWorldLayout.ForestBounds, PrototypeWorldLayout.CreateForestBlocks(true), PrototypeWorldLayout.ForestSpawn, npc: new Vector2(1.5f, 1));
        var castle = new Paths(PrototypeWorldLayout.CastleBounds, PrototypeWorldLayout.CastleBlocks, PrototypeWorldLayout.CastleSpawn, npc: PrototypeWorldLayout.Throne);
        var shop = new Paths(new Rect(-7, -5, 14, 10), new[]
        {
            new CityBlock("Balcao", 0, 0.15f, 4, 0.6f, "", true, 0),
            new CityBlock("Estante oeste", -4.8f, 1.5f, 1.5f, 3, "", true, 0),
            new CityBlock("Estante leste", 4.8f, 1.5f, 1.5f, 3, "", true, 0)
        }, new Vector2(0, -2.7f));
        var paths = new Dictionary<string, Paths>
        {
            { PrototypeTravel.World, world }, { PrototypeTravel.WestForest, west }, { PrototypeTravel.EastForest, east },
            { PrototypeTravel.Castle, castle }, { "item-shop", shop }, { "equipment-shop", shop }, { "materia-shop", shop }
        };
        foreach (Vector2 icon in new[] { PrototypeWorldLayout.Castle, PrototypeWorldLayout.WestForest, PrototypeWorldLayout.EastForest })
            Assert(world.Accessible(icon + Vector2.down * 0.85f), "Entrada global deve ser acessivel pela ilha.");
        Assert(world.Accessible(PrototypeWorldLayout.Trail), "Passagem oculta deve poder ser examinada.");
        Assert(!world.Accessible(PrototypeWorldLayout.Volcano), "Vulcao nao pode ser alcancado enquanto a trilha estiver bloqueada.");
        Assert(!world.Walkable(new Vector2(3, 6.5f)), "Rochas precisam impedir atravesar a trilha.");
        var openWorld = new Paths(PrototypeWorldLayout.Bounds, WorldBlocks().Where(block=>block.Name!=PrototypeSecretLayout.SealedRocks),PrototypeWorldLayout.Spawn,true);
        Assert(openWorld.Accessible(PrototypeWorldLayout.Volcano), "Remover o colisor dos selos deve abrir um caminho real ate o vulcao.");
        Assert(openWorld.Accessible(PrototypeWorldLayout.Trail), "Trilha aberta deve permitir retornar ao sul da ilha.");
        foreach(var block in PrototypeSecretLayout.WestMonoliths)
            Assert(west.Accessible(block.Position+Vector2.down), "Cada monolito deve ter um ponto livre de interacao.");
        Assert(west.Accessible(PrototypeSecretLayout.Inscription+Vector2.down), "Inscricao deve poder ser lida.");
        Assert(east.Accessible(PrototypeSecretLayout.ClawStone+Vector2.down), "Pedra com marcas precisa ser acessivel.");
        Assert(east.Accessible(PrototypeSecretLayout.HollowTree+Vector2.left*1.5f), "Arvore oca precisa de acesso dentro do alcance de E.");
        foreach (Vector2 water in new[] { new Vector2(-18, 0), new Vector2(18, 0), new Vector2(0, -12), new Vector2(0, 12), new Vector2(-14, -8) })
            Assert(!world.Walkable(water), "Mar e baia nao podem permitir caminhar fora da ilha.");
        Assert(west.Accessible(PrototypeWorldLayout.LocalExit) && east.Accessible(PrototypeWorldLayout.LocalExit), "Duas florestas devem ter saidas acessiveis.");
        Assert(west.Accessible(new Vector2(1.5f, 0)) && east.Accessible(new Vector2(1.5f, 0)), "Moradores das florestas precisam ser alcancaveis.");
        Assert(castle.Accessible(PrototypeWorldLayout.CastleExit) && castle.Accessible(PrototypeWorldLayout.Throne + Vector2.down), "Castelo precisa de caminho ate o trono e a saida.");
        Assert(CityLayoutChecks.Accessible(PrototypeWorldLayout.CastleDoor) && CityLayoutChecks.Accessible(PrototypeWorldLayout.CityExit),
            "Periferia precisa permitir entrar no castelo e voltar a ilha.");
        foreach (IReadOnlyList<CityBlock> icon in new[] { PrototypeWorldLayout.CastleIcon, PrototypeWorldLayout.ForestIcon })
        {
            float height = icon.Max(block => block.Bounds.yMax) - icon.Min(block => block.Bounds.yMin);
            Assert(height > 1.05f && height < 1.8f, "Icones globais devem ser apenas um pouco mais altos que o heroi.");
        }
        CheckNavigation(paths);
        if (exportDirectory != null)
            File.WriteAllText(Path.Combine(exportDirectory, "WorldGeometry.json"), JsonSerializer.Serialize(WorldBlocks().Select(block => new
            { name = block.Name, x = block.Position.x, y = block.Position.y, width = block.Size.x, height = block.Size.y, color = block.Color, solid = block.Solid, order = block.Order })));
        return checks;
    }
}
