using UnityEngine;
using UnityEngine.UI;
using MindTiles.Core;
using MindTiles.Grid;
using MindTiles.Validation;

namespace MindTiles.UI
{
    public class ButtonManager : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button checkButton;

        [SerializeField] private GameObject pausePanel;

        private void Awake()
        {
            pauseButton?.onClick.AddListener(PauseGame);
            backButton?.onClick.AddListener(BackToMenu);
            hintButton?.onClick.AddListener(HintAny);
            checkButton?.onClick.AddListener(CheckBoard);
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        public void BackToMenu()
        {
            GameManager.Instance?.GoToMainMenu();
        }

        public void HintAny()
        {
            TileController hintTile = PatternValidator.Instance?.GetHintTile();
            hintTile?.GetComponent<TileHighlight>()?.FlashHint(1f);
        }

        public void CheckBoard()
        {
            PatternValidator.Instance?.CheckAndFinishRound();
        }
    }
}
