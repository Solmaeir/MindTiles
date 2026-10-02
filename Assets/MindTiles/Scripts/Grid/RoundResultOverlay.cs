using System.Collections.Generic;
using UnityEngine;
using MindTiles.Validation;

namespace MindTiles.Grid
{
    // Round-end reveal. Correct cells get a light green wash. Every misplaced
    // tile gets a unique color over the tile itself, and the cell it belongs
    // in gets the same full-cell color, so the player can see which tile
    // should go where. A cell can play both roles at once (it holds a misplaced
    // tile AND is another tile's destination); it is then split diagonally:
    // upper-left shows the tile's pair color, lower-right the destination's.
    // The overlay stays up until Clear() (next level).
    //
    // Squares and triangles come from a small pool and are reused, so showing
    // a result allocates nothing after the first round.
    public class RoundResultOverlay : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Material maskMaterial;
        [SerializeField] private int sortingOrder = 3;

        [Header("Highlight Opacity (tune these)")]
        [Tooltip("Opacity of the green wash on correct tiles. 0 = invisible, 1 = solid.")]
        [SerializeField, Range(0f, 1f)] private float correctAlpha = 0.4f;
        [Tooltip("Opacity of the pair colors on misplaced tiles and their target cells. 0 = invisible, 1 = solid.")]
        [SerializeField, Range(0f, 1f)] private float pairAlpha = 0.75f;

        [Header("Colors")]
        [Tooltip("Only RGB is used; opacity comes from Correct Alpha above.")]
        [SerializeField] private Color correctColor = new Color(0.2f, 0.9f, 0.3f, 1f);

        private const float GoldenRatio = 0.618034f;
        // Hue band reserved for green (correct color); pair colors skip it.
        private const float GreenStart = 0.18f;
        private const float GreenEnd = 0.46f;

        private const int TriangleSize = 128;

        private struct CellRoles
        {
            public int tilePair;
            public int destPair;
        }

        private static Sprite squareSprite;
        private static Sprite triangleSprite;

        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private readonly Dictionary<Vector2Int, CellRoles> roles = new Dictionary<Vector2Int, CellRoles>();
        private List<CellMark> shownMarks;
        private bool opacityDirty;
        private int used;

        // Follows the board when it moves/resizes (rotation, safe area edits...).
        private void OnEnable()
        {
            if (gridManager != null) gridManager.LayoutChanged += Refresh;
        }

        private void OnDisable()
        {
            if (gridManager != null) gridManager.LayoutChanged -= Refresh;
        }

        // Editing the opacity sliders while playing refreshes the visible reveal.
        private void OnValidate()
        {
            opacityDirty = true;
        }

        private void Update()
        {
            if (!opacityDirty) return;
            opacityDirty = false;
            Refresh();
        }

        // Redraws the current reveal (new colors / new screen layout). No-op when nothing is shown.
        public void Refresh()
        {
            if (shownMarks != null)
                Show(shownMarks);
        }

        public void Show(List<CellMark> marks)
        {
            Clear();
            roles.Clear();
            shownMarks = marks;

            float cell = gridManager.CurrentCellSize;

            for (int i = 0; i < marks.Count; i++)
            {
                CellMark m = marks[i];
                if (m.kind == MarkKind.Correct)
                {
                    Place(GetSquare(), gridManager.GetWorldPositionFromGridCoordinate(m.cell), cell, 0f, WithAlpha(correctColor, correctAlpha));
                    continue;
                }

                if (!roles.TryGetValue(m.cell, out CellRoles r))
                    r = new CellRoles { tilePair = -1, destPair = -1 };

                if (m.kind == MarkKind.WrongTile) r.tilePair = m.pairId;
                else r.destPair = m.pairId;
                roles[m.cell] = r;
            }

            foreach (var entry in roles)
            {
                Vector3 pos = gridManager.GetWorldPositionFromGridCoordinate(entry.Key);
                CellRoles r = entry.Value;

                if (r.tilePair >= 0 && r.destPair >= 0)
                {
                    // Lower-right half = destination, upper-left half = the tile sitting here.
                    Place(GetTriangle(), pos, cell, 0f, PairColor(r.destPair));
                    Place(GetTriangle(), pos, cell, 180f, PairColor(r.tilePair));
                }
                else
                {
                    int pair = r.tilePair >= 0 ? r.tilePair : r.destPair;
                    Place(GetSquare(), pos, cell, 0f, PairColor(pair));
                }
            }
        }

        public void Clear()
        {
            shownMarks = null;
            for (int i = 0; i < used; i++)
                pool[i].gameObject.SetActive(false);
            used = 0;
        }

        // Evenly spread hues across the whole wheel except the green band.
        private Color PairColor(int pairId)
        {
            float u = (pairId * GoldenRatio + 0.05f) % 1f;
            float hue = (GreenEnd + u * (1f - (GreenEnd - GreenStart))) % 1f;
            return WithAlpha(Color.HSVToRGB(hue, 0.75f, 1f), pairAlpha);
        }

        private static Color WithAlpha(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        private void Place(Sprite sprite, Vector3 position, float size, float rotationZ, Color color)
        {
            SpriteRenderer sr = Acquire();
            sr.sprite = sprite;
            sr.transform.position = position;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            sr.transform.localScale = new Vector3(size, size, 1f);
            sr.color = color;
        }

        private SpriteRenderer Acquire()
        {
            if (used == pool.Count)
            {
                var go = new GameObject("Mask");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;
                if (maskMaterial != null) sr.sharedMaterial = maskMaterial;
                pool.Add(sr);
            }

            SpriteRenderer result = pool[used++];
            result.gameObject.SetActive(true);
            return result;
        }

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

        // Lower-right half of a unit square (diagonal from bottom-left to top-right).
        private static Sprite GetTriangle()
        {
            if (triangleSprite == null)
            {
                var tex = new Texture2D(TriangleSize, TriangleSize, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color32[TriangleSize * TriangleSize];
                for (int y = 0; y < TriangleSize; y++)
                    for (int x = 0; x < TriangleSize; x++)
                        pixels[y * TriangleSize + x] = x >= y ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                tex.SetPixels32(pixels);
                tex.Apply();
                triangleSprite = Sprite.Create(tex, new Rect(0, 0, TriangleSize, TriangleSize), new Vector2(0.5f, 0.5f), TriangleSize);
            }
            return triangleSprite;
        }
    }
}
