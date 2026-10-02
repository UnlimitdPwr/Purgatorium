using UnityEngine;

// How the enemy notices the player:
// - SIGHT: in front of the enemy, within detectionRange, inside the sight cone,
//   and not hidden behind ground/walls.
// - HEARING: anywhere within hearingRange, even behind the enemy, so the
//   player can't stand right behind it forever.
// Once noticed, the enemy remembers the player for memoryDuration seconds
// after losing them, so ducking out of view for a moment doesn't reset it.
public class EnemyDetection : MonoBehaviour
{
    [Header("Sight")]
    [Tooltip("How far the enemy can see.")]
    public float detectionRange = 5f;

    [Tooltip("Half-angle of the sight cone in degrees, measured from straight ahead.")]
    [Range(0f, 180f)] public float sightHalfAngle = 60f;

    [Tooltip("Where the eyes are, in the enemy's local space (scaled with it).")]
    public Vector2 eyeOffset = new Vector2(0f, 0.6f);

    [Tooltip("Layers that block sight. Defaults to the Ground layer when left empty.")]
    public LayerMask obstacleMask;

    [Header("Hearing")]
    [Tooltip("The enemy notices the player within this range in any direction.")]
    public float hearingRange = 1.5f;

    [Header("Memory")]
    [Tooltip("Seconds the enemy keeps tracking the player after losing them.")]
    public float memoryDuration = 2f;

    private Transform player;
    private Collider2D playerCollider;
    private Collider2D body;
    private EnemyMovement movement;

    private float lastSensedTime = float.NegativeInfinity;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        body = GetComponent<Collider2D>();

        if (obstacleMask == 0)
            obstacleMask = LayerMask.GetMask("Ground");

        AcquirePlayer();
    }

    // =========================
    // DETECTION
    // =========================

    // True when the enemy can see or hear the player right now.
    public bool CanSensePlayer()
    {
        // The player reference can be lost if the player is (re)spawned after
        // this component woke up — try to re-acquire it before giving up.
        if (player == null)
            AcquirePlayer();

        if (player == null)
            return false;

        bool sensed = CanHearPlayer() || CanSeePlayer();

        if (sensed)
            lastSensedTime = Time.time;

        return sensed;
    }

    // Kept for existing callers: true when sensed now.
    public bool CanDetectPlayer()
    {
        return CanSensePlayer();
    }

    // The player while sensed now or still remembered; otherwise null.
    public Transform GetDetectedPlayer()
    {
        if (CanSensePlayer())
            return player;

        if (player != null && Time.time - lastSensedTime <= memoryDuration)
            return player;

        return null;
    }

    // =========================
    // SENSES
    // =========================

    private bool CanHearPlayer()
    {
        // Body centre to body centre — the player's and enemy's transform
        // pivots sit at different heights.
        Vector2 center = body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;
        return Vector2.Distance(center, GetPlayerAimPoint()) <= hearingRange;
    }

    private bool CanSeePlayer()
    {
        Vector2 eye = GetEyePosition();
        Vector2 target = GetPlayerAimPoint();
        Vector2 toPlayer = target - eye;
        float distance = toPlayer.magnitude;

        if (distance > detectionRange)
            return false;

        // Inside the cone in front of the enemy?
        float facing = movement != null ? movement.GetFacingDirection() : 1f;

        if (Vector2.Angle(new Vector2(facing, 0f), toPlayer) > sightHalfAngle)
            return false;

        // Nothing solid in the way?
        return !Physics2D.Raycast(eye, toPlayer / distance, distance, obstacleMask);
    }

    // =========================
    // INTERNAL
    // =========================

    private Vector2 GetEyePosition()
    {
        return transform.TransformPoint(eyeOffset);
    }

    private Vector2 GetPlayerAimPoint()
    {
        // Aim at the middle of the player's body rather than their feet.
        if (playerCollider != null && playerCollider.enabled)
            return playerCollider.bounds.center;

        return player.position;
    }

    private void AcquirePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerCollider = playerObject.GetComponent<Collider2D>();
    }

    // =========================
    // GIZMOS
    // =========================

    void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.TransformPoint(eyeOffset);
        float facing = 1f;

        if (Application.isPlaying && movement != null)
            facing = movement.GetFacingDirection();

        // Sight cone
        Gizmos.color = Color.yellow;
        Vector3 forward = new Vector3(facing, 0f, 0f) * detectionRange;
        Vector3 upper = Quaternion.Euler(0f, 0f, sightHalfAngle * facing) * forward;
        Vector3 lower = Quaternion.Euler(0f, 0f, -sightHalfAngle * facing) * forward;
        Gizmos.DrawLine(eye, eye + upper);
        Gizmos.DrawLine(eye, eye + lower);
        Gizmos.DrawLine(eye, eye + forward);

        // Hearing radius
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
