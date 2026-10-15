using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using MindTiles.Core;

namespace MindTiles.Audio
{
    // The one place that plays sound. Lives for the whole app run.
    //
    // It is not placed in any scene: the prefab at Resources/AudioManager is
    // spawned automatically before the first scene loads, so sound works no
    // matter which scene you press Play in. Assign the clips on that prefab.
    //
    // - Music: picked per scene (see SceneMusic), loops, fades between tracks
    //   and is quieter while the game is paused.
    // - SFX: AudioManager.Play(Sfx.X) from code, UISound on buttons. Round
    //   results come in through GameManager events.
    // - Volume follows the Sound / Music switches in the settings panel.
    // It also carries the app's only AudioListener (cameras have none), so
    // sound plays even in scenes without a camera (Loading).
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        // A clip plus its own volume, so loud and quiet files can be balanced here.
        [Serializable]
        public class Sound
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        private const string ResourcePath = "AudioManager";

        // Scene names (Build Profiles > Scene List).
        private const string LoadingScene = "Loading";
        private const string MenuScene = "MenuSpace";
        private const string GameScene = "SampleScene";

        public static AudioManager Instance { get; private set; }

        [Header("Music (loops)")]
        [Tooltip("Plays on the loading screen and the menu (continues from one to the other without restarting).")]
        [SerializeField] private Sound menuMusic = new Sound();
        [Tooltip("Plays in the game scene. Leave empty for no music while playing.")]
        [SerializeField] private Sound gameMusic = new Sound();
        [Tooltip("Seconds to fade out the old track and fade in the new one.")]
        [SerializeField, Min(0f)] private float musicFadeSeconds = 0.6f;
        [Tooltip("Music volume multiplier while the game is paused.")]
        [SerializeField, Range(0f, 1f)] private float pausedMusicVolume = 0.4f;

        [Header("UI")]
        [SerializeField] private Sound buttonClick = new Sound();
        [SerializeField] private Sound panelOpen = new Sound();
        [SerializeField] private Sound panelClose = new Sound();
        [Tooltip("Daily Streak / Endless buttons on the menu.")]
        [SerializeField] private Sound playButton = new Sound();
        [SerializeField] private Sound toggleOn = new Sound();
        [SerializeField] private Sound toggleOff = new Sound();

        [Header("Tiles")]
        [Tooltip("A tile starts being dragged.")]
        [SerializeField] private Sound tilePickUp = new Sound();
        [Tooltip("A tile lands in an empty cell.")]
        [SerializeField] private Sound tileDrop = new Sound();
        [Tooltip("A tile lands on another tile and the two swap.")]
        [SerializeField] private Sound tileSwap = new Sound();
        [Tooltip("A tile is let go in its own cell and slides back.")]
        [SerializeField] private Sound tileReturn = new Sound();
        [Tooltip("Random pitch change on tile sounds so repeats don't sound robotic (0.05 = +-5%).")]
        [SerializeField, Range(0f, 0.3f)] private float tilePitchVariation = 0.05f;

        [Header("Round")]
        [Tooltip("The whole 3-2-1 countdown as ONE clip (it contains the ticks itself). Played once when the countdown starts.")]
        [SerializeField] private Sound countdownTick = new Sound();
        [Tooltip("The countdown clip is sped up or slowed down to last exactly this long (3 = the 3, 2, 1). 0 = play at normal speed. Speed and pitch change together.")]
        [SerializeField, Min(0f)] private float countdownFitSeconds = 3f;
        [Tooltip("\"GO!\" - the tiles get scrambled.")]
        [SerializeField] private Sound countdownGo = new Sound();
        [Tooltip("Every second of the Endless timer's last seconds (when it turns red).")]
        [SerializeField] private Sound timerWarning = new Sound();
        [Tooltip("Level checked with enough correct tiles.")]
        [SerializeField] private Sound levelPassed = new Sound();
        [Tooltip("Level checked with too few correct tiles (no points, level is replayed).")]
        [SerializeField] private Sound levelFailed = new Sound();
        [Tooltip("Daily Streak run finished.")]
        [SerializeField] private Sound runCompleted = new Sound();

        [Header("Hints")]
        [SerializeField] private Sound hintUsed = new Sound();
        [Tooltip("Hint pressed but it can't be used right now.")]
        [SerializeField] private Sound hintUnavailable = new Sound();

        [Header("Mixer (optional)")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        private AudioSource musicSource;
        private AudioSource uiSource;
        private AudioSource tileSource;
        private AudioSource countdownSource;

        private Sound currentMusic;
        private float musicFade = 1f;        // 0..1, driven by the fade coroutine
        private bool gamePaused;
        private Coroutine musicRoutine;

        // ---- Lifetime ---------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            AudioManager prefab = Resources.Load<AudioManager>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning("[AudioManager] No prefab at Resources/" + ResourcePath + "; the game runs without sound.");
                return;
            }
            Instantiate(prefab).name = "AudioManager";
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (GetComponent<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();

            musicSource = CreateSource("Music", musicGroup, true);
            uiSource = CreateSource("UI", sfxGroup, false);
            tileSource = CreateSource("Tiles", sfxGroup, false);
            countdownSource = CreateSource("Countdown", sfxGroup, false);
        }

        private AudioSource CreateSource(string sourceName, AudioMixerGroup group, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;    // 2D
            source.outputAudioMixerGroup = group;
            return source;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameManager.RoundResult += OnRoundResult;
            GameManager.RunCompleted += OnRunCompleted;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameManager.RoundResult -= OnRoundResult;
            GameManager.RunCompleted -= OnRunCompleted;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // The first scene may have loaded before OnEnable subscribed.
            PlayMusicForScene(SceneManager.GetActiveScene().name);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;

            gamePaused = false;     // a new scene always starts unpaused
            countdownSource.Stop(); // a countdown never carries over into another scene
            PlayMusicForScene(scene.name);
        }

        // ---- Static API (safe to call when there is no AudioManager) ----------------

        public static void Play(Sfx id)
        {
            if (Instance != null) Instance.PlaySfx(id);
        }

        // Memorize countdown: plays the countdown clip once, stretched to the
        // configured length (see countdownFitSeconds).
        public static void PlayCountdown()
        {
            if (Instance != null) Instance.StartCountdown();
        }

        // Settings panel changed Sound / Music.
        public static void RefreshVolumes()
        {
            if (Instance != null) Instance.ApplyMusicVolume();
        }

        // Game paused / resumed: music gets quieter.
        public static void SetGamePaused(bool paused)
        {
            if (Instance == null) return;
            Instance.gamePaused = paused;
            Instance.ApplyMusicVolume();

            // The countdown text freezes while paused, so its sound does too.
            if (paused) Instance.countdownSource.Pause();
            else Instance.countdownSource.UnPause();
        }

        // ---- Music --------------------------------------------------------------------

        // Where music plays. Change this when scenes are added or renamed.
        private Sound MusicFor(string sceneName)
        {
            switch (sceneName)
            {
                case LoadingScene:
                case MenuScene:
                    return menuMusic;
                case GameScene:
                    return gameMusic;
                default:
                    return null;
            }
        }

        private void PlayMusicForScene(string sceneName)
        {
            Sound next = MusicFor(sceneName);
            if (next == null || next.clip == null) next = null;

            // Same track already playing: keep it going (Loading -> Menu, or reloading the game scene).
            if (next != null && currentMusic != null && next.clip == currentMusic.clip && musicSource.isPlaying)
            {
                currentMusic = next;
                ApplyMusicVolume();
                return;
            }
            if (next == null && currentMusic == null) return;

            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(SwitchMusic(next));
        }

        private IEnumerator SwitchMusic(Sound next)
        {
            // Fade out what is playing.
            if (currentMusic != null && musicSource.isPlaying)
                yield return FadeMusic(0f);

            musicSource.Stop();
            currentMusic = next;
            if (next == null) yield break;

            musicSource.clip = next.clip;
            musicFade = 0f;
            ApplyMusicVolume();
            musicSource.Play();
            yield return FadeMusic(1f);
            musicRoutine = null;
        }

        private IEnumerator FadeMusic(float target)
        {
            if (musicFadeSeconds <= 0f)
            {
                musicFade = target;
                ApplyMusicVolume();
                yield break;
            }

            while (!Mathf.Approximately(musicFade, target))
            {
                // Unscaled: fades also run while the game is paused (timeScale 0).
                musicFade = Mathf.MoveTowards(musicFade, target, Time.unscaledDeltaTime / musicFadeSeconds);
                ApplyMusicVolume();
                yield return null;
            }
        }

        private void ApplyMusicVolume()
        {
            if (musicSource == null) return;

            float baseVolume = currentMusic != null ? currentMusic.volume : 1f;
            float setting = SaveSystem.Data.settings.musicVolume;
            float pause = gamePaused ? pausedMusicVolume : 1f;
            musicSource.volume = baseVolume * setting * pause * musicFade;
        }

        // ---- Sound effects ------------------------------------------------------------

        private void PlaySfx(Sfx id)
        {
            Sound sound = SoundFor(id);
            if (sound == null || sound.clip == null) return;

            float setting = SaveSystem.Data.settings.sfxVolume;
            if (setting <= 0f) return;

            if (IsTileSound(id))
            {
                // Pitch is per source, so tile sounds have their own source.
                tileSource.pitch = 1f + UnityEngine.Random.Range(-tilePitchVariation, tilePitchVariation);
                tileSource.PlayOneShot(sound.clip, sound.volume * setting);
            }
            else
            {
                // PlayOneShot lets quick repeated clicks overlap instead of cutting each other off.
                uiSource.PlayOneShot(sound.clip, sound.volume * setting);
            }
        }

        private void StartCountdown()
        {
            if (countdownTick.clip == null) return;

            float setting = SaveSystem.Data.settings.sfxVolume;
            if (setting <= 0f) return;

            // Pitch is also the playback speed: an 8 s clip at 8 / 3 = 2.67x lasts 3 s.
            float length = countdownTick.clip.length;
            countdownSource.pitch = countdownFitSeconds > 0f ? Mathf.Clamp(length / countdownFitSeconds, 0.1f, 3f) : 1f;
            countdownSource.clip = countdownTick.clip;
            countdownSource.volume = countdownTick.volume * setting;
            countdownSource.Play();
        }

        private static bool IsTileSound(Sfx id)
        {
            return id == Sfx.TilePickUp || id == Sfx.TileDrop || id == Sfx.TileSwap || id == Sfx.TileReturn;
        }

        private Sound SoundFor(Sfx id)
        {
            switch (id)
            {
                case Sfx.ButtonClick: return buttonClick;
                case Sfx.PanelOpen: return panelOpen;
                case Sfx.PanelClose: return panelClose;
                case Sfx.PlayButton: return playButton;
                case Sfx.ToggleOn: return toggleOn;
                case Sfx.ToggleOff: return toggleOff;
                case Sfx.TilePickUp: return tilePickUp;
                case Sfx.TileDrop: return tileDrop;
                case Sfx.TileSwap: return tileSwap;
                case Sfx.TileReturn: return tileReturn;
                case Sfx.CountdownTick: return countdownTick;
                case Sfx.CountdownGo: return countdownGo;
                case Sfx.TimerWarning: return timerWarning;
                case Sfx.LevelPassed: return levelPassed;
                case Sfx.LevelFailed: return levelFailed;
                case Sfx.RunCompleted: return runCompleted;
                case Sfx.HintUsed: return hintUsed;
                case Sfx.HintUnavailable: return hintUnavailable;
                default: return null;
            }
        }

        // ---- Game events ----------------------------------------------------------------

        private void OnRoundResult(bool passed)
        {
            PlaySfx(passed ? Sfx.LevelPassed : Sfx.LevelFailed);
        }

        private void OnRunCompleted()
        {
            // No dedicated run-complete clip yet: use the level-passed one.
            PlaySfx(runCompleted.clip != null ? Sfx.RunCompleted : Sfx.LevelPassed);
        }
    }
}
