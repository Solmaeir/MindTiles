using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MindTiles.UI
{
    /// <summary>
    /// Plays the "menu elements slide off-screen" animation, then shows a loading text
    /// while the game scene finishes loading in the background.
    /// Idle cost is zero: the component stays disabled until <see cref="Begin"/> is called.
    /// </summary>
    public class MenuExitTransition : MonoBehaviour
    {
        [Serializable]
        public class SlideEntry
        {
            public RectTransform target;

            [NonSerialized] public Vector2 startPos;
            [NonSerialized] public Vector2 dir;
            [NonSerialized] public float distance;
            [NonSerialized] public float travelled;
        }

        [Header("Slide Groups")]
        [Tooltip("Shared speed for every element, in canvas units per second.")]
        [SerializeField] private float slideSpeed = 1500f;
        [SerializeField] private List<SlideEntry> slideUp = new List<SlideEntry>();
        [SerializeField] private List<SlideEntry> slideDown = new List<SlideEntry>();
        [SerializeField] private List<SlideEntry> slideRight = new List<SlideEntry>();
        [SerializeField] private List<SlideEntry> slideLeft = new List<SlideEntry>();
        [Tooltip("Extra canvas units added past the screen edge so nothing peeks in.")]
        [SerializeField] private float edgeMargin = 20f;

        [Header("References")]
        [Tooltip("Root Canvas RectTransform; its rect defines the visible area.")]
        [SerializeField] private RectTransform canvasRect;
        [Tooltip("Full-screen transparent Image (Raycast Target on), initially inactive. Blocks all input during the transition.")]
        [SerializeField] private GameObject inputBlocker;
        [Tooltip("Initially inactive. Shown once every element has left the screen.")]
        [SerializeField] private TMP_Text loadingText;

        [Header("Loading")]
        [SerializeField] private int sceneBuildIndex = 1;
        [Tooltip("Minimum seconds the loading text stays visible, so it never just flashes.")]
        [SerializeField] private float minLoadingTime = 2f;
        [SerializeField] private float dotInterval = 0.4f;

        private const float ActivationProgress = 0.9f;

        private enum Phase { Idle, Sliding, Loading }

        private readonly Vector3[] corners = new Vector3[4];
        private readonly List<SlideEntry> all = new List<SlideEntry>(16);
        private string[] dotFrames;
        private AsyncOperation loadOp;
        private Phase phase = Phase.Idle;
        private float loadingTimer;
        private int dotIndex = -1;

        public bool IsPlaying => phase != Phase.Idle;

        private void Awake()
        {
            enabled = false;
        }

        /// <summary>Call from the Play buttons. Ignored if already running.</summary>
        public void Begin()
        {
            if (phase != Phase.Idle) return;

            if (inputBlocker != null) inputBlocker.SetActive(true);

            // Start loading right away so it overlaps the animation; activation is held back.
            loadOp = SceneManager.LoadSceneAsync(sceneBuildIndex);
            loadOp.allowSceneActivation = false;

            Prepare();
            phase = Phase.Sliding;
            enabled = true;
        }

        private void Prepare()
        {
            all.Clear();
            canvasRect.GetWorldCorners(corners);
            Vector2 cMin = corners[0];
            Vector2 cMax = corners[2];

            Register(slideUp, cMin, cMax, 0, 1);
            Register(slideDown, cMin, cMax, 0, -1);
            Register(slideRight, cMin, cMax, 1, 0);
            Register(slideLeft, cMin, cMax, -1, 0);

            if (loadingText != null)
            {
                string baseText = loadingText.text.TrimEnd('.');
                dotFrames = new[] { baseText, baseText + ".", baseText + "..", baseText + "..." };
            }
        }

        // Distance is computed once; the per-frame work is just position = start + dir * travelled.
        private void Register(List<SlideEntry> list, Vector2 cMin, Vector2 cMax, int dirX, int dirY)
        {
            for (int i = 0; i < list.Count; i++)
            {
                SlideEntry e = list[i];
                if (e.target == null) continue;

                e.target.GetWorldCorners(corners);
                Vector2 eMin = corners[0];
                Vector2 eMax = corners[2];

                Vector3 s = e.target.parent != null ? e.target.parent.lossyScale : Vector3.one;
                float worldDist = dirX > 0 ? cMax.x - eMin.x
                                : dirX < 0 ? eMax.x - cMin.x
                                : dirY > 0 ? cMax.y - eMin.y
                                : eMax.y - cMin.y;
                float scale = dirX != 0 ? Mathf.Abs(s.x) : Mathf.Abs(s.y);

                // FloatingCloud rewrites anchoredPosition every frame; stop it so the slide isn't overridden.
                FloatingCloud cloud = e.target.GetComponent<FloatingCloud>();
                if (cloud != null) cloud.enabled = false;

                e.startPos = e.target.anchoredPosition;
                e.distance = Mathf.Max(0f, worldDist / Mathf.Max(scale, 0.0001f)) + edgeMargin;
                e.travelled = 0f;
                all.Add(e);
                e.dir = new Vector2(dirX, dirY);
            }
        }

        private void Update()
        {
            // unscaledDeltaTime: works even if something paused the game.
            float dt = Time.unscaledDeltaTime;

            if (phase == Phase.Sliding)
            {
                if (TickSlide(dt)) EnterLoading();
            }
            else if (phase == Phase.Loading)
            {
                TickLoading(dt);
            }
        }

        private bool TickSlide(float dt)
        {
            bool finished = true;
            for (int i = 0; i < all.Count; i++)
            {
                SlideEntry e = all[i];
                if (e.travelled >= e.distance) continue;

                e.travelled = Mathf.Min(e.travelled + slideSpeed * dt, e.distance);
                e.target.anchoredPosition = e.startPos + e.dir * e.travelled;
                if (e.travelled < e.distance) finished = false;
            }
            return finished;
        }

        private void EnterLoading()
        {
            phase = Phase.Loading;
            loadingTimer = 0f;
            if (loadingText != null) loadingText.gameObject.SetActive(true);
        }

        private void TickLoading(float dt)
        {
            loadingTimer += dt;

            if (loadingText != null && dotFrames != null)
            {
                int idx = (int)(loadingTimer / dotInterval) & 3;
                if (idx != dotIndex)
                {
                    dotIndex = idx;
                    loadingText.text = dotFrames[idx];
                }
            }

            if (loadingTimer >= minLoadingTime && loadOp.progress >= ActivationProgress)
            {
                loadOp.allowSceneActivation = true;
                enabled = false;
            }
        }
    }
}
