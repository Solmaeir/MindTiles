using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MindTiles.Core;

namespace MindTiles.UI
{
    // The weekly streak on the menu: 7 tile slots like the loading bar. A slot
    // is colored (pink / peach / lavender in turn) for every day of the current
    // streak week, the next one is dashed, the rest are empty. Reads everything
    // from DailyStreak, so it only needs to refresh when shown or after a reset.
    public class StreakBar : MonoBehaviour
    {
        [SerializeField] private Image[] slots;
        [SerializeField] private Sprite[] filledSprites;
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite nextSprite;
        [Tooltip("\"3 / 7\"")]
        [SerializeField] private TMP_Text countText;

        private void OnEnable()
        {
            SettingsPanel.ProgressReset += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            SettingsPanel.ProgressReset -= Refresh;
        }

        public void Refresh()
        {
            int filled = DailyStreak.FilledBoxes;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                if (i < filled && filledSprites.Length > 0)
                    slots[i].sprite = filledSprites[i % filledSprites.Length];
                else
                    slots[i].sprite = i == filled ? nextSprite : emptySprite;
            }

            if (countText != null)
                countText.text = filled + " / " + DailyStreak.WeekLength;
        }
    }
}
