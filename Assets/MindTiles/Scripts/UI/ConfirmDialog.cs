using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindTiles.UI
{
    // Reusable "Are you sure?" pop-up. Put this on the dialog's root object
    // (inactive by default; Show() activates it). The callback runs only when
    // the player presses Yes; No / closing just hides the dialog.
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onConfirm;

        public void Show(string message, Action confirmAction)
        {
            onConfirm = confirmAction;
            if (messageText != null) messageText.text = message;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // always on top of the panels it was opened from
        }

        public void Hide()
        {
            onConfirm = null;
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (yesButton != null) yesButton.onClick.AddListener(Confirm);
            if (noButton != null) noButton.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            if (yesButton != null) yesButton.onClick.RemoveListener(Confirm);
            if (noButton != null) noButton.onClick.RemoveListener(Hide);
        }

        private void Confirm()
        {
            Action action = onConfirm;
            Hide();
            action?.Invoke();
        }
    }
}
