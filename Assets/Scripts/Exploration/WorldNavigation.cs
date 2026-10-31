using DragonQuest.Exploration.Layouts;
using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Progression;

namespace DragonQuest.Exploration
{
    public sealed class MapDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public Rect Bounds { get; }
        public Rect? CoveredGroundBounds { get; }
        public Vector2 Spawn { get; }
        public float CameraSize { get; }
        public bool IsShop { get; }
        public bool FixedCamera { get; }
        public MapDefinition(string id, string name, Rect bounds, Vector2 spawn, float cameraSize, bool isShop = false, bool fixedCamera = false, Rect? coveredGroundBounds = null)
        { Id = id; Name = name; Bounds = bounds; CoveredGroundBounds = coveredGroundBounds; Spawn = spawn; CameraSize = cameraSize; IsShop = isShop; FixedCamera = fixedCamera; }
    }

    public sealed class MapRoute
    {
        public string From { get; }
        public string To { get; }
        public Vector2 Arrival { get; }
        public Rect Entrance { get; }
        public MapRoute(string from, string to, Vector2 entrance, Vector2 arrival, Vector2 size)
        {
            From = from; To = to; Arrival = arrival;
            Entrance = new Rect(entrance.x-size.x/2,entrance.y-size.y/2,size.x,size.y);
        }
    }

    // Politica de viagem separada da Unity: somente conexoes declaradas podem trocar mapas.
    public sealed class WorldNavigation
    {
        public const string World = "world", City = "city", Castle = "castle", WestForest = "west-forest", EastForest = "east-forest", Ruins = "ancient-ruins", Volcano = "volcano", Inn = "inn";
        private static readonly Rect LegacyShopBounds = new Rect(-7,-5,14,10);
        private readonly AncientSecrets secrets;
        private readonly Dictionary<string, MapDefinition> maps = new Dictionary<string, MapDefinition>();
        private readonly Dictionary<(string, string), MapRoute> routes = new Dictionary<(string, string), MapRoute>();
        public IReadOnlyList<MapDefinition> Maps { get; }
        public IReadOnlyList<MapRoute> Routes { get; }
        public MapDefinition Current { get; private set; }
        public bool CanEnterVolcano => secrets?.PassageOpen == true;

        public WorldNavigation(AncientSecrets ancientSecrets = null)
        {
            secrets = ancientSecrets;
            var items=CompactShopLayout.Items; var materia=CompactShopLayout.Materia;
            var definitions = new[]
            {
                new MapDefinition(World, "ILHA / MAPA GLOBAL", WorldLayout.Bounds, WorldLayout.Spawn, 6.85f),
                new MapDefinition(City, "PERIFERIA DO CASTELO", CityLayout.Bounds, WorldLayout.CityEntry, 6,
                    coveredGroundBounds: CityLayout.TerrainBounds),
                new MapDefinition(Castle, "SALA DO TRONO", ThroneRoomLayout.Bounds, ThroneRoomLayout.Spawn, 9, false, true),
                new MapDefinition(WestForest, "PEDREIRA DOS MONOLITOS", QuarryLayout.Bounds, QuarryLayout.Spawn, 8, false, true),
                new MapDefinition(EastForest, "FLORESTA LESTE", EastForestLayout.Bounds, EastForestLayout.Spawn, 8, false, true),
                new MapDefinition(Ruins,"RUINAS ANTIGAS",RuinsLayout.Bounds,RuinsLayout.Spawn,RuinsLayout.CameraSize),
                new MapDefinition(Volcano,"COVIL DO DRAGAO",VolcanoLayout.Bounds,VolcanoLayout.Spawn,VolcanoLayout.CameraSize),
                new MapDefinition(Inn,"HOSPEDARIA",InnLayout.Bounds,InnLayout.Spawn,InnLayout.CameraSize,false,true),
                new MapDefinition(items.MapId,"LOJA DE ITENS",items.Bounds,items.Spawn,CompactShopLayout.CameraSize,true,true),
                new MapDefinition(WeaponShopLayout.MapId, "LOJA DE EQUIPAMENTOS", WeaponShopLayout.Bounds, WeaponShopLayout.Spawn, WeaponShopLayout.CameraSize, true, true),
                new MapDefinition(materia.MapId,"LOJA DE MATERIAS",materia.Bounds,materia.Spawn,CompactShopLayout.CameraSize,true,true)
            };
            foreach (MapDefinition map in definitions) maps.Add(map.Id, map);
            Maps = Array.AsReadOnly(definitions);
            var connections = new List<MapRoute>();
            Connect(connections, World, City, WorldLayout.Castle,
                WorldLayout.CityExit, WorldLayout.Castle + Vector2.down * 2.5f,
                exitSize: CityLayout.ExitSize);
            Connect(connections, World, WestForest, WorldLayout.WestForest,
                QuarryLayout.Exit, WorldLayout.WestForest + Vector2.down * 2.5f, exitSize: QuarryLayout.ExitSize);
            Connect(connections, World, EastForest, WorldLayout.EastForest,
                EastForestLayout.Exit, WorldLayout.EastForest + Vector2.down * 2.5f, exitSize: EastForestLayout.ExitSize);
            Connect(connections, World, Ruins, WorldLayout.Ruins,
                RuinsLayout.Exit, WorldLayout.RuinsReturn);
            Connect(connections, City, Castle, WorldLayout.CastleDoor, ThroneRoomLayout.Exit, CityLayout.CastleReturn);
            Connect(connections, City, items.MapId, CityLayout.ItemDoor, items.Exit, CityLayout.DoorReturn(CityLayout.ItemDoor), entranceSize: CityLayout.DoorSize);
            Connect(connections, City, materia.MapId, CityLayout.MateriaDoor, materia.Exit, CityLayout.DoorReturn(CityLayout.MateriaDoor), entranceSize: CityLayout.DoorSize);
            Connect(connections, City, WeaponShopLayout.MapId, CityLayout.EquipmentDoor, WeaponShopLayout.Exit, CityLayout.DoorReturn(CityLayout.EquipmentDoor), entranceSize: CityLayout.DoorSize);
            Connect(connections, City, Inn, CityLayout.InnDoor, InnLayout.Exit, CityLayout.DoorReturn(CityLayout.InnDoor), entranceSize: CityLayout.DoorSize);
            Connect(connections, World, Volcano, WorldLayout.Volcano,
                VolcanoLayout.Exit, WorldLayout.Volcano+Vector2.down*1.8f);
            Routes = connections.AsReadOnly();
            Current = maps[World];
        }

        private void Connect(List<MapRoute> connections, string outside, string inside, Vector2 entrance, Vector2 exit, Vector2 returnPoint,
            Vector2? entranceSize = null, Vector2? exitSize = null)
        {
            AddRoute(connections, new MapRoute(outside, inside, entrance, maps[inside].Spawn,
                entranceSize ?? (outside==World?new Vector2(2.8f,2.2f):new Vector2(1.4f,1))));
            AddRoute(connections, new MapRoute(inside, outside, exit, returnPoint,exitSize ?? new Vector2(1.4f,1)));
        }
        private void AddRoute(List<MapRoute> connections, MapRoute route)
        {
            if (!maps[route.To].Bounds.Contains(route.Arrival)) throw new ArgumentException("Chegada fora do mapa.");
            routes.Add((route.From, route.To), route); connections.Add(route);
        }
        public bool TryTravel(string destination, out Vector2 arrival)
        {
            arrival = Current.Spawn;
            if (destination == Volcano && !CanEnterVolcano) return false;
            if (destination == null || !routes.TryGetValue((Current.Id, destination), out MapRoute route)) return false;
            Current = maps[destination]; arrival = route.Arrival; return true;
        }

        public MapRoute RouteFrom(string from, string destination) => routes[(from, destination)];

        // Carregamento nao depende de uma rota: o mapa/posicao precisam existir e respeitar os selos.
        public bool TryRestore(string mapId, Vector2 position)
        {
            if (mapId == null || !maps.TryGetValue(mapId, out var map) || float.IsNaN(position.x) || float.IsNaN(position.y)
                || float.IsInfinity(position.x) || float.IsInfinity(position.y)
                || !map.Bounds.Contains(position) && (mapId != City || !CityLayout.LegacyBounds.Contains(position))
                && (!map.IsShop || !LegacyShopBounds.Contains(position))
                && (mapId != Inn || !InnLayout.LegacyBounds.Contains(position))
                && (mapId != WeaponShopLayout.MapId || !WeaponShopLayout.LegacyBounds.Contains(position))
                && (mapId != Ruins || !RuinsLayout.LegacyBounds.Contains(position))
                && (mapId != Volcano || !VolcanoLayout.LegacyBounds.Contains(position))
                || mapId == Volcano && !CanEnterVolcano) return false;
            Current = map; return true;
        }

        public bool TryRecoverAtInn(out Vector2 arrival)
        {
            arrival = Current.Spawn;
            if (Current.Id != Volcano && Current.Id != Castle) return false;
            Current = maps[Inn]; arrival = Current.Spawn; return true;
        }

        // O ponto no chao do heroi precisa entrar na area; menus suspendem a troca.
        public bool TryEnter(Vector2 position, bool blocked, out Vector2 arrival)
        {
            arrival = Current.Spawn;
            if (blocked) return false;
            foreach (MapRoute route in Routes)
                if (route.From == Current.Id && (route.From == EastForest && route.To == World
                    ? EastForestLayout.IsExit(position) : route.From == WestForest && route.To == World
                    ? QuarryLayout.IsExit(position) : route.Entrance.Contains(position)))
                    return TryTravel(route.To, out arrival);
            return false;
        }
    }
}
