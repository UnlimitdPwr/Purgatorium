using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController1 : MonoBehaviour
{
    private MovementScript movement;
    private ParryScript parry;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movement = GetComponent<MovementScript>();
        parry = GetComponent<ParryScript>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovementInput();
        HandleJumpInput();
        HandleParryInput();
    }

    // =========================
    // MOVEMENT INPUT
    // =========================

    void HandleMovementInput()
    {
        float x = 0f;

        if (Keyboard.current.aKey.isPressed)
            x = -1f;

        if (Keyboard.current.dKey.isPressed)
            x = 1f;

        movement.SetMoveInput(x);
    }


    // =========================
    // JUMP INPUT
    // =========================

    void HandleJumpInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            movement.Jump();
    }

    // =========================
    // PARRY INPUT
    // =========================

    void HandleParryInput()
    {
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
            parry.TryParry();
    }
}
    