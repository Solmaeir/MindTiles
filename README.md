# MindTiles

A mobile memory-puzzle game built with Unity. A pattern of tiles is shown for a few seconds, then the tiles are scrambled and the player has to put each one back where it was, by drag-and-drop or swipe.

- **Engine:** Unity 6 (6000.0.79f1), 2D, Universal Render Pipeline
- **Target:** Android (portrait only), IL2CPP / ARM64, 60 fps
- **Language:** C#, namespace `MindTiles.*`

---

## How the game plays

The app opens on a loading screen (tile progress bar), then the menu: logo, weekly streak bar, total score, and the two mode buttons. The "i" and gear buttons at the top right open the Info and Settings panels.

1. **Memorize.** The tiles are shown in their original cells while a countdown runs.
2. **Scramble.** All tiles are moved to random cells and the level timer starts (Endless mode).
3. **Rebuild.** The player moves tiles back by dragging them onto a cell, or by flicking them one cell in a direction. Moving onto an occupied cell swaps the two tiles.
4. **Check.** Pressing the check button (or the timer running out) scores the level and shows a reveal: green wash plus check mark on correct tiles, red wash plus an arrow to the right cell on wrong ones. The end panel opens one second later.

A level is **passed** when at least 66% of the tiles are correct, rounded up (4 of 6, 7 of 10, 10 of 15; `passAccuracy` on `GameManager`). A failed level gives no points and is played again (the end panel shows "TRY AGAIN" / "Retry").

Tiles of the same kind are interchangeable: a cell counts as correct if it holds *any* tile of the type that belongs there, because two identical-looking tiles can't be told apart by the player.


<img width="230" height="512" alt="image" src="https://github.com/user-attachments/assets/4759bd8f-3e0a-480d-956c-dac72bf43298" /> <img width="230" height="512" alt="image" src="https://github.com/user-attachments/assets/b7021520-50a0-4697-a260-382e955f7f57" /> <img width="230" height="512" alt="image" src="https://github.com/user-attachments/assets/19d01493-2598-4ef7-b6db-5167e80d4c4b" />



### Game modes

| Mode | What it is |
|---|---|
| **Daily Streak** | A fixed 10-level run with three difficulty tiers. Score is 10 points per correct tile. Progress (level, highest score) is saved. Passing level 10 ends the run: the end panel shows the total score and its button ("Main Menu") returns to the menu. |
| **Endless** | Adaptive difficulty over a 500-level ladder, with a per-level time limit, speed-weighted scoring and a total score that is never reset by closing the app. |

The mode is chosen on the menu (`GameSession.Mode`) before the game scene loads.

### Info panel

The menu's "i" button opens a two-tab panel (`InfoPanel`): **Info** (studio, version read from the Player Settings, contact e-mail, Instagram and X links) and **How to Play** (a scrollable list of steps). The contact e-mail and the two links are fields on the `InfoPanel` component of `Assets/MindTiles/UI/Prefabs/InfoPanel.prefab`; a link left empty is hidden. The How to Play text is plain text in the same prefab (`HowToPlayPage/Scroll/Viewport/Content`).

### Weekly streak

Finishing a Daily Streak run (passing level 10) fills one box of the 7-box bar on the menu (`StreakBar`). Finishing it again the same day changes nothing; missing a whole day empties the bar. After 7 days in a row the bar starts over. The streak (`DailyStreak`, `SaveData.dailyStreak`) is stored in the local save file with the device's local date, so changing the phone's clock changes it, and "Reset progress" clears it.

### Settings

One shared Settings panel prefab is used from the menu and from the in-game pause panel: Sound and Music switches (applied to `AudioManager` immediately), Vibration (saved, not used yet) and "Reset progress" behind a confirmation dialog.

### Hints

Two hint types, 3 of each at first install. Counts are shown in a red badge on each button and are persisted.

- **One-tile hint:** marks one misplaced tile and the cell it belongs in. The marks stay until the tile (or a twin) is placed correctly, then clear and the hint can be used again.
- **All-tiles hint:** marks every tile that is already correct for a short, timed window with an on-screen countdown. Play is *not* locked while it shows and the marks follow the player's moves.

---

## Difficulty systems

### Daily Streak (fixed tiers)

| Levels | Tiles | Tile kinds | Notes |
|---|---|---|---|
| 1-3 | 6 | 1 prefab (all identical) | position-only memory |
| 4-7 | 10 | 3 prefabs | pattern + position |
| 8-10 | 15 | 5 prefabs | pattern + position, smaller cells |

Memorize time is 3 s on level 1, then `3 + level` seconds. The grid size is not hard-coded; it is solved from the free screen area (see *Responsive layout*).

### Endless (adaptive)

`EndlessDifficulty` is a director that decides the next level's board after every checked level. There are **no fixed level gates**: how fast a player climbs depends only on how well they play.

Per level:

- **Level Points** = `tiles x 10 x accuracy x speed multiplier` (multiplier 1.0 when the timer runs out up to 2.0 when checked instantly; 0 points if nothing is correct).
- **Level Performance** (0-1) = accuracy, with 30% of the grade depending on speed.
- **Performance Rating** = exponential running average of Level Performance. This is the player's "absolute" skill rating.
- **Stage Score** = points earned since entering the current stage.

A stage is a row of a designer-editable table (grid size, easy/mid/hard tile counts, memorize time, level time limit, targets). Nine stages from Easy I (4x4, 4 easy tiles) to Hard III (6x6, 15 hard tiles).

- **Promotion:** Stage Score >= stage target **and** Performance Rating >= stage target.
- **Demotion (soft):** rating below a threshold after a minimum number of levels in the stage; the lower stage is entered with half of its score target already credited.
- **Memorize ramp:** inside a stage the memorize time shrinks slowly (down to 75%) the longer the player stays.
- **Progression cap:** after level 500 the stage is frozen and only the total score keeps growing.

All thresholds are exposed in the Inspector on the `EndlessDifficulty` component.

---

## Architecture

```
Assets/MindTiles/Scripts
├── Boot         BootLoader (loading screen)
├── Core         GameManager, EndlessDifficulty, DailyStreak, HintManager, SaveSystem/SaveData, GameSession, FrameRateSetup
├── Grid         GridManager, TileController, GridLinesRenderer, RoundResultOverlay, MarkStyle, LevelSpec
├── Audio        AudioManager, UISound, Sfx
├── Input        DragController
├── Validation   PatternValidator
├── Score        ScoreCalculator
└── UI           ButtonManager, UIManager, MenuManager, MenuExitTransition, BoardFitter, SafeArea,
                 FillToScreenEdges, ConfirmDialog, SettingsPanel, PausePanelView, ToggleSwitch, InfoPanel,
                 StreakBar, ScorePillFitter, CountdownPanel + small visual effects

Assets/MindTiles/Editor   MindTilesUIBuilder (Tools > MindTiles menu, see "UI builder")
Assets/MindTiles/UI       Generated UI sprites and the shared panel prefabs
Tools/                    Python scripts that generate the UI sprites and social icons
```

Three scenes, in build order: `Loading` (0), `MenuSpace` (1, the menu) and `SampleScene` (2, the game).

### Responsibilities

| Class | Role |
|---|---|
| `GameManager` | Round flow (build level -> memorize -> scramble -> play -> check -> next), pause, mode switch, scoring, hint inventory API, save lifecycle. Single entry point for game-wide state. |
| `GridManager` | Grid geometry (cell size, world<->grid conversion), occupancy map, tile spawning, tile layout. Two entry points: `GenerateTiles(int level)` for Daily Streak and `GenerateTiles(LevelSpec)` for Endless. |
| `PatternValidator` | Knows the expected tile **type** per cell, answers "is this cell correct" in O(1), builds the reveal marks. |
| `DragController` | One gesture handles drag-and-drop and swipe. |
| `HintManager` | Hint mechanics. `ButtonManager` only forwards presses to it. |
| `EndlessDifficulty` | Endless stage ladder, rating, scoring. |
| `SaveSystem` / `SaveData` | Local persistence. |
| `ButtonManager` | Thin input layer: binds buttons in code (`OnEnable`/`OnDisable`), contains no game rules. |
| `UIManager` | Text readouts only; two header layouts (one per mode). |
| `AudioManager` | All sound. Spawned from `Resources/AudioManager` before the first scene, survives scene loads, owns the only `AudioListener`. Music per scene with fades; SFX via `AudioManager.Play(Sfx.X)`; volumes follow the settings switches. |
| `UISound` | Put on a `Button` to play a click / panel open / panel close sound. |
| `DailyStreak` | Weekly streak rules (days in a row, boxes to fill, recording a finished run). |
| `BootLoader` | Loading screen: loads the menu in the background and drives the progress bar. |
| `SettingsPanel` / `InfoPanel` | The shared settings panel and the two-tab info panel. |

### Technical notes

**Responsive layout - the board is the single source of truth.**
`BoardFitter` computes the free area on screen (inside the safe area, below the header, above the end panel). `GridManager` solves the best columns x rows for it (trying rounding each axis down/up and keeping the square-cell grid that covers the most area), then tells the fitter, which resizes the `GameBoard` to exactly that grid plus inner padding. From then on, cell size, tile scale and position, grid lines and result marks are all derived from the board's world rect in one function (`ApplyLayout`). A cheap per-frame check (four floats) re-runs the layout when the board changes (rotation, safe area edit, resolution change). Grid dimensions stay fixed for a level, so game state is never touched by a re-layout.

**Startup race.** The first frames after Play can run at a different screen size (Device Simulator) with the canvas not scaled yet. `GameManager` waits until the screen size is stable for a few frames before building the first level.

**Safe area.** `SafeArea` fits a panel to `Screen.safeArea`, zeroes leftover offsets, re-applies only when the screen or safe area changes, and exposes per-edge inset scale and extra padding in the Inspector. `FillToScreenEdges` lets the end panel extend to the real screen edges while its top edge stays where the layout puts it.

**Save system.** JSON in `Application.persistentDataPath`. Writes go to a temp file first with the previous file kept as `.bak`; loading falls back to temp/backup if the main file is missing or corrupt. Saved on hint use, after scoring, on level change, and on `OnApplicationPause` / `OnApplicationQuit` (the reliable moment on mobile). Stored (save version 5): hint counts, Daily Streak high score and level, the weekly streak (days in a row and the last day), Endless level/total score/stage/rating, preferences (sound, music, vibration). The current Daily Streak run score is intentionally *not* persisted (`GameSession`, static, lives for one app run).

**Rendering of marks.** `RoundResultOverlay` draws washes, check marks and arrows from a small pool of `SpriteRenderer`s (no per-show allocations). The check, arrow head and capsule line are generated procedurally into mip-mapped textures (smooth edges, no art files); the arrow line is a 9-sliced capsule so its round caps never stretch. All colors and sizes live in a shared `MarkStyle` ScriptableObject (`Assets/MindTiles/Settings/MarkStyle.asset`) and update live in Play. Grid lines are a single dynamic mesh (one draw call).

**Input.** Unity Input System (`Pointer.current`, works for mouse and touch). One raycast per press; while dragging the per-frame cost is a clamp and a transform write. Settling animations re-read target positions every frame so a layout change mid-animation can't leave a tile at a stale position. Pausing or locking mid-drag puts the tile back.

**Loading screen.** `BootLoader` starts loading the menu with `LoadSceneAsync` right away and fills the tile bar with the smaller of the real load progress and a minimum display time, so it never just flashes.

**Menu transition.** `MenuExitTransition` slides the menu elements (logo, buttons, streak bar, score pill) off-screen at a shared speed (distance precomputed once, then `start + dir * travelled` per frame, zero cost while idle), shows a loading text and starts `LoadSceneAsync` immediately with `allowSceneActivation = false`, releasing it once the animation and a minimum display time are done and loading has reached 90%.

**Audio.** `AudioManager` is a prefab in `Resources`, spawned before the first scene loads, so sound works whichever scene Play starts in. It owns the only `AudioListener` (cameras have none). Music is picked per scene name and cross-fades; it plays quieter while paused. UI clicks use one source, tile sounds a second one with a small random pitch, and the memorize countdown a third one whose pitch stretches the countdown clip to exactly 3 seconds (`Countdown Fit Seconds`).

**Events.** Cross-system notifications (`RoundFinished`, `RoundStarted`, `RoundResult(passed)`, `RunCompleted`, `HintCountsChanged`, `GridManager.LayoutChanged`, `SettingsPanel.ProgressReset`) are C# events, subscribed in `OnEnable`/`OnDisable` or `Awake`/`OnDestroy` so subscribers don't depend on initialization order.

**Other.** Score/performance formulas, including the pass threshold, are pure static functions (`ScoreCalculator`). The menu score pill (`ScorePillFitter`) widens with the score instead of shrinking the text. A reusable `ConfirmDialog` backs the "Reset progress" flow (pause menu and menu settings). Frame rate is set to 60 before the first scene loads.

---

## Tools & packages

- Unity 6 (6000.0.79f1), Universal Render Pipeline 17.0.4, 2D feature set
- Unity **Input System** 1.19.0 (active input handling set to the new system only)
- **uGUI** 2.0.0 and **TextMesh Pro** for UI and text
- Unity Test Framework, Timeline, Visual Scripting (installed via the default 2D template; not used by game code)
- IDE integrations: Visual Studio and Rider packages
- AI-assisted development with Claude Code and the Unity AI Assistant / MCP bridge

## Assets

- **[Space Game GUI Kit](https://assetstore.unity.com/packages/2d/gui/space-game-gui-kit-298577)** by danprops - buttons, containers/frames, backgrounds, clouds, moon/sun/star decorations and icons used across the menu and in-game UI. Lives in `Assets/Space_Exploration_GUI_Kit`; see the README inside that folder for its terms.
- **TextMesh Pro** essentials. UI fonts: **Fredoka Bold** (titles, buttons) and **Nunito ExtraBold** (labels), both SIL Open Font License (license files next to the TTFs in `Assets/MindTiles/Fonts/Font Sources/static`), as TMP SDF assets in `Assets/MindTiles/Fonts`. The older **Asap Bold** and **Righteous** SDF assets are still in that folder.
- **Casual Game Sounds U6** (`Assets/Casual Game Sounds U6`) - sound effects; see `license.pdf` inside the package.
- Music and sound clips in `Assets/MindTiles/Sounds` (including *Cosmic-Puzzle*, the menu music): _add the source / license of each clip here_.
- UI sprites (buttons, tiles, slots, logo) are generated by `Tools/generate_ui_sprites.py`; the Instagram and X icons on the Info panel are simple drawn glyphs from `Tools/generate_social_icons.py`, not the official brand artwork.
- Tile artwork (`Assets/MindTiles/Prefabs`): _add the source / license of the tile sprites here_.

---

## Building

1. Open the project in Unity 6000.0.79f1.
2. `File > Build Profiles`, switch to **Android**.
3. Check *Build App Bundle (Google Play)* for an `.aab` (store upload), leave it unchecked for an `.apk` (direct install on a device). Scenes in the build: `Loading`, `MenuSpace`, then `SampleScene` (the Unity splash screen is turned off; `Loading` is the first screen).
4. Android SDK, NDK, OpenJDK and Gradle come from the Unity Hub Android module.

Before a store release: check the package name in *Player Settings > Other Settings* (company is `PakueGames`, product `Mind Tiles`), set the app icon and a release keystore. The version shown on the Info panel is the Player Settings *Version*.

## Tuning without touching code

| What | Where |
|---|---|
| Endless stages, targets, scoring weights, rating rules | `EndlessDifficulty` on the `GameManager` object |
| Starting hints, points per tile, reveal duration | `GameManager` |
| Mark colors, opacity, check/arrow size | `Assets/MindTiles/Settings/MarkStyle.asset` |
| Board spacing, board padding, header gaps | `BoardFitter` on `GameBoard` |
| Safe-area insets and padding | `SafeArea` components |
| Hint duration, menu transition speed | `HintManager`, `MenuExitTransition` |
| Music and sound clips, per-clip volume, fade time | `Assets/MindTiles/Resources/AudioManager.prefab` |
| Pass threshold | `passAccuracy` on `GameManager` |
| Countdown sound length, paused music volume | `AudioManager.prefab` (`Countdown Fit Seconds`, `Paused Music Volume`) |
| Contact e-mail, Instagram and X links | `InfoPanel` component on `Assets/MindTiles/UI/Prefabs/InfoPanel.prefab` |
| How to Play text | Text objects inside the same prefab |
| End panel titles and button labels | `UIManager` (End Panel section) |

### UI builder

`Tools > MindTiles` rebuilds the redesigned UI from code: 1 import sprites, 2 create fonts, 3 build panel prefabs, 4 build the loading scene, 5 rebuild the menu, 6 rebuild game panels, 7 restyle the game HUD, 8 add the streak bar, 9 build the info panel, 10 update the info panel and score pill in place. Steps 3 and 9 recreate the panel prefabs from scratch, so hand edits made to them (texts, links) are lost; step 10 only patches the existing Info panel and menu.

Test helpers (right-click the component header): `GameManager > Save > Reset Save Data`, `Add 3 Hints Of Each Type`; `EndlessDifficulty > Endless > Reset Endless Progress`.

## Known limitations

- Daily Streak can be played any number of times a day; only the first finished run of a day counts for the weekly streak.
- Vibration is stored in the save file but not used yet.
- The Instagram and X buttons open the sites' home pages until real profile links are entered on the `InfoPanel` prefab.
- The weekly streak uses the device's local date, so changing the phone's clock changes it.
- The game scene opened directly in the editor always starts in Daily Streak; start from the menu to test Endless.
