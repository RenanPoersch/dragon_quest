using System;
using DragonQuest.Combat;

namespace DragonQuest.Prototype
{
    public static class BattleAnimationCatalog
    {
        public static string Family(string id)
        {
            if (id == null) return string.Empty;
            foreach (string family in new[] { "pirate", "golem", "wolf" })
                if (id == family || id.StartsWith(family + "-", StringComparison.Ordinal)) return family;
            return id == "soldier" ? "guard" : id;
        }
        public static string Idle(string id)
        {
            switch (Family(id))
            {
                case "aren": return "ArenBattle";
                case "lia": return "LiaBattle";
                case "bram": return "BramBattle";
                case "guard": return "GuardBattle";
                case "pirate": return "PirateBanditBattle";
                case "golem": return "GolemBattle";
                case "wolf": return "WolfBattle";
                case "tyrant": return "TyrantBattle";
                case "evil-spirit": return "SpiritBattle";
                default: return null;
            }
        }
        public static string Resource(string id, AbilityDefinition ability)
        {
            if (ability == null) return Idle(id);
            string family = Family(id), action = ability.SourceAbilityId;
            if (family == "tyrant")
            {
                switch (action)
                {
                    case "royal-strike": return "TyrantRoyalStrike";
                    case "royal-heavy": return "TyrantRoyalHeavy";
                    case "tyrant-protect": return "TyrantBattle";
                    case "prepare-meteor": return "TyrantMeteorCharge";
                }
                return null;
            }
            if (family == "evil-spirit" && (action == "mind-control" || action == "silence"
                || action == "drain-life" || action == "chains" || action == "chaos-magic")) return "SpiritAttacks";
            if (!ability.UsesLimit && (action == "attack" || family == "guard"
                && (action == "poison-strike" || action == "enemy-sweep"))) return Idle(id);
            return null;
        }
        public static bool Sequential(string resource) => resource == "TyrantRoyalStrike" || resource == "TyrantRoyalHeavy"
            || resource == "TyrantMeteorCharge" || resource == "TyrantFlamingMeteor";
        public static float Duration(string id, AbilityDefinition ability) => Family(id) == "tyrant"
            && ability?.SourceAbilityId == "flaming-meteor" ? CastleMeteorPresentation.Duration
            : Family(id) == "tyrant" && (ability?.SourceAbilityId == "prepare-meteor"
                || ability?.SourceAbilityId == "tyrant-protect") ? CastleMeteorPresentation.ProtectDuration : Family(id) == "evil-spirit" ? 1.4f : .8f;
        public static float ImpactDelay(string id, AbilityDefinition ability) => CastleMeteorPresentation.IsMeteor(id, ability)
            ? CastleMeteorPresentation.ImpactDelay : Duration(id, ability) * .5f;
        public static int Frame(string resource, float elapsed, float duration)
        {
            float progress = Math.Max(0, Math.Min(1, elapsed / Math.Max(.01f, duration)));
            return Sequential(resource) ? Math.Min(3, (int)(progress * 4)) : progress < .5f ? 1 : progress < .78f ? 2 : 3;
        }
    }
}
