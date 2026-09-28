using UnityEngine;
using TMPro;

namespace MindTiles.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ScorePulseEffect : MonoBehaviour
    {
        [Header("Boyut Ayarları")]
        [Tooltip("Metnin en küçük hali (Orijinal boyutu)")]
        [SerializeField] private float minScale = 1f;

        [Tooltip("Metnin ne kadar büyüyeceği")]
        [SerializeField] private float maxScale = 1.15f;

        [Tooltip("Efektin hızı")]
        [SerializeField] private float pulseSpeed = 2f;

        [Header("Renk (Parlama) Ayarları")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.2f, 1f); // Altın/Sarımsı parlama

        private TextMeshProUGUI scoreText;
        private RectTransform rectTransform;

        private void Awake()
        {
            scoreText = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            // Sinüs dalgasını 0 ile 1 arasında salınacak şekilde ayarlıyoruz
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            // Boyutu Lerp ile yumuşat
            float currentScale = Mathf.Lerp(minScale, maxScale, t);
            rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);

            // Rengi normal ile parlak renk arasında Lerp ile yumuşat
            scoreText.color = Color.Lerp(normalColor, glowColor, t);
        }
    }
}