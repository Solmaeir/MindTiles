using System;
using UnityEngine;
using UnityEngine.UI;
using MindTiles.Audio;
using MindTiles.Core;

namespace MindTiles.UI
{
    // The one settings panel, used from the menu and from the in-game pause
    // panel (same prefab in both scenes). Sound / music / vibration are saved
    // to the save file right away (AudioManager picks up the new volumes).
    // "Reset progress" asks first; in the game scene it restarts the run, in
    // the menu it just raises ProgressReset.
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private ToggleSwitch soundToggle;
        [SerializeField] private ToggleSwitch musicToggle;
        [SerializeField] private ToggleSwitch vibrationToggle;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button doneButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private ConfirmDialog confirmDialog;

        // Raised after a reset outside the game scene (the menu refreshes its score).
        public static event Action ProgressReset;

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (confirmDialog != null) confirmDialog.Hide();
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (gameObject.activeSelf) Close();
            else Open();
        }

        private void OnEnable()
        {
            SettingsData s = SaveSystem.Data.settings;
            Init(soundToggle, s.sfxVolume > 0f, OnSoundChanged);
            Init(musicToggle, s.musicVolume > 0f, OnMusicChanged);
            Init(vibrationToggle, s.vibration, OnVibrationChanged);

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (doneButton != null) doneButton.onClick.AddListener(Close);
            if (resetButton != null) resetButton.onClick.AddListener(AskReset);
        }

        private void OnDisable()
        {
            if (soundToggle != null) soundToggle.Changed -= OnSoundChanged;
            if (musicToggle != null) musicToggle.Changed -= OnMusicChanged;
            if (vibrationToggle != null) vibrationToggle.Changed -= OnVibrationChanged;

            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (doneButton != null) doneButton.onClick.RemoveListener(Close);
            if (resetButton != null) resetButton.onClick.RemoveListener(AskReset);
        }

        private static void Init(ToggleSwitch toggle, bool value, Action<bool> handler)
        {
            if (toggle == null) return;
            toggle.SetIsOn(value, false);
            toggle.Snap();
            toggle.Changed += handler;
        }

        private static void OnSoundChanged(bool on)
        {
            SaveSystem.Data.settings.sfxVolume = on ? 1f : 0f;
            SaveSystem.Save();
            AudioManager.RefreshVolumes();
        }

        private static void OnMusicChanged(bool on)
        {
            SaveSystem.Data.settings.musicVolume = on ? 1f : 0f;
            SaveSystem.Save();
            AudioManager.RefreshVolumes();
        }

        private static void OnVibrationChanged(bool on)
        {
            SaveSystem.Data.settings.vibration = on;
            SaveSystem.Save();
        }

        private void AskReset()
        {
            if (confirmDialog == null) return;

            confirmDialog.Show("Reset your score and progress?\nThis can't be undone.", () =>
            {
                if (GameManager.Instance != null)
                {
                    // In the game: wipe and restart the run in the same mode.
                    GameManager.Instance.ResetProgressAndRestart();
                    return;
                }

                SaveSystem.ResetProgress();
                ProgressReset?.Invoke();
            });
        }
    }
}
