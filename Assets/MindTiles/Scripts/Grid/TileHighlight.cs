using System.Collections;
using UnityEngine;

namespace MindTiles.Grid
{
    // Purely visual: paints the tile according to a correctness verdict it's
    // handed, never computes one itself. PatternValidator owns what "correct"
    // means (type-at-position matching), so this stays a dumb repaint.
    [RequireComponent(typeof(TileController))]
    public class TileHighlight : MonoBehaviour
    {
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color incorrectColor = Color.red;
        [SerializeField] private Color neutralColor = Color.white;
        [SerializeField] private Color hintColor = Color.yellow;

        private SpriteRenderer sr;
        private bool lastIsCorrect;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void SetNeutral()
        {
            if (sr != null) sr.color = neutralColor;
        }

        public void SetCorrect(bool isCorrect)
        {
            lastIsCorrect = isCorrect;
            if (sr != null)
                sr.color = isCorrect ? correctColor : incorrectColor;
        }

        public void FlashHint(float duration)
        {
            StartCoroutine(FlashHintRoutine(duration));
        }

        private IEnumerator FlashHintRoutine(float duration)
        {
            if (sr == null) yield break;
            sr.color = hintColor;
            yield return new WaitForSeconds(duration);
            sr.color = lastIsCorrect ? correctColor : incorrectColor;
        }
    }
}
