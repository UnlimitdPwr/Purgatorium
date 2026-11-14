
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    // =========================
    // STATES
    // =========================

    public enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        Blocking,
        Parrying,
        Dashing,
        Attacking,
        Climbing,
        WallJumping,
        Stunned,
        Resting,
        Dead
    }

    public PlayerState CurrentState { get; private set; }

    // =========================
    // COMPONENT REFERENCES
    // =========================

    private MovementScript movement;
    private BlockScript block;
    private DashScript dash;
    private ParryScript parry;
    private WallJumpScript wallJump;
    private LadderClimbScript ladderClimb;
    private PlayerKnockback knockback;
    private PlayerController1 controller;
    private PlayerDeath death;

    // =========================
    // SETUP
    // =========================

    // This function gets the required components and subscribes to action events.
    void Awake()
    {
        movement = GetComponent<MovementScript>();
        block = GetComponent<BlockScript>();
        dash = GetComponent<DashScript>();
        parry = GetComponent<ParryScript>();
        wallJump = GetComponent<WallJumpScript>();
        ladderClimb = GetComponent<LadderClimbScript>();
        knockback = GetComponent<PlayerKnockback>();
        controller = GetComponent<PlayerController1>();
        death = GetComponent<PlayerDeath>();

        CurrentState = PlayerState.Idle;

        // Subscribe to dash events.
        if (dash != null)
        {
            dash.OnDashStarted += HandleDashStarted;
            dash.OnDashEnded += HandleDashEnded;
        }

        // Subscribe to parry events.
        if (parry != null)
        {
            parry.OnParryStarted += HandleParryStarted;
            parry.OnParryEnded += HandleParryEnded;
        }
    }

    // =========================
    // UPDATE
    // =========================

    // This function checks the player's current actions and updates their state.
    void Update()
    {
        UpdateMovementState();
    }

    // This function unsubscribes from events when the state machine is destroyed.
    void OnDestroy()
    {
        if (dash != null)
        {
            dash.OnDashStarted -= HandleDashStarted;
            dash.OnDashEnded -= HandleDashEnded;
        }

        if (parry != null)
        {
            parry.OnParryStarted -= HandleParryStarted;
            parry.OnParryEnded -= HandleParryEnded;
        }
    }

    // =========================
    // ACTION EVENT HANDLERS
    // =========================

    // This function switches the player into the dashing state when a dash begins.
    void HandleDashStarted()
    {
        ChangeState(PlayerState.Dashing);
    }

    // This function updates the player's state when a dash ends.
    void HandleDashEnded()
    {
        UpdateMovementState();
    }

    // This function switches the player into the parrying state when a parry begins.
    void HandleParryStarted()
    {
        ChangeState(PlayerState.Parrying);
    }

    // This function updates the player's state when a parry ends.
    void HandleParryEnded()
    {
        UpdateMovementState();
    }

    // =========================
    // STATE UPDATES
    // =========================

    // This function determines which state should currently be active.
    void UpdateMovementState()
    {
        if (movement == null)
            return;

        // Dead has the highest priority.
        if (death != null && death.IsDead)
        {
            ChangeState(PlayerState.Dead);
            return;
        }

        // Keep the player dead until the death script completes a respawn.
        if (CurrentState == PlayerState.Dead &&
            (death == null || death.IsDead))
            return;

        // Resting takes priority over normal movement and actions.
        if (controller != null && controller.IsResting)
        {
            ChangeState(PlayerState.Resting);
            return;
        }

        // Stun prevents normal player actions.
        if (knockback != null && knockback.IsStunned)
        {
            ChangeState(PlayerState.Stunned);
            return;
        }

        // Parrying takes priority over ordinary movement.
        if (parry != null && parry.IsParrying)
        {
            ChangeState(PlayerState.Parrying);
            return;
        }

        // Dashing takes priority over ordinary movement.
        if (dash != null && dash.IsDashing)
        {
            ChangeState(PlayerState.Dashing);
            return;
        }

        // Blocking takes priority over ordinary movement.
        if (block != null && block.IsBlocking)
        {
            ChangeState(PlayerState.Blocking);
            return;
        }

        // Enter the climbing state while climbing a ladder.
        if (ladderClimb != null && ladderClimb.IsClimbing)
        {
            ChangeState(PlayerState.Climbing);
            return;
        }

        // Enter the wall-jumping state during the wall-jump input lock.
        if (wallJump != null && wallJump.IsWallJumping)
        {
            ChangeState(PlayerState.WallJumping);
            return;
        }

        // Determine whether the player is grounded or airborne.
        bool grounded = movement.IsGrounded();

        float moveInput = movement.GetMoveInput();
        float verticalVelocity = movement.GetVerticalVelocity();

        // Player is rising.
        if (!grounded && verticalVelocity > 0.1f)
        {
            ChangeState(PlayerState.Jumping);
            return;
        }

        // Player is falling.
        if (!grounded)
        {
            ChangeState(PlayerState.Falling);
            return;
        }

        // Player is moving horizontally.
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            ChangeState(PlayerState.Moving);
            return;
        }

        // Player is standing still.
        ChangeState(PlayerState.Idle);
    }

    // =========================
    // STATE MANAGEMENT
    // =========================

    // This function changes the player's state while preventing invalid transitions.
    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState)
            return;

        // Prevent leaving Dead until the death script completes a respawn.
        if (CurrentState == PlayerState.Dead &&
            (death == null || death.IsDead))
            return;

        CurrentState = newState;

        Debug.Log("Player State: " + CurrentState);
    }

    // =========================
    // STATE PERMISSIONS
    // =========================

    // This function checks whether the player is allowed to move horizontally.
    public bool CanMove()
    {
        return CurrentState == PlayerState.Idle ||
               CurrentState == PlayerState.Moving ||
               CurrentState == PlayerState.Jumping ||
               CurrentState == PlayerState.Falling;
    }

    // This function checks whether the player is allowed to block.
    public bool CanBlock()
    {
        return CurrentState == PlayerState.Idle ||
               CurrentState == PlayerState.Moving;
    }

    // This function checks whether the player is allowed to jump normally.
    public bool CanJump()
    {
        return CurrentState == PlayerState.Idle ||
               CurrentState == PlayerState.Moving;
    }

    // This function checks whether the player is allowed to dash.
    public bool CanDash()
    {
        return CurrentState == PlayerState.Idle ||
               CurrentState == PlayerState.Moving ||
               CurrentState == PlayerState.Jumping ||
               CurrentState == PlayerState.Falling;
    }

    // This function checks whether the player is allowed to parry.
    public bool CanParry()
    {
        return CurrentState == PlayerState.Idle ||
               CurrentState == PlayerState.Moving;
    }
}

