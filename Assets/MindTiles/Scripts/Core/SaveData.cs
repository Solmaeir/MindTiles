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
        public const int CurrentVersion = 5;

        public int version = CurrentVersion;

        // Hints
        public bool hintsInitialized;   // false until the starting hints were granted once
        public int oneTileHints;
        public int allTilesHints;

        // Daily Streak progress
        public int highScore;           // best run score ever
        public int currentLevel;        // level to resume at; 0 = nothing saved yet

        // Days in a row with a finished Daily Streak run (the weekly bar on the menu)
        public DailyStreakSave dailyStreak = new DailyStreakSave();

        // Endless progress
        public EndlessSave endless = new EndlessSave();

        // Preferences
        public SettingsData settings = new SettingsData();

        // Repairs anything a hand-edited / old / partially written file could leave invalid.
        public void Sanitize()
        {
            if (settings == null) settings = new SettingsData();
            if (endless == null) endless = new EndlessSave();
            if (dailyStreak == null) dailyStreak = new DailyStreakSave();
            endless.Sanitize();
            dailyStreak.Sanitize();
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

    // Daily Streak chain: how many days in a row the player finished the Daily
    // run, and the (device-local) date of the last one. Read through DailyStreak.
    [Serializable]
    public class DailyStreakSave
    {
        public int count;
        public string lastDay = "";     // yyyy-MM-dd, local time; empty = never finished

        public void Sanitize()
        {
            count = Math.Max(0, count);
            if (lastDay == null) lastDay = "";
        }
    }

    // Endless mode state. Kept forever: total score and difficulty progress
    // survive app restarts.
    [Serializable]
    public class EndlessSave
    {
        public int level = 1;               // level the player is on (next to be played)
        public int totalScore;              // every point ever earned in Endless
        public int stage;                   // index into the difficulty stage table
        public int stageScore;              // points earned since entering the current stage
        public int levelsInStage;           // levels played in the current stage
        public float rating = 0.5f;         // Performance Rating: running average of level grades (0..1)

        public void Sanitize()
        {
            level = Math.Max(1, level);
            totalScore = Math.Max(0, totalScore);
            stage = Math.Max(0, stage);
            stageScore = Math.Max(0, stageScore);
            levelsInStage = Math.Max(0, levelsInStage);
            rating = Math.Min(1f, Math.Max(0f, rating));
        }
    }
}
