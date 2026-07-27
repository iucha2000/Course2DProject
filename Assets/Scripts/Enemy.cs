using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float chaseRange = 5f;
    public float giveUpRange = 8f;

    [Header("Combat")]
    [Tooltip("Spiky enemies cannot be jumped on and have to be killed with a shuriken.")]
    public bool canBeStomped = true;
    [SerializeField] private GameObject ammoDropPrefab;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Transform player;
    private Vector3 startPosition;
    private bool isChasing;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        startPosition = transform.position;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
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

        if (Mathf.Abs(horizontalDistance) > 0.05f)
        {
            Vector3 newPosition = transform.position;
            newPosition.x = Mathf.MoveTowards(transform.position.x, target.x, moveSpeed * Time.deltaTime);
            transform.position = newPosition;

            spriteRenderer.flipX = horizontalDistance < 0;
            animator.SetFloat("Run", 1f);
        }
        else
        {
            animator.SetFloat("Run", 0f);
        }
    }

    /// <summary>Called when the player stomps this enemy or hits it with a shuriken.</summary>
    public void Die()
    {
        if (ammoDropPrefab != null)
        {
            // The reward for killing an enemy: one shuriken back.
            Instantiate(ammoDropPrefab, transform.position, Quaternion.identity);
        }

        // TODO(audio): AudioManager.Instance.PlaySfx(deathClip);
        Destroy(gameObject);
    }
}
