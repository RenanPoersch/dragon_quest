using System;

namespace DragonQuest.Combat
{
    public enum CombatTeam { Party, Enemy }

    public readonly struct CombatVitalChange
    {
        public readonly int Sequence, Amount, ActionHitIndex;
        public readonly bool IsCritical, IsMp, IsShielded, IsDamageOverTime;
        public CombatVitalChange(int sequence, int amount, bool critical, bool mp, bool shielded = false, bool damageOverTime = false, int actionHitIndex = 0)
        { Sequence = sequence; Amount = amount; IsCritical = critical; IsMp = mp; IsShielded = shielded; IsDamageOverTime = damageOverTime; ActionHitIndex = actionHitIndex; }
    }

    public sealed class CombatantState
    {
        public string Id { get; }
        public CombatantDefinition Definition { get; private set; }
        public CombatTeam Team { get; }
        private readonly System.Collections.Generic.List<CombatVitalChange> vitalChanges = new System.Collections.Generic.List<CombatVitalChange>();
        private int vitalSequence;
        public System.Collections.Generic.IReadOnlyList<CombatVitalChange> VitalChanges => vitalChanges.AsReadOnly();
        private void RecordVitalChange(int amount, bool critical = false, bool mp = false, bool shielded = false, bool damageOverTime = false, int actionHitIndex = 0)
        {
            if (amount == 0) return;
            if (vitalChanges.Count == 64) vitalChanges.RemoveAt(0);
            vitalChanges.Add(new CombatVitalChange(++vitalSequence, amount, critical, mp, shielded, damageOverTime, actionHitIndex));
        }
        public int Hp { get; private set; }
        public int Mp { get; private set; }
        public bool IsAlive => Hp > 0;
        public bool IsDefending { get; internal set; }
        public bool IsCharging { get; internal set; }
        public int BoundActions { get; internal set; }
        public int MindControlTurns { get; internal set; }
        public int SilenceTurns { get; internal set; }
        public bool IsSilenced => SilenceTurns > 0;
        // Positive means poisoned; poison lasts until cleansed or defeated.
        public int PoisonTurns { get; internal set; }
        public bool IsPoisoned => PoisonTurns > 0;
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

        // Preserve the actor, turn, vitals and temporary conditions when a cheat raises its level.
        internal void RefreshDefinition(CombatantDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Hp = Math.Min(Hp, definition.MaxHp);
            Mp = Math.Min(Mp, definition.MaxMp);
        }

        internal void SpendMp(int cost) => Mp -= cost;
        internal void TakeDamage(int amount, bool critical = false, bool damageOverTime = false, int actionHitIndex = 0,
            bool deferDefeatCleanup = false)
        {
            bool shielded = ShieldTurns > 0 && amount > 0;
            if (shielded) amount = (int)(((long)amount + 1) / 2);
            int previousHp = Hp;
            Hp = Math.Max(0, Hp - amount);
            // Each combo impact displays its full damage, even after an earlier hit depleted HP.
            RecordVitalChange(actionHitIndex > 0 ? -amount : Hp - previousHp, critical, shielded: shielded, damageOverTime: damageOverTime, actionHitIndex: actionHitIndex);
            if (Team == CombatTeam.Party) Limit.AddDamage(previousHp - Hp, Definition.MaxHp);
            // Keep mitigation consistent until the current multi-hit action finishes.
            if (!IsAlive && !deferDefeatCleanup) ClearConditions();
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

        internal void RestoreVitals(int hp, int mp, bool poisoned, int boundActions)
        {
            Hp = Math.Max(0, Math.Min(Definition.MaxHp, hp));
            Mp = Math.Max(0, Math.Min(Definition.MaxMp, mp));
            PoisonTurns = IsAlive && poisoned ? 1 : 0;
            BoundActions = IsAlive ? Math.Max(0, boundActions) : 0;
        }

        internal int TickPoison()
        {
            if (!IsAlive || !IsPoisoned) return 0;
            int before = Hp;
            TakeDamage((int)(((long)Definition.MaxHp + 19) / 20), damageOverTime: true);
            return before - Hp;
        }

        internal void ClearConditions()
        {
            IsDefending = false;
            IsCharging = false;
            BoundActions = 0;
            MindControlTurns = SilenceTurns = 0;
            PoisonTurns = 0;
            ShieldTurns = 0; skipShieldTick = false;
            TauntTurns = 0;
            TauntedBy = null;
            ProtectedBy = null;
        }

        internal void ClearHarmfulConditions()
        {
            PoisonTurns = 0; BoundActions = 0; MindControlTurns = SilenceTurns = 0; TauntTurns = 0; TauntedBy = null;
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
