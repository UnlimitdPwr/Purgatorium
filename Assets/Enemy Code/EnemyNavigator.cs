using System;
using UnityEngine;

// What a navigation call did this frame.
public enum NavStep
{
    NotNeeded, // plain walking will do — the caller moves the enemy
    Handled,   // the navigator moved, stopped or turned the enemy itself
    Jumped     // a jump was launched — the controller switches to its Jump state
}

// Gets the enemy where EnemyController wants it to go. The controller decides
// WHERE (the player, home, the last-seen spot); this script works out HOW:
// walking without falling off ledges or shoving allies, jumping up, down or
// across, walking to a spot a jump can be made from, and getting past an ally
// that's in the way.
//
// It keeps a small plan between frames (a run-up spot, an ally to jump over).
// The controller calls ResetPlan() on every state change, since a plan belongs
// to whatever the enemy was doing.
public class EnemyNavigator : MonoBehaviour
{
    [Header("Platforms")]
    [Tooltip("The player counts as 'above' or 'below' the enemy when the ground under them " +
             "is at least this much higher or lower than the enemy's feet.")]
    public float levelDifference = 0.5f;

    private EnemyMovement movement;
    private EnemyTargeting targeting;
    private EnemyJump jump;

    // Platform following: where to stand to make a jump that can't be made
    // from here (e.g. onto a platform that's overhead), and where it lands.
    private bool hasApproachPoint;
    private float approachX;
    private Vector2 approachLanding;
    private float nextJumpSearchTime;
    private float nextAllyJumpSearchTime;

    // Set while backing up for a run-up to jump over this other enemy.
    private Collider2D blockingAlly;
    private float allyRunUpX;

    // True when the last search found no jump and no spot to jump from.
    private bool noWayFound;

    // True when the last search found no jump and no spot to jump from — the
    // controller then gives up on the goal (patrols here, searches from here).
    public bool NoWayFound => noWayFound;

    // False without EnemyJump: the enemy only ever walks.
    public bool CanJumpAtAll => jump != null;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        targeting = GetComponent<EnemyTargeting>();
        jump = GetComponent<EnemyJump>();
    }

    // Forgets the current plan (run-up spot, ally to jump over).
    public void ResetPlan()
    {
        hasApproachPoint = false;
        blockingAlly = null;
    }

    // =========================
    // WALKING
    // =========================

    // Moves in a direction unless that would walk off a ledge, into a wall
    // or into another enemy — in which case the enemy turns that way and waits.
    public void Walk(float direction)
    {
        if (direction == 0f)
        {
            movement.Stop();
            return;
        }

        // Never shove another enemy along.
        if (movement.GetAllyAhead(direction) != null)
        {
            movement.Stop();
            movement.Face(direction);
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

    // =========================
    // PLATFORMS
    // =========================

    // Gets the enemy to a goal on another level, or past a gap/wall:
    // jumps up, drops down, jumps across, or walks to a spot a jump can be
    // made from. `goalLevelKnown` is false when only the goal's x matters
    // (walking home).
    public NavStep Navigate(float direction, Vector2 goalGround, bool goalLevelKnown)
    {
        noWayFound = false;

        if (jump == null || movement.IsLaunched || !movement.IsGrounded())
            return NavStep.NotNeeded;

        Vector2 feet = movement.GetFeetPosition();
        float rise = goalLevelKnown ? goalGround.y - feet.y : 0f;

        bool above = rise > levelDifference;
        bool below = rise < -levelDifference;
        bool blocked = direction != 0f && !movement.CanMove(direction);

        // Same level and a clear path — plain walking works. (Unless the
        // enemy is backing up from a wall for a run-up: stepping back clears
        // the wall, but the plan still stands until the next search.)
        if (!above && !blocked && !hasApproachPoint)
            return NavStep.NotNeeded;

        if (Time.time >= nextJumpSearchTime && jump.CanJump)
        {
            nextJumpSearchTime = Time.time + jump.searchInterval;

            bool dropAllowed = below || !goalLevelKnown;

            if (TryJumpToward(direction, goalGround, above, dropAllowed, blocked))
                return NavStep.Jumped;

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
                return NavStep.Handled;
            }

            if (jump.CanJump && jump.TryPlanJumpTo(approachLanding, out Vector2 velocity))
            {
                Launch(velocity);
                return NavStep.Jumped;
            }

            movement.Stop();
            movement.Face(direction);
            return NavStep.Handled;
        }

        // No way up from here — wait below the player, facing them, instead
        // of pacing back and forth underneath.
        if (above && Mathf.Abs(goalGround.x - feet.x) < 1f)
        {
            movement.Stop();
            movement.Face(direction);
            return NavStep.Handled;
        }

        return NavStep.NotNeeded;
    }

    // Tries the jump that fits: up to the goal's level, down a ledge, or
    // across a gap/wall. Launches it and returns true if one was found.
    bool TryJumpToward(float direction, Vector2 goalGround, bool above, bool dropAllowed, bool blocked)
    {
        Vector2 velocity;

        if (above && jump.TryFindJumpUp(goalGround, out velocity))
        {
            Launch(velocity);
            return true;
        }

        if (blocked && dropAllowed && movement.IsLedgeAhead(direction) && jump.TryFindDropDown(direction, out velocity))
        {
            Launch(velocity);
            return true;
        }

        if (blocked && jump.TryFindJumpAcross(direction, out velocity))
        {
            Launch(velocity);
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
    public Vector2 GetTargetGround()
    {
        Vector2 targetFeet = targeting.GetTargetFeetPosition();

        if (jump != null && jump.TryGetSurface(targetFeet + new Vector2(0f, 0.1f), 20f, out float groundY))
            return new Vector2(targetFeet.x, groundY);

        return targetFeet;
    }

    void Launch(Vector2 velocity)
    {
        hasApproachPoint = false;
        jump.Jump(velocity);
    }

    // =========================
    // OTHER ENEMIES IN THE WAY
    // =========================

    // Another enemy is right in front. Instead of shoving it (into the
    // player), leap at the player over it, or jump over it — landing behind
    // the player when there's room — backing up a step first for a run-up
    // if it's too close to clear. If none of that works, wait.
    // `chasing`: false when just walking home (no leap, nothing to get behind).
    // `tryLeap`: asks the controller to start a leap attack now; true if it did.
    public NavStep HandleBlockingAlly(float direction, bool chasing, Func<bool> tryLeap)
    {
        // Keep allySpacing back from an ally that's in the way — but an ally
        // on the far side of the player isn't in the way.
        Collider2D ally = movement.GetAllyAhead(direction, movement.allySpacing);

        if (ally != null && chasing && targeting.TryGetTargetBounds(out Bounds playerBounds))
        {
            float sign = Mathf.Sign(direction);
            float allyNear = sign > 0f ? ally.bounds.min.x : ally.bounds.max.x;
            float playerNear = sign > 0f ? playerBounds.min.x : playerBounds.max.x;

            if ((allyNear - playerNear) * sign > 0f)
                ally = null;
        }

        // Only jump over an ally that has settled (attacking, waiting). One
        // that's walking the same way isn't in the way — just follow it at
        // the spacing gap; one that's hopping or walking about could end up
        // under the landing spot. Either way, hold back until it settles.
        if (ally != null && blockingAlly == null && IsAllyMoving(ally))
        {
            movement.Stop();
            movement.Face(direction);
            return NavStep.Handled;
        }

        if (ally == null)
        {
            // Not backing up for a run-up either — nothing to do.
            if (blockingAlly == null || !blockingAlly.enabled)
            {
                blockingAlly = null;
                return NavStep.NotNeeded;
            }

            ally = blockingAlly;
        }

        Vector2 feet = movement.GetFeetPosition();

        if (jump != null && jump.CanJump && Time.time >= nextAllyJumpSearchTime)
        {
            nextAllyJumpSearchTime = Time.time + jump.searchInterval;

            // Leaping from right up against the ally would clip it on take-off;
            // the plain jump below backs up for a run-up instead.
            if (chasing && tryLeap != null && GapTo(ally, direction) >= movement.allySpacing * 0.6f && tryLeap())
            {
                blockingAlly = null;
                return NavStep.Handled;
            }

            Bounds targetBounds = default;
            bool hasTarget = chasing && targeting.TryGetTargetBounds(out targetBounds);

            blockingAlly = null;

            if (jump.TryFindJumpPast(direction, ally.bounds, hasTarget, targetBounds, out Vector2 velocity, out float runUp))
            {
                if (velocity != Vector2.zero)
                {
                    Launch(velocity);
                    return NavStep.Jumped;
                }

                blockingAlly = ally;
                allyRunUpX = runUp;
            }
        }

        // Back up for the run-up, still facing the way it's going.
        if (blockingAlly != null && Mathf.Abs(allyRunUpX - feet.x) > EnemyJump.ApproachTolerance)
        {
            float back = Mathf.Sign(allyRunUpX - feet.x);

            if (movement.CanMove(back) && movement.GetAllyAhead(back) == null)
            {
                movement.Move(back);
                movement.Face(direction);
                return NavStep.Handled;
            }

            blockingAlly = null;
        }

        movement.Stop();
        movement.Face(direction);
        return NavStep.Handled;
    }

    // True when another enemy stands between this one and the player.
    public bool IsAllyBetweenMeAndTarget()
    {
        if (!targeting.TryGetTargetBounds(out Bounds playerBounds))
            return false;

        float direction = targeting.GetHorizontalDirectionToTarget();

        if (direction == 0f)
            return false;

        Collider2D body = movement.GetBody();
        float myEdge = body != null ? (direction > 0f ? body.bounds.max.x : body.bounds.min.x) : transform.position.x;
        float playerNear = direction > 0f ? playerBounds.min.x : playerBounds.max.x;

        return movement.GetAllyAhead(direction, Mathf.Max(0f, (playerNear - myEdge) * direction)) != null;
    }

    // Gap between this enemy's body and `other`'s, in `direction`.
    float GapTo(Collider2D other, float direction)
    {
        Collider2D body = movement.GetBody();

        if (body == null)
            return float.PositiveInfinity;

        return direction > 0f ? other.bounds.min.x - body.bounds.max.x : body.bounds.min.x - other.bounds.max.x;
    }

    static bool IsAllyMoving(Collider2D ally)
    {
        Rigidbody2D allyBody = ally.attachedRigidbody;
        return allyBody != null && allyBody.linearVelocity.sqrMagnitude > 0.3f * 0.3f;
    }
}
