using System;

namespace DragonQuest.Combat
{
    // Compartilhada com o membro da equipe; nao e recriada ao iniciar outra batalha.
    public sealed class LimitGauge
    {
        private int charge;
        public bool IsInfinite { get; private set; }
        public int Charge => IsInfinite ? 100 : charge;
        internal int SavedCharge => charge;
        internal void SetInfinite(bool enabled) => IsInfinite = enabled;
        public bool IsReady => Charge == 100;

        internal void AddDamage(int actualDamage, int maxHp)
        {
            if (actualDamage < 0 || maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(actualDamage));
            if (actualDamage == 0 || IsInfinite) return;
            int increase = (int)Math.Min(100L, ((long)actualDamage * 100 + maxHp - 1) / maxHp);
            charge = Math.Min(100, charge + increase);
        }

        internal void Spend() { if (!IsInfinite) charge = 0; }
        internal void Restore(int charge)
        {
            if (charge < 0 || charge > 100) throw new ArgumentOutOfRangeException(nameof(charge));
            this.charge = charge;
        }
    }
}
