using UnityEngine;
using System.Collections.Generic;

namespace Legacy2048
{
public class BlockGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject EasyBlockPrefab;
    [SerializeField] private List<GameObject> IntermediateBlockPrefabs;
    [SerializeField] private List<GameObject> HardBlockPrefabs;

    [Header("Grid Ayarları")]
    [SerializeField] private float baseCellSize = 1f;
    [SerializeField] private int baseGridWidth = 4;
    [SerializeField] private int baseGridHeight = 4;
    [SerializeField] private int gridIncreasePerLevel = 1;

    [Header("Scale Ayarları")]
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private float scaleDecreasePerLevel = 0.05f;
    [SerializeField] private float minScale = 0.3f;

    private int gridWidth;
    private int gridHeight;

    // Şu an kullanılan hücre boyutu
    public float CurrentCellSize { get; private set; }
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    public void GenerateBlocks(int currentLevel)
    {
        // Level’e göre grid boyutlarını güncelle
        UpdateGridSize(currentLevel);

        int numberOfBlocks;
        GameObject prefabToUse = null;
        List<GameObject> prefabPool = null;

        // LEVEL AYARLARI
        if (currentLevel <= 3)
        {
            numberOfBlocks = 6;
            CurrentCellSize = baseCellSize;
            prefabToUse = EasyBlockPrefab;
            InputController.Instance.moveDistance = 1;
        }
        else if (currentLevel <= 7)
        {
            numberOfBlocks = 10;
            CurrentCellSize = 0.8f;
            prefabPool = IntermediateBlockPrefabs;
            InputController.Instance.moveDistance = 0.8f;
        }
        else
        {
            numberOfBlocks = 15;
            CurrentCellSize = 0.6f;
            prefabPool = HardBlockPrefabs;
            InputController.Instance.moveDistance = 0.6f;
        }

        // Grid pozisyonlarını hazırla
        List<Vector2Int> allGridPositions = GetAllGridPositions();
        if (numberOfBlocks > allGridPositions.Count)
            numberOfBlocks = allGridPositions.Count;

        for (int i = 0; i < numberOfBlocks; i++)
        {
            int index = Random.Range(0, allGridPositions.Count);
            Vector2Int chosenGridPos = allGridPositions[index];
            allGridPositions.RemoveAt(index);

            Vector3 worldPos = GetWorldPositionFromGridCoordinate(chosenGridPos);

            GameObject block;
            if (prefabToUse != null)
            {
                block = Instantiate(prefabToUse, worldPos, Quaternion.identity);
            }
            else if (prefabPool != null && prefabPool.Count > 0)
            {
                block = Instantiate(prefabPool[Random.Range(0, prefabPool.Count)], worldPos, Quaternion.identity);
            }
            else
            {
                Debug.LogError("Prefab ayarı yapılmamış!");
                return;
            }

            // Zorluk seviyesine göre scale uygula
            ApplyScale(block, currentLevel);

            block.name = $"Block_{chosenGridPos.x}_{chosenGridPos.y}";
            GameManager.Instance.RegisterBlock(block, chosenGridPos);
        }
    }

    private void UpdateGridSize(int level)
    {
        if (level <= 3)
        {
            gridWidth = 4;
            gridHeight = 4;
        }
        else if (level <= 7)
        {
            gridWidth = 5;
            gridHeight = 5;
        }
        else
        {
            gridWidth = 5;
            gridHeight = 6;
        }
        
    }

    private void ApplyScale(GameObject block, int level)
    {
        float newScale;
        if (level <= 3)
        {
            newScale = 0.8f;
        }
        else if (level <= 7)
        {
            newScale = 0.6f;
        }
        else
        {
            newScale = 0.4f;
        }
       
        block.transform.localScale = Vector3.one * newScale;
    }

    public Vector3 GetWorldPositionFromGridCoordinate(Vector2Int gridCoordinate)
    {
        Vector3 startPos = new Vector3(
            transform.position.x - (gridWidth * CurrentCellSize / 2f),
            transform.position.y - (gridHeight * CurrentCellSize / 2f),
            0f
        );

        Vector3 worldPos = new Vector3(
            startPos.x + (gridCoordinate.x * CurrentCellSize) + (CurrentCellSize / 2f),
            startPos.y + (gridCoordinate.y * CurrentCellSize) + (CurrentCellSize / 2f),
            0f
        );

        return worldPos;
    }

    public List<Vector2Int> GetAllGridPositions()
    {
        List<Vector2Int> allPositions = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                allPositions.Add(new Vector2Int(x, y));
            }
        }
        return allPositions;
    }
}
}
