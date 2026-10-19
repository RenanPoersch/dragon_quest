using System;
using System.Collections.Generic;

namespace DragonQuest.Progression
{
    public sealed class EndingAchievement
    {
        public GameEnding Ending { get; }
        public string Name { get; }
        public string IconResource { get; }
        internal int Bit { get; }

        internal EndingAchievement(GameEnding ending, string name, string icon, int bit)
        { Ending = ending; Name = name; IconResource = "PixelArt/Achievements/" + icon; Bit = bit; }
    }

    // Profile progress is independent of journeys and counts each ending only once.
    public sealed class EndingAchievements
    {
        public const int Total = 3;
        public const int AllUnlocked = 7;
        public static readonly IReadOnlyList<EndingAchievement> All = Array.AsReadOnly(new[]
        {
            new EndingAchievement(GameEnding.KingFreed, "The good", "TheGood", 1),
            new EndingAchievement(GameEnding.CastleDestroyed, "The bad", "TheBad", 2),
            new EndingAchievement(GameEnding.HeroPossessed, "The ugly", "TheUgly", 4)
        });

        public int Mask { get; private set; }
        public int Count { get; private set; }
        public string Counter => Count + "/" + Total;

        public EndingAchievements(int mask = 0)
        {
            if (mask < 0 || (mask & ~AllUnlocked) != 0) throw new ArgumentOutOfRangeException(nameof(mask));
            Mask = mask;
            foreach (var achievement in All)
                if (Has(achievement.Ending)) Count++;
        }

        public bool Has(GameEnding ending)
        {
            foreach (var achievement in All)
                if (achievement.Ending == ending) return (Mask & achievement.Bit) != 0;
            return false;
        }

        public bool Unlock(GameEnding ending)
        {
            foreach (var achievement in All)
            {
                if (achievement.Ending != ending) continue;
                if (Has(ending)) return false;
                Mask |= achievement.Bit;
                Count++;
                return true;
            }
            return false;
        }
    }
}
