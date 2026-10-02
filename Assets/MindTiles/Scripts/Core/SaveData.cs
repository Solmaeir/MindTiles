using System;

namespace MindTiles.Core
{
    // Everything that is persisted between app launches. Plain serializable
    // data only (JsonUtility); logic lives in SaveSystem / GameManager.
    // Bump `version` and handle it in SaveSystem when the shape changes.
    // (The current run's score is deliberately NOT stored: it restarts every launch.)
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

        // Hints
        public bool hintsInitialized;   // false until the starting hints were granted once
        public int oneTileHints;
        public int allTilesHints;

        // Progress
        public int highScore;           // best run score ever
        public int currentLevel;        // level to resume at; 0 = nothing saved yet

        // Preferences
        public SettingsData settings = new SettingsData();

        // Repairs anything a hand-edited / old / partially written file could leave invalid.
        public void Sanitize()
        {
            if (settings == null) settings = new SettingsData();
            oneTileHints = Math.Max(0, oneTileHints);
            allTilesHints = Math.Max(0, allTilesHints);
            highScore = Math.Max(0, highScore);
            currentLevel = Math.Max(0, currentLevel);
            settings.musicVolume = Math.Min(1f, Math.Max(0f, settings.musicVolume));
            settings.sfxVolume = Math.Min(1f, Math.Max(0f, settings.sfxVolume));
        }
    }

    [Serializable]
    public class SettingsData
    {
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool vibration = true;
    }
}
