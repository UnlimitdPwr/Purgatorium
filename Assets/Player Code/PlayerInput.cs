using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public float MoveInput { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool ParryPressed { get; private set; }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            // No keyboard connected this frame — report neutral input.
            MoveInput = 0f;
            JumpPressed = false;
            ParryPressed = false;
            return;
        }

        // Movement input
        MoveInput = 0f;

        if (keyboard.aKey.isPressed)
            MoveInput = -1f;

        if (keyboard.dKey.isPressed)
            MoveInput = 1f;

        // Button inputs
        JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
        ParryPressed = keyboard.leftShiftKey.wasPressedThisFrame;
    }
}
