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
    [SerializeField] private float upwardForce = 6f;

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

    [Header("Enemy Contact")]
    [Tooltip("Knock the player back when they bump into an enemy's body.")]
    [SerializeField] private bool knockbackOnEnemyContact = true;

    [Tooltip("Also deal that enemy's attack damage on contact. Blocking reduces it " +
             "the same way it reduces a real attack.")]
    [SerializeField] private bool damageOnEnemyContact = true;

    [Tooltip("Seconds after a knockback ends before touching an enemy can knock the " +
             "player back again — stops a pinned player being stun-locked.")]
    [SerializeField] private float contactCooldown = 0.5f;

    // =========================
    // STATE
    // =========================

    private Rigidbody2D rb;
    private DashScript dash;
    private PlayerDeath death;
    private PlayerHealth health;
    private float defaultDrag;
    private float knockbackTimer;
    private float stunTimer;
    private float contactCooldownTimer;

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
        death = GetComponent<PlayerDeath>();
        health = GetComponent<PlayerHealth>();
        defaultDrag = rb.linearDamping;
    }

    // =========================
    // TICK
    // =========================

    void FixedUpdate()
    {
        if (stunTimer > 0f)
            stunTimer = Mathf.Max(0f, stunTimer - Time.fixedDeltaTime);

        if (contactCooldownTimer > 0f)
            contactCooldownTimer = Mathf.Max(0f, contactCooldownTimer - Time.fixedDeltaTime);

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

        contactCooldownTimer = knockbackDuration + contactCooldown;

        OnKnockbackStarted?.Invoke();
    }

    // =========================
    // ENEMY CONTACT
    // =========================

    // Stay as well as Enter, so a player who keeps walking into an enemy after
    // the cooldown gets pushed off again.
    void OnCollisionEnter2D(Collision2D collision)
    {
        TryContactKnockback(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryContactKnockback(collision);
    }

    private void TryContactKnockback(Collision2D collision)
    {
        if (!knockbackOnEnemyContact || contactCooldownTimer > 0f)
            return;

        if (death != null && death.IsDead)
            return;

        // Dead enemies switch their colliders off, so any enemy we touch is alive.
        EnemyController enemy = collision.collider.GetComponentInParent<EnemyController>();

        if (enemy == null)
            return;

        if (damageOnEnemyContact && health != null)
        {
            EnemyAttack attack = enemy.GetComponent<EnemyAttack>();

            if (attack != null)
                health.TakeDamage(attack.damage);
        }

        ApplyKnockback(enemy.transform.position);
    }
}
