using UnityEngine;

namespace MindTiles.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class PulseGlow : MonoBehaviour
    {
        [Header("Parlama Ayarları")]
        [Tooltip("Parlamanın en sönük (küçük) halindeki boyutu")]
        [SerializeField] private float minScale = 1f;

        [Tooltip("Parlamanın en güçlü (büyük) halindeki boyutu")]
        [SerializeField] private float maxScale = 1.15f;

        [Tooltip("Büyüyüp küçülme hızı")]
        [SerializeField] private float pulseSpeed = 1.5f;

        private RectTransform rectTransform;
        private float randomOffset;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();

            // Birden fazla yıldıza atarsan hepsi aynı anda parlamasın diye offset ekliyoruz
            randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            // Sinüs dalgası normalde -1 ile 1 arasıdır. 
            // Bunu (sin + 1) / 2 yaparak 0 ile 1 arasına sıkıştırıyoruz ki Lerp fonksiyonuna tam uysun.
            float t = (Mathf.Sin(Time.time * pulseSpeed + randomOffset) + 1f) / 2f;

            // minScale ve maxScale arasında yumuşak geçiş
            float currentScale = Mathf.Lerp(minScale, maxScale, t);

            rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);
        }
    }
}