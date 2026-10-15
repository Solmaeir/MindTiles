namespace MindTiles.Audio
{
    // Every sound effect the game can ask for. Code and UISound refer to sounds
    // by this id; which clip plays is set on the AudioManager prefab.
    public enum Sfx
    {
        None,

        // UI
        ButtonClick,
        PanelOpen,
        PanelClose,
        PlayButton,
        ToggleOn,
        ToggleOff,

        // Tiles
        TilePickUp,
        TileDrop,
        TileSwap,
        TileReturn,

        // Round
        CountdownTick,
        CountdownGo,
        TimerWarning,
        LevelPassed,
        LevelFailed,
        RunCompleted,

        // Hints
        HintUsed,
        HintUnavailable
    }
}
