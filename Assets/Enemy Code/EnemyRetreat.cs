using UnityEngine;

// Backing off after a combo: the enemy hops backwards a little (still facing
// the player), pauses, then EnemyController sends it back in. Gives the
// player a window to breathe or punish, so a fight isn't non-stop pressure.
//
// Sometimes (retreatChance) the enemy holds its ground instead, so the
// pattern can't be read perfectly. The hop is checked first by EnemyJump —
// it never hops off a ledge or into a wall; it tries a shorter hop, or
// skips the retreat.
[RequireComponent(typeof(EnemyJump))]
public class EnemyRetreat : MonoBehaviour
{
    [Header("Choice")]
    [Tooltip("Chance (0–1) the enemy backs off after a combo. Otherwise it stands its ground " +
             "and starts the next combo after EnemyAttack.attackCooldown.")]
    [Range(0f, 1f)] public float retreatChance = 0.75f;

    [Header("Hop")]
    [Tooltip("How far back the enemy hops, in world units.")]
    public float hopDistance = 1.5f;

    [Tooltip("How high the hop goes, in world units.")]
    public float hopHeight = 0.5f;

    [Tooltip("Seconds the enemy waits after landing before coming back in.")]
    public float pauseAfterLanding = 0.4f;

    private EnemyJump jump;
    private EnemyMovement movement;
    private EnemyAnimation anim;

    private bool retreating;
    private bool hopLanded;
    private float pauseTimer;

    void Awake()
    {
        jump = GetComponent<EnemyJump>();
        movement = GetComponent<EnemyMovement>();
        anim = GetComponent<EnemyAnimation>();
    }

    void Update()
    {
        if (!retreating)
            return;

        // Still in the air.
        if (movement.IsLaunched)
            return;

        if (!hopLanded)
        {
            hopLanded = true;
            pauseTimer = pauseAfterLanding;
        }

        pauseTimer -= Time.deltaTime;

        if (pauseTimer <= 0f)
            retreating = false;
    }

    // True from the start of the hop until the pause after it is over.
    public bool IsRetreating => retreating;

    // Rolls retreatChance and, if it hits, hops away from `facing` (the way
    // the enemy faces — it keeps facing that way). Returns false when the
    // enemy should stand its ground: the roll missed, or there's no safe
    // spot to hop to.
    public bool TryStartRetreat(float facing)
    {
        if (Random.value >= retreatChance)
            return false;

        if (!jump.CanJump)
            return false;

        if (!jump.TryFindHop(-facing, hopDistance, hopHeight, out Vector2 velocity))
            return false;

        jump.Hop(velocity);

        if (anim != null)
            anim.PlayRetreat();

        retreating = true;
        hopLanded = false;

        return true;
    }

    // Stops tracking the retreat (e.g. the enemy got stunned mid-hop).
    public void Cancel()
    {
        retreating = false;
    }
}
