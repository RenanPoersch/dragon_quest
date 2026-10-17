using System;

namespace DragonQuest.Combat
{
    // Shared by the animation, individual damage numbers, hit reactions and defeat timing.
    public static class UltimateTimeline
    {
        public const float CutStart = 1.05f, CutInterval = .18f;
        public const float JumpStart = 3.28f, HangStart = 3.72f, DiveStart = 4.26f, FinalImpact = 4.52f;
        public static bool IsUltimate(AbilityDefinition ability) => ability != null && ability.UsesLimit
            && (ability.SourceAbilityId == "omnislash" || ability.SourceAbilityId == "lia-salvation" || ability.SourceAbilityId == "bram-aegis");
        public static bool IsOmnislash(AbilityDefinition ability) => IsUltimate(ability) && ability.SourceAbilityId == "omnislash";
        public static float Duration(AbilityDefinition ability) => IsOmnislash(ability) ? 5.55f
            : ability?.SourceAbilityId == "lia-salvation" ? 4.1f : 3.85f;
        public static float ImpactDelay(AbilityDefinition ability) => IsOmnislash(ability) ? FinalImpact
            : ability?.SourceAbilityId == "lia-salvation" ? 2.35f : 2.25f;
        public static float HitTime(int hitIndex) => hitIndex >= HeroUltimates.Omnislash.HitCount
            ? FinalImpact : CutStart + Math.Max(0, hitIndex - 1) * CutInterval;
        public static int CutsLanded(float elapsed)
        {
            if (elapsed < CutStart) return 0;
            if (elapsed >= FinalImpact) return 13;
            return Math.Min(12, 1 + (int)((elapsed - CutStart) / CutInterval));
        }
        public static float HitAge(AbilityDefinition ability, float elapsed)
        {
            if (!IsOmnislash(ability)) return elapsed - ImpactDelay(ability);
            int landed = CutsLanded(elapsed);
            return landed == 0 ? -1 : elapsed - HitTime(landed);
        }
    }
}
