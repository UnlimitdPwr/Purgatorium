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
    // LEDGE / WALL CHECK
    // =========================

    [Tooltip("How far past the front of the body to look for a ledge or wall.")]
    public float edgeLookAhead = 0.15f;

    [Tooltip("How far down to look for ground in front before calling it a ledge.")]
    public float ledgeCheckDepth = 1f;

    // =========================
    // PRIVATE VARIABLES
    // =========================

    private Rigidbody2D rb;
    private Collider2D body;
    private EnemyKnockback knockback;
    private float moveDirection;

    // 1 = facing right, -1 = facing left. The sprite art faces right, so this
    // starts at 1. Movement updates it; Face() sets it without moving.
    private float facingDirection = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
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
    // LEDGE / WALL CHECK
    // =========================

    // False when a step in this direction would walk off a ledge or into a
    // wall. EnemyController checks this before moving so patrols and chases
    // stop at the edge instead of falling off it.
    public bool CanMove(float direction)
    {
        if (direction == 0f || body == null)
            return true;

        float sign = Mathf.Sign(direction);
        Bounds bounds = body.bounds;

        // Ledge: is there ground just in front of the feet?
        Vector2 ledgeProbe = new Vector2(
            bounds.center.x + sign * (bounds.extents.x + edgeLookAhead),
            bounds.min.y + 0.05f
        );

        if (!Physics2D.Raycast(ledgeProbe, Vector2.down, ledgeCheckDepth, groundLayer))
            return false;

        // Wall: is something solid right in front of the body?
        Vector2 wallProbe = new Vector2(bounds.center.x, bounds.center.y);

        if (Physics2D.Raycast(wallProbe, new Vector2(sign, 0f), bounds.extents.x + edgeLookAhead, groundLayer))
            return false;

        return true;
    }

    // =========================
    // GETTERS
    // =========================

    public float GetMoveDirection()
    {
        return moveDirection;
    }

    // Bottom-centre of the enemy's body (see EnemyTargeting.GetTargetFeetPosition).
    public Vector2 GetFeetPosition()
    {
        if (body == null)
            return transform.position;

        Bounds bounds = body.bounds;
        return new Vector2(bounds.center.x, bounds.min.y);
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