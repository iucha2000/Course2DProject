using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The title screen: New Game, Continue and Options.
///
/// <para><b>Continue</b> is only offered once there is something to continue from, which is the
/// visible proof that the save survived the application quitting.</para>
/// </summary>
public class IntroCanvasHandler : MonoBehaviour
{
    private Button newGameButton;
    private Button continueButton;
    private Button optionsButton;

    // The whole button column, hidden as a unit while the options sliders are showing.
    private GameObject menuGroup;
    private OptionsPanel optionsPanel;

    private void Awake()
    {
        newGameButton = Find<Button>("NewGameButton");
        continueButton = Find<Button>("ContinueButton");
        optionsButton = Find<Button>("OptionsButton");

        menuGroup = Find<Transform>("MenuGroup").gameObject;
        optionsPanel = GetComponentInChildren<OptionsPanel>(true);
    }

    private void Start()
    {
        // A Continue button that does nothing on a fresh install is worse than no button, so it
        // is hidden outright rather than greyed out. SetActive takes the whole object out of the
        // canvas - it is not drawn, not laid out and not clickable.
        continueButton.gameObject.SetActive(SaveSystem.HasProgress);

        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        optionsButton.onClick.AddListener(OnOptions);
    }

    private void OnNewGame()
    {
        // Wipes progress but deliberately not the volume settings - one is progress, the other
        // is a preference, and starting a new game should not reset the player's audio choices.
        SaveSystem.ResetProgress();

        AudioManager.PlayUiClick();
        SceneManager.LoadScene(SaveSystem.FirstLevelBuildIndex);
    }

    private void OnContinue()
    {
        AudioManager.PlayUiClick();

        // The checkpoint is the save, so resuming is just loading the level it is in - the
        // player then restores their own position and inventory from the same record.
        SceneManager.LoadScene(SaveSystem.ResumeLevel);
    }

    private void OnOptions() => optionsPanel.Open(menuGroup);

    private T Find<T>(string childName) where T : Component
    {
        // By name rather than by dragged reference, and including inactive objects, because
        // the options panel switches itself off before this ever runs.
        foreach (T candidate in GetComponentsInChildren<T>(true))
        {
            if (candidate.name == childName)
            {
                return candidate;
            }
        }

        Debug.LogError($"IntroCanvasHandler expects a child called '{childName}'.", this);
        return null;
    }
}
