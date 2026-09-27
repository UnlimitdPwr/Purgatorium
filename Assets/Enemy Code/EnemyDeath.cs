using System.Collections;
using UnityEngine;

// Placeholder death behaviour until a real death animation exists: freeze the
// enemy's gameplay systems, drop its collision so it falls straight through
// the ground, and despawn once it's no longer visible to the camera.
public class EnemyDeath : MonoBehaviour
{
    [Header("Despawn")]
    [Tooltip("Safety net — despawn after this many seconds even if the corpse " +
             "never leaves camera view (e.g. the camera stops following the player).")]
    [SerializeField] private float maxLifetimeAfterDeath = 15f;

    private EnemyHealth health;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EnemyController controller;
    private EnemyMovement movement;
    private EnemyKnockback knockback;
    private EnemyAttack attack;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        controller = GetComponent<EnemyController>();
        movement = GetComponent<EnemyMovement>();
        knockback = GetComponent<EnemyKnockback>();
        attack = GetComponent<EnemyAttack>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        // Stop every gameplay system from acting on this enemy again.
        if (controller != null)
            controller.enabled = false;

        if (movement != null)
            movement.enabled = false;

        if (knockback != null)
            knockback.enabled = false;

        if (attack != null)
            attack.enabled = false;

        // No death animation yet — freeze the Animator so a queued animation
        // event (e.g. a mid-swing EnableHitbox call) can't fire after this.
        if (animator != null)
            animator.enabled = false;

        // Fall straight down under gravity: no drag, no residual sideways motion.
        if (rb != null)
        {
            rb.linearDamping = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Fall through everything, ground included.
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        StartCoroutine(DespawnWhenOffScreen());
    }

    private IEnumerator DespawnWhenOffScreen()
    {
        float deadline = Time.time + maxLifetimeAfterDeath;

        while (spriteRenderer != null && spriteRenderer.isVisible && Time.time < deadline)
        {
            yield return null;
        }

        if (this != null)
            Destroy(gameObject);
    }
}
