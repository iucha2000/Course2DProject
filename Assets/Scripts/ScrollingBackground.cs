using UnityEngine;

/// <summary>
/// Drifts a decorative layer sideways forever, snapping back by exactly one pattern period so the
/// motion never visibly ends.
/// </summary>
public class ScrollingBackground : MonoBehaviour
{
    [Tooltip("Units per second. Small values read as distance - the further away something is " +
             "meant to look, the slower it should move.")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.35f, 0f);
    [Tooltip("The width of one repeat of the artwork. Wrapping by exactly this is invisible, " +
             "which is what lets an endless drift run on a finite tilemap.")]
    [SerializeField] private float wrapDistance = 8f;

    private float drifted;

    private void Update()
    {
        // Transform.Translate is the right tool here precisely *because* this object has no
        // Rigidbody2D. It is scenery: nothing collides with it and no solver has an opinion
        // about where it is, so there is no simulation to fight and nothing to tunnel through.
        // On a physics body the same call would teleport the collider past the solver, which is
        // exactly why the player stopped using it in M1.
        Vector3 step = new Vector3(scrollSpeed.x, scrollSpeed.y, 0f) * Time.deltaTime;
        transform.Translate(step);

        drifted += step.x;
        if (Mathf.Abs(drifted) >= wrapDistance)
        {
            // Snap back a whole period. Because the artwork repeats every wrapDistance, the
            // frame after the snap is identical to the frame before it.
            float shift = wrapDistance * Mathf.Sign(drifted);
            transform.position -= new Vector3(shift, 0f, 0f);
            drifted -= shift;
        }
    }
}
