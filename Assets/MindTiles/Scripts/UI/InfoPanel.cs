using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MindTiles.Audio;

namespace MindTiles.UI
{
    // Menu "i" panel: a folder with two tabs, Info (who made it, version,
    // contact, social links) and How to Play. Pressing a tab shows its page and
    // colors the tab like the page, so it reads as the open folder tab.
    // Contact and social addresses are plain Inspector fields on the prefab.
    public class InfoPanel : MonoBehaviour
    {
        [Serializable]
        private class Tab
        {
            public Button button;
            public GameObject page;
            [Tooltip("Tab background pieces (the rounded top and the flat bottom): colored active / inactive.")]
            public Image[] fills;
            [Tooltip("Tab icon and label: colored active / inactive.")]
            public Graphic[] contents;
        }

        [SerializeField] private Tab[] tabs;
        [SerializeField] private Color activeFill = new Color(0.2f, 0.16f, 0.416f, 1f);
        [SerializeField] private Color inactiveFill = new Color(0.11f, 0.08f, 0.26f, 1f);
        [SerializeField] private Color activeContent = Color.white;
        [SerializeField] private Color inactiveContent = new Color(0.6f, 0.55f, 0.78f, 1f);
        [SerializeField] private Button closeButton;
        [SerializeField] private Button doneButton;
        [Tooltip("Resets to the top whenever the panel opens.")]
        [SerializeField] private ScrollRect howToPlayScroll;

        [Header("Info Page")]
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text emailText;
        [SerializeField] private Button emailButton;
        [SerializeField] private Button instagramButton;
        [SerializeField] private Button xButton;

        [Header("Contact And Links (edit these)")]
        [SerializeField] private string contactEmail = "pakuegames@gmail.com";
        [SerializeField] private string emailSubject = "MindTiles";
        [SerializeField] private string instagramUrl = "https://www.instagram.com/";
        [SerializeField] private string xUrl = "https://x.com/";

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;     // captured per tab
                if (tabs[i].button != null)
                    tabs[i].button.onClick.AddListener(() => OnTabPressed(index));
            }
        }

        private void OnEnable()
        {
            if (versionText != null) versionText.text = "Version " + Application.version;
            if (emailText != null) emailText.text = contactEmail;

            // Links without an address are hidden instead of opening nothing.
            if (emailButton != null) emailButton.gameObject.SetActive(!string.IsNullOrEmpty(contactEmail));
            if (instagramButton != null) instagramButton.gameObject.SetActive(!string.IsNullOrEmpty(instagramUrl));
            if (xButton != null) xButton.gameObject.SetActive(!string.IsNullOrEmpty(xUrl));

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (doneButton != null) doneButton.onClick.AddListener(Close);
            if (emailButton != null) emailButton.onClick.AddListener(OpenEmail);
            if (instagramButton != null) instagramButton.onClick.AddListener(OpenInstagram);
            if (xButton != null) xButton.onClick.AddListener(OpenX);

            ShowTab(0);
            if (howToPlayScroll != null) howToPlayScroll.verticalNormalizedPosition = 1f;
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (doneButton != null) doneButton.onClick.RemoveListener(Close);
            if (emailButton != null) emailButton.onClick.RemoveListener(OpenEmail);
            if (instagramButton != null) instagramButton.onClick.RemoveListener(OpenInstagram);
            if (xButton != null) xButton.onClick.RemoveListener(OpenX);
        }

        private void OnTabPressed(int index)
        {
            AudioManager.Play(Sfx.ButtonClick);
            ShowTab(index);
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = i == index;
                Tab tab = tabs[i];

                if (tab.page != null) tab.page.SetActive(active);
                foreach (Image fill in tab.fills)
                    if (fill != null) fill.color = active ? activeFill : inactiveFill;
                foreach (Graphic content in tab.contents)
                    if (content != null) content.color = active ? activeContent : inactiveContent;
            }
        }

        private void OpenEmail()
        {
            Application.OpenURL("mailto:" + contactEmail + "?subject=" + Uri.EscapeDataString(emailSubject));
        }

        private void OpenInstagram()
        {
            Application.OpenURL(instagramUrl);
        }

        private void OpenX()
        {
            Application.OpenURL(xUrl);
        }
    }
}
