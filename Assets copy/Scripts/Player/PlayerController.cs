using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 14f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer sr;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        float moveX = 0f;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
        }

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        if (moveX != 0f) sr.flipX = moveX < 0f;

        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(moveX));
            animator.SetBool("Grounded", isGrounded);
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }
    }
}
