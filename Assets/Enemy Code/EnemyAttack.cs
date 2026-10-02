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

    // True when a body with these bounds would be at least `margin` inside
    // the slash for an enemy facing `direction`. Used to decide when to swing,
    // so the swing always reaches whatever it was aimed at.
    public bool IsInReach(Bounds target, float direction, float margin)
    {
        if (hitbox == null)
            return false;

        Bounds reach = hitbox.GetReachBounds(direction);

        // How far the slash's leading edge reaches past the target's near side.
        float depth = direction >= 0f
            ? reach.max.x - target.min.x
            : target.max.x - reach.min.x;

        bool notBehind = direction >= 0f
            ? target.max.x > reach.min.x
            : target.min.x < reach.max.x;

        bool verticalOverlap = target.max.y > reach.min.y && target.min.y < reach.max.y;

        return depth >= margin && notBehind && verticalOverlap;
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
