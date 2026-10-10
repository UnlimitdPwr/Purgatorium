using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public int damage = 10;

    [Tooltip("Status buildup each sword hit adds to the player. Leave empty for none.")]
    public StatusInfliction[] statusOnHit = new StatusInfliction[0];

    [Tooltip("Pause after a combo ends before the enemy can start another.")]
    public float attackCooldown = 1f;

    [Header("Combo")]
    [Tooltip("Fewest swings in one combo.")]
    [Min(1)] public int minComboSwings = 1;

    [Tooltip("Most swings in one combo. Each combo picks a number in between at random.")]
    [Min(1)] public int maxComboSwings = 3;

    [Tooltip("Pause between swings inside a combo.")]
    public float comboSwingDelay = 0.15f;

    private float attackTimer;

    // Swings left in the current combo. 0 = combo finished.
    private int swingsLeft;

    // How long one swing lasts — read from the Attack1 clip so it stays in
    // sync if the animation changes.
    private float swingDuration;
    private float swingTimer;

    private const string AttackClipName = "Attack1";
    private const float FallbackSwingDuration = 0.6f;

    [Tooltip("The sword swing's hitbox. Found among the children automatically if left " +
             "empty (skipping the leap attack's JumpAttackHitbox).")]
    [SerializeField] private EnemyHitbox hitbox;

    private EnemyAnimation anim;
    private EnemyMovement movement;

    void Awake()
    {
        anim = GetComponent<EnemyAnimation>();
        movement = GetComponent<EnemyMovement>();
        if (hitbox == null)
            hitbox = FindSwingHitbox();

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

    // True once every swing of the current combo has been thrown.
    public bool ComboFinished => swingsLeft <= 0;

    // Rolls how many swings the next combo has (minComboSwings–maxComboSwings).
    public void BeginCombo()
    {
        int min = Mathf.Max(1, minComboSwings);
        int max = Mathf.Max(min, maxComboSwings);

        swingsLeft = Random.Range(min, max + 1);
    }

    // Starts the next swing of the combo if the timing allows it. Returns
    // true if it did. Starts a new combo if the last one is finished.
    public bool Attack()
    {
        if (attackTimer > 0f)
            return false;

        if (swingsLeft <= 0)
            BeginCombo();

        Debug.Log("Enemy attacks for " + damage + " damage!");

        anim.PlayAttack();

        swingsLeft--;
        swingTimer = swingDuration;

        // Short gap to the next swing of the combo; the full cooldown after
        // the last one. Both count from the end of this swing.
        attackTimer = swingDuration + (swingsLeft > 0 ? comboSwingDelay : attackCooldown);

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

    EnemyHitbox FindSwingHitbox()
    {
        foreach (EnemyHitbox candidate in GetComponentsInChildren<EnemyHitbox>(true))
        {
            if (candidate.name != EnemyJumpAttack.HitboxName)
                return candidate;
        }

        return null;
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

        hitbox.EnableHitbox(damage, statusOnHit);
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.DisableHitbox();
    }
}
