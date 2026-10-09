using System;
using System.IO;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Crash-safe save file: writes to a temp file, keeps the previous good file as a backup, then swaps. Loading
    /// falls back to the backup when the main file is missing or unreadable. A file from a newer game version is
    /// never overwritten (ReadOnly).
    /// </summary>
    public sealed class SaveFile
    {
        public readonly string Path;
        string Backup { get { return Path + ".bak"; } }
        string Temp { get { return Path + ".tmp"; } }

        /// <summary>True when the stored save could not be used safely; saving is then disabled.</summary>
        public bool ReadOnly { get; private set; }
        public string LoadNote { get; private set; }

        public SaveFile(string path) { Path = path; }

        public bool Exists { get { return File.Exists(Path) || File.Exists(Backup); } }

        public SaveData Load()
        {
            ReadOnly = false;
            LoadNote = null;
            string newer = null;
            foreach (var p in new[] { Path, Backup })
            {
                if (!File.Exists(p)) continue;
                try
                {
                    var data = SaveData.FromJson(File.ReadAllText(p));
                    if (p == Backup) LoadNote = "restored from backup";
                    return data;
                }
                catch (FormatException e)
                {
                    if (e.Message.Contains("newer")) newer = e.Message;
                    LoadNote = (LoadNote != null ? LoadNote + "; " : "") + System.IO.Path.GetFileName(p) + ": " + e.Message;
                }
                catch (IOException e)
                {
                    LoadNote = (LoadNote != null ? LoadNote + "; " : "") + e.Message;
                }
            }
            if (newer != null) ReadOnly = true; // keep the newer file intact; play without saving
            return null;
        }

        public bool Save(SaveData data)
        {
            if (ReadOnly || data == null) return false;
            try
            {
                string dir = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Temp, data.ToJson());
                if (File.Exists(Path))
                {
                    if (File.Exists(Backup)) File.Delete(Backup);
                    File.Move(Path, Backup);
                }
                File.Move(Temp, Path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
