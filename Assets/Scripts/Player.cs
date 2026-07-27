using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Ground check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Taking damage")]
    public float hurtDuration = 0.5f;
    public float knockbackForce = 5f;

    [Header("Stomping enemies")]
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

    private GameCanvasHandler gameCanvasHandler;
    private readonly Dictionary<string, int> collectedItemCounts = new();

    // Input is read in Update but applied in FixedUpdate, so it has to be
    // remembered in between.
    private float horizontalInput;
    private bool jumpRequested;

    private bool isGrounded;
    private bool isHurt;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider2D>();
        combat = GetComponent<PlayerCombat>();
    }

    private void Start()
    {
        // The HUD lives elsewhere in the scene, so it can't be a RequireComponent.
        gameCanvasHandler = FindFirstObjectByType<GameCanvasHandler>();
    }

    private void Update()
    {
        isGrounded = CheckGround();
        ReadInput();

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", rb.linearVelocity.y < -0.1f);
        animator.SetFloat("Run", Mathf.Abs(horizontalInput));
    }

    private void FixedUpdate()
    {
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
    }

    private void ReadInput()
    {
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
    }

    private void Move()
    {
        // Setting the velocity directly, instead of moving the transform, keeps the player
        // inside the physics simulation so it cannot tunnel through the tilemap collider.
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
        return Physics2D.OverlapCircle(feet, groundCheckRadius, groundLayer) != null;
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
                if (combat != null)
                {
                    combat.AddAmmo(1);
                }
                break;
            case ItemType.Cherry:
                // Healing is added in M2 together with PlayerHealth.
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

        if (hitEnemy && normal.y > stompNormalThreshold)
        {
            Enemy enemy = other.collider.GetComponent<Enemy>();
            if (enemy != null && enemy.canBeStomped)
            {
                enemy.Die();

                // Bounce off the enemy, the same way jumping works.
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * stompBounceForce, ForceMode2D.Impulse);
                // TODO(audio): AudioManager.Instance.PlaySfx(stompClip);
                return;
            }
        }

        if ((hitEnemy || other.collider.CompareTag("Obstacle")) && !isHurt)
        {
            StartCoroutine(HurtRoutine(normal));
        }
    }

    private IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        animator.SetFloat("Run", 0f);
        animator.SetTrigger("IsHurt");
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        // TODO(audio): AudioManager.Instance.PlaySfx(hurtClip);

        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
    }
}
