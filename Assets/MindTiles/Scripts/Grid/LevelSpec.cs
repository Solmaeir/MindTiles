namespace MindTiles.Grid
{
    // Everything needed to build one Endless level: grid shape, how many tiles
    // of each visual tier, how long the player gets to memorize them and how
    // long they have to rebuild the pattern.
    public struct LevelSpec
    {
        public int columns;
        public int rows;
        public int easyTiles;
        public int midTiles;
        public int hardTiles;
        public float memorizeSeconds;
        public float timeLimitSeconds;
    }
}
