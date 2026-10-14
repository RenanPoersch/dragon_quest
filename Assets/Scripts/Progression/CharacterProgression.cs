using System;

namespace DragonQuest.Progression
{
    // XP total e a fonte do nivel; atributos sao sempre derivados, nunca acumulados no save.
    public sealed class CharacterProgression
    {
        public const int MaxLevel = 99;
        public static int MaxExperience => ExperienceForLevel(MaxLevel);
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceInLevel => Experience - ExperienceForLevel(Level);
        public int ExperienceToNextLevel => Level == MaxLevel ? 0 : ExperienceForLevel(Level + 1) - Experience;

        public static int ExperienceForLevel(int level)
        {
            if (level < 1 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return 25 * (level - 1) * (level + 2);
        }

        public int AddExperience(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int previous = Level;
            Experience += (int)Math.Min(amount, MaxExperience - Experience);
            while (Level < MaxLevel && Experience >= ExperienceForLevel(Level + 1)) Level++;
            return Level - previous;
        }

        internal void Restore(int experience)
        {
            if (experience < 0 || experience > MaxExperience) throw new ArgumentOutOfRangeException(nameof(experience));
            Level = 1; Experience = 0;
            AddExperience(experience);
        }
    }
}
