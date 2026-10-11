using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class MapDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public Rect Bounds { get; }
        public Vector2 Spawn { get; }
        public float CameraSize { get; }
        public bool IsShop { get; }
        public bool FixedCamera { get; }
        public MapDefinition(string id, string name, Rect bounds, Vector2 spawn, float cameraSize, bool isShop = false, bool fixedCamera = false)
        { Id = id; Name = name; Bounds = bounds; Spawn = spawn; CameraSize = cameraSize; IsShop = isShop; FixedCamera = fixedCamera; }
    }

    public sealed class MapRoute
    {
        public string From { get; }
        public string To { get; }
        public Vector2 Arrival { get; }
        public Rect Entrance { get; }
        public MapRoute(string from, string to, Vector2 entrance, Vector2 arrival)
        {
            From = from; To = to; Arrival = arrival;
            Entrance = new Rect(entrance.x - 0.7f, entrance.y - 0.5f, 1.4f, 1f);
        }
    }

    // Politica de viagem separada da Unity: somente conexoes declaradas podem trocar mapas.
    public sealed class PrototypeTravel
    {
        public const string World = "world", City = "city", Castle = "castle", WestForest = "west-forest", EastForest = "east-forest", Ruins = "ancient-ruins";
        private readonly Dictionary<string, MapDefinition> maps = new Dictionary<string, MapDefinition>();
        private readonly Dictionary<(string, string), MapRoute> routes = new Dictionary<(string, string), MapRoute>();
        public IReadOnlyList<MapDefinition> Maps { get; }
        public IReadOnlyList<MapRoute> Routes { get; }
        public MapDefinition Current { get; private set; }

        public PrototypeTravel()
        {
            var definitions = new[]
            {
                new MapDefinition(World, "ILHA / MAPA GLOBAL", PrototypeWorldLayout.Bounds, PrototypeWorldLayout.Spawn, 6.85f),
                new MapDefinition(City, "PERIFERIA DO CASTELO", PrototypeCityLayout.Bounds, PrototypeWorldLayout.CityEntry, 7.5f),
                new MapDefinition(Castle, "INTERIOR DO CASTELO", PrototypeWorldLayout.CastleBounds, PrototypeWorldLayout.CastleSpawn, 9, false, true),
                new MapDefinition(WestForest, "FLORESTA OESTE", PrototypeWorldLayout.ForestBounds, PrototypeWorldLayout.ForestSpawn, 8, false, true),
                new MapDefinition(EastForest, "FLORESTA LESTE", PrototypeWorldLayout.ForestBounds, PrototypeWorldLayout.ForestSpawn, 8, false, true),
                new MapDefinition(Ruins,"RUINAS ANTIGAS",PrototypeRuinsLayout.Bounds,PrototypeRuinsLayout.Spawn,8,false,true),
                new MapDefinition("item-shop", "LOJA DE ITENS", new Rect(-7, -5, 14, 10), new Vector2(0, -2.7f), 6, true, true),
                new MapDefinition("equipment-shop", "LOJA DE EQUIPAMENTOS", new Rect(-7, -5, 14, 10), new Vector2(0, -2.7f), 6, true, true),
                new MapDefinition("materia-shop", "LOJA DE MATERIAS", new Rect(-7, -5, 14, 10), new Vector2(0, -2.7f), 6, true, true)
            };
            foreach (MapDefinition map in definitions) maps.Add(map.Id, map);
            Maps = Array.AsReadOnly(definitions);
            var connections = new List<MapRoute>();
            Connect(connections, World, City, PrototypeWorldLayout.Castle + Vector2.down * 0.85f,
                PrototypeWorldLayout.CityExit, PrototypeWorldLayout.Castle + Vector2.down * 2.5f);
            Connect(connections, World, WestForest, PrototypeWorldLayout.WestForest + Vector2.down * 0.85f,
                PrototypeWorldLayout.LocalExit, PrototypeWorldLayout.WestForest + Vector2.down * 2.5f);
            Connect(connections, World, EastForest, PrototypeWorldLayout.EastForest + Vector2.down * 0.85f,
                PrototypeWorldLayout.LocalExit, PrototypeWorldLayout.EastForest + Vector2.down * 2.5f);
            Connect(connections, World, Ruins, PrototypeWorldLayout.Ruins + Vector2.down * 0.85f,
                PrototypeRuinsLayout.Exit, PrototypeWorldLayout.Ruins + Vector2.down * 2.5f);
            Connect(connections, City, Castle, PrototypeWorldLayout.CastleDoor, PrototypeWorldLayout.CastleExit, new Vector2(0, 14.5f));
            Connect(connections, City, "item-shop", new Vector2(-9, 2.1f), new Vector2(0, -4.1f), new Vector2(-9, 1));
            Connect(connections, City, "materia-shop", new Vector2(9, 2.1f), new Vector2(0, -4.1f), new Vector2(9, 1));
            Connect(connections, City, "equipment-shop", new Vector2(9, -7.4f), new Vector2(0, -4.1f), new Vector2(9, -8.5f));
            Routes = connections.AsReadOnly();
            Current = maps[World];
        }

        private void Connect(List<MapRoute> connections, string outside, string inside, Vector2 entrance, Vector2 exit, Vector2 returnPoint)
        {
            AddRoute(connections, new MapRoute(outside, inside, entrance, maps[inside].Spawn));
            AddRoute(connections, new MapRoute(inside, outside, exit, returnPoint));
        }
        private void AddRoute(List<MapRoute> connections, MapRoute route)
        {
            if (!maps[route.To].Bounds.Contains(route.Arrival)) throw new ArgumentException("Chegada fora do mapa.");
            routes.Add((route.From, route.To), route); connections.Add(route);
        }
        public bool TryTravel(string destination, out Vector2 arrival)
        {
            arrival = Current.Spawn;
            if (destination == null || !routes.TryGetValue((Current.Id, destination), out MapRoute route)) return false;
            Current = maps[destination]; arrival = route.Arrival; return true;
        }

        public MapRoute RouteFrom(string from, string destination) => routes[(from, destination)];

        // O ponto no chao do heroi precisa entrar na area; menus suspendem a troca.
        public bool TryEnter(Vector2 position, bool blocked, out Vector2 arrival)
        {
            arrival = Current.Spawn;
            if (blocked) return false;
            foreach (MapRoute route in Routes)
                if (route.From == Current.Id && route.Entrance.Contains(position))
                    return TryTravel(route.To, out arrival);
            return false;
        }
    }
}
