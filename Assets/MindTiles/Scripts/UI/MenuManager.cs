using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using MindTiles.Audio;
using MindTiles.Core;

namespace MindTiles.UI
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private MenuExitTransition exitTransition;

        private const string GameSceneName = "SampleScene";

        [Header("Score Readout")]
        [Tooltip("Shows the player's Endless total score.")]
        [SerializeField] private TMP_Text scoreText;

        private void OnEnable()
        {
            SettingsPanel.ProgressReset += RefreshScore;
            RefreshScore();
        }

        private void OnDisable()
        {
            SettingsPanel.ProgressReset -= RefreshScore;
        }

        private void RefreshScore()
        {
            if (scoreText != null)
                scoreText.text = SaveSystem.Data.endless.totalScore.ToString();
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
                SceneManager.LoadScene(GameSceneName);
        }

        // Pressing the button again closes its panel; opening one closes the other.
        // These play their own open/close sound (no UISound on these two buttons).
        public void InfoButton()
        {
            bool open = !infoPanel.activeSelf;
            infoPanel.SetActive(open);
            if (open) settingsPanel.SetActive(false);
            AudioManager.Play(open ? Sfx.PanelOpen : Sfx.PanelClose);
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
            AudioManager.Play(open ? Sfx.PanelOpen : Sfx.PanelClose);
        }

        public void SettingsClose()
        {
            settingsPanel.SetActive(false);
        }
    }
}
