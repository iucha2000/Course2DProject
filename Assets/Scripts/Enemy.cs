using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float chaseRange = 5f;
    public float giveUpRange = 8f;
    [Tooltip("How close to the target counts as arrived. Stops the enemy twitching on the spot.")]
    [SerializeField] private float stopDistance = 0.05f;

    [Header("What counts as solid")]
    // Keep this to Ground only. It must never include Default: CameraBounds is a level-sized
    // trigger sitting on Default, and Physics2D has both "Queries Hit Triggers" and "Queries
    // Start In Colliders" enabled, so every check below would report a hit everywhere and the
    // enemy would freeze on the spot. Solid scenery belongs on Ground instead.
    [Tooltip("Everything this enemy can stand on or is blocked by. Ground only.")]
    [SerializeField] private LayerMask solidLayers;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Ledge check")]
    [Tooltip("How far in front of the enemy to look for floor. Must reach past its own body.")]
    [SerializeField] private float ledgeCheckAhead = 1f;
    [Tooltip("How far down to look. Anything deeper than this counts as a drop.")]
    [SerializeField] private float ledgeCheckDepth = 1.2f;

    [Header("Jumping over obstacles")]
    [SerializeField] private float jumpForce = 10f;
    [Tooltip("Tallest thing the enemy will try to hop over, measured up from its feet. " +
             "Anything taller is treated as a wall and it stops instead of head-butting it.")]
    [SerializeField] private float maxJumpableHeight = 1.5f;
    [Tooltip("How far in front of its body to look for something blocking the way.")]
    [SerializeField] private float obstacleCheckDistance = 0.35f;
    [Tooltip("Stops the enemy re-jumping the instant it lands if it did not quite clear something.")]
    [SerializeField] private float jumpCooldown = 0.5f;

    [Header("Combat")]
    [Tooltip("Whether the player can currently kill this by diving onto it.")]
    public bool canBeStomped = true;
    [SerializeField] private GameObject ammoDropPrefab;

    [Header("Retractable spikes")]
    [Tooltip("If on, this enemy is armoured most of the time but drops its spikes for a short " +
             "window, so it can still be killed without ammo if the player times a dive well.")]
    [SerializeField] private bool hasRetractableSpikes = false;
    [SerializeField] private float spikesUpDuration = 3f;
    [SerializeField] private float spikesDownDuration = 1f;
    [Tooltip("Colour shown while the spikes are down. This is the player's only warning, so it " +
             "needs to be obviously different from the normal colour.")]
    [SerializeField] private Color vulnerableColor = new Color(1f, 0.9f, 0.35f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioClip deathClip;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CapsuleCollider2D capsule;

    private Transform player;
    private Vector3 startPosition;
    private bool isChasing;
    private Color armouredColor;

    // Decided in FixedUpdate and applied there as velocity; Update only reads them to drive the
    // animation. -1 = left, 0 = stand still, 1 = right.
    private float moveDirection;
    private float facingDirection = 1f;
    private bool isGrounded;
    private bool jumpRequested;
    private float nextJumpTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        startPosition = transform.position;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        if (hasRetractableSpikes)
        {
            // Remember whatever tint the prefab was given so the cycle can put it back.
            armouredColor = spriteRenderer.color;
            StartCoroutine(SpikeCycle());
        }
    }

    /// <summary>
    /// Alternates between armoured and vulnerable forever. The colour change is the only
    /// signal the player gets, so it doubles as the telegraph.
    /// </summary>
    private IEnumerator SpikeCycle()
    {
        while (true)
        {
            canBeStomped = false;
            spriteRenderer.color = armouredColor;
            yield return new WaitForSeconds(spikesUpDuration);

            canBeStomped = true;
            spriteRenderer.color = vulnerableColor;
            yield return new WaitForSeconds(spikesDownDuration);
        }
    }

    private void Update()
    {
        // Update is animation only. Everything that decides where this enemy is going is a
        // physics query, and Unity runs every FixedUpdate for a frame before that frame's
        // Update - so deciding here would act on answers that are already a frame stale.
        spriteRenderer.flipX = facingDirection < 0f;
        animator.SetFloat("Run", Mathf.Abs(moveDirection));
    }

    private void FixedUpdate()
    {
        isGrounded = IsGrounded();
        moveDirection = DecideDirection();

        // Drive the Rigidbody2D velocity rather than writing transform.position. This body
        // is Dynamic, and writing its transform teleports it past the solver: it can end up
        // overlapping the tilemap, which then shoves it back out and reads as jitter.
        // Same movement model as the player, so the two behave consistently.
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            jumpRequested = false;
            nextJumpTime = Time.time + jumpCooldown;

            // Same shape as Player.Jump: clear the leftover vertical speed first so every
            // hop reaches the same height regardless of what the body was doing.
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// The whole AI, in one place: chase or go home, then let the three physics checks veto it.
    /// Returns -1, 0 or 1. May also set <c>jumpRequested</c> as a side effect.
    /// </summary>
    private float DecideDirection()
    {
        if (player == null)
        {
            return 0f;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Two different ranges (start chasing closer than we stop chasing) so the enemy
        // does not flicker in and out of chasing while the player stands near the edge.
        if (!isChasing && distanceToPlayer <= chaseRange)
        {
            isChasing = true;
        }
        else if (isChasing && distanceToPlayer > giveUpRange)
        {
            isChasing = false;
        }

        Vector3 target = isChasing ? player.position : startPosition;
        float horizontalDistance = target.x - transform.position.x;

        if (Mathf.Abs(horizontalDistance) <= stopDistance)
        {
            return 0f;
        }

        float wantedDirection = Mathf.Sign(horizontalDistance);

        // Face the way we want to go even if we then decide not to move, so an enemy
        // waiting at the edge of a platform still looks at the player.
        facingDirection = wantedDirection;

        // Mid-air, keep whatever direction we jumped in - the checks below only make
        // sense from the ground, and re-running them would stall the jump halfway over.
        if (!isGrounded)
        {
            return wantedDirection;
        }

        if (IsBlockedAhead(wantedDirection))
        {
            if (CanClear(wantedDirection) && Time.time >= nextJumpTime)
            {
                // Small enough to hop over, so hop instead of grinding into it.
                jumpRequested = true;
                return wantedDirection;
            }

            // Too tall to clear. Stop rather than push against it forever.
            return 0f;
        }

        if (!HasGroundAhead(wantedDirection))
        {
            // The floor runs out ahead and there is nothing to jump onto. Hold position.
            return 0f;
        }

        return wantedDirection;
    }

    private bool IsGrounded()
    {
        Vector2 feet = new Vector2(capsule.bounds.center.x, capsule.bounds.min.y);
        return Physics2D.OverlapCircle(feet, groundCheckRadius, solidLayers) != null;
    }

    /// <summary>
    /// Casts a short ray straight down from just in front of the enemy's feet.
    /// No hit means the floor stops there.
    /// </summary>
    private bool HasGroundAhead(float direction)
    {
        // Start just above the feet, not level with them. Physics2D has "Queries Start In
        // Colliders" enabled project-wide, so an origin sunk into the tilemap would report
        // a hit no matter what is actually ahead.
        Vector2 origin = new Vector2(
            capsule.bounds.center.x + direction * ledgeCheckAhead,
            capsule.bounds.min.y + 0.05f);

        // The LayerMask is what keeps this honest: without it the ray could hit the
        // player, another enemy, or a pickup and mistake any of them for solid floor.
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, ledgeCheckDepth, solidLayers);
        return hit.collider != null;
    }

    /// <summary>Is something solid right in front of the enemy's feet?</summary>
    private bool IsBlockedAhead(float direction)
    {
        Vector2 origin = new Vector2(capsule.bounds.center.x, capsule.bounds.min.y + 0.1f);
        return Physics2D.Raycast(origin, Vector2.right * direction, ForwardCheckLength(), solidLayers);
    }

    /// <summary>
    /// Would a jump actually get over whatever is blocking us? Casts a second ray at the top
    /// of our jumping range: if that one is clear the obstacle is short enough to hop, and if
    /// it is blocked too then this is a wall and jumping would only bump our head.
    /// </summary>
    private bool CanClear(float direction)
    {
        Vector2 origin = new Vector2(capsule.bounds.center.x, capsule.bounds.min.y + maxJumpableHeight);
        return !Physics2D.Raycast(origin, Vector2.right * direction, ForwardCheckLength(), solidLayers);
    }

    /// <summary>
    /// Both forward rays start at the middle of the body, so they have to reach out past
    /// our own half-width before the extra look-ahead distance counts for anything.
    /// </summary>
    private float ForwardCheckLength() => capsule.bounds.extents.x + obstacleCheckDistance;

    /// <summary>Called when the player stomps this enemy or hits it with a shuriken.</summary>
    public void Die()
    {
        if (ammoDropPrefab != null)
        {
            // The reward for killing an enemy: one shuriken back.
            Instantiate(ammoDropPrefab, transform.position, Quaternion.identity);
        }

        AudioManager.PlaySfx(deathClip);
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    // Draws the three checks in the Scene view so their distances can be tuned by eye.
    private void OnDrawGizmosSelected()
    {
        CapsuleCollider2D c = GetComponent<CapsuleCollider2D>();
        if (c == null)
        {
            return;
        }

        float direction = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        float forward = c.bounds.extents.x + obstacleCheckDistance;

        // Ledge ray, yellow.
        Vector3 ledgeOrigin = new Vector3(
            c.bounds.center.x + direction * ledgeCheckAhead,
            c.bounds.min.y + 0.05f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector3.down * ledgeCheckDepth);

        // Blocked-ahead ray, red. Anything it hits stops the enemy.
        Vector3 lowOrigin = new Vector3(c.bounds.center.x, c.bounds.min.y + 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(lowOrigin, lowOrigin + Vector3.right * direction * forward);

        // Clearance ray, green. If this one is clear the obstacle is jumpable.
        Vector3 highOrigin = new Vector3(c.bounds.center.x, c.bounds.min.y + maxJumpableHeight);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(highOrigin, highOrigin + Vector3.right * direction * forward);
    }
#endif
}
