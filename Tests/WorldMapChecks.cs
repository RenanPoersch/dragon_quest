using DragonQuest.Progression;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Exploration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

internal static class WorldMapChecks
{
    private static int checks;
    private static void Assert(bool condition, string description) { checks++; if (!condition) throw new Exception(description); }

    private static IEnumerable<CityBlock> At(IReadOnlyList<CityBlock> blocks, Vector2 offset)
        => blocks.Select(block => new CityBlock(block.Name, block.Position.x + offset.x, block.Position.y + offset.y,
            block.Size.x, block.Size.y, block.Color, block.Solid, block.Order));
    private static IEnumerable<CityBlock> WorldBlocks()
        => WorldLayout.Blocks.Concat(At(WorldLayout.CastleIcon, WorldLayout.Castle))
            .Concat(At(WorldLayout.QuarryIcon, WorldLayout.WestForest))
            .Concat(At(WorldLayout.ForestIcon, WorldLayout.EastForest))
            .Concat(At(RuinsLayout.Icon,WorldLayout.Ruins))
            .Concat(At(WorldLayout.VolcanoIcon, WorldLayout.Volcano));

    private sealed class Paths
    {
        private const float HalfWidth = 0.355f, HalfHeight = 0.38f;
        private readonly float Step;
        private readonly Func<Vector2,bool> geometry;
        private readonly Rect bounds;
        private readonly List<Rect> solids;
        private readonly bool island;
        private readonly bool sealedWard;
        private readonly HashSet<(int, int)> reachable = new HashSet<(int, int)>();
        private (int, int) Cell(Vector2 point) => ((int)Math.Round((point.x - bounds.xMin) / Step), (int)Math.Round((point.y - bounds.yMin) / Step));
        private Vector2 Point((int, int) cell) => new Vector2(bounds.xMin + cell.Item1 * Step, bounds.yMin + cell.Item2 * Step);
        public bool Walkable(Vector2 point)
        {
            if (geometry != null) return geometry(point);
            if (point.x - HalfWidth <= bounds.xMin || point.x + HalfWidth >= bounds.xMax
                || point.y - HalfHeight <= bounds.yMin || point.y + HalfHeight >= bounds.yMax) return false;
            if (island)
                foreach (Vector2 corner in new[] { new Vector2(-HalfWidth, -HalfHeight), new Vector2(-HalfWidth, HalfHeight),
                    new Vector2(HalfWidth, -HalfHeight), new Vector2(HalfWidth, HalfHeight) })
                    if (!WorldLayout.IsLand(point + corner)) return false;
            if (sealedWard && WorldLayout.WardOverlaps(new Rect(point.x - HalfWidth, point.y - HalfHeight, HalfWidth * 2, HalfHeight * 2))) return false;
            foreach (Rect solid in solids)
                if (point.x + HalfWidth >= solid.xMin && point.x - HalfWidth <= solid.xMax
                    && point.y + HalfHeight >= solid.yMin && point.y - HalfHeight <= solid.yMax) return false;
            return true;
        }
        public bool Accessible(Vector2 point) => Walkable(point) && reachable.Contains(Cell(point));
        public Vector2? ReachablePoint(Rect area)
        {
            foreach(var cell in reachable) {Vector2 point=Point(cell);if(area.Contains(point)) return point;}
            return null;
        }
        public Paths(Rect limits, IEnumerable<CityBlock> blocks, Vector2 spawn, bool isIsland = false, Vector2? npc = null, Func<Vector2,bool> floor = null, float step = .5f)
        {
            bounds = limits; island = isIsland; geometry = floor; Step = step;
            sealedWard = blocks.Any(block => block.Name == SecretLayout.SealedRocks);
            solids = blocks.Where(block => block.Solid && block.Name != SecretLayout.SealedRocks).Select(block => block.Bounds).ToList();
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

    private static void NavigateTo(WorldNavigation travel, string target)
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
        var travel = new WorldNavigation();
        Assert(travel.Current.Id == WorldNavigation.World && !travel.Current.IsShop, "Demo deve iniciar na ilha e nao numa loja.");
        Assert(travel.Maps.Any(map=>map.Id==WorldNavigation.Ruins&&map.FixedCamera&&!map.IsShop),"Ruinas devem ser um cenario proprio de exploracao.");
        foreach (MapDefinition map in travel.Maps)
            Assert(map.IsShop == map.Id.EndsWith("-shop"), "Florestas, periferia e castelo nao devem ser tratados como lojas.");
        foreach (string destination in new[] { WorldNavigation.Castle, "item-shop", "volcano", "missing", WorldNavigation.World, null })
        {
            MapDefinition before = travel.Current;
            Assert(!travel.TryTravel(destination, out _) && travel.Current == before, "Atalho invalido nao pode alterar o mapa atual.");
        }
        foreach (MapRoute route in travel.Routes)
        {
            travel = ReadyTravel(); NavigateTo(travel, route.From);
            Assert(travel.TryTravel(route.To, out Vector2 arrival) && travel.Current.Id == route.To, "Rota declarada deve trocar para o destino correto.");
            Assert(arrival == route.Arrival && travel.Current.Bounds.Contains(arrival), "Rota deve usar sua chegada especifica dentro do novo mapa.");
            Assert(route.To == WorldNavigation.City ? CityLayoutChecks.Accessible(arrival) : paths[route.To].Accessible(arrival),
                "Chegada bloqueada ou sem caminho de retorno: " + route.From + " -> " + route.To);
            travel = ReadyTravel(); NavigateTo(travel, route.From);
            MapDefinition before = travel.Current;
            Vector2? entry=route.From==WorldNavigation.City ? CityLayoutChecks.ReachablePoint(route.Entrance)
                : paths[route.From].ReachablePoint(route.Entrance);
            Assert(entry.HasValue,
                "Area automatica precisa ser alcancavel: " + route.From + " -> " + route.To);
            Assert(!travel.TryEnter(new Vector2(route.Entrance.center.x,route.Entrance.yMin-0.01f),false,out _) && travel.Current==before,
                "Aproximar-se sem entrar na area nao pode trocar o mapa.");
            Assert(!travel.TryEnter(entry.Value,true,out _) && travel.Current==before,
                "Dialogo, inventario ou batalha suspendem a passagem.");
            Assert(travel.TryEnter(entry.Value,false,out arrival) && travel.Current.Id==route.To && arrival==route.Arrival,
                "Entrar na area deve trocar o cenario sem uma tecla de confirmacao.");
            Assert(!travel.TryEnter(arrival,false,out _) && travel.Current.Id==route.To,
                "Chegada nao pode acionar a saida e devolver imediatamente ao mapa anterior.");
            if(route.From==WorldNavigation.World)
                foreach(Vector2 side in new[]{new Vector2(route.Entrance.xMin+0.05f,route.Entrance.center.y),new Vector2(route.Entrance.xMax-0.05f,route.Entrance.center.y),
                    new Vector2(route.Entrance.center.x,route.Entrance.yMin+0.05f),new Vector2(route.Entrance.center.x,route.Entrance.yMax-0.05f)})
                {
                    travel=ReadyTravel();
                    Assert(paths[WorldNavigation.World].Walkable(side)&&travel.TryEnter(side,false,out _)&&travel.Current.Id==route.To,
                        "POI global deve aceitar entrada pelos quatro lados acessiveis: "+route.To);
                }
        }
        CheckCityDoorways();
        travel = new WorldNavigation();
        for (int i = 0; i < 3; i++)
        {
            Assert(travel.TryTravel(WorldNavigation.City, out _) && travel.TryTravel("item-shop", out _)
                && travel.TryTravel(WorldNavigation.City, out Vector2 shopReturn) && shopReturn == CityLayout.DoorReturn(CityLayout.ItemDoor),
                "Entrar e sair da loja deve devolver a mesma porta da periferia.");
            Assert(travel.TryTravel(WorldNavigation.Castle, out _) && travel.TryTravel(WorldNavigation.City, out _)
                && travel.TryTravel(WorldNavigation.World, out Vector2 worldReturn) && worldReturn == WorldLayout.Castle + Vector2.down * 2.5f,
                "Percurso loja/castelo/periferia/ilha nao pode confundir o retorno global.");
        }
        travel.TryTravel(WorldNavigation.WestForest, out _);
        Assert(!travel.TryTravel(WorldNavigation.EastForest, out _) && !travel.TryTravel("item-shop", out _) && travel.Current.Id == WorldNavigation.WestForest,
            "Floresta deve exigir retorno a ilha antes de entrar noutro ponto.");
        travel.TryTravel(WorldNavigation.World, out Vector2 westReturn);
        travel.TryTravel(WorldNavigation.EastForest, out _); travel.TryTravel(WorldNavigation.World, out Vector2 eastReturn);
        Assert(westReturn != eastReturn, "Florestas diferentes precisam de retornos globais diferentes.");
        travel.TryTravel(WorldNavigation.Ruins,out Vector2 ruinsEntry);
        Assert(ruinsEntry==RuinsLayout.Spawn,"Entrada das ruinas deve usar ponto livre do cenario local.");
        Assert(!travel.TryTravel(WorldNavigation.City,out _)&&!travel.TryTravel("item-shop",out _),"Ruinas exigem voltar a ilha para viajar a outros locais.");
        travel.TryTravel(WorldNavigation.World,out Vector2 ruinsReturn);
        Assert(ruinsReturn==WorldLayout.RuinsReturn,"Saida das ruinas deve devolver ao seu icone global, fora do mar.");
        var secrets=new DragonQuest.Progression.AncientSecrets();secrets.LearnPoem();
        Assert(travel.TryTravel(WorldNavigation.Ruins,out _)&&secrets.TrySolveAltar(DragonQuest.Progression.LegendPoem.Flame),"Poema aprendido deve permitir resolver altar durante a visita.");
        travel.TryTravel(WorldNavigation.World,out _);travel.TryTravel(WorldNavigation.Ruins,out _);
        Assert(secrets.AltarSolved&&secrets.CompletedCount==1&&!secrets.TrySolveAltar(DragonQuest.Progression.LegendPoem.Flame),"Sair e retornar nao duplica nem apaga a solucao do altar.");
        Assert(new WorldNavigation().Current.Id == WorldNavigation.World, "Uma nova sessao nao pode herdar o mapa de outra.");
        foreach (MapDefinition map in travel.Maps)
        {
            travel=ReadyTravel();NavigateTo(travel,map.Id);
            Assert(!travel.TryEnter(map.Spawn,false,out _) && travel.Current.Id==map.Id,"Posicao inicial nao pode disparar uma passagem automaticamente.");
        }
        travel=new WorldNavigation();
        Assert(!travel.TryEnter(travel.RouteFrom(WorldNavigation.World,WorldNavigation.Volcano).Entrance.center,false,out _)
            &&travel.Current.Id==WorldNavigation.World,"Entrada automatica do vulcao tambem exige selos abertos.");
        Assert(!travel.TryRecoverAtInn(out _),"Recuperacao nao pode virar atalho de exploracao.");
        travel=ReadyTravel();travel.TryTravel(WorldNavigation.Volcano,out _);
        Assert(travel.TryRecoverAtInn(out Vector2 recovery)&&travel.Current.Id==WorldNavigation.Inn&&recovery==InnLayout.Spawn,"Derrota no vulcao deve devolver a hospedaria.");
        Assert(!travel.TryEnter(recovery,false,out _)&&travel.Current.Id==WorldNavigation.Inn,"Recuperacao nao pode acionar a saida da hospedaria.");
        travel=ReadyTravel();travel.TryTravel(WorldNavigation.City,out _);travel.TryTravel(WorldNavigation.Castle,out _);
        Assert(travel.TryRecoverAtInn(out recovery)&&travel.Current.Id==WorldNavigation.Inn&&!travel.TryEnter(recovery,false,out _),"Derrota comum no castelo tambem recupera na hospedaria sem retornar automaticamente.");
    }
    private static void CheckCityDoorways()
    {
        foreach(var pair in new[]{("item-shop","Loja de itens"),("equipment-shop","Loja de equipamentos"),
            ("materia-shop","Loja de materias"),(WorldNavigation.Inn,"Hospedaria")})
        {
            var travel=new WorldNavigation(); travel.TryTravel(WorldNavigation.City,out _);
            var route=travel.RouteFrom(WorldNavigation.City,pair.Item1);
            var building=CityLayout.Buildings.First(b=>b.Name==pair.Item2);
            var visibleDoor=new Vector2(building.Position.x,building.Bounds.yMin-0.035f);
            Assert((route.Entrance.center-visibleDoor).sqrMagnitude<0.000001f,"Passagem deve coincidir com a porta da fachada: "+pair.Item2);
            var approach=CityLayout.DoorApproach(visibleDoor);
            Assert(CityLayoutChecks.Accessible(approach),"Pes do heroi precisam alcancar a entrada sem atravessar o collider da casa.");
            Assert(!travel.TryEnter(new Vector2(visibleDoor.x,route.Entrance.yMin-0.1f),false,out _),
                "Passar no patio distante da porta nao pode entrar na loja.");
            Assert(!travel.TryEnter(new Vector2(visibleDoor.x+building.Size.x*0.3f,approach.y),false,out _),
                "Passar diante da janela nao pode entrar na loja.");
            Assert(travel.TryEnter(approach,false,out _)&&travel.Current.Id==pair.Item1,"Encostar nos acessos das portas deve entrar automaticamente.");
        }
        int sampled=0;
        for(float x=CityLayout.Bounds.xMin+0.5f;x<CityLayout.Bounds.xMax;x+=0.5f)
        {
            var point=new Vector2(x,CityLayout.Exit.y);
            if(!CityLayoutChecks.Accessible(point)) continue;
            sampled++;
            var travel=new WorldNavigation(); travel.TryTravel(WorldNavigation.City,out _);
            Assert(travel.TryEnter(point,false,out _)&&travel.Current.Id==WorldNavigation.World,
                "Saida sul deve cobrir toda a faixa caminhavel, incluindo laterais: X="+x);
        }
        Assert(sampled>25,"Conferencia da saida deve cobrir varios pontos ao longo da largura da periferia.");
    }

    private static WorldNavigation ReadyTravel()
    {
        var secrets=new DragonQuest.Progression.AncientSecrets();
        secrets.LearnPoem();secrets.TrySolveAltar(DragonQuest.Progression.LegendPoem.Flame);
        secrets.Activate(DragonQuest.Progression.MonolithSymbol.Moon);secrets.Activate(DragonQuest.Progression.MonolithSymbol.Star);secrets.Activate(DragonQuest.Progression.MonolithSymbol.Sun);
        secrets.TryCollectScale();secrets.TryOpenPassage();return new WorldNavigation(secrets);
    }

    public static int Run(string exportDirectory = null)
    {
        var world = new Paths(WorldLayout.Bounds, WorldBlocks(), WorldLayout.Spawn, true);
        var west = new Paths(WorldLayout.ForestBounds, WorldLayout.CreateForestBlocks(false), WorldLayout.ForestSpawn, npc: new Vector2(1.5f, 1));
        var east = new Paths(WorldLayout.ForestBounds, WorldLayout.CreateForestBlocks(true), WorldLayout.ForestSpawn, npc: new Vector2(1.5f, 1));
        var castle = new Paths(ThroneRoomLayout.Bounds,Array.Empty<CityBlock>(),ThroneRoomLayout.Spawn,floor:ThroneRoomLayout.IsWalkable,step:.1f);
        var ruins = new Paths(RuinsLayout.Bounds,Array.Empty<CityBlock>(),RuinsLayout.Spawn);
        var volcano=new Paths(VolcanoLayout.Bounds,VolcanoLayout.Blocks.Concat(new[]{new CityBlock("Dragao",0,8,1.8f,1.8f,"",true,0)}),VolcanoLayout.Spawn);
        var inn=new Paths(InnLayout.Bounds,Array.Empty<CityBlock>(),InnLayout.Spawn,floor:InnLayout.IsWalkable,step:.1f);
        var openWorld = new Paths(WorldLayout.Bounds, WorldBlocks().Where(block=>block.Name!=SecretLayout.SealedRocks),WorldLayout.Spawn,true);
        var item=CompactShopLayout.Items; var materia=CompactShopLayout.Materia;
        var itemPaths=new Paths(item.Bounds,Array.Empty<CityBlock>(),item.Spawn,floor:item.IsWalkable,step:.1f);
        var materiaPaths=new Paths(materia.Bounds,Array.Empty<CityBlock>(),materia.Spawn,floor:materia.IsWalkable,step:.1f);
        var paths = new Dictionary<string, Paths>
        {
            { WorldNavigation.World, openWorld }, { WorldNavigation.WestForest, west }, { WorldNavigation.EastForest, east },
            { WorldNavigation.Volcano,volcano }, { WorldNavigation.Inn,inn },
            { WorldNavigation.Castle, castle }, { WorldNavigation.Ruins,ruins }, { "item-shop", itemPaths }, { WeaponShopLayout.MapId, new Paths(WeaponShopLayout.Bounds,Array.Empty<CityBlock>(),WeaponShopLayout.Spawn,floor:WeaponShopLayout.IsWalkable,step:.1f) }, { "materia-shop", materiaPaths }
        };
        foreach (Vector2 icon in new[] { WorldLayout.Castle, WorldLayout.WestForest, WorldLayout.EastForest,WorldLayout.Ruins })
            Assert(world.Accessible(icon + Vector2.down * 0.85f), "Entrada global deve ser acessivel pela ilha.");
        Assert(world.Accessible(WorldLayout.Trail), "Passagem oculta deve poder ser examinada.");
        Assert(!world.Accessible(WorldLayout.Volcano), "Vulcao nao pode ser alcancado enquanto a trilha estiver bloqueada.");
        Assert(!world.Walkable(WorldLayout.Volcano + Vector2.down * (WorldLayout.WardRadius - 0.1f)), "Barreira precisa impedir atravessar a trilha.");
        Assert(openWorld.Accessible(WorldLayout.Volcano), "Remover o colisor dos selos deve abrir um caminho real ate o vulcao.");
        Assert(openWorld.Accessible(WorldLayout.Trail), "Trilha aberta deve permitir retornar ao sul da ilha.");
        Assert(WorldLayout.Ruins.x < 0 && WorldLayout.Ruins.y < 0
            && WorldLayout.Volcano == -WorldLayout.Ruins
            && WorldLayout.WestForest.x < 0 && WorldLayout.WestForest.y > 0,
            "Ruinas no sudoeste, vulcao exatamente oposto e pedreira no noroeste.");
        for (int i = 0; i < 32; i++)
        {
            double angle = i * Math.PI * 2 / 32;
            Vector2 edge = WorldLayout.Volcano + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (WorldLayout.WardRadius - 0.05f);
            Assert(!world.Walkable(edge), "Barreira circular deve bloquear por todos os lados, incluindo diagonais.");
        }
        Assert(WorldLayout.SafeSavedPosition(new Vector2(-15, 5), true) == WorldLayout.Spawn,
            "Save antigo fora da nova costa deve recuperar uma posicao livre, conservando a jornada.");
        Assert(WorldLayout.SafeSavedPosition(WorldLayout.Volcano, false) == WorldLayout.Trail
            && openWorld.Accessible(WorldLayout.SafeSavedPosition(WorldLayout.Volcano, true)),
            "Save dentro da barreira fechada deve retornar ao acesso; aberta conserva a posicao.");
        Assert(volcano.Accessible(VolcanoLayout.Dragon+Vector2.down*1.5f),"Trilha de basalto deve levar a um ponto de interacao do dragao.");
        Assert(!volcano.Walkable(new Vector2(5,-5)),"A lava deve impedir caminhar fora da trilha.");
        foreach(var block in SecretLayout.WestMonoliths)
            Assert(west.Accessible(block.Position+Vector2.down), "Cada monolito deve ter um ponto livre de interacao.");
        Assert(west.Accessible(SecretLayout.Inscription+Vector2.down), "Inscricao deve poder ser lida.");
        Assert(east.Accessible(SecretLayout.ClawStone+Vector2.down), "Pedra com marcas precisa ser acessivel.");
        Assert(east.Accessible(SecretLayout.HollowTree+Vector2.left*1.5f), "Arvore oca precisa de acesso dentro do alcance de E.");
        Assert(ruins.Accessible(RuinsLayout.Altar),"Corredor lateral deve chegar ao altar.");
        Assert(Math.Abs(RuinsLayout.FloorAt(RuinsLayout.Altar.x)-RuinsLayout.Altar.y)<.001f,
            "O caminho deve subir ate a plataforma do altar.");
        Assert(ruins.Accessible(RuinsLayout.Exit),"Ruinas precisam permitir retornar a ilha.");
        foreach (Vector2 water in new[] { new Vector2(-18, 0), new Vector2(18, 0), new Vector2(0, -12), new Vector2(0, 12), new Vector2(-14, -8) })
            Assert(!world.Walkable(water), "Mar e baia nao podem permitir caminhar fora da ilha.");
        Assert(west.Accessible(WorldLayout.LocalExit) && east.Accessible(WorldLayout.LocalExit), "Duas florestas devem ter saidas acessiveis.");
        Assert(west.Accessible(new Vector2(1.5f, 0)) && east.Accessible(new Vector2(1.5f, 0)), "Moradores das florestas precisam ser alcancaveis.");
        Assert(castle.Accessible(ThroneRoomLayout.Exit) && castle.Accessible(ThroneRoomLayout.Audience), "Castelo precisa de caminho ate o trono e a saida.");
        Assert(CityLayoutChecks.Accessible(WorldLayout.CastleDoor) && CityLayoutChecks.Accessible(WorldLayout.CityExit),
            "Periferia precisa permitir entrar no castelo e voltar a ilha.");
        foreach (IReadOnlyList<CityBlock> icon in new[] { WorldLayout.CastleIcon, WorldLayout.ForestIcon })
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
