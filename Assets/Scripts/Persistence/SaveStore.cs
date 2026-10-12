using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace DragonQuest.Persistence
{
    public sealed class SaveStore
    {
        public const int SlotCount = 3;
        private const int MaxBytes = 4 * 1024 * 1024;
        private readonly string directory;
        public SaveStore(string saveDirectory) { directory = Path.GetFullPath(saveDirectory); }
        public string SlotPath(int slot)
        {
            if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, "slot-" + slot + ".json");
        }
        public bool Exists(int slot) => File.Exists(SlotPath(slot)) || File.Exists(SlotPath(slot) + ".bak");
        public static byte[] Encode(JourneyData data)
        {
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(JourneyData)).WriteObject(stream, data); return stream.ToArray(); }
        }
        public static JourneyData Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxBytes) throw new InvalidDataException("Tamanho de save invalido.");
            using (var stream = new MemoryStream(bytes)) return (JourneyData)new DataContractJsonSerializer(typeof(JourneyData)).ReadObject(stream);
        }
        public bool TryWrite(int slot, JourneyData data, out string error)
        {
            string path = SlotPath(slot), temporary = path + ".tmp";
            try
            {
                JourneySnapshot.Restore(data); // Valida antes de tocar no arquivo anterior.
                byte[] bytes = Encode(data); if (bytes.Length > MaxBytes) throw new InvalidDataException("Save muito grande.");
                Directory.CreateDirectory(directory);
                using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (File.Exists(path))
                {
                    // Guarda apenas uma revisao validada como backup, inclusive depois de recuperar corrupcao.
                    bool valid = TryReadFile(path, out _, out _, out _);
                    File.Replace(temporary, path, valid ? path + ".bak" : null);
                }
                else File.Move(temporary, path);
                error = null; return true;
            }
            catch (Exception ex) { error = "Nao foi possivel salvar: " + ex.Message; return false; }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
        public bool TryRead(int slot, out RestoredJourney journey, out string message)
        {
            string path = SlotPath(slot);
            if (TryReadFile(path, out journey, out string primaryError, out bool unsupported)) { message = null; return true; }
            // Um save de outra versao nao deve ser silenciosamente substituido pela copia antiga.
            if (unsupported) { message = primaryError; return false; }
            if (TryReadFile(path + ".bak", out journey, out _, out _))
            { message = "Save principal indisponivel. A copia anterior foi recuperada."; return true; }
            message = Exists(slot) ? "Save indisponivel: " + primaryError : "Slot vazio."; return false;
        }
        private static bool TryReadFile(string path, out RestoredJourney journey, out string error, out bool unsupported)
        {
            journey = null; unsupported = false;
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) throw new FileNotFoundException("Arquivo ausente.");
                if (info.Length > MaxBytes) throw new InvalidDataException("Arquivo muito grande.");
                JourneyData data = Decode(File.ReadAllBytes(path));
                if (data.Version != JourneyData.CurrentVersion)
                { unsupported = true; error = "Versao de save nao suportada: " + data.Version + "."; return false; }
                journey = JourneySnapshot.Restore(data); error = null; return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
    }
}
