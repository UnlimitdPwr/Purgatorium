using UnityEngine;

public class BonfireSeed : MonoBehaviour
{
    [Header("Bonfire")]
    [SerializeField] private GameObject bonfirePrefab;

    private Rigidbody2D rb;

    private bool hasLanded;

    // Keeps track of bonfires created during the current game session.
    private static int bonfireCount = 0;

    // This function resets session-based bonfire numbering whenever 
    // Unity starts a new game session.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] 
    private static void ResetBonfireCount() 
    { 
        bonfireCount = 0; 
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(float direction, float throwForce)
    {
        Vector2 velocity = new Vector2(
            direction * throwForce,
            throwForce
        );

        rb.linearVelocity = velocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Prevent the seed from spawning multiple bonfires.
        if (hasLanded)
            return;

        // Only objects tagged Ground can be used for bonfire placement.
        if (!collision.gameObject.CompareTag("Ground"))
            return;

        hasLanded = true;

        SpawnBonfire(collision);
    }

    // This function creates the bonfire at the point where 
    // the seed collided with the ground.
    private void SpawnBonfire(Collision2D collision)
    {
        if (bonfirePrefab == null)
        {
            Debug.LogWarning(
                "Bonfire prefab is not assigned to BonfireSeed."
            );

            Destroy(gameObject);
            return;
        }

        // Get the exact point where the seed touched the ground.
        ContactPoint2D contact = collision.GetContact(0);

        Vector3 spawnPosition = contact.point;

        // Create the bonfire at the landing position.
        GameObject bonfire = Instantiate(
            bonfirePrefab,
            spawnPosition,
            Quaternion.identity
        );

        // Increase the number of bonfires created during this game session.
        bonfireCount++;

        // Give the spawned bonfire a unique name.
        bonfire.name = "Bonfire_" + bonfireCount.ToString("00");

        // Remove the spent bonfire seed from the world.
        Destroy(gameObject);
    }
}
