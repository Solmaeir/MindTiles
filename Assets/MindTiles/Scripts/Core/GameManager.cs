using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MindTiles.Audio;
using MindTiles.Grid;
using MindTiles.InputHandling;
using MindTiles.Score;
using MindTiles.UI;
using MindTiles.Validation;

namespace MindTiles.Core
{
    // Drives the round loop: generate tiles -> memorize -> scramble -> player
    // rearranges -> validate -> next level. Difficulty scaling itself lives
    // in GridManager; this class just sequences the phases.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // UI listens to these (static so subscribers don't depend on Awake order).
        // RoundFinished: check pressed / game ended. RoundStarted: moving on to the next level.
        public static event System.Action RoundFinished;
        public static event System.Action RoundStarted;
        public static event System.Action HintCountsChanged;
        // RoundResult: a level was scored; true = enough tiles correct (AudioManager plays pass/fail).
        // RunCompleted: the last Daily Streak level was passed (sent instead of RoundResult).
        public static event System.Action<bool> RoundResult;
        public static event System.Action RunCompleted;

        [SerializeField] private GridManager gridManager;
        [SerializeField] private CountdownPanel countdownPanel;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private RoundResultOverlay resultOverlay;
        [Tooltip("Difficulty director for Endless mode.")]
        [SerializeField] private EndlessDifficulty endless;

        [Header("Save Defaults")]
        [Tooltip("Hints granted once, when there is no save file yet. Changing these does not affect existing saves (use the context menu to reset).")]
        [SerializeField] private int startingOneTileHints = 3;
        [SerializeField] private int startingAllTilesHints = 3;

        [Header("Scoring")]
        [Tooltip("Points added for every correctly placed tile when a level is checked.")]
        [SerializeField] private int pointsPerCorrectTile = 10;
        [Tooltip("Share of tiles that must be correct to pass a level (rounded up: 0.66 -> 4 of 6, 7 of 10, 10 of 15). Below it the level gives no points and is replayed.")]
        [SerializeField, Range(0.1f, 1f)] private float passAccuracy = 0.66f;

        [Header("Timing")]
        [SerializeField] private float baseMemorizeTime = 3f;
        [Tooltip("How long the correct/wrong reveal stays on screen before the end panel opens.")]
        [SerializeField] private float resultRevealSeconds = 1f;

        [SerializeField] private int currentLevel = 1;
        private const int MaxLevel = 10;
        private const string MenuSceneName = "MenuSpace";

        public bool inputLocked = true;
        public bool IsPaused { get; private set; }
        public bool CanAcceptInput => !inputLocked && !IsPaused;

        private readonly List<GameObject> placedTileObjects = new List<GameObject>();
        private readonly List<TileController> placedTiles = new List<TileController>();
        private readonly List<CellMark> resultMarks = new List<CellMark>();

        public IReadOnlyList<GameObject> PlacedTiles => placedTileObjects;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                GrantStartingHintsOnce();
                if (Mode == GameMode.Endless && endless == null)
                    Debug.LogError("[GameManager] Endless mode was chosen but no EndlessDifficulty is assigned; running Daily Streak instead.");
                if (!IsEndless)
                    RestoreSavedLevel();
            }
            else
                Destroy(gameObject);
        }

        // ---- Save data -------------------------------------------------------
        // The save file itself is handled by SaveSystem; GameManager is the
        // game-facing entry point (hint inventory, preferences, lifecycle saves).

        public SettingsData Settings => SaveSystem.Data.settings;

        // Score of this app run (restarts every launch) and the best run ever (saved).
        public int SessionScore => GameSession.Score;
        public int HighScore => SaveSystem.Data.highScore;
        public int CurrentLevel => currentLevel;

        // ---- Game mode ---------------------------------------------------------
        // Picked in the menu (GameSession.Mode). Daily Streak = fixed 10-level run;
        // Endless = adaptive difficulty driven by EndlessDifficulty.
        public GameMode Mode => GameSession.Mode;
        public bool IsEndless => Mode == GameMode.Endless && endless != null;
        public int DisplayLevel => IsEndless ? endless.Level : currentLevel;
        public int EndlessTotalScore => IsEndless ? endless.TotalScore : 0;
        // Points earned by the last checked level (shown on the end panel).
        public int LastLevelPoints { get; private set; }
        // Whether the last checked level had enough correct tiles. A failed level is replayed.
        public bool LastRoundPassed { get; private set; } = true;
        // The last Daily Streak level was just passed: the end panel shows the run's
        // total score and its button leads back to the menu.
        public bool RunComplete { get; private set; }

        // ---- Level timer (Endless) --------------------------------------------
        // Starts when the tiles are scrambled; when it hits zero the level is
        // checked automatically. Pausing freezes it (scaled time).
        private bool timerRunning;
        private float timeLimit;
        private float timeRemaining;

        private void Update()
        {
            if (!timerRunning) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                timerRunning = false;
                UIManager.Instance?.UpdateTimer(timeRemaining);
                FinishRound();
                return;
            }
            UIManager.Instance?.UpdateTimer(timeRemaining);
        }

        private void AddScore(int amount)
        {
            GameSession.Score += amount;

            SaveData data = SaveSystem.Data;
            if (GameSession.Score > data.highScore)
                data.highScore = GameSession.Score;

            SaveSystem.Save();
        }

        // Resume where the player left off. A fresh save keeps the Inspector's starting level.
        private void RestoreSavedLevel()
        {
            int saved = SaveSystem.Data.currentLevel;
            if (saved > 0)
                currentLevel = Mathf.Clamp(saved, 1, MaxLevel);
        }

        private void SaveLevel(int level)
        {
            SaveSystem.Data.currentLevel = level;
            SaveSystem.Save();
        }

        public int GetHintCount(HintType type)
        {
            SaveData data = SaveSystem.Data;
            return type == HintType.OneTile ? data.oneTileHints : data.allTilesHints;
        }

        // Spends one hint of this type if the player has any. Saved immediately.
        public bool TryConsumeHint(HintType type)
        {
            if (GetHintCount(type) <= 0) return false;
            AddHints(type, -1);
            return true;
        }

        public void AddHints(HintType type, int amount)
        {
            SaveData data = SaveSystem.Data;
            if (type == HintType.OneTile)
                data.oneTileHints = Mathf.Max(0, data.oneTileHints + amount);
            else
                data.allTilesHints = Mathf.Max(0, data.allTilesHints + amount);

            SaveSystem.Save();
            HintCountsChanged?.Invoke();
        }

        public void SaveGame()
        {
            SaveSystem.Save();
        }

        private void GrantStartingHintsOnce()
        {
            SaveData data = SaveSystem.Data;
            if (data.hintsInitialized) return;

            data.oneTileHints = startingOneTileHints;
            data.allTilesHints = startingAllTilesHints;
            data.hintsInitialized = true;
            SaveSystem.Save();
        }

        [ContextMenu("Save/Reset Save Data (deletes progress)")]
        private void ResetSaveData()
        {
            SaveSystem.Reset();
            GameSession.Score = 0;
            GrantStartingHintsOnce();
            HintCountsChanged?.Invoke();
        }

        [ContextMenu("Save/Add 3 Hints Of Each Type")]
        private void AddTestHints()
        {
            AddHints(HintType.OneTile, 3);
            AddHints(HintType.AllTiles, 3);
        }

        // Mobile OSes can kill a backgrounded app without any further callback,
        // so pause is the reliable moment to flush to disk.
        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save();
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save();
        }

        private void OnEnable()
        {
            gridManager.LayoutChanged += OnBoardLayoutChanged;
        }

        private void OnDisable()
        {
            gridManager.LayoutChanged -= OnBoardLayoutChanged;
        }

        // The board moved/resized (rotation, safe area edit...) and GridManager
        // already re-fitted tiles and lines (overlays refresh themselves). Game
        // state is untouched; just drop an in-progress drag.
        private void OnBoardLayoutChanged()
        {
            DragController.Instance?.CancelSelection();
        }

        private void Start()
        {
            if (endPanel != null)
                endPanel.SetActive(false);

            UIManager.Instance?.ConfigureForMode(IsEndless);
            StartCoroutine(StartWhenLayoutSettled());
        }

        // The first frames after Play can run at a different screen size (device
        // simulator, window resize) with the canvas not scaled yet. The grid is
        // chosen from the board's size, so building it then gives a wrong grid.
        // Wait until the screen size has stayed the same for a few frames.
        private IEnumerator StartWhenLayoutSettled()
        {
            const int StableFramesNeeded = 3;
            const float MaxWaitSeconds = 1f;

            var last = new Vector2Int(Screen.width, Screen.height);
            var first = last;
            int stable = 0;
            float waited = 0f;

            while (stable < StableFramesNeeded && waited < MaxWaitSeconds)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;

                var now = new Vector2Int(Screen.width, Screen.height);
                if (now == last) stable++;
                else { stable = 0; last = now; }
            }

            if (last != first)
                Debug.Log($"[GameManager] Screen changed {first} -> {last} while starting; level built after it settled.");

            Canvas.ForceUpdateCanvases();
            StartLevel();
        }

        // Builds the current level for the active mode and starts the memorize countdown.
        private void StartLevel()
        {
            float memorizeSeconds;
            if (IsEndless)
            {
                LevelSpec spec = endless.CurrentSpec();
                gridManager.GenerateTiles(spec);
                memorizeSeconds = spec.memorizeSeconds;
                timeLimit = spec.timeLimitSeconds;
                timeRemaining = timeLimit;
                UIManager.Instance?.UpdateTimer(timeRemaining);
            }
            else
            {
                gridManager.GenerateTiles(currentLevel);
                memorizeSeconds = DailyMemorizeTime(currentLevel);
            }

            UIManager.Instance?.UpdateLevelText(DisplayLevel.ToString());
            UIManager.Instance?.RefreshScoreTexts();
            StartCoroutine(ScrambleAfterDelay(memorizeSeconds));
        }

        // Daily Streak: 3 s on level 1, then base + level number.
        private float DailyMemorizeTime(int level)
        {
            return level <= 1 ? baseMemorizeTime : baseMemorizeTime + level;
        }

        public void RegisterTile(GameObject tileObj, TileController tile)
        {
            placedTileObjects.Add(tileObj);
            placedTiles.Add(tile);
        }

        public void ClearTiles()
        {
            foreach (var tileObj in placedTileObjects)
                Destroy(tileObj);

            placedTileObjects.Clear();
            placedTiles.Clear();
            gridManager.ClearOccupancy();
        }

        private IEnumerator ScrambleAfterDelay(float memorizeSeconds)
        {
            yield return StartCoroutine(countdownPanel.ShowCountdown(memorizeSeconds));

            ScrambleTiles();
            inputLocked = false;
            timerRunning = IsEndless;

            PatternValidator.Instance?.BeginRound(new List<TileController>(placedTiles));
        }

        private void ScrambleTiles()
        {
            List<Vector2Int> availablePositions = gridManager.GetAllGridPositions();

            foreach (var tile in placedTiles)
            {
                if (availablePositions.Count == 0) break;

                int index = Random.Range(0, availablePositions.Count);
                Vector2Int newGridPos = availablePositions[index];
                availablePositions.RemoveAt(index);

                gridManager.MoveTile(tile, newGridPos);
            }
        }

        // Check pressed: score the round, show the correct/wrong reveal for a
        // moment, then open the end panel.
        public void FinishRound()
        {
            if (!CanAcceptInput || PatternValidator.Instance == null) return;

            inputLocked = true;
            timerRunning = false;
            RoundFinished?.Invoke();
            StartCoroutine(FinishRoundRoutine());
        }

        private IEnumerator FinishRoundRoutine()
        {
            PatternValidator validator = PatternValidator.Instance;
            int correct = validator.EvaluateRound();
            int total = validator.TotalTiles;
            bool passed = ScoreCalculator.IsPassed(correct, total, passAccuracy);
            LastRoundPassed = passed;

            if (IsEndless)
            {
                LastLevelPoints = endless.RecordLevel(correct, total,
                    timeLimit - timeRemaining, timeLimit, pointsPerCorrectTile, passed);
            }
            else
            {
                LastLevelPoints = passed ? correct * pointsPerCorrectTile : 0;
                if (passed) AddScore(LastLevelPoints);
            }

            // Passing the last level finishes the run and counts for today's streak.
            RunComplete = !IsEndless && passed && currentLevel >= MaxLevel;
            if (RunComplete)
            {
                DailyStreak.RecordCompletion();
                SaveLevel(1);       // the next run starts from level 1 again
            }

            UIManager.Instance?.RefreshScoreTexts();
            UIManager.Instance?.ShowRoundResult(passed, RunComplete);
            if (RunComplete) RunCompleted?.Invoke();
            else RoundResult?.Invoke(passed);

            if (resultOverlay != null)
            {
                validator.CollectMarks(resultMarks);
                resultOverlay.Show(resultMarks);
            }

            yield return new WaitForSecondsRealtime(resultRevealSeconds);

            // The reveal stays visible behind the panel; it is cleared when the next level starts.
            if (endPanel != null)
                endPanel.SetActive(true);
        }

        // End panel button: next level, retry, or (after the last level) back to the menu.
        public void CallLevel()
        {
            if (RunComplete)
            {
                GoToMainMenu();
                return;
            }
            StartCoroutine(CallLevelRoutine());
        }

        private IEnumerator CallLevelRoutine()
        {
            inputLocked = true;
            RoundStarted?.Invoke();
            if (resultOverlay != null)
                resultOverlay.Clear();
            ClearTiles();

            if (endPanel != null)
                endPanel.SetActive(false);

            yield return null;

            // Endless already advanced its level (and difficulty) when the result was recorded;
            // a failed Daily Streak level is played again.
            if (IsEndless || !LastRoundPassed)
            {
                StartLevel();
                yield break;
            }

            if (currentLevel < MaxLevel)
                currentLevel++;

            SaveLevel(currentLevel);
            StartLevel();
        }

        // Pause-menu "Reset progress" (after the player confirmed): wipe scores and
        // levels, then reload the game scene so the run starts clean in the same mode.
        public void ResetProgressAndRestart()
        {
            SaveSystem.ResetProgress();
            IsPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioManager.SetGamePaused(paused);
        }

        public void GoToMainMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            // A finished run is over: the next one starts with a fresh run score.
            if (RunComplete) GameSession.Score = 0;
            SaveSystem.Save();
            SceneManager.LoadScene(MenuSceneName);
        }
    }
}
