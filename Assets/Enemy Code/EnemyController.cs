using System;
using UnityEngine;

public enum EnemyState
{
    Patrol,   // walking back and forth around home, looking for the player
    Chase,    // player noticed — closing the distance
    Attack,   // in range — swinging (holds still until the swing finishes)
    Return,   // lost the player or strayed too far — walking back home
    Stunned,  // knocked back (e.g. by a parry) — no AI until it wears off
    Dead      // health ran out — does nothing
}

// Umbrella script for the enemy: runs the state machine and tells the other
// enemy scripts what to do. It decides; they act — movement, sensing, patrol
// and attacking all live in their own scripts.
public class EnemyController : MonoBehaviour
{
    [Header("Attack")]
    [Tooltip("Horizontal distance to the player at which the enemy stops and swings.")]
    public float attackRange = 1.2f;

    [Tooltip("How far above/below the enemy the player can be and still be attacked.")]
    public float attackHeightTolerance = 1f;

    [Header("Chase")]
    [Tooltip("How far from home the enemy will chase before giving up and returning.")]
    public float leashDistance = 10f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private EnemyMovement movement;
    private EnemyDetection detection;
    private EnemyTargeting targeting;
    private EnemyAttack attack;
    private EnemyKnockback knockback;
    private EnemyPatrol patrol;
    private EnemyHealth health;

    private EnemyState state = EnemyState.Patrol;

    public EnemyState State => state;

    // (previous, next) — hook for animation/audio/debug UI.
    public event Action<EnemyState, EnemyState> OnStateChanged;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        detection = GetComponent<EnemyDetection>();
        targeting = GetComponent<EnemyTargeting>();
        attack = GetComponent<EnemyAttack>();
        knockback = GetComponent<EnemyKnockback>();
        patrol = GetComponent<EnemyPatrol>();
        health = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        // =========================
        // OVERRIDES (any state)
        // =========================

        if (health != null && health.IsDead)
        {
            ChangeState(EnemyState.Dead);
        }
        else if (knockback != null && knockback.IsKnockedBack)
        {
            ChangeState(EnemyState.Stunned);
        }

        // =========================
        // STATE MACHINE
        // =========================

        switch (state)
        {
            case EnemyState.Patrol:  UpdatePatrol();  break;
            case EnemyState.Chase:   UpdateChase();   break;
            case EnemyState.Attack:  UpdateAttack();  break;
            case EnemyState.Return:  UpdateReturn();  break;
            case EnemyState.Stunned: UpdateStunned(); break;
            case EnemyState.Dead:    movement.Stop(); break;
        }
    }

    // =========================
    // PATROL
    // =========================

    void UpdatePatrol()
    {
        if (TryNoticePlayer())
            return;

        // No EnemyPatrol on this enemy — guard the spot instead.
        if (patrol == null)
        {
            movement.Stop();
            return;
        }

        float direction = patrol.GetPatrolDirection();
        Walk(direction * patrol.SpeedMultiplier);
    }

    // =========================
    // CHASE
    // =========================

    void UpdateChase()
    {
        if (!RefreshTarget())
        {
            ChangeState(EnemyState.Return);
            return;
        }

        if (IsTooFarFromHome())
        {
            ChangeState(EnemyState.Return);
            return;
        }

        if (IsTargetInAttackRange())
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        Walk(targeting.GetHorizontalDirectionToTarget());
    }

    // =========================
    // ATTACK
    // =========================

    void UpdateAttack()
    {
        movement.Stop();

        // Committed to the swing — let it finish before deciding anything.
        if (attack.IsAttacking)
            return;

        if (!RefreshTarget())
        {
            ChangeState(EnemyState.Return);
            return;
        }

        if (!IsTargetInAttackRange())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Turn toward the target first — standing still never updates facing,
        // so without this the enemy could swing the wrong way.
        movement.Face(targeting.GetHorizontalDirectionToTarget());

        attack.Attack();
    }

    // =========================
    // RETURN
    // =========================

    void UpdateReturn()
    {
        if (TryNoticePlayer())
            return;

        if (patrol == null || patrol.IsHome())
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        float direction = patrol.GetDirectionHome();

        // Can't get home (knocked across a gap, etc.) — patrol here instead.
        if (!movement.CanMove(direction))
        {
            patrol.MakeCurrentPositionHome();
            ChangeState(EnemyState.Patrol);
            return;
        }

        Walk(direction);
    }

    // =========================
    // STUNNED
    // =========================

    void UpdateStunned()
    {
        movement.Stop();

        if (knockback != null && knockback.IsKnockedBack)
            return;

        // Stun over — go straight back after the player if it's still around.
        ChangeState(RefreshTarget() ? EnemyState.Chase : EnemyState.Return);
    }

    // =========================
    // HELPERS
    // =========================

    // Starts a chase if the player can be sensed and is within the leash.
    bool TryNoticePlayer()
    {
        if (!detection.CanSensePlayer())
            return false;

        Transform player = detection.GetDetectedPlayer();

        // Don't re-aggro on a player standing beyond the leash — that would
        // flip between Chase and Return at the leash boundary.
        if (patrol != null && Mathf.Abs(player.position.x - patrol.HomeX) > leashDistance)
            return false;

        targeting.SetTarget(player);
        ChangeState(EnemyState.Chase);
        return true;
    }

    // Updates the target from detection. False when the player is lost.
    bool RefreshTarget()
    {
        Transform player = detection.GetDetectedPlayer();

        if (player == null)
        {
            targeting.ClearTarget();
            return false;
        }

        targeting.SetTarget(player);
        return true;
    }

    bool IsTargetInAttackRange()
    {
        if (!targeting.HasTarget())
            return false;

        Vector2 delta = targeting.GetTargetFeetPosition() - movement.GetFeetPosition();

        return Mathf.Abs(delta.x) <= attackRange
            && Mathf.Abs(delta.y) <= attackHeightTolerance;
    }

    bool IsTooFarFromHome()
    {
        return patrol != null && Mathf.Abs(transform.position.x - patrol.HomeX) > leashDistance;
    }

    // Moves in a direction unless that would walk off a ledge or into a wall,
    // in which case the enemy turns to face that way and waits.
    void Walk(float direction)
    {
        if (direction == 0f)
        {
            movement.Stop();
            return;
        }

        if (!movement.CanMove(direction))
        {
            movement.Stop();
            movement.Face(direction);
            return;
        }

        movement.Move(direction);
    }

    void ChangeState(EnemyState next)
    {
        if (next == state)
            return;

        EnemyState previous = state;
        state = next;

        if (logStateChanges)
            Debug.Log(name + ": " + previous + " -> " + next, this);

        OnStateChanged?.Invoke(previous, next);
    }
}
