using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MindTiles.Grid;
using MindTiles.InputHandling;
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

        [SerializeField] private GridManager gridManager;
        [SerializeField] private CountdownPanel countdownPanel;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private RoundResultOverlay resultOverlay;

        [Header("Save Defaults")]
        [Tooltip("Hints granted once, when there is no save file yet. Changing these does not affect existing saves (use the context menu to reset).")]
        [SerializeField] private int startingOneTileHints = 3;
        [SerializeField] private int startingAllTilesHints = 3;

        [Header("Scoring")]
        [Tooltip("Points added for every correctly placed tile when a level is checked.")]
        [SerializeField] private int pointsPerCorrectTile = 10;

        [Header("Timing")]
        [SerializeField] private float baseMemorizeTime = 3f;
        [Tooltip("How long the correct/wrong reveal stays on screen before the end panel opens.")]
        [SerializeField] private float resultRevealSeconds = 1f;

        [SerializeField] private int currentLevel = 1;
        private const int MaxLevel = 10;
        private const int MenuSceneIndex = 0;

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
                currentLevel = Mathf.Clamp(saved, 1, MaxLevel - 1);
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

            gridManager.GenerateTiles(currentLevel);
            StartCoroutine(ScrambleAfterDelay(baseMemorizeTime));
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

            UIManager.Instance?.UpdateLevelText(currentLevel.ToString());

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
            RoundFinished?.Invoke();
            StartCoroutine(FinishRoundRoutine());
        }

        private IEnumerator FinishRoundRoutine()
        {
            PatternValidator validator = PatternValidator.Instance;
            int correct = validator.EvaluateRound();
            AddScore(correct * pointsPerCorrectTile);
            UIManager.Instance?.RefreshScoreTexts();

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

        public void CallLevel()
        {
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

            if (currentLevel < MaxLevel)
                currentLevel++;

            if (currentLevel == MaxLevel)
            {
                EndGame();
                yield break;
            }

            SaveLevel(currentLevel);
            gridManager.GenerateTiles(currentLevel);
            StartCoroutine(ScrambleAfterDelay(baseMemorizeTime + currentLevel));
        }

        private void EndGame()
        {
            RoundFinished?.Invoke();
            if (endPanel != null)
                endPanel.SetActive(true);
            Time.timeScale = 0f;

            // Game completed: the next run starts from level 1 again.
            SaveLevel(1);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void GoToMainMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            SaveSystem.Save();
            SceneManager.LoadScene(MenuSceneIndex);
        }
    }
}
