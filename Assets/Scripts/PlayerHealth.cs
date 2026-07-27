using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHearts = 3;

    // The HUD reads these. Private set so only this script can change them.
    public int Hearts { get; private set; }
    public int MaxHearts => maxHearts;

    private void Awake()
    {
        Hearts = maxHearts;
    }

    public void TakeDamage(int amount)
    {
        // Mathf.Max stops the heart count going negative if two things hit at once.
        Hearts = Mathf.Max(Hearts - amount, 0);

        if (Hearts == 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        Hearts = Mathf.Min(Hearts + amount, maxHearts);
    }

    private void Die()
    {
        // TODO(audio): AudioManager.Instance.PlaySfx(deathClip);

        // Restart whichever level we are currently in. Reloading by build index rather
        // than by name means this one line works for every level.
        // M3 replaces this with a proper game over screen.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
