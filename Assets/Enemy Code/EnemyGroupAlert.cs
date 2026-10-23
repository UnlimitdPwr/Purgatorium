using System.Collections.Generic;
using UnityEngine;

// When this enemy notices the player, it alerts nearby allies on about the
// same level that it has a clear line to — so a group reacts together
// instead of being picked off one at a time. Allies that hear the alarm do
// their own short "noticed" pause (slightly staggered) and join in. They don't
// pass the alarm on, so it stays local.
//
// An enemy without this component neither sends nor receives alarms.
public class EnemyGroupAlert : MonoBehaviour
{
    [Tooltip("Allies within this distance hear the alarm.")]
    public float alertRadius = 7f;

    [Tooltip("Only allies within this height of this enemy hear it (roughly the same level).")]
    public float maxHeightDifference = 2f;

    [Tooltip("Allies hidden behind ground/walls don't hear it.")]
    public bool requireLineOfSight = true;

    [Tooltip("Random extra pause before each ally reacts, so a group doesn't move as one.")]
    public Vector2 responseDelay = new Vector2(0.1f, 0.4f);

    private static readonly List<EnemyGroupAlert> all = new List<EnemyGroupAlert>();

    // Domain reload is off when entering Play Mode, so statics survive
    // between runs — clear the list at the start of each one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        all.Clear();
    }

    private EnemyController controller;
    private EnemyMovement movement;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        movement = GetComponent<EnemyMovement>();
    }

    void OnEnable()
    {
        all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    // Tells every ally in range about the player.
    public void RaiseAlarm(Transform player)
    {
        Vector2 from = BodyCentre();

        foreach (EnemyGroupAlert other in all)
        {
            if (other == this || other.controller == null)
                continue;

            Vector2 to = other.BodyCentre();

            if (Vector2.Distance(from, to) > alertRadius)
                continue;

            if (Mathf.Abs(to.y - from.y) > maxHeightDifference)
                continue;

            if (requireLineOfSight && movement != null &&
                Physics2D.Linecast(from, to, movement.groundLayer))
                continue;

            other.controller.HearAlarm(player, Random.Range(responseDelay.x, responseDelay.y));
        }
    }

    Vector2 BodyCentre()
    {
        Collider2D body = movement != null ? movement.GetBody() : GetComponent<Collider2D>();
        return body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;
    }
}
