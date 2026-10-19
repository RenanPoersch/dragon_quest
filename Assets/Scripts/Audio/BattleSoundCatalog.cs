using System;
using System.Collections.Generic;
using DragonQuest.Combat;

namespace DragonQuest.Audio
{
    public readonly struct BattleSoundCue
    {
        public string Key { get; }
        public float ClipImpact { get; }
        public bool StartsWithAction { get; }
        public bool IsValid => Key != null;

        public BattleSoundCue(string key, float clipImpact = 0, bool startsWithAction = false)
        { Key = key; ClipImpact = clipImpact; StartsWithAction = startsWithAction; }

        // Short recordings are placed around the animation's hit. The authored
        // multi-stage limits and meteors keep their own timeline from cast start.
        public float StartOffset(float actionImpactDelay)
            => StartsWithAction ? 0 : Math.Max(0, actionImpactDelay - ClipImpact);
    }

    public static class BattleSoundCatalog
    {
        // Contact markers belong to the approved V3/V4 PCM masters.
        private static readonly Dictionary<string, float> impacts = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            { "attack-aren", 0.11f },
            { "attack-lia", 0.08f },
            { "attack-bram", 0.15f },
            { "heavy-strike", 0.23f },
            { "cleave", 0.24f },
            { "defend", 0.091f },
            { "protect", 0.206f },
            { "confused-attack", 0.265f },
            { "fire-all", 0.879f },
            { "ice-all", 0.419f },
            { "thunder-all", 0.305f },
            { "earth-all", 0.567f },
            { "chaos-all", 0.612f },
            { "heal-all", 0.395f },
            { "cleanse-all", 0.305f },
            { "revive-all", 1.132f },
            { "infuse-fire", 0.144f },
            { "infuse-ice", 0.34f },
            { "infuse-thunder", 0.144f },
            { "infuse-earth", 0.144f },
            { "infuse-chaos", 0.144f },
            { "phoenix", 0.34f },
            { "bahamut", 1.05f },
            { "titan", 1.38f },
            { "attack-guard", 0.115f },
            { "attack-pirate", 0.114f },
            { "attack-wolf", 0.017f },
            { "attack-golem", 0.186f },
            { "poison-strike", 0.114f },
            { "enemy-sweep", 0.25f },
            { "dragon-claw", 0.188f },
            { "royal-strike", 0.199f },
            { "royal-heavy", 0.32f },
            { "tyrant-protect", 0.108f },
            { "drain-life", 0.384f },
            { "mind-control", 1.348f },
            { "silence", 0.177f },
            { "chains", 1.12f },
            { "chaos-magic", 1.07f }
        };
        private static readonly HashSet<string> fromStart = new HashSet<string>(StringComparer.Ordinal)
        { "omnislash", "lia-salvation", "bram-aegis", "charge-breath", "dragon-breath",
            "flaming-meteor", "meteor-absorbed", "taunt" };
        // Both target modes use the same approved All recording and level.
        private static readonly HashSet<string> sharedSpells = new HashSet<string>(StringComparer.Ordinal)
        { "fire", "ice", "thunder", "earth", "chaos", "heal", "cleanse", "revive" };
        public static IReadOnlyList<string> ResourceKeys { get; } = BuildKeys();

        private static IReadOnlyList<string> BuildKeys()
        {
            var keys = new List<string>(impacts.Keys);
            keys.AddRange(fromStart); keys.Sort(StringComparer.Ordinal);
            return keys.AsReadOnly();
        }

        public static BattleSoundCue Resolve(string actorId, AbilityDefinition ability, bool meteorAbsorbed = false)
        {
            if (ability == null || ability.SourceAbilityId == "prepare-meteor") return default;
            string key = ability.SourceAbilityId;
            if (ability.Id == "confused-attack") key = "confused-attack";
            else if (key == "attack")
            {
                switch (ability.Element)
                {
                    case DamageElement.Fire: key = "infuse-fire"; break;
                    case DamageElement.Ice: key = "infuse-ice"; break;
                    case DamageElement.Lightning: key = "infuse-thunder"; break;
                    case DamageElement.Earth: key = "infuse-earth"; break;
                    case DamageElement.Chaos: key = "infuse-chaos"; break;
                    default:
                        string family = actorId ?? "aren";
                        foreach (string enemy in new[] { "pirate", "golem", "wolf" })
                            if (family.StartsWith(enemy + "-", StringComparison.Ordinal)) { family = enemy; break; }
                        if (family == "soldier") family = "guard";
                        key = "attack-" + family;
                        if (!impacts.ContainsKey(key)) key = "attack-aren";
                        break;
                }
            }
            else if (key == "summon") key = "phoenix";
            else if (key == "flaming-meteor" && meteorAbsorbed) key = "meteor-absorbed";
            else if (sharedSpells.Contains(key)) key += "-all";

            if (fromStart.Contains(key)) return new BattleSoundCue(key, startsWithAction: true);
            return impacts.TryGetValue(key, out float impact) ? new BattleSoundCue(key, impact) : default;
        }
    }
}
