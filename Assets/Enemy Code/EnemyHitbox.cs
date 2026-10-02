using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    private Collider2D hitbox;

    private int damage;
    private bool canDamage;

    // The hitbox is laid out in the scene for an enemy facing right (the way
    // the sprite art faces). These remember that layout so SetFacing() can
    // mirror it to the left.
    private float designLocalX;
    private float designOffsetX;

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();

        if (hitbox == null)
        {
            Debug.LogError("EnemyHitbox has no Collider2D!");
            return;
        }

        designLocalX = transform.localPosition.x;
        designOffsetX = hitbox.offset.x;

        hitbox.enabled = false;
    }

    // =========================
    // REACH
    // =========================

    // World-space box the hitbox covers when the enemy faces this way —
    // computed from the layout, so it works while the hitbox is switched off
    // (a disabled collider reports empty bounds).
    public Bounds GetReachBounds(float direction)
    {
        float sign = direction < 0f ? -1f : 1f;

        Vector3 localPosition = transform.localPosition;
        localPosition.x = designLocalX * sign;

        Vector3 pivot = transform.parent != null
            ? transform.parent.TransformPoint(localPosition)
            : localPosition;

        Vector3 scale = transform.lossyScale;
        Vector2 offset = new Vector2(designOffsetX * sign, hitbox != null ? hitbox.offset.y : 0f);

        Vector2 size = Vector2.zero;

        if (hitbox is CapsuleCollider2D capsule)
            size = capsule.size;
        else if (hitbox is BoxCollider2D box)
            size = box.size;

        Vector3 center = pivot + new Vector3(offset.x * scale.x, offset.y * scale.y, 0f);
        Vector3 worldSize = new Vector3(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y), 0.1f);

        return new Bounds(center, worldSize);
    }

    // =========================
    // FACING
    // =========================

    // direction: 1 = facing right, -1 = facing left.
    public void SetFacing(float direction)
    {
        if (hitbox == null)
            return;

        float sign = direction < 0f ? -1f : 1f;

        Vector3 localPosition = transform.localPosition;
        localPosition.x = designLocalX * sign;
        transform.localPosition = localPosition;

        Vector2 offset = hitbox.offset;
        offset.x = designOffsetX * sign;
        hitbox.offset = offset;
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

        // Push the player away from the enemy's body (not the hitbox, which
        // can sit past the player's center when they're overlapping).
        PlayerKnockback playerKnockback = other.GetComponentInParent<PlayerKnockback>();

        if (playerKnockback != null)
        {
            EnemyController enemy = GetComponentInParent<EnemyController>();
            Vector2 source = enemy != null ? enemy.transform.position : transform.position;

            playerKnockback.ApplyKnockback(source);
        }

        // One damage application per attack.
        canDamage = false;
    }
}
