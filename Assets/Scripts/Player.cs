using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rigidbody2D;
    public Animator animator;
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    bool isGrounded;

    void Start()
    {
        
    }



    //MOVEMENT AND JUMPING
    private void Update()
    {
        Move();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
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



    //COLLISIONS AND TRIGGERS
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Obstacle"))
        {
            print("Collided with an obstacle!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            other.gameObject.SetActive(false);
            print("Item collected!");
        }
    }



    //MOUSE HOVER
    //private void OnMouseEnter()
    //{
    //    spriteRenderer.color = Color.red;
    //}
    //
    //private void OnMouseExit()
    //{
    //    spriteRenderer.color = Color.white;
    //}



    //MOUSE CLICKS
    //void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        Debug.Log("Left click");
    //    }
    //    if (Input.GetMouseButtonDown(2))
    //    {
    //        Debug.Log("Scroll click");
    //    }
    //    if (Input.GetMouseButtonDown(1))
    //    {
    //        Debug.Log("Right click");
    //    }
    //}



    //KEY PRESSES
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.W))
    //    {
    //        Debug.Log("W key pressed");
    //    }
    //    if (Input.GetKeyDown(KeyCode.A))
    //    {
    //        Debug.Log("A key pressed");
    //    }
    //    if (Input.GetKeyDown(KeyCode.S))
    //    {
    //        Debug.Log("S key pressed");
    //    }
    //    if (Input.GetKeyDown(KeyCode.D))
    //    {
    //        Debug.Log("D key pressed");
    //    }

}
