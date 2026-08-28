using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;


    // =========================
    // GROUND CHECK
    // =========================

    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;


    // =========================
    // PRIVATE VARIABLES
    // =========================

    private Rigidbody2D rb;
    private float moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetMoveInput(float input)
    {
        moveInput = input;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Jump()
    {
        if (!IsGrounded())
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed,
            rb.linearVelocity.y
        );
    }

    // =========================
    // GROUND CHECK
    // =========================

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    // =========================
    // GETTERS
    // =========================

    public float GetMoveInput()
    {
        return moveInput;
    }

    public float GetVerticalVelocity()
    {
        return rb.linearVelocity.y;
    }

    // =========================
    // GROUND CHECK VISIBLITY
    // =========================

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
