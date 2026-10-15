using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
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
    public sealed class PrototypeTravel
    {
        public const string World = "world", City = "city", Castle = "castle", WestForest = "west-forest", EastForest = "east-forest", Ruins = "ancient-ruins", Volcano = "volcano", Inn = "inn";
        private readonly AncientSecrets secrets;
        private readonly Dictionary<string, MapDefinition> maps = new Dictionary<string, MapDefinition>();
        private readonly Dictionary<(string, string), MapRoute> routes = new Dictionary<(string, string), MapRoute>();
        public IReadOnlyList<MapDefinition> Maps { get; }
        public IReadOnlyList<MapRoute> Routes { get; }
        public MapDefinition Current { get; private set; }
        public bool CanEnterVolcano => secrets?.PassageOpen == true;

        public PrototypeTravel(AncientSecrets ancientSecrets = null)
        {
            secrets = ancientSecrets;
            var definitions = new[]
            {
                new MapDefinition(World, "ILHA / MAPA GLOBAL", PrototypeWorldLayout.Bounds, PrototypeWorldLayout.Spawn, 6.85f),
                new MapDefinition(City, "PERIFERIA DO CASTELO", PrototypeCityLayout.Bounds, PrototypeWorldLayout.CityEntry, 6,
                    coveredGroundBounds: PrototypeCityLayout.TerrainBounds),
                new MapDefinition(Castle, "INTERIOR DO CASTELO", PrototypeWorldLayout.CastleBounds, PrototypeWorldLayout.CastleSpawn, 9, false, true),
                new MapDefinition(WestForest, "PEDREIRA DOS MONOLITOS", QuarryLayout.Bounds, QuarryLayout.Spawn, 8, false, true),
                new MapDefinition(EastForest, "FLORESTA LESTE", EastForestLayout.Bounds, EastForestLayout.Spawn, 8, false, true),
                new MapDefinition(Ruins,"RUINAS ANTIGAS",PrototypeRuinsLayout.Bounds,PrototypeRuinsLayout.Spawn,PrototypeRuinsLayout.CameraSize),
                new MapDefinition(Volcano,"COVIL DO DRAGAO",PrototypeVolcanoLayout.Bounds,PrototypeVolcanoLayout.Spawn,PrototypeVolcanoLayout.CameraSize),
                new MapDefinition(Inn,"HOSPEDARIA",PrototypeInnLayout.Bounds,PrototypeInnLayout.Spawn,PrototypeInnLayout.CameraSize,false,true),
                new MapDefinition("item-shop", "LOJA DE ITENS", PrototypeShopLayout.Bounds, PrototypeShopLayout.Spawn, PrototypeShopLayout.CameraSize, true, true),
                new MapDefinition("equipment-shop", "LOJA DE EQUIPAMENTOS", PrototypeShopLayout.Bounds, PrototypeShopLayout.Spawn, PrototypeShopLayout.CameraSize, true, true),
                new MapDefinition("materia-shop", "LOJA DE MATERIAS", PrototypeShopLayout.Bounds, PrototypeShopLayout.Spawn, PrototypeShopLayout.CameraSize, true, true)
            };
            foreach (MapDefinition map in definitions) maps.Add(map.Id, map);
            Maps = Array.AsReadOnly(definitions);
            var connections = new List<MapRoute>();
            Connect(connections, World, City, PrototypeWorldLayout.Castle,
                PrototypeWorldLayout.CityExit, PrototypeWorldLayout.Castle + Vector2.down * 2.5f,
                exitSize: PrototypeCityLayout.ExitSize);
            Connect(connections, World, WestForest, PrototypeWorldLayout.WestForest,
                QuarryLayout.Exit, PrototypeWorldLayout.WestForest + Vector2.down * 2.5f, exitSize: QuarryLayout.ExitSize);
            Connect(connections, World, EastForest, PrototypeWorldLayout.EastForest,
                EastForestLayout.Exit, PrototypeWorldLayout.EastForest + Vector2.down * 2.5f, exitSize: EastForestLayout.ExitSize);
            Connect(connections, World, Ruins, PrototypeWorldLayout.Ruins,
                PrototypeRuinsLayout.Exit, PrototypeWorldLayout.RuinsReturn);
            Connect(connections, City, Castle, PrototypeWorldLayout.CastleDoor, PrototypeWorldLayout.CastleExit, PrototypeCityLayout.CastleReturn);
            Connect(connections, City, "item-shop", PrototypeCityLayout.ItemDoor, PrototypeShopLayout.Exit, PrototypeCityLayout.DoorReturn(PrototypeCityLayout.ItemDoor), entranceSize: PrototypeCityLayout.DoorSize);
            Connect(connections, City, "materia-shop", PrototypeCityLayout.MateriaDoor, PrototypeShopLayout.Exit, PrototypeCityLayout.DoorReturn(PrototypeCityLayout.MateriaDoor), entranceSize: PrototypeCityLayout.DoorSize);
            Connect(connections, City, "equipment-shop", PrototypeCityLayout.EquipmentDoor, PrototypeShopLayout.Exit, PrototypeCityLayout.DoorReturn(PrototypeCityLayout.EquipmentDoor), entranceSize: PrototypeCityLayout.DoorSize);
            Connect(connections, City, Inn, PrototypeCityLayout.InnDoor, PrototypeInnLayout.Exit, PrototypeCityLayout.DoorReturn(PrototypeCityLayout.InnDoor), entranceSize: PrototypeCityLayout.DoorSize);
            Connect(connections, World, Volcano, PrototypeWorldLayout.Volcano,
                PrototypeVolcanoLayout.Exit, PrototypeWorldLayout.Volcano+Vector2.down*1.8f);
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
                || !map.Bounds.Contains(position) && (mapId != City || !PrototypeCityLayout.LegacyBounds.Contains(position))
                && (!map.IsShop || !PrototypeShopLayout.LegacyBounds.Contains(position))
                && (mapId != Ruins || !PrototypeRuinsLayout.LegacyBounds.Contains(position))
                && (mapId != Volcano || !PrototypeVolcanoLayout.LegacyBounds.Contains(position))
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
