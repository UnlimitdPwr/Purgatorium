using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public int damage = 10;
    public float attackCooldown = 1f;

    private float attackTimer;

    private EnemyAnimation anim;
    private EnemyMovement movement;
    private EnemyHitbox hitbox;

    void Awake()
    {
        anim = GetComponent<EnemyAnimation>();
        movement = GetComponent<EnemyMovement>();
        hitbox = GetComponentInChildren<EnemyHitbox>();

        if (hitbox == null)
            Debug.LogError("EnemyAttack: no EnemyHitbox found in children.", this);
    }

    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    public void Attack()
    {
        if (attackTimer > 0f)
            return;

        Debug.Log("Enemy attacks for " + damage + " damage!");

        anim.PlayAttack();

        attackTimer = attackCooldown;
    }

    // =========================
    // ANIMATION EVENT HOOKS
    // =========================

    public void EnableHitbox()
    {
        if (hitbox == null)
            return;

        // Put the hitbox on the side the enemy is facing before it goes live.
        if (movement != null)
            hitbox.SetFacing(movement.GetFacingDirection());

        hitbox.EnableHitbox(damage);
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.DisableHitbox();
    }
}
