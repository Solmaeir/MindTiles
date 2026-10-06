using System.Collections.Generic;
using UnityEngine;
using MindTiles.Validation;

namespace MindTiles.Grid
{
    // Draws tile marks for the round-end reveal (also reused for hints).
    // Correct cells: light green wash + green check mark in the middle.
    // Misplaced tiles: translucent red wash + a red arrow from the tile's center
    // to the center of the cell it belongs in.
    // How it all looks comes from a shared MarkStyle asset.
    //
    // Marks come from a small pool and are reused, so showing a result
    // allocates nothing after the first time. Stays up until Clear().
    public class RoundResultOverlay : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Material maskMaterial;
        [SerializeField] private int sortingOrder = 3;
        [Tooltip("Shared look for all marks (colors, sizes). Edit the asset, not this component.")]
        [SerializeField] private MarkStyle style;

        // Icons are drawn big and mip-mapped so they stay smooth when shrunk.
        private const int IconTextureSize = 256;
        private const int LineTextureHeight = 64;

        private static Sprite squareSprite;
        private static Sprite checkSprite;
        private static Sprite lineSprite;
        private Sprite arrowHeadSprite;
        private float arrowHeadStroke = -1f;

        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private readonly Dictionary<int, Vector2Int> destinationByPair = new Dictionary<int, Vector2Int>();
        private List<CellMark> shownMarks;
        private bool styleDirty;
        private int used;

        private MarkStyle Style
        {
            get
            {
                if (style == null) style = ScriptableObject.CreateInstance<MarkStyle>();
                return style;
            }
        }

        // Follows the board when it moves/resizes, and the style when it is edited.
        private void OnEnable()
        {
            if (gridManager != null) gridManager.LayoutChanged += Refresh;
            MarkStyle.Changed += MarkStyleChanged;
        }

        private void OnDisable()
        {
            if (gridManager != null) gridManager.LayoutChanged -= Refresh;
            MarkStyle.Changed -= MarkStyleChanged;
        }

        private void MarkStyleChanged()
        {
            styleDirty = true;
        }

        private void OnValidate()
        {
            styleDirty = true;
        }

        private void Update()
        {
            if (!styleDirty) return;
            styleDirty = false;
            Refresh();
        }

        // Redraws the current marks (new style / new screen layout). No-op when nothing is shown.
        public void Refresh()
        {
            if (shownMarks != null)
                Show(shownMarks);
        }

        public void Show(List<CellMark> marks)
        {
            Clear();
            shownMarks = marks;

            MarkStyle s = Style;
            float cell = gridManager.CurrentCellSize;
            float wash = cell * (1f - 2f * s.washInset);

            // Where each misplaced tile belongs (Destination marks share the tile's pairId).
            destinationByPair.Clear();
            for (int i = 0; i < marks.Count; i++)
                if (marks[i].kind == MarkKind.Destination)
                    destinationByPair[marks[i].pairId] = marks[i].cell;

            for (int i = 0; i < marks.Count; i++)
            {
                CellMark m = marks[i];
                if (m.kind == MarkKind.Destination) continue;   // only used as the arrow's target

                Vector3 pos = gridManager.GetWorldPositionFromGridCoordinate(m.cell);

                if (m.kind == MarkKind.Correct)
                {
                    Place(GetSquare(), pos, wash, 0f, WithAlpha(s.correctColor, s.correctAlpha));
                    if (s.showCheckMark)
                        Place(GetCheck(), pos, cell * s.checkSize, 0f, s.checkColor, 1);
                    continue;
                }

                // WrongTile
                Place(GetSquare(), pos, wash, 0f, WithAlpha(s.wrongColor, s.wrongAlpha));
                if (s.showArrow && destinationByPair.TryGetValue(m.pairId, out Vector2Int target))
                    DrawArrow(pos, gridManager.GetWorldPositionFromGridCoordinate(target), cell, s);
            }
        }

        public void Clear()
        {
            shownMarks = null;
            for (int i = 0; i < used; i++)
                pool[i].gameObject.SetActive(false);
            used = 0;
        }

        private static Color WithAlpha(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        // Straight arrow from one cell center to another: a round-capped line
        // plus an arrow head whose tip sits exactly on the target's center.
        private void DrawArrow(Vector3 from, Vector3 to, float cell, MarkStyle s)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.0001f) return;

            Vector3 dir = delta / length;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float thickness = cell * s.arrowThickness;
            float headScale = cell * s.arrowHeadSize;

            // Line stops under the head's rounded tip.
            float lineLength = Mathf.Max(thickness, length - thickness * 0.5f);
            PlaceLine(from + dir * (lineLength * 0.5f), lineLength, thickness, angle, s.arrowColor);

            // Head strokes as thick as the line.
            Place(GetArrowHead(s.arrowThickness / (2f * s.arrowHeadSize)), to, headScale, angle - 90f, s.arrowColor, 1);
        }

        private void Place(Sprite sprite, Vector3 position, float size, float rotationZ, Color color, int orderOffset = 0)
        {
            SpriteRenderer sr = Acquire(orderOffset);
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sprite = sprite;
            sr.transform.position = position;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            sr.transform.localScale = new Vector3(size, size, 1f);
            sr.color = color;
        }

        // 9-sliced capsule: the round ends keep their shape however long the line is.
        private void PlaceLine(Vector3 center, float length, float thickness, float rotationZ, Color color)
        {
            SpriteRenderer sr = Acquire(1);
            sr.sprite = GetLine();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.transform.position = center;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            sr.transform.localScale = new Vector3(thickness, thickness, 1f);
            sr.size = new Vector2(length / thickness, 1f);   // sprite is 1 unit tall
            sr.color = color;
        }

        private SpriteRenderer Acquire(int orderOffset)
        {
            if (used == pool.Count)
            {
                var go = new GameObject("Mask");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (maskMaterial != null) sr.sharedMaterial = maskMaterial;
                pool.Add(sr);
            }

            SpriteRenderer result = pool[used++];
            result.sortingOrder = sortingOrder + orderOffset;
            result.gameObject.SetActive(true);
            return result;
        }

        // ---- Generated sprites -------------------------------------------------

        // 1x1 white pixel at 1 pixel-per-unit = a 1x1 world-unit square we can scale to the cell size.
        private static Sprite GetSquare()
        {
            if (squareSprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return squareSprite;
        }

        private static Sprite GetCheck()
        {
            if (checkSprite == null)
            {
                var a = new Vector2(0.18f, 0.52f);
                var b = new Vector2(0.40f, 0.28f);
                var c = new Vector2(0.84f, 0.76f);
                checkSprite = DrawStrokes(0.075f, (a, b), (b, c));
            }
            return checkSprite;
        }

        // Arrow head pointing up with its tip at the sprite's exact center, so
        // placing it on a cell center puts the tip on that center. Rebuilt only
        // when the stroke weight changes.
        private Sprite GetArrowHead(float halfStroke)
        {
            halfStroke = Mathf.Clamp(halfStroke, 0.01f, 0.2f);
            if (arrowHeadSprite == null || !Mathf.Approximately(halfStroke, arrowHeadStroke))
            {
                var tip = new Vector2(0.5f, 0.5f);
                var left = new Vector2(0.22f, 0.20f);
                var right = new Vector2(0.78f, 0.20f);
                arrowHeadSprite = DrawStrokes(halfStroke, (tip, left), (tip, right));
                arrowHeadStroke = halfStroke;
            }
            return arrowHeadSprite;
        }

        // Capsule (2:1) with soft edges, sliced so only its straight middle stretches.
        private static Sprite GetLine()
        {
            if (lineSprite == null)
            {
                int h = LineTextureHeight;
                int w = h * 2;
                var tex = NewTexture(w, h);
                float r = h * 0.5f;
                var pixels = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        float cx = Mathf.Clamp(px, r, w - r);
                        float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, r));
                        float alpha = Mathf.Clamp01(r - d + 0.5f - 1f);   // 1px soft edge
                        pixels[y * w + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply(true);
                lineSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h,
                    0, SpriteMeshType.FullRect, new Vector4(r, 0f, r, 0f));
            }
            return lineSprite;
        }

        // White, anti-aliased rounded strokes in a 1x1 world-unit sprite.
        private static Sprite DrawStrokes(float halfThickness, params (Vector2 from, Vector2 to)[] strokes)
        {
            int size = IconTextureSize;
            var tex = NewTexture(size, size);
            float edge = 1.5f / size;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float d = float.MaxValue;
                    for (int s = 0; s < strokes.Length; s++)
                        d = Mathf.Min(d, DistanceToSegment(p, strokes[s].from, strokes[s].to));

                    float alpha = Mathf.Clamp01((halfThickness - d) / edge + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Mip-mapped, trilinear: stays smooth when drawn much smaller than its texture.
        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 2
            };
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
