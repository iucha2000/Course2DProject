using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Music and Sound sliders, shared by the intro screen and the pause menu.
///
/// <para>It knows nothing about either of them. Whoever opens it hands over the screen it should
/// replace, and Back puts that screen back — so the same panel works anywhere without a special
/// case for "am I in the menu or in a level".</para>
///
/// <para><b>It ships inactive, and wires itself on first use rather than in Awake.</b> The
/// obvious alternative - ship it active and switch it off in <c>Awake</c> - has a real cost: when
/// this panel sits inside another panel that is itself being enabled, it gets activated and
/// deactivated within the same frame. That queues a layout rebuild which is then thrown away, and
/// TextMeshPro can come back with stale zero-width geometry, which shows up as button labels
/// rendering one letter per line. Starting inactive avoids the churn entirely; the lazy
/// <see cref="Initialise"/> covers the fact that an inactive object never receives Awake.</para>
/// </summary>
public class OptionsPanel : MonoBehaviour
{
    private Slider musicSlider;
    private Slider sfxSlider;
    private Button backButton;

    // The menu that was showing when we were opened, so Back can restore it.
    private GameObject previousScreen;
    private bool initialised;

    /// <summary>True while the sliders are showing instead of the menu that opened them.</summary>
    public bool IsOpen => gameObject.activeSelf;

    // Runs only if this ever does start active. Initialise guards against running twice.
    private void Awake() => Initialise();

    private void Initialise()
    {
        if (initialised)
        {
            return;
        }

        initialised = true;

        musicSlider = Find<Slider>("MusicSlider");
        sfxSlider = Find<Slider>("SfxSlider");
        backButton = Find<Button>("BackButton");

        backButton.onClick.AddListener(Close);

        // SetValueWithoutNotify first, so showing the saved value does not immediately
        // count as the player changing it and write it straight back.
        musicSlider.SetValueWithoutNotify(SaveSystem.MusicVolume);
        sfxSlider.SetValueWithoutNotify(SaveSystem.SfxVolume);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    /// <summary>Shows the sliders in place of <paramref name="screenToReplace"/>.</summary>
    public void Open(GameObject screenToReplace)
    {
        // The panel ships inactive, so this is the first chance we get to wire anything.
        Initialise();

        previousScreen = screenToReplace;

        if (previousScreen != null)
        {
            previousScreen.SetActive(false);
        }

        // Re-read rather than trusting the slider positions: the other copy of this panel
        // may have changed the volume since this one was last looked at.
        musicSlider.SetValueWithoutNotify(SaveSystem.MusicVolume);
        sfxSlider.SetValueWithoutNotify(SaveSystem.SfxVolume);

        gameObject.SetActive(true);
        AudioManager.PlayUiClick();
    }

    /// <summary>Hides the sliders and brings back whatever was showing before.</summary>
    public void Close()
    {
        gameObject.SetActive(false);

        if (previousScreen != null)
        {
            previousScreen.SetActive(true);
        }

        AudioManager.PlayUiClick();
    }

    private void SetMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            // The AudioManager writes it to the save file, so the choice survives a relaunch.
            AudioManager.Instance.MusicVolume = value;
        }
    }

    private void SetSfxVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SfxVolume = value;
        }
    }

    private T Find<T>(string childName) where T : Component
    {
        // Inactive children included: the panel switches itself off in Awake, and Open may be
        // called again afterwards, so nothing here can rely on the subtree being active.
        foreach (T candidate in GetComponentsInChildren<T>(true))
        {
            if (candidate.name == childName)
            {
                return candidate;
            }
        }

        Debug.LogError($"OptionsPanel expects a '{childName}' child.", this);
        return null;
    }
}
