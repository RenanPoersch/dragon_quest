using System;
using System.IO;
using DragonQuest.Persistence;
using DragonQuest.Progression;

public static class EndingAchievementChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }

    public static int Main(string[] args)
    {
        string root = Path.Combine(args[0], "achievements-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var endings = new[] { GameEnding.KingFreed, GameEnding.CastleDestroyed, GameEnding.HeroPossessed };
            Check(EndingAchievements.All[0].Name == "The good" && EndingAchievements.All[0].Ending == endings[0], "Good mapping.");
            Check(EndingAchievements.All[1].Name == "The bad" && EndingAchievements.All[1].Ending == endings[1], "Bad mapping.");
            Check(EndingAchievements.All[2].Name == "The ugly" && EndingAchievements.All[2].Ending == endings[2], "Ugly mapping.");
            int[] countsByMask = { 0, 1, 1, 2, 1, 2, 2, 3 };
            for (int mask = 0; mask < countsByMask.Length; mask++)
            {
                var restored = new EndingAchievements(mask);
                Check(restored.Count == countsByMask[mask] && restored.Counter == countsByMask[mask] + "/3",
                    "Existing profiles restore the number of distinct endings, not the numeric mask.");
                Check(!restored.Unlock(GameEnding.None) && !restored.Unlock((GameEnding)99)
                    && restored.Count == countsByMask[mask], "Invalid endings cannot increment restored progress.");
            }
            foreach (var order in new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } })
            {
                string directory = Path.Combine(root, string.Join("", order));
                var profile = new EndingAchievementStore(directory);
                Check(profile.Progress.Count == 0 && profile.Progress.Counter == "0/3", "Fresh profile.");
                Check(!Directory.Exists(directory), "Reading progress does not create saves.");
                Check(profile.TryUnlock(GameEnding.None, out _) && profile.TryUnlock((GameEnding)99, out _)
                    && !Directory.Exists(directory), "No unlock for defeat or unknown ending.");
                for (int i = 0; i < order.Length; i++)
                {
                    var ending = endings[order[i]];
                    Check(profile.TryUnlock(ending, out string error) && error == null, "Ending persists.");
                    Check(profile.Progress.Counter == (i + 1) + "/3", "Unique ending count.");
                    profile = new EndingAchievementStore(directory);
                    Check(profile.Progress.Has(ending) && profile.Progress.Count == i + 1, "Restart preserves unlocks.");
                    byte[] saved = File.ReadAllBytes(Path.Combine(directory, "endings.json"));
                    for (int completed = 0; completed <= i; completed++)
                        Check(profile.TryUnlock(endings[order[completed]], out _) && profile.Progress.Count == i + 1,
                            "Replaying any previous ending cannot increment the count.");
                    Check(Convert.ToBase64String(saved) == Convert.ToBase64String(File.ReadAllBytes(Path.Combine(directory, "endings.json"))),
                        "Replay does not rewrite the profile.");
                }
                Check(profile.Progress.Mask == EndingAchievements.AllUnlocked, "All three endings accumulated.");
                File.WriteAllText(Path.Combine(directory, "slot-1.json"), "journey sentinel");
                Check(new EndingAchievementStore(directory).Progress.Count == 3, "Journey contents do not affect profile.");
                File.Delete(Path.Combine(directory, "slot-1.json"));
                Check(new EndingAchievementStore(directory).Progress.Count == 3, "Deleting a journey cannot erase achievements.");
                File.WriteAllText(Path.Combine(directory, "endings.json"), "corrupt");
                var recovered = new EndingAchievementStore(directory);
                Check(recovered.Progress.Count == 2 && recovered.LastError == null, "Corrupt primary recovers valid backup.");
                Check(recovered.TryUnlock(endings[order[2]], out _) && new EndingAchievementStore(directory).Progress.Count == 3,
                    "Recovery permits collecting the remaining achievement again.");
            }
            string blocked = Path.Combine(root, "blocked");
            File.WriteAllText(blocked, "not a directory");
            var failed = new EndingAchievementStore(blocked);
            Check(!failed.TryUnlock(GameEnding.KingFreed, out string failure) && !string.IsNullOrEmpty(failure), "Storage failure is reported.");
            Check(failed.Progress.Count == 1, "Storage failure retains session progress.");
            File.Delete(blocked);
            Check(failed.TryUnlock(GameEnding.KingFreed, out _) && new EndingAchievementStore(blocked).Progress.Count == 1,
                "Repeated ending retries an unsuccessful write.");
            string future = Path.Combine(root, "future");
            Directory.CreateDirectory(future);
            string futureFile = Path.Combine(future, "endings.json"), futureJson = "{\"Version\":99,\"Unlocked\":7}";
            File.WriteAllText(futureFile, futureJson);
            Check(!new EndingAchievementStore(future).TryUnlock(GameEnding.KingFreed, out _)
                && File.ReadAllText(futureFile) == futureJson, "Unknown future version is preserved.");
            foreach (int invalid in new[] { -1, 8, 99 })
            {
                bool rejected = false;
                try { new EndingAchievements(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "Invalid achievement mask is rejected.");
            }
            Console.WriteLine("ENDING_ACHIEVEMENTS_OK: " + checks + " mapping, six play orders, persistence, replay and recovery checks.");
            return 0;
        }
        finally { Directory.Delete(root, true); }
    }
}
