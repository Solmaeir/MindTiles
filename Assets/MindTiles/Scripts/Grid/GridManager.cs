using System.Collections.Generic;
using UnityEngine;
using MindTiles.Core;

namespace MindTiles.Grid
{
    // Owns the grid geometry (size, cell size, world<->grid conversion) and
    // the occupancy map (which tile sits in which cell). Spawns tiles for a
    // given level according to the difficulty curve.
    public class GridManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject easyTilePrefab;
        [SerializeField] private List<GameObject> intermediateTilePrefabs;
        [SerializeField] private List<GameObject> hardTilePrefabs;

        [Header("Grid Settings")]
        [SerializeField] private float baseCellSize = 1f;

        [Header("Grid Boundaries (UI)")]
        [SerializeField] private RectTransform gridBoundaryPanel; // Sınırları belirleyecek UI Paneli
        [SerializeField] private Camera mainCamera; // Ekran-Dünya dönüşümü için kamera

        [Header("Scale Settings")]
        [SerializeField] private float easyScale = 0.8f;
        [SerializeField] private float intermediateScale = 0.6f;
        [SerializeField] private float hardScale = 0.4f;

        private int gridWidth;
        private int gridHeight;
        private readonly Dictionary<Vector2Int, TileController> occupancy = new Dictionary<Vector2Int, TileController>();

        public float CurrentCellSize { get; private set; }
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        public void GenerateTiles(int currentLevel)
        {
            occupancy.Clear();

            // 1. ÖNCE HÜCRE BOYUTUNU BELİRLE (Level'a göre)
            // Mevcut kodunda önce UpdateGridSize çalışıyordu, artık boyutu dinamik
            // hesaplayacağımız için önce tile boyutunu (CurrentCellSize) bilmemiz lazım.
            int tileCount;
            GameObject singlePrefab = null;
            List<GameObject> prefabPool = null;
            float scale;

            if (currentLevel <= 3)
            {
                tileCount = 6;
                CurrentCellSize = baseCellSize;
                singlePrefab = easyTilePrefab;
                scale = easyScale;
            }
            else if (currentLevel <= 7)
            {
                tileCount = 10;
                CurrentCellSize = 0.8f;
                prefabPool = intermediateTilePrefabs;
                scale = intermediateScale;
            }
            else
            {
                tileCount = 15;
                CurrentCellSize = 0.6f;
                prefabPool = hardTilePrefabs;
                scale = hardScale;
            }

            // 2. UI PANELİNE GÖRE DİNAMİK IZGARA BOYUTUNU HESAPLA
            CalculateDynamicGridSize();

            // 3. RASTGELE YERLEŞTİRME (Mevcut mantık)
            List<Vector2Int> availablePositions = GetAllGridPositions();

            // Eğer sığabilecek yer, üretilmesi gereken kareden azsa sayıyı kısıtla (Crash önleyici)
            if (tileCount > availablePositions.Count)
                tileCount = availablePositions.Count;

            for (int i = 0; i < tileCount; i++)
            {
                int index = Random.Range(0, availablePositions.Count);
                Vector2Int gridPos = availablePositions[index];
                availablePositions.RemoveAt(index);

                GameObject prefabToSpawn = singlePrefab;
                if (prefabToSpawn == null && prefabPool != null && prefabPool.Count > 0)
                    prefabToSpawn = prefabPool[Random.Range(0, prefabPool.Count)];

                if (prefabToSpawn == null)
                {
                    Debug.LogError("GridManager: no tile prefab configured for this level.");
                    return;
                }

                Vector3 worldPos = GetWorldPositionFromGridCoordinate(gridPos);
                GameObject tileObj = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
                tileObj.transform.localScale = Vector3.one * scale;
                tileObj.name = $"Tile_{gridPos.x}_{gridPos.y}";

                TileController tile = tileObj.GetComponent<TileController>();
                if (tile == null)
                    tile = tileObj.AddComponent<TileController>();

                tile.Initialize(gridPos, prefabToSpawn.GetInstanceID());
                occupancy[gridPos] = tile;
                GameManager.Instance.RegisterTile(tileObj, tile);
            }
        }
        private void CalculateDynamicGridSize()
        {
            if (gridBoundaryPanel == null)
            {
                gridWidth = 4;
                gridHeight = 4;
                return;
            }

            // 1. Panelin dünyadaki (World Space) gerçek boyutlarını kameradan bağımsız hesapla
            float worldWidth = gridBoundaryPanel.rect.width * gridBoundaryPanel.lossyScale.x;
            float worldHeight = gridBoundaryPanel.rect.height * gridBoundaryPanel.lossyScale.y;

            // 2. Kapasiteyi hesapla (Crash olmaması için Mathf.Max ile minimum 1'de tutuyoruz)
            gridWidth = Mathf.Max(1, Mathf.FloorToInt(worldWidth / CurrentCellSize));
            gridHeight = Mathf.Max(1, Mathf.FloorToInt(worldHeight / CurrentCellSize));

            // 3. GridManager'ı panelin tam merkezine hizala
            transform.position = new Vector3(gridBoundaryPanel.position.x, gridBoundaryPanel.position.y, 0f);

            Debug.Log($"[GridManager] Panel Dünya Boyutu: {worldWidth} x {worldHeight} | Hücre Boyutu: {CurrentCellSize} | Çıkan Grid: {gridWidth}x{gridHeight}");
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
