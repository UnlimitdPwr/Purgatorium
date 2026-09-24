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
        if (!canDamage)
            return;

        // Only the player is a valid target for this attack.
        if (!other.CompareTag("Player"))
            return;

        // =========================
        // PARRY CHECK (before any damage logic)
        // =========================

        ParryScript parry = other.GetComponentInParent<ParryScript>();

        if (parry != null)
        {
            EnemyKnockback knockback = GetComponentInParent<EnemyKnockback>();
            GameObject attacker = knockback != null ? knockback.gameObject : gameObject;

            if (parry.TryParryAttack(transform.position, attacker))
            {
                Debug.Log("Attack PARRIED by " + other.name);

                // Consume this swing so it can't hit again.
                canDamage = false;

                if (knockback != null)
                {
                    knockback.ApplyKnockback(parry.transform.position);
                }

                EnemyHealth health = GetComponentInParent<EnemyHealth>();

                if (health != null)
                {
                    health.ApplyParry();
                }

                return;
            }
        }

        // =========================
        // DAMAGE
        // =========================

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        Debug.Log("PLAYER HIT! Applying " + damage + " damage.");

        playerHealth.TakeDamage(damage);

        // One damage application per attack.
        canDamage = false;
    }
}
