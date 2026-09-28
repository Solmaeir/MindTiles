using UnityEngine;
using TMPro;
using System.Collections;

namespace Legacy2048
{
public class CountdownPanel : MonoBehaviour
{
    public GameObject panel;       // UI Panel
    public TextMeshProUGUI text;   // Saya� yaz�s�

    public IEnumerator ShowCountdown(float totalWaitTime)
    {
        panel.SetActive(true);
        text.text = ""; // ba�ta bo� kals�n

        // Saya�tan �nceki bekleme (totalWaitTime - 3 saniye)
        float preWait = Mathf.Max(0, totalWaitTime - 3f);
        if (preWait > 0)
            yield return new WaitForSeconds(preWait);

        // Son 3 saniye i�in saya�
        for (int count = 3; count > 0; count--)
        {
            text.text = count.ToString();
            yield return new WaitForSeconds(1f);
        }

        text.text = "GO!";
        yield return new WaitForSeconds(1f);

        panel.SetActive(false);
    }
}
}
