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

    void HandleMovement()
    {
        movement.SetMoveInput(playerInput.MoveInput);
    }

    // =========================
    // JUMP
    // =========================

    void HandleJump()
    {
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
        if (playerInput.ParryPressed)
            parry.TryParry();
    }

    // =========================
    // DASH
    // =========================

    void HandleDash()
    {
        if (dash == null)
            return;

        dash.SetDashHeld(playerInput.DashHeld);

        if (playerInput.DashPressed)
            dash.TryDash();
    }

    //BLOCKING

    void HandleBlock()
    {
        if (block == null)
            return;

        if (playerInput.BlockHeld)
            block.StartBlocking();
        else
            block.StopBlocking();
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
    