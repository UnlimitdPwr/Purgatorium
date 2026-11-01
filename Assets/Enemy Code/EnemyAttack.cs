using System.Collections.Generic;
using UnityEngine;

// The enemy's close-range attacks: combos of swings, each one an
// EnemyAttackData asset (animation, hitbox timing, telegraph, damage, whether
// it can be parried/blocked). Each swing picks one of `attacks` at random,
// weighted by the assets' weights.
//
// With `attacks` left empty the enemy uses its old built-in slash: the
// Attack1 animation, with the clip's own animation events switching the
// hitbox on and off. Enemies that haven't been given attack assets yet
// keep playing exactly as before.
public class EnemyAttack : MonoBehaviour
{
    [Tooltip("The enemy's base damage. Each attack multiplies it by its own damageMultiplier.")]
    public int damage = 10;

    [Tooltip("How strongly the sword applies the enemy's own statuses (EnemyStatusOnHit). " +
             "1 = as set there, 0 = none.")]
    [Min(0f)] public float statusMultiplier = 1f;

    [Tooltip("Extra status buildup only the sword adds, on top of the enemy's own. Usually empty.")]
    public StatusInfliction[] statusOnHit = new StatusInfliction[0];

    [Tooltip("Pause after a combo ends before the enemy can start another.")]
    public float attackCooldown = 1f;

    [Header("Attacks")]
    [Tooltip("The moves this enemy picks from, one per swing. Empty = the old built-in Attack1 slash.")]
    public EnemyAttackData[] attacks = new EnemyAttackData[0];

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

    // True until the first swing of the current combo has been thrown.
    private bool firstSwingOfCombo;

    // Built-in slash only: how long one swing lasts — read from the Attack1
    // clip so it stays in sync if the animation changes.
    private float swingDuration;
    private float swingTimer;

    // The attack asset in progress (null when idle or using the built-in
    // slash), and where it is: seconds since it started, when the wind-up
    // flash starts, when the swing itself starts (after any hesitation and
    // wind-up), and how far through it is.
    private EnemyAttackData current;
    private float elapsed;
    private float windUpStartsAt;
    private float swingStartsAt;
    private bool windUpStarted;
    private bool swingStarted;
    private bool hitboxOpened;
    private bool hitboxClosed;

    private const string AttackClipName = "Attack1";
    private const float FallbackSwingDuration = 0.6f;

    [Tooltip("The sword swing's hitbox. Found among the children automatically if left " +
             "empty (skipping the leap attack's JumpAttackHitbox).")]
    [SerializeField] private EnemyHitbox hitbox;

    private EnemyAnimation anim;
    private EnemyMovement movement;
    private EnemyStatusEffects status;

    void Awake()
    {
        anim = GetComponent<EnemyAnimation>();
        movement = GetComponent<EnemyMovement>();
        status = GetComponent<EnemyStatusEffects>();

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

        if (current != null)
        {
            elapsed += Time.deltaTime;
            UpdateCurrentAttack();
        }
    }

    // True from the moment an attack starts (including any wind-up) until
    // its animation has finished. EnemyController holds the enemy in place
    // while this is true.
    public bool IsAttacking => current != null || swingTimer > 0f;

    public bool CanAttack => attackTimer <= 0f;

    // Base damage after status conditions (weaker attacks). Used by body
    // contact; each attack asset multiplies it again.
    public int CurrentDamage =>
        status != null ? Mathf.RoundToInt(damage * status.AttackDamageMultiplier) : damage;

    // True once every swing of the current combo has been thrown.
    public bool ComboFinished => swingsLeft <= 0;

    bool UsesAttackAssets => attacks != null && attacks.Length > 0;

    // Rolls how many swings the next combo has (minComboSwings–maxComboSwings).
    public void BeginCombo()
    {
        int min = Mathf.Max(1, minComboSwings);
        int max = Mathf.Max(min, maxComboSwings);

        swingsLeft = Random.Range(min, max + 1);
        firstSwingOfCombo = true;
    }

    // Starts the next swing of the combo if the timing allows it. Returns
    // true if it did. Starts a new combo if the last one is finished.
    public bool Attack()
    {
        if (attackTimer > 0f)
            return false;

        if (swingsLeft <= 0)
            BeginCombo();

        if (UsesAttackAssets)
            return StartAttack(PickAttack());

        Debug.Log("Enemy attacks for " + damage + " damage!");

        anim.PlayAttack();

        swingsLeft--;
        firstSwingOfCombo = false;
        swingTimer = swingDuration;

        // Short gap to the next swing of the combo; the full cooldown after
        // the last one. Both count from the end of this swing.
        attackTimer = swingDuration + PauseAfterSwing();

        return true;
    }

    // Stops the attack in progress — the enemy was parried, knocked back or
    // killed mid-swing — and ends the combo. Only attack assets can be
    // stopped; the built-in slash plays out as it always has.
    public void Cancel()
    {
        swingsLeft = 0;

        if (current == null)
            return;

        if (hitboxOpened && !hitboxClosed)
            CloseHitbox();

        if (anim != null)
            anim.StopWindUpFlash();

        current = null;
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
    // ATTACK ASSETS
    // =========================

    // Weighted random pick. With only one attack no dice are rolled, so an
    // enemy with a single attack uses the random sequence it always has.
    EnemyAttackData PickAttack()
    {
        if (attacks.Length == 1)
            return attacks[0];

        float total = 0f;

        foreach (EnemyAttackData candidate in attacks)
        {
            if (candidate != null)
                total += candidate.weight;
        }

        float roll = Random.Range(0f, total);

        foreach (EnemyAttackData candidate in attacks)
        {
            if (candidate == null || candidate.weight <= 0f)
                continue;

            roll -= candidate.weight;

            if (roll <= 0f)
                return candidate;
        }

        // Every weight was 0 — take the first one that's set.
        foreach (EnemyAttackData candidate in attacks)
        {
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    bool StartAttack(EnemyAttackData data)
    {
        if (data == null)
            return false;

        // A delayed swing to catch an early parry — never on a combo's opener.
        float hesitation = firstSwingOfCombo || data.comboHesitation.y <= 0f
            ? 0f
            : Random.Range(Mathf.Max(0f, data.comboHesitation.x), data.comboHesitation.y);

        current = data;
        elapsed = 0f;
        windUpStartsAt = hesitation;
        swingStartsAt = hesitation + data.windUp;
        windUpStarted = false;
        swingStarted = false;
        hitboxOpened = false;
        hitboxClosed = false;

        swingsLeft--;
        firstSwingOfCombo = false;

        attackTimer = swingStartsAt + data.SwingDuration + PauseAfterSwing();

        Debug.Log("Enemy attacks (" + data.name + ") for " + AttackDamage(data) + " damage!");

        // Nothing to wait for — swing this frame, as the built-in slash does.
        UpdateCurrentAttack();
        return true;
    }

    // Steps the attack in progress through hesitation → wind-up → swing,
    // switching the hitbox on and off at the asset's times.
    void UpdateCurrentAttack()
    {
        EnemyAttackData data = current;

        if (!windUpStarted && elapsed >= windUpStartsAt)
        {
            windUpStarted = true;

            if (data.windUp > 0f && anim != null)
                anim.PlayAttackWindUp(data.windUp, data.windUpTint);
        }

        if (!swingStarted && elapsed >= swingStartsAt)
        {
            swingStarted = true;

            if (anim != null)
            {
                anim.StopWindUpFlash();
                anim.PlayAttack(data.clip);
            }
        }

        if (!swingStarted)
            return;

        float intoSwing = elapsed - swingStartsAt;

        if (!hitboxOpened && intoSwing >= data.hitboxStart)
        {
            hitboxOpened = true;
            OpenHitbox(data);
        }

        if (hitboxOpened && !hitboxClosed && intoSwing >= data.hitboxEnd)
        {
            hitboxClosed = true;
            CloseHitbox();
        }

        if (intoSwing >= data.SwingDuration)
        {
            if (hitboxOpened && !hitboxClosed)
                CloseHitbox();

            current = null;
        }
    }

    void OpenHitbox(EnemyAttackData data)
    {
        if (hitbox == null)
            return;

        if (movement != null)
            hitbox.SetFacing(movement.GetFacingDirection());

        hitbox.EnableHitbox(AttackDamage(data), CombineStatuses(statusOnHit, data.extraStatusOnHit),
            statusMultiplier * data.statusMultiplier, data.canBeParried, data.canBeBlocked);
    }

    void CloseHitbox()
    {
        if (hitbox != null)
            hitbox.DisableHitbox();
    }

    int AttackDamage(EnemyAttackData data)
    {
        return Mathf.RoundToInt(CurrentDamage * data.damageMultiplier);
    }

    static StatusInfliction[] CombineStatuses(StatusInfliction[] a, StatusInfliction[] b)
    {
        if (b == null || b.Length == 0)
            return a;

        if (a == null || a.Length == 0)
            return b;

        var combined = new List<StatusInfliction>(a);
        combined.AddRange(b);
        return combined.ToArray();
    }

    // Short gap to the next swing of the combo; the full cooldown after the
    // last one. Status conditions can lengthen both (slower attacks).
    float PauseAfterSwing()
    {
        float pauseMultiplier = status != null ? status.AttackCooldownMultiplier : 1f;
        return (swingsLeft > 0 ? comboSwingDelay : attackCooldown) * pauseMultiplier;
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
    // Called by the events in the Attack1 clip. Only the built-in slash uses
    // them — an attack asset switches the hitbox on its own timing, so the
    // events are ignored while one is in use.

    public void EnableHitbox()
    {
        if (UsesAttackAssets || hitbox == null)
            return;

        // Put the hitbox on the side the enemy is facing before it goes live.
        if (movement != null)
            hitbox.SetFacing(movement.GetFacingDirection());

        hitbox.EnableHitbox(CurrentDamage, statusOnHit, statusMultiplier);
    }

    public void DisableHitbox()
    {
        if (!UsesAttackAssets)
            CloseHitbox();
    }
}
