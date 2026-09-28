using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MindTiles.Grid;
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

        [SerializeField] private GridManager gridManager;
        [SerializeField] private CountdownPanel countdownPanel;
        [SerializeField] private GameObject endPanel;

        [Header("Timing")]
        [SerializeField] private float baseMemorizeTime = 3f;

        [SerializeField] private int currentLevel = 1;
        private const int MaxLevel = 10;

        public bool inputLocked = true;

        private readonly List<GameObject> placedTileObjects = new List<GameObject>();
        private readonly List<TileController> placedTiles = new List<TileController>();

        public IReadOnlyList<GameObject> PlacedTiles => placedTileObjects;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
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

            float roundTimeLimit = memorizeSeconds + currentLevel * 5f;
            PatternValidator.Instance?.BeginRound(new List<TileController>(placedTiles), roundTimeLimit);
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

            foreach (var tileObj in placedTileObjects)
                tileObj.GetComponent<TileHighlight>()?.SetNeutral();
        }

        public void CallLevel()
        {
            StartCoroutine(CallLevelRoutine());
        }

        private IEnumerator CallLevelRoutine()
        {
            inputLocked = true;
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

            gridManager.GenerateTiles(currentLevel);
            StartCoroutine(ScrambleAfterDelay(baseMemorizeTime + currentLevel));
        }

        private void EndGame()
        {
            if (endPanel != null)
                endPanel.SetActive(true);
            Time.timeScale = 0f;

            Score.ScoreManager.Instance?.EndSession();
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            Score.ScoreManager.Instance?.EndSession();
            SceneManager.LoadScene("Menu");
        }
    }
}
