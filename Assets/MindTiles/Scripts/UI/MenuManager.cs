using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MindTiles.Core;

namespace MindTiles.UI
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private MenuExitTransition exitTransition;

        [Header("Score Readout")]
        [Tooltip("Shows the player's Endless total score.")]
        [SerializeField] private TMP_Text scoreText;

        [Header("Settings Panel")]
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private ConfirmDialog confirmDialog;

        private void OnEnable()
        {
            if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(SettingsClose);
            if (resetProgressButton != null) resetProgressButton.onClick.AddListener(AskResetProgress);
            RefreshScore();
        }

        private void OnDisable()
        {
            if (settingsCloseButton != null) settingsCloseButton.onClick.RemoveListener(SettingsClose);
            if (resetProgressButton != null) resetProgressButton.onClick.RemoveListener(AskResetProgress);
        }

        private void RefreshScore()
        {
            if (scoreText != null)
                scoreText.text = "Score " + SaveSystem.Data.endless.totalScore;
        }

        private void AskResetProgress()
        {
            if (confirmDialog == null) return;

            confirmDialog.Show("Reset your score and progress?\nThis can't be undone.", () =>
            {
                SaveSystem.ResetProgress();
                RefreshScore();
            });
        }

        // Menu buttons: pick the mode, then leave the menu.
        public void PlayDailyStreak()
        {
            GameSession.Mode = GameMode.DailyStreak;
            Play();
        }

        public void PlayEndless()
        {
            GameSession.Mode = GameMode.Endless;
            Play();
        }

        private void Play()
        {
            if (exitTransition != null)
                exitTransition.Begin();
            else
                SceneManager.LoadScene(1);
        }

        // Pressing the button again closes its panel; opening one closes the other.
        public void InfoButton()
        {
            bool open = !infoPanel.activeSelf;
            infoPanel.SetActive(open);
            if (open) settingsPanel.SetActive(false);
        }

        public void InfoClose()
        {
            infoPanel.SetActive(false);
        }

        public void SettingsButton()
        {
            bool open = !settingsPanel.activeSelf;
            settingsPanel.SetActive(open);
            if (open) infoPanel.SetActive(false);
        }

        public void SettingsClose()
        {
            settingsPanel.SetActive(false);
        }
    }
}
