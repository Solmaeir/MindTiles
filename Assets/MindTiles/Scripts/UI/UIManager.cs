using TMPro;
using UnityEngine;
using MindTiles.Core;

namespace MindTiles.UI
{
    // Text readouts only: level, current score and highest score. The numbers
    // come from GameManager (current score = this run, highest = saved best).
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI highestScoreText;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            RefreshScoreTexts();
        }

        public void UpdateLevelText(string level)
        {
            if (levelText != null)
                levelText.text = "LEVEL " + level;
        }

        public void RefreshScoreTexts()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            if (currentScoreText != null)
                currentScoreText.text = "Score: " + gm.SessionScore.ToString("D3");

            if (highestScoreText != null)
                highestScoreText.text = "Highest Score: " + gm.HighScore.ToString("D3");
        }
    }
}
