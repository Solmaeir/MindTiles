namespace MindTiles.Core
{
    // State that lives for one app run only: it survives scene changes
    // (menu <-> game) but is gone when the app is closed, so the current score
    // starts from zero on every launch. Anything that must outlive the app
    // belongs in SaveData instead.
    public static class GameSession
    {
        // Daily Streak score of this app run.
        public static int Score;

        // Chosen in the menu before the game scene loads. Defaults to Daily
        // Streak so starting the game scene directly in the editor still works.
        public static GameMode Mode = GameMode.DailyStreak;
    }
}
