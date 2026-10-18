using System.Collections.Generic;

namespace DragonQuest.Combat
{
    public enum CombatStatus { Poison, Shield, Defending, Protected, Taunted, Bound, Charging, Defeated, MindControlled, Silenced }
    public readonly struct StatusIndicator
    {
        public readonly CombatStatus Status;
        public readonly string Description;
        public readonly int Turns;
        public StatusIndicator(CombatStatus status, string description, int turns = 0)
        { Status = status; Description = description; Turns = turns; }
    }
    public static class CombatStatuses
    {
        public static List<StatusIndicator> Active(CombatantState member)
        {
            var result = new List<StatusIndicator>();
            if (!member.IsAlive) { result.Add(new StatusIndicator(CombatStatus.Defeated, "Derrotado")); return result; }
            if (member.PoisonTurns > 0) result.Add(new StatusIndicator(CombatStatus.Poison, "Veneno: 5% do HP maximo por turno ou a cada 3s no mapa; ate curar ou chegar a 0 HP"));
            if (member.ShieldTurns > 0) result.Add(new StatusIndicator(CombatStatus.Shield, "Shield: -50% de todo dano recebido", member.ShieldTurns));
            if (member.IsDefending) result.Add(new StatusIndicator(CombatStatus.Defending, "Defender: -50% de dano de ataques ate o proximo turno"));
            if (member.ProtectedBy != null) result.Add(new StatusIndicator(CombatStatus.Protected, "Protegido por " + member.ProtectedBy.Definition.Name));
            if (member.TauntTurns > 0) result.Add(new StatusIndicator(CombatStatus.Taunted, "Provocado", member.TauntTurns));
            if (member.BoundActions > 0) result.Add(new StatusIndicator(CombatStatus.Bound, "Acorrentado"));
            if (member.MindControlTurns > 0) result.Add(new StatusIndicator(CombatStatus.MindControlled,
                "Confusao: ataca automaticamente um aliado ou inimigo aleatorio", member.MindControlTurns));
            if (member.SilenceTurns > 0) result.Add(new StatusIndicator(CombatStatus.Silenced,
                "Silencio: nao pode usar magias ou invocacoes", member.SilenceTurns));
            if (member.IsCharging) result.Add(new StatusIndicator(CombatStatus.Charging, "Preparando ataque"));
            return result;
        }
    }
}
