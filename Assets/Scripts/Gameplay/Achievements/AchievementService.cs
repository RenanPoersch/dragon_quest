using System.IO;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Gameplay.Achievements
{
    public static class AchievementService
    {
        private static EndingAchievementStore store;
        private static EndingAchievement pendingNotification;
        private static EndingAchievementStore Store => store ?? (store = new EndingAchievementStore(
            Path.Combine(Application.persistentDataPath, "Achievements")));

        public static EndingAchievements Progress => Store.Progress;
        public static string LastError => Store.LastError;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { store = null; pendingNotification = null; }

        public static void Complete(GameEnding ending)
        {
            if (!Store.TryUnlock(ending, out string error)) Debug.LogWarning(error);
            if (!Store.Progress.Has(ending)) return;
            foreach (var achievement in EndingAchievements.All)
                if (achievement.Ending == ending) { pendingNotification = achievement; break; }
        }

        public static EndingAchievement TakeNotification()
        {
            // Only a completed endgame in this play session can create an announcement.
            var notification = pendingNotification;
            pendingNotification = null;
            return notification;
        }
    }
}
