using System;
using UnityEngine;

namespace MindTiles.Grid
{
    // One shared look for every tile mark (round-end reveal and hints), so a
    // color or size is changed in exactly one place. Edits apply live in Play.
    [CreateAssetMenu(fileName = "MarkStyle", menuName = "MindTiles/Mark Style")]
    public class MarkStyle : ScriptableObject
    {
        [Header("Washes")]
        [Tooltip("Only RGB is used; opacity comes from Correct Alpha.")]
        public Color correctColor = new Color(0.2f, 0.9f, 0.3f, 1f);
        [Range(0f, 1f)] public float correctAlpha = 0.4f;
        [Tooltip("Only RGB is used; opacity comes from Wrong Alpha.")]
        public Color wrongColor = new Color(0.95f, 0.15f, 0.15f, 1f);
        [Range(0f, 1f)] public float wrongAlpha = 0.45f;
        [Tooltip("Gap between a wash and its cell edge, as a share of the cell.")]
        [Range(0f, 0.2f)] public float washInset = 0.04f;

        [Header("Correct: Check Mark")]
        public bool showCheckMark = true;
        public Color checkColor = new Color(0.1f, 0.75f, 0.25f, 1f);
        [Tooltip("Check mark size as a share of the cell; stays inside the cell edges.")]
        [Range(0.2f, 0.9f)] public float checkSize = 0.55f;

        [Header("Wrong: Arrow To The Correct Cell")]
        public bool showArrow = true;
        public Color arrowColor = new Color(0.9f, 0.1f, 0.1f, 1f);
        [Tooltip("Arrow head size as a share of the cell.")]
        [Range(0.2f, 0.9f)] public float arrowHeadSize = 0.45f;
        [Tooltip("Line thickness as a share of the cell.")]
        [Range(0.015f, 0.12f)] public float arrowThickness = 0.045f;

        public static event Action Changed;

        private void OnValidate()
        {
            Changed?.Invoke();
        }
    }
}
