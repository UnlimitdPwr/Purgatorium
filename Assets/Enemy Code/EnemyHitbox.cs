using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    private Collider2D hitbox;

    private int damage;
    private StatusInfliction[] statusOnHit;
    private float statusMultiplier = 1f;
    private bool canDamage;

    // The enemy's own statuses (its "element") — added to every hit this
    // hitbox lands, whichever attack switched it on.
    private EnemyStatusOnHit enemyStatus;

    // The hitbox is laid out in the scene for an enemy facing right (the way
    // the sprite art faces). These remember that layout so SetFacing() can
    // mirror it to the left.
    private float designLocalX;
    private float designOffsetX;

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        enemyStatus = GetComponentInParent<EnemyStatusOnHit>();

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

    // `statuses`: extra statuses only this attack adds. `multiplier` scales
    // the enemy's own statuses (EnemyStatusOnHit) for this attack.
    public void EnableHitbox(int damageAmount, StatusInfliction[] statuses = null, float multiplier = 1f)
    {
        damage = damageAmount;
        statusOnHit = statuses;
        statusMultiplier = multiplier;
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

    private static void ApplyStatuses(IStatusReceiver receiver, StatusInfliction[] inflictions, float multiplier, BlockScript block)
    {
        if (inflictions == null || multiplier <= 0f)
            return;

        foreach (StatusInfliction infliction in inflictions)
        {
            if (infliction == null)
                continue;

            float buildup = infliction.buildup * multiplier;

            if (block != null)
                buildup = block.GetBlockedStatusBuildup(buildup);

            if (buildup > 0f)
                receiver.AddBuildup(infliction.status, buildup);
        }
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

        // Checked before TakeDamage, which reduces the damage itself.
        BlockScript block = other.GetComponentInParent<BlockScript>();
        bool blocked = block != null && block.IsBlocking;

        playerHealth.TakeDamage(damage);

        // Status buildup rides on the hit: the enemy's own statuses (scaled
        // for this attack) plus any extras the attack adds. A parried hit
        // never gets here, so parrying stops all of it; blocking stops part.
        IStatusReceiver receiver = other.GetComponentInParent<IStatusReceiver>();

        if (receiver != null)
        {
            if (enemyStatus != null)
                ApplyStatuses(receiver, enemyStatus.inflictions, statusMultiplier, blocked ? block : null);

            ApplyStatuses(receiver, statusOnHit, 1f, blocked ? block : null);
        }

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
