using System;

namespace DragonQuest.Combat
{
    public enum AbilityEffect { PhysicalDamage, Heal, Defend }
    public enum TargetRule { Enemy, Ally, Self }

    public sealed class AbilityDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int MpCost { get; }
        public int Power { get; }
        public AbilityEffect Effect { get; }
        public TargetRule Targets { get; }

        public AbilityDefinition(string id, string name, string description, int mpCost, int power,
            AbilityEffect effect, TargetRule targets)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Habilidade precisa de ID.", nameof(id));
            if (mpCost < 0 || power < 0) throw new ArgumentOutOfRangeException(nameof(mpCost));
            if (effect == AbilityEffect.Defend && targets != TargetRule.Self)
                throw new ArgumentException("Defesa deve ter o proprio usuario como alvo.");
            Id = id;
            Name = name;
            Description = description;
            MpCost = mpCost;
            Power = power;
            Effect = effect;
            Targets = targets;
        }
    }
}
