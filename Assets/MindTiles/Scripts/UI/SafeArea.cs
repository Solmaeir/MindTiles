using UnityEngine;

namespace MindTiles.UI
{
    // Fits this RectTransform to Screen.safeArea (notches, rounded corners,
    // gesture bars). Anchors are set to the safe rect and offsets are zeroed,
    // so leftover Inspector offsets / sizeDelta can't push content around.
    //
    // Per edge you control how much of the device's inset is respected
    // (Inset Scale: 0 = ignore it, 1 = full) and can add your own padding
    // (% of screen size). Changing the values in the Inspector while playing
    // applies instantly. Re-applies on screen size / safe area changes too.
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        [Header("Device Inset Scale (0 = ignore notch/bar, 1 = full)")]
        [SerializeField, Range(0f, 1f)] private float topInsetScale = 1f;
        [SerializeField, Range(0f, 1f)] private float bottomInsetScale = 1f;
        [SerializeField, Range(0f, 1f)] private float leftInsetScale = 1f;
        [SerializeField, Range(0f, 1f)] private float rightInsetScale = 1f;

        [Header("Extra Padding (% of screen height for top/bottom, width for left/right)")]
        [SerializeField, Range(0f, 25f)] private float topPadding = 0f;
        [SerializeField, Range(0f, 25f)] private float bottomPadding = 0f;
        [SerializeField, Range(0f, 25f)] private float leftPadding = 0f;
        [SerializeField, Range(0f, 25f)] private float rightPadding = 0f;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool settingsDirty;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void OnValidate()
        {
            settingsDirty = true;
        }

        private void Update()
        {
            if (settingsDirty ||
                Screen.safeArea != lastSafeArea ||
                Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
                Apply();
        }

        private void Apply()
        {
            settingsDirty = false;

            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            float w = Screen.width;
            float h = Screen.height;
            if (w <= 0f || h <= 0f) return;

            // Device insets in pixels, scaled, plus the extra padding.
            float left = safe.xMin * leftInsetScale + w * leftPadding * 0.01f;
            float right = (w - safe.xMax) * rightInsetScale + w * rightPadding * 0.01f;
            float bottom = safe.yMin * bottomInsetScale + h * bottomPadding * 0.01f;
            float top = (h - safe.yMax) * topInsetScale + h * topPadding * 0.01f;

            rectTransform.anchorMin = new Vector2(left / w, bottom / h);
            rectTransform.anchorMax = new Vector2(1f - right / w, 1f - top / h);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
