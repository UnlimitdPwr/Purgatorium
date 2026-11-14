using UnityEngine;

public class PlayerController1 : MonoBehaviour
{
    private PlayerInput playerInput;
    private MovementScript movement;
    private ParryScript parry;
    private DashScript dash;
    private BlockScript block;
    private BonfirePlacement bonfirePlacement;
    private PlayerKnockback knockback;
    private WallJumpScript wallJump;
    private LadderClimbScript ladderClimb;
    private PlayerStatusEffects status;
    private PlayerStateMachine stateMachine;

    private bool isResting;

    public bool IsResting => isResting;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        movement = GetComponent<MovementScript>();
        parry = GetComponent<ParryScript>();
        dash = GetComponent<DashScript>();
        block = GetComponent<BlockScript>();
        bonfirePlacement = GetComponent<BonfirePlacement>();
        knockback = GetComponent<PlayerKnockback>();
        wallJump = GetComponent<WallJumpScript>();
        ladderClimb = GetComponent<LadderClimbScript>();
        status = GetComponent<PlayerStatusEffects>();
        stateMachine = GetComponent<PlayerStateMachine>();
    }

    void Update()
    {
        if (isResting)
        {
            movement.SetMoveInput(0f);
            block.StopBlocking();
            return;
        }

        if (HandleStun())
            return;

        HandleMovement();
        HandleLadderClimb();
        HandleJump();
        HandleWallJump();
        HandleParry();
        HandleDash();
        HandleBlock();
        HandleBonfirePlacement();
    }

    // =========================
    // RESTING
    // =========================

    public void SetResting(bool resting)
    {
        isResting = resting;
    }

    // =========================
    // STUN
    // =========================

    // Returns true while stunned by a hit — all input is locked until it wears off.
    bool HandleStun()
    {
        if (knockback == null || !knockback.IsStunned)
            return false;

        movement.SetMoveInput(0f);

        if (dash != null)
            dash.SetDashHeld(false);

        if (block != null)
            block.StopBlocking();

        return true;
    }

    // =========================
    // MOVEMENT
    // =========================

    // Sends movement input only when the current state allows movement.
    void HandleMovement()
    {
        if (stateMachine != null && !stateMachine.CanMove())
        {
            movement.SetMoveInput(0f);
            return;
        }

        movement.SetMoveInput(playerInput.MoveInput);
    }

    // =========================
    // LADDER
    // =========================

    // Runs before HandleJump: jumping off a ladder replaces the normal jump
    // (which then does nothing anyway, since the player isn't grounded).
    void HandleLadderClimb()
    {
        if (ladderClimb == null)
            return;

        ladderClimb.SetClimbInput(playerInput.VerticalInput, playerInput.MoveInput);

        if (playerInput.JumpPressed)
            ladderClimb.TryJumpOff();
    }

    // =========================
    // JUMP
    // =========================

    void HandleJump()
    {
        if (stateMachine != null && !stateMachine.CanJump())
            return;

        if (playerInput.JumpPressed)
            movement.Jump();

        // Letting go early makes a shorter jump; holding gives the full height.
        if (playerInput.JumpReleased)
            movement.ReleaseJump();
    }

    // =========================
    // WALL JUMP
    // =========================

    // Runs after HandleJump: on the ground the normal jump wins, and
    // TryWallJump() does nothing because the player is still grounded.
    void HandleWallJump()
    {
        if (wallJump == null)
            return;

        if (playerInput.JumpPressed)
            wallJump.TryWallJump();
    }

    // =========================
    // PARRY
    // =========================

    void HandleParry()
    {
        // A status condition can stop parrying altogether.
        if (status != null && status.ParryDisabled)
            return;

        if (stateMachine != null && !stateMachine.CanParry())
            return;

        if (playerInput.ParryPressed)
            parry.TryParry();
    }

    // =========================
    // DASH
    // =========================


    // Handles dash input while respecting state restrictions and status effects.
    void HandleDash()
    {
        if (dash == null)
            return;

        // Status conditions can disable sprinting and/or dashing.
        bool sprintDisabled = status != null && status.SprintDisabled;
        bool dashDisabled = status != null && status.DashDisabled;

        // Check whether the current player state allows dashing.
        bool canDash = stateMachine == null || stateMachine.CanDash();

        // Update sprint input continuously.
        dash.SetDashHeld(playerInput.DashHeld && !sprintDisabled && canDash);

        // Only attempt a dash when the input is pressed and all conditions allow it.
        if (playerInput.DashPressed && !dashDisabled && canDash)
        {
            dash.TryDash();
        }
    }


    //BLOCKING

    // Handles blocking input and changes state only when blocking actually starts.
    void HandleBlock()
    {
        if (block == null)
            return;

        bool blockDisabled = status != null && status.BlockDisabled;

        bool canBlock = stateMachine == null || stateMachine.CanBlock();

        if (playerInput.BlockHeld && !blockDisabled && canBlock)
        {
            block.StartBlocking();
        }
        else
        {
            block.StopBlocking();
        }
    }

    // =========================
    // BONFIRE
    // =========================

    void HandleBonfirePlacement()
    {
        if (bonfirePlacement == null)
            return;

        if (playerInput.PlaceBonfirePressed)
            bonfirePlacement.TryPlaceBonfire();
    }
}
    