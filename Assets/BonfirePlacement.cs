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

    [Header("Equipment")]
    [SerializeField] private EquipmentManager equipmentManager;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // This function gets the player's sprite renderer and finds the inventory if needed.
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (inventory == null)
            inventory = GetComponent<InventorySystem>();
    }

    private void Update()
    {
        // This function keeps the throw point aligned with the direction the player faces.
        UpdateThrowPointDirection();
    }

    private void UpdateThrowPointDirection()
    {
        // This function rotates the throw point to face left or right with the player.
        if (throwPoint == null || spriteRenderer == null)
            return;

        throwPoint.localRotation = spriteRenderer.flipX
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;
    }

    public void TryPlaceBonfire()
    {
        // This function throws one Bonfire Seed only if it is equipped and available in inventory.

        if (inventory == null)
        {
            Debug.LogWarning("BonfirePlacement: InventorySystem is not assigned.");
            return;
        }

        if (equipmentManager == null)
        {
            Debug.LogWarning("BonfirePlacement: EquipmentManager is not assigned.");
            return;
        }

        if (bonfireSeedItem == null)
        {
            Debug.LogWarning("BonfirePlacement: Bonfire Seed ItemData is not assigned.");
            return;
        }

        // Require the Bonfire Seed to occupy a quick-item equipment slot.
        if (!IsBonfireSeedEquipped())
        {
            Debug.Log("Cannot throw Bonfire Seed: it is not equipped.");
            return;
        }

        // Require at least one seed to remain in the shared inventory.
        if (!inventory.HasItem(bonfireSeedItem))
        {
            Debug.Log("Cannot throw Bonfire Seed: none remain in inventory.");
            return;
        }

        if (bonfireSeedPrefab == null)
        {
            Debug.LogWarning("BonfirePlacement: Bonfire Seed prefab is not assigned.");
            return;
        }

        if (throwPoint == null)
        {
            Debug.LogWarning("BonfirePlacement: Throw point is not assigned.");
            return;
        }

        // Verify that the prefab contains the required projectile behaviour before consuming an item.
        BonfireSeed seedComponent = bonfireSeedPrefab.GetComponent<BonfireSeed>();

        if (seedComponent == null)
        {
            Debug.LogWarning("Bonfire Seed prefab does not contain a BonfireSeed component.");
            return;
        }

        float direction = GetFacingDirection();

        // Remove exactly one seed before creating the projectile.
        if (!inventory.RemoveItem(bonfireSeedItem, 1))
        {
            Debug.LogWarning("Could not remove Bonfire Seed from inventory.");
            return;
        }

        // Create the projectile at the throw point after successfully consuming one seed.
        GameObject seed = Instantiate(
            bonfireSeedPrefab,
            throwPoint.position,
            Quaternion.identity
        );

        // Prevent the projectile from colliding with or pushing the player.
        IgnorePlayerCollision(seed);

        // Launch the projectile in the direction the player is facing.
        BonfireSeed bonfireSeed = seed.GetComponent<BonfireSeed>();
        bonfireSeed.Launch(direction, throwForce);

        Debug.Log(
            "Bonfire Seed thrown. Remaining: " +
            inventory.GetItemAmount(bonfireSeedItem)
        );
    }

    private bool IsBonfireSeedEquipped()
    {
        // This function checks whether the Bonfire Seed is assigned to any quick-item slot.
        if (equipmentManager == null || bonfireSeedItem == null)
            return false;

        foreach (ItemData equippedItem in equipmentManager.QuickSlots)
        {
            if (equippedItem == bonfireSeedItem)
                return true;
        }

        return false;
    }

    private float GetFacingDirection()
    {
        // This function returns -1 for facing left and 1 for facing right.
        if (spriteRenderer != null && spriteRenderer.flipX)
            return -1f;

        return 1f;
    }

    private void IgnorePlayerCollision(GameObject seed)
    {
        // This function ignores collisions between all player and projectile colliders.
        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();
        Collider2D[] seedColliders = seed.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D seedCollider in seedColliders)
            {
                if (playerCollider != null && seedCollider != null)
                    Physics2D.IgnoreCollision(playerCollider, seedCollider);
            }
        }
    }
}