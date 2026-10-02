using System;
using System.IO;
using UnityEngine;

namespace MindTiles.Core
{
    // Local save file (JSON in Application.persistentDataPath, which survives
    // app restarts on Android and iOS). Loaded lazily on first access.
    //
    // Writes are crash-safe: the new data goes to a temp file first, the old
    // file is kept as a backup, and loading falls back to the temp/backup file
    // if the main one is missing or corrupt, so a kill mid-write can't wipe progress.
    public static class SaveSystem
    {
        private const string FileName = "mindtiles_save.json";

        private static SaveData data;

        private static string MainPath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => MainPath + ".tmp";
        private static string BackupPath => MainPath + ".bak";

        public static SaveData Data
        {
            get
            {
                if (data == null) data = Load();
                return data;
            }
        }

        public static void Save()
        {
            if (data == null) return;   // nothing was loaded or changed

            try
            {
                data.version = SaveData.CurrentVersion;
                File.WriteAllText(TempPath, JsonUtility.ToJson(data));

                if (File.Exists(MainPath))
                {
                    File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                }
                File.Move(TempPath, MainPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Could not write save file: {e.Message}");
            }
        }

        // Deletes the save and starts over with defaults (testing / "reset progress").
        public static void Reset()
        {
            data = new SaveData();
            foreach (string path in new[] { MainPath, TempPath, BackupPath })
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (Exception e) { Debug.LogWarning($"[SaveSystem] Could not delete {path}: {e.Message}"); }
            }
        }

        private static SaveData Load()
        {
            foreach (string path in new[] { MainPath, TempPath, BackupPath })
            {
                SaveData loaded = TryRead(path);
                if (loaded == null) continue;

                loaded.Sanitize();
                return loaded;
            }
            return new SaveData();
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Could not read {path}: {e.Message}");
                return null;
            }
        }
    }
}
