using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Tooltip("Upward speed is multiplied by this if the jump button is released " +
             "while still rising. Lower = shorter tap-jumps. Holding the button " +
             "the whole way always reaches the full jumpForce height.")]
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;


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
    private PlayerStamina stamina;
    private PlayerKnockback knockback;
    private WallJumpScript wallJump;
    private float moveInput;

    // True from Jump() until the player stops rising — only our own jump can be
    // cut short, not a knockback pop.
    private bool jumpRising;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<DashScript>();
        stamina = GetComponent<PlayerStamina>();
        knockback = GetComponent<PlayerKnockback>();
        wallJump = GetComponent<WallJumpScript>();
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

        jumpRising = true;
    }

    public void ReleaseJump()
    {
        if (!jumpRising)
            return;

        jumpRising = false;

        if (rb.linearVelocity.y <= 0f)
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            rb.linearVelocity.y * jumpCutMultiplier
        );
    }

    void FixedUpdate()
    {
        if (jumpRising && rb.linearVelocity.y <= 0f)
            jumpRising = false;

        // Dash owns the Rigidbody while it's active — don't overwrite the burst.
        if (dash != null && dash.IsDashing)
            return;

        // Let a knockback impulse play out instead of overwriting it.
        if (knockback != null && knockback.IsKnockedBack)
        {
            jumpRising = false;
            return;
        }

        // Same for the push away from a wall at the start of a wall jump.
        if (wallJump != null && wallJump.IsWallJumping)
            return;

        float speedMultiplier = 1f;

        if (dash != null)
            speedMultiplier *= dash.SprintSpeedMultiplier;

        if (stamina != null)
            speedMultiplier *= stamina.SpeedMultiplier;

        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed * speedMultiplier,
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
