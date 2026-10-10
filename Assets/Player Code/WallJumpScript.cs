using UnityEngine;

// Wall slide + wall jump. While airborne and pushing into a climbable wall the
// player slides down slowly; pressing jump there launches them up and away.
// PlayerController1 calls TryWallJump() from HandleWallJump().
[RequireComponent(typeof(Rigidbody2D))]
public class WallJumpScript : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Header("Wall Detection")]
    [Tooltip("How far past the side of the player's collider to look for a wall.")]
    [SerializeField] private float wallCheckDistance = 0.1f;

    [Tooltip("Only walls with a WallJumpSurface component count. Turn off to let " +
             "every wall on the ground layer be climbed.")]
    [SerializeField] private bool requireWallJumpSurface = true;

    [Header("Wall Slide")]
    [Tooltip("Fastest the player falls while pushing into a wall, in units/second.")]
    [SerializeField] private float wallSlideMaxFallSpeed = 2f;

    [Header("Wall Jump")]
    [Tooltip("Speed away from the wall at the start of a wall jump.")]
    [SerializeField] private float wallJumpHorizontalSpeed = 6f;

    [Tooltip("Upward speed at the start of a wall jump.")]
    [SerializeField] private float wallJumpVerticalSpeed = 11f;

    [Tooltip("Seconds after a wall jump during which move input is ignored, so " +
             "holding toward the wall doesn't cancel the push away from it.")]
    [SerializeField] private float inputLockDuration = 0.15f;

    [Tooltip("Seconds after leaving a wall that a wall jump is still allowed.")]
    [SerializeField] private float wallCoyoteTime = 0.1f;

    // =========================
    // STATE
    // =========================

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private MovementScript movement;

    // -1 = wall on the left, 1 = wall on the right, 0 = no wall.
    private int wallSide;
    private int lastWallSide;
    private float coyoteTimer;
    private float inputLockTimer;

    public bool IsTouchingWall => wallSide != 0;
    public bool IsWallSliding { get; private set; }

    // MovementScript stands down while this is true so the launch isn't overwritten.
    public bool IsWallJumping => inputLockTimer > 0f;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MovementScript>();

        // The body collider, not a trigger child (parry/pickup areas etc.).
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
            {
                bodyCollider = col;
                break;
            }
        }
    }

    // =========================
    // INPUT ENTRY POINT
    // =========================

    // Called by PlayerController1 when jump is pressed. Returns true if a wall
    // jump happened.
    public bool TryWallJump()
    {
        if (movement == null || movement.IsGrounded())
            return false;

        int side = wallSide != 0 ? wallSide : (coyoteTimer > 0f ? lastWallSide : 0);

        if (side == 0)
            return false;

        rb.linearVelocity = new Vector2(
            -side * wallJumpHorizontalSpeed,
            wallJumpVerticalSpeed
        );

        inputLockTimer = inputLockDuration;
        coyoteTimer = 0f;
        IsWallSliding = false;
        return true;
    }

    // =========================
    // TICK
    // =========================

    void Update()
    {
        if (inputLockTimer > 0f)
            inputLockTimer -= Time.deltaTime;

        if (coyoteTimer > 0f)
            coyoteTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        bool grounded = movement != null && movement.IsGrounded();

        wallSide = grounded ? 0 : DetectWall();

        if (wallSide != 0)
        {
            lastWallSide = wallSide;
            coyoteTimer = wallCoyoteTime;
        }

        // Slide only while pushing into the wall and falling faster than the cap.
        float moveInput = movement != null ? movement.GetMoveInput() : 0f;
        bool pushingIntoWall = wallSide != 0 && Mathf.Sign(moveInput) == wallSide && moveInput != 0f;

        IsWallSliding = pushingIntoWall && !IsWallJumping && rb.linearVelocity.y < 0f;

        if (IsWallSliding && rb.linearVelocity.y < -wallSlideMaxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideMaxFallSpeed);
        }
    }

    // =========================
    // WALL DETECTION
    // =========================

    int DetectWall()
    {
        if (bodyCollider == null || movement == null)
            return 0;

        if (HasWallOnSide(1))
            return 1;

        if (HasWallOnSide(-1))
            return -1;

        return 0;
    }

    bool HasWallOnSide(int side)
    {
        Bounds bounds = bodyCollider.bounds;

        // A thin box just outside the collider's side, shorter than the body so
        // the floor and ceiling aren't mistaken for walls.
        Vector2 center = new Vector2(
            bounds.center.x + side * (bounds.extents.x + wallCheckDistance * 0.5f),
            bounds.center.y
        );
        Vector2 size = new Vector2(wallCheckDistance, bounds.size.y * 0.6f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, movement.groundLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.isTrigger)
                continue;

            if (!requireWallJumpSurface || hit.GetComponent<WallJumpSurface>() != null)
                return true;
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();

        if (col == null)
            return;

        Bounds bounds = col.bounds;
        Vector3 size = new Vector3(wallCheckDistance, bounds.size.y * 0.6f, 0f);

        Gizmos.DrawWireCube(bounds.center + Vector3.right * (bounds.extents.x + wallCheckDistance * 0.5f), size);
        Gizmos.DrawWireCube(bounds.center + Vector3.left * (bounds.extents.x + wallCheckDistance * 0.5f), size);
    }
}
