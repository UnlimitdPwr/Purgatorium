using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 3f;

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
    private HazardReceiver hazards;
    private float moveDirection;

    // True while a jump/hop launched by Launch() is in the air. Movement
    // leaves the Rigidbody alone until it lands, so the arc isn't flattened.
    private bool launched;
    private float launchTime;

    // Launch happens with the feet still touching the ground — ignore the
    // ground check for this long so the jump doesn't "land" instantly.
    private const float MinAirTime = 0.1f;

    // 1 = facing right, -1 = facing left. The sprite art faces right, so this
    // starts at 1. Movement updates it; Face() sets it without moving.
    private float facingDirection = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        knockback = GetComponent<EnemyKnockback>();
        hazards = GetComponent<HazardReceiver>();
    }

    void FixedUpdate()
    {
        // Knockback owns the Rigidbody while it is active — don't overwrite the impulse.
        if (knockback != null && knockback.IsKnockedBack)
        {
            launched = false;
            return;
        }

        // Mid-jump: let the launch velocity carry the enemy until it lands.
        if (launched)
        {
            bool landed = Time.time - launchTime >= MinAirTime
                          && rb.linearVelocity.y <= 0.01f
                          && IsGrounded();

            if (!landed)
                return;

            launched = false;
        }

        float speed = hazards != null ? moveSpeed * hazards.SpeedMultiplier : moveSpeed;

        rb.linearVelocity = new Vector2(
            moveDirection * speed,
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

    // Sets the enemy flying with this velocity (a jump, hop or leap — see
    // EnemyJump for how the velocity is worked out). Doesn't change facing,
    // so a hop backwards keeps the enemy looking at the player.
    public void Launch(Vector2 velocity)
    {
        moveDirection = 0f;
        rb.linearVelocity = velocity;

        launched = true;
        launchTime = Time.time;
    }

    // True from Launch() until the enemy lands (or is knocked out of it).
    public bool IsLaunched => launched;

    // =========================
    // GROUND CHECK
    // =========================

    public bool IsGrounded()
    {
        if (groundCheck != null)
        {
            return Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
        }

        // No GroundCheck child — test a thin strip under the body instead.
        if (body == null)
            return false;

        Bounds bounds = body.bounds;

        return Physics2D.OverlapBox(
            new Vector2(bounds.center.x, bounds.min.y),
            new Vector2(bounds.size.x * 0.9f, groundCheckRadius * 2f),
            0f,
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
        return !IsLedgeAhead(direction) && !IsWallAhead(direction);
    }

    // True when there's no ground just in front of the feet.
    public bool IsLedgeAhead(float direction)
    {
        if (direction == 0f || body == null)
            return false;

        float sign = Mathf.Sign(direction);
        Bounds bounds = body.bounds;

        Vector2 ledgeProbe = new Vector2(
            bounds.center.x + sign * (bounds.extents.x + edgeLookAhead),
            bounds.min.y + 0.05f
        );

        return !Physics2D.Raycast(ledgeProbe, Vector2.down, ledgeCheckDepth, groundLayer);
    }

    // True when something solid is right in front of the body — anywhere
    // from just above the feet to the top of the head, so a knee-high step
    // or a low overhang counts as well as a full wall.
    public bool IsWallAhead(float direction)
    {
        if (direction == 0f || body == null)
            return false;

        float sign = Mathf.Sign(direction);
        Bounds bounds = body.bounds;

        // Thin box the height of the body, minus a little at the feet so the
        // floor itself isn't counted.
        const float footClearance = 0.1f;
        Vector2 size = new Vector2(0.05f, Mathf.Max(0.05f, bounds.size.y - footClearance - 0.02f));
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + footClearance + size.y * 0.5f);

        return Physics2D.BoxCast(origin, size, 0f, new Vector2(sign, 0f), bounds.extents.x + edgeLookAhead, groundLayer);
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

    // The enemy's body collider (null if it has none). EnemyJump sizes its
    // landing and arc checks from this.
    public Collider2D GetBody()
    {
        return body;
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