using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace Legacy2048
{
public class BlockHighlight : MonoBehaviour
{
    private SpriteRenderer sr;

    // BlockGenerator'a referans tutmak için bir değişken
    private BlockGenerator blockGenerator;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // BlockGenerator referansını oyunun başında GameManager üzerinden alıyoruz.
        // Bu en güvenli yöntemdir, çünkü GameManager'ın her zaman var olduğunu biliyoruz.
        if (GameManager.Instance != null)
        {
            blockGenerator = GameManager.Instance.GetComponent<BlockGenerator>();
        }
        else
        {
            Debug.LogError("BlockGenerator bulunamadı çünkü GameManager mevcut değil!");
        }
       
    }

    public void CheckIfCorrect()
    {
        // blockGenerator referansının dolu olduğundan emin olalım.
        if (blockGenerator == null)
        {
            Debug.LogError("BlockGenerator referansı boş, pozisyon kontrolü yapılamıyor!");
            return;
        }

        bool isInCorrectPosition = false;

        // Tüm orijinal pozisyonları (doğru kabul edilen yerleri) kontrol et
        foreach (Vector2Int originalPos in GameManager.Instance.GetOriginalBlockPositions())
        {
            // OLMASI GEREKEN DÜNYA POZİSYONUNU HESAPLA:
            // Artık kendimiz hatalı hesap yapmak yerine, bu işin uzmanı olan
            // BlockGenerator'a soruyoruz.
            Vector3 expectedWorldPos = blockGenerator.GetWorldPositionFromGridCoordinate(originalPos);

            // Mevcut pozisyonumuz ile olması gereken pozisyon arasındaki mesafeyi kontrol et
            if (Vector3.Distance(transform.position, expectedWorldPos) < 0.1f)
            {
                // Eşleşme bulundu! Bu blok, doğru yerlerden birinde.
                isInCorrectPosition = true;
                break; // Daha fazla pozisyon kontrol etmeye gerek yok, döngüden çık
            }
        }

        // CheckController'a bu bloğun durumunu bildir.
        // Bu satırda bir değişiklik yok, doğru çalışıyordu.
        if (CheckController.Instance != null)
        {
            CheckController.Instance.UpdateBlockStatus(this.gameObject, isInCorrectPosition);
        }

        // Bloğun rengini durumuna göre ayarla.
        // Bu kısımda da bir değişiklik yok.
        if (isInCorrectPosition)
        {
            sr.color = Color.green;
        }
        else
        {
            sr.color = Color.red;
        }
    }
}
}