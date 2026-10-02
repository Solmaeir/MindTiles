using System.Collections.Generic;
using UnityEngine;

namespace MindTiles.Grid
{
    // Draws thin chessboard-style cell separators over the play area as one
    // small mesh (a single draw call). Rebuilt only when the grid size
    // changes, i.e. once per level. The outer edge is skipped by default
    // because the board panel already frames the area.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridLinesRenderer : MonoBehaviour
    {
        [SerializeField] private float lineThickness = 0.02f;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private bool drawOuterBorder = false;
        [SerializeField] private int sortingOrder = 0;

        private Mesh mesh;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();

        private void Awake()
        {
            mesh = new Mesh { name = "GridLines" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        }

        public void Rebuild(int width, int height, float cellSize, Vector3 center)
        {
            transform.position = new Vector3(center.x, center.y, 0f);

            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            float halfW = width * cellSize * 0.5f;
            float halfH = height * cellSize * 0.5f;
            float half = lineThickness * 0.5f;

            int first = drawOuterBorder ? 0 : 1;
            int lastX = drawOuterBorder ? width : width - 1;
            int lastY = drawOuterBorder ? height : height - 1;

            for (int i = first; i <= lastX; i++)
            {
                float x = -halfW + i * cellSize;
                AddQuad(x - half, -halfH, x + half, halfH);
            }

            for (int j = first; j <= lastY; j++)
            {
                float y = -halfH + j * cellSize;
                AddQuad(-halfW, y - half, halfW, y + half);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void AddQuad(float x0, float y0, float x1, float y1)
        {
            int v = vertices.Count;
            vertices.Add(new Vector3(x0, y0, 0f));
            vertices.Add(new Vector3(x0, y1, 0f));
            vertices.Add(new Vector3(x1, y1, 0f));
            vertices.Add(new Vector3(x1, y0, 0f));
            for (int k = 0; k < 4; k++) colors.Add(lineColor);

            triangles.Add(v); triangles.Add(v + 1); triangles.Add(v + 2);
            triangles.Add(v); triangles.Add(v + 2); triangles.Add(v + 3);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
