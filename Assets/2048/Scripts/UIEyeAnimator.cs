using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Legacy2048
{
public class EyeAnimator : MonoBehaviour
{
    [Header("Eye References")]
    public RectTransform leftEye;   // sol g�z bebe�i
    public RectTransform rightEye;  // sa� g�z bebe�i

    [Header("Movement Settings")]
    public float moveRangeX = 30f; // sa�a-sola kayma aral���
    public float moveSpeed = 2f;   // hareket h�z�
    public float scaleRange = 0.15f; // k���lme-b�y�me oran�

    private Vector3 leftEyeStartPos;
    private Vector3 rightEyeStartPos;
    private Vector3 defaultScale;

    void Start()
    {
        // Ba�lang�� pozisyonlar�n� ve scale�� kaydet
        leftEyeStartPos = leftEye.localPosition;
        rightEyeStartPos = rightEye.localPosition;
        defaultScale = leftEye.localScale;

        // Animasyon d�ng�s�n� ba�lat
        StartCoroutine(EyeLoop());
    }

    IEnumerator EyeLoop()
    {
        while (true)
        {
            int choice = Random.Range(0, 3);

            switch (choice)
            {
                case 0: // 1) Birlikte sa�a sola bakma
                    yield return StartCoroutine(LookSideToSide());
                    break;

                case 1: // 2) Ortada k���l�p b�y�me
                    yield return StartCoroutine(PulseEyes());
                    break;

                case 2: // 3) �a�� bakma
                    yield return StartCoroutine(CrossEyes());
                    break;
            }

            // Hareketler aras�nda biraz bekle
            yield return new WaitForSeconds(Random.Range(1f, 2f));
        }
    }

    IEnumerator LookSideToSide()
    {
        Vector3 rightOffset = new Vector3(moveRangeX, 0, 0);
        Vector3 leftOffset = new Vector3(-moveRangeX, 0, 0);

        // sa�a bak
        yield return MoveEyes(leftEyeStartPos + rightOffset, rightEyeStartPos + rightOffset);
        // sola bak
        yield return MoveEyes(leftEyeStartPos + leftOffset, rightEyeStartPos + leftOffset);
        // tekrar merkeze d�n
        yield return MoveEyes(leftEyeStartPos, rightEyeStartPos);
    }

    IEnumerator PulseEyes()
    {
        Vector3 targetScale = defaultScale * (1f - scaleRange);
        yield return ScaleEyes(targetScale);
        yield return ScaleEyes(defaultScale);
    }

    IEnumerator CrossEyes()
    {
        Vector3 leftOffset = new Vector3(-moveRangeX, 0, 0);
        Vector3 rightOffset = new Vector3(moveRangeX, 0, 0);

        // biri sola biri sa�a
        yield return MoveEyes(leftEyeStartPos + leftOffset, rightEyeStartPos + rightOffset);
        // tekrar merkeze d�n
        yield return MoveEyes(leftEyeStartPos, rightEyeStartPos);
    }

    IEnumerator MoveEyes(Vector3 leftTarget, Vector3 rightTarget)
    {
        float t = 0;
        Vector3 leftStart = leftEye.localPosition;
        Vector3 rightStart = rightEye.localPosition;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            leftEye.localPosition = Vector3.Lerp(leftStart, leftTarget, Mathf.SmoothStep(0, 1, t));
            rightEye.localPosition = Vector3.Lerp(rightStart, rightTarget, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
    }

    IEnumerator ScaleEyes(Vector3 targetScale)
    {
        float t = 0;
        Vector3 leftStart = leftEye.localScale;
        Vector3 rightStart = rightEye.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            leftEye.localScale = Vector3.Lerp(leftStart, targetScale, Mathf.SmoothStep(0, 1, t));
            rightEye.localScale = Vector3.Lerp(rightStart, targetScale, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
    }
}
}
