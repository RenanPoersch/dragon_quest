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
        private static readonly Dictionary<string, float> impacts = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            { "attack-aren", .102f }, { "attack-lia", .04f }, { "attack-bram", .16f },
            { "heavy-strike", .18f }, { "cleave", .16f }, { "defend", .055f },
            { "taunt", .21f }, { "protect", .19f }, { "confused-attack", .22f },
            { "fire", .025f }, { "ice", .06f }, { "thunder", 0 }, { "earth", .035f },
            { "chaos", .21f }, { "heal", 0 }, { "cleanse", 0 }, { "revive", .55f },
            { "fire-all", .025f }, { "ice-all", .06f }, { "thunder-all", 0 }, { "earth-all", .035f },
            { "chaos-all", .21f }, { "heal-all", 0 }, { "cleanse-all", 0 }, { "revive-all", .55f },
            { "infuse-fire", .09f }, { "infuse-ice", .09f }, { "infuse-thunder", .09f },
            { "infuse-earth", .09f }, { "infuse-chaos", .09f },
            { "phoenix", .82f }, { "bahamut", 1.1f }, { "titan", 1.34f },
            { "attack-guard", .14f }, { "attack-pirate", .11f }, { "attack-wolf", .13f },
            { "attack-golem", .16f }, { "poison-strike", .13f }, { "enemy-sweep", .18f },
            { "dragon-claw", .155f }, { "royal-strike", .17f }, { "royal-heavy", .31f },
            { "tyrant-protect", .2f }, { "drain-life", 1.15f }, { "mind-control", 1.35f },
            { "silence", .38f }, { "chains", .78f }, { "chaos-magic", 1.9f }
        };
        private static readonly HashSet<string> fromStart = new HashSet<string>(StringComparer.Ordinal)
        { "omnislash", "lia-salvation", "bram-aegis", "charge-breath", "dragon-breath",
            "prepare-meteor", "flaming-meteor", "meteor-absorbed" };
        public static IReadOnlyList<string> ResourceKeys { get; } = BuildKeys();

        private static IReadOnlyList<string> BuildKeys()
        {
            var keys = new List<string>(impacts.Keys);
            keys.AddRange(fromStart); keys.Sort(StringComparer.Ordinal);
            return keys.AsReadOnly();
        }

        public static BattleSoundCue Resolve(string actorId, AbilityDefinition ability, bool meteorAbsorbed = false)
        {
            if (ability == null) return default;
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
            else if ((ability.Targets == TargetRule.AllEnemies || ability.Targets == TargetRule.AllAllies)
                && impacts.ContainsKey(key + "-all")) key += "-all";

            if (fromStart.Contains(key)) return new BattleSoundCue(key, startsWithAction: true);
            return impacts.TryGetValue(key, out float impact) ? new BattleSoundCue(key, impact) : default;
        }
    }
}
