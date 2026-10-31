using DragonQuest.Combat;
using System.Collections.Generic;

namespace DragonQuest.Presentation.Battle
{
    // Battle state is resolved before its animation; change the pose at the visible impact.
    public sealed class HeroKnockoutPresentation
    {
        private sealed class Pose
        {
            public bool Visible, Requested;
            public float ChangesAt;
        }
        private readonly Dictionary<CombatantState, Pose> poses = new Dictionary<CombatantState, Pose>();
        public void Reset() => poses.Clear();
        public void Observe(CombatantState fighter, float impactTime, float now)
        {
            if (fighter.Team != CombatTeam.Party) return;
            bool knockedOut = !fighter.IsAlive;
            if (!poses.TryGetValue(fighter, out var pose))
            {
                poses.Add(fighter, new Pose { Visible = knockedOut, Requested = knockedOut });
                return;
            }
            if (pose.Requested != knockedOut)
            {
                pose.Requested = knockedOut;
                pose.ChangesAt = impactTime;
            }
            if (now >= pose.ChangesAt) pose.Visible = pose.Requested;
        }
        public bool IsKnockedOut(CombatantState fighter) => fighter.Team == CombatTeam.Party
            && (poses.TryGetValue(fighter, out var pose) ? pose.Visible : !fighter.IsAlive);
    }
}
