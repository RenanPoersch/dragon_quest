using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DragonQuest.Progression;

namespace DragonQuest.Persistence
{
    public sealed class EndingAchievementStore
    {
        [DataContract]
        private sealed class Profile
        {
            [DataMember(IsRequired = true)] public int Version;
            [DataMember(IsRequired = true)] public int Unlocked;
        }

        private readonly string path;
        private bool dirty;
        private bool unsupportedVersion;
        public EndingAchievements Progress { get; private set; }
        public string LastError { get; private set; }

        public EndingAchievementStore(string profileDirectory)
        {
            path = Path.Combine(Path.GetFullPath(profileDirectory), "endings.json");
            Progress = new EndingAchievements();
            if (TryRead(path, out var current, out unsupportedVersion)) Progress = current;
            else if (!unsupportedVersion && TryRead(path + ".bak", out var backup, out _)) Progress = backup;
            else if (File.Exists(path) || File.Exists(path + ".bak"))
                LastError = "Nao foi possivel ler as conquistas salvas.";
        }

        public bool TryUnlock(GameEnding ending, out string error)
        {
            dirty |= Progress.Unlock(ending);
            error = LastError;
            if (!dirty) return error == null;
            if (unsupportedVersion)
            {
                error = LastError = "Versao de conquistas nao suportada. O arquivo foi preservado.";
                return false;
            }
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new DataContractJsonSerializer(typeof(Profile)).WriteObject(file,
                        new Profile { Version = 1, Unlocked = Progress.Mask });
                    file.Flush(true);
                }
                if (File.Exists(path))
                {
                    bool valid = TryRead(path, out _, out _);
                    File.Replace(temporary, path, valid ? path + ".bak" : null);
                }
                else File.Move(temporary, path);
                dirty = false;
                error = LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                error = LastError = "Nao foi possivel salvar as conquistas: " + ex.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static bool TryRead(string file, out EndingAchievements progress, out bool unsupported)
        {
            progress = null; unsupported = false;
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists || info.Length == 0 || info.Length > 4096) return false;
                using (var stream = File.OpenRead(file))
                {
                    var data = (Profile)new DataContractJsonSerializer(typeof(Profile)).ReadObject(stream);
                    if (data == null) return false;
                    if (data.Version != 1) { unsupported = true; return false; }
                    progress = new EndingAchievements(data.Unlocked);
                    return true;
                }
            }
            catch (Exception) { return false; }
        }
    }
}
