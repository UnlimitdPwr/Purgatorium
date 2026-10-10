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
    private Collider2D bodyCollider;
    private MovementScript movement;
    private PlayerKnockback knockback;

    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private ContactFilter2D triggerFilter;

    private Ladder currentLadder;
    private float climbInput;
    private float normalGravityScale;

    public bool IsClimbing { get; private set; }

    // The climb stops this far below the ladder's top, so the body stays
    // inside the ladder instead of slipping off the top edge.
    const float TopMargin = 0.1f;

    float Feet => bodyCollider != null ? bodyCollider.bounds.min.y : rb.position.y;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MovementScript>();
        knockback = GetComponent<PlayerKnockback>();
        normalGravityScale = rb.gravityScale;

        // The body collider, not a trigger child (parry/pickup areas etc.).
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
            {
                bodyCollider = col;
                break;
            }
        }

        triggerFilter = new ContactFilter2D();
        triggerFilter.useTriggers = true;
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
        if (vertical > 0f && Feet >= currentLadder.TopY - TopMargin)
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
        currentLadder = FindOverlappingLadder();

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
        if (vertical > 0f && Feet >= currentLadder.TopY - TopMargin)
            vertical = 0f;

        rb.linearVelocity = new Vector2(0f, vertical * climbSpeed);
    }

    // =========================
    // LADDER TRACKING
    // =========================

    // Checked against the body collider only — the player's trigger children
    // (parry, pickup range...) entering or leaving a ladder must not count.
    Ladder FindOverlappingLadder()
    {
        if (bodyCollider == null)
            return null;

        int count = bodyCollider.Overlap(triggerFilter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Ladder ladder = overlapResults[i].GetComponent<Ladder>();

            if (ladder != null)
                return ladder;
        }

        return null;
    }

    void OnDisable()
    {
        StopClimbing();
    }
}
