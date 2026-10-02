using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MindTiles.Grid;
using MindTiles.InputHandling;
using MindTiles.Validation;

namespace MindTiles.Core
{
    // Owns the hint mechanics. ButtonManager only forwards button presses here.
    // Hint inventory (counts) lives in the save data, reached through GameManager.
    //
    //  - One-tile hint: marks one misplaced tile and the cell it belongs in. The
    //    marks stay until the player puts the tile (or a twin) in a correct cell,
    //    then clear on their own and the hint can be used again.
    //  - All-tiles hint: marks every tile that is already correct for a short,
    //    timed window (with a countdown). Play is NOT locked while it shows, and
    //    the marks follow the player's moves.
    //
    // Both are drawn on a dedicated overlay, so they never mix with the round-end
    // reveal. The component only runs (Update) while a hint is active.
    public class HintManager : MonoBehaviour
    {
        [Header("All-Tiles Hint")]
        [SerializeField] private float allTilesDuration = 1f;
        [Tooltip("Shows the remaining seconds while the all-tiles hint is up. Optional.")]
        [SerializeField] private TMP_Text countdownText;

        [Header("References")]
        [SerializeField] private RoundResultOverlay hintOverlay;

        private readonly List<CellMark> marks = new List<CellMark>();

        private bool allTilesActive;
        private float allTilesRemaining;
        private int shownSeconds;

        private bool oneTileActive;
        private TileController oneTileTile;
        private Vector2Int oneTileTarget;

        private int lastMoveCount;

        // Subscribed in Awake/OnDestroy (not OnEnable): this component disables
        // itself between hints and must still hear round changes.
        private void Awake()
        {
            enabled = false;
            if (countdownText != null) countdownText.gameObject.SetActive(false);

            GameManager.RoundFinished += CancelAll;
            GameManager.RoundStarted += CancelAll;
        }

        private void OnDestroy()
        {
            GameManager.RoundFinished -= CancelAll;
            GameManager.RoundStarted -= CancelAll;
        }

        // ---- Public hint actions (return true if a hint was spent) -------------

        public bool UseOneTileHint()
        {
            if (!CanUseHint() || oneTileActive) return false;

            PatternValidator validator = PatternValidator.Instance;
            TileController tile = validator.GetHintTile();
            if (tile == null || !validator.TryGetHintTarget(tile, out Vector2Int target)) return false;
            if (!GameManager.Instance.TryConsumeHint(HintType.OneTile)) return false;

            oneTileTile = tile;
            oneTileTarget = target;
            oneTileActive = true;
            Begin();
            return true;
        }

        public bool UseAllTilesHint()
        {
            if (!CanUseHint() || allTilesActive) return false;
            if (!GameManager.Instance.TryConsumeHint(HintType.AllTiles)) return false;

            allTilesActive = true;
            allTilesRemaining = allTilesDuration;
            shownSeconds = -1;
            Begin();
            UpdateCountdownText();
            return true;
        }

        // Drops any active hint (round ended / next level).
        public void CancelAll()
        {
            allTilesActive = false;
            oneTileActive = false;
            oneTileTile = null;
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (hintOverlay != null) hintOverlay.Clear();
            enabled = false;
        }

        // ---- Internals --------------------------------------------------------

        private void Begin()
        {
            lastMoveCount = CurrentMoveCount();
            enabled = true;
            RefreshOverlay();
        }

        private void Update()
        {
            bool dirty = false;

            if (allTilesActive)
            {
                // Scaled time: the hint timer freezes while the game is paused.
                allTilesRemaining -= Time.deltaTime;
                if (allTilesRemaining <= 0f)
                {
                    allTilesActive = false;
                    if (countdownText != null) countdownText.gameObject.SetActive(false);
                    dirty = true;
                }
                else
                {
                    UpdateCountdownText();
                }
            }

            // Marks only change when the player moves a tile.
            int moves = CurrentMoveCount();
            if (moves != lastMoveCount)
            {
                lastMoveCount = moves;
                dirty = true;
                if (oneTileActive && IsOneTileDone()) oneTileActive = false;
            }

            if (dirty) RefreshOverlay();
        }

        private bool IsOneTileDone()
        {
            PatternValidator validator = PatternValidator.Instance;
            return validator.IsPositionCorrect(oneTileTarget) ||
                   validator.IsPositionCorrect(oneTileTile.CurrentGridPos);
        }

        private void RefreshOverlay()
        {
            if (!allTilesActive && !oneTileActive)
            {
                if (hintOverlay != null) hintOverlay.Clear();
                enabled = false;
                return;
            }

            if (hintOverlay == null) return;

            marks.Clear();
            if (allTilesActive)
                PatternValidator.Instance.AppendCorrectMarks(marks);
            if (oneTileActive)
            {
                marks.Add(new CellMark { cell = oneTileTile.CurrentGridPos, kind = MarkKind.WrongTile, pairId = 0 });
                marks.Add(new CellMark { cell = oneTileTarget, kind = MarkKind.Destination, pairId = 0 });
            }
            hintOverlay.Show(marks);
        }

        private void UpdateCountdownText()
        {
            if (countdownText == null) return;

            int seconds = Mathf.CeilToInt(allTilesRemaining);
            if (seconds == shownSeconds) return;

            shownSeconds = seconds;
            countdownText.text = seconds.ToString();
            countdownText.gameObject.SetActive(true);
        }

        private static int CurrentMoveCount()
        {
            return DragController.Instance != null ? DragController.Instance.MoveCount : 0;
        }

        private static bool CanUseHint()
        {
            return GameManager.Instance != null
                && GameManager.Instance.CanAcceptInput
                && PatternValidator.Instance != null;
        }
    }
}
