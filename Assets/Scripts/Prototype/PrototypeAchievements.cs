using System.IO;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public static class PrototypeAchievements
    {
        private static EndingAchievementStore store;
        private static EndingAchievementStore Store => store ?? (store = new EndingAchievementStore(
            Path.Combine(Application.persistentDataPath, "Achievements")));

        public static EndingAchievements Progress => Store.Progress;
        public static string LastError => Store.LastError;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { store = null; }

        public static void Complete(GameEnding ending)
        {
            if (!Store.TryUnlock(ending, out string error)) Debug.LogWarning(error);
        }
    }
}
