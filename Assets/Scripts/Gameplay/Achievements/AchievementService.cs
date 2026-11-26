using System.IO;
using DragonQuest.Persistence;
using DragonQuest.Progression;
using UnityEngine;

// Conecta a conclusão de um final ao armazenamento de conquistas e à notificação exibida na interface.
namespace DragonQuest.Gameplay.Achievements
{
    public static class AchievementService
    {
        private static EndingAchievementStore store;
        // Guarda uma conquista que ainda precisa ser anunciada pela interface.
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

        // Entrega a notificação pendente ao consumidor e limpa o registro.
        public static EndingAchievement TakeNotification()
        {
            // Only a completed endgame in this play session can create an announcement.
            var notification = pendingNotification;
            pendingNotification = null;
            return notification;
        }
    }
}
