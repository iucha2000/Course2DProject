using System.Collections;
using UnityEngine;

public class Platform : MonoBehaviour
{
    private Vector3 startPosition;
    public Vector3 endPosition;
    public float moveSpeed = 2f;

    void Start()
    {
        startPosition = transform.position;
        StartCoroutine(Move());
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    private IEnumerator Move()
    {
        while (true)
        {
            while (transform.position != endPosition)
            {
                transform.position = Vector3.MoveTowards(transform.position, endPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(1);

            while (transform.position != startPosition)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(1);
        }
    }
}
