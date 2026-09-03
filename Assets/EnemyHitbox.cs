using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    private Collider2D hitbox;

    private int damage;
    private bool canDamage;

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();

        if (hitbox == null)
        {
            Debug.LogError("EnemyHitbox has no Collider2D!");
            return;
        }

        hitbox.enabled = false;
    }

    public void EnableHitbox(int damageAmount)
    {
        damage = damageAmount;
        canDamage = true;

        hitbox.enabled = true;

        Debug.Log("Enemy hitbox ON - Damage: " + damage);
    }

    public void DisableHitbox()
    {
        canDamage = false;
        hitbox.enabled = false;

        Debug.Log("Enemy hitbox OFF");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Enemy hitbox detected: " + other.name);

        if (!canDamage)
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.Log("No PlayerHealth found on " + other.name);
            return;
        }

        Debug.Log("PLAYER HIT! Applying " + damage + " damage.");

        playerHealth.TakeDamage(damage);

        // One damage application per attack.
        canDamage = false;
    }
}
