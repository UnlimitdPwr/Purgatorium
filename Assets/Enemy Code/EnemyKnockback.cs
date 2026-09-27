using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyKnockback : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Header("Impulse")]
    [Tooltip("Horizontal impulse applied to the enemy, pointing away from the attack source.")]
    [SerializeField] private float knockbackForce = 8f;

    [Tooltip("Extra upward impulse so the enemy pops off the ground a little.")]
    [SerializeField] private float upwardForce = 2f;

    [Header("Recovery")]
    [Tooltip("Linear damping applied while knocked back so the enemy slides to a stop.")]
    [SerializeField] private float knockbackDrag = 5f;

    [Tooltip("Seconds the enemy stays stunned (movement + AI locked) after a hit.")]
    [SerializeField] private float stunDuration = 0.4f;

    // =========================
    // STATE
    // =========================

    private Rigidbody2D rb;
    private float defaultDrag;
    private float stunTimer;

    // True while the enemy is being knocked back / stunned. EnemyMovement and
    // EnemyController both check this to stand down and stop fighting the impulse.
    public bool IsKnockedBack => stunTimer > 0f;

    // Hooks for VFX / audio. Fired once at the start and once at the end of a
    // knockback so callers can react without editing this script.
    public event Action OnKnockbackStarted;
    public event Action OnKnockbackEnded;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultDrag = rb.linearDamping;
    }

    // =========================
    // TICK
    // =========================

    void FixedUpdate()
    {
        if (stunTimer <= 0f)
            return;

        stunTimer -= Time.fixedDeltaTime;

        if (stunTimer <= 0f)
        {
            stunTimer = 0f;
            rb.linearDamping = defaultDrag;

            OnKnockbackEnded?.Invoke();
        }
    }

    // =========================
    // KNOCKBACK
    // =========================

    // sourcePosition is the world position the attack came from (the player).
    public void ApplyKnockback(Vector2 sourcePosition)
    {
        float direction = Mathf.Sign(transform.position.x - sourcePosition.x);

        if (direction == 0f)
            direction = 1f;

        // Zero existing momentum so repeated parries don't stack into a launch.
        rb.linearVelocity = Vector2.zero;

        Vector2 impulse = new Vector2(direction * knockbackForce, upwardForce);
        rb.AddForce(impulse, ForceMode2D.Impulse);

        rb.linearDamping = knockbackDrag;
        stunTimer = stunDuration;

        OnKnockbackStarted?.Invoke();
    }
}
