using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindTiles.Boot
{
    // First screen of the app (replaces the engine splash). Loads the menu in
    // the background and shows the tile progress bar: filled slots cycle
    // pink / peach / lavender, the next slot is dashed. Stays up for a short
    // minimum so it never just flashes.
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private string nextScene = "MenuSpace";
        [SerializeField] private float minDisplaySeconds = 1.6f;

        [Header("Progress Bar")]
        [SerializeField] private Image[] slots;
        [SerializeField] private Sprite[] filledSprites;
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite nextSprite;
        [SerializeField] private TMP_Text percentText;

        private const float LoadDoneProgress = 0.9f;   // Unity stops here until activation is allowed

        private IEnumerator Start()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
            op.allowSceneActivation = false;

            float elapsed = 0f;
            int shown = -1;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float loaded = Mathf.Clamp01(op.progress / LoadDoneProgress);
                float timed = Mathf.Clamp01(elapsed / minDisplaySeconds);
                float progress = Mathf.Min(loaded, timed);

                int percent = Mathf.RoundToInt(progress * 100f);
                if (percent != shown)
                {
                    shown = percent;
                    if (percentText != null) percentText.text = percent + "%";
                    UpdateSlots(progress);
                }

                if (loaded >= 1f && timed >= 1f) break;
                yield return null;
            }

            op.allowSceneActivation = true;
        }

        private void UpdateSlots(float progress)
        {
            if (slots == null || slots.Length == 0) return;

            int filled = Mathf.FloorToInt(progress * slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                if (i < filled && filledSprites.Length > 0)
                    slots[i].sprite = filledSprites[i % filledSprites.Length];
                else if (i == filled)
                    slots[i].sprite = nextSprite;
                else
                    slots[i].sprite = emptySprite;
            }
        }
    }
}
