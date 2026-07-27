using System.Collections;
using UnityEngine;

/// <summary>
/// Moves an object back and forth between where it starts and an offset from there.
/// Leave the offset at zero and the object simply stays put, so the same prefab works
/// for both a fixed saw and a moving one - the level decides which it is.
/// </summary>
public class PatrolMover : MonoBehaviour
{
    [Tooltip("How far from the starting position to travel. Zero means it does not move.")]
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float pauseAtEnds = 0.5f;

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
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + (Vector3)offset;

        while (true)
        {
            // "yield return" on another coroutine waits for it to finish before going on.
            yield return MoveTo(endPosition);
            yield return new WaitForSeconds(pauseAtEnds);
            yield return MoveTo(startPosition);
            yield return new WaitForSeconds(pauseAtEnds);
        }
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        // Vector3 comparison has a small built-in tolerance, which is what lets
        // this loop actually finish instead of never quite reaching the target.
        while (transform.position != target)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
