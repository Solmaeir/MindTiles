using TMPro;
using UnityEngine;
using MindTiles.Audio;
using MindTiles.Core;

namespace MindTiles.UI
{
    // Text readouts only. Two header layouts, one per mode:
    //   Daily Streak: "LEVEL n"
    //   Endless:      total score on top, "LEVEL n" below it, level timer under that
    // End panel: Daily Streak shows run score + highest score,
    //            Endless shows this level's points + total score.
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Daily Streak Header")]
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Endless Header")]
        [SerializeField] private GameObject endlessHeader;
        [SerializeField] private TextMeshProUGUI endlessTotalScoreText;
        [SerializeField] private TextMeshProUGUI endlessLevelText;
        [SerializeField] private TextMeshProUGUI endlessTimerText;
        [Tooltip("Timer turns this color in the last seconds.")]
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField, Min(0)] private int timerWarningSeconds = 5;

        [Header("Header Panel")]
        [Tooltip("Panel behind the header; its height follows the mode (Endless shows more lines).")]
        [SerializeField] private FillToScreenEdges headerPanel;
        [SerializeField] private float dailyHeaderHeight = 230f;
        [SerializeField] private float endlessHeaderHeight = 330f;

        [Header("End Panel")]
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI highestScoreText;
        [Tooltip("Title of the end panel (\"COMPLETED!\" / \"TRY AGAIN\").")]
        [SerializeField] private TMP_Text resultTitleText;
        [Tooltip("Label of the end panel's Next button (becomes \"Retry\" when the level failed).")]
        [SerializeField] private TMP_Text nextButtonLabel;
        [SerializeField] private string passedTitle = "COMPLETED!";
        [SerializeField] private string failedTitle = "TRY AGAIN";
        [SerializeField] private string nextLabel = "Next";
        [SerializeField] private string retryLabel = "Retry";
        [Tooltip("After the last Daily Streak level.")]
        [SerializeField] private string runCompleteTitle = "RUN COMPLETE!";
        [SerializeField] private string runCompleteLabel = "Main Menu";

        private bool endlessMode;
        private int shownTimerSeconds = -1;
        private Color timerNormalColor = Color.white;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            if (endlessTimerText != null) timerNormalColor = endlessTimerText.color;
        }

        // Shows the header that belongs to the active mode.
        public void ConfigureForMode(bool endless)
        {
            endlessMode = endless;
            if (levelText != null) levelText.gameObject.SetActive(!endless);
            if (endlessHeader != null) endlessHeader.SetActive(endless);
            if (headerPanel != null) headerPanel.SetDesignHeight(endless ? endlessHeaderHeight : dailyHeaderHeight);
        }

        // m:ss, rewritten only when the shown second changes (no per-frame strings).
        // In the last seconds it turns red and ticks once per second.
        public void UpdateTimer(float secondsRemaining)
        {
            if (endlessTimerText == null) return;

            int seconds = Mathf.CeilToInt(secondsRemaining);
            if (seconds == shownTimerSeconds) return;
            shownTimerSeconds = seconds;

            endlessTimerText.text = $"{seconds / 60}:{seconds % 60:00}";
            bool warning = seconds <= timerWarningSeconds;
            endlessTimerText.color = warning ? timerWarningColor : timerNormalColor;
            if (warning && seconds > 0) AudioManager.Play(Sfx.TimerWarning);
        }

        // End panel title and Next/Retry/Main Menu label for the level just checked.
        public void ShowRoundResult(bool passed, bool runComplete)
        {
            if (resultTitleText != null)
                resultTitleText.text = runComplete ? runCompleteTitle : passed ? passedTitle : failedTitle;
            if (nextButtonLabel != null)
                nextButtonLabel.text = runComplete ? runCompleteLabel : passed ? nextLabel : retryLabel;
        }

        public void UpdateLevelText(string level)
        {
            TextMeshProUGUI target = endlessMode ? endlessLevelText : levelText;
            if (target != null)
                target.text = "LEVEL " + level;
        }

        // End panel stat cells: small dim label over a big gold value.
        private const string LabelStart = "<size=46%><color=#D9CCFFCC><cspace=0.12em>";
        private const string LabelEnd = "</cspace></color></size>\n<color=#FFC79A>";
        private const string ValueEnd = "</color>";

        private static string Stat(string label, string value)
        {
            return LabelStart + label + LabelEnd + value + ValueEnd;
        }

        public void RefreshScoreTexts()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            if (endlessMode)
            {
                if (endlessTotalScoreText != null)
                    endlessTotalScoreText.text = "TOTAL " + gm.EndlessTotalScore;
                if (currentScoreText != null)
                    currentScoreText.text = Stat("LEVEL SCORE", "+" + gm.LastLevelPoints);
                if (highestScoreText != null)
                    highestScoreText.text = Stat("TOTAL SCORE", gm.EndlessTotalScore.ToString());
                return;
            }

            if (currentScoreText != null)
                currentScoreText.text = Stat(gm.RunComplete ? "TOTAL SCORE" : "SCORE", gm.SessionScore.ToString("D3"));
            if (highestScoreText != null)
                highestScoreText.text = Stat("HIGHEST SCORE", gm.HighScore.ToString("D3"));
        }
    }
}
