using System;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rigidbody2D;
    public Animator animator;
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float hurtDuration = 0.5f;
    public float knockbackForce = 5f;

    bool isGrounded;
    bool isHurt;

    void Start()
    {

    }

    private void Update()
    {
        if (!isHurt)
        {
            Move();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
        }

        isGrounded = CheckGround();
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", rigidbody2D.linearVelocity.y < -0.1f);
    }
    
    private void Move()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(horizontalInput, 0f, 0f);
        transform.Translate(movement * moveSpeed * Time.deltaTime);

        if (horizontalInput > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < 0)
        {
            spriteRenderer.flipX = true;
        }

        animator.SetFloat("Run", Math.Abs(horizontalInput));
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rigidbody2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetTrigger("Jump");
        }
    }

    bool CheckGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.down * 1.1f, Vector2.down, 0.25f);
        return hit.collider != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            other.gameObject.SetActive(false);
            print("Item collected!");
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Obstacle") && !isHurt)
        {
            Vector2 knockbackDirection = other.GetContact(0).normal;
            StartCoroutine(HurtRoutine(knockbackDirection));
        }
    }

    private IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        animator.SetFloat("Run", 0f);
        animator.SetTrigger("IsHurt");
        rigidbody2D.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(hurtDuration);
        isHurt = false;
    }
}
