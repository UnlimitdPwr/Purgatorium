using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
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
    private EnemyKnockback knockback;
    private float moveDirection;

    // 1 = facing right, -1 = facing left. The sprite art faces right, so this
    // starts at 1. Movement updates it; Face() sets it without moving.
    private float facingDirection = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<EnemyKnockback>();
    }

    void FixedUpdate()
    {
        // Knockback owns the Rigidbody while it is active — don't overwrite the impulse.
        if (knockback != null && knockback.IsKnockedBack)
            return;

        rb.linearVelocity = new Vector2(
            moveDirection * moveSpeed,
            rb.linearVelocity.y
        );
    }

    // =========================
    // MOVEMENT
    // =========================

    public void Move(float direction)
    {
        moveDirection = direction;

        if (direction != 0f)
            facingDirection = Mathf.Sign(direction);
    }

    // Turn toward a direction without moving (e.g. to face the player
    // before an attack).
    public void Face(float direction)
    {
        if (direction != 0f)
            facingDirection = Mathf.Sign(direction);
    }

    public void Stop()
    {
        moveDirection = 0f;
    }

    // =========================
    // JUMP
    // =========================

    public void Jump()
    {
        if (!IsGrounded())
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
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

    public float GetMoveDirection()
    {
        return moveDirection;
    }

    public float GetFacingDirection()
    {
        return facingDirection;
    }

    public float GetVerticalVelocity()
    {
        return rb.linearVelocity.y;
    }

    // =========================
    // GROUND CHECK VISIBILITY
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