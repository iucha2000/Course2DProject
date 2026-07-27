using UnityEngine;

/// <summary>
/// Anything that hurts the player on contact and cannot be killed, such as a saw.
/// Put this on an object with a trigger collider.
/// </summary>
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHurt(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Stay is needed as well as Enter. If the player is knocked back but is still
        // overlapping the saw when the invulnerability ends, Enter would never fire
        // again and the player could stand inside the blade taking no damage.
        TryHurt(other);
    }

    private void TryHurt(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        // A trigger has no contact point, so knock the player directly away from the
        // centre of the hazard instead of using a collision normal.
        Vector2 knockbackDirection = other.transform.position - transform.position;
        if (knockbackDirection.sqrMagnitude < 0.0001f)
        {
            knockbackDirection = Vector2.up;
        }

        // TakeHit ignores this while the player is already hurt, so calling it every
        // frame from OnTriggerStay2D is safe.
        player.TakeHit(knockbackDirection.normalized);
    }
}
