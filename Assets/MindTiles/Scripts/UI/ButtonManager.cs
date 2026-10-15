using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using MindTiles.Audio;
using MindTiles.Core;

namespace MindTiles.UI
{
    // Thin input layer for the game scene: it only wires buttons to actions.
    // Game rules live elsewhere (GameManager = flow/pause, HintManager = hints,
    // PatternValidator = checking), so this class never decides anything itself.
    //
    // All listeners are added in OnEnable and removed in OnDisable, so there is
    // no need for (and no point adding) OnClick entries in the Inspector.
    public class ButtonManager : MonoBehaviour
    {
        [Header("Gameplay Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button secondHintButton;

        [Header("Hint Count Badges (red circle, top-right of each hint button)")]
        [SerializeField] private TMP_Text hintCountText;
        [SerializeField] private TMP_Text secondHintCountText;

        [Header("Pause Panel Buttons")]
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button pauseResetButton;
        [SerializeField] private ConfirmDialog confirmDialog;

        [Header("Result Panel Buttons")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button endMenuButton;

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;

        [Header("Systems")]
        [SerializeField] private HintManager hintManager;

        private void OnEnable()
        {
            Bind(pauseButton, PauseGame);
            Bind(resumeButton, ResumeGame);
            Bind(backButton, BackToMenu);
            Bind(checkButton, CheckBoard);
            Bind(hintButton, UseHint);
            Bind(secondHintButton, UseSecondHint);
            Bind(nextLevelButton, NextLevel);
            Bind(endMenuButton, BackToMenu);
            Bind(pauseMenuButton, BackToMenu);
            Bind(pauseResetButton, AskResetProgress);

            GameManager.RoundFinished += HideRoundButtons;
            GameManager.RoundStarted += ShowRoundButtons;
            GameManager.HintCountsChanged += RefreshHintCounts;
            RefreshHintCounts();
        }

        private void OnDisable()
        {
            Unbind(pauseButton, PauseGame);
            Unbind(resumeButton, ResumeGame);
            Unbind(backButton, BackToMenu);
            Unbind(checkButton, CheckBoard);
            Unbind(hintButton, UseHint);
            Unbind(secondHintButton, UseSecondHint);
            Unbind(nextLevelButton, NextLevel);
            Unbind(endMenuButton, BackToMenu);
            Unbind(pauseMenuButton, BackToMenu);
            Unbind(pauseResetButton, AskResetProgress);

            GameManager.RoundFinished -= HideRoundButtons;
            GameManager.RoundStarted -= ShowRoundButtons;
            GameManager.HintCountsChanged -= RefreshHintCounts;
        }

        private void Start()
        {
            // GameManager.Instance exists by now (it sets itself in Awake); OnEnable may have run first.
            RefreshHintCounts();
        }

        // Badge numbers + greying out a hint button once its hints are used up.
        private void RefreshHintCounts()
        {
            if (GameManager.Instance == null) return;

            ApplyCount(hintButton, hintCountText, GameManager.Instance.GetHintCount(HintType.OneTile));
            ApplyCount(secondHintButton, secondHintCountText, GameManager.Instance.GetHintCount(HintType.AllTiles));
        }

        private static void ApplyCount(Button button, TMP_Text badge, int count)
        {
            if (badge != null) badge.text = count.ToString();
            if (button != null) button.interactable = count > 0;
        }

        // Check + both hint buttons are only usable while a round is being played.
        private void HideRoundButtons() => SetRoundButtonsVisible(false);
        private void ShowRoundButtons() => SetRoundButtonsVisible(true);

        private void SetRoundButtonsVisible(bool visible)
        {
            if (checkButton != null) checkButton.gameObject.SetActive(visible);
            if (hintButton != null) hintButton.gameObject.SetActive(visible);
            if (secondHintButton != null) secondHintButton.gameObject.SetActive(visible);
        }

        private static void Bind(Button button, UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Unbind(Button button, UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private void PauseGame()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.SetPaused(true);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        private void ResumeGame()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.SetPaused(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void BackToMenu()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GoToMainMenu();
        }

        private void AskResetProgress()
        {
            if (confirmDialog == null || GameManager.Instance == null) return;

            confirmDialog.Show("Reset your score and progress?\nThis can't be undone.",
                GameManager.Instance.ResetProgressAndRestart);
        }

        private void NextLevel()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.CallLevel();
        }

        private void CheckBoard()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.FinishRound();
        }

        private void UseHint()
        {
            if (hintManager != null) PlayHintResult(hintManager.UseOneTileHint());
        }

        private void UseSecondHint()
        {
            if (hintManager != null) PlayHintResult(hintManager.UseAllTilesHint());
        }

        // Hint buttons have no UISound: whether the hint went through decides the sound.
        private static void PlayHintResult(bool used)
        {
            AudioManager.Play(used ? Sfx.HintUsed : Sfx.HintUnavailable);
        }
    }
}
