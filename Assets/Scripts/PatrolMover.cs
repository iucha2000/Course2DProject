using System.Collections;
using UnityEngine;

/// <summary>
/// Moves an object back and forth between where it starts and an offset from there.
/// Leave the offset at zero and the object simply stays put, so the same prefab works
/// for both a fixed saw and a moving one - the level decides which it is.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolMover : MonoBehaviour
{
    [Tooltip("How far from the starting position to travel. Zero means it does not move.")]
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float pauseAtEnds = 0.5f;

    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void Start()
    {
        if (offset == Vector2.zero)
        {
            // Nothing to do, so do not start a coroutine that would run forever.
            return;
        }

        StartCoroutine(Patrol());
    }

    private IEnumerator Patrol()
    {
        Vector2 startPosition = rb.position;
        Vector2 endPosition = startPosition + offset;

        while (true)
        {
            // "yield return" on another coroutine waits for it to finish before going on.
            yield return MoveTo(endPosition);
            yield return new WaitForSeconds(pauseAtEnds);
            yield return MoveTo(startPosition);
            yield return new WaitForSeconds(pauseAtEnds);
        }
    }

    private IEnumerator MoveTo(Vector2 target)
    {
        while (Vector2.Distance(rb.position, target) > 0.001f)
        {
            // MovePosition on a Kinematic body moves the collider through the physics
            // system, so contacts are generated properly along the way. Writing
            // transform.position instead teleports it and can skip them entirely.
            rb.MovePosition(Vector2.MoveTowards(rb.position, target, moveSpeed * Time.fixedDeltaTime));

            // Paired with MovePosition: the move is applied on the next physics step,
            // so stepping the loop in time with the physics clock is what makes it smooth.
            yield return new WaitForFixedUpdate();
        }
    }
}
