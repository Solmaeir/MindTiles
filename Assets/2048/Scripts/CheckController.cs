using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq; // LINQ kullan�m� i�in eklendi

namespace Legacy2048
{
public class CheckController : MonoBehaviour
{
    public static CheckController Instance { get; private set; }
    [SerializeField]
    private List<GameObject> allBlocks = new List<GameObject>();
    private Dictionary<GameObject, bool> blockChecks = new Dictionary<GameObject, bool>();

    public TextMeshProUGUI finishText;
    public TextMeshProUGUI headText;
    public GameObject panel;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private IEnumerator Start()
    {
        
        yield return null;

        // GameManager'dan blok listesini al
        allBlocks = GameManager.Instance.placedBlocks;

        // Dictionary'yi ba�lang�� durumuyla doldur.
        InitializeBlockChecks();
    }

    // Ba�lang��ta dictionary'yi dolduran metot
    private void InitializeBlockChecks()
    {
        blockChecks.Clear();
        foreach (GameObject block in allBlocks)
        {
            
            if (!blockChecks.ContainsKey(block))
            {
                blockChecks.Add(block, false);
            }
        }
       
    }

   
    public void UpdateBlockStatus(GameObject block, bool isInCorrectPosition)
    {
        // Dictionary'nin bu blo�u i�erdi�inden emin ol
        if (blockChecks.ContainsKey(block))
        {
            blockChecks[block] = isInCorrectPosition;
            Debug.Log(block.name + " durumu g�ncellendi: " + isInCorrectPosition);
        }
    }

    // Bu metot, "Kontrol Et" butonuna bas�ld���nda �a�r�labilir.
    public void CheckAllBlocksAndFinish()
    {
        if (AreAllBlocksCorrect())
        {
            finishText.text = "Ba�ar�l�!";
            Debug.Log("Tebrikler! T�m bloklar do�ru yerde.");
            FinishEpisode();
        }
        else
        {
            finishText.text = "Ba�ar�s�z!";
            Debug.Log("Hen�z t�m bloklar do�ru yerde de�il. Tekrar dene.");
            // �ste�e ba�l�: Oyuncuya bir geri bildirim verilebilir.
            FinishEpisode();
        }
    }

    public void FinishEpisode()
    {
        GameManager.Instance.inputLocked = true;
        panel.SetActive(true);
        // B�l�m� bitirme, sonraki seviyeye ge�me veya skor g�sterme kodlar� buraya gelecek.
        headText.text = "B�l�m Bitti";
        Debug.Log("B�L�M B�TT�!");

    }

    // LINQ kullanarak t�m bloklar�n do�ru olup olmad���n� kontrol eden metot
    private bool AreAllBlocksCorrect()
    {
        // E�er hi� blok yoksa veya dictionary bo�sa, kontrol anlams�zd�r.
        if (blockChecks.Count == 0)
        {
            return false;
        }

        // Dictionary'deki t�m de�erlerin 'true' olup olmad���n� kontrol et.
        return blockChecks.Values.All(isCorrect => isCorrect);
    }

    public void Reinitialize(List<GameObject> newBlocks)
    {
        // Referans� g�ncelle
        allBlocks = newBlocks;

        // Eski dictionary'yi temizle ve yeni bloklarla doldur
        blockChecks.Clear();
        foreach (GameObject block in allBlocks)
        {
            if (block != null && !blockChecks.ContainsKey(block))
                blockChecks.Add(block, false);
        }

        Debug.Log("CheckController: Reinitialized with " + allBlocks.Count + " blocks.");
    }
}
}

