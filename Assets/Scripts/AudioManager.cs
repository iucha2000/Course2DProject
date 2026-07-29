using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The game's one audio owner: a looping music source that survives scene loads, and a second
/// source for fire-and-forget sound effects.
///
/// <para>Two separate <see cref="AudioSource"/>s rather than one, because they are used in
/// opposite ways. Music is a single long clip whose volume the player sets directly; SFX are many
/// short clips layered on top of each other with <c>PlayOneShot</c>. One source could not do both:
/// starting a sound effect would cut the music off.</para>
///
/// <para><b>Why DontDestroyOnLoad.</b> Loading a level destroys everything in the old scene. If the
/// music lived in the scene it would restart from the beginning on every level change and on every
/// death-reload, which is worse than having no music at all.</para>
///
/// <para>A copy of <c>AudioManager.prefab</c> sits in every scene. The first one to load claims
/// <see cref="Instance"/> and survives; every later copy sees that the slot is taken and deletes
/// itself. That is what makes it safe to press Play in any scene during development - the manager
/// is always there, so no call site ever has to null-check it.</para>
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [Tooltip("Played on the intro screen.")]
    [SerializeField] private AudioClip menuMusic;
    [Tooltip("Played in all three levels.")]
    [SerializeField] private AudioClip levelMusic;

    [Header("Shared UI sounds")]
    [SerializeField] private AudioClip uiClickClip;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public float MusicVolume
    {
        get => musicSource.volume;
        set
        {
            musicSource.volume = Mathf.Clamp01(value);
            SaveSystem.MusicVolume = musicSource.volume;
        }
    }

    public float SfxVolume
    {
        get => sfxSource.volume;
        set
        {
            sfxSource.volume = Mathf.Clamp01(value);
            SaveSystem.SfxVolume = sfxSource.volume;
        }
    }

    private void Awake()
    {
        // The singleton guard. A second manager arriving with a new scene deletes itself rather
        // than fighting the first one over the music.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Added in code rather than dragged onto the prefab, for the same reason every other
        // component in this project is resolved in Awake: nothing to wire, nothing to leave null.
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        // Whatever the player chose last session.
        MusicVolume = SaveSystem.MusicVolume;
        SfxVolume = SaveSystem.SfxVolume;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    /// <summary>
    /// Picks the track for whichever scene just loaded. Doing it from this event means no scene
    /// has to know anything about audio - there is nothing to wire up per level.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic(scene.buildIndex == SaveSystem.MenuBuildIndex ? menuMusic : levelMusic);
    }

    /// <summary>Starts a music track, or does nothing if that track is already playing.</summary>
    public void PlayMusic(AudioClip clip)
    {
        // The "already playing" test is what stops the level music restarting every time the
        // player dies and the level reloads.
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying))
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    /// <summary>
    /// Plays a one-off sound effect. <c>PlayOneShot</c> layers sounds on top of each other instead
    /// of replacing whatever the source is currently playing, which is what makes it right for
    /// things like coins collected in quick succession.
    ///
    /// <para><b>Static, and it swallows both kinds of nothing.</b> A null <paramref name="clip"/>
    /// is normal rather than an error - the clips are assigned late in development, so until then
    /// every call is a no-op and the game runs exactly as it does now. A null
    /// <see cref="Instance"/> is normal too: there is an AudioManager in every scene, but opening
    /// one scene on its own mid-development should never throw. Doing both checks here means no
    /// call site anywhere has to guard, so they all read as one plain line.</para>
    /// </summary>
    public static void PlaySfx(AudioClip clip)
    {
        if (Instance != null && clip != null)
        {
            Instance.sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>The click every button shares, so each menu script does not need its own copy.</summary>
    public static void PlayUiClick()
    {
        if (Instance != null)
        {
            PlaySfx(Instance.uiClickClip);
        }
    }
}
