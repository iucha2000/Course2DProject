using System.Collections;
using UnityEngine;

/// <summary>
/// A platform that gives way shortly after the player stands on it, then comes back.
/// This is a separate component rather than a mode of <see cref="Platform"/>: the two do
/// different jobs, and a platform that both moves and crumbles is simply both components on
/// one object.
/// </summary>
// BoxCollider2D rather than Collider2D: RequireComponent cannot add an abstract type, so naming
// the concrete one is what actually gets added when the component is dropped on a bare object.
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CrumblingPlatform : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("How long the platform holds after the player lands on it.")]
    [SerializeField] private float crumbleDelay = 0.6f;
    [Tooltip("How long it stays gone before coming back.")]
    [SerializeField] private float respawnDelay = 2f;
    [Tooltip("Colour it fades to while giving way - the warning is the colour, the same " +
             "language the Spiked Enemy already uses for its vulnerable window.")]
    [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0.3f, 1f);

    private Collider2D platformCollider;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private bool crumbling;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (crumbling || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // The contact normal points from the player back towards us, so the player standing on
        // top gives a *downward* normal. Without this check, clipping the side of the platform
        // while jumping past would set it off from underneath.
        if (collision.GetContact(0).normal.y > -0.5f)
        {
            return;
        }

        StartCoroutine(Crumble());
    }

    private IEnumerator Crumble()
    {
        crumbling = true;

        // Telegraphed by colour rather than by shaking. Shaking would mean writing transform
        // positions on a kinematic body every frame, which is exactly the movement model this
        // project avoids everywhere else - and the player already reads colour as a warning.
        float elapsed = 0f;
        while (elapsed < crumbleDelay)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(baseColor, warningColor, elapsed / crumbleDelay);
            yield return null;
        }

        // Switching the collider and the renderer off is the whole of "disappearing". The object
        // stays alive so it can come back on its own; Destroy would need something else to
        // remember it and respawn it.
        platformCollider.enabled = false;
        spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        spriteRenderer.color = baseColor;
        platformCollider.enabled = true;
        spriteRenderer.enabled = true;
        crumbling = false;
    }
}
