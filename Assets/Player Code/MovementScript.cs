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
    private DashScript dash;
    private float moveInput;

    // Set by DashScript while grounded and holding the dash button; 1 otherwise.
    public float SpeedMultiplier { get; set; } = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<DashScript>();
    }

    public void SetMoveInput(float input)
    {
        moveInput = input;
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
        // Dash owns the Rigidbody while it's active — don't overwrite the burst.
        if (dash != null && dash.IsDashing)
            return;

        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed * SpeedMultiplier,
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
