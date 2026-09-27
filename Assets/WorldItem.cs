using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Pickup Prompt")]
    [SerializeField] private GameObject pickupPrompt;

    private InventorySystem playerInventory;
    private PlayerInput playerInput;

    private bool playerInRange;

    public ItemData ItemData => itemData;
    public int Amount => amount;

    private void Awake()
    {
        // This function sets up the item's world appearance when it is created.
        SetupWorldVisual();

        // This function hides the pickup prompt until the player enters the area.
        SetPickupPrompt(false);
    }

    private void Update()
    {
        // This checks whether the nearby player has pressed the pickup button.
        if (playerInRange &&
            playerInput != null &&
            playerInput.PickupPressed)
        {
            TryPickup();
        }
    }

    // This function gives the world pickup its world-specific sprite.
    private void SetupWorldVisual()
    {
        if (itemData == null)
        {
            Debug.LogWarning(
                "WorldItem does not have an ItemData assigned.",
                this
            );

            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning(
                "WorldItem does not have a SpriteRenderer assigned.",
                this
            );

            return;
        }

        spriteRenderer.sprite = itemData.worldSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // This detects when the player enters the pickup range.
        InventorySystem inventory =
            other.GetComponent<InventorySystem>();

        if (inventory == null)
            return;

        PlayerInput input =
            other.GetComponent<PlayerInput>();

        if (input == null)
            return;

        playerInventory = inventory;
        playerInput = input;

        playerInRange = true;

        SetPickupPrompt(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // This detects when the player leaves the pickup range.
        InventorySystem inventory =
            other.GetComponent<InventorySystem>();

        if (inventory == null)
            return;

        if (inventory == playerInventory)
        {
            playerInventory = null;
            playerInput = null;

            playerInRange = false;

            SetPickupPrompt(false);
        }
    }

    // This function attempts to move the item from the world into the player's inventory.
    private void TryPickup()
    {
        if (playerInventory == null)
            return;

        bool added =
            playerInventory.AddItem(
                itemData,
                amount
            );

        // The world item is removed only if the inventory successfully accepted it.
        if (added)
        {
            Destroy(gameObject);
        }
    }

    // This function controls whether the pickup prompt is visible.
    private void SetPickupPrompt(bool visible)
    {
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(visible);
        }
    }
}
