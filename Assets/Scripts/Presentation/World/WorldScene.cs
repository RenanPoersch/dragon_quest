using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEngine;

// Reúne as referências produzidas pelo construtor e consumidas na inicialização da cena.
namespace DragonQuest.Presentation.World
{
    public sealed class WorldScene
    {
        // Permite localizar a raiz de cada mapa pelo seu identificador.
        public Dictionary<string, GameObject> Maps { get; }
        public PlayerMovement Player { get; }
        // Entrega ao jogador os alvos que podem ser avaliados ao buscar uma interação próxima.
        public Interactable[] Interactables { get; }
        public BattleInteraction BridgeGuards { get; }
        public InnSavePoint SavePoint { get; }

        public WorldScene(Dictionary<string, GameObject> maps, PlayerMovement player,
            Interactable[] interactables, BattleInteraction bridgeGuards, InnSavePoint savePoint)
        {
            Maps = maps; Player = player; Interactables = interactables;
            BridgeGuards = bridgeGuards; SavePoint = savePoint;
        }
    }
}
