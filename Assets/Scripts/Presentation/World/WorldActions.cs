using DragonQuest.Progression;
using System;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEngine;

// Entrega ao construtor do mundo callbacks para acionar sistemas que o bootstrap conecta separadamente.
namespace DragonQuest.Presentation.World
{
    // Resolve the current journey when an interaction occurs, including after loading a save.
    public sealed class WorldActions
    {
        // Consulta o grupo atual no momento da interação, inclusive depois de restaurar uma partida.
        public Func<PartyProgress> GetParty { get; }
        public Func<ExplorationProgress> GetProgress { get; }
        public Action StartCastleBattle { get; }
        public Action StartDragonBattle { get; }
        // Aciona a audiência com a referência do rei e o controlador que conduz a conversa.
        public Action<Transform, DialogueController> BeginAudience { get; }
        public Action OpenInnSaveMenu { get; }
        public Action RestAtInn { get; }

        public WorldActions(Func<PartyProgress> party, Func<ExplorationProgress> progress,
            Action castleBattle, Action dragonBattle, Action<Transform, DialogueController> audience,
            Action saveMenu, Action rest)
        {
            GetParty = party; GetProgress = progress;
            StartCastleBattle = castleBattle; StartDragonBattle = dragonBattle;
            BeginAudience = audience; OpenInnSaveMenu = saveMenu; RestAtInn = rest;
        }
    }
}
