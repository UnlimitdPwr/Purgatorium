using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public int damage = 10;
    public float attackCooldown = 1f;

    private float attackTimer;

    // How long one swing lasts — read from the Attack1 clip so it stays in
    // sync if the animation changes.
    private float swingDuration;
    private float swingTimer;

    private const string AttackClipName = "Attack1";
    private const float FallbackSwingDuration = 0.6f;

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

        swingDuration = anim != null ? anim.GetClipLength(AttackClipName) : 0f;

        if (swingDuration <= 0f)
            swingDuration = FallbackSwingDuration;
    }

    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if (swingTimer > 0f)
        {
            swingTimer -= Time.deltaTime;
        }
    }

    // True from the moment a swing starts until its animation has finished.
    // EnemyController holds the enemy in place while this is true.
    public bool IsAttacking => swingTimer > 0f;

    public bool CanAttack => attackTimer <= 0f;

    // Starts a swing if the cooldown allows it. Returns true if it did.
    public bool Attack()
    {
        if (attackTimer > 0f)
            return false;

        Debug.Log("Enemy attacks for " + damage + " damage!");

        anim.PlayAttack();

        attackTimer = attackCooldown;
        swingTimer = swingDuration;

        return true;
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
