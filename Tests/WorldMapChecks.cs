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
            .Concat(At(PrototypeWorldLayout.QuarryIcon, PrototypeWorldLayout.WestForest))
            .Concat(At(PrototypeWorldLayout.ForestIcon, PrototypeWorldLayout.EastForest))
            .Concat(At(PrototypeRuinsLayout.Icon,PrototypeWorldLayout.Ruins))
            .Concat(At(PrototypeWorldLayout.VolcanoIcon, PrototypeWorldLayout.Volcano));

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
                    if (!PrototypeWorldLayout.IsLand(point + corner)) return false;
            if (sealedWard && PrototypeWorldLayout.WardOverlaps(new Rect(point.x - HalfWidth, point.y - HalfHeight, HalfWidth * 2, HalfHeight * 2))) return false;
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
            sealedWard = blocks.Any(block => block.Name == PrototypeSecretLayout.SealedRocks);
            solids = blocks.Where(block => block.Solid && block.Name != PrototypeSecretLayout.SealedRocks).Select(block => block.Bounds).ToList();
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
        Assert(travel.Maps.Any(map=>map.Id==PrototypeTravel.Ruins&&map.FixedCamera&&!map.IsShop),"Ruinas devem ser um cenario proprio de exploracao.");
        foreach (MapDefinition map in travel.Maps)
            Assert(map.IsShop == map.Id.EndsWith("-shop"), "Florestas, periferia e castelo nao devem ser tratados como lojas.");
        foreach (string destination in new[] { PrototypeTravel.Castle, "item-shop", "volcano", "missing", PrototypeTravel.World, null })
        {
            MapDefinition before = travel.Current;
            Assert(!travel.TryTravel(destination, out _) && travel.Current == before, "Atalho invalido nao pode alterar o mapa atual.");
        }
        foreach (MapRoute route in travel.Routes)
        {
            travel = ReadyTravel(); NavigateTo(travel, route.From);
            Assert(travel.TryTravel(route.To, out Vector2 arrival) && travel.Current.Id == route.To, "Rota declarada deve trocar para o destino correto.");
            Assert(arrival == route.Arrival && travel.Current.Bounds.Contains(arrival), "Rota deve usar sua chegada especifica dentro do novo mapa.");
            Assert(route.To == PrototypeTravel.City ? CityLayoutChecks.Accessible(arrival) : paths[route.To].Accessible(arrival),
                "Chegada bloqueada ou sem caminho de retorno: " + route.From + " -> " + route.To);
            travel = ReadyTravel(); NavigateTo(travel, route.From);
            MapDefinition before = travel.Current;
            Vector2? entry=route.From==PrototypeTravel.City ? CityLayoutChecks.ReachablePoint(route.Entrance)
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
            if(route.From==PrototypeTravel.World)
                foreach(Vector2 side in new[]{new Vector2(route.Entrance.xMin+0.05f,route.Entrance.center.y),new Vector2(route.Entrance.xMax-0.05f,route.Entrance.center.y),
                    new Vector2(route.Entrance.center.x,route.Entrance.yMin+0.05f),new Vector2(route.Entrance.center.x,route.Entrance.yMax-0.05f)})
                {
                    travel=ReadyTravel();
                    Assert(paths[PrototypeTravel.World].Walkable(side)&&travel.TryEnter(side,false,out _)&&travel.Current.Id==route.To,
                        "POI global deve aceitar entrada pelos quatro lados acessiveis: "+route.To);
                }
        }
        CheckCityDoorways();
        travel = new PrototypeTravel();
        for (int i = 0; i < 3; i++)
        {
            Assert(travel.TryTravel(PrototypeTravel.City, out _) && travel.TryTravel("item-shop", out _)
                && travel.TryTravel(PrototypeTravel.City, out Vector2 shopReturn) && shopReturn == PrototypeCityLayout.DoorReturn(PrototypeCityLayout.ItemDoor),
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
        travel.TryTravel(PrototypeTravel.Ruins,out Vector2 ruinsEntry);
        Assert(ruinsEntry==PrototypeRuinsLayout.Spawn,"Entrada das ruinas deve usar ponto livre do cenario local.");
        Assert(!travel.TryTravel(PrototypeTravel.City,out _)&&!travel.TryTravel("item-shop",out _),"Ruinas exigem voltar a ilha para viajar a outros locais.");
        travel.TryTravel(PrototypeTravel.World,out Vector2 ruinsReturn);
        Assert(ruinsReturn==PrototypeWorldLayout.RuinsReturn,"Saida das ruinas deve devolver ao seu icone global, fora do mar.");
        var secrets=new DragonQuest.Progression.AncientSecrets();secrets.LearnPoem();
        Assert(travel.TryTravel(PrototypeTravel.Ruins,out _)&&secrets.TrySolveAltar(DragonQuest.Progression.LegendPoem.Flame),"Poema aprendido deve permitir resolver altar durante a visita.");
        travel.TryTravel(PrototypeTravel.World,out _);travel.TryTravel(PrototypeTravel.Ruins,out _);
        Assert(secrets.AltarSolved&&secrets.CompletedCount==1&&!secrets.TrySolveAltar(DragonQuest.Progression.LegendPoem.Flame),"Sair e retornar nao duplica nem apaga a solucao do altar.");
        Assert(new PrototypeTravel().Current.Id == PrototypeTravel.World, "Uma nova sessao nao pode herdar o mapa de outra.");
        foreach (MapDefinition map in travel.Maps)
        {
            travel=ReadyTravel();NavigateTo(travel,map.Id);
            Assert(!travel.TryEnter(map.Spawn,false,out _) && travel.Current.Id==map.Id,"Posicao inicial nao pode disparar uma passagem automaticamente.");
        }
        travel=new PrototypeTravel();
        Assert(!travel.TryEnter(travel.RouteFrom(PrototypeTravel.World,PrototypeTravel.Volcano).Entrance.center,false,out _)
            &&travel.Current.Id==PrototypeTravel.World,"Entrada automatica do vulcao tambem exige selos abertos.");
        Assert(!travel.TryRecoverAtInn(out _),"Recuperacao nao pode virar atalho de exploracao.");
        travel=ReadyTravel();travel.TryTravel(PrototypeTravel.Volcano,out _);
        Assert(travel.TryRecoverAtInn(out Vector2 recovery)&&travel.Current.Id==PrototypeTravel.Inn&&recovery==PrototypeInnLayout.Spawn,"Derrota no vulcao deve devolver a hospedaria.");
        Assert(!travel.TryEnter(recovery,false,out _)&&travel.Current.Id==PrototypeTravel.Inn,"Recuperacao nao pode acionar a saida da hospedaria.");
        travel=ReadyTravel();travel.TryTravel(PrototypeTravel.City,out _);travel.TryTravel(PrototypeTravel.Castle,out _);
        Assert(travel.TryRecoverAtInn(out recovery)&&travel.Current.Id==PrototypeTravel.Inn&&!travel.TryEnter(recovery,false,out _),"Derrota comum no castelo tambem recupera na hospedaria sem retornar automaticamente.");
    }
    private static void CheckCityDoorways()
    {
        foreach(var pair in new[]{("item-shop","Loja de itens"),("equipment-shop","Loja de equipamentos"),
            ("materia-shop","Loja de materias"),(PrototypeTravel.Inn,"Hospedaria")})
        {
            var travel=new PrototypeTravel(); travel.TryTravel(PrototypeTravel.City,out _);
            var route=travel.RouteFrom(PrototypeTravel.City,pair.Item1);
            var building=PrototypeCityLayout.Buildings.First(b=>b.Name==pair.Item2);
            var visibleDoor=new Vector2(building.Position.x,building.Bounds.yMin-0.035f);
            Assert((route.Entrance.center-visibleDoor).sqrMagnitude<0.000001f,"Passagem deve coincidir com a porta da fachada: "+pair.Item2);
            var approach=PrototypeCityLayout.DoorApproach(visibleDoor);
            Assert(CityLayoutChecks.Accessible(approach),"Pes do heroi precisam alcancar a entrada sem atravessar o collider da casa.");
            Assert(!travel.TryEnter(new Vector2(visibleDoor.x,route.Entrance.yMin-0.1f),false,out _),
                "Passar no patio distante da porta nao pode entrar na loja.");
            Assert(!travel.TryEnter(new Vector2(visibleDoor.x+building.Size.x*0.3f,approach.y),false,out _),
                "Passar diante da janela nao pode entrar na loja.");
            Assert(travel.TryEnter(approach,false,out _)&&travel.Current.Id==pair.Item1,"Encostar nos acessos das portas deve entrar automaticamente.");
        }
        int sampled=0;
        for(float x=PrototypeCityLayout.Bounds.xMin+0.5f;x<PrototypeCityLayout.Bounds.xMax;x+=0.5f)
        {
            var point=new Vector2(x,PrototypeCityLayout.Exit.y);
            if(!CityLayoutChecks.Accessible(point)) continue;
            sampled++;
            var travel=new PrototypeTravel(); travel.TryTravel(PrototypeTravel.City,out _);
            Assert(travel.TryEnter(point,false,out _)&&travel.Current.Id==PrototypeTravel.World,
                "Saida sul deve cobrir toda a faixa caminhavel, incluindo laterais: X="+x);
        }
        Assert(sampled>25,"Conferencia da saida deve cobrir varios pontos ao longo da largura da periferia.");
    }

    private static PrototypeTravel ReadyTravel()
    {
        var secrets=new DragonQuest.Progression.AncientSecrets();
        secrets.LearnPoem();secrets.TrySolveAltar(DragonQuest.Progression.LegendPoem.Flame);
        secrets.Activate(DragonQuest.Progression.MonolithSymbol.Moon);secrets.Activate(DragonQuest.Progression.MonolithSymbol.Star);secrets.Activate(DragonQuest.Progression.MonolithSymbol.Sun);
        secrets.TryCollectScale();secrets.TryOpenPassage();return new PrototypeTravel(secrets);
    }

    public static int Run(string exportDirectory = null)
    {
        var world = new Paths(PrototypeWorldLayout.Bounds, WorldBlocks(), PrototypeWorldLayout.Spawn, true);
        var west = new Paths(PrototypeWorldLayout.ForestBounds, PrototypeWorldLayout.CreateForestBlocks(false), PrototypeWorldLayout.ForestSpawn, npc: new Vector2(1.5f, 1));
        var east = new Paths(PrototypeWorldLayout.ForestBounds, PrototypeWorldLayout.CreateForestBlocks(true), PrototypeWorldLayout.ForestSpawn, npc: new Vector2(1.5f, 1));
        var castle = new Paths(PrototypeWorldLayout.CastleBounds, PrototypeWorldLayout.CastleBlocks, PrototypeWorldLayout.CastleSpawn, npc: PrototypeWorldLayout.Throne);
        var ruins = new Paths(PrototypeRuinsLayout.Bounds,Array.Empty<CityBlock>(),PrototypeRuinsLayout.Spawn);
        var volcano=new Paths(PrototypeVolcanoLayout.Bounds,PrototypeVolcanoLayout.Blocks.Concat(new[]{new CityBlock("Dragao",0,8,1.8f,1.8f,"",true,0)}),PrototypeVolcanoLayout.Spawn);
        var inn=new Paths(PrototypeInnLayout.Bounds,Array.Empty<CityBlock>(),PrototypeInnLayout.Spawn,floor:PrototypeInnLayout.IsWalkable,step:.1f);
        var openWorld = new Paths(PrototypeWorldLayout.Bounds, WorldBlocks().Where(block=>block.Name!=PrototypeSecretLayout.SealedRocks),PrototypeWorldLayout.Spawn,true);
        var shop = new Paths(new Rect(-7, -5, 14, 10), new[]
        {
            new CityBlock("Balcao", 0, 0.15f, 4, 0.6f, "", true, 0),
            new CityBlock("Estante oeste", -4.8f, 1.5f, 1.5f, 3, "", true, 0),
            new CityBlock("Estante leste", 4.8f, 1.5f, 1.5f, 3, "", true, 0),
            new CityBlock("Parede norte",0,4.7f,14,.6f,"",true,0),new CityBlock("Parede sul",0,-4.7f,14,.6f,"",true,0),
            new CityBlock("Parede oeste",-6.7f,0,.6f,10,"",true,0),new CityBlock("Parede leste",6.7f,0,.6f,10,"",true,0)
        }, new Vector2(0, -2.7f));
        var paths = new Dictionary<string, Paths>
        {
            { PrototypeTravel.World, openWorld }, { PrototypeTravel.WestForest, west }, { PrototypeTravel.EastForest, east },
            { PrototypeTravel.Volcano,volcano }, { PrototypeTravel.Inn,inn },
            { PrototypeTravel.Castle, castle }, { PrototypeTravel.Ruins,ruins }, { "item-shop", shop }, { WeaponShopLayout.MapId, new Paths(WeaponShopLayout.Bounds,Array.Empty<CityBlock>(),WeaponShopLayout.Spawn,floor:WeaponShopLayout.IsWalkable,step:.1f) }, { "materia-shop", shop }
        };
        foreach (Vector2 icon in new[] { PrototypeWorldLayout.Castle, PrototypeWorldLayout.WestForest, PrototypeWorldLayout.EastForest,PrototypeWorldLayout.Ruins })
            Assert(world.Accessible(icon + Vector2.down * 0.85f), "Entrada global deve ser acessivel pela ilha.");
        Assert(world.Accessible(PrototypeWorldLayout.Trail), "Passagem oculta deve poder ser examinada.");
        Assert(!world.Accessible(PrototypeWorldLayout.Volcano), "Vulcao nao pode ser alcancado enquanto a trilha estiver bloqueada.");
        Assert(!world.Walkable(PrototypeWorldLayout.Volcano + Vector2.down * (PrototypeWorldLayout.WardRadius - 0.1f)), "Barreira precisa impedir atravessar a trilha.");
        Assert(openWorld.Accessible(PrototypeWorldLayout.Volcano), "Remover o colisor dos selos deve abrir um caminho real ate o vulcao.");
        Assert(openWorld.Accessible(PrototypeWorldLayout.Trail), "Trilha aberta deve permitir retornar ao sul da ilha.");
        Assert(PrototypeWorldLayout.Ruins.x < 0 && PrototypeWorldLayout.Ruins.y < 0
            && PrototypeWorldLayout.Volcano == -PrototypeWorldLayout.Ruins
            && PrototypeWorldLayout.WestForest.x < 0 && PrototypeWorldLayout.WestForest.y > 0,
            "Ruinas no sudoeste, vulcao exatamente oposto e pedreira no noroeste.");
        for (int i = 0; i < 32; i++)
        {
            double angle = i * Math.PI * 2 / 32;
            Vector2 edge = PrototypeWorldLayout.Volcano + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (PrototypeWorldLayout.WardRadius - 0.05f);
            Assert(!world.Walkable(edge), "Barreira circular deve bloquear por todos os lados, incluindo diagonais.");
        }
        Assert(PrototypeWorldLayout.SafeSavedPosition(new Vector2(-15, 5), true) == PrototypeWorldLayout.Spawn,
            "Save antigo fora da nova costa deve recuperar uma posicao livre, conservando a jornada.");
        Assert(PrototypeWorldLayout.SafeSavedPosition(PrototypeWorldLayout.Volcano, false) == PrototypeWorldLayout.Trail
            && openWorld.Accessible(PrototypeWorldLayout.SafeSavedPosition(PrototypeWorldLayout.Volcano, true)),
            "Save dentro da barreira fechada deve retornar ao acesso; aberta conserva a posicao.");
        Assert(volcano.Accessible(PrototypeVolcanoLayout.Dragon+Vector2.down*1.5f),"Trilha de basalto deve levar a um ponto de interacao do dragao.");
        Assert(!volcano.Walkable(new Vector2(5,-5)),"A lava deve impedir caminhar fora da trilha.");
        foreach(var block in PrototypeSecretLayout.WestMonoliths)
            Assert(west.Accessible(block.Position+Vector2.down), "Cada monolito deve ter um ponto livre de interacao.");
        Assert(west.Accessible(PrototypeSecretLayout.Inscription+Vector2.down), "Inscricao deve poder ser lida.");
        Assert(east.Accessible(PrototypeSecretLayout.ClawStone+Vector2.down), "Pedra com marcas precisa ser acessivel.");
        Assert(east.Accessible(PrototypeSecretLayout.HollowTree+Vector2.left*1.5f), "Arvore oca precisa de acesso dentro do alcance de E.");
        Assert(ruins.Accessible(PrototypeRuinsLayout.Altar),"Corredor lateral deve chegar ao altar.");
        Assert(Math.Abs(PrototypeRuinsLayout.FloorAt(PrototypeRuinsLayout.Altar.x)-PrototypeRuinsLayout.Altar.y)<.001f,
            "O caminho deve subir ate a plataforma do altar.");
        Assert(ruins.Accessible(PrototypeRuinsLayout.Exit),"Ruinas precisam permitir retornar a ilha.");
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
