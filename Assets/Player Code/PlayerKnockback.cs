using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerKnockback : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Header("Impulse")]
    [Tooltip("Horizontal impulse applied to the player, pointing away from the attack source.")]
    [SerializeField] private float knockbackForce = 8f;

    [Tooltip("Extra upward impulse so the player pops off the ground a little.")]
    [SerializeField] private float upwardForce = 2f;

    [Header("Recovery")]
    [Tooltip("Linear damping applied while knocked back so the player slides to a stop.")]
    [SerializeField] private float knockbackDrag = 5f;

    [Tooltip("Seconds the knockback push lasts. Walking input can't fight the push " +
             "during this window.")]
    [SerializeField] private float knockbackDuration = 0.4f;

    [Header("Stun")]
    [Tooltip("When on, the player can't act at all (move, jump, parry, dash, block) " +
             "while stunned. When off, only the push itself is applied.")]
    [SerializeField] private bool stunEnabled = true;

    [Tooltip("Seconds the player is stunned after a hit. Only used if Stun Enabled is on.")]
    [SerializeField] private float stunDuration = 0.4f;

    // =========================
    // STATE
    // =========================

    private Rigidbody2D rb;
    private DashScript dash;
    private float defaultDrag;
    private float knockbackTimer;
    private float stunTimer;

    // True while the push is playing out. MovementScript checks this to stand
    // down and stop overwriting the impulse.
    public bool IsKnockedBack => knockbackTimer > 0f;

    // True while stunned. PlayerController1 checks this to lock all input.
    public bool IsStunned => stunTimer > 0f;

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
        dash = GetComponent<DashScript>();
        defaultDrag = rb.linearDamping;
    }

    // =========================
    // TICK
    // =========================

    void FixedUpdate()
    {
        if (stunTimer > 0f)
            stunTimer = Mathf.Max(0f, stunTimer - Time.fixedDeltaTime);

        if (knockbackTimer <= 0f)
            return;

        knockbackTimer -= Time.fixedDeltaTime;

        if (knockbackTimer <= 0f)
        {
            knockbackTimer = 0f;
            rb.linearDamping = defaultDrag;

            OnKnockbackEnded?.Invoke();
        }
    }

    // =========================
    // KNOCKBACK
    // =========================

    // sourcePosition is the world position the attack came from (the enemy).
    public void ApplyKnockback(Vector2 sourcePosition)
    {
        float direction = Mathf.Sign(transform.position.x - sourcePosition.x);

        if (direction == 0f)
            direction = 1f;

        // A dash in progress would overwrite the push every physics step.
        if (dash != null)
            dash.CancelDash();

        // Zero existing momentum so repeated hits don't stack into a launch.
        rb.linearVelocity = Vector2.zero;

        Vector2 impulse = new Vector2(direction * knockbackForce, upwardForce);
        rb.AddForce(impulse, ForceMode2D.Impulse);

        rb.linearDamping = knockbackDrag;
        knockbackTimer = knockbackDuration;

        if (stunEnabled)
            stunTimer = stunDuration;

        OnKnockbackStarted?.Invoke();
    }
}
