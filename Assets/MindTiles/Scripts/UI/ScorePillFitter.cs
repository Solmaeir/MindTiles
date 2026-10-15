using TMPro;
using UnityEngine;

namespace MindTiles.UI
{
    // Menu score pill: widens to fit the score (the font size never changes).
    // The pill is centered, so it grows to both sides equally. Only re-fits
    // when the text actually changes; idle cost is one reference compare.
    [RequireComponent(typeof(RectTransform))]
    public class ScorePillFitter : MonoBehaviour
    {
        [SerializeField] private TMP_Text valueText;
        [Tooltip("The small \"SCORE\" caption above the value (the pill fits whichever is wider).")]
        [SerializeField] private TMP_Text labelText;
        [Tooltip("Where the texts start, measured from the pill's left edge (right of the trophy).")]
        [SerializeField] private float textLeft = 122f;
        [SerializeField] private float rightPadding = 40f;
        [Tooltip("The pill never gets narrower than this.")]
        [SerializeField] private float minWidth = 330f;

        private string fittedText;

        private void OnEnable()
        {
            fittedText = null;
            Fit();
        }

        private void LateUpdate()
        {
            if (valueText != null && !ReferenceEquals(valueText.text, fittedText))
                Fit();
        }

        public void Fit()
        {
            if (valueText == null) return;
            fittedText = valueText.text;

            float textWidth = valueText.GetPreferredValues(valueText.text).x;
            if (labelText != null)
                textWidth = Mathf.Max(textWidth, labelText.GetPreferredValues(labelText.text).x);

            RectTransform valueRt = valueText.rectTransform;
            valueRt.sizeDelta = new Vector2(textWidth, valueRt.sizeDelta.y);
            var pill = (RectTransform)transform;
            pill.sizeDelta = new Vector2(Mathf.Max(minWidth, textLeft + textWidth + rightPadding), pill.sizeDelta.y);
        }
    }
}
