using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SortEverything.Domain;
using UnityEngine;

namespace SortEverything.Runtime
{
    public enum SaveLoadOutcome { NewSave, Loaded, Recovered, AllCorrupt }

    public interface ISaveStore
    {
        /// <summary>Durably commits the candidate. Returns false and leaves the previous disk state when writing fails.</summary>
        bool TryCommit(SaveData candidate, out string error);
        SaveLoadOutcome Load(out SaveData save, out string diagnostic);
        void DeleteAllForDevelopment();
    }

    [Serializable]
    sealed class SaveEnvelope
    {
        public long revision;
        public string payload;
        public string sha256;
    }

    /// <summary>
    /// save.json / save.backup.json / save.pending.json with a SHA-256 checked envelope (Section 26).
    /// Pending is fully written and verified, then promoted through File.Replace (or a rename fallback that never
    /// deletes the only valid copy). The hash is corruption detection, not anti-cheat.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        readonly string _dir;
        readonly object _lock = new object();
        public string MainPath => Path.Combine(_dir, "save.json");
        public string BackupPath => Path.Combine(_dir, "save.backup.json");
        public string PendingPath => Path.Combine(_dir, "save.pending.json");

        /// <summary>Test hook: throw at a named write stage ("pending", "promote").</summary>
        public string FailAtStage;

        public FileSaveStore(string directory)
        {
            _dir = directory;
            Directory.CreateDirectory(_dir);
        }

        public static FileSaveStore Default() => new FileSaveStore(Path.Combine(Application.persistentDataPath, "SortEverything"));

        public static string Hash(string payload)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string Serialize(SaveData s)
        {
            var payload = JsonUtility.ToJson(s);
            return JsonUtility.ToJson(new SaveEnvelope { revision = s.revision, payload = payload, sha256 = Hash(payload) });
        }

        public static SaveData TryParse(string text, out long revision)
        {
            revision = -1;
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var env = JsonUtility.FromJson<SaveEnvelope>(text);
                if (env == null || string.IsNullOrEmpty(env.payload) || Hash(env.payload) != env.sha256) return null;
                var s = JsonUtility.FromJson<SaveData>(env.payload);
                if (s == null || s.revision != env.revision) return null;
                revision = env.revision;
                return s;
            }
            catch { return null; }
        }

        public bool TryCommit(SaveData candidate, out string error)
        {
            lock (_lock)
            {
                error = null;
                try
                {
                    var text = Serialize(candidate);
                    if (FailAtStage == "pending") throw new IOException("injected pending failure");
                    using (var fs = new FileStream(PendingPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var bytes = Encoding.UTF8.GetBytes(text);
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Flush(true);
                    }
                    if (TryParse(File.ReadAllText(PendingPath), out _) == null) throw new IOException("pending verification failed");
                    if (FailAtStage == "promote") throw new IOException("injected promote failure");
                    Promote();
                    BrowserStorage.Flush();
                    return true;
                }
                catch (Exception e)
                {
                    error = e.GetType().Name + ": " + e.Message;
                    return false;
                }
            }
        }

        void Promote()
        {
            if (!File.Exists(MainPath)) { File.Move(PendingPath, MainPath); return; }
            try
            {
                File.Replace(PendingPath, MainPath, BackupPath, true);
            }
            catch (PlatformNotSupportedException) { RenameFallback(); }
            catch (NotSupportedException) { RenameFallback(); }
            catch (IOException) { if (File.Exists(PendingPath)) RenameFallback(); else throw; }
        }

        // Documented fallback: copy main to backup first, so a valid copy always exists, then move pending over main.
        void RenameFallback()
        {
            File.Copy(MainPath, BackupPath, true);
            File.Delete(MainPath);
            File.Move(PendingPath, MainPath);
        }

        public SaveLoadOutcome Load(out SaveData save, out string diagnostic)
        {
            lock (_lock)
            {
                diagnostic = null;
                save = null;
                bool anyExists = File.Exists(MainPath) || File.Exists(BackupPath) || File.Exists(PendingPath);
                if (!anyExists) return SaveLoadOutcome.NewSave;

                SaveData best = null; long bestRev = -1; string bestFile = null;
                foreach (var path in new[] { MainPath, PendingPath, BackupPath })
                {
                    if (!File.Exists(path)) continue;
                    string text;
                    try { text = File.ReadAllText(path); } catch { continue; }
                    var s = TryParse(text, out var rev);
                    if (s != null && s.schemaVersion == 4 && rev > bestRev) { best = s; bestRev = rev; bestFile = path; }
                }
                if (best == null)
                {
                    diagnostic = "no valid save file (bytes preserved)";
                    return SaveLoadOutcome.AllCorrupt;
                }
                save = best;
                if (bestFile == MainPath)
                {
                    if (File.Exists(PendingPath)) TryDelete(PendingPath); // stale or partial pending: ignore
                    return SaveLoadOutcome.Loaded;
                }
                // Newer valid pending, or recovery from backup: promote once.
                try
                {
                    if (bestFile == PendingPath) Promote();
                    else File.Copy(BackupPath, MainPath, true);
                }
                catch (Exception e) { diagnostic = "recovery write failed: " + e.Message; }
                return SaveLoadOutcome.Recovered;
            }
        }

        static void TryDelete(string p) { try { File.Delete(p); } catch { } }

        public void DeleteAllForDevelopment()
        {
            lock (_lock)
            {
                TryDelete(MainPath); TryDelete(BackupPath); TryDelete(PendingPath);
            }
        }
    }

    /// <summary>In-memory store with failure injection for deterministic tests.</summary>
    public sealed class MemorySaveStore : ISaveStore
    {
        public string Stored;
        public bool FailNext;
        public int Commits;

        public bool TryCommit(SaveData candidate, out string error)
        {
            if (FailNext) { FailNext = false; error = "injected failure"; return false; }
            Stored = FileSaveStore.Serialize(candidate);
            Commits++;
            error = null;
            return true;
        }

        public SaveLoadOutcome Load(out SaveData save, out string diagnostic)
        {
            diagnostic = null;
            save = null;
            if (Stored == null) return SaveLoadOutcome.NewSave;
            save = FileSaveStore.TryParse(Stored, out _);
            return save == null ? SaveLoadOutcome.AllCorrupt : SaveLoadOutcome.Loaded;
        }

        public void DeleteAllForDevelopment() => Stored = null;
    }
}
