using System;
using UnityEngine;
using UnityEngine.UI;
using MindTiles.Audio;

namespace MindTiles.UI
{
    // iOS-style on/off switch: tap anywhere on it to flip. The knob slides and
    // the track recolors with unscaled time, so it also works while the game is paused.
    [RequireComponent(typeof(Button))]
    public class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] private Image track;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Color onColor = new Color(1f, 0.58f, 0.78f, 1f);
        [SerializeField] private Color offColor = new Color(0.29f, 0.26f, 0.5f, 1f);
        [Tooltip("How far the knob sits from the center on each side (canvas units).")]
        [SerializeField] private float knobTravel = 28f;
        [SerializeField] private float animationSpeed = 10f;

        public bool IsOn { get; private set; }
        public event Action<bool> Changed;

        private float position;   // 0 = off, 1 = on

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Flip);
        }

        // Sets the state; notify = false when loading a saved value.
        public void SetIsOn(bool on, bool notify)
        {
            IsOn = on;
            if (notify) Changed?.Invoke(on);
        }

        // Opening a panel shows the current state at once, without an animation.
        public void Snap()
        {
            position = IsOn ? 1f : 0f;
            Apply();
        }

        // Sound plays after Changed, so switching Sound off is silent and switching it on is heard.
        private void Flip()
        {
            SetIsOn(!IsOn, true);
            AudioManager.Play(IsOn ? Sfx.ToggleOn : Sfx.ToggleOff);
        }

        private void Update()
        {
            float target = IsOn ? 1f : 0f;
            if (Mathf.Approximately(position, target)) return;

            position = Mathf.MoveTowards(position, target, Time.unscaledDeltaTime * animationSpeed);
            Apply();
        }

        private void Apply()
        {
            float eased = position * position * (3f - 2f * position);
            if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Lerp(-knobTravel, knobTravel, eased), 0f);
            if (track != null) track.color = Color.Lerp(offColor, onColor, eased);
        }
    }
}
