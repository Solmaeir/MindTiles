using System.Collections.Generic;
using UnityEngine;
using MindTiles.Core;

namespace MindTiles.Grid
{
    // Owns the grid geometry (size, cell size, world<->grid conversion) and
    // the occupancy map (which tile sits in which cell). Spawns tiles either
    // for a Daily Streak level number or for an explicit LevelSpec (Endless).
    //
    // The board panel (gridBoundaryPanel) is the single source of truth for
    // layout: cell size, tile scale, grid center, grid lines and the result
    // overlay are all derived from its on-screen rect in ApplyLayout(). A cheap
    // per-frame check re-runs ApplyLayout() whenever that rect changes (screen
    // rotation, safe area edits, board resizing...), so nothing can drift.
    // Grid dimensions (columns x rows) stay fixed for the level, so game state
    // is never touched by a re-layout.
    [DefaultExecutionOrder(-50)]
    public class GridManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject easyTilePrefab;
        [SerializeField] private List<GameObject> intermediateTilePrefabs;
        [SerializeField] private List<GameObject> hardTilePrefabs;

        [Header("Grid Settings")]
        [Tooltip("Design cell size of the easy tier. Tile scales below are relative to their tier's cell size.")]
        [SerializeField] private float baseCellSize = 1f;
        [SerializeField] private float intermediateCellSize = 0.8f;
        [SerializeField] private float hardCellSize = 0.6f;

        [Header("Grid Boundaries (UI)")]
        [SerializeField] private RectTransform gridBoundaryPanel; // Sınırları belirleyecek UI Paneli

        [Tooltip("Sizes the board to the level's grid shape. Optional: without it the grid is derived from the board as it is.")]
        [SerializeField] private MindTiles.UI.BoardFitter boardFitter;

        [Header("Visuals")]
        [SerializeField] private GridLinesRenderer gridLines;

        [Header("Scale Settings")]
        [SerializeField] private float easyScale = 0.8f;
        [SerializeField] private float intermediateScale = 0.6f;
        [SerializeField] private float hardScale = 0.4f;

        private int gridWidth;
        private int gridHeight;
        // Design cell size of the current level (used to pick a grid when none is given).
        private float levelCellSize;
        private readonly List<GameObject> spawnPrefabs = new List<GameObject>();
        private readonly List<float> spawnScales = new List<float>();
        // Board rect (world center xy, world size zw) the layout was last built for.
        private Vector4 lastBoardRect;
        private bool hasLayout;
        private readonly Vector3[] boardCorners = new Vector3[4];
        private readonly Dictionary<Vector2Int, TileController> occupancy = new Dictionary<Vector2Int, TileController>();

        public float CurrentCellSize { get; private set; }
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        // Raised after the layout was recomputed because the board changed.
        public event System.Action LayoutChanged;

        // Tile scale per world unit of cell, per tier: a tile always fills the
        // same share of its cell, whatever the cell size ends up being.
        private float EasyScalePerCell => easyScale / baseCellSize;
        private float MidScalePerCell => intermediateScale / intermediateCellSize;
        private float HardScalePerCell => hardScale / hardCellSize;

        // Daily Streak: fixed tiers by level number; the grid is chosen to fill the screen.
        public void GenerateTiles(int currentLevel)
        {
            int tileCount;
            ClearSpawnList();

            if (currentLevel <= 3)
            {
                tileCount = 6;
                levelCellSize = baseCellSize;
                AddSpawns(tileCount, easyTilePrefab, null, EasyScalePerCell);
            }
            else if (currentLevel <= 7)
            {
                tileCount = 10;
                levelCellSize = intermediateCellSize;
                AddSpawns(tileCount, null, intermediateTilePrefabs, MidScalePerCell);
            }
            else
            {
                tileCount = 15;
                levelCellSize = hardCellSize;
                AddSpawns(tileCount, null, hardTilePrefabs, HardScalePerCell);
            }

            occupancy.Clear();
            CalculateGridDimensions();
            ApplyLayout();
            SpawnQueued();
        }

        // Endless: the difficulty system decides the exact grid and tile mix.
        public void GenerateTiles(LevelSpec spec)
        {
            ClearSpawnList();
            AddSpawns(spec.easyTiles, easyTilePrefab, null, EasyScalePerCell);
            AddSpawns(spec.midTiles, null, intermediateTilePrefabs, MidScalePerCell);
            AddSpawns(spec.hardTiles, null, hardTilePrefabs, HardScalePerCell);

            occupancy.Clear();
            levelCellSize = baseCellSize;
            gridWidth = Mathf.Max(1, spec.columns);
            gridHeight = Mathf.Max(1, spec.rows);
            if (boardFitter != null)
                boardFitter.SetGrid(gridWidth, gridHeight);
            ApplyLayout();
            SpawnQueued();
        }

        private void ClearSpawnList()
        {
            spawnPrefabs.Clear();
            spawnScales.Clear();
        }

        // Queues `count` tiles from either a single prefab or a random pick from a pool.
        private void AddSpawns(int count, GameObject single, List<GameObject> pool, float scalePerCell)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = single;
                if (prefab == null && pool != null && pool.Count > 0)
                    prefab = pool[Random.Range(0, pool.Count)];

                if (prefab == null)
                {
                    Debug.LogError("GridManager: no tile prefab configured for this tier.");
                    return;
                }

                spawnPrefabs.Add(prefab);
                spawnScales.Add(scalePerCell);
            }
        }

        // Places every queued tile in a random free cell.
        private void SpawnQueued()
        {
            List<Vector2Int> availablePositions = GetAllGridPositions();

            // Never ask for more tiles than there are cells.
            int count = Mathf.Min(spawnPrefabs.Count, availablePositions.Count);

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, availablePositions.Count);
                Vector2Int gridPos = availablePositions[index];
                availablePositions.RemoveAt(index);

                GameObject prefab = spawnPrefabs[i];
                GameObject tileObj = Instantiate(prefab, GetWorldPositionFromGridCoordinate(gridPos), Quaternion.identity);
                tileObj.transform.localScale = Vector3.one * (spawnScales[i] * CurrentCellSize);
                tileObj.name = $"Tile_{gridPos.x}_{gridPos.y}";

                TileController tile = tileObj.GetComponent<TileController>();
                if (tile == null)
                    tile = tileObj.AddComponent<TileController>();

                tile.Initialize(gridPos, prefab.GetInstanceID(), spawnScales[i]);
                occupancy[gridPos] = tile;
                GameManager.Instance.RegisterTile(tileObj, tile);
            }
        }

        // Board rect in world space: xy = center, zw = size. Uses the world
        // corners, so pivot, parent scale and non-uniform scale are all handled.
        private Vector4 ReadBoardRect()
        {
            gridBoundaryPanel.GetWorldCorners(boardCorners);
            Vector3 min = boardCorners[0];
            Vector3 max = boardCorners[2];
            return new Vector4((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, max.x - min.x, max.y - min.y);
        }

        // Space kept between the grid and the board's edges (set on the BoardFitter).
        private float BoardPadding => boardFitter != null ? boardFitter.PaddingWorld : 0f;

        // Smallest cell size, as a fraction of the level's design cell size, the solver will accept.
        private const float MinCellRatio = 0.8f;

        // Columns x rows for this level. With a BoardFitter the grid is chosen
        // to fill the free screen area as fully as possible (cells stay square
        // and close to the level's design size) and the board is then resized
        // to exactly that shape. Without one, it is derived from the board.
        private void CalculateGridDimensions()
        {
            if (boardFitter != null && boardFitter.TryGetAvailableArea(out Vector4 area))
            {
                float pad = BoardPadding;
                SolveGrid(area.z - 2f * pad, area.w - 2f * pad, out gridWidth, out gridHeight);
                boardFitter.SetGrid(gridWidth, gridHeight);
                return;
            }

            if (gridBoundaryPanel == null)
            {
                gridWidth = 4;
                gridHeight = 4;
                return;
            }

            Vector4 board = ReadBoardRect();
            gridWidth = Mathf.Max(1, Mathf.FloorToInt(board.z / levelCellSize));
            gridHeight = Mathf.Max(1, Mathf.FloorToInt(board.w / levelCellSize));
        }

        // Tries rounding each axis down or up and keeps the combination whose
        // square-cell grid covers the most of the area (cell = tighter axis).
        private void SolveGrid(float areaWidth, float areaHeight, out int columns, out int rows)
        {
            int baseCols = Mathf.Max(1, Mathf.FloorToInt(areaWidth / levelCellSize));
            int baseRows = Mathf.Max(1, Mathf.FloorToInt(areaHeight / levelCellSize));

            columns = baseCols;
            rows = baseRows;
            float bestCovered = -1f;

            for (int c = baseCols; c <= baseCols + 1; c++)
            {
                for (int r = baseRows; r <= baseRows + 1; r++)
                {
                    float cell = Mathf.Min(areaWidth / c, areaHeight / r);
                    if (cell < levelCellSize * MinCellRatio) continue;

                    float covered = c * r * cell * cell;
                    if (covered > bestCovered)
                    {
                        bestCovered = covered;
                        columns = c;
                        rows = r;
                    }
                }
            }
        }

        // The one place layout is computed. Fits the (fixed) grid to the board:
        // cell size, grid center, tile scale/positions and the grid lines all
        // follow from the board rect.
        private void ApplyLayout()
        {
            if (gridBoundaryPanel != null)
            {
                Vector4 board = ReadBoardRect();
                if (board.z <= 0f || board.w <= 0f) return;

                lastBoardRect = board;
                float pad = BoardPadding;
                CurrentCellSize = Mathf.Min((board.z - 2f * pad) / gridWidth, (board.w - 2f * pad) / gridHeight);
                transform.position = new Vector3(board.x, board.y, 0f);
            }
            else
            {
                CurrentCellSize = levelCellSize;
            }
            hasLayout = true;

            foreach (var entry in occupancy)
            {
                Transform t = entry.Value.transform;
                t.position = GetWorldPositionFromGridCoordinate(entry.Key);
                t.localScale = Vector3.one * (entry.Value.ScalePerCell * CurrentCellSize);
            }

            if (gridLines != null)
                gridLines.Rebuild(gridWidth, gridHeight, CurrentCellSize, transform.position);
        }

        // Cheap change detection: four floats compared per frame; the layout
        // work only happens when the board actually moved or resized.
        private void LateUpdate()
        {
            if (!hasLayout || gridBoundaryPanel == null) return;

            Vector4 board = ReadBoardRect();
            if ((board - lastBoardRect).sqrMagnitude < 1e-8f) return;

            ApplyLayout();
            LayoutChanged?.Invoke();
        }

        public Vector3 GetWorldPositionFromGridCoordinate(Vector2Int gridCoordinate)
        {
            Vector3 startPos = new Vector3(
                transform.position.x - (gridWidth * CurrentCellSize / 2f),
                transform.position.y - (gridHeight * CurrentCellSize / 2f),
                0f
            );
            return new Vector3(
                startPos.x + (gridCoordinate.x * CurrentCellSize) + (CurrentCellSize / 2f),
                startPos.y + (gridCoordinate.y * CurrentCellSize) + (CurrentCellSize / 2f),
                0f
            );
        }

        // Inverse of GetWorldPositionFromGridCoordinate, clamped to the grid bounds.
        public Vector2Int GetNearestGridCoordinate(Vector3 worldPos)
        {
            Vector3 startPos = new Vector3(
                transform.position.x - (gridWidth * CurrentCellSize / 2f),
                transform.position.y - (gridHeight * CurrentCellSize / 2f),
                0f
            );

            int x = Mathf.RoundToInt((worldPos.x - startPos.x - CurrentCellSize / 2f) / CurrentCellSize);
            int y = Mathf.RoundToInt((worldPos.y - startPos.y - CurrentCellSize / 2f) / CurrentCellSize);

            x = Mathf.Clamp(x, 0, gridWidth - 1);
            y = Mathf.Clamp(y, 0, gridHeight - 1);

            return new Vector2Int(x, y);
        }

        public List<Vector2Int> GetAllGridPositions()
        {
            List<Vector2Int> allPositions = new List<Vector2Int>();
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    allPositions.Add(new Vector2Int(x, y));
            return allPositions;
        }

        public TileController GetTileAt(Vector2Int gridPos)
        {
            occupancy.TryGetValue(gridPos, out TileController tile);
            return tile;
        }

        // Moves a tile to targetPos, swapping with whatever tile is already
        // there (if any). Keeps the occupancy map and each tile's
        // CurrentGridPos in sync so validation never has to guess. Returns
        // the tile it swapped with, or null if the target cell was empty.
        public TileController MoveTile(TileController tile, Vector2Int targetPos)
        {
            Vector2Int originPos = tile.CurrentGridPos;
            if (originPos == targetPos)
                return null;

            TileController occupant = GetTileAt(targetPos);

            occupancy[targetPos] = tile;
            tile.SetCurrentGridPos(targetPos);
            tile.transform.position = GetWorldPositionFromGridCoordinate(targetPos);

            if (occupant != null)
            {
                occupancy[originPos] = occupant;
                occupant.SetCurrentGridPos(originPos);
                occupant.transform.position = GetWorldPositionFromGridCoordinate(originPos);
            }
            else
            {
                occupancy.Remove(originPos);
            }

            return occupant;
        }

        public void ClearOccupancy()
        {
            occupancy.Clear();
        }
    }
}
