using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MindTiles.Core;
using MindTiles.Grid;
using MindTiles.InputHandling;

namespace MindTiles.Validation
{
    public enum MarkKind { Correct, WrongTile, Destination }

    // One colored cell in the round-end reveal. WrongTile and Destination
    // that share a pairId belong together: that tile should end up there.
    public struct CellMark
    {
        public Vector2Int cell;
        public MarkKind kind;
        public int pairId;
    }

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

        private readonly Dictionary<Vector2Int, int> expectedTypeByPosition = new Dictionary<Vector2Int, int>();
        private List<TileController> tiles = new List<TileController>();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void BeginRound(List<TileController> roundTiles)
        {
            tiles = roundTiles;

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

        // Adds a Correct mark for every tile currently sitting where it belongs.
        public void AppendCorrectMarks(List<CellMark> marks)
        {
            foreach (var tile in tiles)
                if (IsPositionCorrect(tile.CurrentGridPos))
                    marks.Add(new CellMark { cell = tile.CurrentGridPos, kind = MarkKind.Correct, pairId = -1 });
        }

        // Finds a cell this tile's type belongs in that isn't correctly filled yet.
        public bool TryGetHintTarget(TileController tile, out Vector2Int target)
        {
            foreach (var entry in expectedTypeByPosition)
            {
                if (entry.Value != tile.TileTypeId || IsPositionCorrect(entry.Key)) continue;
                target = entry.Key;
                return true;
            }

            target = default;
            return false;
        }

        // Fills in the result text and returns how many tiles are correct.
        // Turning that into points is GameManager's job.
        public int EvaluateRound()
        {
            int correct = CountCorrectTiles();
            int total = expectedTypeByPosition.Count;

            if (finishText != null)
                finishText.text = correct == total ? "Perfect!" : $"{correct}/{total} correct";

            return correct;
        }

        // Fills `marks` with the round-end reveal: correct cells, plus for
        // every misplaced tile the cell it belongs in. Pairing is by tile
        // type (identical-looking tiles are interchangeable), and since the
        // tile set never changes, every misplaced tile finds exactly one cell.
        public void CollectMarks(List<CellMark> marks)
        {
            marks.Clear();
            var wrongByType = new Dictionary<int, Queue<TileController>>();

            foreach (var tile in tiles)
            {
                if (IsPositionCorrect(tile.CurrentGridPos))
                {
                    marks.Add(new CellMark { cell = tile.CurrentGridPos, kind = MarkKind.Correct, pairId = -1 });
                    continue;
                }

                if (!wrongByType.TryGetValue(tile.TileTypeId, out var queue))
                    wrongByType[tile.TileTypeId] = queue = new Queue<TileController>();
                queue.Enqueue(tile);
            }

            int pairId = 0;
            foreach (var entry in expectedTypeByPosition)
            {
                if (IsPositionCorrect(entry.Key)) continue;
                if (!wrongByType.TryGetValue(entry.Value, out var queue) || queue.Count == 0) continue;

                TileController wrongTile = queue.Dequeue();
                marks.Add(new CellMark { cell = wrongTile.CurrentGridPos, kind = MarkKind.WrongTile, pairId = pairId });
                marks.Add(new CellMark { cell = entry.Key, kind = MarkKind.Destination, pairId = pairId });
                pairId++;
            }
        }

        public void Reinitialize(List<TileController> newTiles)
        {
            tiles = newTiles;
        }
    }
}
