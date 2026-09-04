using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{

    public float MoveInput { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool ParryPressed { get; private set; }

    void Update()
    {
        // Movement input
        MoveInput = 0f;

        if (Keyboard.current.aKey.isPressed)
            MoveInput = -1f;

        if (Keyboard.current.dKey.isPressed)
            MoveInput = 1f;

        // Button inputs
        JumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        ParryPressed = Keyboard.current.leftShiftKey.wasPressedThisFrame;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    
}
