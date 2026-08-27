using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController1 : MonoBehaviour
{
    private MovementScript movement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movement = GetComponent<MovementScript>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = 0f;

        if (Keyboard.current.aKey.isPressed)
            x = -1f;

        if (Keyboard.current.dKey.isPressed)
            x = 1f;

        movement.SetMoveInput(x);
    }
}
