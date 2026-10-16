using System;

namespace DragonQuest.Combat
{
    public enum RandomEncounterType { None, Pirate, Golem, Wolf }

    // Distance-based scheduling: pauses do not roll dice and teleports reset the meter.
    public sealed class RandomEncounterRules
    {
        private readonly Func<double> random;
        private string location;
        private float remaining;
        public bool IsEnabled { get; private set; } = true;
        public RandomEncounterRules(Func<double> random) { this.random = random ?? throw new ArgumentNullException(nameof(random)); }
        public void SetEnabled(bool enabled, string map)
        {
            if (IsEnabled == enabled) return;
            IsEnabled = enabled;
            Reset(map);
        }
        public static RandomEncounterType TypeAt(string map) => map == "world" ? RandomEncounterType.Pirate
            : map == "west-forest" ? RandomEncounterType.Golem : map == "east-forest" ? RandomEncounterType.Wolf : RandomEncounterType.None;
        public static string Title(RandomEncounterType type) => type == RandomEncounterType.Pirate ? "BANDIDOS PIRATAS"
            : type == RandomEncounterType.Golem ? "GOLENS DA PEDREIRA" : type == RandomEncounterType.Wolf ? "LOBOS DA FLORESTA" : "PATRULHA DO TIRANO";
        public void Reset(string map)
        {
            location = map;
            remaining = 0;
            if (!IsEnabled) return;
            // Minimum walking distance also protects arrivals and the return from combat.
            remaining = map == "world" ? 18f + Sample() * 12f : 9f + Sample() * 7f;
        }
        private float Sample() => (float)Math.Max(0, Math.Min(1, random()));
        public bool Advance(string map, float distance, bool blocked, bool teleported)
        {
            if (!IsEnabled) return false;
            if (location != map || teleported) { Reset(map); return false; }
            if (blocked || TypeAt(map) == RandomEncounterType.None || float.IsNaN(distance)
                || float.IsInfinity(distance) || distance <= 0 || distance > 2f) return false;
            remaining -= distance;
            if (remaining > 0) return false;
            Reset(map);
            return true;
        }
    }
}
