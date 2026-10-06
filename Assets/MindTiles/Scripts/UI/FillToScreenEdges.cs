using UnityEngine;

namespace MindTiles.UI
{
    // Stretches a panel out to the screen's left, right and bottom edges
    // (past the safe area), while its top edge stays exactly where the layout
    // puts it. Used for the end panel so it reaches the bottom of the screen
    // with no gap, without moving the edge the board is fitted against.
    //
    // Re-applies only when the safe area or canvas changes, and on enable
    // (the panel is hidden most of the time).
    [RequireComponent(typeof(RectTransform))]
    public class FillToScreenEdges : MonoBehaviour
    {
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

            // Top edge from the designed layout.
            panel.anchoredPosition = designAnchoredPosition;
            panel.sizeDelta = designSize;
            panel.GetWorldCorners(corners);
            float top = corners[1].y;

            // Left, right and bottom from the whole screen (canvas).
            canvasRect.GetWorldCorners(corners);
            float left = corners[0].x;
            float right = corners[2].x;
            float bottom = corners[0].y;

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
