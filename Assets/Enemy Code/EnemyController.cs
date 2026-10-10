using System;
using UnityEngine;

public enum EnemyState
{
    Patrol,   // walking back and forth around home, looking for the player
    Chase,    // player noticed — closing the distance
    Attack,   // in range — swinging (holds still until the swing finishes)
    Return,   // lost the player or strayed too far — walking back home
    Stunned,  // knocked back (e.g. by a parry) — no AI until it wears off
    Dead,     // health ran out — does nothing
    Jump,     // in the air on a jump (onto a platform, over a gap, down a ledge)
    Retreat,  // hopping back after a combo, then a short pause
    JumpAttack // winding up, leaping at the player, landing (EnemyJumpAttack runs it)
}

// Umbrella script for the enemy: runs the state machine and tells the other
// enemy scripts what to do. It decides; they act — movement, sensing, patrol
// and attacking all live in their own scripts.
public class EnemyController : MonoBehaviour
{
    [Header("Attack")]
    [Tooltip("How far inside the slash the player's body must be before the enemy swings. " +
             "Higher = the enemy steps in closer, so a player who steps back a little still gets hit.")]
    public float reachMargin = 0.25f;

    [Tooltip("Only used if the player has no collider: horizontal distance at which the enemy swings.")]
    public float fallbackAttackRange = 1.1f;

    [Header("Chase")]
    [Tooltip("How far from home the enemy will chase before giving up and returning.")]
    public float leashDistance = 10f;

    [Header("Platforms")]
    [Tooltip("The player counts as 'above' or 'below' the enemy when the ground under them " +
             "is at least this much higher or lower than the enemy's feet.")]
    public float levelDifference = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private EnemyMovement movement;
    private EnemyDetection detection;
    private EnemyTargeting targeting;
    private EnemyAttack attack;
    private EnemyKnockback knockback;
    private EnemyPatrol patrol;
    private EnemyHealth health;
    private EnemyJump jump;
    private EnemyRetreat retreat;
    private EnemyJumpAttack jumpAttack;
    private EnemyAttackToken token;
    private EnemyStatusEffects status;

    private EnemyState state = EnemyState.Patrol;

    // Which state to go back to when a jump lands (Chase or Return).
    private EnemyState stateAfterJump = EnemyState.Chase;

    // Platform following: where to stand to make a jump that can't be made
    // from here (e.g. onto a platform that's overhead), and where it lands.
    private bool hasApproachPoint;
    private float approachX;
    private Vector2 approachLanding;
    private float nextJumpSearchTime;

    // True when the last search found no jump and no spot to jump from.
    private bool noWayFound;

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
        jump = GetComponent<EnemyJump>();
        retreat = GetComponent<EnemyRetreat>();
        jumpAttack = GetComponent<EnemyJumpAttack>();
        token = GetComponent<EnemyAttackToken>();
        status = GetComponent<EnemyStatusEffects>();
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
        else if (status != null && status.IsStunned)
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
            case EnemyState.Jump:    UpdateJump();    break;
            case EnemyState.Retreat: UpdateRetreat(); break;
            case EnemyState.JumpAttack: UpdateJumpAttack(); break;
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
            // Too many others already attacking — wait for a turn.
            if (!TryTakeAttackTurn())
            {
                HoldBack();
                return;
            }

            ChangeState(EnemyState.Attack);
            return;
        }

        if (TryStartJumpAttack())
            return;

        if (IsWaitingForTurn())
        {
            HoldBack();
            return;
        }

        float direction = targeting.GetHorizontalDirectionToTarget();

        if (Navigate(direction, GetTargetGround(), true))
            return;

        Walk(direction);
    }

    // Gets the enemy to a goal on another level, or past a gap/wall:
    // jumps up, drops down, jumps across, or walks to a spot a jump can be
    // made from. `goalLevelKnown` is false when only the goal's x matters
    // (walking home). Returns true if it handled movement this frame.
    bool Navigate(float direction, Vector2 goalGround, bool goalLevelKnown)
    {
        noWayFound = false;

        if (jump == null || movement.IsLaunched || !movement.IsGrounded())
            return false;

        Vector2 feet = movement.GetFeetPosition();
        float rise = goalLevelKnown ? goalGround.y - feet.y : 0f;

        bool above = rise > levelDifference;
        bool below = rise < -levelDifference;
        bool blocked = direction != 0f && !movement.CanMove(direction);

        // Same level and a clear path — plain walking works. (Unless the
        // enemy is backing up from a wall for a run-up: stepping back clears
        // the wall, but the plan still stands until the next search.)
        if (!above && !blocked && !hasApproachPoint)
            return false;

        if (Time.time >= nextJumpSearchTime && jump.CanJump)
        {
            nextJumpSearchTime = Time.time + jump.searchInterval;

            bool dropAllowed = below || !goalLevelKnown;

            if (TryJumpToward(direction, goalGround, above, dropAllowed, blocked))
                return true;

            hasApproachPoint = FindApproachPoint(direction, goalGround, above, below, blocked);
            noWayFound = !hasApproachPoint;
        }

        // Walk to the spot the jump can be made from, then make it.
        if (hasApproachPoint)
        {
            float toApproach = approachX - feet.x;

            if (Mathf.Abs(toApproach) > EnemyJump.ApproachTolerance)
            {
                Walk(Mathf.Sign(toApproach));
                return true;
            }

            if (jump.CanJump && jump.TryPlanJumpTo(approachLanding, out Vector2 velocity))
            {
                StartJump(velocity);
                return true;
            }

            movement.Stop();
            movement.Face(direction);
            return true;
        }

        // No way up from here — wait below the player, facing them, instead
        // of pacing back and forth underneath.
        if (above && Mathf.Abs(goalGround.x - feet.x) < 1f)
        {
            movement.Stop();
            movement.Face(direction);
            return true;
        }

        return false;
    }

    // Tries the jump that fits: up to the goal's level, down a ledge, or
    // across a gap/wall. Starts it and returns true if one was found.
    bool TryJumpToward(float direction, Vector2 goalGround, bool above, bool dropAllowed, bool blocked)
    {
        Vector2 velocity;

        if (above && jump.TryFindJumpUp(goalGround, out velocity))
        {
            StartJump(velocity);
            return true;
        }

        if (blocked && dropAllowed && movement.IsLedgeAhead(direction) && jump.TryFindDropDown(direction, out velocity))
        {
            StartJump(velocity);
            return true;
        }

        if (blocked && jump.TryFindJumpAcross(direction, out velocity))
        {
            StartJump(velocity);
            return true;
        }

        return false;
    }

    // No jump from where the enemy stands: find a spot to make one from —
    // out past the edge of the goal's platform, or back from a wall/overhang.
    bool FindApproachPoint(float direction, Vector2 goalGround, bool above, bool below, bool blocked)
    {
        if (above && jump.TryFindApproachPoint(goalGround, out approachX, out approachLanding))
            return true;

        if (blocked && !below && movement.IsWallAhead(direction) &&
            jump.TryGetObstacleTop(direction, out Vector2 top) &&
            jump.TryFindApproachPoint(top, out approachX, out approachLanding))
            return true;

        return false;
    }

    // The top of whatever the player is standing on (or will land on, if
    // they're mid-jump) — so jumping on the spot doesn't look like the
    // player moved up a level.
    Vector2 GetTargetGround()
    {
        Vector2 targetFeet = targeting.GetTargetFeetPosition();

        if (jump != null && jump.TryGetSurface(targetFeet + new Vector2(0f, 0.1f), 20f, out float groundY))
            return new Vector2(targetFeet.x, groundY);

        return targetFeet;
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

        // Combo over: back off, or (sometimes) stand ground and go again
        // once the cooldown is up.
        if (attack.ComboFinished)
        {
            if (retreat != null && retreat.TryStartRetreat(movement.GetFacingDirection()))
            {
                ChangeState(EnemyState.Retreat);
                return;
            }

            attack.BeginCombo();
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
    // JUMP ATTACK
    // =========================

    // From a distance, sometimes leap at the player (EnemyJumpAttack decides
    // how often and whether the leap can be made).
    bool TryStartJumpAttack()
    {
        if (jumpAttack == null || jump == null || movement.IsLaunched || !movement.IsGrounded())
            return false;

        if (!jump.JumpAllowed)
            return false;

        Vector2 targetGround = GetTargetGround();

        // Don't leap past the leash.
        if (patrol != null && Mathf.Abs(targetGround.x - patrol.HomeX) > leashDistance)
            return false;

        if (token != null && !token.CanAcquire)
            return false;

        if (!jumpAttack.TryChooseLeap(targetGround))
            return false;

        TryTakeAttackTurn();
        ChangeState(EnemyState.JumpAttack);
        jumpAttack.Begin();
        return true;
    }

    // EnemyJumpAttack runs the wind-up, leap and landing; wait for it.
    void UpdateJumpAttack()
    {
        if (jumpAttack != null && jumpAttack.IsBusy)
            return;

        ChangeState(RefreshTarget() ? EnemyState.Chase : EnemyState.Return);
    }

    // =========================
    // ATTACK TURNS
    // =========================

    // True if this enemy may attack now (it has, or just took, a token — or
    // doesn't use tokens).
    bool TryTakeAttackTurn()
    {
        return token == null || token.TryAcquire();
    }

    // No free token and already close enough: hang back instead of closing in.
    bool IsWaitingForTurn()
    {
        if (token == null || token.CanAcquire)
            return false;

        float distance = Mathf.Abs(targeting.GetTargetFeetPosition().x - movement.GetFeetPosition().x);
        return distance <= token.waitDistance;
    }

    void HoldBack()
    {
        movement.Stop();
        movement.Face(targeting.GetHorizontalDirectionToTarget());
    }

    // =========================
    // RETREAT
    // =========================

    // Hopping back, then pausing (EnemyRetreat runs both). Comes straight
    // back in afterwards.
    void UpdateRetreat()
    {
        if (!movement.IsLaunched)
            movement.Stop();

        if (retreat != null && retreat.IsRetreating)
            return;

        ChangeState(RefreshTarget() ? EnemyState.Chase : EnemyState.Return);
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

        // Hop down off the platform, or over the gap/wall, on the way home.
        Vector2 home = new Vector2(patrol.HomeX, movement.GetFeetPosition().y);

        if (Navigate(direction, home, false))
            return;

        if (!movement.CanMove(direction))
        {
            // Can't get home (knocked across a gap, etc.) — patrol here
            // instead. Without EnemyJump that's decided straight away;
            // with it, only once a search has come up empty.
            if (jump == null || noWayFound)
            {
                patrol.MakeCurrentPositionHome();
                ChangeState(EnemyState.Patrol);
                return;
            }

            movement.Stop();
            return;
        }

        Walk(direction);
    }

    // =========================
    // JUMP
    // =========================

    void StartJump(Vector2 velocity)
    {
        stateAfterJump = state == EnemyState.Return ? EnemyState.Return : EnemyState.Chase;
        hasApproachPoint = false;

        jump.Jump(velocity);
        ChangeState(EnemyState.Jump);
    }

    // Committed to the jump — no steering in the air. Picks up where it left
    // off once it lands.
    void UpdateJump()
    {
        if (movement.IsLaunched)
            return;

        if (stateAfterJump == EnemyState.Chase && !RefreshTarget())
        {
            ChangeState(EnemyState.Return);
            return;
        }

        ChangeState(stateAfterJump);
    }

    // =========================
    // STUNNED
    // =========================

    void UpdateStunned()
    {
        movement.Stop();

        if (knockback != null && knockback.IsKnockedBack)
            return;

        if (status != null && status.IsStunned)
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

    // Asks the attack whether its slash would actually land, facing the
    // target. Measuring from the hitbox (rather than a distance between body
    // centres) keeps both sides identical — the body colliders are offset
    // from the pivots and don't flip with the sprite.
    bool IsTargetInAttackRange()
    {
        if (!targeting.HasTarget())
            return false;

        float direction = targeting.GetHorizontalDirectionToTarget();

        if (direction == 0f)
            direction = movement.GetFacingDirection();

        if (targeting.TryGetTargetBounds(out Bounds targetBounds))
        {
            // No swinging through a floor or wall — e.g. at a player standing
            // on a platform right overhead.
            if (IsSolidBetween(targetBounds.center))
                return false;

            return attack.IsInReach(targetBounds, direction, reachMargin);
        }

        Vector2 delta = targeting.GetTargetFeetPosition() - movement.GetFeetPosition();
        return Mathf.Abs(delta.x) <= fallbackAttackRange && Mathf.Abs(delta.y) <= 1f;
    }

    bool IsSolidBetween(Vector2 point)
    {
        Collider2D body = movement.GetBody();
        Vector2 from = body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;

        return Physics2D.Linecast(from, point, movement.groundLayer);
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

    static bool IsAlertState(EnemyState s)
    {
        return s == EnemyState.Chase || s == EnemyState.Attack || s == EnemyState.Jump ||
               s == EnemyState.Retreat || s == EnemyState.JumpAttack;
    }

    void ChangeState(EnemyState next)
    {
        if (next == state)
            return;

        EnemyState previous = state;
        state = next;

        // A jump-from spot belongs to whatever the enemy was doing before.
        hasApproachPoint = false;

        // Every time the enemy closes in to attack, it throws a fresh combo.
        if (next == EnemyState.Attack)
            attack.BeginCombo();

        if (previous == EnemyState.Retreat && retreat != null)
            retreat.Cancel();

        if (previous == EnemyState.JumpAttack && jumpAttack != null)
            jumpAttack.Cancel();

        // Done attacking (retreating, chasing again, stunned, dead...) —
        // hand the turn to another enemy.
        if (token != null && next != EnemyState.Attack && next != EnemyState.JumpAttack)
            token.Release();

        // While after the player, keep track of them outside the sight cone.
        if (detection != null)
            detection.SetAlerted(IsAlertState(next));

        if (logStateChanges)
            Debug.Log(name + ": " + previous + " -> " + next, this);

        OnStateChanged?.Invoke(previous, next);
    }
}
