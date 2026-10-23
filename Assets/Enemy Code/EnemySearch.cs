using UnityEngine;

// Searching for a player the enemy has lost: it remembers where it last
// actually saw them, walks there (jumping platforms if it has to), then looks
// left and right for a while before giving up and going home. Makes enemies
// harder to shake off than "out of sight, straight home".
//
// EnemyController decides when to search and drives the walking; this keeps
// the memory and the timers.
public class EnemySearch : MonoBehaviour
{
    [Tooltip("Seconds spent looking around the last-seen spot before giving up and going home.")]
    public float searchDuration = 4f;

    [Tooltip("Seconds between turning to look the other way while searching.")]
    public float lookInterval = 1f;

    [Tooltip("Walking speed while searching, as a fraction of EnemyMovement.moveSpeed.")]
    [Range(0.1f, 1f)] public float searchSpeedMultiplier = 0.7f;

    [Tooltip("How close to the last-seen spot counts as there.")]
    public float arriveDistance = 0.4f;

    [Tooltip("Stop heading for the last-seen spot after this many seconds (e.g. it can't be reached) " +
             "and search from wherever the enemy got to.")]
    public float maxTravelTime = 6f;

    private Vector2 lastSeen;
    private bool hasSighting;

    private bool arrived;
    private float travelTimer;
    private float searchTimer;
    private float lookTimer;
    private float lookDirection = 1f;

    // =========================
    // MEMORY
    // =========================

    // Called while the enemy can actually see or hear the player: `ground`
    // is the top of whatever the player stands on.
    public void RememberSighting(Vector2 ground)
    {
        lastSeen = ground;
        hasSighting = true;
    }

    public bool HasSighting => hasSighting;
    public Vector2 LastSeen => lastSeen;

    // =========================
    // SEARCHING
    // =========================

    // Starts a search; `facing` is the way the enemy looks first once there.
    public void Begin(float facing)
    {
        arrived = false;
        travelTimer = maxTravelTime;
        searchTimer = searchDuration;
        lookTimer = lookInterval;
        lookDirection = facing != 0f ? Mathf.Sign(facing) : 1f;
    }

    // Still on the way to the last-seen spot.
    public bool IsTravelling => !arrived;

    // Counts down the walk there; true once it's taken too long.
    public bool TravelTimedOut()
    {
        travelTimer -= Time.deltaTime;
        return travelTimer <= 0f;
    }

    // At the spot (or given up getting there): start looking around.
    public void MarkArrived()
    {
        arrived = true;
    }

    // Looking around. Outputs the way to face this frame; returns false once
    // the search is over.
    public bool LookAround(out float facing)
    {
        searchTimer -= Time.deltaTime;
        lookTimer -= Time.deltaTime;

        if (lookTimer <= 0f)
        {
            lookTimer = lookInterval;
            lookDirection = -lookDirection;
        }

        facing = lookDirection;

        if (searchTimer > 0f)
            return true;

        // Search over — forget the sighting so a later loss starts fresh.
        hasSighting = false;
        return false;
    }
}
