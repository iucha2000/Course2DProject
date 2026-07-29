using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The door at the end of a level. Reaching it records the unlock and loads the next scene.
///
/// <para>Levels are loaded <b>by build index</b> rather than by name, so this one component works
/// for all three without being told which level it is in - "the next one" is always
/// <c>buildIndex + 1</c>. That is also why the build order in Build Profiles matters:
/// IntroScene must be 0 and the levels 1..3, which is what <see cref="SaveSystem"/> assumes.</para>
///
/// <para>Sits on the <c>Ignore Raycast</c> layer, for the same reason <c>CameraBounds</c> does: it
/// is a marker volume, and anything on a layer the enemy or ground checks can see would be
/// mistaken for solid floor (see CLAUDE.md). Trigger callbacks are driven by the collision matrix
/// rather than by queries, so they still fire from there.</para>
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LevelExit : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip exitClip;

    // The load does not happen until the end of the frame, so without this a player straddling
    // the trigger could fire it twice and skip a level.
    private bool used;

    private void Awake()
    {
        // Enforced here rather than trusted to the prefab: a solid exit would be a wall the
        // player bumps into, and the failure would look like level geometry rather than a bug.
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player"))
        {
            return;
        }

        used = true;

        int nextLevel = SceneManager.GetActiveScene().buildIndex + 1;

        AudioManager.PlaySfx(exitClip);

        if (nextLevel > SaveSystem.LastLevelBuildIndex)
        {
            // That was the last level, so the game is finished. Back to the intro screen.
            SceneManager.LoadScene(SaveSystem.MenuBuildIndex);
            return;
        }

        // Saved before the load, not after: the next scene has no idea which one it followed.
        //
        // The carried state is written as a *level start* rather than a checkpoint - it records
        // the ammo and coins but no position, because the next level has its own spawn point and
        // knows nothing about where this one ended.
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.SaveCarriedState(nextLevel);
        }

        SaveSystem.UnlockLevel(nextLevel);
        SceneManager.LoadScene(nextLevel);
    }
}
