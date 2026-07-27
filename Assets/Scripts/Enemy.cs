using System.Collections;
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

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Transform player;
    private Vector3 startPosition;
    private bool isChasing;
    private Color armouredColor;

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
