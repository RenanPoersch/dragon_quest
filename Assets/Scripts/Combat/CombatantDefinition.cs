using System;
using System.Collections.Generic;

namespace DragonQuest.Combat
{
    public sealed class CombatantDefinition
    {
        public string Name { get; }
        public string Role { get; }
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int Magic { get; }
        public int Speed { get; }
        public int GoldReward { get; }
        public ElementalAffinities Affinities { get; }
        public IReadOnlyList<AbilityDefinition> Abilities { get; }

        public CombatantDefinition(string name, string role, int maxHp, int maxMp, int attack,
            int defense, int magic, int speed, params AbilityDefinition[] abilities)
            : this(name, role, maxHp, maxMp, attack, defense, magic, speed, ElementalAffinities.Neutral, abilities) { }

        public CombatantDefinition(string name, string role, int maxHp, int maxMp, int attack,
            int defense, int magic, int speed, ElementalAffinities affinities, params AbilityDefinition[] abilities)
            : this(name, role, maxHp, maxMp, attack, defense, magic, speed, affinities, 0, abilities) { }

        public CombatantDefinition(string name, string role, int maxHp, int maxMp, int attack,
            int defense, int magic, int speed, ElementalAffinities affinities, int goldReward, params AbilityDefinition[] abilities)
        {
            Affinities = affinities ?? throw new ArgumentNullException(nameof(affinities));
            if (goldReward < 0) throw new ArgumentOutOfRangeException(nameof(goldReward), "Recompensa nao pode ser negativa.");
            if (maxHp <= 0 || maxMp < 0 || attack < 0 || defense < 0 || magic < 0 || speed < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp), "Atributos invalidos.");
            if (abilities == null || abilities.Length == 0) throw new ArgumentException("Combatente precisa de habilidades.");
            var ids = new HashSet<string>();
            foreach (AbilityDefinition ability in abilities)
                if (ability == null || !ids.Add(ability.Id)) throw new ArgumentException("Habilidades nulas ou IDs repetidos.");
            Name = name;
            Role = role;
            MaxHp = maxHp;
            MaxMp = maxMp;
            Attack = attack;
            Defense = defense;
            Magic = magic;
            Speed = speed;
            GoldReward = goldReward;
            Abilities = Array.AsReadOnly((AbilityDefinition[])abilities.Clone());
        }
    }
}
