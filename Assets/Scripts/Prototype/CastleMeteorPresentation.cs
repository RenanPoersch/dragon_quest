using System;
using DragonQuest.Combat;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public static class CastleMeteorPresentation
    {
        public const float Duration = 3.6f, ImpactDelay = 2.1f, AbsorptionStart = 1.4f;
        public const float ProtectDuration = 1.2f, ShieldHitDuration = .55f;
        public static bool IsMeteor(string actorId, AbilityDefinition ability)
            => actorId == CastleBattleRules.TyrantId && ability?.SourceAbilityId == "flaming-meteor";
        public static int ChargeLevel(CastleBattleRules rules)
            => rules == null || !rules.MeteorStarted ? 0
                : Math.Max(1, Math.Min(CastleBattleRules.MeteorCastTurns,
                    CastleBattleRules.MeteorCastTurns - rules.PreparationsRemaining));
        public static float ChargeHeight(int level) => 55 + 25 * Math.Max(1, Math.Min(5, level));
        public static int ChargePose(CastleBattleRules rules) => Math.Max(0, Math.Min(3, ChargeLevel(rules) - 1));
        public static int EffectFrame(float elapsed, float duration)
            => Math.Max(0, Math.Min(3, (int)(elapsed / Math.Max(.01f, duration) * 4)));
        public static int MeteorFrame(float elapsed)
            => elapsed < ImpactDelay ? (elapsed < ImpactDelay * .55f ? 0 : 1)
                : elapsed < ImpactDelay + .65f ? 2 : 3;
        public static bool HoldCastPose(CastleBattleRules rules, bool meteorPresentation)
            => meteorPresentation || rules != null && rules.MeteorStarted && !rules.MeteorAbsorbed
                && rules.Ending == GameEnding.None;
    }
}
