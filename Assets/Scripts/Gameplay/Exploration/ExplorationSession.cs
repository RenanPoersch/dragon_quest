using System;
using DragonQuest.Combat;
using DragonQuest.Progression;
using UnityEngine;

// Acompanha o deslocamento na exploração para alimentar a regra de encontros e detectar a derrota do grupo.
namespace DragonQuest.Gameplay.Exploration
{
    public enum ExplorationStep { None, Defeat, Encounter }
    public sealed class ExplorationSession
    {
        private readonly PartyProgress party;
        private readonly RandomEncounterRules encounters;
        private Vector2 lastPosition;
        // Distingue um teleporte de um deslocamento que pode contar para o próximo encontro.
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
            // Mede a distância entre duas posições quando já existe um ponto anterior de referência.
            float distance = trackingWalk ? Vector2.Distance(position, lastPosition) : 0;
            lastPosition = position; lastTeleportRevision = teleportRevision; trackingWalk = true;
            return encounters.Advance(location, distance, blocked || !moving, teleported) ? ExplorationStep.Encounter : ExplorationStep.None;
        }
    }
}
