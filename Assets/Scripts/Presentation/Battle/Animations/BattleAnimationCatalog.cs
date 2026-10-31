using DragonQuest.Presentation.Battle;
using System;
using DragonQuest.Combat;

namespace DragonQuest.Presentation.Battle.Animations
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
            if (family == "evil-spirit") return SpiritAttackTimeline.Resource(ability);
            if (!ability.UsesLimit && (action == "attack" || family == "guard"
                && (action == "poison-strike" || action == "enemy-sweep"))) return Idle(id);
            return null;
        }
        public static bool Sequential(string resource) => resource == "TyrantRoyalStrike" || resource == "TyrantRoyalHeavy"
            || resource == "TyrantMeteorCharge" || resource == "TyrantFlamingMeteor";
        public static float Duration(string id, AbilityDefinition ability) => Family(id) == "tyrant"
            && ability?.SourceAbilityId == "flaming-meteor" ? CastleMeteorPresentation.Duration
            : Family(id) == "tyrant" && (ability?.SourceAbilityId == "prepare-meteor"
                || ability?.SourceAbilityId == "tyrant-protect") ? CastleMeteorPresentation.ProtectDuration : Family(id) == "evil-spirit" ? SpiritAttackTimeline.Duration(ability) : .8f;
        public static float ImpactDelay(string id, AbilityDefinition ability) => CastleMeteorPresentation.IsMeteor(id, ability)
            ? CastleMeteorPresentation.ImpactDelay : Family(id) == "evil-spirit" ? SpiritAttackTimeline.Impact(ability) : Duration(id, ability) * .5f;
        public static int Frame(string resource, float elapsed, float duration)
        {
            float progress = Math.Max(0, Math.Min(1, elapsed / Math.Max(.01f, duration)));
            return Sequential(resource) ? Math.Min(3, (int)(progress * 4)) : progress < .5f ? 1 : progress < .78f ? 2 : 3;
        }
    }
    // Presentation timing is shared by the renderer, damage numbers and defeat transitions.
    public static class SpiritAttackTimeline
    {
        public static readonly string[] Resources = { "SpiritBoss/SpiritMindControl", "SpiritBoss/SpiritSilence",
            "SpiritBoss/SpiritLifeDrain", "SpiritBoss/SpiritChains", "SpiritBoss/SpiritChaos" };
        public static int Skill(AbilityDefinition ability)
        {
            switch (ability?.SourceAbilityId)
            {
                case "mind-control": return 0;
                case "silence": return 1;
                case "drain-life": return 2;
                case "chains": return 3;
                case "chaos-magic": return 4;
                default: return -1;
            }
        }
        public static string Resource(AbilityDefinition ability) => Skill(ability) < 0 ? null : Resources[Skill(ability)];
        public static float Duration(AbilityDefinition ability)
        {
            switch (Skill(ability)) { case 0: return 3.1f; case 1: return 2.7f; case 2: return 3.6f;
                case 3: return 2.9f; case 4: return 4.1f; default: return .8f; }
        }
        public static float Impact(AbilityDefinition ability)
        {
            switch (Skill(ability)) { case 0: return 1.75f; case 1: return 1.35f; case 2: return 2.45f;
                case 3: return 1.5f; case 4: return 2.55f; default: return .4f; }
        }
        public static float EffectStart(AbilityDefinition ability) => Skill(ability) == 4 ? .65f : .4f;
        public static int ActorFrame(AbilityDefinition ability, float time) => time < Impact(ability) * .32f ? 0
            : time < Impact(ability) * .76f ? 1 : time < Impact(ability) + .3f ? 2 : 3;
        public static int EffectFrame(AbilityDefinition ability, float time)
        {
            int impactFrame = Skill(ability) == 4 || Skill(ability) == 1 ? 4 : 3;
            float impact = Impact(ability), start = EffectStart(ability);
            if (time < impact)
                return Math.Max(0, Math.Min(impactFrame - 1, (int)((time - start) / (impact - start) * impactFrame)));
            return Math.Min(7, impactFrame + (int)((time - impact) / (Duration(ability) - impact - .2f) * (8 - impactFrame)));
        }
        public static float Opacity(AbilityDefinition ability, float time) => Math.Max(0, Math.Min(1,
            Math.Min((time - EffectStart(ability)) / .18f, (Duration(ability) - time) / .3f)));
        public static float Shake(AbilityDefinition ability, float time)
        {
            float age = time - Impact(ability);
            if (age < 0 || age > .42f) return 0;
            float force = Skill(ability) == 4 ? 15 : Skill(ability) == 3 ? 10 : Skill(ability) == 1 ? 6 : 3;
            return force * (1 - age / .42f);
        }
    }
}
