using UnityEngine;

namespace MindTiles.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class FloatingCloud : MonoBehaviour
    {
        [Header("Hareket Ayarları")]
        [Tooltip("Bulutun sağa ve sola maksimum kaç piksel kayacağı")]
        [SerializeField] private float moveDistance = 20f;

        [Tooltip("Kayma hızının çarpanı (düşük değer = yavaş ve sakin)")]
        [SerializeField] private float moveSpeed = 0.5f;

        private RectTransform rectTransform;
        private Vector2 startPos;
        private float randomOffset;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            startPos = rectTransform.anchoredPosition;

            // Tüm bulutların aynı anda sağa/sola gitmemesi için rastgele bir başlangıç noktası veriyoruz.
            // Bu sayede "tekrar eden video" hissiyatı kırılıyor.
            randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            // Sinüs dalgası -1 ile 1 arasında yumuşak bir değer döndürür.
            float wave = Mathf.Sin(Time.time * moveSpeed + randomOffset);

            // Sadece X ekseninde başlangıç pozisyonuna göre offset ekle
            float newX = startPos.x + (wave * moveDistance);

            rectTransform.anchoredPosition = new Vector2(newX, startPos.y);
        }
    }
}