using System;
using DragonQuest.Combat;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public static class CastleMeteorPresentation
    {
        public const float Duration = 3.6f, ImpactDelay = 2.1f, AbsorptionStart = .8f;
        public const float AbsorptionEnd = 2.65f, AbsorberReturn = 2.8f;
        public const int MeteorDamageDisplay = 9999;
        public const float ProtectDuration = 1.2f, ShieldHitDuration = .55f;
        public static bool IsMeteor(string actorId, AbilityDefinition ability)
            => actorId == CastleBattleRules.TyrantId && ability?.SourceAbilityId == "flaming-meteor";
        public const float TurnNumberDuration = 1.4f;
        public const int TurnNumberFontIncrease = 4;
        public static int ChargePose(CastleBattleRules rules)
            => Math.Max(0, Math.Min(3, (rules?.MeteorTurnsElapsed ?? 0) - 1));
        public static float TurnNumberDelay(AbilityDefinition ability)
            => ability?.SourceAbilityId == "tyrant-protect" ? ProtectDuration : 0;
        public static int EffectFrame(float elapsed, float duration)
            => Math.Max(0, Math.Min(3, (int)(elapsed / Math.Max(.01f, duration) * 4)));
        public static int MeteorFrame(float elapsed)
            => elapsed < ImpactDelay ? (elapsed < ImpactDelay * .55f ? 0 : 1)
                : elapsed < ImpactDelay + .65f ? 2 : 3;
        public static int DisplayedAmount(CombatVitalChange change, bool destructiveMeteor)
            => destructiveMeteor && change.Amount < 0 && !change.IsMp && !change.IsDamageOverTime
                ? -MeteorDamageDisplay : change.Amount;
        public static float AbsorberAdvance(float elapsed)
        {
            if (elapsed <= 0 || elapsed >= Duration) return 0;
            float value = elapsed < AbsorberReturn ? elapsed / .7f
                : (Duration - elapsed) / (Duration - AbsorberReturn);
            value = Math.Max(0, Math.Min(1, value));
            return value * value * (3 - 2 * value);
        }
        public static float AbsorptionProgress(float elapsed)
            => Math.Max(0, Math.Min(1, (elapsed - AbsorptionStart) / (AbsorptionEnd - AbsorptionStart)));
        public static int AbsorberPose(float elapsed) => elapsed < .35f ? 0
            : elapsed < .7f ? 1 : elapsed < AbsorberReturn ? 2 : 3;
        public static bool HoldCastPose(CastleBattleRules rules, bool meteorPresentation, bool protectPresentation = false)
            => !protectPresentation && (meteorPresentation || rules != null && rules.MeteorStarted
                && rules.ProtectCast && !rules.MeteorAbsorbed && rules.Ending == GameEnding.None);
    }

    // Mantem um unico numero por turno, independente da quantidade de Update/OnGUI.
    public sealed class CastleCastCounter
    {
        public int Value { get; private set; }
        public int DisplayValue => Value > 0 ? CastleBattleRules.MeteorCastTurns - Value + 1 : 0;
        public float StartedAt { get; private set; }
        public void Reset() { Value = 0; StartedAt = 0; }
        public void Observe(string actorId, AbilityDefinition ability, int elapsedTurns, float actionStartedAt)
        {
            if (actorId != CastleBattleRules.TyrantId || ability == null || elapsedTurns <= Value
                || elapsedTurns > CastleBattleRules.MeteorCastTurns) return;
            string action = ability.SourceAbilityId;
            if (action != "tyrant-protect" && action != "prepare-meteor" && action != "flaming-meteor") return;
            Value = elapsedTurns;
            StartedAt = actionStartedAt + CastleMeteorPresentation.TurnNumberDelay(ability);
        }
        public bool IsVisible(float now) => Value > 0 && now >= StartedAt
            && now - StartedAt < CastleMeteorPresentation.TurnNumberDuration;
    }
}
