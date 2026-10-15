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
        public static bool HoldCastPose(CastleBattleRules rules, bool meteorPresentation, bool protectPresentation = false)
            => !protectPresentation && (meteorPresentation || rules != null && rules.MeteorStarted
                && rules.ProtectCast && !rules.MeteorAbsorbed && rules.Ending == GameEnding.None);
    }

    // Mantem um unico numero por turno, independente da quantidade de Update/OnGUI.
    public sealed class CastleCastCounter
    {
        public int Value { get; private set; }
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
