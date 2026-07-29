using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHearts = 3;

    [Header("Falling out of the level")]
    [Tooltip("How far below the camera bounds the player must fall before dying. Small, so " +
             "falling out of view kills almost immediately instead of after a silent drop.")]
    [SerializeField] private float fallDeathMargin = 2f;
    [Tooltip("Used only if a level has no camera bounds to measure against.")]
    [SerializeField] private float fallDeathHeight = -20f;

    [Header("Audio")]
    [SerializeField] private AudioClip deathClip;

    // The HUD reads these. Private set so only this script can change them.
    public int Hearts { get; private set; }
    public int MaxHearts => maxHearts;

    // SceneManager.LoadScene does not take effect until the end of the frame, so everything
    // carries on running after Die() is called. Without this, a second hit in the same frame -
    // or one more Update - would queue a second load.
    private bool isDead;

    // Where "fallen out of the level" actually starts, worked out in Start.
    private float deathHeight;

    private void Awake()
    {
        Hearts = maxHearts;
    }

    private void Start()
    {
        deathHeight = fallDeathHeight;

        // The camera bounds already describe where the level is, so there is no reason to write
        // a second number describing the same thing and then keep the two in step by hand.
        // Measuring from them means the death line follows the level whenever M4 resizes it.
        CinemachineConfiner2D confiner = FindFirstObjectByType<CinemachineConfiner2D>();

        if (confiner != null && confiner.BoundingShape2D != null)
        {
            deathHeight = confiner.BoundingShape2D.bounds.min.y - fallDeathMargin;
        }
    }

    private void Update()
    {
        // A pit is a hole in the tilemap, so there is no collider at the bottom of it and nothing
        // else in the game can notice the player has gone. A plain height test is the whole fix.
        if (!isDead && transform.position.y < deathHeight)
        {
            Hearts = 0;
            Die();
        }
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

    /// <summary>Back to full. Used by checkpoints, which heal as well as save.</summary>
    public void Refill()
    {
        Hearts = maxHearts;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        AudioManager.PlaySfx(deathClip);

        // Restart whichever level we are currently in. Reloading by build index rather
        // than by name means this one line works for every level.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
