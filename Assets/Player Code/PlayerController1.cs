using UnityEngine;

public class PlayerController1 : MonoBehaviour
{
    private PlayerInput playerInput;
    private MovementScript movement;
    private ParryScript parry;
    private DashScript dash;
    private BlockScript block;
    private BonfirePlacement bonfirePlacement;

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
    }

    void Update()
    {
        if (isResting)
        {
            movement.SetMoveInput(0f);
            block.StopBlocking();
            return;
        }

        HandleMovement();
        HandleJump();
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
    