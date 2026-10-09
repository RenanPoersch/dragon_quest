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

        public CombatantState(string id, CombatantDefinition definition, CombatTeam team)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Combatente precisa de ID.", nameof(id));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Id = id;
            Team = team;
            Hp = definition.MaxHp;
            Mp = definition.MaxMp;
        }

        internal void SpendMp(int cost) => Mp -= cost;
        internal void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);
        internal void RecoverHp(int amount) => Hp = (int)Math.Min(Definition.MaxHp, (long)Hp + amount);
    }
}
