using UnityEngine;

// The leap attack: from a distance, the enemy winds up, leaps at the player,
// and slashes in an arc from over its head down to in front of it as it
// falls. Separate from EnemyAttack (the sword swing) so the two can be tuned
// independently.
//
// EnemyController decides when to use it (TryChooseLeap while chasing); this
// script runs the whole move: wind-up → leap → falling slash → landing
// recovery.
//
// The hitbox is a child object named "JumpAttackHitbox" with a trigger
// collider and an EnemyHitbox (so parry, damage and knockback work exactly
// like the sword swing). If the enemy doesn't have one, it's created at
// start from the Hitbox settings below. Until the animation exists, the
// hitbox is swept along the arc in code.
[RequireComponent(typeof(EnemyJump))]
public class EnemyJumpAttack : MonoBehaviour
{
    // EnemyAttack skips a hitbox with this name when looking for its own.
    public const string HitboxName = "JumpAttackHitbox";

    [Header("Damage")]
    public int damage = 15;

    [Tooltip("How strongly the leap applies the enemy's own statuses (EnemyStatusOnHit). " +
             "1 = as set there, 0 = none, 1.5 = a heavier dose.")]
    [Min(0f)] public float statusMultiplier = 1f;

    [Tooltip("Extra status buildup only the leap adds, on top of the enemy's own. Usually empty.")]
    public StatusInfliction[] statusOnHit = new StatusInfliction[0];

    [Header("When To Use It")]
    [Tooltip("Closest the player can be for a leap — any closer and the enemy just swings.")]
    public float minRange = 2.5f;

    [Tooltip("Furthest the player can be for a leap. Also limited to what the enemy can see " +
             "(EnemyDetection.detectionRange, sight cone and line of sight).")]
    public float maxRange = 5f;

    [Tooltip("Chance (0–1) the enemy leaps each time it considers it.")]
    [Range(0f, 1f)] public float useChance = 0.35f;

    [Tooltip("Seconds between considering a leap while the player is in range.")]
    public float decisionInterval = 0.5f;

    [Tooltip("Seconds after a leap before another can be chosen.")]
    public float cooldown = 4f;

    [Tooltip("Only leaps at a player standing roughly on the enemy's level (within this height).")]
    public float maxHeightDifference = 1f;

    [Header("Leap")]
    [Tooltip("How high the leap goes above the take-off, in world units.")]
    public float leapHeight = 1.7f;

    [Tooltip("Fastest the enemy can travel sideways in the leap.")]
    public float maxLeapSpeed = 9f;

    [Tooltip("Lands this far short of the player so the falling slash comes down on them.")]
    public float landShortOfTarget = 0.6f;

    [Header("Wind-up")]
    [Tooltip("Seconds the enemy telegraphs (crouches/flashes) before leaping.")]
    public float windUpDuration = 0.4f;

    [Header("Landing")]
    [Tooltip("Seconds the enemy stays put after landing — the player's window to punish.")]
    public float landingRecovery = 0.35f;

    [Header("Hitbox Arc")]
    [Tooltip("Size of the created hitbox in world units (ignored if the enemy already has a JumpAttackHitbox).")]
    public Vector2 hitboxSize = new Vector2(0.7f, 0.7f);

    [Tooltip("Distance from the middle of the body to the hitbox along the arc.")]
    public float arcRadius = 0.9f;

    [Tooltip("Where the slash starts, in degrees: 90 = straight overhead, 0 = straight ahead.")]
    public float arcStartAngle = 100f;

    [Tooltip("Where the slash ends, in degrees (negative = below straight ahead).")]
    public float arcEndAngle = -20f;

    // =========================
    // PRIVATE VARIABLES
    // =========================

    private enum Phase { Idle, WindUp, Airborne, Recovery }

    private EnemyJump jump;
    private EnemyMovement movement;
    private EnemyDetection detection;
    private EnemyTargeting targeting;
    private EnemyAnimation anim;
    private EnemyHitbox hitbox;
    private EnemyStatusEffects status;

    private Phase phase = Phase.Idle;
    private float phaseTimer;
    private float cooldownTimer;
    private float nextDecisionTime;

    private Vector2 plannedVelocity;
    private float landingY;
    private float apexY;
    private bool passedApex;

    void Awake()
    {
        jump = GetComponent<EnemyJump>();
        movement = GetComponent<EnemyMovement>();
        detection = GetComponent<EnemyDetection>();
        targeting = GetComponent<EnemyTargeting>();
        anim = GetComponent<EnemyAnimation>();
        status = GetComponent<EnemyStatusEffects>();

        hitbox = FindOrCreateHitbox();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        switch (phase)
        {
            case Phase.WindUp:   UpdateWindUp();   break;
            case Phase.Airborne: UpdateAirborne(); break;
            case Phase.Recovery: UpdateRecovery(); break;
        }
    }

    // =========================
    // STATE
    // =========================

    // True from the wind-up until the landing recovery is over.
    public bool IsBusy => phase != Phase.Idle;

    // =========================
    // CHOOSING A LEAP
    // =========================

    // Called while chasing. Every decisionInterval, if the player is in
    // plain sight, on about the same level and between minRange and
    // maxRange, rolls useChance — and on a hit, checks the leap can actually
    // be made (EnemyJump). True means "start a leap now" (then call Begin).
    public bool TryChooseLeap(Vector2 targetGround)
    {
        if (phase != Phase.Idle || cooldownTimer > 0f || Time.time < nextDecisionTime)
            return false;

        if (!InLeapRange(targetGround))
            return false;

        nextDecisionTime = Time.time + decisionInterval;

        if (Random.value >= useChance)
            return false;

        return jump.CanJump && TryPlanLeap(targetGround, out plannedVelocity);
    }

    // Starts the wind-up for the leap TryChooseLeap just picked.
    public void Begin()
    {
        phase = Phase.WindUp;
        phaseTimer = windUpDuration;

        movement.Stop();
        FaceTarget();

        if (anim != null)
            anim.PlayJumpAttackWindUp(windUpDuration);
    }

    // Abandons the move (stunned, killed, etc.).
    public void Cancel()
    {
        if (phase == Phase.Idle)
            return;

        phase = Phase.Idle;
        cooldownTimer = cooldown;

        if (hitbox != null)
            hitbox.DisableHitbox();

        if (anim != null)
            anim.StopWindUpFlash();
    }

    // =========================
    // PHASES
    // =========================

    void UpdateWindUp()
    {
        movement.Stop();
        FaceTarget();

        phaseTimer -= Time.deltaTime;

        if (phaseTimer > 0f)
            return;

        // Aim at where the player is now; if that leap can't be made (they
        // stepped behind a wall, off a ledge...), use the one planned at the
        // start of the wind-up.
        if (TryGetTargetGround(out Vector2 targetGround) && TryPlanLeap(targetGround, out Vector2 velocity))
            plannedVelocity = velocity;

        movement.Face(plannedVelocity.x);
        jump.Hop(plannedVelocity);

        if (anim != null)
            anim.PlayJumpAttack();

        phase = Phase.Airborne;
        passedApex = false;
        apexY = movement.GetFeetPosition().y;
    }

    void UpdateAirborne()
    {
        Vector2 feet = movement.GetFeetPosition();

        if (!passedApex)
        {
            apexY = Mathf.Max(apexY, feet.y);

            // Top of the leap: the slash starts overhead.
            if (movement.GetVerticalVelocity() <= 0f)
            {
                passedApex = true;
                // Status conditions can weaken the hit (weaker attacks).
                int leapDamage = status != null
                    ? Mathf.RoundToInt(damage * status.AttackDamageMultiplier)
                    : damage;

                hitbox.EnableHitbox(leapDamage, statusOnHit, statusMultiplier);
            }
        }

        if (passedApex)
        {
            float fall = Mathf.Max(0.01f, apexY - landingY);
            PlaceHitbox(Mathf.Clamp01((apexY - feet.y) / fall));
        }

        if (movement.IsLaunched)
            return;

        // Landed.
        hitbox.DisableHitbox();

        phase = Phase.Recovery;
        phaseTimer = landingRecovery;
    }

    void UpdateRecovery()
    {
        movement.Stop();

        phaseTimer -= Time.deltaTime;

        if (phaseTimer > 0f)
            return;

        phase = Phase.Idle;
        cooldownTimer = cooldown;
    }

    // =========================
    // INTERNAL
    // =========================

    bool InLeapRange(Vector2 targetGround)
    {
        if (detection == null || !detection.CanSeePlayerNow())
            return false;

        Vector2 feet = movement.GetFeetPosition();
        float distance = Mathf.Abs(targetGround.x - feet.x);
        float range = Mathf.Min(maxRange, detection.detectionRange);

        return distance >= minRange && distance <= range &&
               Mathf.Abs(targetGround.y - feet.y) <= maxHeightDifference;
    }

    // A leap that lands landShortOfTarget in front of the player (or right
    // at them, if there's no floor in front).
    bool TryPlanLeap(Vector2 targetGround, out Vector2 velocity)
    {
        Vector2 feet = movement.GetFeetPosition();
        float toward = Mathf.Sign(targetGround.x - feet.x);

        float shortX = targetGround.x - toward * landShortOfTarget;

        foreach (float x in new[] { shortX, targetGround.x })
        {
            if (!jump.TryGetSurface(new Vector2(x, targetGround.y + 0.5f), 1f, out float y))
                continue;

            if (jump.TryPlanLeap(new Vector2(x, y), leapHeight, maxLeapSpeed, out velocity))
            {
                landingY = y;
                return true;
            }
        }

        velocity = Vector2.zero;
        return false;
    }

    bool TryGetTargetGround(out Vector2 ground)
    {
        ground = Vector2.zero;

        if (targeting == null || !targeting.HasTarget())
            return false;

        Vector2 targetFeet = targeting.GetTargetFeetPosition();

        if (!jump.TryGetSurface(targetFeet + new Vector2(0f, 0.1f), 20f, out float y))
            return false;

        ground = new Vector2(targetFeet.x, y);
        return true;
    }

    void FaceTarget()
    {
        if (targeting != null)
            movement.Face(targeting.GetHorizontalDirectionToTarget());
    }

    // progress 0 = start of the slash (overhead), 1 = end (in front).
    void PlaceHitbox(float progress)
    {
        hitbox.transform.position = GetArcPoint(progress, movement.GetFacingDirection());
    }

    Vector3 GetArcPoint(float progress, float facing)
    {
        float angle = Mathf.Lerp(arcStartAngle, arcEndAngle, progress) * Mathf.Deg2Rad;

        Collider2D body = movement != null ? movement.GetBody() : GetComponent<Collider2D>();
        Vector2 centre = body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;

        Vector2 offset = new Vector2(Mathf.Cos(angle) * facing, Mathf.Sin(angle)) * arcRadius;
        return centre + offset;
    }

    EnemyHitbox FindOrCreateHitbox()
    {
        Transform existing = transform.Find(HitboxName);

        if (existing != null && existing.TryGetComponent(out EnemyHitbox found))
            return found;

        GameObject child = new GameObject(HitboxName);
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);

        // Collider first: EnemyHitbox looks for it (and switches it off) in Awake.
        BoxCollider2D box = child.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        Vector3 scale = transform.lossyScale;
        box.size = new Vector2(
            hitboxSize.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            hitboxSize.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));

        return child.AddComponent<EnemyHitbox>();
    }

    // =========================
    // GIZMOS
    // =========================

    void OnDrawGizmosSelected()
    {
        // The slash's path, start (overhead) to end (in front).
        float facing = Application.isPlaying && movement != null ? movement.GetFacingDirection() : 1f;

        Gizmos.color = Color.red;
        Vector3 previous = GetArcPoint(0f, facing);

        for (int i = 1; i <= 12; i++)
        {
            Vector3 point = GetArcPoint(i / 12f, facing);
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.DrawWireCube(GetArcPoint(1f, facing), hitboxSize);
    }
}
