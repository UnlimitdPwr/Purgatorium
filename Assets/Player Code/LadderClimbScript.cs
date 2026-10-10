using UnityEngine;

// Ladder climbing. Inside a Ladder's trigger, pressing up or down grabs it:
// gravity is switched off and up/down moves the player along it. Left/right
// or jump lets go. PlayerController1 drives it from HandleLadderClimb().
[RequireComponent(typeof(Rigidbody2D))]
public class LadderClimbScript : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Tooltip("Climbing speed in units/second.")]
    [SerializeField] private float climbSpeed = 4f;

    [Tooltip("Jumping off a ladder gives this fraction of the normal jump.")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpOffMultiplier = 0.6f;

    // =========================
    // STATE
    // =========================

    private Rigidbody2D rb;
    private MovementScript movement;
    private PlayerKnockback knockback;

    private Ladder currentLadder;
    private float climbInput;
    private float normalGravityScale;

    public bool IsClimbing { get; private set; }

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MovementScript>();
        knockback = GetComponent<PlayerKnockback>();
        normalGravityScale = rb.gravityScale;
    }

    // =========================
    // INPUT ENTRY POINTS
    // =========================

    // Called every frame by PlayerController1 with up/down (W/S) and left/right input.
    public void SetClimbInput(float vertical, float horizontal)
    {
        climbInput = vertical;

        if (IsClimbing)
        {
            if (horizontal != 0f)
                StopClimbing();

            return;
        }

        if (vertical == 0f || currentLadder == null || !currentLadder.IsUsable)
            return;

        bool grounded = movement != null && movement.IsGrounded();

        // Standing at the foot of the ladder, down means nothing.
        if (grounded && vertical < 0f)
            return;

        // Already at (or above) the top — nothing to climb.
        if (vertical > 0f && rb.position.y >= currentLadder.TopY)
            return;

        StartClimbing();
    }

    // Called by PlayerController1 when jump is pressed. Returns true if it
    // jumped off a ladder (so the normal jump should be skipped).
    public bool TryJumpOff()
    {
        if (!IsClimbing)
            return false;

        StopClimbing();

        float jumpSpeed = movement != null ? movement.jumpForce * jumpOffMultiplier : 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
        return true;
    }

    // =========================
    // CLIMB
    // =========================

    void StartClimbing()
    {
        IsClimbing = true;
        rb.gravityScale = 0f;

        // Line up with the ladder.
        rb.position = new Vector2(currentLadder.CenterX, rb.position.y);
        rb.linearVelocity = Vector2.zero;
    }

    void StopClimbing()
    {
        if (!IsClimbing)
            return;

        IsClimbing = false;
        rb.gravityScale = normalGravityScale;
    }

    void FixedUpdate()
    {
        if (!IsClimbing)
            return;

        bool knockedBack = knockback != null && knockback.IsKnockedBack;
        bool ladderGone = currentLadder == null || !currentLadder.IsUsable;

        if (knockedBack || ladderGone)
        {
            StopClimbing();
            return;
        }

        // Reached the floor at the bottom.
        if (climbInput < 0f && movement != null && movement.IsGrounded())
        {
            StopClimbing();
            return;
        }

        float vertical = climbInput;

        // Stop at the top instead of climbing off into the air.
        if (vertical > 0f && rb.position.y >= currentLadder.TopY)
            vertical = 0f;

        rb.linearVelocity = new Vector2(0f, vertical * climbSpeed);
    }

    // =========================
    // LADDER TRACKING
    // =========================

    void OnTriggerEnter2D(Collider2D other)
    {
        Ladder ladder = other.GetComponent<Ladder>();

        if (ladder != null)
            currentLadder = ladder;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Ladder ladder = other.GetComponent<Ladder>();

        if (ladder == null || ladder != currentLadder)
            return;

        currentLadder = null;
        StopClimbing();
    }

    void OnDisable()
    {
        StopClimbing();
    }
}
