using UnityEngine;

namespace MindTiles.UI
{
    // Sizes and places the game board. It knows the free area the current
    // phone leaves (between the header and the bottom panel, inside the safe
    // area) and, once GridManager tells it the level's grid (columns x rows),
    // shrinks the board to exactly that grid's shape: square cells, no leftover
    // space inside the board, centered in the free area. A 4x4 level gets a
    // square board, a 4x5 level a taller one.
    //
    // GridManager then derives cell size, tiles, lines and masks from the board,
    // so the board is the single source of truth for layout. Re-fits by itself
    // whenever the free area changes (rotation, safe area edits, panel moves).
    // Nothing changes in edit mode.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(RectTransform))]
    public class BoardFitter : MonoBehaviour
    {
        [Header("Available Area")]
        [Tooltip("The safe area the board lives in; its left/right edges (and top, if there are no header references) bound the board.")]
        [SerializeField] private RectTransform areaRoot;
        [Tooltip("Header elements. The board stays below the lowest of them.")]
        [SerializeField] private RectTransform[] topReferences;
        [Tooltip("Panel at the bottom (EndPanel). The board stays above its top edge.")]
        [SerializeField] private RectTransform bottomPanel;

        [Header("Spacing (canvas units)")]
        [SerializeField] private float sideMargin = 48f;
        [SerializeField] private float topGap = 24f;
        [SerializeField] private float bottomGap = 12f;
        [Tooltip("Space inside the board around the grid, so tiles and marks never touch the board frame.")]
        [SerializeField, Min(0f)] private float innerPadding = 14f;

        [Header("Placement")]
        [Tooltip("Where the board sits in leftover vertical space: 1 = against the header, 0 = against the bottom panel, 0.5 = centered.")]
        [SerializeField, Range(0f, 1f)] private float verticalAlignment = 0.5f;
        [Tooltip("If the free space is smaller than this (canvas units), the board keeps its current size.")]
        [SerializeField] private float minSize = 300f;

        private RectTransform board;
        private int gridColumns;
        private int gridRows;
        private Vector4 lastTarget;
        private bool hasTarget;
        private readonly Vector3[] corners = new Vector3[4];

        private void Start()
        {
            Fit();
        }

        private void LateUpdate()
        {
            Fit();
        }

        // Inner padding in world units (GridManager keeps the grid this far inside the board).
        public float PaddingWorld => areaRoot != null ? innerPadding * areaRoot.lossyScale.x : 0f;

        // The free area in world space (x = left, y = bottom, z = width, w = height).
        public bool TryGetAvailableArea(out Vector4 area)
        {
            area = default;
            if (areaRoot == null) return false;

            Vector3 areaScale = areaRoot.lossyScale;
            if (areaScale.x <= 0f || areaScale.y <= 0f) return false;

            areaRoot.GetWorldCorners(corners);
            float left = corners[0].x + sideMargin * areaScale.x;
            float right = corners[2].x - sideMargin * areaScale.x;
            float top = corners[2].y;
            float bottom = corners[0].y;

            if (topReferences != null)
            {
                for (int i = 0; i < topReferences.Length; i++)
                {
                    // Hidden headers (the other mode's) don't take space.
                    if (topReferences[i] == null || !topReferences[i].gameObject.activeInHierarchy) continue;
                    topReferences[i].GetWorldCorners(corners);
                    top = Mathf.Min(top, corners[0].y);
                }
            }
            top -= topGap * areaScale.y;

            if (bottomPanel != null)
            {
                bottomPanel.GetWorldCorners(corners);
                bottom = corners[1].y;
            }
            bottom += bottomGap * areaScale.y;

            float width = right - left;
            float height = top - bottom;
            if (width < minSize * areaScale.x || height < minSize * areaScale.y) return false;

            area = new Vector4(left, bottom, width, height);
            return true;
        }

        // Tells the fitter the level's grid shape; the board is resized to it right away.
        public void SetGrid(int columns, int rows)
        {
            gridColumns = Mathf.Max(0, columns);
            gridRows = Mathf.Max(0, rows);
            hasTarget = false;
            Fit();
        }

        // Computes the target rect (world space) and applies it only if it changed.
        public void Fit()
        {
            if (board == null) board = (RectTransform)transform;

            // Needs point anchors on both axes; stretched anchors are driven by the parent instead.
            if (board.anchorMin != board.anchorMax) return;
            if (!TryGetAvailableArea(out Vector4 area)) return;

            float width = area.z;
            float height = area.w;
            float left = area.x;
            float bottom = area.y;

            if (gridColumns > 0 && gridRows > 0)
            {
                // Square cells: the tighter axis decides the cell size; the board is the grid plus padding.
                float pad = PaddingWorld;
                float cell = Mathf.Min((area.z - 2f * pad) / gridColumns, (area.w - 2f * pad) / gridRows);
                width = cell * gridColumns + 2f * pad;
                height = cell * gridRows + 2f * pad;
                left = area.x + (area.z - width) * 0.5f;
                bottom = area.y + (area.w - height) * (1f - verticalAlignment);
            }

            var target = new Vector4(left, bottom, width, height);
            if (hasTarget && (target - lastTarget).sqrMagnitude < 1e-10f) return;
            lastTarget = target;
            hasTarget = true;

            Vector3 lossy = board.lossyScale;
            if (lossy.x <= 0f || lossy.y <= 0f) return;

            board.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width / lossy.x);
            board.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height / lossy.y);

            Vector3 pos = board.position;
            pos.x = left + board.pivot.x * width;
            pos.y = bottom + board.pivot.y * height;
            board.position = pos;
        }
    }
}
