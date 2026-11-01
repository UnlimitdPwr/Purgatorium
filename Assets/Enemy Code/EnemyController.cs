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
    JumpAttack, // winding up, leaping at the player, landing (EnemyJumpAttack runs it)
    Search,   // lost the player — going to where it was last seen, then looking around
    Noticed   // just spotted the player (or an ally raised the alarm) — a short pause before charging
}

// Umbrella script for the enemy: runs the state machine and tells the other
// enemy scripts what to do. It decides; they act — movement, sensing, patrol
// and attacking all live in their own scripts, and EnemyNavigator works out
// how to get wherever the controller sends the enemy.
//
// Each state has an Update method (what it does every frame) and, where it
// needs one, an entry in OnEnter / OnExit (what happens once on the way in
// or out). ChangeState is the only way to switch state.
//
// Movement, attack, targeting and the navigator are used without null checks,
// so Unity adds them along with this script. Every other enemy system is
// optional — leave one off an enemy (or a prefab variant) to switch that
// behaviour off.
[RequireComponent(typeof(EnemyMovement), typeof(EnemyAttack), typeof(EnemyTargeting))]
[RequireComponent(typeof(EnemyNavigator))]
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

    [Header("Noticing")]
    [Tooltip("Seconds the enemy stops and stares after first spotting the player, before it charges. " +
             "Skipped when it spots them again while already searching. 0 = charge straight away.")]
    public float noticePause = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private EnemyMovement movement;
    private EnemyNavigator navigator;
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
    private EnemySearch search;
    private EnemyGroupAlert groupAlert;

    private float noticeTimer;

    private EnemyState state = EnemyState.Patrol;

    // Which state to go back to when a jump lands (Chase, Return or Search).
    private EnemyState stateAfterJump = EnemyState.Chase;

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
        search = GetComponent<EnemySearch>();
        groupAlert = GetComponent<EnemyGroupAlert>();

        // RequireComponent only adds it when the controller itself is added;
        // enemies built before EnemyNavigator existed get one here.
        navigator = GetComponent<EnemyNavigator>();

        if (navigator == null)
            navigator = gameObject.AddComponent<EnemyNavigator>();
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
            case EnemyState.Search:  UpdateSearch();  break;
            case EnemyState.Noticed: UpdateNoticed(); break;
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
        navigator.Walk(direction * patrol.SpeedMultiplier);
    }

    // =========================
    // CHASE
    // =========================

    void UpdateChase()
    {
        if (!RefreshTarget())
        {
            LoseTarget();
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

        // A random leap never goes over an ally that's in the way — that's
        // HandleBlockingAlly's call (it waits for the ally to stop first).
        if (!navigator.IsAllyBetweenMeAndTarget() && TryStartJumpAttack())
            return;

        if (IsWaitingForTurn())
        {
            HoldBack();
            return;
        }

        float direction = targeting.GetHorizontalDirectionToTarget();

        if (Steer(navigator.HandleBlockingAlly(direction, true, LeapOverAlly)))
            return;

        if (Steer(navigator.Navigate(direction, navigator.GetTargetGround(), true)))
            return;

        navigator.Walk(direction);
    }

    // Acts on what the navigator did. True if it took care of this frame.
    bool Steer(NavStep step)
    {
        if (step == NavStep.Jumped)
            ChangeState(EnemyState.Jump);

        return step != NavStep.NotNeeded;
    }

    bool LeapOverAlly()
    {
        return TryStartJumpAttack(true);
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
            LoseTarget();
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
    // `skipChance`: leap whenever possible (another enemy is in the way).
    bool TryStartJumpAttack(bool skipChance = false)
    {
        if (jumpAttack == null || jump == null || movement.IsLaunched || !movement.IsGrounded())
            return false;

        if (!jump.JumpAllowed)
            return false;

        Vector2 targetGround = navigator.GetTargetGround();

        // Don't leap past the leash.
        if (patrol != null && Mathf.Abs(targetGround.x - patrol.HomeX) > leashDistance)
            return false;

        if (token != null && !token.CanAcquire)
            return false;

        if (!jumpAttack.TryChooseLeap(targetGround, skipChance))
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

        if (RefreshTarget())
            ChangeState(EnemyState.Chase);
        else
            LoseTarget();
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

        if (RefreshTarget())
            ChangeState(EnemyState.Chase);
        else
            LoseTarget();
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

        if (Steer(navigator.HandleBlockingAlly(direction, false, null)))
            return;

        // Hop down off the platform, or over the gap/wall, on the way home.
        Vector2 home = new Vector2(patrol.HomeX, movement.GetFeetPosition().y);

        if (Steer(navigator.Navigate(direction, home, false)))
            return;

        if (!movement.CanMove(direction))
        {
            // Can't get home (knocked across a gap, etc.) — patrol here
            // instead. Without EnemyJump that's decided straight away;
            // with it, only once a search has come up empty.
            if (!navigator.CanJumpAtAll || navigator.NoWayFound)
            {
                patrol.MakeCurrentPositionHome();
                ChangeState(EnemyState.Patrol);
                return;
            }

            movement.Stop();
            return;
        }

        navigator.Walk(direction);
    }

    // =========================
    // JUMP
    // =========================

    // Committed to the jump — no steering in the air. Picks up where it left
    // off once it lands.
    void UpdateJump()
    {
        if (movement.IsLaunched)
            return;

        if (stateAfterJump == EnemyState.Chase && !RefreshTarget())
        {
            LoseTarget();
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
        if (RefreshTarget())
            ChangeState(EnemyState.Chase);
        else
            LoseTarget();
    }

    // =========================
    // HELPERS
    // =========================

    // Starts a chase if the player can be sensed and is within the leash —
    // after the "noticed" pause, unless `pause` is false (already searching).
    bool TryNoticePlayer(bool pause = true)
    {
        if (!detection.CanSensePlayer())
            return false;

        Transform player = detection.GetDetectedPlayer();

        // Don't re-aggro on a player standing beyond the leash — that would
        // flip between Chase and Return at the leash boundary.
        if (patrol != null && Mathf.Abs(player.position.x - patrol.HomeX) > leashDistance)
            return false;

        targeting.SetTarget(player);
        RememberSighting();

        if (groupAlert != null)
            groupAlert.RaiseAlarm(player);

        if (pause && noticePause > 0f)
            StartNoticed(noticePause);
        else
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
        RememberSighting();
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

    // =========================
    // NOTICED
    // =========================

    void StartNoticed(float duration)
    {
        noticeTimer = duration;
        ChangeState(EnemyState.Noticed);
    }

    // Stops and faces the player for a moment, then charges.
    void UpdateNoticed()
    {
        movement.Stop();

        if (!targeting.HasTarget() || detection.IsPlayerDead())
        {
            ChangeState(EnemyState.Return);
            return;
        }

        movement.Face(targeting.GetHorizontalDirectionToTarget());

        noticeTimer -= Time.deltaTime;

        if (noticeTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    // An ally spotted the player (EnemyGroupAlert). Joins in after its own
    // noticed pause plus `delay` — unless it's already busy fighting.
    public void HearAlarm(Transform player, float delay)
    {
        if (state != EnemyState.Patrol && state != EnemyState.Return && state != EnemyState.Search)
            return;

        if (player == null || detection == null || detection.IsPlayerDead())
            return;

        if (patrol != null && Mathf.Abs(player.position.x - patrol.HomeX) > leashDistance)
            return;

        detection.Notify();
        targeting.SetTarget(player);

        if (search != null)
            search.RememberSighting(navigator.GetTargetGround());

        StartNoticed(noticePause + delay);
    }

    // =========================
    // SEARCH
    // =========================

    // Lost the player: go and look where they were last seen if possible,
    // otherwise head home. A dead player is never searched for.
    void LoseTarget()
    {
        bool canSearch = search != null && search.HasSighting && !detection.IsPlayerDead() &&
                         (patrol == null || Mathf.Abs(search.LastSeen.x - patrol.HomeX) <= leashDistance);

        if (!canSearch)
        {
            ChangeState(EnemyState.Return);
            return;
        }

        // Started here rather than in OnEnter: landing from a jump made
        // mid-search goes back to Search and must carry on, not start over.
        search.Begin(movement.GetFacingDirection());
        ChangeState(EnemyState.Search);
    }

    void RememberSighting()
    {
        if (search != null && detection.CanSensePlayer())
            search.RememberSighting(navigator.GetTargetGround());
    }

    // Walk to the last-seen spot (jumping if needed), then look left and
    // right until the search runs out. Spotting the player again means an
    // immediate chase — no noticed pause, it's already on edge.
    void UpdateSearch()
    {
        if (TryNoticePlayer(false))
            return;

        if (search == null || IsTooFarFromHome())
        {
            ChangeState(EnemyState.Return);
            return;
        }

        if (search.IsTravelling)
        {
            Vector2 goal = search.LastSeen;
            Vector2 feet = movement.GetFeetPosition();
            float toGoal = goal.x - feet.x;
            bool there = Mathf.Abs(toGoal) <= search.arriveDistance &&
                         Mathf.Abs(goal.y - feet.y) <= navigator.levelDifference;

            if (there || search.TravelTimedOut())
            {
                search.MarkArrived();
            }
            else
            {
                float direction = Mathf.Abs(toGoal) > 0.05f ? Mathf.Sign(toGoal) : movement.GetFacingDirection();

                if (Steer(navigator.HandleBlockingAlly(direction, false, null)))
                    return;

                if (Steer(navigator.Navigate(direction, goal, true)))
                    return;

                if (!movement.CanMove(direction))
                {
                    // Blocked with no way round: search from here.
                    if (!navigator.CanJumpAtAll || navigator.NoWayFound)
                        search.MarkArrived();
                    else
                        movement.Stop();

                    return;
                }

                navigator.Walk(direction * search.searchSpeedMultiplier);
                return;
            }
        }

        movement.Stop();

        if (search.LookAround(out float facing))
            movement.Face(facing);
        else
            ChangeState(EnemyState.Return);
    }

    // =========================
    // STATE CHANGES
    // =========================

    static bool IsAlertState(EnemyState s)
    {
        return s == EnemyState.Chase || s == EnemyState.Attack || s == EnemyState.Jump ||
               s == EnemyState.Retreat || s == EnemyState.JumpAttack || s == EnemyState.Noticed;
    }

    static bool IsAttackingState(EnemyState s)
    {
        return s == EnemyState.Attack || s == EnemyState.JumpAttack;
    }

    void ChangeState(EnemyState next)
    {
        if (next == state)
            return;

        EnemyState previous = state;

        OnExit(previous, next);
        state = next;
        OnEnter(next, previous);

        if (logStateChanges)
            Debug.Log(name + ": " + previous + " -> " + next, this);

        OnStateChanged?.Invoke(previous, next);
    }

    // Runs once when leaving `exiting` for `next`.
    void OnExit(EnemyState exiting, EnemyState next)
    {
        // A run-up spot or ally to jump over belongs to what the enemy was doing.
        navigator.ResetPlan();

        switch (exiting)
        {
            case EnemyState.Retreat:
                if (retreat != null)
                    retreat.Cancel();
                break;

            case EnemyState.JumpAttack:
                if (jumpAttack != null)
                    jumpAttack.Cancel();
                break;
        }

        // Done attacking (retreating, chasing again, stunned, dead...) —
        // hand the turn to another enemy.
        if (token != null && !IsAttackingState(next))
            token.Release();
    }

    // Runs once when entering `entering` from `previous`.
    void OnEnter(EnemyState entering, EnemyState previous)
    {
        switch (entering)
        {
            // Every time the enemy closes in to attack, it throws a fresh combo.
            case EnemyState.Attack:
                attack.BeginCombo();
                break;

            // Land back in whatever it was doing — chasing, unless it was
            // heading home or searching.
            case EnemyState.Jump:
                stateAfterJump = previous == EnemyState.Return || previous == EnemyState.Search
                    ? previous
                    : EnemyState.Chase;
                break;
        }

        // While after the player, keep track of them outside the sight cone.
        if (detection != null)
            detection.SetAlerted(IsAlertState(entering));
    }
}
