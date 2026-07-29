using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerHealth))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Ground check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Taking damage")]
    [Tooltip("How long the player loses control of the character after being hit. Keep this short.")]
    [SerializeField] private float hitStunDuration = 0.25f;
    [Tooltip("How long the player cannot be hurt again after being hit. Keep this much longer.")]
    [SerializeField] private float invulnerabilityDuration = 1.2f;
    [SerializeField] private float knockbackForce = 5f;
    [Tooltip("How fast the sprite flashes while invulnerable.")]
    [SerializeField] private float blinkInterval = 0.08f;

    [Header("Dive and stomp")]
    [Tooltip("Downward speed when diving. Diving is the only way to kill an enemy by landing on it.")]
    [SerializeField] private float diveSpeed = 20f;
    [SerializeField] private float stompBounceForce = 11f;
    [Tooltip("How straight-down the contact has to be to count as a stomp. 1 = perfectly on top.")]
    [SerializeField] private float stompNormalThreshold = 0.5f;

    // Components on this same GameObject. Found in Awake so nothing has to be dragged
    // in the Inspector, which also means they can never be left null by accident.
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CapsuleCollider2D capsule;
    private PlayerCombat combat;
    private PlayerHealth health;

    private GameCanvasHandler gameCanvasHandler;
    private readonly Dictionary<string, int> collectedItemCounts = new();

    // Input is read in Update but applied in FixedUpdate, so it has to be
    // remembered in between.
    private float horizontalInput;
    private bool jumpRequested;
    private bool diveRequested;
    private bool diveKeyWasHeld;

    private bool isGrounded;
    private bool isDiving;

    // The moving platform we are currently standing on, or null. Filled in by CheckGround,
    // since the ground check already has to find out what is under our feet.
    private Platform carrier;

    // Two separate ideas that are easy to confuse:
    // isHurt        - the player has lost control for a moment (hit stun)
    // isInvulnerable - the player cannot be hurt again yet (invulnerability frames)
    // Invulnerability lasts much longer than the stun, so control comes back quickly
    // but you do not get shredded by standing next to a saw.
    private bool isHurt;
    private bool isInvulnerable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider2D>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        // The HUD lives elsewhere in the scene, so it can't be a RequireComponent.
        gameCanvasHandler = FindFirstObjectByType<GameCanvasHandler>();
    }

    private void Update()
    {
        ReadInput();

        animator.SetBool("IsGrounded", isGrounded);

        // Falling means airborne and heading down. The grounded test matters now that riding
        // a descending platform gives a real negative velocity while standing perfectly still
        // on it - without it, the fall animation would play the whole way down.
        animator.SetBool("IsFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
        animator.SetFloat("Run", Mathf.Abs(horizontalInput));
    }

    private void FixedUpdate()
    {
        // The ground check is a physics query, so it runs on the physics clock. Doing it in
        // Update left it a frame stale, because Unity runs every FixedUpdate for a frame
        // before that frame's Update - so the platform logic below was reading last frame's
        // answer about what we were standing on.
        isGrounded = CheckGround();

        if (isGrounded)
        {
            // Landing always ends a dive, whether it hit anything or not.
            isDiving = false;
        }

        // While hurt the player keeps the knockback velocity instead of being driven
        // by input, otherwise Move() would overwrite the knockback on the very next step.
        if (!isHurt)
        {
            Move();
        }

        if (jumpRequested)
        {
            Jump();
        }

        if (diveRequested)
        {
            Dive();
        }
    }

    private void ReadInput()
    {
        // Pressing S or Down while in the air slams the player straight down.
        // "Vertical" is a default Input Manager axis, so S and Down arrow both work.
        // GetAxisRaw skips the smoothing, so the dive starts the instant the key goes down.
        //
        // Only a fresh press counts, never a held key. Without that, bouncing off a stomped
        // enemy while still holding Down would immediately start another dive and wipe out
        // the bounce on the very next physics step.
        //
        // This is tracked before the hit-stun check below, otherwise letting go of the key
        // while stunned would go unnoticed and the next press would be ignored.
        bool diveKeyHeld = Input.GetAxisRaw("Vertical") < -0.5f;
        bool diveKeyPressed = diveKeyHeld && !diveKeyWasHeld;
        diveKeyWasHeld = diveKeyHeld;

        if (isHurt)
        {
            horizontalInput = 0f;
            return;
        }

        horizontalInput = Input.GetAxis("Horizontal");

        if (horizontalInput > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < 0)
        {
            spriteRenderer.flipX = true;
        }

        // GetKeyDown is only true for one frame, so it has to be polled in Update
        // and remembered until the next physics step.
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
        }

        if (!isGrounded && !isDiving && diveKeyPressed)
        {
            diveRequested = true;
        }
    }

    private void Dive()
    {
        diveRequested = false;
        isDiving = true;

        // Replace the current fall speed rather than adding to it, so a dive always
        // feels the same whether it starts at the top of a jump or halfway down.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -diveSpeed);
    }

    private void Move()
    {
        // Setting the velocity directly, instead of moving the transform, keeps the player
        // inside the physics simulation so it cannot tunnel through the tilemap collider.
        //
        // Note there is nothing about moving platforms here. A platform we are standing on
        // moves us by position in its own FixedUpdate, which leaves our velocity - and with it
        // our gravity, jumping and knockback - completely untouched. See Platform for why.
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        jumpRequested = false;

        // Zero out any leftover vertical speed first so every jump reaches the same height.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        animator.SetTrigger("Jump");
        // TODO(audio): AudioManager.Instance.PlaySfx(jumpClip);
    }

    private bool CheckGround()
    {
        // A small circle at the bottom of the capsule. The LayerMask is what makes this
        // reliable: Physics2D has "Queries Start In Colliders" enabled project-wide, so
        // without a mask this could hit the player's own collider and always say grounded.
        Vector2 feet = new Vector2(capsule.bounds.center.x, capsule.bounds.min.y);
        Collider2D ground = Physics2D.OverlapCircle(feet, groundCheckRadius, groundLayer);

        // The ground check is already the one thing that knows what is under our feet, so it
        // is also what decides which platform we are riding.
        SetCarrier(ground != null ? ground.GetComponent<Platform>() : null);

        return ground != null;
    }

    /// <summary>
    /// Registers or deregisters us with the platform we are standing on. Riding is a
    /// registration rather than something we read each frame: the platform moves us itself,
    /// in the very step it moves in, so we can never act on a stale idea of where it is.
    /// </summary>
    private void SetCarrier(Platform platform)
    {
        if (platform == carrier)
        {
            return;
        }

        if (carrier != null)
        {
            carrier.RemoveRider(rb);
        }

        carrier = platform;

        if (carrier != null)
        {
            carrier.AddRider(rb);
        }
    }

    private void OnDisable()
    {
        // Do not leave a dangling entry in a platform's rider list when the level reloads.
        SetCarrier(null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Item"))
        {
            return;
        }

        Item item = other.GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        switch (item.itemType)
        {
            case ItemType.Shuriken:
                combat.AddAmmo(1);
                break;
            case ItemType.Cherry:
                health.Heal(1);
                break;
            case ItemType.Coin:
                // Coins are score only.
                break;
        }

        collectedItemCounts.TryGetValue(item.itemName, out int currentCount);
        int newCount = currentCount + 1;
        collectedItemCounts[item.itemName] = newCount;

        if (gameCanvasHandler != null)
        {
            gameCanvasHandler.DisplayItemInfo(item.itemName, item.itemSprite);
            gameCanvasHandler.UpdateCollectedCount(item.itemName, newCount);
        }

        // TODO(audio): AudioManager.Instance.PlaySfx(pickupClip);
        Destroy(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // The contact normal points from the surface we hit back towards us, so a normal
        // pointing upwards means we landed on top of whatever we just touched.
        Vector2 normal = other.GetContact(0).normal;
        bool hitEnemy = other.collider.CompareTag("Enemy");

        // A stomp needs all three: coming down on top of it, actively diving, and an
        // enemy that is currently stompable. Merely falling onto an enemy hurts you,
        // so killing something by landing on it has to be a deliberate act.
        if (hitEnemy && isDiving && normal.y > stompNormalThreshold)
        {
            Enemy enemy = other.collider.GetComponent<Enemy>();
            if (enemy != null && enemy.canBeStomped)
            {
                enemy.Die();

                // Bounce off the enemy, the same way jumping works.
                isDiving = false;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * stompBounceForce, ForceMode2D.Impulse);
                // TODO(audio): AudioManager.Instance.PlaySfx(stompClip);
                return;
            }
        }

        if (hitEnemy || other.collider.CompareTag("Obstacle"))
        {
            TakeHit(normal);
        }
    }

    /// <summary>
    /// The single way the player gets hurt, whether from an enemy, an obstacle or a hazard.
    /// Ignored while already hurt, which is what gives the brief invulnerability after a hit.
    /// </summary>
    public void TakeHit(Vector2 knockbackDirection)
    {
        // Invulnerability, not hit stun, is what blocks a second hit. That is the whole
        // point of separating them: control returns long before you can be hurt again.
        if (isInvulnerable)
        {
            return;
        }

        StartCoroutine(HurtRoutine(knockbackDirection));
    }

    private IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        isInvulnerable = true;
        isDiving = false;

        animator.SetFloat("Run", 0f);
        animator.SetTrigger("IsHurt");
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        health.TakeDamage(1);
        // TODO(audio): AudioManager.Instance.PlaySfx(hurtClip);

        // Flash the sprite for the whole invulnerable window. Without this the player has
        // no way of knowing they are safe, which is what makes taking damage feel random.
        float elapsed = 0f;
        bool controlRestored = false;

        while (elapsed < invulnerabilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;

            float wait = Mathf.Min(blinkInterval, invulnerabilityDuration - elapsed);
            yield return new WaitForSeconds(wait);
            elapsed += wait;

            if (!controlRestored && elapsed >= hitStunDuration)
            {
                isHurt = false;
                controlRestored = true;
            }
        }

        // Always leave the sprite visible, whichever half of the blink we ended on.
        spriteRenderer.enabled = true;
        isHurt = false;
        isInvulnerable = false;
    }
}
