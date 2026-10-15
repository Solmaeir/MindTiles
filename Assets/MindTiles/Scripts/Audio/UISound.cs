using UnityEngine;
using UnityEngine.UI;

namespace MindTiles.Audio
{
    // Put on a Button to play a sound when it is clicked. Pick which sound
    // (click / panel open / panel close / play...); the clip itself is set on
    // the AudioManager prefab, so all buttons of a kind sound the same.
    //
    // Buttons whose action already plays its own sound (hint, check, toggle
    // switches, menu info/settings) don't need this component.
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class UISound : MonoBehaviour
    {
        [SerializeField] private Sfx sound = Sfx.ButtonClick;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(PlaySound);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(PlaySound);
        }

        private void PlaySound()
        {
            AudioManager.Play(sound);
        }
    }
}
