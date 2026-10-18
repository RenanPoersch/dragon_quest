using System.Collections.Generic;

namespace DragonQuest.Combat
{
    public enum CombatStatus { Poison, Shield, Defending, Protected, Taunted, Bound, Charging, Defeated, MindControlled, Silenced }
    public readonly struct StatusIndicator
    {
        public readonly CombatStatus Status;
        public readonly int Turns;
        public StatusIndicator(CombatStatus status, int turns = 0)
        { Status = status; Turns = turns; }
    }
    public static class CombatStatuses
    {
        public static List<StatusIndicator> Active(CombatantState member)
        {
            var result = new List<StatusIndicator>();
            if (!member.IsAlive) { result.Add(new StatusIndicator(CombatStatus.Defeated)); return result; }
            if (member.PoisonTurns > 0) result.Add(new StatusIndicator(CombatStatus.Poison));
            if (member.ShieldTurns > 0) result.Add(new StatusIndicator(CombatStatus.Shield, member.ShieldTurns));
            if (member.IsDefending) result.Add(new StatusIndicator(CombatStatus.Defending));
            if (member.ProtectedBy != null) result.Add(new StatusIndicator(CombatStatus.Protected));
            if (member.TauntTurns > 0) result.Add(new StatusIndicator(CombatStatus.Taunted, member.TauntTurns));
            if (member.BoundActions > 0) result.Add(new StatusIndicator(CombatStatus.Bound));
            if (member.MindControlTurns > 0) result.Add(new StatusIndicator(CombatStatus.MindControlled, member.MindControlTurns));
            if (member.SilenceTurns > 0) result.Add(new StatusIndicator(CombatStatus.Silenced, member.SilenceTurns));
            if (member.IsCharging) result.Add(new StatusIndicator(CombatStatus.Charging));
            return result;
        }
    }
}
