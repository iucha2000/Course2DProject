using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 14f;
    [Tooltip("Backstop in case the shuriken somehow never leaves the screen.")]
    [SerializeField] private float lifetime = 3f;
    [Tooltip("How far past the edge of the screen the shuriken may travel before it despawns.")]
    [SerializeField] private float offScreenMargin = 0.03f;

    private Rigidbody2D rb;
    private Camera mainCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            return;
        }

        // Despawn as soon as the shuriken leaves the visible screen, otherwise the player
        // could kill enemies far away that they cannot even see. The viewport is 0..1 on
        // both axes, so anything outside that range is off screen.
        //
        // This is done by hand rather than with OnBecameInvisible because that message also
        // counts the Scene view as a camera, so in the Editor it would not fire while the
        // Scene window still has the shuriken in frame.
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(transform.position);
        bool offScreen =
            viewportPoint.x < -offScreenMargin || viewportPoint.x > 1f + offScreenMargin ||
            viewportPoint.y < -offScreenMargin || viewportPoint.y > 1f + offScreenMargin;

        if (offScreen)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Called by PlayerCombat right after the shuriken is instantiated.</summary>
    public void Launch(Vector2 direction)
    {
        // Gravity scale is 0 on the prefab, so it keeps flying in a straight line.
        rb.linearVelocity = direction.normalized * speed;

        // Safety net only: normally the off-screen check in Update gets there first.
        // Destroy has an overload that waits the given number of seconds before removing.
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // The shuriken can never touch the player or another shuriken: those pairs are
        // switched off in the Physics 2D layer collision matrix, not checked here.
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();
            }
        }

        // Anything solid stops the shuriken, including the tilemap.
        Destroy(gameObject);
    }
}
