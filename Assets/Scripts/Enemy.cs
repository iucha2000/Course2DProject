using UnityEngine;

public class Enemy : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public float moveSpeed = 4f;
    public float chaseRange = 5f;
    public float giveUpRange = 8f;

    private Transform player;
    private Vector3 startPosition;
    private bool isChasing;

    void Start()
    {
        startPosition = transform.position;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

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
}
