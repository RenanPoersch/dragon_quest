using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEngine;

namespace DragonQuest.Presentation.World
{
    public sealed class WorldScene
    {
        public Dictionary<string, GameObject> Maps { get; }
        public PlayerMovement Player { get; }
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
