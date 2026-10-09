using System;

namespace DragonQuest.Combat
{
    // Compartilhada com o membro da equipe; nao e recriada ao iniciar outra batalha.
    public sealed class LimitGauge
    {
        public int Charge { get; private set; }
        public bool IsReady => Charge == 100;

        internal void AddDamage(int actualDamage, int maxHp)
        {
            if (actualDamage < 0 || maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(actualDamage));
            if (actualDamage == 0) return;
            int increase = (int)Math.Min(100L, ((long)actualDamage * 100 + maxHp - 1) / maxHp);
            Charge = Math.Min(100, Charge + increase);
        }

        internal void Spend() => Charge = 0;
    }
}
