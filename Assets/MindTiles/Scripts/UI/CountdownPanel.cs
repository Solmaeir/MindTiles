using System.Collections;
using TMPro;
using UnityEngine;
using MindTiles.Audio;

namespace MindTiles.UI
{
    public class CountdownPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI countdownText;

        public IEnumerator ShowCountdown(float totalWaitTime)
        {
            panel.SetActive(true);
            countdownText.text = "";

            float preWait = Mathf.Max(0, totalWaitTime - 3f);
            if (preWait > 0)
                yield return new WaitForSeconds(preWait);

            // One clip covers the whole 3-2-1.
            AudioManager.PlayCountdown();
            for (int count = 3; count > 0; count--)
            {
                countdownText.text = count.ToString();
                yield return new WaitForSeconds(1f);
            }

            countdownText.text = "GO!";
            AudioManager.Play(Sfx.CountdownGo);
            yield return new WaitForSeconds(1f);

            panel.SetActive(false);
        }
    }
}
