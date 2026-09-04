using UnityEngine;

public class PlayerController1 : MonoBehaviour
{
    private PlayerInput playerInput;
    private MovementScript movement;
    private ParryScript parry;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        movement = GetComponent<MovementScript>();
        parry = GetComponent<ParryScript>();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleParry();
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
}
    