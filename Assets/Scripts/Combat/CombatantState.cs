using System;

namespace DragonQuest.Combat
{
    public enum CombatTeam { Party, Enemy }

    public sealed class CombatantState
    {
        public string Id { get; }
        public CombatantDefinition Definition { get; }
        public CombatTeam Team { get; }
        public int Hp { get; private set; }
        public int Mp { get; private set; }
        public bool IsAlive => Hp > 0;
        public bool IsDefending { get; internal set; }
        public int PoisonTurns { get; internal set; }
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
        internal void TakeDamage(int amount)
        {
            int previousHp = Hp;
            Hp = Math.Max(0, Hp - amount);
            if (Team == CombatTeam.Party) Limit.AddDamage(previousHp - Hp, Definition.MaxHp);
            if (!IsAlive) ClearConditions();
        }
        internal void RecoverHp(int amount) => Hp = (int)Math.Min(Definition.MaxHp, (long)Hp + amount);
        internal void RecoverMp(int amount) => Mp = (int)Math.Min(Definition.MaxMp, (long)Mp + amount);

        internal void ClearConditions()
        {
            IsDefending = false;
            PoisonTurns = 0;
            TauntTurns = 0;
            TauntedBy = null;
            ProtectedBy = null;
        }
    }
}
