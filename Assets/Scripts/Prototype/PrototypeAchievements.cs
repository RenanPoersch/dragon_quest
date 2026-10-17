using System.IO;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeAchievements
    {
        private const string NotifiedMaskKey = "DragonQuest.EndingAchievements.NotifiedMask.v1";
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
            if (pendingNotification != null)
            {
                var notification = pendingNotification;
                pendingNotification = null;
                return notification;
            }
            // Recover saved unlocks whose notification was never displayed, including older profiles.
            int notified = PlayerPrefs.GetInt(NotifiedMaskKey, 0);
            foreach (var achievement in EndingAchievements.All)
                if (Store.Progress.Has(achievement.Ending) && (notified & achievement.Bit) == 0)
                    return achievement;
            return null;
        }

        public static void MarkNotified(EndingAchievement achievement)
        {
            if (achievement == null) return;
            PlayerPrefs.SetInt(NotifiedMaskKey, PlayerPrefs.GetInt(NotifiedMaskKey, 0) | achievement.Bit);
            PlayerPrefs.Save();
        }
    }
}
