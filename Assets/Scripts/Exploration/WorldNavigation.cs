using DragonQuest.Exploration.Layouts;
using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Progression;

// Define os mapas disponíveis e controla a navegação entre eles com acesso aos segredos da jornada.
namespace DragonQuest.Exploration
{
    // Configuração de um mapa. A navegação usa estes dados para validar posições e preparar a chegada.
    public sealed class MapDefinition
    {
        // Identificador usado para buscar o mapa e relacioná-lo às rotas.
        public string Id { get; }
        // Nome mostrado ao jogador ao identificar o local atual.
        public string Name { get; }
        // Delimita a área do mundo usada para enquadrar este mapa.
        public Rect Bounds { get; }
        // Representa um limite visual adicional quando o mapa precisa restringir o enquadramento ao piso coberto.
        public Rect? CoveredGroundBounds { get; }
        // Posição padrão em que o jogador aparece ao entrar neste mapa.
        public Vector2 Spawn { get; }
        // Tamanho de enquadramento solicitado pelo mapa à câmera.
        public float CameraSize { get; }
        // Indica se o mapa representa uma loja.
        public bool IsShop { get; }
        // Indica se o mapa usa enquadramento fixo em vez de acompanhar o jogador.
        public bool FixedCamera { get; }
        // Guarda a configuração recebida. Por padrão, o mapa não é loja, usa câmera móvel e não delimita um piso adicional.
        public MapDefinition(string id, string name, Rect bounds, Vector2 spawn, float cameraSize, bool isShop = false, bool fixedCamera = false, Rect? coveredGroundBounds = null)
        { Id = id; Name = name; Bounds = bounds; CoveredGroundBounds = coveredGroundBounds; Spawn = spawn; CameraSize = cameraSize; IsShop = isShop; FixedCamera = fixedCamera; }
    }

    // Representa uma viagem em um só sentido. A volta é registrada em outra rota.
    public sealed class MapRoute
    {
        // ID do mapa onde o jogador precisa estar para usar a rota.
        public string From { get; }
        // ID do mapa que será ativado ao completar a viagem.
        public string To { get; }
        // Posição em que o jogador aparece no mapa de destino.
        public Vector2 Arrival { get; }
        // Área no mapa de origem usada para detectar a passagem do jogador.
        public Rect Entrance { get; }
        // Recebe o centro e as dimensões da entrada, além dos dois mapas e da posição de chegada.
        public MapRoute(string from, string to, Vector2 entrance, Vector2 arrival, Vector2 size)
        {
            From = from; To = to; Arrival = arrival;
            // O Rect recebe o canto inferior esquerdo. Subtrair metade da largura e da altura mantém a área centrada na entrada.
            Entrance = new Rect(entrance.x-size.x/2,entrance.y-size.y/2,size.x,size.y);
        }
    }

    // Politica de viagem separada da Unity: somente conexoes declaradas podem trocar mapas.
    public sealed class WorldNavigation
    {
        // IDs estáveis dos mapas principais. São usados nas definições, nas rotas e no salvamento.
        public const string World = "world", City = "city", Castle = "castle", WestForest = "west-forest", EastForest = "east-forest", Ruins = "ancient-ruins", Volcano = "volcano", Inn = "inn";
        // Limites antigos das lojas aceitos ao carregar posições gravadas em versões anteriores.
        private static readonly Rect LegacyShopBounds = new Rect(-7,-5,14,10);
        // Aponta para o estado dos segredos recebido no construtor. Na cena, o bootstrap compartilha esse objeto.
        private readonly AncientSecrets secrets;
        // Índice interno dos mapas por ID para consultas durante viagens e carregamentos.
        private readonly Dictionary<string, MapDefinition> maps = new Dictionary<string, MapDefinition>();
        // Índice interno de rotas pelo par (origem, destino). O sentido faz parte da chave.
        private readonly Dictionary<(string, string), MapRoute> routes = new Dictionary<(string, string), MapRoute>();
        // Lista de mapas que outros sistemas podem percorrer para montar a cena.
        public IReadOnlyList<MapDefinition> Maps { get; }
        // Lista de rotas usada para verificar as áreas de passagem durante a exploração.
        public IReadOnlyList<MapRoute> Routes { get; }
        // Mapa ativo. A navegação atualiza essa referência em viagens e ao restaurar a partida.
        public MapDefinition Current { get; private set; }
        // Libera a entrada no vulcão quando existe um estado de segredos com a passagem aberta.
        public bool CanEnterVolcano => secrets?.PassageOpen == true;

        public WorldNavigation(AncientSecrets ancientSecrets = null)
        {
            // Guarda a referência recebida para consultar a passagem sempre que for necessário.
            secrets = ancientSecrets;
            // Carrega os layouts das lojas compactas de itens e matérias, cada uma com entrada e saída próprias.
            var items=CompactShopLayout.Items; var materia=CompactShopLayout.Materia;
            // Reúne as definições que identificam os mapas e seus pontos de entrada.
            var definitions = new[]
            {
                // Os argumentos opcionais usam os padrões do construtor: câmera móvel, mapa comum e nenhum limite extra de piso.
                new MapDefinition(
                    World,                  // ID do mapa global.
                    "ILHA / MAPA GLOBAL",    // Nome exibido ao jogador.
                    WorldLayout.Bounds,     // Limites do mapa definidos no layout.
                    WorldLayout.Spawn,      // Posição inicial do jogador na ilha.
                    6.85f),                 // Tamanho de enquadramento solicitado à câmera.
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
            // Indexa cada definição pelo ID e disponibiliza a lista de mapas somente para leitura.
            foreach (MapDefinition map in definitions) maps.Add(map.Id, map);
            Maps = Array.AsReadOnly(definitions);
            // Acumula as rotas de ida e volta na ordem em que são declaradas.
            var connections = new List<MapRoute>();
            // A ida termina no Spawn da cidade. A área de entrada na ilha usa o tamanho padrão de Connect.
            Connect(
                connections,                                    // Lista que recebe as duas rotas.
                World,                                          // Mapa de fora, a ilha.
                City,                                           // Mapa de dentro, a periferia do castelo.
                WorldLayout.Castle,                              // Centro da entrada da cidade, na ilha.
                WorldLayout.CityExit,                            // Centro da saída, dentro da cidade.
                WorldLayout.Castle + Vector2.down * 2.5f,        // Posição na ilha ao sair da cidade, 2,5 unidades abaixo da entrada.
                exitSize: CityLayout.ExitSize);                  // Largura e altura da área de saída dentro da cidade.
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
            // Expõe as rotas por uma lista somente para leitura após concluir a montagem.
            Routes = connections.AsReadOnly();
            // A navegação começa no mapa global antes que uma viagem ou um save mude o local.
            Current = maps[World];
        }

        // Cria duas rotas. A ida chega no Spawn do destino e a volta chega no ponto de retorno informado.
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
