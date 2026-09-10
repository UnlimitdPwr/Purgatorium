using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float attackRange = 1.2f;

    private EnemyMovement movement;
    private EnemyDetection detection;
    private EnemyTargeting targeting;
    private EnemyAttack attack;
    private EnemyKnockback knockback;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        detection = GetComponent<EnemyDetection>();
        targeting = GetComponent<EnemyTargeting>();
        attack = GetComponent<EnemyAttack>();
        knockback = GetComponent<EnemyKnockback>();
    }

    void Update()
    {
        // =========================
        // KNOCKBACK LOCKOUT
        // =========================

        // While stunned, don't chase or attack — let the impulse play out.
        if (knockback != null && knockback.IsKnockedBack)
        {
            movement.Stop();
            return;
        }

        // =========================
        // DETECTION
        // =========================

        Transform detectedPlayer = detection.GetDetectedPlayer();

        if (detectedPlayer != null)
        {
            targeting.SetTarget(detectedPlayer);
        }
        else
        {
            targeting.ClearTarget();
        }

        // =========================
        // NO TARGET
        // =========================

        if (!targeting.HasTarget())
        {
            movement.Stop();
            return;
        }

        // =========================
        // TARGET DISTANCE
        // =========================

        float distanceToTarget = targeting.GetDistanceToTarget();

        // =========================
        // ATTACK
        // =========================

        if (distanceToTarget <= attackRange)
        {
            AttackTarget();
        }
        else
        {
            ChaseTarget();
        }
    }

    // =========================
    // CHASE
    // =========================

    void ChaseTarget()
    {
        float direction = targeting.GetHorizontalDirectionToTarget();

        if (direction == 0f)
        {
            movement.Stop();
            return;
        }

        movement.Move(direction);
    }

    // =========================
    // ATTACK
    // =========================

    void AttackTarget()
    {
        movement.Stop();

        attack.Attack();
    }
}