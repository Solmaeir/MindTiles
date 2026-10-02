using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using MindTiles.Grid;
using MindTiles.Core;

namespace MindTiles.InputHandling
{
    // One gesture handles both control styles:
    //  - Drag & drop: hold a tile, it follows the pointer, and on release it
    //    snaps into the nearest cell (swapping with whatever sits there).
    //  - Swipe: a short flick that doesn't carry the tile into another cell
    //    still moves it one cell in the dominant direction.
    // Raycasting happens once per press; while dragging the per-frame work is
    // just a clamp and a transform write.
    public class DragController : MonoBehaviour
    {
        public static DragController Instance { get; private set; }

        [SerializeField] private GridManager gridManager;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 15f;
        [Tooltip("Minimum pointer travel (world units) for a release to count as a swipe when the tile stays in its own cell.")]
        [SerializeField] private float swipeThreshold = 0.5f;
        [Tooltip("Pointer travel (world units) before a press turns into a drag. Filters out plain taps.")]
        [SerializeField] private float dragStartThreshold = 0.15f;

        private TileController selectedTile;
        private Vector3 pressWorldPos;
        private Vector3 grabOffset;
        private Vector3 dragMin;
        private Vector3 dragMax;
        private bool isDragging;
        private bool isAnimating;
        private Camera cam;

        public int MoveCount { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            cam = Camera.main;
        }

        public void ResetMoveCount()
        {
            MoveCount = 0;
        }

        private void Update()
        {
            // Pause / lock mid-drag: put the tile back and drop the gesture.
            if (GameManager.Instance != null && !GameManager.Instance.CanAcceptInput)
            {
                if (selectedTile != null) CancelSelection();
                return;
            }
            if (isAnimating) return;

            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                TryBeginPress();
            }
            else if (selectedTile != null)
            {
                if (pointer.press.wasReleasedThisFrame)
                    EndPress();
                else if (pointer.press.isPressed)
                    UpdateDrag();
            }
        }

        private void TryBeginPress()
        {
            Vector3 pointerWorldPos = GetPointerWorldPos();
            RaycastHit2D hit = Physics2D.Raycast(pointerWorldPos, Vector2.zero);

            if (hit.collider == null || !hit.collider.CompareTag("Tile")) return;

            TileController tile = hit.collider.GetComponent<TileController>();
            if (tile == null) return;

            selectedTile = tile;
            pressWorldPos = pointerWorldPos;
            grabOffset = tile.transform.position - pointerWorldPos;
            isDragging = false;

            // Tile centers can't leave the grid; cached once per press.
            dragMin = gridManager.GetWorldPositionFromGridCoordinate(Vector2Int.zero);
            dragMax = gridManager.GetWorldPositionFromGridCoordinate(
                new Vector2Int(gridManager.GridWidth - 1, gridManager.GridHeight - 1));
        }

        private void UpdateDrag()
        {
            Vector3 pointerWorldPos = GetPointerWorldPos();

            if (!isDragging)
            {
                if ((pointerWorldPos - pressWorldPos).sqrMagnitude < dragStartThreshold * dragStartThreshold)
                    return;

                isDragging = true;
                selectedTile.SetDragging(true);
            }

            Vector3 target = pointerWorldPos + grabOffset;
            target.x = Mathf.Clamp(target.x, dragMin.x, dragMax.x);
            target.y = Mathf.Clamp(target.y, dragMin.y, dragMax.y);
            target.z = 0f;
            selectedTile.transform.position = target;
        }

        private void EndPress()
        {
            TileController tile = selectedTile;
            bool wasDragging = isDragging;
            Vector3 dragVector = GetPointerWorldPos() - pressWorldPos;

            selectedTile = null;
            isDragging = false;

            // Plain tap: nothing to do.
            if (!wasDragging) return;

            tile.SetDragging(false);

            Vector3 dropPos = tile.transform.position;
            Vector2Int origin = tile.CurrentGridPos;
            Vector2Int target = gridManager.GetNearestGridCoordinate(dropPos);

            // Dropped back in its own cell but flicked far enough: treat as a swipe.
            if (target == origin && dragVector.magnitude >= swipeThreshold)
            {
                Vector2Int dir = Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y)
                    ? (dragVector.x > 0 ? Vector2Int.right : Vector2Int.left)
                    : (dragVector.y > 0 ? Vector2Int.up : Vector2Int.down);

                Vector2Int swipeTarget = origin + dir;
                if (swipeTarget.x >= 0 && swipeTarget.x < gridManager.GridWidth &&
                    swipeTarget.y >= 0 && swipeTarget.y < gridManager.GridHeight)
                    target = swipeTarget;
            }

            StartCoroutine(SettleTile(tile, target, dropPos));
        }

        // Drops an in-progress drag and puts the tile back (pause, screen rotation...).
        public void CancelSelection()
        {
            if (isDragging)
            {
                selectedTile.SetDragging(false);
                selectedTile.transform.position = gridManager.GetWorldPositionFromGridCoordinate(selectedTile.CurrentGridPos);
            }
            selectedTile = null;
            isDragging = false;
        }

        // Moves the tile from where the player let go into its target cell
        // (swapping with the occupant if any). If target == origin the tile
        // just slides back and no move is counted.
        private IEnumerator SettleTile(TileController tile, Vector2Int target, Vector3 dropPos)
        {
            isAnimating = true;

            Vector2Int origin = tile.CurrentGridPos;
            Vector3 tile1End = gridManager.GetWorldPositionFromGridCoordinate(target);
            TileController tile2 = null;
            Vector3 tile2Start = Vector3.zero;
            Vector3 tile2End = Vector3.zero;

            if (target != origin)
            {
                // Logical swap happens now; visuals catch up in the loop below.
                tile2 = gridManager.MoveTile(tile, target);
                MoveCount++;

                tile2Start = tile1End;
                tile2End = gridManager.GetWorldPositionFromGridCoordinate(origin);
            }

            Vector3 tile1Start = dropPos;
            tile.transform.position = tile1Start;
            if (tile2 != null) tile2.transform.position = tile2Start;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                float step = Mathf.Clamp01(t);

                // Ends are re-read every frame so a screen rotation mid-animation can't leave tiles at stale positions.
                tile1End = gridManager.GetWorldPositionFromGridCoordinate(tile.CurrentGridPos);
                tile.transform.position = Vector3.Lerp(tile1Start, tile1End, step);
                if (tile2 != null)
                {
                    tile2End = gridManager.GetWorldPositionFromGridCoordinate(tile2.CurrentGridPos);
                    tile2.transform.position = Vector3.Lerp(tile2Start, tile2End, step);
                }

                yield return null;
            }

            tile.transform.position = gridManager.GetWorldPositionFromGridCoordinate(tile.CurrentGridPos);
            if (tile2 != null)
                tile2.transform.position = gridManager.GetWorldPositionFromGridCoordinate(tile2.CurrentGridPos);

            isAnimating = false;
        }

        private Vector3 GetPointerWorldPos()
        {
            Vector3 screenPos = Pointer.current.position.ReadValue();
            screenPos.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(screenPos);
        }
    }
}
