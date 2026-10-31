using System;
using DragonQuest.Combat;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Gameplay.Exploration
{
    public enum ExplorationStep { None, Defeat, Encounter }
    public sealed class ExplorationSession
    {
        private readonly PartyProgress party;
        private readonly RandomEncounterRules encounters;
        private Vector2 lastPosition;
        private int lastTeleportRevision;
        private bool trackingWalk;
        public bool IsEnabled => encounters.IsEnabled;
        public ExplorationSession(PartyProgress party, string location, Func<double> roll)
        { this.party = party; encounters = new RandomEncounterRules(roll); Reset(location); encounters.SetEnabled(true, location); }
        public void SetEnabled(bool enabled, string location) => encounters.SetEnabled(enabled, location);
        public void ResetWalk() => trackingWalk = false;
        public void Reset(string location) { encounters.Reset(location); ResetWalk(); }
        public ExplorationStep Advance(string location, Vector2 position, int teleportRevision, bool moving, float delta, bool blocked)
        {
            party.AdvancePoison(delta, blocked);
            if (!blocked && !party.HasLivingMembers) return ExplorationStep.Defeat;
            bool teleported = !trackingWalk || lastTeleportRevision != teleportRevision;
            float distance = trackingWalk ? Vector2.Distance(position, lastPosition) : 0;
            lastPosition = position; lastTeleportRevision = teleportRevision; trackingWalk = true;
            return encounters.Advance(location, distance, blocked || !moving, teleported) ? ExplorationStep.Encounter : ExplorationStep.None;
        }
    }
}
