using UnityEngine;

public class BonfirePlacement : MonoBehaviour
{
    [Header("Bonfire")]
    [SerializeField] private GameObject bonfireSeedPrefab;

    [Header("Placement")]
    [SerializeField] private Transform throwPoint;

    [Header("Throw")]
    [SerializeField] private float throwForce = 3f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        UpdateThrowPointDirection();
    }

    // This function keeps the throw point on the side the player is facing.
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
            throwPoint.localRotation =
                Quaternion.Euler(0f, 0f, 0f);
        }
    }

    // This function creates and throws the bonfire seed.
    public void TryPlaceBonfire()
    {
        if (bonfireSeedPrefab == null)
        {
            Debug.LogWarning(
                "Bonfire seed prefab is not assigned."
            );

            return;
        }

        if (throwPoint == null)
        {
            Debug.LogWarning(
                "Bonfire throw point is not assigned."
            );

            return;
        }

        float direction = GetFacingDirection();

        GameObject seed = Instantiate(
            bonfireSeedPrefab,
            throwPoint.position,
            Quaternion.identity
        );

        BonfireSeed bonfireSeed =
            seed.GetComponent<BonfireSeed>();

        if (bonfireSeed == null)
        {
            Debug.LogWarning(
                "Bonfire seed prefab does not contain BonfireSeed."
            );

            Destroy(seed);
            return;
        }

        IgnorePlayerCollision(seed);

        bonfireSeed.Launch(direction, throwForce);

        Debug.Log(
            "Bonfire seed thrown. Direction: " + direction
        );
    }

    // This function determines the player's horizontal facing direction.
    private float GetFacingDirection()
    {
        if (spriteRenderer != null && spriteRenderer.flipX)
            return -1f;

        return 1f;
    }

    // This function prevents the newly thrown seed from physically pushing the player.
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
