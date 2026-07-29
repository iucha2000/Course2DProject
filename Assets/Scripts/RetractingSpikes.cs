using System.Collections;
using UnityEngine;

/// <summary>
/// Floor spikes that sink and rise on a fixed cycle, so a corridor is only safe part of the time.
/// The player is meant to read the rhythm and move on the beat, which is why the retracted state
/// is still visible - a hazard that vanished completely would be a memory test instead.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class RetractingSpikes : MonoBehaviour
{
    [Header("Cycle")]
    [Tooltip("How long the spikes stay out and dangerous.")]
    [SerializeField] private float upDuration = 1.5f;
    [Tooltip("How long they stay retracted and safe to cross.")]
    [SerializeField] private float downDuration = 1.5f;
    [Tooltip("Seconds of warning before they come back up. The tell is what makes this fair.")]
    [SerializeField] private float warningDuration = 0.4f;
    [Tooltip("Offset added while retracted, so they visibly sink into the floor.")]
    [SerializeField] private Vector2 retractedOffset = new Vector2(0f, -0.55f);
    [Tooltip("Stagger, so a row of them does not fire as one wall. Set per instance.")]
    [SerializeField] private float startDelay;

    private BoxCollider2D spikeCollider;
    private SpriteRenderer spriteRenderer;
    private Vector3 extendedPosition;
    private Color baseColor;

    private void Awake()
    {
        spikeCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        extendedPosition = transform.position;
        baseColor = spriteRenderer.color;
    }

    private void Start()
    {
        StartCoroutine(Cycle());
    }

    private IEnumerator Cycle()
    {
        // Staggering happens here rather than by giving each instance its own timer, so a row
        // still shares one rhythm and only the phase differs.
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            SetExtended(true);
            yield return new WaitForSeconds(upDuration);

            SetExtended(false);
            yield return new WaitForSeconds(Mathf.Max(0f, downDuration - warningDuration));

            // The warning is a colour pulse while still retracted and still harmless. Without a
            // tell, a timed hazard is only survivable by having died to it once already.
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                spriteRenderer.color = Color.Lerp(baseColor, Color.white,
                                                  Mathf.PingPong(elapsed * 8f, 1f));
                yield return null;
            }
            spriteRenderer.color = baseColor;
        }
    }

    private void SetExtended(bool extended)
    {
        // Moving by transform is fine here: this object has no Rigidbody2D, so there is no
        // physics body whose motion we could be fighting. It is scenery that switches state.
        transform.position = extended ? extendedPosition
                                      : extendedPosition + (Vector3)retractedOffset;

        // Switching the collider off is what actually makes it safe; sinking it is the tell.
        spikeCollider.enabled = extended;
        spriteRenderer.color = baseColor;
    }
}
