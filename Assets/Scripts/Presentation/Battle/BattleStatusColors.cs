using DragonQuest.Combat;
using UnityEngine;

// Padroniza as cores usadas para representar condições e efeitos em combate.
namespace DragonQuest.Presentation.Battle
{
    public static class BattleStatusColors
    {
        public static Color Poison => new Color(.40f, 1f, .35f);
        public static Color Bound => new Color(.60f, .65f, 1f);
        // Define a cor reservada aos indicadores de dano periódico.
        public static Color DamageOverTime => new Color(.78f, .40f, 1f);
        public static Color Tint(bool poisoned, int boundActions) =>
            poisoned ? Poison : boundActions > 0 ? Bound : Color.white;
        public static Color Tint(CombatantState member) => Tint(member.IsPoisoned, member.BoundActions);
        public static Color VitalChange(CombatVitalChange change) => change.IsDamageOverTime ? DamageOverTime
            : change.Amount > 0 ? new Color(.30f, 1f, .40f)
            : change.IsCritical ? new Color(1f, .88f, .10f) : Color.white;
    }
}
