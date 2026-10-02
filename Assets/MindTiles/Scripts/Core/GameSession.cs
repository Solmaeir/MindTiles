namespace MindTiles.Core
{
    // State that lives for one app run only: it survives scene changes
    // (menu <-> game) but is gone when the app is closed, so the current score
    // starts from zero on every launch. Anything that must outlive the app
    // belongs in SaveData instead.
    public static class GameSession
    {
        public static int Score;
    }
}
