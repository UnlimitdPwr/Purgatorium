using System.Collections.Generic;
using UnityEngine;

// A trigger area that hurts the player for as long as they stand in it.
// Blocking doesn't help — acid isn't a hit you can guard against.
// The poison build-up planned for acid will hook in at DealTick() once that
// system exists; for now each tick is plain damage.
[RequireComponent(typeof(Collider2D))]
public class AcidPool : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Tooltip("Damage dealt each tick while the player is in the acid.")]
    [SerializeField] private int damagePerTick = 4;

    [Tooltip("Seconds between damage ticks. The first tick lands on entry.")]
    [SerializeField] private float tickInterval = 0.5f;

    // =========================
    // STATE
    // =========================

    // The player can have several colliders — they're only out of the acid
    // once all of them have left.
    private readonly HashSet<Collider2D> playerCollidersInside = new HashSet<Collider2D>();

    private PlayerHealth playerInside;
    private float tickTimer;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // =========================
    // ENTER / EXIT
    // =========================

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health == null)
            return;

        playerCollidersInside.Add(other);

        if (playerInside != null)
            return;

        playerInside = health;
        DealTick();
        tickTimer = tickInterval;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        playerCollidersInside.Remove(other);

        if (playerCollidersInside.Count == 0)
            playerInside = null;
    }

    // =========================
    // DAMAGE
    // =========================

    void Update()
    {
        if (playerInside == null)
            return;

        tickTimer -= Time.deltaTime;

        if (tickTimer > 0f)
            return;

        DealTick();
        tickTimer += tickInterval;
    }

    void DealTick()
    {
        playerInside.TakeDamage(damagePerTick, false);
    }
}
