using UnityEngine;

// Patrol and return-home logic. Remembers where the enemy spawned ("home"),
// walks back and forth around it, and tells EnemyController which way to walk.
// EnemyController owns the decision of *when* to patrol; this only answers
// *where*.
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [Tooltip("How far the enemy walks to each side of its home point.")]
    [SerializeField] private float patrolDistance = 3f;

    [Tooltip("Seconds the enemy waits at each end of its patrol before turning.")]
    [SerializeField] private float waitTime = 1f;

    [Tooltip("Patrol walking speed as a fraction of EnemyMovement.moveSpeed.")]
    [SerializeField, Range(0.1f, 1f)] private float patrolSpeedMultiplier = 0.5f;

    [Header("Home")]
    [Tooltip("How close to home counts as 'back home'.")]
    [SerializeField] private float arriveThreshold = 0.15f;

    private EnemyMovement movement;

    private float homeX;
    private float patrolDirection = 1f;
    private float waitTimer;

    public float HomeX => homeX;
    public float SpeedMultiplier => patrolSpeedMultiplier;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        homeX = transform.position.x;
    }

    // =========================
    // PATROL
    // =========================

    // Direction to walk this frame (-1, 0 or 1). 0 while waiting at an end.
    public float GetPatrolDirection()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return 0f;
        }

        float offset = transform.position.x - homeX;

        bool pastEnd = offset * patrolDirection >= patrolDistance;
        bool blocked = movement != null && !movement.CanMove(patrolDirection);

        // Reached the end of the patrol, or a ledge/wall — wait, then turn.
        if (pastEnd || blocked)
        {
            patrolDirection = -patrolDirection;
            waitTimer = waitTime;
            return 0f;
        }

        return patrolDirection;
    }

    // =========================
    // RETURN HOME
    // =========================

    public bool IsHome()
    {
        return Mathf.Abs(transform.position.x - homeX) <= arriveThreshold;
    }

    public float GetDirectionHome()
    {
        if (IsHome())
            return 0f;

        return Mathf.Sign(homeX - transform.position.x);
    }

    // Used when the way home is blocked (e.g. knocked across a gap): patrol
    // around wherever the enemy is now instead.
    public void MakeCurrentPositionHome()
    {
        homeX = transform.position.x;
    }

    // =========================
    // GIZMOS
    // =========================

    private void OnDrawGizmosSelected()
    {
        float centerX = Application.isPlaying ? homeX : transform.position.x;
        Vector3 left = new Vector3(centerX - patrolDistance, transform.position.y, 0f);
        Vector3 right = new Vector3(centerX + patrolDistance, transform.position.y, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.15f);
        Gizmos.DrawWireSphere(right, 0.15f);
    }
}
