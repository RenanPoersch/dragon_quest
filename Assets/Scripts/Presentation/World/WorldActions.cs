using DragonQuest.Progression;
using System;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEngine;

namespace DragonQuest.Presentation.World
{
    // Resolve the current journey when an interaction occurs, including after loading a save.
    public sealed class WorldActions
    {
        public Func<PartyProgress> GetParty { get; }
        public Func<ExplorationProgress> GetProgress { get; }
        public Action StartCastleBattle { get; }
        public Action StartDragonBattle { get; }
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
