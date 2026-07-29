using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Esc opens the pause panel and freezes the game with <c>Time.timeScale = 0</c>.
///
/// <para><b>What timeScale actually stops.</b> It scales the clock the engine runs the simulation
/// on, so at zero: FixedUpdate stops being called, so all physics stops; <c>Time.deltaTime</c>
/// becomes 0; and any coroutine waiting on <c>WaitForSeconds</c> never wakes up. It does <i>not</i>
/// stop <c>Update</c>, and it does not stop uGUI - which is exactly why the menu still works while
/// everything behind it is frozen.</para>
///
/// <para>That last point cuts both ways: because Update keeps running, scripts that read input in
/// Update would still act on it while paused. <see cref="Player"/> and <see cref="PlayerCombat"/>
/// both check <see cref="IsPaused"/> for that reason - without it, clicking Continue would also
/// throw a shuriken.</para>
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>Read by the gameplay scripts so they ignore input while the panel is up.</summary>
    public static bool IsPaused { get; private set; }

    private GameObject panel;
    private GameObject buttonGroup;
    private Button continueButton;
    private Button optionsButton;
    private Button quitButton;
    private OptionsPanel optionsPanel;

    private void Awake()
    {
        panel = transform.Find("PausePanel")?.gameObject;

        if (panel == null)
        {
            Debug.LogError("PauseMenu expects a child called 'PausePanel'.", this);
            enabled = false;
            return;
        }

        buttonGroup = FindIn<Transform>("ButtonGroup").gameObject;
        continueButton = FindIn<Button>("ContinueButton");
        optionsButton = FindIn<Button>("OptionsButton");
        quitButton = FindIn<Button>("QuitButton");
        optionsPanel = panel.GetComponentInChildren<OptionsPanel>(true);

        // Wired in Awake rather than Start because the panel starts inactive, and Start never
        // runs on an object that has not been active yet.
        continueButton.onClick.AddListener(Resume);
        optionsButton.onClick.AddListener(OnOptions);
        quitButton.onClick.AddListener(QuitToMenu);

        // timeScale and IsPaused are both global and both survive a scene load, so a level that
        // was quit while paused would otherwise start the next one frozen. Clearing them here
        // means every scene begins running no matter how the last one ended.
        Time.timeScale = 1f;
        IsPaused = false;

        panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsPaused)
            {
                Pause();
            }
            else if (optionsPanel != null && optionsPanel.IsOpen)
            {
                // Esc inside the options backs out one step rather than unpausing, which is
                // what every menu with a sub-screen does.
                optionsPanel.Close();
            }
            else
            {
                Resume();
            }
        }
    }

    private void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        panel.SetActive(true);

        // Always open on the buttons rather than on whatever screen was showing when the menu
        // was last closed.
        buttonGroup.SetActive(true);
        optionsPanel.gameObject.SetActive(false);

        AudioManager.PlayUiClick();
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        panel.SetActive(false);
        AudioManager.PlayUiClick();
    }

    private void OnOptions() => optionsPanel.Open(buttonGroup);

    private void QuitToMenu()
    {
        // Putting the clock back before loading is essential, not tidiness: timeScale is global
        // and survives the load, so leaving it at 0 would drop the player into a frozen menu.
        Time.timeScale = 1f;
        IsPaused = false;

        AudioManager.PlayUiClick();
        SceneManager.LoadScene(SaveSystem.MenuBuildIndex);
    }

    private T FindIn<T>(string childName) where T : Component
    {
        // Searches the whole panel subtree including inactive objects, so the layout can be
        // rearranged in the Editor without breaking the lookup - only the names matter.
        foreach (T candidate in panel.GetComponentsInChildren<T>(true))
        {
            if (candidate.name == childName)
            {
                return candidate;
            }
        }

        Debug.LogError($"PauseMenu expects a '{childName}' under PausePanel.", this);
        return null;
    }

    private void OnDestroy()
    {
        // Safety net for anything that destroys this without going through QuitToMenu, such as
        // the level reloading after a death while the panel happened to be open.
        Time.timeScale = 1f;
        IsPaused = false;
    }
}
