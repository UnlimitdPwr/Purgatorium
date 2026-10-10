using UnityEngine;

// A boss room. When the player walks into the trigger area the gates close;
// when the boss dies they open again and any ladders drop.
//
// With no boss assigned yet (e.g. while the level is still a blockout), the
// encounter completes as soon as the player enters, so the route behind it can
// be tested. Right-click the component > Complete Encounter does the same.
[RequireComponent(typeof(Collider2D))]
public class ArenaEncounter : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Tooltip("The boss. Leave empty until the boss exists.")]
    [SerializeField] private EnemyHealth boss;

    [Tooltip("Closed when the fight starts, opened when it's won. A gate that " +
             "starts closed (Starts Open off) simply stays shut until the win.")]
    [SerializeField] private LevelGate[] gates;

    [Tooltip("Dropped into place when the fight is won.")]
    [SerializeField] private Ladder[] laddersToDrop;

    [Tooltip("With no boss assigned, finish the encounter as soon as the player enters.")]
    [SerializeField] private bool completeOnEnterWithoutBoss = true;

    // =========================
    // STATE
    // =========================

    private bool started;
    private bool completed;

    public bool IsCompleted => completed;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnEnable()
    {
        if (boss != null)
            boss.OnDeath += Complete;
    }

    void OnDisable()
    {
        if (boss != null)
            boss.OnDeath -= Complete;
    }

    // =========================
    // START
    // =========================

    void OnTriggerEnter2D(Collider2D other)
    {
        if (started || completed)
            return;

        if (other.GetComponentInParent<PlayerController1>() == null)
            return;

        started = true;

        if (boss == null || boss.IsDead)
        {
            if (boss != null || completeOnEnterWithoutBoss)
                Complete();

            return;
        }

        foreach (LevelGate gate in gates)
        {
            if (gate != null)
                gate.Close();
        }
    }

    // =========================
    // COMPLETE
    // =========================

    [ContextMenu("Complete Encounter")]
    public void Complete()
    {
        if (completed || !Application.isPlaying)
            return;

        completed = true;

        foreach (LevelGate gate in gates)
        {
            if (gate != null)
                gate.Open();
        }

        foreach (Ladder ladder in laddersToDrop)
        {
            if (ladder != null)
                ladder.Drop();
        }
    }
}
