using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MindTiles.Core;
using MindTiles.Grid;
using MindTiles.InputHandling;
using MindTiles.Score;

namespace MindTiles.Validation
{
    // Validates by TYPE-at-position, not by tile instance: two tiles spawned
    // from the same prefab look identical to the player, so either one
    // sitting in a given cell should count as correct for that cell.
    //
    // expectedTypeByPosition is built once per round (O(n)); every check
    // afterwards is an O(1) dictionary lookup against GridManager's
    // occupancy map, so this stays cheap even as the grid grows.
    public class PatternValidator : MonoBehaviour
    {
        public static PatternValidator Instance { get; private set; }

        [SerializeField] private GridManager gridManager;
        [SerializeField] private TextMeshProUGUI finishText;
        [SerializeField] private TextMeshProUGUI headText;
        [SerializeField] private GameObject resultPanel;

        private readonly Dictionary<Vector2Int, int> expectedTypeByPosition = new Dictionary<Vector2Int, int>();
        private List<TileController> tiles = new List<TileController>();
        private float roundStartTime;
        private float roundTimeLimit;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void BeginRound(List<TileController> roundTiles, float timeLimitSeconds)
        {
            tiles = roundTiles;
            roundStartTime = Time.time;
            roundTimeLimit = timeLimitSeconds;

            expectedTypeByPosition.Clear();
            foreach (var tile in tiles)
                expectedTypeByPosition[tile.OriginalGridPos] = tile.TileTypeId;

            DragController.Instance?.ResetMoveCount();
        }

        // O(1): is whatever tile currently sits in gridPos the type that
        // belongs there?
        public bool IsPositionCorrect(Vector2Int gridPos)
        {
            if (!expectedTypeByPosition.TryGetValue(gridPos, out int expectedType))
                return false;

            TileController occupant = gridManager.GetTileAt(gridPos);
            return occupant != null && occupant.TileTypeId == expectedType;
        }

        public bool AreAllTilesCorrect()
        {
            if (expectedTypeByPosition.Count == 0) return false;
            foreach (var gridPos in expectedTypeByPosition.Keys)
                if (!IsPositionCorrect(gridPos)) return false;
            return true;
        }

        public int CountCorrectTiles()
        {
            int correct = 0;
            foreach (var gridPos in expectedTypeByPosition.Keys)
                if (IsPositionCorrect(gridPos)) correct++;
            return correct;
        }

        // Returns one currently-misplaced tile for the hint button to flash,
        // or null if the pattern is already fully solved.
        public TileController GetHintTile()
        {
            foreach (var tile in tiles)
                if (!IsPositionCorrect(tile.CurrentGridPos)) return tile;
            return null;
        }

        // Call when the player presses "Check" (or the round timer runs out).
        public void CheckAndFinishRound()
        {
            int correct = CountCorrectTiles();
            int total = expectedTypeByPosition.Count;
            float timeTaken = Time.time - roundStartTime;
            int moveCount = DragController.Instance != null ? DragController.Instance.MoveCount : 0;

            float roundScore = ScoreCalculator.CalculateRoundScore(correct, total, timeTaken, roundTimeLimit, moveCount);
            ScoreManager.Instance?.AddRoundScore(roundScore);

            bool success = correct == total;
            if (finishText != null)
                finishText.text = success ? "Başarılı!" : $"{correct}/{total} doğru";
            if (headText != null)
                headText.text = "Bölüm Bitti";

            GameManager.Instance.inputLocked = true;
            if (resultPanel != null)
                resultPanel.SetActive(true);

            MindTiles.UI.UIManager.Instance?.RefreshScoreTexts();
        }

        public void Reinitialize(List<TileController> newTiles)
        {
            tiles = newTiles;
        }
    }
}
