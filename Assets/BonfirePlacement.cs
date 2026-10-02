using UnityEngine;

public class BonfirePlacement : MonoBehaviour
{
    [Header("Bonfire")]
    [SerializeField] private GameObject bonfireSeedPrefab;

    [Header("Placement")]
    [SerializeField] private Transform throwPoint;

    [Header("Throw")]
    [SerializeField] private float throwForce = 3f;

    [Header("Inventory")]
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private ItemData bonfireSeedItem;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {

        // throw direction matches the direction the player is facing.
        spriteRenderer = GetComponent<SpriteRenderer>();

        // This function automatically finds the InventorySystem
        // on the same player if it was not assigned manually.
        if (inventory == null)
        {
            inventory = GetComponent<InventorySystem>();
        }
    }




    private void Update()
    {
        // This function keeps the throw point on the side
        // the player is currently facing.
        UpdateThrowPointDirection();
    }

    // This function keeps the throw point on the side of the player
    // that the player is facing.
    private void UpdateThrowPointDirection()
    {
        if (throwPoint == null || spriteRenderer == null)
            return;

        if (spriteRenderer.flipX)
        {
            // Face left.
            // Rotate the throw point 180 degrees around the Y axis.
            throwPoint.localRotation =
                Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            // Face right.
            // Keep the throw point in its normal orientation.
            throwPoint.localRotation =
                Quaternion.Euler(0f, 0f, 0f);
        }
    }

    // This function checks the inventory and throws one Bonfire Seed
    // only when the player actually owns one.
    public void TryPlaceBonfire()
    {
        // Make sure an InventorySystem exists.
        if (inventory == null)
        {
            Debug.LogWarning(
                "BonfirePlacement does not have an InventorySystem."
            );

            return;
        }

        // Make sure the correct Bonfire Seed ItemData has been assigned.
        if (bonfireSeedItem == null)
        {
            Debug.LogWarning(
                "Bonfire Seed ItemData is not assigned to BonfirePlacement."
            );

            return;
        }

        // Check the inventory BEFORE creating the physical seed.
        if (!inventory.HasItem(bonfireSeedItem))
        {
            Debug.Log(
                "Cannot throw Bonfire Seed. Player has 0."
            );

            return;
        }

        // Make sure the physical Bonfire Seed prefab exists.
        if (bonfireSeedPrefab == null)
        {
            Debug.LogWarning(
                "Bonfire seed prefab is not assigned."
            );

            return;
        }

        // Make sure the throw point exists.
        if (throwPoint == null)
        {
            Debug.LogWarning(
                "Bonfire throw point is not assigned."
            );

            return;
        }

        float direction = GetFacingDirection();

        // Create the physical Bonfire Seed in the world.
        GameObject seed = Instantiate(
            bonfireSeedPrefab,
            throwPoint.position,
            Quaternion.identity
        );

        // Get the BonfireSeed component from the spawned object.
        BonfireSeed bonfireSeed =
            seed.GetComponent<BonfireSeed>();

        // Make sure the spawned object is actually a valid Bonfire Seed.
        if (bonfireSeed == null)
        {
            Debug.LogWarning(
                "Bonfire seed prefab does not contain BonfireSeed."
            );

            // Destroy the invalid spawned object.
            Destroy(seed);

            // Do not remove anything from the inventory.
            return;
        }

        // Prevent the thrown seed from physically colliding
        // with and pushing the player.
        IgnorePlayerCollision(seed);

        // Remove exactly one Bonfire Seed from the inventory.
        bool removed =
            inventory.RemoveItem(bonfireSeedItem, 1);

        // Safety check in case the item was removed or changed
        // between the inventory check and this point.
        if (!removed)
        {
            Debug.LogWarning(
                "Could not remove Bonfire Seed from inventory."
            );

            // Destroy the physical seed because the inventory
            // was not successfully consumed.
            Destroy(seed);

            return;
        }

        // Launch the physical Bonfire Seed.
        bonfireSeed.Launch(direction, throwForce);

        Debug.Log(
            "Bonfire Seed thrown. Remaining: " +
            inventory.GetItemAmount(bonfireSeedItem)
        );
    }

    // This function determines the player's horizontal facing direction.
    private float GetFacingDirection()
    {
        if (spriteRenderer != null && spriteRenderer.flipX)
            return -1f;

        return 1f;
    }

    // This function prevents the newly thrown seed from physically
    // pushing or colliding with the player.
    private void IgnorePlayerCollision(GameObject seed)
    {
        Collider2D[] playerColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] seedColliders =
            seed.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D seedCollider in seedColliders)
            {
                Physics2D.IgnoreCollision(
                    playerCollider,
                    seedCollider
                );
            }
        }
    }
}
