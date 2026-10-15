using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MindTiles.Core;

namespace MindTiles.UI
{
    // Visual side of the pause panel prefab: fills in "Level N · Take a
    // breather" when shown and opens the shared settings panel from the gear.
    // Resume / Go to Menu / Reset are wired by ButtonManager.
    public class PausePanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsPanel settingsPanel;

        private void OnEnable()
        {
            if (subtitle != null && GameManager.Instance != null)
                subtitle.text = "Level " + GameManager.Instance.DisplayLevel + " · Take a breather";

            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        }

        private void OnDisable()
        {
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (settingsPanel != null && settingsPanel.gameObject.activeSelf) settingsPanel.Close();
        }

        private void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }
    }
}
