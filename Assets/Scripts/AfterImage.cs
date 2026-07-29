using System.Collections;
using UnityEngine;

/// <summary>
/// A frozen copy of the player's sprite, left behind during a dash and fading out where it stands.
///
/// <para>It copies whichever animation frame the player was on at that instant rather than using
/// art of its own, so the trail always matches what the player actually looks like - including
/// which way they are facing.</para>
///
/// <para>It destroys itself when the fade finishes. Nothing keeps a reference to it, which is the
/// point: the player fires and forgets, and there is no list to maintain or clean up on death.</para>
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AfterImage : MonoBehaviour
{
    [Tooltip("Keep this shorter than the dash itself, or the ghosts outlive the move that made " +
             "them and hang in the air as a clump after the player has stopped.")]
    [SerializeField] private float lifetime = 0.2f;

    [Tooltip("Starting colour. The alpha is faded to zero over the lifetime.")]
    [SerializeField] private Color tint = new Color(0.75f, 0.95f, 1f, 0.45f);

    private SpriteRenderer spriteRenderer;

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    /// <summary>Called by the player straight after spawning this, with its current appearance.</summary>
    public void Show(Sprite sprite, bool flipX, Vector3 scale)
    {
        spriteRenderer.sprite = sprite;
        spriteRenderer.flipX = flipX;
        spriteRenderer.color = tint;
        transform.localScale = scale;

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / lifetime);

            // Squared rather than linear. A linear fade spends half its life at half opacity,
            // which is exactly where a ghost looks like a second character standing there;
            // squaring drops it away quickly and leaves only the faintest tail behind.
            float remaining = 1f - t;

            Color faded = tint;
            faded.a = tint.a * remaining * remaining;
            spriteRenderer.color = faded;

            yield return null;
        }

        Destroy(gameObject);
    }
}
