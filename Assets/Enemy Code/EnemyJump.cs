using System.Collections.Generic;
using UnityEngine;

// Works out where the enemy can jump and the velocity that gets it there.
// EnemyController decides *when* to jump (follow the player onto a platform,
// cross a gap or wall, drop off a ledge); this answers *where* and *how*,
// and EnemyMovement does the actual launch.
//
// Every jump is checked before it happens: there must be room to stand where
// it lands and nothing solid along the arc, so the enemy never jumps into
// the underside of a platform or off into a pit. The arc is simulated step by
// step the same way the physics engine moves the Rigidbody, so where the
// check says it lands is where it really lands.
[RequireComponent(typeof(EnemyMovement))]
public class EnemyJump : MonoBehaviour
{
    [Header("Height")]
    [Tooltip("Jump as high as the player's full (held) jump. Worked out from the player's " +
             "MovementScript.jumpForce and Rigidbody2D gravity when the game starts, so it " +
             "stays matched if the player's jump is retuned.")]
    public bool matchPlayerJumpHeight = true;

    [Tooltip("Jump height in world units, used when not matching the player (or no player is found).")]
    public float jumpHeight = 1.7f;

    [Header("Reach")]
    [Tooltip("Furthest the enemy will jump sideways, in world units.")]
    public float maxJumpDistance = 4f;

    [Tooltip("Fastest the enemy travels sideways in a jump, as a multiple of EnemyMovement.moveSpeed.")]
    public float maxAirSpeedMultiplier = 1.5f;

    [Tooltip("A ledge must be at least this far below the top of the jump, so the feet clear its edge.")]
    public float ledgeClearance = 0.2f;

    [Tooltip("How far in from a platform's edge the enemy aims to land.")]
    public float landingInset = 0.5f;

    [Header("Dropping Down")]
    [Tooltip("Highest drop the enemy will take on purpose to follow the player down.")]
    public float maxDropHeight = 4f;

    [Tooltip("Height of the little hop the enemy makes when stepping off a ledge.")]
    public float dropHopHeight = 0.3f;

    [Header("Timing")]
    [Tooltip("Seconds after landing before the enemy can jump again.")]
    public float jumpCooldown = 0.4f;

    [Tooltip("When no jump is found, seconds before EnemyController searches again " +
             "(a search is a lot of physics queries, so it isn't done every frame).")]
    public float searchInterval = 0.2f;

    // =========================
    // PRIVATE VARIABLES
    // =========================

    private Rigidbody2D rb;
    private EnemyMovement movement;
    private EnemyAnimation anim;
    private EnemyStatusEffects status;

    private float resolvedJumpHeight;
    private float cooldownTimer;
    private bool wasLaunched;

    // Step between the spots tested as landing places.
    private const float ScanStep = 0.25f;

    // The arc check uses a box slightly smaller than the body so brushing a
    // corner or standing on the ground at launch doesn't count as blocked.
    private const float Skin = 0.08f;

    // Surfaces closer than this in height count as "the same level".
    private const float SameLevel = 0.3f;

    // How close the enemy has to get to a jump-from spot to count as there.
    public const float ApproachTolerance = 0.15f;

    private readonly List<Candidate> candidates = new List<Candidate>();

    private struct Candidate
    {
        public Vector2 landing;
        public float score;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<EnemyMovement>();
        anim = GetComponent<EnemyAnimation>();
        status = GetComponent<EnemyStatusEffects>();

        resolvedJumpHeight = jumpHeight;
    }

    // Start rather than Awake: this reads another object (the player), and
    // every Awake has run by the time any Start does.
    void Start()
    {
        if (matchPlayerJumpHeight)
            MatchPlayerJumpHeight();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Start the cooldown on the frame the enemy lands.
        bool launched = movement.IsLaunched;

        if (wasLaunched && !launched)
            cooldownTimer = jumpCooldown;

        wasLaunched = launched;
    }

    // =========================
    // STATE
    // =========================

    public float JumpHeight => resolvedJumpHeight;

    // Downward acceleration on this enemy, in units/second².
    public float Gravity => -Physics2D.gravity.y * rb.gravityScale;

    // Standing on the ground and not just landed.
    public bool CanJump => JumpAllowed && cooldownTimer <= 0f && !movement.IsLaunched && movement.IsGrounded();

    // False while a status condition forbids jumping (also stops leaps and
    // retreat hops).
    public bool JumpAllowed => status == null || !status.CannotJump;

    // =========================
    // JUMPING
    // =========================

    // A jump the enemy is "doing" (following the player, crossing a gap):
    // turns to face the way it's going and plays the jump animation.
    public void Jump(Vector2 velocity)
    {
        movement.Face(velocity.x);
        movement.Launch(velocity);

        if (anim != null)
            anim.PlayJump();
    }

    // A launch that keeps the enemy's facing and leaves the animation to the
    // caller (a hop backwards, a leap attack).
    public void Hop(Vector2 velocity)
    {
        movement.Launch(velocity);
    }

    // =========================
    // FINDING JUMPS
    // =========================

    // The player is standing on something higher: find a spot on it (or on
    // anything higher on the way to it) the enemy can jump to from here.
    public bool TryFindJumpUp(Vector2 targetGround, out Vector2 velocity)
    {
        velocity = Vector2.zero;

        Vector2 feet = movement.GetFeetPosition();
        float maxRise = resolvedJumpHeight - ledgeClearance;
        float dx = targetGround.x - feet.x;

        candidates.Clear();

        // Look toward the player — both ways when they're nearly overhead.
        if (Mathf.Abs(dx) > 0.5f)
        {
            CollectUpCandidates(feet, Mathf.Sign(dx), maxRise, targetGround);
        }
        else
        {
            CollectUpCandidates(feet, 1f, maxRise, targetGround);
            CollectUpCandidates(feet, -1f, maxRise, targetGround);
        }

        candidates.Sort((a, b) => a.score.CompareTo(b.score));

        // Only check the arcs of the most promising few.
        int tries = Mathf.Min(candidates.Count, 6);

        for (int i = 0; i < tries; i++)
        {
            if (TryPlan(feet, candidates[i].landing, resolvedJumpHeight, ledgeClearance, MaxAirSpeed, out velocity))
                return true;
        }

        return false;
    }

    // A gap or a wall blocks the way: find the nearest spot past it, at the
    // same level or higher.
    public bool TryFindJumpAcross(float direction, out Vector2 velocity)
    {
        velocity = Vector2.zero;

        Vector2 feet = movement.GetFeetPosition();
        float sign = Mathf.Sign(direction);
        float maxRise = resolvedJumpHeight - ledgeClearance;

        for (float offset = FirstScanOffset(); offset <= maxJumpDistance; offset += ScanStep)
        {
            float x = feet.x + sign * offset;

            if (!TryGetSurface(new Vector2(x, feet.y + maxRise), maxRise + SameLevel, out float surfaceY))
                continue;

            Vector2 landing = new Vector2(x, surfaceY);

            if (!HasFooting(landing, sign))
                continue;

            if (TryPlan(feet, landing, resolvedJumpHeight, ledgeClearance, MaxAirSpeed, out velocity))
                return true;
        }

        return false;
    }

    // The player is below: find somewhere safe to hop down to past the ledge
    // in front. False if the drop is too far or there's nothing to land on.
    public bool TryFindDropDown(float direction, out Vector2 velocity)
    {
        velocity = Vector2.zero;

        Vector2 feet = movement.GetFeetPosition();
        float sign = Mathf.Sign(direction);

        for (float offset = FirstScanOffset(); offset <= maxJumpDistance; offset += ScanStep)
        {
            float x = feet.x + sign * offset;

            if (!TryGetSurface(new Vector2(x, feet.y + 0.1f), maxDropHeight + 0.1f, out float surfaceY))
                continue;

            if (surfaceY > feet.y - SameLevel)
                continue;

            Vector2 landing = new Vector2(x, surfaceY);

            if (!HasFooting(landing, sign))
                continue;

            if (TryPlan(feet, landing, dropHopHeight, 0f, MaxAirSpeed, out velocity))
                return true;
        }

        return false;
    }

    // The goal's platform is overhead (or a wall/overhang is in the way), so
    // a jump from here would hit its underside. Finds a spot on the enemy's
    // level, out past one of the platform's edges, that a jump onto it can be
    // made from — and where that jump lands.
    public bool TryFindApproachPoint(Vector2 goalGround, out float approachX, out Vector2 landing)
    {
        approachX = 0f;
        landing = Vector2.zero;

        Vector2 feet = movement.GetFeetPosition();
        float halfWidth = GetBodySize().x * 0.5f;

        bool hasLeft = TryFindSurfaceEdge(goalGround, -1f, out float leftEdge);
        bool hasRight = TryFindSurfaceEdge(goalGround, 1f, out float rightEdge);

        // Try the edge nearer the enemy first. An edge that wasn't found
        // (the surface runs on and on, like the top of a big step) is skipped.
        bool leftFirst = !hasRight || (hasLeft && Mathf.Abs(feet.x - leftEdge) <= Mathf.Abs(feet.x - rightEdge));

        for (int i = 0; i < 2; i++)
        {
            bool useLeft = (i == 0) == leftFirst;

            if (useLeft ? !hasLeft : !hasRight)
                continue;

            float edge = useLeft ? leftEdge : rightEdge;
            float outward = useLeft ? -1f : 1f;
            Vector2 target = new Vector2(edge - outward * landingInset, goalGround.y);

            for (float back = halfWidth + 0.3f; back <= maxJumpDistance; back += 0.4f)
            {
                float x = edge + outward * back;

                // Somewhere to stand on the enemy's own level.
                if (!TryGetSurface(new Vector2(x, feet.y + 0.5f), 1f, out float standY))
                    continue;

                if (Mathf.Abs(standY - feet.y) > SameLevel)
                    continue;

                // The enemy stops anywhere within ApproachTolerance of the
                // spot, so the jump has to work from the whole range — a spot
                // that only just clears the platform's corner from its exact
                // x would leave the enemy stuck a hair short of it.
                if (TryPlan(new Vector2(x, standY), target, resolvedJumpHeight, ledgeClearance, MaxAirSpeed, out _) &&
                    TryPlan(new Vector2(x - outward * ApproachTolerance, standY), target, resolvedJumpHeight, ledgeClearance, MaxAirSpeed, out _) &&
                    TryPlan(new Vector2(x + outward * ApproachTolerance, standY), target, resolvedJumpHeight, ledgeClearance, MaxAirSpeed, out _))
                {
                    approachX = x;
                    landing = target;
                    return true;
                }
            }
        }

        return false;
    }

    // The top of the wall/step/overhang right in front, if it's low enough
    // to jump onto.
    public bool TryGetObstacleTop(float direction, out Vector2 top)
    {
        top = Vector2.zero;

        Vector2 feet = movement.GetFeetPosition();
        float sign = Mathf.Sign(direction);
        float halfWidth = GetBodySize().x * 0.5f;
        float maxRise = resolvedJumpHeight - ledgeClearance;

        for (float offset = halfWidth + 0.05f; offset <= halfWidth + 1f; offset += 0.15f)
        {
            float x = feet.x + sign * offset;

            if (TryGetSurface(new Vector2(x, feet.y + maxRise), maxRise, out float y) && y > feet.y + 0.1f)
            {
                top = new Vector2(x, y);
                return true;
            }
        }

        return false;
    }

    // A full-height jump from where the enemy stands to `landing`.
    public bool TryPlanJumpTo(Vector2 landing, out Vector2 velocity)
    {
        return TryPlan(movement.GetFeetPosition(), landing, resolvedJumpHeight, ledgeClearance, MaxAirSpeed, out velocity);
    }

    // A short hop of about `distance` toward `direction` that lands on the
    // same level (used for backing off after attacking). Tries a shorter hop
    // if the full one would go off a ledge or into a wall.
    public bool TryFindHop(float direction, float distance, float height, out Vector2 velocity)
    {
        velocity = Vector2.zero;

        Vector2 feet = movement.GetFeetPosition();
        float sign = Mathf.Sign(direction);

        foreach (float hopDistance in new[] { distance, distance * 0.6f })
        {
            float x = feet.x + sign * hopDistance;

            if (!TryGetSurface(new Vector2(x, feet.y + height), height + SameLevel, out float surfaceY))
                continue;

            if (Mathf.Abs(surfaceY - feet.y) > SameLevel)
                continue;

            Vector2 landing = new Vector2(x, surfaceY);

            if (!HasFooting(landing, sign))
                continue;

            if (TryPlan(feet, landing, height, 0f, float.PositiveInfinity, out velocity))
                return true;
        }

        return false;
    }

    // A jump from where the enemy stands to `landing`, peaking at least
    // `height` above the take-off (used for the leap attack).
    public bool TryPlanLeap(Vector2 landing, float height, float maxSpeed, out Vector2 velocity)
    {
        Vector2 feet = movement.GetFeetPosition();
        float peak = Mathf.Max(height, landing.y - feet.y + ledgeClearance);

        return TryPlan(feet, landing, peak, 0f, maxSpeed, out velocity);
    }

    // =========================
    // SURFACES
    // =========================

    // The top of the ground under `point`, within `maxDistance`. False when
    // there's none, or when `point` is inside something solid.
    public bool TryGetSurface(Vector2 point, float maxDistance, out float surfaceY)
    {
        surfaceY = 0f;

        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.down, maxDistance, movement.groundLayer);

        // A ray that starts inside a collider hits it at distance 0.
        if (!hit || hit.distance <= 0f)
            return false;

        surfaceY = hit.point.y;
        return true;
    }

    // =========================
    // INTERNAL
    // =========================

    float MaxAirSpeed => movement.moveSpeed * maxAirSpeedMultiplier;

    // First spot past the front of the body worth testing.
    float FirstScanOffset()
    {
        return GetBodySize().x * 0.5f + ScanStep;
    }

    void CollectUpCandidates(Vector2 feet, float sign, float maxRise, Vector2 targetGround)
    {
        for (float offset = FirstScanOffset(); offset <= maxJumpDistance; offset += ScanStep)
        {
            float x = feet.x + sign * offset;

            // Ray from the highest reachable height down to just above the
            // enemy's own level: finds tops of things it could land on.
            if (!TryGetSurface(new Vector2(x, feet.y + maxRise), maxRise - SameLevel, out float surfaceY))
                continue;

            Vector2 landing = new Vector2(x, surfaceY);

            if (!HasFooting(landing, sign))
                continue;

            // Closest to the player's height first, then closest to the player.
            float score = Mathf.Abs(surfaceY - targetGround.y) * 4f + Mathf.Abs(x - targetGround.x);
            candidates.Add(new Candidate { landing = landing, score = score });
        }
    }

    // True when there's solid ground a landing inset behind `landing` too —
    // so the enemy doesn't land with its feet hanging over the near edge.
    bool HasFooting(Vector2 landing, float travelSign)
    {
        Vector2 behind = new Vector2(landing.x - travelSign * landingInset, landing.y + 0.1f);

        if (!TryGetSurface(behind, 0.3f, out float behindY))
            return false;

        return Mathf.Abs(behindY - landing.y) < 0.1f;
    }

    // Walks along the surface at `ground` in one direction until it ends
    // (a drop or a wall). Returns that end's x.
    bool TryFindSurfaceEdge(Vector2 ground, float sign, out float edgeX)
    {
        const float step = 0.2f;
        const float maxSearch = 20f;

        edgeX = ground.x;

        for (float offset = step; offset <= maxSearch; offset += step)
        {
            float x = ground.x + sign * offset;

            if (!TryGetSurface(new Vector2(x, ground.y + 0.2f), 0.4f, out float y) ||
                Mathf.Abs(y - ground.y) > 0.1f)
                return true;

            edgeX = x;
        }

        // No edge within reach (e.g. the main floor) — not a platform.
        return false;
    }

    // Works out the launch velocity for a jump from `from` to `landing` that
    // peaks `peak` above `from`, then flies it in a simulation to make sure
    // nothing gets in the way. `clearance` = how far above the landing the
    // peak must be.
    bool TryPlan(Vector2 from, Vector2 landing, float peak, float clearance, float maxSpeed, out Vector2 velocity)
    {
        velocity = Vector2.zero;

        float g = Gravity;

        if (g <= 0f || peak <= 0f)
            return false;

        float rise = landing.y - from.y;
        float vy = Mathf.Sqrt(2f * g * peak);

        if (!TryGetFlightTime(vy, rise, clearance, out float horizontalTravelPerSpeed))
            return false;

        float vx = (landing.x - from.x) / horizontalTravelPerSpeed;

        if (Mathf.Abs(vx) > maxSpeed)
            return false;

        if (!HasRoomToStand(landing))
            return false;

        if (movement.IsHazardAt(landing, GetBodySize()))
            return false;

        velocity = new Vector2(vx, vy);
        return IsArcClear(from, velocity, landing.y);
    }

    // Steps the vertical motion the way the physics engine does (velocity
    // first, then position, with the Rigidbody's damping) until it comes down
    // through `rise`. Outputs how far one unit of horizontal speed carries
    // the enemy in that time. False if the jump never gets high enough.
    bool TryGetFlightTime(float vy, float rise, float clearance, out float horizontalTravelPerSpeed)
    {
        horizontalTravelPerSpeed = 0f;

        float dt = Time.fixedDeltaTime;
        float g = Gravity;
        float damping = 1f / (1f + dt * rb.linearDamping);

        float y = 0f;
        float peakY = 0f;
        float vertical = vy;
        float horizontal = 1f;

        for (int i = 0; i < 1000; i++)
        {
            vertical = (vertical - g * dt) * damping;
            horizontal *= damping;

            y += vertical * dt;
            horizontalTravelPerSpeed += horizontal * dt;
            peakY = Mathf.Max(peakY, y);

            if (vertical < 0f && y <= rise)
                return peakY >= rise + clearance && horizontalTravelPerSpeed > 0f;
        }

        return false;
    }

    // Flies a body-sized box along the arc and checks it never hits ground.
    bool IsArcClear(Vector2 from, Vector2 velocity, float landingY)
    {
        Vector2 bodySize = GetBodySize();
        Vector2 boxSize = new Vector2(bodySize.x * 0.9f, Mathf.Max(0.05f, bodySize.y - Skin * 2f));
        Vector2 feetToCentre = new Vector2(0f, bodySize.y * 0.5f);

        float dt = Time.fixedDeltaTime;
        float g = Gravity;
        float damping = 1f / (1f + dt * rb.linearDamping);

        Vector2 position = from;
        Vector2 v = velocity;

        for (int i = 0; i < 1000; i++)
        {
            v = new Vector2(v.x, v.y - g * dt) * damping;
            Vector2 next = position + v * dt;

            // Down to the landing height — the landing itself was already
            // checked by HasRoomToStand.
            if (v.y < 0f && next.y <= landingY + Skin)
                return true;

            Vector2 step = next - position;
            float distance = step.magnitude;

            if (distance > 0f &&
                Physics2D.BoxCast(position + feetToCentre, boxSize, 0f, step / distance, distance, movement.groundLayer))
                return false;

            position = next;
        }

        return false;
    }

    // True when a body standing with its feet at `feet` overlaps no ground.
    bool HasRoomToStand(Vector2 feet)
    {
        Vector2 bodySize = GetBodySize();
        Vector2 boxSize = new Vector2(bodySize.x * 0.9f, Mathf.Max(0.05f, bodySize.y - Skin * 2f));

        return !Physics2D.OverlapBox(feet + new Vector2(0f, bodySize.y * 0.5f), boxSize, 0f, movement.groundLayer);
    }

    Vector2 GetBodySize()
    {
        Collider2D body = movement.GetBody();
        return body != null ? (Vector2)body.bounds.size : new Vector2(0.6f, 1f);
    }

    void MatchPlayerJumpHeight()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        MovementScript playerMovement = player.GetComponent<MovementScript>();
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();

        if (playerMovement == null || playerBody == null || playerBody.gravityScale <= 0f)
            return;

        float playerGravity = -Physics2D.gravity.y * playerBody.gravityScale;
        resolvedJumpHeight = playerMovement.jumpForce * playerMovement.jumpForce / (2f * playerGravity);
    }

    // =========================
    // GIZMOS
    // =========================

    void OnDrawGizmosSelected()
    {
        // How high the enemy can jump, as a line over its head.
        float height = Application.isPlaying ? resolvedJumpHeight : jumpHeight;
        Collider2D body = GetComponent<Collider2D>();
        Vector3 feet = body != null ? new Vector3(body.bounds.center.x, body.bounds.min.y, 0f) : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(feet + new Vector3(-0.5f, height, 0f), feet + new Vector3(0.5f, height, 0f));
        Gizmos.DrawLine(feet, feet + new Vector3(0f, height, 0f));
    }
}
