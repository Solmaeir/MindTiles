using UnityEngine;
using UnityEngine.SceneManagement;

namespace MindTiles.UI
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject infoPanel;

        public void Play()
        {
            SceneManager.LoadScene(1);
        }

        public void InfoButton()
        {
            infoPanel.SetActive(true);
        }

        public void InfoClose()
        {
            infoPanel.SetActive(false);
        }

        public void SettingsButton()
        {
            settingsPanel.SetActive(true);
        }

        public void SettingsClose()
        {
            settingsPanel.SetActive(false);
        }
    }
}
