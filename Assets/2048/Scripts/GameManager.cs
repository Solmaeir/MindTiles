using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Legacy2048
{
public class GameManager : MonoBehaviour
{
    
    BlockGenerator blockGenerator;
    public float waitBeforeScramble = 3f;
    private bool blocksScrambled = false;
    public static GameManager Instance { get; private set; }
    public int numberOfBlocks = 0;

    private List<Vector2Int> originalBlockPositions = new List<Vector2Int>();

    public List<GameObject> placedBlocks = new List<GameObject>();

    public CountdownPanel countdownPanel;
    

    public bool inputLocked = true;

    public GameObject Endpanel;

    private static int defaultLevel = 1;
    [SerializeField] public int currentLevel = 1;
    public GameObject endPanel;



    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        blockGenerator= GetComponent<BlockGenerator>();
    }

    void Start()
    {
        if (endPanel != null)
            endPanel.SetActive(false);
        blockGenerator.GenerateBlocks(currentLevel);
        waitBeforeScramble = 3f;
        StartCoroutine(ScrambleAfterDelay(waitBeforeScramble));
    }

    public void RegisterBlock(GameObject block, Vector2Int position)
    {
        placedBlocks.Add(block);
        originalBlockPositions.Add(position);
        Debug.Log($"Kayıt: {position}");
    }

    public List<Vector2Int> GetOriginalBlockPositions()
    {
        return originalBlockPositions;
    }

    public void ClearBlocks()
    {
        foreach (var block in placedBlocks)
        {
            Destroy(block);
        }
        placedBlocks.Clear();
        originalBlockPositions.Clear();
    }

    private IEnumerator ScrambleAfterDelay(float seconds)
    {
        yield return StartCoroutine(countdownPanel.ShowCountdown(seconds));
        
        inputLocked = false;
        ScrambleBlocks();
        UI_Text.Instance.updateLevelText(currentLevel.ToString());

    }

    private void ScrambleBlocks()
    {
        if (blocksScrambled) return;
        blocksScrambled = true;

        // 1. Karıştırılacak pozisyonları, BlockGenerator'dan TÜM GRİD'İ (4x4=16 pozisyon) isteyerek al.
        // DEĞİŞEN TEK SATIR BURASI: Artık sadece 6 pozisyonu değil, grid'in tamamını alıyoruz.
        List<Vector2Int> availablePositions = blockGenerator.GetAllGridPositions();

        // 2. Her bloğu döngüye al ve ona 16 pozisyonluk bu geniş havuzdan rastgele bir pozisyon ata.
        foreach (var block in placedBlocks)
        {
            // Havuzda pozisyon kalmadıysa (blok sayısı grid boyutundan fazlaysa diye bir güvenlik önlemi)
            if (availablePositions.Count == 0) break;

            // Rastgele bir pozisyon seç
            int index = Random.Range(0, availablePositions.Count);
            Vector2Int newGridPos = availablePositions[index];

            // Seçilen pozisyonu havuzdan çıkar ki başka bir blok aynı yere atanmasın
            availablePositions.RemoveAt(index);

            // 3. DOĞRU DÜNYA POZİSYONUNU HESAPLA
            // Bu kısım zaten doğruydu, olduğu gibi kalıyor.
            Vector3 newWorldPos = blockGenerator.GetWorldPositionFromGridCoordinate(newGridPos);

            block.transform.position = newWorldPos;
        }

        foreach (var block in placedBlocks)
        {
            var highlight = block.GetComponent<BlockHighlight>();
            if (highlight != null)
            {
                highlight.CheckIfCorrect();
            }
        }


    }
    public void CallLevel()
    {
        StartCoroutine(CallLevelRoutine());
        
    }

    private IEnumerator CallLevelRoutine()
    {

        // Yeni level başlarken input kilitle
        inputLocked = true;

        // Scramble flag’ini sıfırla
        blocksScrambled = false;

        // Eski blokları temizle
        ClearBlocks();
        Endpanel.SetActive(false);

        // Destroy() frame sonunda çalıştığı için bir frame bekle
        yield return null;

        // Level artır (10’dan büyük olmasın)
        if (currentLevel < 10)
        {
            currentLevel++;
        }
        if (currentLevel == 10) // 10 level bitince
        {
            EndGame();
        }
        
        // Yeni blokları üret (zorluk BlockGenerator içinde ayarlanıyor)
        blockGenerator.GenerateBlocks(currentLevel);

        // CheckController resetle
        if (CheckController.Instance != null)
        {
            CheckController.Instance.Reinitialize(placedBlocks);
        }

        // Yeniden scramble başlat
        StartCoroutine(ScrambleAfterDelay(waitBeforeScramble+currentLevel));
    }
    private void EndGame()
    {
        if (endPanel != null)
            endPanel.SetActive(true);
        Time.timeScale = 0f; // oyun dursun istersen
    }
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu"); // Menü sahnesinin ismini buraya yaz
    }

}
}
