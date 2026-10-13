using System;

namespace DragonQuest.Combat
{
    public enum CombatTeam { Party, Enemy }

    public readonly struct CombatVitalChange
    {
        public readonly int Sequence, Amount;
        public readonly bool IsCritical, IsMp;
        public CombatVitalChange(int sequence, int amount, bool critical, bool mp)
        { Sequence = sequence; Amount = amount; IsCritical = critical; IsMp = mp; }
    }

    public sealed class CombatantState
    {
        public string Id { get; }
        public CombatantDefinition Definition { get; }
        public CombatTeam Team { get; }
        private readonly System.Collections.Generic.List<CombatVitalChange> vitalChanges = new System.Collections.Generic.List<CombatVitalChange>();
        private int vitalSequence;
        public System.Collections.Generic.IReadOnlyList<CombatVitalChange> VitalChanges => vitalChanges.AsReadOnly();
        private void RecordVitalChange(int amount, bool critical = false, bool mp = false)
        {
            if (amount == 0) return;
            if (vitalChanges.Count == 64) vitalChanges.RemoveAt(0);
            vitalChanges.Add(new CombatVitalChange(++vitalSequence, amount, critical, mp));
        }
        public int Hp { get; private set; }
        public int Mp { get; private set; }
        public bool IsAlive => Hp > 0;
        public bool IsDefending { get; internal set; }
        public bool IsCharging { get; internal set; }
        public int BoundActions { get; internal set; }
        public int PoisonTurns { get; internal set; }
        public int ShieldTurns { get; private set; }
        private bool skipShieldTick;
        public int TauntTurns { get; internal set; }
        public CombatantState TauntedBy { get; internal set; }
        public CombatantState ProtectedBy { get; internal set; }
        public int TurnsTaken { get; internal set; }
        public LimitGauge Limit { get; }

        public CombatantState(string id, CombatantDefinition definition, CombatTeam team, LimitGauge limit = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Combatente precisa de ID.", nameof(id));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Id = id;
            Team = team;
            Hp = definition.MaxHp;
            Mp = definition.MaxMp;
            Limit = limit ?? new LimitGauge();
        }

        internal void SpendMp(int cost) => Mp -= cost;
        internal void TakeDamage(int amount, bool critical = false)
        {
            if (ShieldTurns > 0 && amount > 0) amount = (int)(((long)amount + 1) / 2);
            int previousHp = Hp;
            Hp = Math.Max(0, Hp - amount);
            RecordVitalChange(Hp - previousHp, critical);
            if (Team == CombatTeam.Party) Limit.AddDamage(previousHp - Hp, Definition.MaxHp);
            if (!IsAlive) ClearConditions();
        }
        internal void RecoverHp(int amount)
        {
            int before = Hp;
            Hp = (int)Math.Min(Definition.MaxHp, (long)Hp + amount);
            RecordVitalChange(Hp - before);
        }
        internal void RecoverMp(int amount)
        {
            int before = Mp;
            Mp = (int)Math.Min(Definition.MaxMp, (long)Mp + amount);
            RecordVitalChange(Mp - before, mp: true);
        }

        internal void ClearConditions()
        {
            IsDefending = false;
            IsCharging = false;
            BoundActions = 0;
            PoisonTurns = 0;
            ShieldTurns = 0; skipShieldTick = false;
            TauntTurns = 0;
            TauntedBy = null;
            ProtectedBy = null;
        }

        internal void GrantDamageShield(int turns, bool castingAction)
        {
            if (!IsAlive) return;
            ShieldTurns = Math.Max(ShieldTurns, turns);
            skipShieldTick = castingAction;
        }

        internal void AdvanceShieldDuration()
        {
            if (skipShieldTick) skipShieldTick = false;
            else if (ShieldTurns > 0) ShieldTurns--;
        }
    }
}
