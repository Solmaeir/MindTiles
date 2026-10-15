using UnityEngine;

namespace MindTiles.UI
{
    // Stretches a panel out to chosen screen edges (past the safe area) while
    // the other edges stay exactly where the layout puts them. The end panel
    // extends left/right/bottom (its top is what the board is fitted against);
    // the game header extends left/right/top (its bottom is).
    //
    // Re-applies only when the safe area or canvas changes, and on enable
    // (the panel is hidden most of the time).
    [RequireComponent(typeof(RectTransform))]
    public class FillToScreenEdges : MonoBehaviour
    {
        [SerializeField] private bool extendLeft = true;
        [SerializeField] private bool extendRight = true;
        [SerializeField] private bool extendBottom = true;
        [SerializeField] private bool extendTop = false;

        private RectTransform panel;
        private RectTransform parent;
        private RectTransform canvasRect;

        private Vector2 designAnchoredPosition;
        private Vector2 designSize;
        private bool hasDesign;

        private Vector4 lastParentRect;
        private Vector4 lastCanvasRect;
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable()
        {
            Apply();
        }

        // Changes the designed height (e.g. a taller header in Endless) and re-applies.
        public void SetDesignHeight(float height)
        {
            if (panel == null) Apply();
            designSize.y = height;
            Apply();
        }

        private void LateUpdate()
        {
            if (parent == null || canvasRect == null) return;
            if (ReadRect(parent) != lastParentRect || ReadRect(canvasRect) != lastCanvasRect)
                Apply();
        }

        private void Apply()
        {
            if (panel == null)
            {
                panel = (RectTransform)transform;
                parent = panel.parent as RectTransform;
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null) canvasRect = (RectTransform)canvas.rootCanvas.transform;
            }
            if (parent == null || canvasRect == null) return;
            if (panel.anchorMin != panel.anchorMax) return;   // needs point anchors

            if (!hasDesign)
            {
                designAnchoredPosition = panel.anchoredPosition;
                designSize = panel.sizeDelta;
                hasDesign = true;
            }

            lastParentRect = ReadRect(parent);
            lastCanvasRect = ReadRect(canvasRect);

            // Edges from the designed layout...
            panel.anchoredPosition = designAnchoredPosition;
            panel.sizeDelta = designSize;
            panel.GetWorldCorners(corners);
            float left = corners[0].x, right = corners[2].x, bottom = corners[0].y, top = corners[1].y;

            // ...then the chosen ones pushed out to the whole screen (canvas).
            canvasRect.GetWorldCorners(corners);
            if (extendLeft) left = corners[0].x;
            if (extendRight) right = corners[2].x;
            if (extendBottom) bottom = corners[0].y;
            if (extendTop) top = corners[1].y;

            Vector3 lossy = panel.lossyScale;
            if (lossy.x <= 0f || lossy.y <= 0f || top <= bottom) return;

            float width = right - left;
            float height = top - bottom;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width / lossy.x);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height / lossy.y);

            Vector3 pos = panel.position;
            pos.x = left + panel.pivot.x * width;
            pos.y = bottom + panel.pivot.y * height;
            panel.position = pos;
        }

        // World rect as (minX, minY, maxX, maxY).
        private Vector4 ReadRect(RectTransform rt)
        {
            rt.GetWorldCorners(corners);
            return new Vector4(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
