using TMPro;
using UnityEngine;

namespace MindTiles.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI overallScoreText;
        [SerializeField] private TextMeshProUGUI dailyScoreText;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void UpdateLevelText(string level)
        {
            if (levelText != null)
                levelText.text = "LEVEL " + level;
        }

        public void RefreshScoreTexts()
        {
            var scoreManager = Score.ScoreManager.Instance;
            if (scoreManager == null) return;

            if (overallScoreText != null)
                overallScoreText.text = "TOTAL " + Mathf.RoundToInt(scoreManager.GetOverallScore());

            if (dailyScoreText != null)
                dailyScoreText.text = "TODAY " + Mathf.RoundToInt(scoreManager.GetDailyScore(System.DateTime.Now));
        }
    }
}
