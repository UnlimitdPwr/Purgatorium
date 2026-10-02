using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public float MoveInput { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool ParryPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool DashHeld { get; private set; }
    public bool BlockHeld { get; private set; }

    public bool PlaceBonfirePressed { get; private set; }

    public bool PickupPressed { get; private set; }
    public bool InventoryPressed { get; private set; }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            // This resets all input when no keyboard is available.
            MoveInput = 0f;
            JumpPressed = false;
            JumpReleased = false;
            ParryPressed = false;
            DashPressed = false;
            DashHeld = false;
            BlockHeld = false;
            PlaceBonfirePressed = false;
            PickupPressed = false;
            InventoryPressed = false;

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
        JumpReleased = keyboard.spaceKey.wasReleasedThisFrame;
        ParryPressed = keyboard.leftShiftKey.wasPressedThisFrame;
        DashPressed = keyboard.rightShiftKey.wasPressedThisFrame;
        DashHeld = keyboard.rightShiftKey.isPressed;
        BlockHeld = keyboard.leftCtrlKey.isPressed;

        // =========================
        // BONFIRE
        // =========================

        PlaceBonfirePressed =
            keyboard.qKey.wasPressedThisFrame;

        // =========================
        // ITEM PICKUP
        // =========================

        // This detects when the player presses E to pick up a nearby item.
        PickupPressed =
            keyboard.eKey.wasPressedThisFrame;

        // =========================
        // INVENTORY
        // =========================

        // This detects when the player presses I to open or close the inventory.
        InventoryPressed =
            keyboard.iKey.wasPressedThisFrame;
    }
}
