using System;
using System.Collections.Generic;

namespace DragonQuest.Combat
{
    // Keeps battle/reward data intact while removing the defeated enemy from presentation.
    public sealed class DefeatedEnemyPresentation
    {
        public const float FlashDuration = .20f;
        public const float FadeDuration = .35f;
        public const float Duration = FlashDuration + FadeDuration;
        private readonly Dictionary<CombatantState, float> deaths = new Dictionary<CombatantState, float>();
        public static float EndOfAction(float originalEnd, float impactTime, IReadOnlyList<CombatantState> targets)
        {
            foreach (CombatantState target in targets)
                if (target.Team == CombatTeam.Enemy && !target.IsAlive) return Math.Max(originalEnd, impactTime + Duration);
            return originalEnd;
        }
        public void Reset() => deaths.Clear();
        public void Observe(CombatantState fighter, float impactTime)
        {
            if (fighter.Team != CombatTeam.Enemy) return;
            if (fighter.IsAlive) deaths.Remove(fighter);
            else if (!deaths.ContainsKey(fighter)) deaths.Add(fighter, impactTime);
        }
        public float Alpha(CombatantState fighter, float now)
        {
            if (fighter.IsAlive || fighter.Team != CombatTeam.Enemy) return 1;
            if (!deaths.TryGetValue(fighter, out float started)) return 0;
            float age = now - started;
            return age <= FlashDuration ? 1 : (float)Math.Max(0, 1 - (age - FlashDuration) / FadeDuration);
        }
        public bool IsVisible(CombatantState fighter, float now) => Alpha(fighter, now) > 0;
        public bool IsRed(CombatantState fighter, float now) => !fighter.IsAlive && fighter.Team == CombatTeam.Enemy
            && deaths.TryGetValue(fighter, out float started) && now >= started && now < started + Duration;
    }
}
