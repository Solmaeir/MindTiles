using System.Collections;
using UnityEngine;

namespace MindTiles.UI
{
    // Cosmetic mascot animation, unchanged from the prototype aside from
    // namespacing and tidied naming.
    public class EyeAnimator : MonoBehaviour
    {
        [Header("Eye References")]
        [SerializeField] private RectTransform leftEye;
        [SerializeField] private RectTransform rightEye;

        [Header("Movement Settings")]
        [SerializeField] private float moveRangeX = 30f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float scaleRange = 0.15f;

        private Vector3 leftEyeStartPos;
        private Vector3 rightEyeStartPos;
        private Vector3 defaultScale;

        private void Start()
        {
            leftEyeStartPos = leftEye.localPosition;
            rightEyeStartPos = rightEye.localPosition;
            defaultScale = leftEye.localScale;

            StartCoroutine(EyeLoop());
        }

        private IEnumerator EyeLoop()
        {
            while (true)
            {
                int choice = Random.Range(0, 3);

                switch (choice)
                {
                    case 0:
                        yield return StartCoroutine(LookSideToSide());
                        break;
                    case 1:
                        yield return StartCoroutine(PulseEyes());
                        break;
                    case 2:
                        yield return StartCoroutine(CrossEyes());
                        break;
                }

                yield return new WaitForSeconds(Random.Range(1f, 2f));
            }
        }

        private IEnumerator LookSideToSide()
        {
            Vector3 rightOffset = new Vector3(moveRangeX, 0, 0);
            Vector3 leftOffset = new Vector3(-moveRangeX, 0, 0);

            yield return MoveEyes(leftEyeStartPos + rightOffset, rightEyeStartPos + rightOffset);
            yield return MoveEyes(leftEyeStartPos + leftOffset, rightEyeStartPos + leftOffset);
            yield return MoveEyes(leftEyeStartPos, rightEyeStartPos);
        }

        private IEnumerator PulseEyes()
        {
            Vector3 targetScale = defaultScale * (1f - scaleRange);
            yield return ScaleEyes(targetScale);
            yield return ScaleEyes(defaultScale);
        }

        private IEnumerator CrossEyes()
        {
            Vector3 leftOffset = new Vector3(-moveRangeX, 0, 0);
            Vector3 rightOffset = new Vector3(moveRangeX, 0, 0);

            yield return MoveEyes(leftEyeStartPos + leftOffset, rightEyeStartPos + rightOffset);
            yield return MoveEyes(leftEyeStartPos, rightEyeStartPos);
        }

        private IEnumerator MoveEyes(Vector3 leftTarget, Vector3 rightTarget)
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

        private IEnumerator ScaleEyes(Vector3 targetScale)
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
